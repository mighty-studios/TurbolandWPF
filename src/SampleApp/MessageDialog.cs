using System.Windows;
using System.Windows.Controls;
using TurbolandTheme.Wpf.Controls;

namespace SampleApp;

/// <summary>
/// A minimal message dialog, built in code to show that <see cref="TurbolandDialog"/>
/// needs no XAML file of its own: it is a <c>ContentControl</c>, so any content will do.
/// </summary>
internal sealed class MessageDialog : TurbolandDialog
{
    internal MessageDialog(string title, string message)
    {
        Title = title;

        TextBlock text = new() { Text = message };

        // Deliberately not IsCancel: WPF answers a click on a cancel button by setting
        // DialogResult=false itself, which would fight the Close(true) below. Esc still
        // works - TurbolandDialog falls back to Close(false) when no cancel button exists.
        Button ok = new()
        {
            Content = "OK",
            IsDefault = true,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        ok.Click += (_, _) => Close(true);

        StackPanel panel = new();
        panel.Children.Add(text);
        panel.Children.Add(ok);
        Content = panel;

        // The gap between the message and the button is one character row, taken from
        // the metric tokens so it tracks the active scale factor instead of being a
        // pixel literal. Deferred to Loaded because the resource is only reachable
        // once the dialog is on a host and therefore in the resource tree.
        Loaded += (_, _) =>
        {
            if (TryFindResource("Turboland.Metric.CellHeight") is double cellHeight)
                text.Margin = new Thickness(0, 0, 0, cellHeight);
        };
    }
}
