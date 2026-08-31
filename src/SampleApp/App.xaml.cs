using System.Windows;
using Theme = TurbolandTheme.Wpf.TurbolandTheme;

namespace SampleApp;

/// <summary>
/// Interaction logic for App.xaml.
///
/// Command line: [authentic|accessible] [1|2|3|4]
/// e.g. SampleApp.exe accessible 2
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // ThemeMode is fully qualified because App derives from Application, whose
        // nested System.Windows.Application.ThemeMode would otherwise shadow a using
        // alias (member lookup beats using directives).
        var mode = e.Args.Length > 0 && e.Args[0].StartsWith("access", StringComparison.OrdinalIgnoreCase)
            ? TurbolandTheme.Core.ThemeMode.Accessible
            : TurbolandTheme.Core.ThemeMode.Authentic;

        int? scale = e.Args.Length > 1 && int.TryParse(e.Args[1], out int parsed) ? parsed : null;

        Theme.Apply(this, mode, scale);

        // No Theme.ApplyTo here, unlike the Gallery: that call exists to give a plain
        // derived Window the shell style, and TurbolandWindow already sets its own style
        // (and the text-rendering settings that come with it) in its constructor.
        new MainWindow().Show();
    }
}
