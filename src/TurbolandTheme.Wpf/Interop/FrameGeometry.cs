namespace TurbolandTheme.Wpf.Interop;

/// <summary>An integer, device-pixel rectangle, matching the Win32 <c>RECT</c> convention.</summary>
public readonly record struct PixelRect(int Left, int Top, int Right, int Bottom)
{
    public int Width => Right - Left;
    public int Height => Bottom - Top;

    public bool Contains(int x, int y) => x >= Left && x < Right && y >= Top && y < Bottom;

    public override string ToString() => $"({Left},{Top})-({Right},{Bottom}) {Width}x{Height}";
}

/// <summary>
/// The window-frame arithmetic, kept free of P/Invoke so it can be unit tested.
/// </summary>
/// <remarks>
/// A <c>WindowStyle=None</c> plus <c>WindowChrome</c> window does not get these two
/// calculations right on its own. They are the corrections that must never regress,
/// which is why they live here rather than inline in a window procedure.
/// </remarks>
public static class FrameGeometry
{
    // Win32 hit-test codes, repeated here so tests need no interop.
    public const int HitNowhere = 0;
    public const int HitLeft = 10;
    public const int HitRight = 11;
    public const int HitTop = 12;
    public const int HitTopLeft = 13;
    public const int HitTopRight = 14;
    public const int HitBottom = 15;
    public const int HitBottomLeft = 16;
    public const int HitBottomRight = 17;
    public const int HitClient = 1;
    public const int HitCaption = 2;

    /// <summary>
    /// What the resize band must report instead of a resize code once the window is
    /// maximised.
    /// </summary>
    /// <remarks>
    /// A maximised window cannot be resized, yet simply declining the hit-test is not
    /// enough to suppress the band: WindowChrome answers it instead and returns real
    /// resize codes, so all four edges sprout resize cursors that do nothing. The band
    /// has to be claimed and neutralised.
    /// <para>
    /// The top strip stays caption rather than client so that dragging a maximised
    /// window downwards still restores and moves it, which is what every other Windows
    /// application does.
    /// </para>
    /// </remarks>
    public static int MaximisedBandCode(PixelRect bounds, int y, int captionHeight)
        => y < bounds.Top + captionHeight ? HitCaption : HitClient;

    /// <summary>
    /// The placement a maximised window must be given so it fills the work area
    /// instead of overflowing onto the taskbar.
    /// </summary>
    /// <remarks>
    /// Without this correction a <c>WindowStyle=None</c> plus <c>WindowChrome</c>
    /// window maximises to the full monitor rectangle inflated by the invisible
    /// resize margin, so it overshoots the work area on every side and covers the
    /// taskbar.
    /// <para>
    /// The returned offset is <b>relative to the monitor origin</b>, not the desktop
    /// origin, because that is what <c>MINMAXINFO.ptMaxPosition</c> expects. Getting
    /// this wrong is invisible on a primary monitor at (0,0) and wrong everywhere else.
    /// </para>
    /// </remarks>
    public static (int OffsetX, int OffsetY, int Width, int Height) MaximisedPlacement(
        PixelRect monitor, PixelRect work)
        => (work.Left - monitor.Left, work.Top - monitor.Top, work.Width, work.Height);

    /// <summary>
    /// The resize hit-test code for a point in screen pixels, or <see cref="HitNowhere"/>
    /// when the point is not in the resize band.
    /// </summary>
    /// <remarks>
    /// This must be answered <b>before</b> anything else in the window procedure.
    /// <c>WindowChrome.IsHitTestVisibleInChrome</c> outranks WindowChrome's own resize
    /// hit-testing, so a caption control lying within the band silently eats that
    /// resize handle - and the close box sits exactly in the top-left corner.
    /// </remarks>
    public static int ResizeHitCode(PixelRect window, int x, int y, int band)
    {
        if (band <= 0 || !window.Contains(x, y)) return HitNowhere;

        bool left = x < window.Left + band;
        bool right = x >= window.Right - band;
        bool top = y < window.Top + band;
        bool bottom = y >= window.Bottom - band;

        if (top && left) return HitTopLeft;
        if (top && right) return HitTopRight;
        if (bottom && left) return HitBottomLeft;
        if (bottom && right) return HitBottomRight;
        if (left) return HitLeft;
        if (right) return HitRight;
        if (top) return HitTop;
        if (bottom) return HitBottom;
        return HitNowhere;
    }
}
