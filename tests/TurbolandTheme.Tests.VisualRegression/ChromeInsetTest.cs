using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Xunit;
using Theme = TurbolandTheme.Wpf.TurbolandTheme;
using ThemeMode = TurbolandTheme.Core.ThemeMode;

namespace TurbolandTheme.Tests.VisualRegression;

/// <summary>
/// Pins <c>Turboland.Size.ChromeInset</c>, the room a clipping container has to reserve
/// for chrome that Turboland controls paint outside their own layout box.
/// </summary>
/// <remarks>
/// <para>
/// A focus ring drawn outside a control's own bounds (a symmetric template with
/// <c>Margin="-1"</c> and <c>BorderThickness="1"</c>) has an overhanging left column.
/// When such a control sits flush against the left edge of a clipping container - a
/// button flush-left inside a ScrollViewer - that column falls outside the viewport and
/// is clipped away, leaving an open-sided highlight with its left edge absent.
/// </para>
/// <para>
/// The defect is invisible to tests that render detached elements with no clipping
/// ancestor, and invisible by inspection because nothing in the button template is
/// wrong. It only appears when an outside-the-box ring meets a clipper, so the fix and
/// these tests are about the CONTAINER, not the control.
/// </para>
/// <para>
/// ListBoxItem is the one template that draws its ring *inside*, precisely so a first or
/// last row is not clipped by the list's own scroll viewer; that local fix does not cover
/// controls a consumer places.
/// </para>
/// </remarks>
[Collection("Wpf")]
public class ChromeInsetTest
{
    private readonly Application _app;

    public ChromeInsetTest(WpfApplicationFixture fixture) => _app = fixture.App;

    /// <summary>
    /// The reservation must cover the ring overhang in every mode and at every scale.
    /// </summary>
    /// <remarks>
    /// This invariant keeps the two tokens honest. Accessible mode thickens the ring to
    /// 2px; if ChromeInset stayed at 1 the mode that exists to make focus obvious would be
    /// the one whose focus indicator got shaved off.
    /// </remarks>
    [Theory]
    [InlineData(ThemeMode.Authentic, 1)]
    [InlineData(ThemeMode.Authentic, 2)]
    [InlineData(ThemeMode.Accessible, 1)]
    [InlineData(ThemeMode.Accessible, 2)]
    public void ChromeInset_CoversTheFocusRingOverhang(ThemeMode mode, int scale)
    {
        StaThread.Run(() =>
        {
            Theme.Apply(_app, mode, scale);

            var inset = (Thickness)_app.Resources["Turboland.Size.ChromeInset"];
            var ring = (Thickness)_app.Resources["Turboland.Size.FocusRingOffset"];
            var shadow = (Thickness)_app.Resources["Turboland.Size.ButtonShadowOffset"];

            // FocusRingOffset is negative: it pushes the ring outwards.
            double overhang = Math.Abs(ring.Left);

            Assert.True(inset.Left >= overhang,
                $"left inset {inset.Left} does not cover a {overhang}px ring overhang");
            Assert.True(inset.Top >= Math.Abs(ring.Top),
                $"top inset {inset.Top} does not cover a {Math.Abs(ring.Top)}px ring overhang");

            // Right and bottom also have to clear a button's hard shadow, which is
            // the larger of the two overhangs on those sides.
            Assert.True(inset.Right >= Math.Max(overhang, shadow.Left),
                $"right inset {inset.Right} does not clear the {shadow.Left}px button shadow");
            Assert.True(inset.Bottom >= Math.Max(Math.Abs(ring.Bottom), shadow.Top),
                $"bottom inset {inset.Bottom} does not clear the {shadow.Top}px button shadow");
        });
    }

    /// <summary>
    /// The real thing, in pixels: a button flush against a ScrollViewer renders a
    /// closed ring when the content carries ChromeInset, and a broken one without it.
    /// </summary>
    /// <remarks>
    /// Both halves matter. The "without" half is a negative control - it proves this
    /// test can actually see the defect, rather than passing because the ring happens
    /// to be drawn somewhere convenient.
    /// </remarks>
    [Theory]
    [InlineData(ThemeMode.Authentic)]
    [InlineData(ThemeMode.Accessible)]
    public void FlushButtonInScrollViewer_KeepsItsRing_OnlyWithChromeInset(ThemeMode mode)
    {
        StaThread.Run(() =>
        {
            Theme.Apply(_app, mode, 1);
            var inset = (Thickness)_app.Resources["Turboland.Size.ChromeInset"];

            int withInset = LeftRingPixels(inset);
            int withoutInset = LeftRingPixels(new Thickness(0));

            Assert.True(withInset > 0,
                "with ChromeInset the ring's left column should render, but no pixel of it did");
            Assert.Equal(0, withoutInset);
        });
    }

    /// <summary>
    /// Renders a flush-left button inside a clipping ScrollViewer and counts how many
    /// pixels of its focus ring's left column survive.
    /// </summary>
    /// <remarks>
    /// The ring is forced on by disabling the button rather than by focusing it. The
    /// disabled trigger drives the same 'focus' Border with the same margin and the same
    /// thickness, so the geometry under test is identical - and it needs neither an
    /// activated window nor an unlocked desktop, so it is deterministic on CI.
    /// </remarks>
    private static int LeftRingPixels(Thickness contentMargin)
    {
        var button = new Button { Content = "OK", IsEnabled = false };
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Margin = contentMargin };
        panel.Children.Add(button);

        var scroller = new ScrollViewer
        {
            Content = panel,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Width = 200,
            Height = 60,
        };

        // A parent window, or the implicit Button style may never be applied. The window
        // is never shown: laying out the ScrollViewer directly is enough, and keeps the
        // test free of any dependency on desktop state.
        var host = new Window { Content = scroller, Width = 240, Height = 100, Left = -4000, Top = -4000 };
        scroller.Measure(new Size(200, 60));
        scroller.Arrange(new Rect(0, 0, 200, 60));
        scroller.UpdateLayout();
        button.ApplyTemplate();
        GC.KeepAlive(host);

        var ring = (Border?)button.Template.FindName("focus", button);
        Assert.NotNull(ring);

        int w = 200, h = 60;
        var bitmap = new RenderTargetBitmap(w, h, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(scroller);

        int stride = w * 4;
        var pixels = new byte[stride * h];
        bitmap.CopyPixels(pixels, stride, 0);

        Rect r = ring.TransformToAncestor(scroller)
                     .TransformBounds(new Rect(0, 0, ring.ActualWidth, ring.ActualHeight));

        var expected = ((SolidColorBrush)ring.BorderBrush).Color;
        int x = (int)Math.Round(r.Left);
        int count = 0;
        for (int y = (int)Math.Round(r.Top); y < (int)Math.Round(r.Bottom); y++)
        {
            if (x < 0 || y < 0 || x >= w || y >= h) continue;
            int i = y * stride + x * 4;
            if (pixels[i + 2] == expected.R && pixels[i + 1] == expected.G && pixels[i] == expected.B)
                count++;
        }
        return count;
    }
}
