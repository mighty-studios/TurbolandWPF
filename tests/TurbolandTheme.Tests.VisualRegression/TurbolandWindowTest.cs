using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shell;
using TurbolandTheme.Wpf.Controls;
using Xunit;
using Theme = TurbolandTheme.Wpf.TurbolandTheme;
using ThemeMode = TurbolandTheme.Core.ThemeMode;

namespace TurbolandTheme.Tests.VisualRegression;

/// <summary>
/// Guards the wiring between <see cref="TurbolandWindow"/> and its template. The window's
/// native behavior depends on template state (the zoom box must be findable for
/// WM_NCHITTEST to claim it) and on WindowChrome being attached with scaled values, and
/// both break silently: the window still opens, it just stops resizing, stops snapping,
/// or loses its caption.
/// </summary>
[Collection("Wpf")]
public class TurbolandWindowTest
{
    private readonly Application _app;

    public TurbolandWindowTest(WpfApplicationFixture fixture) => _app = fixture.App;

    [Fact]
    public void Window_ResolvesItsThemeStyle()
    {
        StaThread.Run(() =>
        {
            Theme.Apply(_app, ThemeMode.Authentic, 1);
            var window = new TurbolandWindow();

            Assert.NotNull(window.Style);
            Assert.NotNull(window.Template);
        });
    }

    [Fact]
    public void Window_RemovesTheNativeTitleBarButKeepsAnOpaqueSurface()
    {
        StaThread.Run(() =>
        {
            Theme.Apply(_app, ThemeMode.Authentic, 1);
            var window = new TurbolandWindow();

            // AllowsTransparency is mutually exclusive with WindowChrome's non-client
            // area: turning it on throws at window creation.
            Assert.Equal(WindowStyle.None, window.WindowStyle);
            Assert.False(window.AllowsTransparency);
        });
    }

    [Fact]
    public void Window_AttachesWindowChromeWithScaledCaptionHeight()
    {
        StaThread.Run(() =>
        {
            Theme.Apply(_app, ThemeMode.Authentic, 1);
            var window = new TurbolandWindow();

            WindowChrome? chrome = WindowChrome.GetWindowChrome(window);
            Assert.NotNull(chrome);

            // WindowChrome is what puts native resize, snap, maximise and taskbar
            // behavior back after WindowStyle=None removes the title bar.
            Assert.Equal(0.0, chrome!.GlassFrameThickness.Left);
            Assert.False(chrome.UseAeroCaptionButtons);

            // One cell row: the title is drawn ON the top frame line.
            Assert.Equal(16.0, chrome.CaptionHeight);
            Assert.Equal(6.0, chrome.ResizeBorderThickness.Left);
        });
    }

    [Fact]
    public void Window_ChromeFollowsTheThemeScale()
    {
        StaThread.Run(() =>
        {
            Theme.Apply(_app, ThemeMode.Authentic, 2);
            var window = new TurbolandWindow();

            WindowChrome chrome = WindowChrome.GetWindowChrome(window)!;

            // A caption that stays at 1x while the frame glyphs double leaves the
            // drag region floating in the middle of the title row.
            Assert.Equal(32.0, chrome.CaptionHeight);
            Assert.Equal(12.0, chrome.ResizeBorderThickness.Left);

            Theme.Apply(_app, ThemeMode.Authentic, 1);
        });
    }

    [Fact]
    public void Window_ExposesTheZoomBoxToHitTesting()
    {
        StaThread.Run(() =>
        {
            Theme.Apply(_app, ThemeMode.Authentic, 1);
            var window = new TurbolandWindow();
            window.ApplyTemplate();

            // WM_NCHITTEST locates this part by name to answer HTMAXBUTTON, which is
            // the only thing that makes the Snap Layouts flyout appear. Rename it in
            // the template and snapping dies with no other symptom.
            object? zoomBox = window.Template.FindName(TurbolandWindow.ZoomBoxPartName, window);
            Assert.NotNull(zoomBox);
            Assert.IsAssignableFrom<FrameworkElement>(zoomBox);
        });
    }

    [Fact]
    public void GlyphFill_TilesTheGlyphAcrossItsWholeWidth()
    {
        StaThread.Run(() =>
        {
            Theme.Apply(_app, ThemeMode.Authentic, 1);

            var fill = new GlyphFill
            {
                Glyph = "\u2550",                       // the double horizontal rule
                Foreground = Brushes.White,
                FontFamily = (FontFamily)_app.Resources["Turboland.Font.Primary"],
                FontSize = (double)_app.Resources["Turboland.Font.Size.Primary"],
            };

            const int width = 200;
            const int height = 16;
            fill.Measure(new Size(width, height));
            fill.Arrange(new Rect(0, 0, width, height));
            fill.UpdateLayout();

            var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(fill);

            var pixels = new int[width * height];
            bitmap.CopyPixels(pixels, width * 4, 0);

            // A plain TextBlock would draw one glyph and leave the rest of the run
            // blank; the point of GlyphFill is that the far end is painted too.
            Assert.True(ColumnIsLit(pixels, width, height, 2), "left end of the rule is blank");
            Assert.True(ColumnIsLit(pixels, width, height, width / 2), "middle of the rule is blank");
            Assert.True(ColumnIsLit(pixels, width, height, width - 3), "right end of the rule is blank");
        });
    }

    [Fact]
    public void GlyphFill_MeasuresToASingleCell()
    {
        StaThread.Run(() =>
        {
            Theme.Apply(_app, ThemeMode.Authentic, 1);

            var fill = new GlyphFill
            {
                Glyph = "\u2551",
                FontFamily = (FontFamily)_app.Resources["Turboland.Font.Primary"],
                FontSize = (double)_app.Resources["Turboland.Font.Size.Primary"],
            };

            fill.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

            // Auto-sized frame columns and rows collapse to exactly one glyph, which
            // is what keeps the frame one cell thick at every scale.
            Assert.Equal(16.0, fill.DesiredSize.Height, precision: 3);
            Assert.True(fill.DesiredSize.Width > 0);
        });
    }

    private static bool ColumnIsLit(int[] pixels, int width, int height, int x)
    {
        for (int y = 0; y < height; y++)
            if ((pixels[(y * width) + x] & 0x00FFFFFF) != 0)
                return true;

        return false;
    }
}
