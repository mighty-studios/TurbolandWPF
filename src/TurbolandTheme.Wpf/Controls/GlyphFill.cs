using System.Globalization;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace TurbolandTheme.Wpf.Controls;

/// <summary>The axis a <see cref="GlyphFill"/> repeats along.</summary>
public enum GlyphFillOrientation
{
    Horizontal,
    Vertical,
}

/// <summary>
/// Fills its own bounds by repeating a single CP437 glyph, so window frames are drawn
/// from the same box-drawing characters the original IDEs used ("═║╔╗╚╝" active,
/// "─│┌┐└┘" inactive) rather than from approximated rectangles.
/// </summary>
/// <remarks>
/// <para>
/// A <see cref="System.Windows.Controls.TextBlock"/> cannot do this: the repeat count
/// depends on the final arranged width, which is not known until after layout, and a
/// fixed-length string either falls short or has to be clipped from an arbitrary
/// overshoot.
/// </para>
/// <para>
/// The repeat count is derived from the glyph's <em>measured advance</em> rather than from
/// <c>Turboland.Metric.CellWidth</c>. The two agree - both are 9px at 1x, because the base
/// cell is the font's native advance - but measuring stays the correct implementation:
/// the authority for how far apart glyphs tile is the glyph itself, and any drift between
/// the token and the font would otherwise show up here as a seam every character.
/// </para>
/// </remarks>
public class GlyphFill : FrameworkElement
{
    public static readonly DependencyProperty GlyphProperty =
        DependencyProperty.Register(
            nameof(Glyph), typeof(string), typeof(GlyphFill),
            new FrameworkPropertyMetadata(
                "\u2550",
                FrameworkPropertyMetadataOptions.AffectsMeasure |
                FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty OrientationProperty =
        DependencyProperty.Register(
            nameof(Orientation), typeof(GlyphFillOrientation), typeof(GlyphFill),
            new FrameworkPropertyMetadata(
                GlyphFillOrientation.Horizontal,
                FrameworkPropertyMetadataOptions.AffectsMeasure |
                FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty ForegroundProperty =
        TextElement.ForegroundProperty.AddOwner(
            typeof(GlyphFill),
            new FrameworkPropertyMetadata(
                Brushes.Black,
                FrameworkPropertyMetadataOptions.Inherits |
                FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty FontFamilyProperty =
        TextElement.FontFamilyProperty.AddOwner(
            typeof(GlyphFill),
            new FrameworkPropertyMetadata(
                SystemFonts.MessageFontFamily,
                FrameworkPropertyMetadataOptions.Inherits |
                FrameworkPropertyMetadataOptions.AffectsMeasure |
                FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty FontSizeProperty =
        TextElement.FontSizeProperty.AddOwner(
            typeof(GlyphFill),
            new FrameworkPropertyMetadata(
                SystemFonts.MessageFontSize,
                FrameworkPropertyMetadataOptions.Inherits |
                FrameworkPropertyMetadataOptions.AffectsMeasure |
                FrameworkPropertyMetadataOptions.AffectsRender));

    /// <summary>The character to repeat.</summary>
    public string Glyph
    {
        get => (string)GetValue(GlyphProperty);
        set => SetValue(GlyphProperty, value);
    }

    /// <summary>The axis to repeat along.</summary>
    public GlyphFillOrientation Orientation
    {
        get => (GlyphFillOrientation)GetValue(OrientationProperty);
        set => SetValue(OrientationProperty, value);
    }

    public Brush Foreground
    {
        get => (Brush)GetValue(ForegroundProperty);
        set => SetValue(ForegroundProperty, value);
    }

    public FontFamily FontFamily
    {
        get => (FontFamily)GetValue(FontFamilyProperty);
        set => SetValue(FontFamilyProperty, value);
    }

    public double FontSize
    {
        get => (double)GetValue(FontSizeProperty);
        set => SetValue(FontSizeProperty, value);
    }

    public GlyphFill()
    {
        // Frame runs are decoration; letting them swallow clicks would break the
        // caption drag region they sit inside.
        IsHitTestVisible = false;
        ClipToBounds = true;
    }

    /// <summary>A single cell, so an Auto row or column collapses to exactly one glyph.</summary>
    protected override Size MeasureOverride(Size availableSize)
    {
        FormattedText one = Format(Glyph);
        return new Size(one.WidthIncludingTrailingWhitespace, one.Height);
    }

    protected override void OnRender(DrawingContext dc)
    {
        string glyph = Glyph;
        if (string.IsNullOrEmpty(glyph)) return;

        Size size = RenderSize;
        if (size.Width <= 0 || size.Height <= 0) return;

        FormattedText one = Format(glyph);
        double advance = one.WidthIncludingTrailingWhitespace;
        double lineHeight = one.Height;
        if (advance <= 0 || lineHeight <= 0) return;

        if (Orientation == GlyphFillOrientation.Horizontal)
        {
            // One FormattedText for the whole run: the text stack then places every
            // glyph on the font's own advance, which is what keeps the joins seamless.
            int count = (int)Math.Ceiling(size.Width / advance);
            FormattedText run = Format(string.Concat(Enumerable.Repeat(glyph, count)));
            dc.DrawText(run, new Point(0, (size.Height - lineHeight) / 2));
        }
        else
        {
            int count = (int)Math.Ceiling(size.Height / lineHeight);
            double x = (size.Width - advance) / 2;
            for (int i = 0; i < count; i++)
                dc.DrawText(one, new Point(x, i * lineHeight));
        }
    }

    private FormattedText Format(string text) => new(
        text,
        CultureInfo.CurrentUICulture,
        FlowDirection.LeftToRight,
        new Typeface(FontFamily, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal),
        FontSize,
        Foreground,
        numberSubstitution: null,
        // Display + the element's own DPI is what keeps the stems on whole pixels;
        // Ideal mode reintroduces the fractional positioning the theme exists to avoid.
        TextFormattingMode.Display,
        VisualTreeHelper.GetDpi(this).PixelsPerDip);
}
