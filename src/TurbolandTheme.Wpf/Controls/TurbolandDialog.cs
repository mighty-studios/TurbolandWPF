using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace TurbolandTheme.Wpf.Controls;

/// <summary>
/// A Turbo Vision dialog drawn <em>inside</em> the main window's client area: light-gray
/// face, white double-line frame, a close box on the top frame line and a hard one-cell
/// shadow. Show one with <see cref="TurbolandDialogHost.Show"/>.
/// </summary>
/// <remarks>
/// <para>
/// The dialog is movable, and movability is a hard requirement rather than a
/// nicety: a dialog that covers the text it is asking about is a usability failure.
/// It can be dragged by its title bar, moved from the keyboard (<c>Ctrl+F5</c>, then the
/// arrow keys, <c>Enter</c> to commit or <c>Esc</c> to revert) and collapsed to its frame
/// by double-clicking the title bar. The keyboard path ships with the mouse path, not
/// after it - mouse-only dragging would be an accessibility regression.
/// </para>
/// <para>
/// Modality is <em>soft</em>: the host blocks input to the content beneath, but the
/// dialog stays movable and the main window stays readable. Because we implement
/// modality rather than the OS, this is both easier than true modality and better suited
/// to the problem.
/// </para>
/// </remarks>
[TemplatePart(Name = TitleBarPart, Type = typeof(FrameworkElement))]
[TemplatePart(Name = CloseBoxPart, Type = typeof(ButtonBase))]
public class TurbolandDialog : ContentControl
{
    public const string TitleBarPart = "PART_TitleBar";
    public const string CloseBoxPart = "PART_CloseBox";

    /// <summary>Resource key of the dialog style, resolved dynamically like the window's.</summary>
    public const string StyleKey = "Turboland.Style.TurbolandDialog";

    private FrameworkElement? _titleBar;
    private ButtonBase? _closeBox;
    private Point _dragOffset;
    private Point _moveOrigin;

    static TurbolandDialog()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(TurbolandDialog), new FrameworkPropertyMetadata(typeof(TurbolandDialog)));
        // The shadow paints outside the dialog's own bounds by design.
        ClipToBoundsProperty.OverrideMetadata(
            typeof(TurbolandDialog), new FrameworkPropertyMetadata(false));
    }

    public TurbolandDialog()
    {
        // Match TurbolandWindow and ApplyTo: a resource reference rather than the implicit
        // style, because an implicit style matches an element's *exact* runtime type and
        // so would silently miss any dialog subclass.
        SetResourceReference(StyleProperty, StyleKey);

        Focusable = true;
        KeyboardNavigation.SetTabNavigation(this, KeyboardNavigationMode.Cycle);
        KeyboardNavigation.SetDirectionalNavigation(this, KeyboardNavigationMode.Contained);

        // Declared as bindings rather than handled inline in OnPreviewKeyDown, so the
        // gesture is inspectable (and testable) instead of buried in a key switch.
        CommandBindings.Add(new CommandBinding(MoveCommand, (_, _) => ToggleMove()));
        CommandBindings.Add(new CommandBinding(ShadeCommand, (_, _) => ToggleShade()));
        InputBindings.Add(new KeyBinding(MoveCommand, Key.F5, ModifierKeys.Control));
    }

    /// <summary>Raised after the dialog has been removed from its host.</summary>
    public event EventHandler? Closed;

    /// <summary>
    /// Enters or leaves keyboard move mode. Bound to <c>Ctrl+F5</c>, the Turbo Vision
    /// gesture, and exposed as a command so a window menu can offer "Move" too.
    /// </summary>
    public static readonly RoutedCommand MoveCommand =
        new(nameof(MoveCommand), typeof(TurbolandDialog));

    /// <summary>Collapses the dialog to its frame, or restores it.</summary>
    public static readonly RoutedCommand ShadeCommand =
        new(nameof(ShadeCommand), typeof(TurbolandDialog));

    // ---------------------------------------------------------------- properties

    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(nameof(Title), typeof(string), typeof(TurbolandDialog),
            new FrameworkPropertyMetadata(string.Empty));

    /// <summary>Text drawn on the top frame line, between the close box and the corner.</summary>
    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public static readonly DependencyProperty LeftProperty =
        DependencyProperty.Register(nameof(Left), typeof(double), typeof(TurbolandDialog),
            new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.AffectsParentArrange));

    /// <summary>Position within the host, in device-independent pixels.</summary>
    public double Left
    {
        get => (double)GetValue(LeftProperty);
        set => SetValue(LeftProperty, value);
    }

    public static readonly DependencyProperty TopProperty =
        DependencyProperty.Register(nameof(Top), typeof(double), typeof(TurbolandDialog),
            new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.AffectsParentArrange));

    public double Top
    {
        get => (double)GetValue(TopProperty);
        set => SetValue(TopProperty, value);
    }

    public static readonly DependencyProperty IsShadedProperty =
        DependencyProperty.Register(nameof(IsShaded), typeof(bool), typeof(TurbolandDialog),
            new FrameworkPropertyMetadata(false));

    /// <summary>
    /// Collapsed to its frame, hiding the content. This is the "minimise but stay bound
    /// to the window" affordance - an in-client dialog has no taskbar to minimise to.
    /// </summary>
    public bool IsShaded
    {
        get => (bool)GetValue(IsShadedProperty);
        set => SetValue(IsShadedProperty, value);
    }

    private static readonly DependencyPropertyKey IsActivePropertyKey =
        DependencyProperty.RegisterReadOnly(nameof(IsActive), typeof(bool), typeof(TurbolandDialog),
            new FrameworkPropertyMetadata(false));

    public static readonly DependencyProperty IsActiveProperty = IsActivePropertyKey.DependencyProperty;

    /// <summary>True when this is the frontmost dialog on its host.</summary>
    public bool IsActive => (bool)GetValue(IsActiveProperty);

    private static readonly DependencyPropertyKey IsMoveModePropertyKey =
        DependencyProperty.RegisterReadOnly(nameof(IsMoveMode), typeof(bool), typeof(TurbolandDialog),
            new FrameworkPropertyMetadata(false));

    public static readonly DependencyProperty IsMoveModeProperty = IsMoveModePropertyKey.DependencyProperty;

    /// <summary>True while keyboard move mode is active (entered with <c>Ctrl+F5</c>).</summary>
    public bool IsMoveMode => (bool)GetValue(IsMoveModeProperty);

    public static readonly DependencyProperty TitleBarHeightProperty =
        DependencyProperty.Register(nameof(TitleBarHeight), typeof(double), typeof(TurbolandDialog),
            new FrameworkPropertyMetadata(16d));

    /// <summary>Height of the draggable strip; the clamp keeps this much on the host.</summary>
    public double TitleBarHeight
    {
        get => (double)GetValue(TitleBarHeightProperty);
        set => SetValue(TitleBarHeightProperty, value);
    }

    public static readonly DependencyProperty MinVisibleWidthProperty =
        DependencyProperty.Register(nameof(MinVisibleWidth), typeof(double), typeof(TurbolandDialog),
            new FrameworkPropertyMetadata(32d));

    /// <summary>How much of the dialog must stay within the host horizontally.</summary>
    public double MinVisibleWidth
    {
        get => (double)GetValue(MinVisibleWidthProperty);
        set => SetValue(MinVisibleWidthProperty, value);
    }

    public static readonly DependencyProperty CellWidthProperty =
        DependencyProperty.Register(nameof(CellWidth), typeof(double), typeof(TurbolandDialog),
            new FrameworkPropertyMetadata(8d));

    /// <summary>Character cell width, used to quantise keyboard movement.</summary>
    public double CellWidth
    {
        get => (double)GetValue(CellWidthProperty);
        set => SetValue(CellWidthProperty, value);
    }

    public static readonly DependencyProperty CellHeightProperty =
        DependencyProperty.Register(nameof(CellHeight), typeof(double), typeof(TurbolandDialog),
            new FrameworkPropertyMetadata(16d));

    public double CellHeight
    {
        get => (double)GetValue(CellHeightProperty);
        set => SetValue(CellHeightProperty, value);
    }

    /// <summary>Result recorded by <see cref="Close"/>, for the caller to inspect.</summary>
    public bool? DialogResult { get; private set; }

    /// <summary>Set by the host when it must centre the dialog after the first measure.</summary>
    internal bool CentreOnShow { get; set; }

    /// <summary>The host this dialog is on, or null if it is not shown.</summary>
    public TurbolandDialogHost? Host => VisualTreeHelper.GetParent(this) as TurbolandDialogHost;

    // ---------------------------------------------------------------- lifetime

    /// <summary>Removes the dialog from its host and raises <see cref="Closed"/>.</summary>
    public void Close(bool? result = null)
    {
        DialogResult = result;
        EndMove(commit: true);
        Host?.Remove(this);
        Closed?.Invoke(this, EventArgs.Empty);
    }

    internal void SetIsActiveInternal(bool value) => SetValue(IsActivePropertyKey, value);

    internal void CentreIn(Size host)
    {
        Point centred = new(
            (host.Width - DesiredSize.Width) / 2,
            (host.Height - DesiredSize.Height) / 2);
        Point snapped = DialogGeometry.SnapToCell(centred, CellWidth, CellHeight);
        Left = snapped.X;
        Top = snapped.Y;
    }

    /// <summary>Moves focus to the first focusable control inside the dialog.</summary>
    public void FocusFirstControl()
    {
        if (!MoveFocus(new TraversalRequest(FocusNavigationDirection.First)))
            Focus();
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        if (_titleBar is not null)
        {
            _titleBar.MouseLeftButtonDown -= OnTitleBarMouseDown;
            _titleBar.MouseLeftButtonUp -= OnTitleBarMouseUp;
            _titleBar.MouseMove -= OnTitleBarMouseMove;
        }

        if (_closeBox is not null)
            _closeBox.Click -= OnCloseBoxClick;

        _titleBar = GetTemplateChild(TitleBarPart) as FrameworkElement;
        _closeBox = GetTemplateChild(CloseBoxPart) as ButtonBase;

        if (_titleBar is not null)
        {
            _titleBar.MouseLeftButtonDown += OnTitleBarMouseDown;
            _titleBar.MouseLeftButtonUp += OnTitleBarMouseUp;
            _titleBar.MouseMove += OnTitleBarMouseMove;
        }

        if (_closeBox is not null)
            _closeBox.Click += OnCloseBoxClick;
    }

    private void OnCloseBoxClick(object sender, RoutedEventArgs e) => Close(false);

    // ---------------------------------------------------------------- mouse

    protected override void OnPreviewMouseDown(MouseButtonEventArgs e)
    {
        // Any click anywhere in the dialog raises it, which is the only z-order rule an
        // overlapping-window UI needs.
        Host?.BringToFront(this);
        base.OnPreviewMouseDown(e);
    }

    private void OnTitleBarMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            ToggleShade();
            e.Handled = true;
            return;
        }

        // Grab the pointer's offset within the dialog so the dialog does not jump to
        // put its corner under the cursor.
        _dragOffset = e.GetPosition(this);
        _titleBar?.CaptureMouse();
        e.Handled = true;
    }

    private void OnTitleBarMouseMove(object sender, MouseEventArgs e)
    {
        if (_titleBar is null || !_titleBar.IsMouseCaptured || Host is not TurbolandDialogHost host)
            return;

        Point pointer = e.GetPosition(host);
        MoveTo(new Point(pointer.X - _dragOffset.X, pointer.Y - _dragOffset.Y));
    }

    private void OnTitleBarMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (_titleBar?.IsMouseCaptured == true)
            _titleBar.ReleaseMouseCapture();
    }

    /// <summary>Collapses the dialog to its frame, or restores it.</summary>
    public void ToggleShade() => IsShaded = !IsShaded;

    // ---------------------------------------------------------------- keyboard

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        if (!IsMoveMode)
        {
            base.OnPreviewKeyDown(e);
            return;
        }

        // While moving, the arrows belong to the dialog rather than to whatever control
        // happens to hold focus - hence Preview. Ctrl+F5 itself is an InputBinding.
        switch (e.Key)
        {
            case Key.Left: NudgeByCells(-1, 0); e.Handled = true; return;
            case Key.Right: NudgeByCells(1, 0); e.Handled = true; return;
            case Key.Up: NudgeByCells(0, -1); e.Handled = true; return;
            case Key.Down: NudgeByCells(0, 1); e.Handled = true; return;
            case Key.Enter: EndMove(commit: true); e.Handled = true; return;
            case Key.Escape: EndMove(commit: false); e.Handled = true; return;
        }

        base.OnPreviewKeyDown(e);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        // Bubbling, not preview: a control that wants Enter or Esc for itself (a
        // multi-line TextBox, an open ComboBox) marks it handled and never reaches here.
        if (!e.Handled)
        {
            switch (e.Key)
            {
                case Key.Escape:
                    // A Cancel button may have cleanup of its own; only fall back to
                    // closing directly when the dialog does not have one.
                    if (!InvokeCancelButton())
                        Close(false);
                    e.Handled = true;
                    return;
                case Key.Enter:
                    if (InvokeDefaultButton())
                    {
                        e.Handled = true;
                        return;
                    }
                    break;
            }
        }

        base.OnKeyDown(e);
    }

    /// <summary>Toggles keyboard move mode.</summary>
    public void ToggleMove()
    {
        if (IsMoveMode)
            EndMove(commit: true);
        else
            BeginMove();
    }

    /// <summary>
    /// Enters keyboard move mode: the arrow keys move the dialog by whole cells,
    /// <c>Enter</c> commits and <c>Esc</c> puts it back where it started.
    /// </summary>
    public void BeginMove()
    {
        if (IsMoveMode)
            return;

        _moveOrigin = new Point(Left, Top);
        // Re-align to the character grid, so a dialog dragged to an arbitrary pixel
        // snaps back onto the cell grid as soon as the keyboard touches it.
        MoveTo(DialogGeometry.SnapToCell(_moveOrigin, CellWidth, CellHeight));
        SetValue(IsMoveModePropertyKey, true);
        Focus();
    }

    /// <summary>Leaves move mode, keeping the new position or restoring the old one.</summary>
    public void EndMove(bool commit)
    {
        if (!IsMoveMode)
            return;

        if (!commit)
            MoveTo(_moveOrigin);

        SetValue(IsMoveModePropertyKey, false);
    }

    private void NudgeByCells(int cellsX, int rowsY) =>
        MoveTo(DialogGeometry.MoveByCells(
            new Point(Left, Top), cellsX, rowsY, CellWidth, CellHeight));

    /// <summary>Applies a position, clamped so the title bar stays reachable.</summary>
    private void MoveTo(Point position)
    {
        Size host = Host is TurbolandDialogHost h
            ? new Size(h.ActualWidth, h.ActualHeight)
            : new Size(double.PositiveInfinity, double.PositiveInfinity);

        Point clamped = DialogGeometry.ClampPosition(
            position, RenderSize, host, TitleBarHeight, MinVisibleWidth);

        Left = clamped.X;
        Top = clamped.Y;
    }

    /// <summary>
    /// Fires the dialog's default button. Done explicitly rather than relying on
    /// <see cref="Button.IsDefault"/>, which resolves against the focus scope root - a
    /// Window in the normal case, and this dialog is not one.
    /// </summary>
    private bool InvokeDefaultButton() => InvokeButton(FindButton(this, b => b.IsDefault));

    /// <summary>
    /// Fires the dialog's cancel button, so <c>Esc</c> runs whatever cleanup that button
    /// does instead of bypassing it.
    /// </summary>
    private bool InvokeCancelButton() => InvokeButton(FindButton(this, b => b.IsCancel));

    private static bool InvokeButton(Button? button)
    {
        if (button is null || !button.IsEnabled)
            return false;

        // Not ButtonAutomationPeer.Invoke: that posts the click through the dispatcher at
        // Input priority, so it happens some time after the key was reported handled.
        // Raising Click directly is synchronous but skips the command, which ButtonBase
        // would normally run from OnClick - hence both halves here.
        button.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent, button));

        if (button.Command is { } command)
        {
            object? parameter = button.CommandParameter;
            if (command is RoutedCommand routed)
            {
                IInputElement target = button.CommandTarget ?? button;
                if (routed.CanExecute(parameter, target))
                    routed.Execute(parameter, target);
            }
            else if (command.CanExecute(parameter))
            {
                command.Execute(parameter);
            }
        }

        return true;
    }

    private static Button? FindButton(DependencyObject root, Func<Button, bool> predicate)
    {
        int count = VisualTreeHelper.GetChildrenCount(root);
        for (int i = 0; i < count; i++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(root, i);
            if (child is Button button && predicate(button))
                return button;

            if (FindButton(child, predicate) is Button found)
                return found;
        }

        return null;
    }
}
