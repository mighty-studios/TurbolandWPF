using System;
using System.Windows;

namespace TurbolandTheme.Wpf.Controls;

/// <summary>
/// Placement arithmetic for in-client dialogs, kept free of WPF plumbing so it can be
/// unit tested without a window.
/// </summary>
/// <remarks>
/// <para>
/// The dialog is a character-grid object: its size and its shadow offset are whole
/// cells and rows, not pixels, so the arithmetic here is expressed in cells and
/// multiplied by the live cell metrics at the call site.
/// </para>
/// <para>
/// The clamp is the reason this class exists. An in-client dialog must be movable -
/// one that covers the text it is asking about is a usability failure - but a dialog
/// that can be dragged entirely off the client area is worse, because an in-client
/// overlay has no taskbar to recover it from. Constraining the <em>title bar</em>
/// rather than the whole dialog is what lets a dialog be shoved almost out of the way
/// without ever becoming unreachable.
/// </para>
/// </remarks>
public static class DialogGeometry
{
    /// <summary>Horizontal shadow offset, in cells.</summary>
    public const int ShadowCellsX = 2;

    /// <summary>Vertical shadow offset, in rows.</summary>
    public const int ShadowRowsY = 1;

    /// <summary>
    /// How much of the dialog's width must stay inside the host, in cells, so there is
    /// always something left to grab.
    /// </summary>
    public const int MinVisibleCells = 4;

    /// <summary>
    /// The offset of the hard shadow from the dialog's own top-left corner.
    /// </summary>
    /// <remarks>
    /// The shadow is the dialog rectangle translated by this vector and painted
    /// <em>behind</em> it - not an L-shaped border. Only the L is visible because the
    /// dialog covers the rest, which is why the two bands always have exactly the
    /// dialog's width and height.
    /// </remarks>
    public static Vector ShadowOffset(double cellWidth, double cellHeight) =>
        new(ShadowCellsX * cellWidth, ShadowRowsY * cellHeight);

    /// <summary>
    /// Constrains a desired dialog position so the title bar stays reachable.
    /// </summary>
    /// <param name="desired">Requested top-left, in host coordinates.</param>
    /// <param name="dialog">Rendered size of the dialog.</param>
    /// <param name="host">Size of the client area the dialog floats over.</param>
    /// <param name="titleBarHeight">Height of the draggable strip (one cell row).</param>
    /// <param name="minVisibleWidth">
    /// Width of dialog that must remain within the host horizontally.
    /// Clamped down to the dialog's own width so a narrow dialog is still placeable.
    /// </param>
    /// <remarks>
    /// Vertically the title bar is held <em>entirely</em> inside the host: it may sit
    /// flush against the top or the bottom but never half off, because a half-visible
    /// grab strip is not a grab strip. Horizontally the dialog may hang off either edge
    /// so long as <paramref name="minVisibleWidth"/> remains - that asymmetry is
    /// deliberate and is what "pushed mostly off-edge but never lost" means.
    /// </remarks>
    public static Point ClampPosition(
        Point desired,
        Size dialog,
        Size host,
        double titleBarHeight,
        double minVisibleWidth)
    {
        double visible = Math.Min(minVisibleWidth, dialog.Width);

        // Left edge may go negative (dialog hangs off the left) provided `visible`
        // pixels of its right-hand side remain on screen.
        double minLeft = visible - dialog.Width;
        double maxLeft = host.Width - visible;

        // A host narrower than the grab strip would invert the range; keep it sane.
        double left = maxLeft < minLeft ? minLeft : Clamp(desired.X, minLeft, maxLeft);

        double maxTop = Math.Max(0, host.Height - titleBarHeight);
        double top = Clamp(desired.Y, 0, maxTop);

        return new Point(left, top);
    }

    /// <summary>
    /// Moves a position by whole cells, for the keyboard move mode (<c>Ctrl+F5</c>,
    /// then arrows). Keyboard movement is quantised to the character grid; mouse
    /// movement is not, because the pointer is already continuous and snapping it
    /// makes dragging feel sticky.
    /// </summary>
    public static Point MoveByCells(Point origin, int cellsX, int rowsY, double cellWidth, double cellHeight) =>
        new(origin.X + cellsX * cellWidth, origin.Y + rowsY * cellHeight);

    /// <summary>
    /// Snaps a position onto the character grid, so a dialog that was dragged with the
    /// mouse re-aligns the moment it is moved from the keyboard.
    /// </summary>
    /// <remarks>
    /// Rounds half away from zero rather than to even. <see cref="Math.Round(double)"/>
    /// defaults to banker's rounding, which would snap an exact half-cell backwards -
    /// 100px against an 8px cell lands on 96, not 104 - and makes the first arrow press
    /// after entering move mode appear to go the wrong way.
    /// </remarks>
    public static Point SnapToCell(Point position, double cellWidth, double cellHeight) =>
        new(
            cellWidth > 0
                ? Math.Round(position.X / cellWidth, MidpointRounding.AwayFromZero) * cellWidth
                : position.X,
            cellHeight > 0
                ? Math.Round(position.Y / cellHeight, MidpointRounding.AwayFromZero) * cellHeight
                : position.Y);

    private static double Clamp(double value, double min, double max) =>
        value < min ? min : value > max ? max : value;
}
