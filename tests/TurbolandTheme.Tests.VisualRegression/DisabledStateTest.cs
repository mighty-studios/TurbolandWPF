using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Media;
using TurbolandTheme.Wpf.Controls;
using Xunit;
using Theme = TurbolandTheme.Wpf.TurbolandTheme;
using ThemeMode = TurbolandTheme.Core.ThemeMode;

namespace TurbolandTheme.Tests.VisualRegression;

/// <summary>
/// Every themed control renders a distinct disabled state: the ink drops to
/// <c>Turboland.Brush.DisabledForeground</c>, and where a control fixes its own
/// colors inside a nested template - the combo's drop arrow, the tree's expander
/// marker, the status hint's key run - the nested part must dim too, because a
/// property trigger on the outer control cannot reach an explicit brush set below it.
/// </summary>
/// <remarks>
/// These are trigger-level assertions, not pixels: the states a pixel harness can
/// reach (IsEnabled is settable) are covered by the baselines for the controls that
/// have them; what is pinned here is that each template actually resolves its
/// disabled parts to the disabled token, in both modes.
/// </remarks>
[Collection("Wpf")]
public class DisabledStateTest
{
    private readonly Application _app;

    public DisabledStateTest(WpfApplicationFixture fixture) => _app = fixture.App;

    private Color Disabled => ((SolidColorBrush)_app.Resources["Turboland.Brush.DisabledForeground"]).Color;

    [Theory]
    [InlineData(ThemeMode.Authentic)]
    [InlineData(ThemeMode.Accessible)]
    public void Button_Disabled_DimsFaceLabelAndAccelerator(ThemeMode mode)
    {
        StaThread.Run(() =>
        {
            Theme.Apply(_app, mode, 1);

            var button = new Button { Content = "_Help", IsEnabled = false };
            using var host = ShowHosted(button);

            var face = (Border)button.Template.FindName("face", button);
            var expectedFace = ((SolidColorBrush)_app.Resources["Turboland.Brush.ButtonDisabledFace"]).Color;
            Assert.Equal(expectedFace, ((SolidColorBrush)face.Background).Color);

            Assert.Equal(Disabled, ((SolidColorBrush)button.Foreground).Color);

            var accel = FindVisual<AcceleratorText>(button);
            Assert.NotNull(accel);
            Assert.Equal(Disabled, ((SolidColorBrush)accel.Inlines.OfType<Run>().First().Foreground).Color);
        });
    }

    [Fact]
    public void ScrollBar_Disabled_DimsArrows()
    {
        StaThread.Run(() =>
        {
            Theme.Apply(_app, ThemeMode.Authentic, 1);

            var bar = new ScrollBar { Height = 120, IsEnabled = false };
            using var host = ShowHosted(bar);

            var up = (RepeatButton)bar.Template.FindName("PART_LineUpButton", bar);
            up.ApplyTemplate();
            var arrow = (TextBlock)up.Template.FindName("arrow", up);
            Assert.Equal(Disabled, ((SolidColorBrush)arrow.Foreground).Color);
        });
    }

    [Fact]
    public void ComboBox_Disabled_DimsFieldAndDropArrow()
    {
        StaThread.Run(() =>
        {
            Theme.Apply(_app, ThemeMode.Authentic, 1);

            var combo = new ComboBox { IsEnabled = false };
            combo.Items.Add("8086");
            combo.SelectedIndex = 0;
            using var host = ShowHosted(combo);

            Assert.Equal(Disabled, ((SolidColorBrush)combo.Foreground).Color);

            var toggle = FindVisual<ToggleButton>(combo);
            Assert.NotNull(toggle);
            toggle.ApplyTemplate();
            var arrow = (TextBlock)toggle.Template.FindName("arrow", toggle);
            Assert.Equal(Disabled, ((SolidColorBrush)arrow.Foreground).Color);
        });
    }

    [Fact]
    public void TreeViewItem_Disabled_DimsLabelAndExpanderMarker()
    {
        StaThread.Run(() =>
        {
            Theme.Apply(_app, ThemeMode.Authentic, 1);

            var root = new TreeViewItem { Header = "TURBO.PAS", IsExpanded = true };
            var child = new TreeViewItem { Header = "var", IsEnabled = false };
            child.Items.Add(new TreeViewItem { Header = "leaf" });
            root.Items.Add(child);
            var tree = new TreeView { Items = { root } };
            using var host = ShowHosted(tree);

            Assert.Equal(Disabled, ((SolidColorBrush)child.Foreground).Color);

            child.ApplyTemplate();
            var expander = FindVisual<ToggleButton>(child);
            Assert.NotNull(expander);
            expander.ApplyTemplate();
            var marker = (TextBlock)expander.Template.FindName("marker", expander);
            Assert.Equal(Disabled, ((SolidColorBrush)marker.Foreground).Color);
        });
    }

    [Fact]
    public void StatusBarHint_Disabled_DimsKeyAndLabel()
    {
        StaThread.Run(() =>
        {
            Theme.Apply(_app, ThemeMode.Authentic, 1);

            var bar = new StatusBar();
            var hint = new StatusBarHint { Key = "F8", Label = "Zoom", IsEnabled = false };
            bar.Items.Add(hint);
            using var host = ShowHosted(bar);

            hint.ApplyTemplate();
            var text = FindVisual<TextBlock>(hint);
            Assert.NotNull(text);

            var runs = text.Inlines.OfType<Run>().ToList();
            Assert.Equal(3, runs.Count);
            // The key fixes its own accelerator ink; the trigger must recolor it.
            Assert.Equal(Disabled, ((SolidColorBrush)runs[0].Foreground).Color);
            // The label inherits: its effective ink must be the dim color.
            Assert.Equal(Disabled, ((SolidColorBrush)runs[2].Foreground).Color);
        });
    }

    [Fact]
    public void TabItem_Disabled_DimsLabelAndAccelerator()
    {
        StaThread.Run(() =>
        {
            Theme.Apply(_app, ThemeMode.Authentic, 1);

            var tabs = new TabControl();
            tabs.Items.Add(new TabItem { Header = "Code" });
            var disabled = new TabItem { Header = "_History", IsEnabled = false };
            tabs.Items.Add(disabled);
            using var host = ShowHosted(tabs);

            Assert.Equal(Disabled, ((SolidColorBrush)disabled.Foreground).Color);

            var accel = FindVisual<AcceleratorText>(disabled);
            Assert.NotNull(accel);
            Assert.Equal(Disabled, ((SolidColorBrush)accel.Inlines.OfType<Run>().First().Foreground).Color);
        });
    }

    [Fact]
    public void MenuItem_Disabled_DimsLabelAndAccelerator()
    {
        StaThread.Run(() =>
        {
            Theme.Apply(_app, ThemeMode.Authentic, 1);

            var menu = new Menu();
            var file = new MenuItem { Header = "_File" };
            var print = new MenuItem { Header = "_Print", IsEnabled = false };
            file.Items.Add(print);
            menu.Items.Add(file);
            using var host = ShowHosted(menu);

            // Submenu items are not realized until the popup opens, and the template
            // trigger that dims the label only runs on a realized template.
            file.IsSubmenuOpen = true;
            System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(
                () => { }, System.Windows.Threading.DispatcherPriority.Loaded);
            print.ApplyTemplate();

            Assert.Equal(Disabled, ((SolidColorBrush)print.Foreground).Color);

            var accel = FindVisual<AcceleratorText>(print);
            Assert.NotNull(accel);
            Assert.Equal(Disabled, ((SolidColorBrush)accel.Inlines.OfType<Run>().First().Foreground).Color);
        });
    }

    [Fact]
    public void CheckBox_And_RadioButton_Disabled_Dim()
    {
        StaThread.Run(() =>
        {
            Theme.Apply(_app, ThemeMode.Authentic, 1);

            var check = new CheckBox { Content = "_Backup", IsEnabled = false };
            var radio = new RadioButton { Content = "_43/50", IsEnabled = false };
            var panel = new StackPanel();
            panel.Children.Add(check);
            panel.Children.Add(radio);
            using var host = ShowHosted(panel);

            Assert.Equal(Disabled, ((SolidColorBrush)check.Foreground).Color);
            Assert.Equal(Disabled, ((SolidColorBrush)radio.Foreground).Color);
        });
    }

    [Fact]
    public void Fields_Disabled_DimTextAndKeepAnOutline()
    {
        StaThread.Run(() =>
        {
            Theme.Apply(_app, ThemeMode.Authentic, 1);

            var text = new TextBox { Text = "read only", IsEnabled = false, Width = 120 };
            var password = new PasswordBox { IsEnabled = false, Width = 120 };
            var panel = new StackPanel();
            panel.Children.Add(text);
            panel.Children.Add(password);
            using var host = ShowHosted(panel);

            foreach (Control field in new Control[] { text, password })
            {
                Assert.Equal(Disabled, ((SolidColorBrush)field.Foreground).Color);

                // A disabled field takes the surface color, so the ring must be drawn
                // in the dim ink or the field stops looking like a field at all.
                var ring = (Border)field.Template.FindName("outline", field);
                Assert.NotEqual(0d, ring.BorderThickness.Left);
                Assert.Equal(Disabled, ((SolidColorBrush)ring.BorderBrush).Color);
            }
        });
    }

    private static HostedWindow ShowHosted(UIElement content)
    {
        var window = new Window
        {
            Content = content,
            Width = 300,
            Height = 200,
            // Off-screen: never let a probe window appear over the desktop.
            Left = -4000,
            Top = -4000,
            ShowInTaskbar = false,
            WindowStartupLocation = WindowStartupLocation.Manual,
        };
        window.Show();
        window.UpdateLayout();
        return new HostedWindow(window);
    }

    private sealed class HostedWindow : System.IDisposable
    {
        public HostedWindow(Window window) => Window = window;

        public Window Window { get; }

        public void Dispose() => Window.Close();
    }

    private static T? FindVisual<T>(DependencyObject? root) where T : DependencyObject
    {
        if (root == null)
            return null;

        int count = VisualTreeHelper.GetChildrenCount(root);
        for (int i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is T hit)
                return hit;

            var nested = FindVisual<T>(child);
            if (nested != null)
                return nested;
        }

        return null;
    }
}
