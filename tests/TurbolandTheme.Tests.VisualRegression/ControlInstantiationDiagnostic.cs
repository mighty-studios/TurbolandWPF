using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Xunit;
using Xunit.Abstractions;
using Theme = TurbolandTheme.Wpf.TurbolandTheme;
using ThemeMode = TurbolandTheme.Core.ThemeMode;

namespace TurbolandTheme.Tests.VisualRegression;

/// <summary>
/// Diagnostic: applies the theme, then instantiates each control type one at
/// a time to find which implicit style throws "Expression type is not a valid
/// Style value". Always passes; writes findings to the test output.
/// </summary>
[Collection("Wpf")]
public class ControlInstantiationDiagnostic
{
    private readonly ITestOutputHelper _output;
    private readonly Application _app;

    public ControlInstantiationDiagnostic(ITestOutputHelper output, WpfApplicationFixture fixture)
    {
        _output = output;
        _app = fixture.App;
    }

    [Fact]
    public void Instantiate_Each_Control()
    {
        Theme.Apply(_app, ThemeMode.Authentic);

        var controls = new (string Name, Func<Control> Make)[]
        {
            ("Menu",        () => new Menu()),
            ("MenuItem",    () => new MenuItem()),
            ("StatusBar",   () => new StatusBar()),
            ("Button",      () => new Button()),
            ("TextBox",     () => new TextBox()),
            ("PasswordBox", () => new PasswordBox()),
            ("ListBox",     () => new ListBox()),
            ("ComboBox",    () => new ComboBox()),
            ("TreeView",    () => new TreeView()),
            ("CheckBox",    () => new CheckBox()),
            ("RadioButton", () => new RadioButton()),
            ("GroupBox",    () => new GroupBox()),
            ("TabControl",  () => new TabControl()),
            ("ScrollBar",   () => new ScrollBar()),
        };

        foreach (var (name, make) in controls)
        {
            try
            {
                var c = make();
                c.Measure(new Size(200, 200));
                _output.WriteLine($"OK   {name}");
            }
            catch (Exception ex)
            {
                _output.WriteLine($"FAIL {name}: {ex.GetBaseException().Message}");
            }
        }
    }
}
