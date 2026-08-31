using System.Windows;
using TurbolandTheme.Wpf.Controls;

namespace SampleApp;

/// <summary>
/// A demo Preferences dialog: check boxes, radio buttons, buttons, a list box and a
/// scroll bar, laid out in two columns over a row of buttons.
/// </summary>
public partial class PreferencesDialog : TurbolandDialog
{
    /// <summary>Visible rows in the file list; fewer than its items, so it scrolls.</summary>
    private const int VisibleFileRows = 5;

    public PreferencesDialog()
    {
        InitializeComponent();

        // A pixel literal would only be a whole number of rows at 1x, so the height is
        // derived from the metric token the way the shell's own size is.
        Files.MaxHeight = VisibleFileRows * (double)FindResource("Turboland.Metric.CellHeight");
    }

    private void OnOk(object sender, RoutedEventArgs e) => Close(true);

    private void OnCancel(object sender, RoutedEventArgs e) => Close(false);

    private void OnHelp(object sender, RoutedEventArgs e) =>
        Host?.Show(new MessageDialog("Help", "Sets the IDE's screen size, command set" +
                                             Environment.NewLine + "and auto-save options."));
}
