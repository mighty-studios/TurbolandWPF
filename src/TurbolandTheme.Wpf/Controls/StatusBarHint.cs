using System.Windows;
using System.Windows.Controls;

namespace TurbolandTheme.Wpf.Controls;

/// <summary>
/// One segment of a Turbo-style status bar: a function-key name drawn in the
/// accelerator color followed by what that key does, for example a red
/// <c>F1</c> next to a black <c>Help</c>.
/// </summary>
/// <remarks>
/// <para>
/// This is the "function key hints" half of the status bar. The
/// other half - the segmented display - needs no control: WPF's own
/// <see cref="StatusBar"/> already maps a <see cref="Separator"/> child onto
/// <see cref="StatusBar.SeparatorStyleKey"/>, so the theme re-templates that key
/// to draw the CP437 vertical bar and the composition stays ordinary WPF:
/// </para>
/// <code>
/// &lt;StatusBar&gt;
///   &lt;ctl:StatusBarHint Key="F1" Label="Help"/&gt;
///   &lt;Separator/&gt;
///   &lt;ctl:StatusBarHint Key="F2" Label="Save"/&gt;
/// &lt;/StatusBar&gt;
/// </code>
/// <para>
/// The spacing is fixed by the template: one blank cell, the key, one blank cell,
/// the label, one blank cell, the separator, one blank cell. The
/// single space between key and label is part of the template rather than
/// something the caller types, so hints cannot drift apart from one another.
/// </para>
/// <para>
/// Deliberately presentation-only. Turbo Vision's status line was clickable, but
/// making this a <c>ButtonBase</c> would put behavior in the theme; a host that
/// wants the click should bind the same command to a key gesture and to the hint,
/// which keeps the key binding as the source of truth.
/// </para>
/// </remarks>
public class StatusBarHint : Control
{
    /// <summary>
    /// The key name as displayed - <c>F1</c>, <c>Alt+X</c>, <c>Ctrl+F5</c>. Drawn
    /// in <c>Turboland.Brush.StatusBarAccelerator</c>. This is a caption, not a
    /// <see cref="System.Windows.Input.KeyGesture"/>: forms such as <c>Alt+X</c>
    /// must render exactly as written, and a gesture would round-trip them into
    /// its own canonical spelling.
    /// </summary>
    public static readonly DependencyProperty KeyProperty =
        DependencyProperty.Register(
            nameof(Key),
            typeof(string),
            typeof(StatusBarHint),
            new FrameworkPropertyMetadata(string.Empty));

    /// <summary>What the key does - <c>Help</c>, <c>Save</c>. Drawn in the status bar foreground.</summary>
    public static readonly DependencyProperty LabelProperty =
        DependencyProperty.Register(
            nameof(Label),
            typeof(string),
            typeof(StatusBarHint),
            new FrameworkPropertyMetadata(string.Empty));

    /// <summary>Resource key of the hint style, resolved dynamically like the dialog's.</summary>
    public const string StyleKey = "Turboland.Style.StatusBarHint";

    static StatusBarHint()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(StatusBarHint),
            new FrameworkPropertyMetadata(typeof(StatusBarHint)));
    }

    public StatusBarHint()
    {
        // A named resource reference rather than the implicit style, matching
        // TurbolandWindow/TurbolandDialog: an implicit style matches an element's
        // *exact* runtime type and so would silently miss a subclass.
        SetResourceReference(StyleProperty, StyleKey);
    }

    public string Key
    {
        get => (string)GetValue(KeyProperty);
        set => SetValue(KeyProperty, value);
    }

    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }
}
