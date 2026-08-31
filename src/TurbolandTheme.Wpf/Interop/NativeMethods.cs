using System.Runtime.InteropServices;

namespace TurbolandTheme.Wpf.Interop;

/// <summary>
/// The minimum Win32 surface <see cref="Controls.TurbolandWindow"/> needs.
/// </summary>
/// <remarks>
/// Deliberately small. Everything here exists to service a specific frame
/// correction; nothing is included speculatively.
/// </remarks>
internal static class NativeMethods
{
    public const int WM_NCHITTEST = 0x0084;
    public const int WM_NCLBUTTONDOWN = 0x00A1;
    public const int WM_NCLBUTTONUP = 0x00A2;
    public const int WM_NCMOUSEMOVE = 0x00A0;
    public const int WM_NCMOUSELEAVE = 0x02A2;
    public const int WM_GETMINMAXINFO = 0x0024;

    public const int HTMAXBUTTON = 9;
    public const int HTCLIENT = 1;
    public const int HTCAPTION = 2;

    public const int MONITOR_DEFAULTTONEAREST = 0x00000002;

    public const uint TME_LEAVE = 0x00000002;
    public const uint TME_NONCLIENT = 0x00000010;

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left, Top, Right, Bottom;

        public readonly PixelRect ToPixelRect() => new(Left, Top, Right, Bottom);
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int X, Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MINMAXINFO
    {
        public POINT ptReserved;
        public POINT ptMaxSize;
        public POINT ptMaxPosition;
        public POINT ptMinTrackSize;
        public POINT ptMaxTrackSize;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MONITORINFO
    {
        public int cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct TRACKMOUSEEVENT
    {
        public uint cbSize;
        public uint dwFlags;
        public IntPtr hwndTrack;
        public uint dwHoverTime;
    }

    [DllImport("user32.dll")]
    public static extern IntPtr MonitorFromWindow(IntPtr hwnd, int dwFlags);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool TrackMouseEvent(ref TRACKMOUSEEVENT lpEventTrack);

    /// <summary>Extracts the signed screen coordinates Windows packs into an LPARAM.</summary>
    /// <remarks>
    /// The cast through <see cref="short"/> is required, not decorative: on a
    /// multi-monitor desktop a coordinate to the left of, or above, the primary
    /// monitor is negative, and masking without sign extension turns it into a
    /// large positive number.
    /// </remarks>
    public static (int X, int Y) GetPoint(IntPtr lParam)
    {
        int packed = (int)(lParam.ToInt64() & 0xFFFFFFFF);
        return ((short)(packed & 0xFFFF), (short)((packed >> 16) & 0xFFFF));
    }
}
