using System.Windows;
using TurbolandTheme.Wpf.Controls;
using Xunit;

namespace TurbolandTheme.Tests.VisualRegression;

/// <summary>
/// Pins the dialog placement arithmetic. None of this needs a window, and none of it is
/// visible when it breaks: a wrong clamp does not throw, it just lets a dialog be dragged
/// somewhere it can never be dragged back from.
/// </summary>
public class DialogGeometryTest
{
    private const double Cell = 8;
    private const double Row = 16;
    private static readonly Size Host = new(800, 600);
    private static readonly Size Dialog = new(320, 160);

    [Fact]
    public void Shadow_IsTwoCellsRightAndOneRowDown()
    {
        // Two cells right (18px against a 9px cell) and one row down (16px against a
        // 16px row).
        Vector offset = DialogGeometry.ShadowOffset(Cell, Row);

        Assert.Equal(2 * Cell, offset.X);
        Assert.Equal(Row, offset.Y);
    }

    [Fact]
    public void Shadow_ScalesWithTheCell()
    {
        Vector offset = DialogGeometry.ShadowOffset(Cell * 2, Row * 2);

        Assert.Equal(32, offset.X);
        Assert.Equal(32, offset.Y);
    }

    [Fact]
    public void Clamp_LeavesALegalPositionAlone()
    {
        Point placed = Clamp(new Point(100, 100));

        Assert.Equal(new Point(100, 100), placed);
    }

    [Fact]
    public void Clamp_HoldsTheTitleBarFullyOnTheHost()
    {
        // Dragged far below the bottom edge: the title bar must stop flush against it,
        // never half off, because half a grab strip is not a grab strip.
        Point placed = Clamp(new Point(100, 5000));

        Assert.Equal(Host.Height - Row, placed.Y);
    }

    [Fact]
    public void Clamp_NeverLetsTheDialogRiseAboveTheHost()
    {
        // Above the top there is no recovery at all - the title bar would be the part
        // that left, so this edge is hard.
        Point placed = Clamp(new Point(100, -400));

        Assert.Equal(0, placed.Y);
    }

    [Fact]
    public void Clamp_AllowsTheDialogToHangOffTheLeftEdge()
    {
        // Deliberately asymmetric with the vertical rule: a dialog may be shoved almost
        // entirely off-screen sideways to uncover the text behind it, so long as a
        // grabbable sliver remains.
        Point placed = Clamp(new Point(-5000, 100));

        Assert.Equal(32 - Dialog.Width, placed.X);
        Assert.True(placed.X < 0);
    }

    [Fact]
    public void Clamp_AllowsTheDialogToHangOffTheRightEdge()
    {
        Point placed = Clamp(new Point(5000, 100));

        Assert.Equal(Host.Width - 32, placed.X);
    }

    [Fact]
    public void Clamp_KeepsANarrowDialogPlaceable()
    {
        // A dialog narrower than the required visible width would invert the allowed
        // range; the visible width is clamped to the dialog's own width instead.
        Size narrow = new(16, 160);

        Point placed = DialogGeometry.ClampPosition(
            new Point(5000, 0), narrow, Host, Row, minVisibleWidth: 32);

        Assert.Equal(Host.Width - narrow.Width, placed.X);
    }

    [Fact]
    public void Clamp_SurvivesAHostSmallerThanTheGrabStrip()
    {
        // Degenerate, but it happens while a window is being restored or is mid-resize.
        // Here the allowed range inverts (the dialog cannot satisfy both edges at once),
        // and the lower bound must win so the dialog stays anchored rather than flying
        // off to some arbitrary coordinate.
        Size tinyHost = new(10, 10);
        Size narrowDialog = new(16, 16);

        Point placed = DialogGeometry.ClampPosition(
            new Point(5000, 5000), narrowDialog, tinyHost, Row, minVisibleWidth: 32);

        Assert.Equal(0, placed.X);
        Assert.Equal(0, placed.Y);
    }

    [Theory]
    [InlineData(-1, 0, 92, 100)]
    [InlineData(1, 0, 108, 100)]
    [InlineData(0, -1, 100, 84)]
    [InlineData(0, 1, 100, 116)]
    public void MoveByCells_StepsOneWholeCell(int cellsX, int rowsY, double x, double y)
    {
        Point moved = DialogGeometry.MoveByCells(new Point(100, 100), cellsX, rowsY, Cell, Row);

        Assert.Equal(new Point(x, y), moved);
    }

    [Fact]
    public void SnapToCell_RealignsAMouseDraggedPosition()
    {
        // The mouse leaves a dialog on an arbitrary pixel; the keyboard puts it back on
        // the character grid, which is what keeps the retro layout believable.
        Point snapped = DialogGeometry.SnapToCell(new Point(101, 105), Cell, Row);

        Assert.Equal(new Point(104, 112), snapped);
    }

    [Fact]
    public void SnapToCell_RoundsHalfAwayFromZero()
    {
        // Math.Round defaults to banker's rounding, which would send an exact half-cell
        // backwards (100 -> 96) and make the first arrow press after entering move mode
        // look like it went the wrong way.
        Point snapped = DialogGeometry.SnapToCell(new Point(100, 104), Cell, Row);

        Assert.Equal(104, snapped.X);
        Assert.Equal(112, snapped.Y);
    }

    [Fact]
    public void SnapToCell_IgnoresAZeroSizedCell()
    {
        // Guards against a divide-by-zero if the metrics are unresolved.
        Point snapped = DialogGeometry.SnapToCell(new Point(101, 105), 0, 0);

        Assert.Equal(new Point(101, 105), snapped);
    }

    private static Point Clamp(Point desired) =>
        DialogGeometry.ClampPosition(desired, Dialog, Host, Row, minVisibleWidth: 32);
}
