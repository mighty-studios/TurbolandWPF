using System.IO;
using System.Windows;
using System.Windows.Markup;
using Xunit;
using Xunit.Abstractions;

namespace TurbolandTheme.Tests.VisualRegression;

/// <summary>
/// Diagnostic: loads each control dictionary individually and reports which
/// one throws, so a XamlParseException in the full aggregator can be pinned
/// to a single file. Not a passing/failing test of behavior - it always
/// passes and writes its findings to the test output.
/// </summary>
[Collection("Wpf")]
public class DictionaryLoadDiagnostic
{
    private readonly ITestOutputHelper _output;

    public DictionaryLoadDiagnostic(ITestOutputHelper output, WpfApplicationFixture fixture)
    {
        _output = output;
        // Touch the Application so the pack URI scheme is registered before we
        // build pack URIs (otherwise the generic Uri parser chokes on the port).
        _ = fixture.App;
    }

    [Fact]
    public void Load_Each_Control_Dictionary()
    {
        string[] files =
        {
            "Controls.Button.xaml", "Controls.CheckBox.xaml", "Controls.RadioButton.xaml",
            "Controls.TextBox.xaml", "Controls.PasswordBox.xaml", "Controls.Menu.xaml",
            "Controls.ListBox.xaml", "Controls.ComboBox.xaml", "Controls.TreeView.xaml",
            "Controls.ScrollBar.xaml", "Controls.StatusBar.xaml", "Controls.TabControl.xaml",
            "Controls.GroupBox.xaml",
        };

        foreach (string file in files)
        {
            string uri = $"pack://application:,,,/TurbolandTheme.Wpf;component/Themes/{file}";
            try
            {
                var dict = new ResourceDictionary { Source = new Uri(uri, UriKind.Absolute) };
                // Force the load by enumerating keys.
                int count = 0;
                foreach (var _ in dict.Keys) count++;
                _output.WriteLine($"OK   {file} ({count} keys)");
            }
            catch (Exception ex)
            {
                _output.WriteLine($"FAIL {file}: {ex.GetBaseException().Message}");
                _output.WriteLine($"     {ex.Message}");
            }
        }
    }
}
