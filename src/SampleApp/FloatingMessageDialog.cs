using System.Windows;
using System.Windows.Controls;
using TurbolandTheme.Wpf.Controls;

namespace SampleApp;

/// <summary>
/// The same message dialog as <see cref="MessageDialog"/>, but as a true top-level
/// <see cref="TurbolandFloatingDialog"/> window: its own HWND, owned by the main window,
/// with the hard shadow drawn inside the transparent window surface. Use this shape when
/// the content behind the dialog is a WebView2 or any other airspace-sensitive control.
/// </summary>
internal sealed class FloatingMessageDialog : TurbolandFloatingDialog
{
    internal FloatingMessageDialog(string title, string message)
    {
        Title = title;

        TextBlock text = new() { Text = message };

        Button ok = new()
        {
            Content = "OK",
            IsDefault = true,
            IsCancel = true,
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        ok.Click += (_, _) => Close(true);

        StackPanel panel = new();
        panel.Children.Add(text);
        panel.Children.Add(ok);
        Content = panel;

        // The gap between the message and the button is one character row, taken from
        // the metric tokens so it tracks the active scale factor. Deferred to Loaded
        // for the same reason MessageDialog defers it.
        Loaded += (_, _) =>
        {
            if (TryFindResource("Turboland.Metric.CellHeight") is double cellHeight)
                text.Margin = new Thickness(0, 0, 0, cellHeight);
        };
    }
}
