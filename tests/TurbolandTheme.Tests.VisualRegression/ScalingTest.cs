using System.Windows;
using TurbolandTheme.Core;
using TurbolandTheme.Wpf.Controls;
using Xunit;
using Theme = TurbolandTheme.Wpf.TurbolandTheme;
using ThemeMode = TurbolandTheme.Core.ThemeMode;

namespace TurbolandTheme.Tests.VisualRegression;

/// <summary>
/// Proves that Theme.Apply actually scales the metric/size/font-size tokens at runtime.
/// Uses the shared WpfApplicationFixture because WPF allows only one Application per
/// AppDomain.
/// </summary>
/// <remarks>
/// Every body runs through <see cref="StaThread"/> even though nothing here builds a
/// control and <c>Theme.Apply</c> needs no STA thread. Applying the theme off the shared
/// thread parses the dictionaries with the *worker* thread's affinity and then posts a
/// resource invalidation to the windows other tests have left on the STA thread; the next
/// dispatcher pump re-resolves a foreign-owned Style and takes the whole test host down
/// with "the calling thread cannot access this object".
/// </remarks>
[Collection("Wpf")]
public class ScalingTest
{
    private readonly Application _app;

    public ScalingTest(WpfApplicationFixture fixture)
    {
        _app = fixture.App;
    }

    /// <summary>
    /// A metric overridden by a mode scales from the override, not from the base.
    /// </summary>
    /// <remarks>
    /// A token may be defined in both <c>Tokens.Metrics.xaml</c> and a per-mode override
    /// dictionary such as <c>Tokens.Accessible.Overrides.xaml</c>. BuildScaleOverrides
    /// must resolve each key through the aggregated dictionary and assign it, rather than
    /// reading and adding each merged child's own value, which would raise "Item has
    /// already been added". Accessible mode defaults to 2x, so this path runs on startup
    /// for that mode. The values below are that override at both scales.
    /// </remarks>
    [Fact]
    public void Apply_Accessible_ScalesModeOverriddenMetrics()
    {
        StaThread.Run(() =>
        {
            Theme.Apply(_app, ThemeMode.Accessible, 1);
            Assert.Equal(2.0, (double)_app.Resources["Turboland.Metric.FocusBorder"]);
            Assert.Equal(new Thickness(2), (Thickness)_app.Resources["Turboland.Size.FocusBorder"]);
            Assert.Equal(new Thickness(-2), (Thickness)_app.Resources["Turboland.Size.FocusRingOffset"]);

            Theme.Apply(_app, ThemeMode.Accessible, 2);
            Assert.Equal(4.0, (double)_app.Resources["Turboland.Metric.FocusBorder"]);
            Assert.Equal(new Thickness(4), (Thickness)_app.Resources["Turboland.Size.FocusBorder"]);
            Assert.Equal(new Thickness(-4), (Thickness)_app.Resources["Turboland.Size.FocusRingOffset"]);

            // Authentic is untouched by the accessible override.
            Theme.Apply(_app, ThemeMode.Authentic, 2);
            Assert.Equal(2.0, (double)_app.Resources["Turboland.Metric.FocusBorder"]);
        });
    }

    /// <summary>
    /// CellSize.Rows/Columns resolve against the live token, so an element sized in
    /// cells follows a scale change instead of freezing at load-time pixels.
    /// </summary>
    /// <remarks>
    /// The point of the property is that XAML has no arithmetic on resource references,
    /// so a consumer sizing a list box on the character grid otherwise has to write the
    /// pixel product by hand - which is wrong at every scale but 1x.
    /// </remarks>
    [Fact]
    public void CellSize_SizesInCells_AndFollowsScale()
    {
        StaThread.Run(() =>
        {
            Theme.Apply(_app, ThemeMode.Authentic, 1);

            // Parented in a Window on purpose. A DynamicResource on a *disconnected*
            // element still resolves once (Application.Resources is the fallback scope)
            // but never hears about a later change, because WPF pushes resource
            // invalidation down the tree from the app's own windows - an orphaned element
            // quietly behaves differently.
            var list = new System.Windows.Controls.ListBox();
            var host = new Window { Content = list };
            CellSize.SetRows(list, 8);
            CellSize.SetColumns(list, 20);

            // The cell is not square: 8 rows is 128, 20 columns is 180.
            Assert.Equal(128.0, list.Height);
            Assert.Equal(180.0, list.Width);

            Theme.Apply(_app, ThemeMode.Authentic, 2);
            Assert.Equal(256.0, list.Height);
            Assert.Equal(360.0, list.Width);

            // Clearing returns the element to auto-sizing rather than leaving it
            // pinned at the last computed pixel value.
            list.ClearValue(CellSize.RowsProperty);
            Assert.True(double.IsNaN(list.Height));

            host.Close();
            Theme.Apply(_app, ThemeMode.Authentic, 1);
        });
    }

    [Fact]
    public void Apply_Authentic_2x_ScalesMetrics()
    {
        StaThread.Run(() =>
        {
            int scale = Theme.Apply(_app, ThemeMode.Authentic, 2);

            Assert.Equal(2, scale);
            // The cell is 9x16, so the two axes do not scale to the same number.
            Assert.Equal(18.0, (double)_app.Resources["Turboland.Metric.CellWidth"]);
            Assert.Equal(32.0, (double)_app.Resources["Turboland.Metric.CellHeight"]);
            Assert.Equal(32.0, (double)_app.Resources["Turboland.Metric.MenuHeight"]);
        });
    }

    [Fact]
    public void Apply_Authentic_2x_ScalesGridLengths()
    {
        StaThread.Run(() =>
        {
            Theme.Apply(_app, ThemeMode.Authentic, 2);

            // GridLength needs its own branch in BuildScaleOverrides: RowDefinition.Height
            // and ColumnDefinition.Width will not accept a Double resource, so fixed grid
            // tracks (scroll-bar buttons, the menu icon column, the tree indent) would
            // otherwise be stuck at 1x.
            // The scroll-bar arrow button is one cell along the bar's own axis, so the
            // vertical bar's button is a 16px row and the horizontal bar's is a 9px column.
            var button = (GridLength)_app.Resources["Turboland.Grid.ScrollBarButtonHeight"];
            Assert.True(button.IsAbsolute);
            Assert.Equal(32.0, button.Value);
            Assert.Equal(18.0, ((GridLength)_app.Resources["Turboland.Grid.ScrollBarButtonWidth"]).Value);

            Assert.Equal(36.0, ((GridLength)_app.Resources["Turboland.Grid.MenuIconColumn"]).Value);
            Assert.Equal(36.0, ((GridLength)_app.Resources["Turboland.Grid.TreeIndent"]).Value);
        });
    }

    [Fact]
    public void Apply_Authentic_1x_LeavesGridLengthsUnscaled()
    {
        StaThread.Run(() =>
        {
            Theme.Apply(_app, ThemeMode.Authentic, 1);

            Assert.Equal(16.0, ((GridLength)_app.Resources["Turboland.Grid.ScrollBarButtonHeight"]).Value);
            Assert.Equal(9.0, ((GridLength)_app.Resources["Turboland.Grid.ScrollBarButtonWidth"]).Value);
        });
    }

    [Fact]
    public void Apply_Authentic_1x_UsesNativeFontCell()
    {
        StaThread.Run(() =>
        {
            Theme.Apply(_app, ThemeMode.Authentic, 1);

            // Size 16 is the only value that yields the font's native 9x16 pixel
            // cell (1.0 em line height, 0.5625 em advance). Anything else puts
            // glyphs on fractional pixels.
            Assert.Equal(16.0, (double)_app.Resources["Turboland.Font.Size.Primary"]);
        });
    }

    [Fact]
    public void Apply_Authentic_2x_ScalesThickness()
    {
        StaThread.Run(() =>
        {
            Theme.Apply(_app, ThemeMode.Authentic, 2);

            var border = (Thickness)_app.Resources["Turboland.Size.Border"];
            Assert.Equal(2.0, border.Left);
            Assert.Equal(2.0, border.Top);
            Assert.Equal(2.0, border.Right);
            Assert.Equal(2.0, border.Bottom);
        });
    }

    [Fact]
    public void Apply_Authentic_1x_LeavesMetricsUnscaled()
    {
        StaThread.Run(() =>
        {
            int scale = Theme.Apply(_app, ThemeMode.Authentic, 1);

            Assert.Equal(1, scale);
            Assert.Equal(9.0, (double)_app.Resources["Turboland.Metric.CellWidth"]);
            Assert.Equal(16.0, (double)_app.Resources["Turboland.Metric.CellHeight"]);
        });
    }

    [Fact]
    public void Apply_Accessible_ForcesMinimum2x()
    {
        StaThread.Run(() =>
        {
            // No explicit scale: Accessible mode must clamp to at least 2x even
            // though the system DPI would resolve to 1x.
            int scale = Theme.Apply(_app, ThemeMode.Accessible);

            Assert.True(scale >= 2, $"expected scale >= 2, got {scale}");
            Assert.True((double)_app.Resources["Turboland.Metric.CellWidth"] >= 16.0);
        });
    }
}

