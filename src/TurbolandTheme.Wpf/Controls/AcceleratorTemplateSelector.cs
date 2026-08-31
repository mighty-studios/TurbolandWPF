using System.Windows;
using System.Windows.Controls;

namespace TurbolandTheme.Wpf.Controls;

/// <summary>
/// Applies an <see cref="AcceleratorText"/> template only to string content that
/// actually carries an access-key marker, and returns null for everything else so
/// the <see cref="ContentPresenter"/> falls back to its default handling.
/// </summary>
/// <remarks>
/// A plain <c>ContentTemplate</c> could not be used: it would apply to <em>all</em>
/// content, so <c>&lt;Button&gt;&lt;Image/&gt;&lt;/Button&gt;</c> would bind the image
/// to a string property and render its type name. Selecting per item keeps the theme
/// safe for arbitrary content while still coloring every text caption.
/// </remarks>
public class AcceleratorTemplateSelector : DataTemplateSelector
{
    /// <summary>Template used for captions that contain an access key.</summary>
    public DataTemplate? AcceleratorTemplate { get; set; }

    public override DataTemplate? SelectTemplate(object item, DependencyObject container)
    {
        if (item is string text && AcceleratorText.TryParse(text, out _, out _, out _, out _))
            return AcceleratorTemplate;

        return null;
    }
}
