using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Media;
using Xunit;
using TurbolandTheme.Wpf.Controls;
using Theme = TurbolandTheme.Wpf.TurbolandTheme;
using ThemeMode = TurbolandTheme.Core.ThemeMode;

namespace TurbolandTheme.Tests.VisualRegression;

/// <summary>
/// Pins the status bar's segment layout: <c>[pad] F1 [space] Help [pad] | [pad] ...</c>,
/// laid out on the character cell.
/// </summary>
/// <remarks>
/// The widths are asserted rather than the pixels because the hint occupies whole cells;
/// getting that wrong shifts everything after the first segment, the failure mode a
/// screenshot comparison would report only as "everything moved".
/// </remarks>
[Collection("Wpf")]
public class StatusBarHintTest
{
    private readonly Application _app;

    public StatusBarHintTest(WpfApplicationFixture fixture) => _app = fixture.App;

    /// <summary>
    /// Builds a themed status bar with two hints and a separator, lays it out, and hands the
    /// realised containers back. A <see cref="StatusBar"/> generates its own containers, so
    /// nothing here can be measured before a layout pass.
    /// </summary>
    /// <remarks>
    /// Deliberately measures and arranges directly instead of showing a <see cref="Window"/>.
    /// A shown window registers itself with the shared <see cref="Application"/>, and every
    /// later <c>Theme.Apply</c> then walks that collection to invalidate resource references -
    /// which aborts the run when it reaches a control whose Style is a resource reference.
    /// The containers are generated during measure regardless, so the window bought nothing.
    /// </remarks>
    private void WithBar(int scale, System.Action<StatusBar, double> body)
    {
        StaThread.Run(() =>
        {
            Theme.Apply(_app, ThemeMode.Authentic, scale);
            double cell = (double)_app.Resources["Turboland.Metric.CellWidth"];

            StatusBar bar = new();
            bar.Items.Add(new StatusBarHint { Key = "F1", Label = "Help" });
            bar.Items.Add(new Separator());
            bar.Items.Add(new StatusBarHint { Key = "F2", Label = "Save" });

            // Measured unconstrained so the last item cannot stretch and mask a wrong
            // width; StatusBar's panel is a DockPanel with LastChildFill.
            bar.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            bar.Arrange(new Rect(bar.DesiredSize));
            bar.UpdateLayout();

            body(bar, cell);
        });
    }

    private static FrameworkElement ContainerAt(StatusBar bar, int index) =>
        (FrameworkElement)(bar.ItemContainerGenerator.ContainerFromIndex(index)
                           ?? bar.Items[index]);

    [Fact]
    public void Hint_OccupiesNineCells()
    {
        WithBar(1, (bar, cell) =>
        {
            // 1 pad + "F1" + 1 space + "Help" + 1 pad = 9 cells. The space between key
            // and label belongs to the template, so callers cannot vary it.
            Assert.Equal(9 * cell, ContainerAt(bar, 0).ActualWidth);
        });
    }

    [Fact]
    public void Separator_IsExactlyOneCellAndCarriesNoPadding()
    {
        WithBar(1, (bar, cell) =>
        {
            var separator = ContainerAt(bar, 1);
            Assert.Equal(cell, separator.ActualWidth);

            // The blank cells either side belong to the neighboring items' padding.
            // If the separator grew its own margin the segments would drift apart.
            Assert.Equal(new Thickness(0), separator.Margin);
        });
    }

    [Fact]
    public void SecondHintStartsOnTheCellAfterTheSeparator()
    {
        WithBar(1, (bar, cell) =>
        {
            var second = ContainerAt(bar, 2);
            double x = second.TransformToAncestor(bar).Transform(new Point(0, 0)).X;

            // Hint c0-c8, separator c9, next hint from c10 (its text at c11).
            Assert.Equal(10 * cell, x);
        });
    }

    [Fact]
    public void LayoutScales()
    {
        WithBar(2, (bar, cell) =>
        {
            Assert.Equal(18, cell);
            Assert.Equal(9 * cell, ContainerAt(bar, 0).ActualWidth);
            Assert.Equal(cell, ContainerAt(bar, 1).ActualWidth);
        });
    }

    [Fact]
    public void KeyIsDrawnInTheAcceleratorColorAndTheLabelIsNot()
    {
        WithBar(1, (bar, _) =>
        {
            var text = FindDescendant<TextBlock>(ContainerAt(bar, 0));
            Assert.NotNull(text);

            var runs = text!.Inlines.OfType<Run>().ToList();
            Assert.Equal(3, runs.Count);
            Assert.Equal("F1", runs[0].Text);
            Assert.Equal(" ", runs[1].Text);
            Assert.Equal("Help", runs[2].Text);

            var accelerator = (SolidColorBrush)_app.Resources["Turboland.Brush.StatusBarAccelerator"];
            Assert.Equal(accelerator.Color, ((SolidColorBrush)runs[0].Foreground).Color);

            // The label must inherit, not re-declare: a local brush here would survive
            // a theme switch and leave a red-on-red or black-on-black segment.
            Assert.Null(runs[2].ReadLocalValue(TextElement.ForegroundProperty) as Brush);
        });
    }

    [Fact]
    public void SeparatorInkIsBlackNotDarkGray()
    {
        WithBar(1, (bar, _) =>
        {
            var text = FindDescendant<TextBlock>(ContainerAt(bar, 1));
            Assert.NotNull(text);

            // CP437 0xB3 (vertical bar), drawn black - not the dark gray of the blank
            // cells around it.
            Assert.Equal("\u2502", text!.Text);
            Assert.Equal(Colors.Black, ((SolidColorBrush)text.Foreground).Color);
        });
    }

    private static T? FindDescendant<T>(DependencyObject root) where T : DependencyObject
    {
        int count = VisualTreeHelper.GetChildrenCount(root);
        for (int i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is T hit)
                return hit;

            var nested = FindDescendant<T>(child);
            if (nested != null)
                return nested;
        }

        return null;
    }
}
