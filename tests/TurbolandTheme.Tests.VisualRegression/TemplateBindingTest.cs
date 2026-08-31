using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Xunit;
using Theme = TurbolandTheme.Wpf.TurbolandTheme;
using ThemeMode = TurbolandTheme.Core.ThemeMode;

namespace TurbolandTheme.Tests.VisualRegression;

/// <summary>
/// Pins the places where a themed template must honour the standard WPF property rather
/// than a hardcoded token. Both cases are silent: nothing throws, the wrong thing is
/// simply drawn.
/// </summary>
[Collection("Wpf")]
public class TemplateBindingTest
{
    private readonly Application _app;

    public TemplateBindingTest(WpfApplicationFixture fixture) => _app = fixture.App;

    private void Themed(System.Action body) =>
        StaThread.Run(() =>
        {
            Theme.Apply(_app, ThemeMode.Authentic, 1);
            body();
        });

    /// <summary>
    /// The submenu template renders <see cref="MenuItem.InputGestureText"/>, the property
    /// every WPF caller already sets and that WPF fills in automatically from a bound
    /// <c>RoutedUICommand</c>'s gestures.
    /// </summary>
    /// <remarks>
    /// Rendering a custom attached property that no consumer sets instead would silently
    /// drop every caller's InputGestureText: nothing fails, the shortcut column is just
    /// always empty.
    /// </remarks>
    [Fact]
    public void SubmenuItem_RendersInputGestureText()
    {
        Themed(() =>
        {
            MenuItem parent = new() { Header = "File" };
            MenuItem child = new() { Header = "Save", InputGestureText = "F2" };
            parent.Items.Add(child);

            Menu menu = new();
            menu.Items.Add(parent);
            menu.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            menu.Arrange(new Rect(menu.DesiredSize));

            // Role is SubmenuItem the moment the item is added to another MenuItem, which
            // is what selects the submenu template; the popup need not be open.
            Assert.Equal(MenuItemRole.SubmenuItem, child.Role);
            child.ApplyTemplate();

            var hint = (TextBlock)child.Template.FindName("keyHint", child);
            Assert.Equal("F2", hint.Text);
        });
    }

    /// <summary>
    /// A group box caption punches a gap in the top border line and fills it, so the fill
    /// has to match whatever surface the box sits on. Binding it to Background lets a
    /// caller on the blue desktop say so; hardcoding the dialog face paints a gray block.
    /// </summary>
    [Fact]
    public void GroupBox_CaptionMaskFollowsBackground()
    {
        Themed(() =>
        {
            SolidColorBrush surface = new(Colors.Magenta);
            GroupBox box = Realise(new GroupBox { Header = "Watch", Background = surface });

            var header = (Border)box.Template.FindName("header", box);
            Assert.Same(surface, header.Background);
        });
    }

    /// <summary>The frame line honours BorderBrush for the same reason.</summary>
    [Fact]
    public void GroupBox_FrameFollowsBorderBrush()
    {
        Themed(() =>
        {
            SolidColorBrush ink = new(Colors.Magenta);
            GroupBox box = Realise(new GroupBox { Header = "Watch", BorderBrush = ink });

            Border frame = FindFrame(box);
            Assert.Same(ink, frame.BorderBrush);
        });
    }

    /// <summary>Defaults stay on the dialog face, where a group box nearly always is.</summary>
    [Fact]
    public void GroupBox_DefaultsToTheDialogFace()
    {
        Themed(() =>
        {
            GroupBox box = Realise(new GroupBox { Header = "Watch" });

            var header = (Border)box.Template.FindName("header", box);
            Assert.Equal(_app.Resources["Turboland.Brush.WindowBackground"], header.Background);
            Assert.Equal(_app.Resources["Turboland.Brush.ControlShadow"], FindFrame(box).BorderBrush);
        });
    }

    private static GroupBox Realise(GroupBox box)
    {
        box.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        box.Arrange(new Rect(box.DesiredSize));
        box.ApplyTemplate();
        return box;
    }

    /// <summary>The frame border is the only unnamed Border in the template root.</summary>
    private static Border FindFrame(GroupBox box)
    {
        var root = (Grid)VisualTreeHelper.GetChild(box, 0);
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
            if (VisualTreeHelper.GetChild(root, i) is Border { Name: "" } frame)
                return frame;

        throw new Xunit.Sdk.XunitException("group box frame border not found");
    }
}
