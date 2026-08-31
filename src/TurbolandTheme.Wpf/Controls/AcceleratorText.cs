using System;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace TurbolandTheme.Wpf.Controls;

/// <summary>
/// A <see cref="TextBlock"/> that renders a caption containing a WPF access-key
/// marker ("_F") with the marked character drawn in a distinct color, the way
/// Turbo Vision drew hot keys.
/// </summary>
/// <remarks>
/// <para>
/// WPF's built-in <see cref="AccessText"/> can only <em>underline</em> the access
/// key - it holds the whole caption in one unsplit run and re-renders from its own
/// internal state, so its color cannot be overridden per character. Turbo Vision
/// marked hot keys with <em>color alone</em> and drew no underline at all, so a
/// replacement is required rather than a tweak.
/// </para>
/// <para>
/// <b>Replacing <see cref="AccessText"/> destroys the access key.</b> WPF registers
/// access keys from string content; once the content is arbitrary inlines nothing is
/// registered and Alt+F stops working. This class therefore re-registers the key
/// itself against the owning control via <see cref="AccessKeyManager"/>, which
/// resolves the correct scope automatically - the window for a button, the
/// <see cref="Menu"/> for a menu item.
/// </para>
/// </remarks>
public class AcceleratorText : TextBlock
{
    /// <summary>
    /// The caption, including a WPF access-key marker: a single underscore escapes
    /// the following character, and a doubled underscore is a literal underscore.
    /// </summary>
    public static readonly DependencyProperty CaptionProperty =
        DependencyProperty.Register(
            nameof(Caption),
            typeof(string),
            typeof(AcceleratorText),
            new FrameworkPropertyMetadata(string.Empty, OnCaptionChanged));

    /// <summary>
    /// Brush for the access-key character. Inheritable, so a control style can set
    /// it once on the control (red for menus, yellow for buttons and fields) and
    /// every <see cref="AcceleratorText"/> in that control's template picks it up -
    /// including through a <see cref="DataTemplate"/>. State triggers can override
    /// it (for example to dim it when the control is disabled).
    /// </summary>
    public static readonly DependencyProperty AcceleratorBrushProperty =
        DependencyProperty.RegisterAttached(
            "AcceleratorBrush",
            typeof(Brush),
            typeof(AcceleratorText),
            new FrameworkPropertyMetadata(
                null,
                FrameworkPropertyMetadataOptions.Inherits
                | FrameworkPropertyMetadataOptions.AffectsRender,
                OnAcceleratorAppearanceChanged));

    /// <summary>
    /// Whether to underline the access key in addition to coloring it. False in
    /// Authentic mode, which marks the key by color alone. Accessible mode turns it
    /// on, because color alone is not a sufficient distinction (WCAG 1.4.1).
    /// </summary>
    public static readonly DependencyProperty ShowUnderlineProperty =
        DependencyProperty.RegisterAttached(
            "ShowUnderline",
            typeof(bool),
            typeof(AcceleratorText),
            new FrameworkPropertyMetadata(
                false,
                FrameworkPropertyMetadataOptions.Inherits
                | FrameworkPropertyMetadataOptions.AffectsRender,
                OnAcceleratorAppearanceChanged));

    /// <summary>
    /// Whether the access key should ignore <see cref="AcceleratorBrushProperty"/> and
    /// simply inherit the surrounding text color.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Authentic mode marks the hot key with hue alone. All three of its accelerator
    /// colors fail WCAG AA against their own backgrounds (red on light gray 3.34:1,
    /// yellow on green 2.92:1, yellow on cyan 2.69:1), and a fixed hue cannot be
    /// rescued by picking a better one, because the character has to stay legible on
    /// the selection bar as well as on the resting surface - two different backgrounds,
    /// one color.
    /// </para>
    /// <para>
    /// Inheriting solves that structurally: the hot key is then exactly as legible as
    /// the label it sits inside, in every state, with no second table of numbers to
    /// maintain. Accessible mode switches this on and turns the underline on to carry
    /// the signal instead.
    /// </para>
    /// <para>
    /// This is a flag rather than an <c>x:Null</c> accelerator brush because WPF's
    /// merged-dictionary lookup cannot express "null": a null entry is indistinguishable
    /// from a missing one, so the search continues into the earlier dictionary and the
    /// authentic color wins anyway.
    /// </para>
    /// </remarks>
    public static readonly DependencyProperty InheritTextColorProperty =
        DependencyProperty.RegisterAttached(
            "InheritTextColor",
            typeof(bool),
            typeof(AcceleratorText),
            new FrameworkPropertyMetadata(
                false,
                FrameworkPropertyMetadataOptions.Inherits
                | FrameworkPropertyMetadataOptions.AffectsRender,
                OnAcceleratorAppearanceChanged));

    private string? _registeredKey;
    private IInputElement? _registeredOwner;

    public AcceleratorText()
    {
        Loaded += (_, _) => RegisterAccessKey();
        Unloaded += (_, _) => UnregisterAccessKey();
    }

    public string Caption
    {
        get => (string)GetValue(CaptionProperty);
        set => SetValue(CaptionProperty, value);
    }

    public Brush AcceleratorBrush
    {
        get => (Brush)GetValue(AcceleratorBrushProperty);
        set => SetValue(AcceleratorBrushProperty, value);
    }

    public bool ShowUnderline
    {
        get => (bool)GetValue(ShowUnderlineProperty);
        set => SetValue(ShowUnderlineProperty, value);
    }

    public bool InheritTextColor
    {
        get => (bool)GetValue(InheritTextColorProperty);
        set => SetValue(InheritTextColorProperty, value);
    }

    public static bool GetInheritTextColor(DependencyObject o) =>
        (bool)o.GetValue(InheritTextColorProperty);

    public static void SetInheritTextColor(DependencyObject o, bool value) =>
        o.SetValue(InheritTextColorProperty, value);

    public static Brush GetAcceleratorBrush(DependencyObject o) =>
        (Brush)o.GetValue(AcceleratorBrushProperty);

    public static void SetAcceleratorBrush(DependencyObject o, Brush value) =>
        o.SetValue(AcceleratorBrushProperty, value);

    public static bool GetShowUnderline(DependencyObject o) =>
        (bool)o.GetValue(ShowUnderlineProperty);

    public static void SetShowUnderline(DependencyObject o, bool value) =>
        o.SetValue(ShowUnderlineProperty, value);

    /// <summary>
    /// Splits a caption on its access-key marker. Returns false when there is no
    /// marker, in which case <paramref name="plain"/> holds the unescaped text.
    /// </summary>
    /// <remarks>
    /// Public because anyone authoring a custom template needs the same parse to
    /// stay consistent with WPF's own access-key rules.
    /// </remarks>
    public static bool TryParse(string? caption, out string before, out string key, out string after, out string plain)
    {
        before = key = after = plain = string.Empty;
        if (caption is null)
            return false;

        var sb = new StringBuilder();
        int markerAt = -1;

        for (int i = 0; i < caption.Length; i++)
        {
            char c = caption[i];
            if (c != '_')
            {
                sb.Append(c);
                continue;
            }

            // "__" is a literal underscore and never marks an access key.
            if (i + 1 < caption.Length && caption[i + 1] == '_')
            {
                sb.Append('_');
                i++;
                continue;
            }

            // Only the first single underscore marks the access key; a trailing
            // underscore with nothing after it is dropped, matching AccessText.
            if (markerAt < 0 && i + 1 < caption.Length)
            {
                markerAt = sb.Length;
                sb.Append(caption[i + 1]);
                i++;
            }
        }

        plain = sb.ToString();
        if (markerAt < 0)
            return false;

        before = plain.Substring(0, markerAt);
        key = plain.Substring(markerAt, 1);
        after = plain.Substring(markerAt + 1);
        return true;
    }

    private static void OnCaptionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var self = (AcceleratorText)d;
        self.Rebuild();
        self.RegisterAccessKey();
    }

    private static void OnAcceleratorAppearanceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is AcceleratorText self)
            self.Rebuild();
    }

    private void Rebuild()
    {
        Inlines.Clear();

        if (!TryParse(Caption, out string before, out string key, out string after, out string plain))
        {
            // No access key: a single plain run, so the caption still renders.
            if (!string.IsNullOrEmpty(plain))
                Inlines.Add(new Run(plain));
            return;
        }

        if (before.Length > 0)
            Inlines.Add(new Run(before));

        var accel = new Run(key);

        // Leave Foreground unset when no brush is supplied, or when the theme
        // asks the hot key to take the surrounding text color, so the run keeps
        // inheriting the control's foreground rather than going transparent.
        var brush = InheritTextColor ? null : AcceleratorBrush;
        if (brush != null)
            accel.Foreground = brush;

        if (ShowUnderline)
            accel.TextDecorations = System.Windows.TextDecorations.Underline;

        Inlines.Add(accel);

        if (after.Length > 0)
            Inlines.Add(new Run(after));
    }

    /// <summary>
    /// Finds the control that should respond to the access key. Inside a
    /// <see cref="DataTemplate"/> the immediate TemplatedParent is the
    /// <see cref="ContentPresenter"/>, so hop through it to the templated control;
    /// otherwise walk the visual tree.
    /// </summary>
    private IInputElement? FindOwner()
    {
        if (TemplatedParent is ContentPresenter presenter && presenter.TemplatedParent is Control hosted)
            return hosted;

        if (TemplatedParent is Control direct)
            return direct;

        DependencyObject? node = this;
        while (node != null)
        {
            node = VisualTreeHelper.GetParent(node);
            if (node is Control control)
                return control;
        }

        return null;
    }

    private void RegisterAccessKey()
    {
        if (!IsLoaded)
            return;

        TryParse(Caption, out _, out string key, out _, out _);
        var owner = FindOwner();

        if (string.Equals(key, _registeredKey, StringComparison.OrdinalIgnoreCase)
            && ReferenceEquals(owner, _registeredOwner))
        {
            return;
        }

        UnregisterAccessKey();

        if (string.IsNullOrEmpty(key) || owner == null)
            return;

        AccessKeyManager.Register(key, owner);
        _registeredKey = key;
        _registeredOwner = owner;
    }

    private void UnregisterAccessKey()
    {
        if (_registeredKey == null || _registeredOwner == null)
            return;

        AccessKeyManager.Unregister(_registeredKey, _registeredOwner);
        _registeredKey = null;
        _registeredOwner = null;
    }
}
