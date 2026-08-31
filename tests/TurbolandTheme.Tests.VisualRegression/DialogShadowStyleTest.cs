using System.Windows;
using System.Windows.Controls;
using Xunit;
using Theme = TurbolandTheme.Wpf.TurbolandTheme;
using ThemeMode = TurbolandTheme.Core.ThemeMode;

namespace TurbolandTheme.Tests.VisualRegression;

/// <summary>
/// Pins the shadow geometry on <c>Turboland.Style.DialogShadow</c>. Owning the offset in the
/// style, rather than leaving each caller to hand-write a margin, means a caller cannot
/// disagree with the token: a caller that drops the margin gets no shadow instead of a
/// subtly wrong one, and the offset stays in step with ShadowOffsetX/Y.
/// </summary>
[Collection("Wpf")]
public class DialogShadowStyleTest
{
    private readonly Application _app;

    public DialogShadowStyleTest(WpfApplicationFixture fixture) => _app = fixture.App;

    private Thickness ResolveShadowMargin(int scale)
    {
        Thickness margin = default;

        StaThread.Run(() =>
        {
            Theme.Apply(_app, ThemeMode.Authentic, scale);

            // The setter is a DynamicResource, so it stays unresolved until the style is
            // applied to a live element that can see the application's resources.
            Border shadow = new() { Style = (Style)_app.Resources["Turboland.Style.DialogShadow"] };
            shadow.Measure(new Size(200, 100));
            margin = shadow.Margin;
        });

        return margin;
    }

    [Fact]
    public void DialogShadow_CarriesTheMeasuredOffset()
    {
        // Two cells right (18) and one row down (16) - different distances, because
        // the character cell is 9x16. The negative half is what lets the shadow paint
        // outside its Grid cell without claiming any layout space.
        Assert.Equal(new Thickness(18, 16, -18, -16), ResolveShadowMargin(1));
    }

    [Fact]
    public void DialogShadow_OffsetScales()
    {
        Assert.Equal(new Thickness(36, 32, -36, -32), ResolveShadowMargin(2));
    }
}
