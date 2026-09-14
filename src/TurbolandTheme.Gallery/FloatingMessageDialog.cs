using System.Windows;
using System.Windows.Controls;
using TurbolandTheme.Wpf.Controls;

namespace TurbolandTheme.Gallery;

/// <summary>
/// A message box as a true top-level <see cref="TurbolandFloatingDialog"/> window:
/// its own HWND, owned by the main window, with the hard shadow drawn inside the
/// transparent window surface. The same shape the SampleApp uses for its floating
/// About box.
/// </summary>
internal sealed class FloatingMessageDialog : TurbolandFloatingDialog
{
    internal FloatingMessageDialog(string title, string message)
    {
        Title = title;

        TextBlock text = new() { Text = message };

        // Deliberately not IsCancel: WPF answers a click on a cancel button by setting
        // DialogResult=false itself, which would fight the Close(true) below. With no
        // cancel button present, TurbolandFloatingDialog closes on Esc on its own.
        Button ok = new()
        {
            Content = "OK",
            IsDefault = true,
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        ok.Click += (_, _) => Close(true);

        StackPanel panel = new();
        panel.Children.Add(text);
        panel.Children.Add(ok);
        Content = panel;

        // The gap between the message and the button is one character row, taken from
        // the metric tokens so it tracks the active scale factor. Deferred to Loaded
        // because the resource is only reachable once the dialog is on a host.
        Loaded += (_, _) =>
        {
            if (TryFindResource("Turboland.Metric.CellHeight") is double cellHeight)
                text.Margin = new Thickness(0, 0, 0, cellHeight);
        };
    }
}
