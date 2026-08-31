using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using TurbolandTheme.Wpf.Controls;
using Xunit;
using Theme = TurbolandTheme.Wpf.TurbolandTheme;
using ThemeMode = TurbolandTheme.Core.ThemeMode;

namespace TurbolandTheme.Tests.VisualRegression;

/// <summary>
/// The pixel-diff suite: every subject below is rendered in both modes at 1x and 2x and
/// compared against a committed PNG.
///
/// <para>
/// The subjects are chosen for what they would <i>catch</i>, not for coverage of the type
/// list. Each one is a place where being wrong would not show up in any property
/// assertion:
/// </para>
///
/// <list type="bullet">
/// <item><c>watch-panel</c> is a <c>GroupBox</c> on the blue desktop - the arrangement where
/// a caption mask hardcoded to the dialog face draws a grey block on blue that every
/// logical-tree test agrees is correct.</item>
/// <item><c>dialog</c> is the drop shadow, two cells right and one row down, which is only two
/// numbers until the cell stops being square.</item>
/// <item><c>lists</c> forces a scroll bar next to items, where they can overlap.</item>
/// <item><c>statusbar</c> is the nine-cell hint grid, where a one-cell error shifts everything
/// after the first segment and nothing throws.</item>
/// </list>
///
/// <para>
/// Rendering both modes at both scales is what makes the suite worth its weight. A theme with
/// one hardcoded pixel value passes at 1x and fails at 2x, and a token that Accessible mode
/// forgets to override passes in Authentic. Neither is visible in a single render.
/// </para>
/// </summary>
[Collection("Wpf")]
public class VisualBaselineTest
{
    private readonly Application _app;

    public VisualBaselineTest(WpfApplicationFixture fixture) => _app = fixture.App;

    /// <summary>
    /// A subject is a control tree plus the surface it belongs on. The surface matters:
    /// half the interesting bugs are a control assuming it sits on the dialog face.
    /// </summary>
    private sealed record Subject(string Surface, Func<Ctx, FrameworkElement> Build);

    /// <summary>
    /// Hands each builder the live metrics so nothing is sized in raw pixels. A literal
    /// would make every 2x baseline a picture of the wrong layout rather than a scaled
    /// copy of the 1x one, which is the whole point of rendering at two scales.
    /// </summary>
    private sealed class Ctx(Application app)
    {
        public double Cell { get; } = (double)app.Resources["Turboland.Metric.CellWidth"];

        public double Row { get; } = (double)app.Resources["Turboland.Metric.CellHeight"];

        public Brush Brush(string key) => (Brush)app.Resources[key];
    }

    private static readonly Dictionary<string, Subject> Subjects = new()
    {
        ["buttons"] = new("Turboland.Brush.WindowBackground", Buttons),
        ["toggles"] = new("Turboland.Brush.WindowBackground", Toggles),
        ["fields"] = new("Turboland.Brush.WindowBackground", Fields),
        ["lists"] = new("Turboland.Brush.WindowBackground", Lists),
        ["tabs"] = new("Turboland.Brush.WindowBackground", Tabs),
        ["groupbox"] = new("Turboland.Brush.WindowBackground", GroupBoxOnDialogFace),
        ["watch-panel"] = new("Turboland.Brush.DesktopBackground", GroupBoxOnDesktop),
        ["statusbar"] = new("Turboland.Brush.DesktopBackground", StatusBar),
        ["menubar"] = new("Turboland.Brush.DesktopBackground", MenuBar),
        ["dialog"] = new("Turboland.Brush.DesktopBackground", Dialog),
    };

    public static IEnumerable<object[]> Cases() =>
        from subject in Subjects.Keys.OrderBy(k => k)
        from mode in new[] { ThemeMode.Authentic, ThemeMode.Accessible }
        from scale in new[] { 1, 2 }
        select new object[] { subject, mode, scale };

    [Theory]
    [MemberData(nameof(Cases))]
    public void Render_MatchesBaseline(string subject, ThemeMode mode, int scale)
    {
        StaThread.Run(() =>
        {
            Theme.Apply(_app, mode, scale);

            Ctx ctx = new(_app);
            FrameworkElement root = Frame(ctx, Subjects[subject]);

            VisualBaseline.Verify(
                $"{subject}.{mode.ToString().ToLowerInvariant()}.{scale}x", root);
        });
    }

    /// <summary>
    /// Puts a subject on its surface with one cell of padding all round.
    /// </summary>
    /// <remarks>
    /// The padding is not decoration: a control that paints outside its own bounds -
    /// which the dialog shadow does deliberately, and a mis-set negative margin does
    /// accidentally - is invisible in a bitmap cropped to the control. The margin gives
    /// that ink somewhere to land where a diff can see it.
    /// </remarks>
    private static FrameworkElement Frame(Ctx ctx, Subject definition)
    {
        Border root = new()
        {
            Background = ctx.Brush(definition.Surface),
            Padding = new Thickness(ctx.Cell, ctx.Row, ctx.Cell, ctx.Row),
            Child = definition.Build(ctx),

            // The window style normally supplies these by inheritance. A detached tree
            // has no window, so without them raw text would rasterise with a different
            // formatting mode than it does in the app - a baseline of something the
            // user never sees.
            UseLayoutRounding = true,
            SnapsToDevicePixels = true,
        };

        TextOptions.SetTextFormattingMode(root, TextFormattingMode.Display);
        TextOptions.SetTextRenderingMode(root, TextRenderingMode.Grayscale);
        return root;
    }

    /// <summary>
    /// Every subject at 2x must be exactly twice the size it is at 1x.
    /// </summary>
    /// <remarks>
    /// The baselines already encode this, but only as unrelated pictures: a broken scale
    /// factor reports itself as a large fraction of pixels differing on half of them and
    /// leaves the cause to be worked out. Asserting the invariant directly names it. It is
    /// also the single cheapest test for a hardcoded pixel value anywhere in the theme,
    /// because a literal is the one thing that does not double.
    /// </remarks>
    [Theory]
    [MemberData(nameof(SubjectsAndModes))]
    public void TwoTimesRender_IsAnExactDoublingOfOneTimes(string subject, ThemeMode mode)
    {
        Size single = MeasureSubject(subject, mode, 1);
        Size doubled = MeasureSubject(subject, mode, 2);

        Assert.Equal(single.Width * 2, doubled.Width);
        Assert.Equal(single.Height * 2, doubled.Height);
    }

    public static IEnumerable<object[]> SubjectsAndModes() =>
        from subject in Subjects.Keys.OrderBy(k => k)
        from mode in new[] { ThemeMode.Authentic, ThemeMode.Accessible }
        select new object[] { subject, mode };

    private Size MeasureSubject(string subject, ThemeMode mode, int scale)
    {
        Size size = default;

        StaThread.Run(() =>
        {
            Theme.Apply(_app, mode, scale);
            Ctx ctx = new(_app);
            FrameworkElement root = Frame(ctx, Subjects[subject]);
            root.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            size = new Size(
                Math.Ceiling(root.DesiredSize.Width), Math.Ceiling(root.DesiredSize.Height));
        });

        return size;
    }

    /// <summary>
    /// Fails if the baselines folder holds a PNG no test claims.
    /// </summary>
    /// <remarks>
    /// A renamed or deleted subject leaves its picture behind, and a stale baseline is
    /// worse than a missing one: it looks like evidence. Nothing points at an orphan, so
    /// nothing finds it.
    /// </remarks>
    [Fact]
    public void BaselineFolder_HoldsNothingUnclaimed()
    {
        HashSet<string> expected = Cases()
            .Select(c => $"{c[0]}.{c[1]!.ToString()!.ToLowerInvariant()}.{c[2]}x.png")
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        string[] orphans = Directory
            .EnumerateFiles(VisualBaseline.BaselineDirectory, "*.png")
            .Select(Path.GetFileName)
            .Where(name => !expected.Contains(name!))
            .OrderBy(name => name)
            .ToArray()!;

        Assert.True(
            orphans.Length == 0,
            $"Unclaimed baseline(s) in {VisualBaseline.BaselineDirectory}: " +
            string.Join(", ", orphans) +
            ". Delete them, or restore the subject that produced them.");
    }

    private static StackPanel Stack(Orientation orientation, double gap, params UIElement[] children)
    {
        StackPanel panel = new() { Orientation = orientation };
        foreach (UIElement child in children)
        {
            if (panel.Children.Count > 0 && child is FrameworkElement element)
            {
                element.Margin = orientation == Orientation.Horizontal
                    ? new Thickness(gap, 0, 0, 0)
                    : new Thickness(0, gap, 0, 0);
            }

            panel.Children.Add(child);
        }

        return panel;
    }

    /// <summary>
    /// Default, plain and disabled. <c>IsDefault</c> and <c>IsEnabled</c> are settable, so
    /// they render here; hover and pressed belong to the input system and stay with the
    /// trigger tests.
    /// </summary>
    // Two cells apart, not one: the shadow is painted outside each button's layout
    // box and fills the first cell of any gap. See Turboland.Size.ButtonStripMargin.
    private static FrameworkElement Buttons(Ctx ctx) => Stack(
        Orientation.Horizontal, ctx.Cell * 2,
        new Button { Content = "OK", IsDefault = true },
        new Button { Content = "Cancel", IsCancel = true },
        new Button { Content = "Help", IsEnabled = false });

    private static FrameworkElement Toggles(Ctx ctx) => Stack(
        Orientation.Vertical, ctx.Row / 2,
        // Accelerator markers are deliberate: they render Turboland.Brush.FieldAccelerator
        // (yellow on cyan, 2.69:1 - the theme's lowest-contrast pair), so a change to it
        // shows up in a baseline.
        new CheckBox { Content = "_Insert mode", IsChecked = true },
        new CheckBox { Content = "_Auto indent", IsChecked = false },
        new CheckBox { Content = "Use _tabs", IsChecked = null },
        new CheckBox { Content = "Optimal _fill", IsEnabled = false },
        new RadioButton { Content = "_25 lines", IsChecked = true, GroupName = "g" },
        new RadioButton { Content = "_43/50 lines", GroupName = "g" },
        new RadioButton { Content = "_80 columns", IsEnabled = false, GroupName = "h" });

    private static FrameworkElement Fields(Ctx ctx)
    {
        double width = 24 * ctx.Cell;

        TextBox filled = new() { Text = "*.PAS", Width = width };
        TextBox disabled = new() { Text = "read only", Width = width, IsEnabled = false };
        PasswordBox password = new() { Password = "secret", Width = width };

        ComboBox combo = new() { Width = width };
        combo.Items.Add("Turboland Pascal");
        combo.Items.Add("Turboland C");
        combo.SelectedIndex = 0;

        return Stack(Orientation.Vertical, ctx.Row / 2, filled, disabled, password, combo);
    }

    /// <summary>
    /// Deliberately more items than fit, so the scroll bar is realised and sits next to a
    /// selected row - the adjacency where the bar can overlap the row it belongs to.
    /// </summary>
    private static FrameworkElement Lists(Ctx ctx)
    {
        ListBox list = new() { Width = 24 * ctx.Cell, Height = 6 * ctx.Row };
        foreach (string item in new[]
                 { "CRT.TPU", "DOS.TPU", "GRAPH.TPU", "PRINTER.TPU", "SYSTEM.TPU", "TURBO3.TPU", "WINDOS.TPU" })
        {
            list.Items.Add(item);
        }

        list.SelectedIndex = 1;
        return list;
    }

    private static FrameworkElement GroupBoxOnDialogFace(Ctx ctx) => new GroupBox
    {
        Header = "Tab size",
        Width = 24 * ctx.Cell,
        Content = Stack(
            Orientation.Vertical, 0,
            new RadioButton { Content = "2", IsChecked = true, GroupName = "t" },
            new RadioButton { Content = "4", GroupName = "t" },
            new RadioButton { Content = "8", GroupName = "t" }),
    };

    /// <summary>
    /// The same control on the blue desktop, configured the way a caller has to configure
    /// it. <c>Background</c> and <c>BorderBrush</c> are template-bound so the caption fill
    /// follows the surface instead of drawing a grey block on blue.
    /// </summary>
    private static FrameworkElement GroupBoxOnDesktop(Ctx ctx) => new GroupBox
    {
        Header = "Watch",
        Width = 30 * ctx.Cell,
        Background = ctx.Brush("Turboland.Brush.DesktopBackground"),
        BorderBrush = ctx.Brush("Turboland.Brush.FrameInactive"),
        Foreground = ctx.Brush("Turboland.Brush.DesktopForeground"),
        Content = new TextBlock
        {
            Text = "Count: 0",
            Foreground = ctx.Brush("Turboland.Brush.DesktopForeground"),
        },
    };

    /// <summary>
    /// A tab control, for the seam between the selected tab and its page. The two are
    /// meant to merge into one shape; if they read as separate boxes with a rule between
    /// them - a pure z-order effect - it is invisible to any test that inspects properties
    /// rather than pixels.
    /// </summary>
    private static FrameworkElement Tabs(Ctx ctx)
    {
        TabControl tabs = new() { Width = 30 * ctx.Cell, Height = 6 * ctx.Row };
        tabs.Items.Add(new TabItem
        {
            Header = "_Editor",
            Content = new TextBlock { Text = "Insert mode" },
        });
        tabs.Items.Add(new TabItem { Header = "_Display" });
        tabs.Items.Add(new TabItem { Header = "_Colors" });
        return tabs;
    }

    private static FrameworkElement StatusBar(Ctx ctx)
    {
        System.Windows.Controls.Primitives.StatusBar bar = new() { Width = 46 * ctx.Cell };
        bar.Items.Add(new StatusBarHint { Key = "F1", Label = "Help" });
        bar.Items.Add(new Separator());
        bar.Items.Add(new StatusBarHint { Key = "F2", Label = "Save" });
        bar.Items.Add(new Separator());
        bar.Items.Add(new StatusBarHint { Key = "F3", Label = "Open" });
        return bar;
    }

    private static FrameworkElement MenuBar(Ctx ctx)
    {
        Menu menu = new() { Width = 46 * ctx.Cell };
        foreach (string header in new[] { "_File", "_Edit", "_Search", "_Run", "_Compile" })
            menu.Items.Add(new MenuItem { Header = header });

        return menu;
    }

    /// <summary>
    /// Rendered inside its real host rather than alone, because the shadow paints outside
    /// the dialog's own bounds and only the host gives it somewhere to land. The host also
    /// marks the dialog active, so this is the active frame.
    /// </summary>
    private static FrameworkElement Dialog(Ctx ctx)
    {
        TurbolandDialog dialog = new()
        {
            Title = "Confirm",
            Width = 34 * ctx.Cell,
            Content = Stack(
                Orientation.Vertical, ctx.Row,
                new TextBlock { Text = "Save changes to NONAME00.PAS?" },
                Stack(
                    Orientation.Horizontal, ctx.Cell,
                    new Button { Content = "Yes", IsDefault = true },
                    new Button { Content = "No" },
                    new Button { Content = "Cancel", IsCancel = true })),
        };

        TurbolandDialogHost host = new()
        {
            Width = 44 * ctx.Cell,
            Height = 10 * ctx.Row,
        };

        host.Show(dialog);
        return host;
    }
}
