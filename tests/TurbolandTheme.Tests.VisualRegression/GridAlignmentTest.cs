using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Xunit;
using Theme = TurbolandTheme.Wpf.TurbolandTheme;
using ThemeMode = TurbolandTheme.Core.ThemeMode;

namespace TurbolandTheme.Tests.VisualRegression;

/// <summary>
/// The character grid, asserted directly.
///
/// <para>
/// A Turboland screen is 80x25 cells and every control lands on one: a cell is exactly
/// 9x16, and a push button is eight cells across and one row down (72x16 px), no
/// exceptions.
/// </para>
///
/// <para>
/// Sizing a control by adding padding to a text line and accepting whatever comes out
/// puts it off the grid - buttons 26 px tall (1.62 rows), fields 24 px (1.5 rows).
/// Stacked, those errors accumulate and the grid drifts, slowly enough that no single
/// render looks wrong.
/// </para>
///
/// <para>
/// The pixel baselines would catch a <i>change</i> to any of this, but only as "these
/// pixels moved". This test states the rule the baselines are evidence for, so a
/// regression arrives named rather than as a picture to interpret.
/// </para>
/// </summary>
[Collection("Wpf")]
public class GridAlignmentTest
{
    private readonly Application _app;

    public GridAlignmentTest(WpfApplicationFixture fixture) => _app = fixture.App;

    public static IEnumerable<object[]> Cases() =>
        from mode in new[] { ThemeMode.Authentic, ThemeMode.Accessible }
        from scale in new[] { 1, 2 }
        select new object[] { mode, scale };

    /// <summary>
    /// Every single-line control occupies exactly one cell row.
    /// </summary>
    /// <remarks>
    /// Checked at both scales because the failure this guards against is a raw pixel
    /// literal, and a literal is exactly the thing that is still 3 px when everything
    /// around it has become 6.
    /// </remarks>
    [Theory]
    [MemberData(nameof(Cases))]
    public void SingleLineControls_AreExactlyOneRowTall(ThemeMode mode, int scale)
    {
        StaThread.Run(() =>
        {
            Theme.Apply(_app, mode, scale);
            double row = (double)_app.Resources["Turboland.Metric.CellHeight"];

            List<string> offenders = [];
            foreach ((string name, FrameworkElement control) in SingleLineControls())
            {
                double height = Measure(control).Height;
                if (height != row)
                    offenders.Add($"{name}: {height} px = {height / row:0.###} rows");
            }

            Assert.True(
                offenders.Count == 0,
                $"one row is {row} px at {mode} {scale}x, but: {string.Join("; ", offenders)}");
        });
    }

    /// <summary>
    /// A button is a whole number of cells wide, whatever its label.
    /// </summary>
    /// <remarks>
    /// This holds for free once the horizontal padding is one cell: the width becomes
    /// (characters + 2) x CellWidth for any label, because the font is monospaced. The
    /// padding must be one cell (9), not 12 - a 12px padding puts every button with a
    /// label longer than six characters a third of a cell off the grid, and no amount of
    /// care in the surrounding layout can put it back.
    /// </remarks>
    [Theory]
    [MemberData(nameof(Cases))]
    public void Buttons_AreAWholeNumberOfCellsWide(ThemeMode mode, int scale)
    {
        StaThread.Run(() =>
        {
            Theme.Apply(_app, mode, scale);
            double cell = (double)_app.Resources["Turboland.Metric.CellWidth"];

            List<string> offenders = [];
            foreach (string label in new[] { "OK", "Cancel", "Delete", "Compile to memory" })
            {
                double width = Measure(new Button { Content = label }).Width;
                if (width % cell != 0)
                    offenders.Add($"\"{label}\": {width} px = {width / cell:0.###} cells");
            }

            Assert.True(
                offenders.Count == 0,
                $"one cell is {cell} px at {mode} {scale}x, but: {string.Join("; ", offenders)}");
        });
    }

    /// <summary>
    /// Short push buttons, at most six characters, where <c>ButtonMinWidth</c> governs:
    /// every one is eight cells by one row (72x16 px).
    /// </summary>
    [Fact]
    public void ShortButtons_MatchTheSampleAtEightCellsByOneRow()
    {
        StaThread.Run(() =>
        {
            Theme.Apply(_app, ThemeMode.Authentic, 1);

            foreach (string label in new[] { "OK", "Cancel", "Delete", "Help" })
            {
                Size size = Measure(new Button { Content = label });
                Assert.Equal(new Size(72, 16), size);
            }
        });
    }

    /// <summary>
    /// Taking focus must not change a control's size.
    /// </summary>
    /// <remarks>
    /// Swapping a 1px outline for a 2px one on focus would grow the control by two pixels
    /// and nudge everything beside it. Focus chrome is drawn outside the layout box, so
    /// taking focus is size-neutral by construction rather than by keeping two values in
    /// step. Asserted through the template's own measure, since a detached tree cannot
    /// take real keyboard focus.
    /// </remarks>
    [Theory]
    [MemberData(nameof(Cases))]
    public void FocusChrome_DoesNotAffectMeasuredSize(ThemeMode mode, int scale)
    {
        StaThread.Run(() =>
        {
            Theme.Apply(_app, mode, scale);
            double row = (double)_app.Resources["Turboland.Metric.CellHeight"];

            // A ring one thickness wide inset by the same amount contributes nothing:
            // the ring token and the offset token have to stay equal and opposite.
            Thickness ring = (Thickness)_app.Resources["Turboland.Size.FocusBorder"];
            Thickness offset = (Thickness)_app.Resources["Turboland.Size.FocusRingOffset"];
            Assert.Equal(ring.Left, -offset.Left);
            Assert.Equal(ring.Top, -offset.Top);

            Assert.Equal(row, Measure(new TextBox { Text = "focused" }).Height);
        });
    }

    /// <summary>
    /// A list row is one cell row, so a list sized in whole rows shows whole rows.
    /// </summary>
    /// <remarks>
    /// A list row must be exactly one cell row. If rows are 20 px (a 2 px vertical padding
    /// on a 16 px line) inside a box sized as a multiple of 16, the last row is always a
    /// fraction and always clipped, so the scroll bar appears to overlap it.
    /// </remarks>
    [Theory]
    [MemberData(nameof(Cases))]
    public void ListRows_AreExactlyOneRowTall(ThemeMode mode, int scale)
    {
        StaThread.Run(() =>
        {
            Theme.Apply(_app, mode, scale);
            double row = (double)_app.Resources["Turboland.Metric.CellHeight"];

            ListBox list = new() { Height = 6 * row };
            foreach (string item in new[] { "CRT.TPU", "DOS.TPU", "GRAPH.TPU" })
                list.Items.Add(item);

            Border host = new() { Child = list };
            host.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            host.Arrange(new Rect(host.DesiredSize));
            host.UpdateLayout();

            ListBoxItem container =
                (ListBoxItem)list.ItemContainerGenerator.ContainerFromIndex(0);

            Assert.Equal(row, container.ActualHeight);
        });
    }

    private static IEnumerable<(string Name, FrameworkElement Control)> SingleLineControls() =>
    [
        ("Button", new Button { Content = "OK" }),
        ("TextBox", new TextBox { Text = "C:\\TP\\BIN" }),
        ("PasswordBox", new PasswordBox()),
        ("CheckBox", new CheckBox { Content = "Range checking" }),
        ("RadioButton", new RadioButton { Content = "80x25" }),
        ("ComboBox", new ComboBox { ItemsSource = new[] { "Turboland Rules!" }, SelectedIndex = 0 }),
    ];

    private static Size Measure(FrameworkElement control)
    {
        // Parent the control before measuring. A code-created element with no parent
        // may never look up its implicit style - TextBox does not, Button does - so
        // measuring an orphan silently measures an unthemed control and reports 0.
        Border host = new() { Child = control };
        host.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        return control.DesiredSize;
    }
}
