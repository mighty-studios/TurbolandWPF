using System.Windows;

namespace TurbolandTheme.Gallery;

/// <summary>Interaction logic for MainWindow.xaml</summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void OnExit(object sender, RoutedEventArgs e) => Close();

    /// <summary>
    /// Shows the About box as a <see cref="FloatingMessageDialog"/>: a real modal
    /// window owned by this one, so it floats above everything - including the
    /// airspace-sensitive content an in-client dialog could not sort against.
    /// </summary>
    private void OnAbout(object sender, RoutedEventArgs e) =>
        new FloatingMessageDialog(
            "About",
            "Turboland theme for WPF." + Environment.NewLine +
            "A TurbolandFloatingDialog: its own HWND, owned by this window," + Environment.NewLine +
            "with the hard shadow drawn inside the transparent window surface.")
            .ShowDialog(this);
}
