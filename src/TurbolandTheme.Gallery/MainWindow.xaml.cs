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
}
