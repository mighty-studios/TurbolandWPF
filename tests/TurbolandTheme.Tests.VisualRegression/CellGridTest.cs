using System.Globalization;
using System.Windows;
using System.Windows.Media;
using TurbolandTheme.Core;
using Xunit;
using Theme = TurbolandTheme.Wpf.TurbolandTheme;
using ThemeMode = TurbolandTheme.Core.ThemeMode;

namespace TurbolandTheme.Tests.VisualRegression;

/// <summary>
/// Pins the cell-grid invariant: the base character cell is <b>9 x 16</b>, and the
/// horizontal token must equal the font's real advance.
///
/// If the token and the font's advance disagree - say the token says 8 while the bundled
/// Px437 face advances 9 - glyphs and layout end up on different grids. Nothing else
/// compares them, so the disagreement leaks out as a dialog shadow a pixel or two off
/// and forces GlyphFill to measure the advance itself. A drift of one pixel per character
/// is invisible in any single control and unmistakable across a row of eighty, which is
/// exactly the kind of error this test holds down.
/// </summary>
[Collection("Wpf")]
public class CellGridTest
{
    private readonly Application _app;

    public CellGridTest(WpfApplicationFixture fixture) => _app = fixture.App;

    /// <summary>
    /// Measures what the font actually does, rather than trusting a constant. Takes the
    /// family straight from the applied theme so this exercises the resource real
    /// controls bind to - reconstructing the pack URI here would silently fall back to
    /// a default face and measure the wrong font.
    /// </summary>
    private double MeasuredAdvance(double emSize)
    {
        var family = (FontFamily)_app.Resources["Turboland.Font.Primary"];

        FormattedText text = new(
            "0", CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
            new Typeface(family, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal),
            emSize, Brushes.Black, 1.0);

        return text.WidthIncludingTrailingWhitespace;
    }

    [Theory]
    [InlineData(1, 9.0, 16.0)]
    [InlineData(2, 18.0, 32.0)]
    public void CellTokens_MatchTheNineBySixteenGrid(int scale, double width, double height)
    {
        StaThread.Run(() =>
        {
            Theme.Apply(_app, ThemeMode.Authentic, scale);

            Assert.Equal(width, (double)_app.Resources["Turboland.Metric.CellWidth"]);
            Assert.Equal(height, (double)_app.Resources["Turboland.Metric.CellHeight"]);
        });
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void CellWidthToken_EqualsTheFontsMeasuredAdvance(int scale)
    {
        StaThread.Run(() =>
        {
            Theme.Apply(_app, ThemeMode.Authentic, scale);

            double token = (double)_app.Resources["Turboland.Metric.CellWidth"];
            double fontSize = (double)_app.Resources["Turboland.Font.Size.Primary"];
            double advance = MeasuredAdvance(fontSize);

            // If these ever diverge, glyphs and layout are on different grids and every
            // tiled surface grows a seam. Rendering is the only place it shows up, so
            // catch it here instead.
            Assert.Equal(advance, token, 3);
        });
    }

    [Fact]
    public void CoreCellMetrics_MirrorTheTokens()
    {
        CellMetrics metrics = CellMetrics.ForScale(1);

        Assert.Equal(9, CellMetrics.BaseCellWidth);
        Assert.Equal(9.0, metrics.CellWidth);
        Assert.Equal(16.0, metrics.CellHeight);

        // Two cells right, one row down - genuinely different distances because the
        // cell is not square.
        Assert.Equal(18.0, metrics.ShadowOffsetX);
        Assert.Equal(16.0, metrics.ShadowOffsetY);
    }

    [Fact]
    public void ScrollBarTokens_DistinguishTheTwoAxes()
    {
        StaThread.Run(() =>
        {
            Theme.Apply(_app, ThemeMode.Authentic, 1);

            // A vertical bar is one cell wide; a horizontal bar is one row tall. A single
            // "thickness" token cannot express both once the cell stops being square,
            // which would leave one of the two axes wrong.
            Assert.Equal(9.0, (double)_app.Resources["Turboland.Metric.ScrollBarWidth"]);
            Assert.Equal(16.0, (double)_app.Resources["Turboland.Metric.ScrollBarHeight"]);

            Assert.Equal(16.0, ((GridLength)_app.Resources["Turboland.Grid.ScrollBarButtonHeight"]).Value);
            Assert.Equal(9.0, ((GridLength)_app.Resources["Turboland.Grid.ScrollBarButtonWidth"]).Value);
        });
    }
}
