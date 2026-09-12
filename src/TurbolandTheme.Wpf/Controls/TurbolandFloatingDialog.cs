using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace TurbolandTheme.Wpf.Controls;

/// <summary>
/// A real top-level dialog window in the Turbo Vision style: its own HWND, owned by the
/// main window so the OS resolves z-order and modality, with the hard one-cell shadow
/// drawn as part of the window content on the transparent surface.
/// </summary>
/// <remarks>
/// <para>
/// This is the alternative to <see cref="TurbolandDialog"/>, which lives inside the main
/// window's client area. In-client dialogs cannot sort against content that uses its own
/// HWND or airspace - a WebView2, a D3DImage - because it is all just pixels in one
/// window. A floating dialog is a separate HWND, so owned-window rules place it above
/// the owner (and everything the owner is above) with no compositing tricks.
/// </para>
/// <para>
/// The shadow is why the window is transparent rather than rectangular:
/// <c>WindowStyle=None</c> + <c>AllowsTransparency=True</c> lets the window region be
/// the face plus its shadow, with the shadow's L-band painted as ordinary content in
/// the reserved margin. The main window avoids transparency to keep native resize and
/// Snap Layouts; a dialog has neither, so it can afford the look.
/// </para>
/// <para>
/// Set <see cref="Window.Owner"/> before showing. An owned window always stays above
/// its owner, which is the entire sorting resolution - and <c>ShowDialog</c> then gives
/// true OS modality for free, unlike the host's soft modality.
/// </para>
/// </remarks>
[TemplatePart(Name = TitleBarPart, Type = typeof(FrameworkElement))]
[TemplatePart(Name = CloseBoxPart, Type = typeof(ButtonBase))]
public class TurbolandFloatingDialog : Window
{
    public const string TitleBarPart = "PART_TitleBar";
    public const string CloseBoxPart = "PART_CloseBox";

    /// <summary>Resource key of the floating dialog style.</summary>
    public const string StyleKey = "Turboland.Style.TurbolandFloatingDialog";

    private FrameworkElement? _titleBar;
    private ButtonBase? _closeBox;
    private Point _moveOrigin;
    private bool _shownAsDialog;

    static TurbolandFloatingDialog()
    {
        // The shadow is content inside the window bounds, so unlike the in-client
        // dialog there is nothing to overhang; but the transparent margin around the
        // face must not be clipped away by a templated parent either.
        ClipToBoundsProperty.OverrideMetadata(
            typeof(TurbolandFloatingDialog), new FrameworkPropertyMetadata(false));
    }

    public TurbolandFloatingDialog()
    {
        SetResourceReference(StyleProperty, StyleKey);

        // The invariants of the transparent-window trick, set as local values so a
        // stray style or subclass cannot break them: AllowsTransparency requires
        // WindowStyle.None, and a non-transparent background would fill the shadow
        // room with an opaque rectangle.
        WindowStyle = WindowStyle.None;
        AllowsTransparency = true;
        Background = Brushes.Transparent;
        ResizeMode = ResizeMode.NoResize;
        ShowInTaskbar = false;
        SizeToContent = SizeToContent.WidthAndHeight;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;

        Focusable = true;
        KeyboardNavigation.SetTabNavigation(this, KeyboardNavigationMode.Cycle);

        CommandBindings.Add(new CommandBinding(MoveCommand, (_, _) => ToggleMove()));
        InputBindings.Add(new KeyBinding(MoveCommand, Key.F5, ModifierKeys.Control));
    }

    /// <summary>
    /// Enters or leaves keyboard move mode (<c>Ctrl+F5</c>, then arrows). Mouse dragging
    /// ships with the keyboard path, not after it - the same rule as
    /// <see cref="TurbolandDialog"/>.
    /// </summary>
    public static readonly RoutedCommand MoveCommand =
        new(nameof(MoveCommand), typeof(TurbolandFloatingDialog));

    // ---------------------------------------------------------------- properties

    public static readonly DependencyProperty CellWidthProperty =
        DependencyProperty.Register(nameof(CellWidth), typeof(double), typeof(TurbolandFloatingDialog),
            new FrameworkPropertyMetadata(8d));

    /// <summary>Character cell width, used to quantise keyboard movement.</summary>
    public double CellWidth
    {
        get => (double)GetValue(CellWidthProperty);
        set => SetValue(CellWidthProperty, value);
    }

    public static readonly DependencyProperty CellHeightProperty =
        DependencyProperty.Register(nameof(CellHeight), typeof(double), typeof(TurbolandFloatingDialog),
            new FrameworkPropertyMetadata(16d));

    public double CellHeight
    {
        get => (double)GetValue(CellHeightProperty);
        set => SetValue(CellHeightProperty, value);
    }

    private static readonly DependencyPropertyKey IsMoveModePropertyKey =
        DependencyProperty.RegisterReadOnly(nameof(IsMoveMode), typeof(bool), typeof(TurbolandFloatingDialog),
            new FrameworkPropertyMetadata(false));

    public static readonly DependencyProperty IsMoveModeProperty = IsMoveModePropertyKey.DependencyProperty;

    /// <summary>True while keyboard move mode is active (entered with <c>Ctrl+F5</c>).</summary>
    public bool IsMoveMode => (bool)GetValue(IsMoveModeProperty);

    // ---------------------------------------------------------------- showing

    /// <summary>
    /// Shows this dialog modally over <paramref name="owner"/>. Sets
    /// <see cref="Window.Owner"/> first, which is what keeps it above the main window
    /// and centres it there.
    /// </summary>
    public bool? ShowDialog(Window owner)
    {
        Owner = owner;
        return ShowDialog();
    }

    /// <summary>
    /// Shows this dialog modally. Identical to <see cref="Window.ShowDialog"/> except
    /// that it records the modal state so <see cref="Close(bool?)"/> can set
    /// <see cref="Window.DialogResult"/> without throwing on a modeless window.
    /// </summary>
    public new bool? ShowDialog()
    {
        _shownAsDialog = true;
        return base.ShowDialog();
    }

    /// <summary>
    /// Closes the dialog, recording <paramref name="result"/> as the dialog result when
    /// it was shown modally. On a modeless window the result has nowhere to go, so it
    /// is silently skipped rather than throwing.
    /// </summary>
    public void Close(bool? result)
    {
        if (_shownAsDialog)
            DialogResult = result;

        Close();
    }

    // ---------------------------------------------------------------- template

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        if (_titleBar is not null)
            _titleBar.MouseLeftButtonDown -= OnTitleBarMouseDown;

        if (_closeBox is not null)
            _closeBox.Click -= OnCloseBoxClick;

        _titleBar = GetTemplateChild(TitleBarPart) as FrameworkElement;
        _closeBox = GetTemplateChild(CloseBoxPart) as ButtonBase;

        if (_titleBar is not null)
            _titleBar.MouseLeftButtonDown += OnTitleBarMouseDown;

        if (_closeBox is not null)
            _closeBox.Click += OnCloseBoxClick;
    }

    private void OnCloseBoxClick(object sender, RoutedEventArgs e) => Close();

    /// <summary>Moves focus to the first focusable control inside the dialog.</summary>
    public void FocusFirstControl()
    {
        if (!MoveFocus(new TraversalRequest(FocusNavigationDirection.First)))
            Focus();
    }

    // ---------------------------------------------------------------- mouse

    private void OnTitleBarMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left && e.ButtonState == MouseButtonState.Pressed)
            DragMove();
    }

    // ---------------------------------------------------------------- keyboard

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        if (!IsMoveMode)
        {
            base.OnPreviewKeyDown(e);
            return;
        }

        // While moving, the arrows belong to the window rather than to whatever control
        // holds focus - hence Preview. Ctrl+F5 itself is an InputBinding.
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
        // Esc normally reaches the IsCancel button via WPF's own access-key handling,
        // which works here because this dialog IS a window - the focus scope root.
        // Only when there is no cancel button to invoke does Esc close the dialog
        // directly, matching TurbolandDialog's fallback.
        if (!e.Handled && e.Key == Key.Escape && !HasEnabledCancelButton())
            Close();

        base.OnKeyDown(e);
    }

    private bool HasEnabledCancelButton() =>
        FindButton(this, b => b.IsCancel && b.IsEnabled) is not null;

    /// <summary>Toggles keyboard move mode.</summary>
    public void ToggleMove()
    {
        if (IsMoveMode)
            EndMove(commit: true);
        else
            BeginMove();
    }

    /// <summary>
    /// Enters keyboard move mode: the arrow keys move the window by whole cells,
    /// <c>Enter</c> commits and <c>Esc</c> puts it back where it started.
    /// </summary>
    public void BeginMove()
    {
        if (IsMoveMode)
            return;

        _moveOrigin = new Point(Left, Top);
        // Re-align to the character grid, so a window dragged to an arbitrary pixel
        // snaps back onto the cell grid as soon as the keyboard touches it.
        Point snapped = DialogGeometry.SnapToCell(_moveOrigin, CellWidth, CellHeight);
        Left = snapped.X;
        Top = snapped.Y;
        SetValue(IsMoveModePropertyKey, true);
        Focus();
    }

    /// <summary>Leaves move mode, keeping the new position or restoring the old one.</summary>
    public void EndMove(bool commit)
    {
        if (!IsMoveMode)
            return;

        if (!commit)
        {
            Left = _moveOrigin.X;
            Top = _moveOrigin.Y;
        }

        SetValue(IsMoveModePropertyKey, false);
    }

    private void NudgeByCells(int cellsX, int rowsY)
    {
        Point moved = DialogGeometry.MoveByCells(
            new Point(Left, Top), cellsX, rowsY, CellWidth, CellHeight);
        Left = moved.X;
        Top = moved.Y;
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
