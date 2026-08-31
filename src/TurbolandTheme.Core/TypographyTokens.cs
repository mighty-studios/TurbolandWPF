using System;

namespace TurbolandTheme.Core;

/// <summary>
/// Typography tokens. The primary face is the Px437 IBM VGA 9x16 bitmap-derived
/// outline font; fallbacks cover Unicode/localization/accessibility.
/// </summary>
public sealed class TypographyTokens
{
    /// <summary>Family name of the bundled DOS 437 bitmap font.</summary>
    public const string PrimaryFontFamily = "Px437 IBM VGA 9x16";

    /// <summary>
    /// Fallback list used for characters outside the 437 set and for accessibility.
    /// Order matters: first match wins.
    /// </summary>
    public const string FallbackFontFamily = "Consolas, 'Courier New', monospace";

    /// <summary>
    /// Line height of the primary face, in em units: exactly <b>1.0 em</b>.
    /// </summary>
    /// <remarks>
    /// This is what WPF actually uses to lay out a line, and it is what keeps the font
    /// size an integer. Deriving a value from the font's hhea ascender and descender
    /// instead gives 0.95, which yields a fractional font size and a 9.47 px advance -
    /// enough to put every glyph off the pixel grid and defeat crisp rendering.
    /// </remarks>
    public const double FontLineHeightEm = 1.0;

    /// <summary>
    /// Horizontal advance of the primary face in em units: 900/1600 upm.
    /// At 16 dip this is exactly 9 px, the font's native cell width.
    /// </summary>
    public const double FontAdvanceEm = 900.0 / 1600.0;

    private TypographyTokens(double primaryFontSize)
    {
        PrimaryFontSize = primaryFontSize;
    }

    /// <summary>
    /// Font size (device-independent pixels) whose line height equals one cell
    /// at the given scale factor. At 1x this is exactly 16, which yields the
    /// font's native 9x16 pixel cell.
    /// </summary>
    public double PrimaryFontSize { get; }

    /// <summary>
    /// Natural horizontal advance, in dip, of one character at the current size:
    /// 9 px at 1x, matching <see cref="CellMetrics.BaseCellWidth"/>.
    /// </summary>
    /// <remarks>
    /// Either this or the constant may be used to size a block cursor or a
    /// character-grid layout. Prefer this one: it is derived from the font metrics
    /// rather than declared alongside them, so it cannot drift.
    /// </remarks>
    public double AdvanceWidth => PrimaryFontSize * FontAdvanceEm;

    /// <summary>Dialog text uses the primary face at the primary size.</summary>
    public double DialogFontSize => PrimaryFontSize;

    /// <summary>Status bar text uses the primary face at the primary size.</summary>
    public double StatusBarFontSize => PrimaryFontSize;

    public static TypographyTokens ForScale(int scaleFactor) =>
        new(CellMetrics.BaseCellHeight * scaleFactor / FontLineHeightEm);

    public static TypographyTokens For(ThemeMode mode) =>
        // Accessible mode never renders below 2x; TurbolandTheme.Apply resolves the
        // effective scale the same way.
        mode == ThemeMode.Accessible
            ? ForScale(2)
            : ForScale(1);
}
