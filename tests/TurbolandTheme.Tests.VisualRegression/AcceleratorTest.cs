using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using TurbolandTheme.Wpf.Controls;
using Xunit;
using Theme = TurbolandTheme.Wpf.TurbolandTheme;
using ThemeMode = TurbolandTheme.Core.ThemeMode;

namespace TurbolandTheme.Tests.VisualRegression;

/// <summary>
/// Accelerator characters are drawn in the accelerator color, and the access key
/// still works after AccessText has been replaced.
/// </summary>
/// <remarks>
/// The access-key assertions are the important ones. WPF derives access keys
/// from *string* content, so re-templating a caption into colored runs silently
/// breaks Alt+F unless AcceleratorText re-registers it.
/// </remarks>
[Collection("Wpf")]
public class AcceleratorTest
{
    private readonly Application _app;

    public AcceleratorTest(WpfApplicationFixture fixture)
    {
        _app = fixture.App;
    }

    [Theory]
    [InlineData("_File", "", "F", "ile")]
    [InlineData("E_xit", "E", "x", "it")]
    [InlineData("Save _As", "Save ", "A", "s")]
    [InlineData("Tail_", "", "", "")]
    public void TryParse_SplitsOnTheAccessKeyMarker(string caption, string before, string key, string after)
    {
        bool found = AcceleratorText.TryParse(caption, out string b, out string k, out string a, out _);

        // A trailing lone underscore marks nothing, matching AccessText.
        if (key.Length == 0)
        {
            Assert.False(found);
            return;
        }

        Assert.True(found);
        Assert.Equal(before, b);
        Assert.Equal(key, k);
        Assert.Equal(after, a);
    }

    [Fact]
    public void TryParse_TreatsDoubledUnderscoreAsLiteral()
    {
        Assert.False(AcceleratorText.TryParse("a__b", out _, out _, out _, out string plain));
        Assert.Equal("a_b", plain);
    }

    [Fact]
    public void TryParse_UsesOnlyTheFirstMarker()
    {
        Assert.True(AcceleratorText.TryParse("_One _Two", out string before, out string key, out string after, out _));
        Assert.Equal("", before);
        Assert.Equal("O", key);
        Assert.Equal("ne Two", after);
    }

    [Fact]
    public void Button_AccessKey_SurvivesAcceleratorTemplating()
    {
        StaThread.Run(() =>
        {
            Theme.Apply(_app, ThemeMode.Authentic, 1);

            var button = new Button { Content = "_OK" };
            using var host = ShowHosted(button);

            var accel = FindVisual<AcceleratorText>(button);
            Assert.NotNull(accel);

            var scope = PresentationSource.FromVisual(host.Window);
            Assert.True(
                AccessKeyManager.IsKeyRegistered(scope, "O"),
                "Replacing AccessText must not lose the access key.");

            bool clicked = false;
            button.Click += (_, _) => clicked = true;
            AccessKeyManager.ProcessKey(scope, "O", false);
            Assert.True(clicked, "The access key must still invoke the control.");
        });
    }

    [Fact]
    public void MenuItem_AccessKey_RegistersInTheMenuScope()
    {
        StaThread.Run(() =>
        {
            Theme.Apply(_app, ThemeMode.Authentic, 1);

            var menu = new Menu();
            var file = new MenuItem { Header = "_File" };
            file.Items.Add(new MenuItem { Header = "_Open" });
            menu.Items.Add(file);
            using var host = ShowHosted(menu);

            // Menus are their own access-key scope, so the window scope is the wrong
            // place to look for a menu access key.
            Assert.True(AccessKeyManager.IsKeyRegistered(menu, "F"));
        });
    }

    [Fact]
    public void Authentic_ColorsTheAcceleratorRed_WithoutUnderline()
    {
        StaThread.Run(() =>
        {
            Theme.Apply(_app, ThemeMode.Authentic, 1);

            var menu = new Menu();
            menu.Items.Add(new MenuItem { Header = "_File" });
            using var host = ShowHosted(menu);

            var accel = FindVisual<AcceleratorText>(menu);
            Assert.NotNull(accel);

            var runs = accel.Inlines.OfType<Run>().ToList();
            Assert.Equal(2, runs.Count);
            Assert.Equal("F", runs[0].Text);
            Assert.Equal("ile", runs[1].Text);

            // Red (VGA 4) is the accelerator color.
            Assert.Equal(Color.FromRgb(170, 0, 0), ((SolidColorBrush)runs[0].Foreground).Color);

            // Authentic mode marks the accelerator by color alone, with no underline.
            Assert.True(runs[0].TextDecorations == null || runs[0].TextDecorations.Count == 0);

            // Only the access key is recolored. Foreground is an inherited
            // property, so the effective value is never null - assert that no
            // local value was set, which is what keeps the rest of the caption
            // following the control's own foreground through state changes.
            Assert.Equal(
                DependencyProperty.UnsetValue,
                runs[1].ReadLocalValue(System.Windows.Documents.TextElement.ForegroundProperty));
        });
    }

    [Fact]
    public void Accessible_AddsTheUnderline()
    {
        StaThread.Run(() =>
        {
            Theme.Apply(_app, ThemeMode.Accessible, 1);

            var menu = new Menu();
            menu.Items.Add(new MenuItem { Header = "_File" });
            using var host = ShowHosted(menu);

            var accel = FindVisual<AcceleratorText>(menu);
            Assert.NotNull(accel);

            var key = accel.Inlines.OfType<Run>().First();

            // Color alone fails WCAG 1.4.1, so Accessible mode restores it.
            Assert.NotNull(key.TextDecorations);
            Assert.NotEmpty(key.TextDecorations);
        });
    }

    [Fact]
    public void NonStringContent_IsLeftAlone()
    {
        StaThread.Run(() =>
        {
            Theme.Apply(_app, ThemeMode.Authentic, 1);

            var image = new Border { Width = 10, Height = 10 };
            var button = new Button { Content = image };
            using var host = ShowHosted(button);

            // The selector must decline non-text content, or arbitrary content would
            // be stringified into a caption.
            Assert.Null(FindVisual<AcceleratorText>(button));
        });
    }

    [Fact]
    public void StringWithoutMarker_IsLeftAlone()
    {
        StaThread.Run(() =>
        {
            Theme.Apply(_app, ThemeMode.Authentic, 1);

            var button = new Button { Content = "Plain" };
            using var host = ShowHosted(button);

            Assert.Null(FindVisual<AcceleratorText>(button));
        });
    }

    private static HostedWindow ShowHosted(UIElement content)
    {
        var window = new Window
        {
            Content = content,
            Width = 200,
            Height = 120,
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

            var deeper = FindVisual<T>(child);
            if (deeper != null)
                return deeper;
        }

        return null;
    }
}
