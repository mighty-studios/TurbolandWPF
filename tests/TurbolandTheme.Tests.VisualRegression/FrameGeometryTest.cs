using TurbolandTheme.Wpf.Interop;
using Xunit;

namespace TurbolandTheme.Tests.VisualRegression;

/// <summary>
/// Pins the two pieces of frame arithmetic WPF gets wrong on its own. Both failures are
/// silent and easy to reintroduce, and neither is visible in a screenshot diff, so they
/// are guarded here rather than by a rendering test.
/// </summary>
public class FrameGeometryTest
{
    // A 1920x1080 monitor with a 40px taskbar along the bottom.
    private static readonly PixelRect Monitor = new(0, 0, 1920, 1080);
    private static readonly PixelRect Work = new(0, 0, 1920, 1040);

    [Fact]
    public void MaximisedPlacement_FillsWorkAreaNotMonitor()
    {
        var (x, y, w, h) = FrameGeometry.MaximisedPlacement(Monitor, Work);

        // Without the WM_GETMINMAXINFO hook a maximised window fills the whole monitor
        // and covers the taskbar, rather than filling the work area.
        Assert.Equal(0, x);
        Assert.Equal(0, y);
        Assert.Equal(1920, w);
        Assert.Equal(1040, h);
    }

    [Fact]
    public void MaximisedPlacement_OffsetIsRelativeToTheMonitorNotTheDesktop()
    {
        // A monitor to the right of the primary, with a taskbar on its left edge.
        var monitor = new PixelRect(1920, 0, 3840, 1080);
        var work = new PixelRect(1960, 0, 3840, 1080);

        var (x, y, w, h) = FrameGeometry.MaximisedPlacement(monitor, work);

        // MINMAXINFO.ptMaxPosition is monitor-relative. Returning the desktop
        // coordinate (1960) would push the window a whole monitor off to the right,
        // which is invisible on a single-monitor test machine.
        Assert.Equal(40, x);
        Assert.Equal(0, y);
        Assert.Equal(1880, w);
        Assert.Equal(1080, h);
    }

    [Theory]
    // Corners take precedence over edges.
    [InlineData(2, 2, FrameGeometry.HitTopLeft)]
    [InlineData(1917, 2, FrameGeometry.HitTopRight)]
    [InlineData(2, 1077, FrameGeometry.HitBottomLeft)]
    [InlineData(1917, 1077, FrameGeometry.HitBottomRight)]
    // Edges.
    [InlineData(2, 540, FrameGeometry.HitLeft)]
    [InlineData(1917, 540, FrameGeometry.HitRight)]
    [InlineData(960, 2, FrameGeometry.HitTop)]
    [InlineData(960, 1077, FrameGeometry.HitBottom)]
    // Interior: the caption and client area must be left alone.
    [InlineData(960, 540, FrameGeometry.HitNowhere)]
    [InlineData(960, 10, FrameGeometry.HitNowhere)]
    public void ResizeHitCode_CoversAllEightEdges(int x, int y, int expected)
        => Assert.Equal(expected, FrameGeometry.ResizeHitCode(Monitor, x, y, band: 6));

    [Fact]
    public void ResizeHitCode_ClaimsTheCornerUnderTheCloseBox()
    {
        // The Turboland close box sits in the top-left corner, and
        // WindowChrome.IsHitTestVisibleInChrome outranks WindowChrome's own resize
        // hit-testing. Answering the band first, before the caption controls, is the
        // only thing that keeps that resize handle alive.
        Assert.Equal(FrameGeometry.HitTopLeft, FrameGeometry.ResizeHitCode(Monitor, 0, 0, band: 6));
    }

    [Fact]
    public void ResizeHitCode_IgnoresPointsOutsideTheWindow()
    {
        Assert.Equal(FrameGeometry.HitNowhere, FrameGeometry.ResizeHitCode(Monitor, -1, 540, 6));
        Assert.Equal(FrameGeometry.HitNowhere, FrameGeometry.ResizeHitCode(Monitor, 1920, 540, 6));
    }

    [Fact]
    public void ResizeHitCode_WithNoBandNeverClaimsAnything()
    {
        // A maximised window has no resize band; letting one survive would allow a
        // drag along the screen edge to resize a window that cannot be resized.
        Assert.Equal(FrameGeometry.HitNowhere, FrameGeometry.ResizeHitCode(Monitor, 0, 0, band: 0));
    }

    [Fact]
    public void ResizeHitCode_BandScalesWithTheTheme()
    {
        // At 2x the band doubles, so a point 10px in is an edge rather than client.
        Assert.Equal(FrameGeometry.HitNowhere, FrameGeometry.ResizeHitCode(Monitor, 10, 540, band: 6));
        Assert.Equal(FrameGeometry.HitLeft, FrameGeometry.ResizeHitCode(Monitor, 10, 540, band: 12));
    }

    [Fact]
    public void MaximisedBandCode_NeutralisesTheEdgesButKeepsTheTopDraggable()
    {
        // While maximised, WindowChrome answers the band with 13/12/10/15 (all four
        // resize codes) on a window Windows will not let you resize, so the edges show
        // resize cursors that do nothing. Declining the message is not enough - it has
        // to be claimed and neutralised.
        Assert.Equal(FrameGeometry.HitCaption,
            FrameGeometry.MaximisedBandCode(Monitor, y: 2, captionHeight: 16));

        // Below the caption strip the band is ordinary client area.
        Assert.Equal(FrameGeometry.HitClient,
            FrameGeometry.MaximisedBandCode(Monitor, y: 16, captionHeight: 16));
        Assert.Equal(FrameGeometry.HitClient,
            FrameGeometry.MaximisedBandCode(Monitor, y: 1078, captionHeight: 16));
    }

    [Fact]
    public void MaximisedBandCode_IsRelativeToTheWindowNotTheScreen()
    {
        // On a secondary monitor the window's top is not zero; comparing against a
        // raw y would make the whole edge draggable or none of it.
        var onSecondMonitor = new PixelRect(0, 1080, 1920, 2160);

        Assert.Equal(FrameGeometry.HitCaption,
            FrameGeometry.MaximisedBandCode(onSecondMonitor, y: 1082, captionHeight: 16));
        Assert.Equal(FrameGeometry.HitClient,
            FrameGeometry.MaximisedBandCode(onSecondMonitor, y: 1100, captionHeight: 16));
    }
}
