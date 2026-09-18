using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TurbolandTheme.Wpf.Controls;
using Xunit;
using Theme = TurbolandTheme.Wpf.TurbolandTheme;
using ThemeMode = TurbolandTheme.Core.ThemeMode;

namespace TurbolandTheme.Tests.VisualRegression;

/// <summary>
/// The scroll bar track is a two-color checkerboard dither, the Borland Vision
/// model: one pair of tokens paints the whole bar, the track as a stipple of
/// the pair and the arrow buttons and thumb as solid base. The pair must be
/// overridable by the application, which is why the tile is composed by
/// <see cref="StippleFill"/> in the element's context rather than declared as
/// a shared Freezable resource.
/// </summary>
[Collection("Wpf")]
public class ScrollBarStippleTest
{
    private readonly Application _app;

    public ScrollBarStippleTest(WpfApplicationFixture fixture) => _app = fixture.App;

    private static Color Base => ((SolidColorBrush)Resolve("Turboland.Brush.ScrollTrackBase")).Color;
    private static Color Stipple => ((SolidColorBrush)Resolve("Turboland.Brush.ScrollTrackStipple")).Color;

    private static object Resolve(string key) =>
        Application.Current.Resources[key]
        ?? throw new InvalidOperationException($"Missing token {key}");

    [Fact]
    public void Track_BackgroundIsCheckerboardTile()
    {
        StaThread.Run(() =>
        {
            Theme.Apply(_app, ThemeMode.Authentic, 1);

            var bar = NewBar();
            using var host = ShowHosted(bar);

            var track = (Grid)VisualTreeHelper.GetChild(bar, 0);
            var tile = track.Background as TileBrush;

            Assert.NotNull(tile);
            Assert.Equal(TileMode.Tile, tile!.TileMode);
            Assert.Equal(BrushMappingMode.Absolute, tile.ViewportUnits);
            Assert.Equal(new Rect(0, 0, StippleFill.TileSize, StippleFill.TileSize), tile.Viewport);
        });
    }

    [Fact]
    public void Track_PixelsAlternateBaseAndStipple()
    {
        StaThread.Run(() =>
        {
            Theme.Apply(_app, ThemeMode.Authentic, 1);

            var bar = NewBar();
            using var host = ShowHosted(bar);
            var (width, height, pixels) = Render(bar);

            // Layout at 1x: 9 px arrow, track, 9 px arrow. Value 0 with a
            // quarter viewport parks the thumb at the track's left end, so the
            // sampled span is bare trough.
            int y = height / 2;
            int x0 = 30;
            int x1 = width - 12;

            Color baseColor = Base;
            Color stippleColor = Stipple;

            for (int x = x0; x <= x1; x++)
            {
                Color c = Pixel(pixels, width, x, y);
                bool isDot = (x + y) % 2 == 0;
                Color expected = isDot ? stippleColor : baseColor;
                Assert.True(Near(c, expected),
                    $"({x},{y}) = {c}, expected {(isDot ? "stipple" : "base")} {expected}");
            }
        });
    }

    [Fact]
    public void AppOverride_RecolorsTrackArrowsAndThumb()
    {
        StaThread.Run(() =>
        {
            Theme.Apply(_app, ThemeMode.Authentic, 1);

            var blue = new SolidColorBrush(Color.FromRgb(0x00, 0x00, 0xAA));
            var cyan = new SolidColorBrush(Color.FromRgb(0x00, 0xAA, 0xAA));
            _app.Resources["Turboland.Brush.ScrollTrackBase"] = blue;
            _app.Resources["Turboland.Brush.ScrollTrackStipple"] = cyan;
            try
            {
                var bar = NewBar();
                using var host = ShowHosted(bar);
                var (width, height, pixels) = Render(bar);

                int y = height / 2;

                // Track: the dither now alternates the override pair.
                for (int x = 30; x <= width - 12; x++)
                {
                    Color expected = (x + y) % 2 == 0 ? cyan.Color : blue.Color;
                    Color c = Pixel(pixels, width, x, y);
                    Assert.True(Near(c, expected),
                        $"({x},{y}) = {c}, expected {expected}");
                }

                // Arrow button: solid base face, no dots. The left button is
                // the first 9 px; sample its top edge, clear of the glyph.
                Assert.True(Near(Pixel(pixels, width, 4, 1), blue.Color));
            }
            finally
            {
                _app.Resources.Remove("Turboland.Brush.ScrollTrackBase");
                _app.Resources.Remove("Turboland.Brush.ScrollTrackStipple");
            }
        });
    }

    /// <summary>A horizontal bar with a quarter-length thumb and bare trough to sample.</summary>
    private static ScrollBar NewBar() => new()
    {
        Orientation = Orientation.Horizontal,
        Width = 90,
        Height = 16,
        Minimum = 0,
        Maximum = 100,
        Value = 0,
        ViewportSize = 25,
    };

    private static HostedWindow ShowHosted(UIElement content)
    {
        var window = new Window
        {
            Content = content,
            Width = 300,
            Height = 200,
            Left = -4000,
            Top = -4000,
            ShowInTaskbar = false,
            WindowStartupLocation = WindowStartupLocation.Manual,
        };
        window.Show();
        window.UpdateLayout();
        return new HostedWindow(window);
    }

    private sealed class HostedWindow : IDisposable
    {
        public HostedWindow(Window window) => Window = window;

        public Window Window { get; }

        public void Dispose() => Window.Close();
    }

    /// <summary>Measures, arranges and rasterises at 96 DPI: one DIP is one pixel.</summary>
    private static (int Width, int Height, byte[] Pixels) Render(FrameworkElement root)
    {
        root.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        var size = new Size(
            Math.Max(1, Math.Ceiling(root.DesiredSize.Width)),
            Math.Max(1, Math.Ceiling(root.DesiredSize.Height)));
        root.Arrange(new Rect(size));
        root.UpdateLayout();

        var bitmap = new RenderTargetBitmap((int)size.Width, (int)size.Height, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(root);

        var converted = new FormatConvertedBitmap(bitmap, PixelFormats.Bgra32, null, 0);
        int stride = converted.PixelWidth * 4;
        var pixels = new byte[stride * converted.PixelHeight];
        converted.CopyPixels(pixels, stride, 0);
        return (converted.PixelWidth, converted.PixelHeight, pixels);
    }

    private static Color Pixel(byte[] pixels, int width, int x, int y)
    {
        int i = (y * width + x) * 4;
        return Color.FromRgb(pixels[i + 2], pixels[i + 1], pixels[i]);
    }

    /// <summary>
    /// Tolerant equality: rasterizing a geometry brush can leave a low bit of
    /// slack in a channel even on integer coordinates.
    /// </summary>
    private static bool Near(Color a, Color b) =>
        Math.Abs(a.R - b.R) <= 2 && Math.Abs(a.G - b.G) <= 2 && Math.Abs(a.B - b.B) <= 2;
}
