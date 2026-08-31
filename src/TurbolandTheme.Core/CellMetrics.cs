using System;

namespace TurbolandTheme.Core;

/// <summary>
/// Character-cell metrics. All lengths in the theme are expressed as multiples of
/// the base cell (9x16 at 96 DPI) and scaled by integer factors only (1x, 2x, ...),
/// so the grid never lands on fractional pixels on per-monitor-DPI displays.
/// </summary>
/// <remarks>
/// <b>The cell is not square.</b> 9 px is the VGA text-mode character clock and the
/// bundled Px437 font's native advance; 16 px is the scan-line height. A single
/// "one cell" constant therefore cannot serve as both a width and a height, which is
/// why the shadow offset and the scroll-bar metrics come in horizontal/vertical pairs.
/// </remarks>
public sealed class CellMetrics
{
    /// <summary>Base cell width in device-independent pixels at 96 DPI.</summary>
    public const int BaseCellWidth = 9;

    /// <summary>Base cell height in device-independent pixels at 96 DPI.</summary>
    public const int BaseCellHeight = 16;

    /// <summary>Physical DPI that maps to 1x scaling.</summary>
    public const double BaseDpi = 96.0;

    public CellMetrics(int scaleFactor)
    {
        if (scaleFactor < 1)
            throw new ArgumentOutOfRangeException(nameof(scaleFactor), scaleFactor, "Scale factor must be >= 1.");
        ScaleFactor = Math.Min(scaleFactor, 4);
    }

    /// <summary>Integer scale factor (1, 2, 3 or 4).</summary>
    public int ScaleFactor { get; }

    public double CellWidth => BaseCellWidth * ScaleFactor;
    public double CellHeight => BaseCellHeight * ScaleFactor;

    /// <summary>One "line" of frame/border, in cell-pixel units.</summary>
    public double BorderThickness => ScaleFactor;

    /// <summary>Dialog double-line border (two cell-pixel lines).</summary>
    public double DialogBorderThickness => 2 * ScaleFactor;

    /// <summary>Padding inside dialogs and fields (one cell).</summary>
    public double DialogPadding => (double)BaseCellWidth * ScaleFactor;

    /// <summary>
    /// Hard shadow offset: <b>two cells right, one row down</b>.
    /// </summary>
    /// <remarks>
    /// The shadow is the dialog rectangle translated by this offset and painted behind
    /// it, which is why only an L-shape along the right and bottom edges is visible.
    /// <para>
    /// The horizontal and vertical offsets are genuinely different distances - 18 and
    /// 16 at 1x - because the cell is not square. Do not collapse them into one value.
    /// </para>
    /// </remarks>
    public double ShadowOffsetX => 2 * (double)BaseCellWidth * ScaleFactor;
    public double ShadowOffsetY => CellHeight;

    /// <summary>Menu bar height (one cell row).</summary>
    public double MenuHeight => CellHeight;

    /// <summary>Status bar height (one cell row).</summary>
    public double StatusBarHeight => CellHeight;

    /// <summary>
    /// Title bar height (one cell row).
    /// </summary>
    /// <remarks>
    /// The title is drawn <em>on</em> the top frame line rather than in a bar above it,
    /// so it occupies exactly one cell row like the menu and status bars. Any other
    /// value would push the rest of the frame off the character grid.
    /// </remarks>
    public double TitleBarHeight => CellHeight;

    /// <summary>Window content padding around the client area (one cell).</summary>
    public double WindowPadding => DialogPadding;

    /// <summary>Turboland geometry is never rounded.</summary>
    public double CornerRadius => 0;

    public static CellMetrics ForScale(int scaleFactor) => new(scaleFactor);

    /// <summary>
    /// Derives the integer scale factor from a physical DPI value (per-monitor DPI
    /// aware hosts pass the monitor's DPI). 96 -> 1x, 120 -> 1x, 144 -> 2x,
    /// 192 -> 2x, 288 -> 3x, 384 -> 4x.
    /// </summary>
    public static int ScaleFactorForPhysicalDpi(double physicalDpi)
    {
        if (physicalDpi <= 0) return 1;
        int factor = (int)Math.Round(physicalDpi / BaseDpi, MidpointRounding.AwayFromZero);
        return Math.Max(1, Math.Min(4, factor));
    }
}
