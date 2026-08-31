using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace TurbolandTheme.Wpf.Controls;

/// <summary>
/// The surface that in-client dialogs float on. Drop one over the main content - it is
/// invisible and click-through until a dialog is opened on it.
/// </summary>
/// <remarks>
/// <para>
/// Dialogs live inside the main window's client area rather than in their own HWNDs,
/// because <c>AllowsTransparency="False"</c> - which <see cref="TurbolandWindow"/> needs to
/// keep the native resize, Snap Layouts and maximise behavior - means nothing can paint
/// outside the window rectangle, and the hard one-cell shadow must paint outside the
/// dialog. It also matches the original: Turbo Vision dialogs were regions inside a
/// single text-mode screen.
/// </para>
/// <para>
/// The price is that everything the OS would normally do for a dialog is ours: z-order,
/// modality, focus scoping. That is what this class provides.
/// </para>
/// </remarks>
public class TurbolandDialogHost : Panel
{
    static TurbolandDialogHost()
    {
        // The shadow deliberately paints outside each dialog's own bounds, and a dialog
        // may be dragged to hang off the host edge. Clipping either would be wrong;
        // the host's own parent (the window) provides the only clip that should apply.
        ClipToBoundsProperty.OverrideMetadata(
            typeof(TurbolandDialogHost), new FrameworkPropertyMetadata(false));
    }

    public TurbolandDialogHost()
    {
        Focusable = false;
        UpdateModalState();
    }

    /// <summary>Dialogs currently on the host, back to front.</summary>
    public IEnumerable<TurbolandDialog> Dialogs => InternalChildren.OfType<TurbolandDialog>();

    /// <summary>The frontmost dialog, or null when the host is empty.</summary>
    public TurbolandDialog? ActiveDialog => Dialogs.LastOrDefault();

    /// <summary>Adds a dialog, places it, brings it to the front and focuses it.</summary>
    /// <param name="dialog">The dialog to show.</param>
    /// <param name="position">
    /// Top-left in host coordinates, or null to centre it. Centring is deferred until the
    /// dialog has been measured, because a dialog sized to its content has no useful
    /// size until then.
    /// </param>
    public void Show(TurbolandDialog dialog, Point? position = null)
    {
        if (dialog is null || InternalChildren.Contains(dialog))
            return;

        dialog.CentreOnShow = position is null;
        if (position is Point p)
        {
            dialog.Left = p.X;
            dialog.Top = p.Y;
        }

        InternalChildren.Add(dialog);
        UpdateModalState();
        BringToFront(dialog);

        // Focus has to wait for the template: until the dialog is loaded there is
        // nothing inside it to move focus to, and MoveFocus would silently fail.
        if (dialog.IsLoaded)
        {
            dialog.FocusFirstControl();
        }
        else
        {
            void OnLoaded(object s, RoutedEventArgs e)
            {
                dialog.Loaded -= OnLoaded;
                dialog.FocusFirstControl();
            }

            dialog.Loaded += OnLoaded;
        }
    }

    /// <summary>Removes a dialog and returns focus to whatever is left in front.</summary>
    public void Remove(TurbolandDialog dialog)
    {
        if (dialog is null || !InternalChildren.Contains(dialog))
            return;

        InternalChildren.Remove(dialog);
        UpdateModalState();
        UpdateActiveFlags();
        ActiveDialog?.FocusFirstControl();
    }

    /// <summary>
    /// Raises a dialog above its siblings. Z-order in a <see cref="Panel"/> is child
    /// order, so this is a move within the collection rather than a ZIndex change -
    /// which also keeps <see cref="ActiveDialog"/> meaningful.
    /// </summary>
    public void BringToFront(TurbolandDialog dialog)
    {
        if (dialog is null)
            return;

        int index = InternalChildren.IndexOf(dialog);
        if (index >= 0 && index != InternalChildren.Count - 1)
        {
            InternalChildren.RemoveAt(index);
            InternalChildren.Add(dialog);
        }

        UpdateActiveFlags();
    }

    /// <summary>
    /// Soft modality: while any dialog is open the host swallows mouse input aimed
    /// at the content beneath it, but stays transparent so the main window remains
    /// readable - which is the entire point, since the dialog is usually asking about
    /// text the user needs to see.
    /// </summary>
    private void UpdateModalState()
    {
        bool hasDialogs = InternalChildren.Count > 0;

        // A null Background is not hit-testable, so an empty host lets clicks through
        // to the content below. Transparent is, so a populated host blocks them.
        Background = hasDialogs ? Brushes.Transparent : null;
        IsHitTestVisible = hasDialogs;
        Focusable = hasDialogs;
        KeyboardNavigation.SetTabNavigation(
            this, hasDialogs ? KeyboardNavigationMode.Cycle : KeyboardNavigationMode.Continue);
    }

    private void UpdateActiveFlags()
    {
        TurbolandDialog? active = ActiveDialog;
        foreach (TurbolandDialog dialog in Dialogs)
            dialog.SetIsActiveInternal(ReferenceEquals(dialog, active));
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        // Dialogs size to their content, so they are measured unconstrained rather than
        // against the host. A dialog larger than the host is still placeable - the clamp
        // guarantees its title bar stays reachable.
        Size unbounded = new(double.PositiveInfinity, double.PositiveInfinity);
        foreach (UIElement child in InternalChildren)
            child.Measure(unbounded);

        // The host fills whatever it is given; it never asks for space of its own.
        return new Size(
            double.IsInfinity(availableSize.Width) ? 0 : availableSize.Width,
            double.IsInfinity(availableSize.Height) ? 0 : availableSize.Height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        foreach (UIElement child in InternalChildren)
        {
            Size size = child.DesiredSize;

            if (child is TurbolandDialog dialog)
            {
                if (dialog.CentreOnShow)
                {
                    // Deferred from Show(): only now is the measured size known.
                    dialog.CentreIn(finalSize);
                    dialog.CentreOnShow = false;
                }

                Point placed = DialogGeometry.ClampPosition(
                    new Point(dialog.Left, dialog.Top),
                    size,
                    finalSize,
                    dialog.TitleBarHeight,
                    dialog.MinVisibleWidth);

                // Write the clamp back so a host that shrinks cannot strand a dialog
                // off-edge, and so the drag code always starts from a legal position.
                dialog.Left = placed.X;
                dialog.Top = placed.Y;
                child.Arrange(new Rect(placed, size));
            }
            else
            {
                child.Arrange(new Rect(new Point(0, 0), size));
            }
        }

        return finalSize;
    }
}
