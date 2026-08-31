using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shell;
using TurbolandTheme.Wpf.Interop;

namespace TurbolandTheme.Wpf.Controls;

/// <summary>
/// A top-level window that draws the Turbo Vision frame - box-drawing border, title on
/// the top frame line, close box at the left, zoom box at the right - inside its own
/// client area, while keeping every native Windows behavior.
/// </summary>
/// <remarks>
/// <para>
/// The visuals come from <c>Turboland.Style.TurbolandWindow</c>. The behavior comes from
/// this class, which applies four corrections that <c>WindowChrome</c> alone does
/// <em>not</em> get right:
/// </para>
/// <list type="number">
/// <item>maximise overflows the taskbar without a <c>WM_GETMINMAXINFO</c> clamp;</item>
/// <item>caption controls silently eat resize handles unless the resize band is
/// answered first;</item>
/// <item>Snap Layouts only appears for a hit-test that returns <c>HTMAXBUTTON</c>, which
/// costs that button its WPF mouse events;</item>
/// <item>and a non-client button therefore has no hover feedback unless it is drawn
/// by hand, which leaves it reading as inert text.</item>
/// </list>
/// <para>
/// Active and inactive frames differ by <em>line weight</em>, not color: every frame is
/// white, and the two states switch between the double-line and single-line box glyphs.
/// <see cref="Window.IsActive"/> drives that directly.
/// </para>
/// </remarks>
public class TurbolandWindow : Window
{
    /// <summary>Resource key for the window's style.</summary>
    public const string StyleKey = "Turboland.Style.TurbolandWindow";

    /// <summary>The name the template must give the zoom box for hit-testing to find it.</summary>
    public const string ZoomBoxPartName = "PART_ZoomBox";

    private static readonly DependencyPropertyKey IsZoomBoxHighlightedPropertyKey =
        DependencyProperty.RegisterReadOnly(
            nameof(IsZoomBoxHighlighted), typeof(bool), typeof(TurbolandWindow),
            new PropertyMetadata(false));

    /// <summary>
    /// True while the pointer is over the zoom box.
    /// </summary>
    /// <remarks>
    /// Maintained from <c>WM_NCMOUSEMOVE</c> because the zoom box is non-client: it
    /// receives no WPF mouse events at all, so <c>IsMouseOver</c> is permanently false
    /// and an ordinary trigger would never fire.
    /// </remarks>
    public static readonly DependencyProperty IsZoomBoxHighlightedProperty =
        IsZoomBoxHighlightedPropertyKey.DependencyProperty;

    public bool IsZoomBoxHighlighted => (bool)GetValue(IsZoomBoxHighlightedProperty);

    private FrameworkElement? _zoomBox;
    private bool _trackingNonClientMouse;

    public TurbolandWindow()
    {
        // Match ApplyTo's convention: a resource reference, so a switch between
        // Authentic and Accessible re-resolves without touching the window.
        SetResourceReference(StyleProperty, StyleKey);

        CommandBindings.Add(new CommandBinding(
            SystemCommands.CloseWindowCommand, (_, _) => SystemCommands.CloseWindow(this)));
        CommandBindings.Add(new CommandBinding(
            SystemCommands.MinimizeWindowCommand, (_, _) => SystemCommands.MinimizeWindow(this)));
        CommandBindings.Add(new CommandBinding(
            SystemCommands.MaximizeWindowCommand, (_, _) => SystemCommands.MaximizeWindow(this)));
        CommandBindings.Add(new CommandBinding(
            SystemCommands.RestoreWindowCommand, (_, _) => SystemCommands.RestoreWindow(this)));
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _zoomBox = GetTemplateChild(ZoomBoxPartName) as FrameworkElement;
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        if (PresentationSource.FromVisual(this) is HwndSource source)
            source.AddHook(WndProc);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        switch (msg)
        {
            case NativeMethods.WM_GETMINMAXINFO:
                ClampToWorkArea(hwnd, lParam);
                break;

            case NativeMethods.WM_NCHITTEST:
                return HitTest(hwnd, lParam, ref handled);

            case NativeMethods.WM_NCLBUTTONDOWN when (int)wParam == NativeMethods.HTMAXBUTTON:
                // Swallowed so Windows does not start its own caption-button loop;
                // the zoom happens on button-up, as it does for a real caption button.
                handled = true;
                break;

            case NativeMethods.WM_NCLBUTTONUP when (int)wParam == NativeMethods.HTMAXBUTTON:
                ToggleZoom();
                handled = true;
                break;

            case NativeMethods.WM_NCMOUSEMOVE when (int)wParam == NativeMethods.HTMAXBUTTON:
                SetZoomHighlight(hwnd, true);
                break;

            case NativeMethods.WM_NCMOUSEMOVE:
            case NativeMethods.WM_NCMOUSELEAVE:
                SetZoomHighlight(hwnd, false);
                break;
        }

        return IntPtr.Zero;
    }

    /// <summary>Keeps a maximised window inside the work area.</summary>
    private static void ClampToWorkArea(IntPtr hwnd, IntPtr lParam)
    {
        IntPtr monitor = NativeMethods.MonitorFromWindow(hwnd, NativeMethods.MONITOR_DEFAULTTONEAREST);
        if (monitor == IntPtr.Zero) return;

        var info = new NativeMethods.MONITORINFO { cbSize = Marshal.SizeOf<NativeMethods.MONITORINFO>() };
        if (!NativeMethods.GetMonitorInfo(monitor, ref info)) return;

        var mmi = Marshal.PtrToStructure<NativeMethods.MINMAXINFO>(lParam);
        (int offsetX, int offsetY, int width, int height) = FrameGeometry.MaximisedPlacement(
            info.rcMonitor.ToPixelRect(), info.rcWork.ToPixelRect());

        mmi.ptMaxPosition.X = offsetX;
        mmi.ptMaxPosition.Y = offsetY;
        mmi.ptMaxSize.X = width;
        mmi.ptMaxSize.Y = height;

        Marshal.StructureToPtr(mmi, lParam, fDeleteOld: true);
    }

    /// <summary>
    /// Answers the resize band first, then the zoom box, so caption controls cannot
    /// steal a resize handle.
    /// </summary>
    private IntPtr HitTest(IntPtr hwnd, IntPtr lParam, ref bool handled)
    {
        (int x, int y) = NativeMethods.GetPoint(lParam);

        if (NativeMethods.GetWindowRect(hwnd, out NativeMethods.RECT rect))
        {
            PixelRect bounds = rect.ToPixelRect();
            int code = FrameGeometry.ResizeHitCode(bounds, x, y, ResizeBandPixels());

            if (code != FrameGeometry.HitNowhere)
            {
                handled = true;

                // A maximised window cannot be resized, but declining the message is
                // not enough to stop the band: WindowChrome then answers it itself and
                // returns HTTOPLEFT/HTTOP/HTLEFT/HTBOTTOM, so all four edges show
                // resize cursors that do nothing. The band has to be claimed and
                // neutralised. The top strip stays caption so dragging a maximised
                // window down to restore it keeps working.
                if (WindowState == WindowState.Maximized)
                    return new IntPtr(FrameGeometry.MaximisedBandCode(
                        bounds, y, CaptionHeightPixels()));

                return new IntPtr(code);
            }
        }

        if (IsOverZoomBox(x, y))
        {
            handled = true;
            return new IntPtr(NativeMethods.HTMAXBUTTON);
        }

        return IntPtr.Zero;
    }

    private int ResizeBandPixels() => ToDevicePixels(
        WindowChrome.GetWindowChrome(this)?.ResizeBorderThickness.Left
        ?? SystemParameters.ResizeFrameVerticalBorderWidth);

    private int CaptionHeightPixels() => ToDevicePixels(
        WindowChrome.GetWindowChrome(this)?.CaptionHeight
        ?? SystemParameters.CaptionHeight);

    /// <summary>
    /// Converts a WindowChrome measurement to the device pixels a hit-test uses.
    /// </summary>
    /// <remarks>
    /// WindowChrome works in device-independent units while WM_NCHITTEST reports
    /// physical screen coordinates. Skipping this conversion leaves the band correct
    /// at 96 DPI and progressively too narrow on every higher-DPI monitor.
    /// </remarks>
    private int ToDevicePixels(double deviceIndependent)
        => (int)Math.Round(deviceIndependent * VisualTreeHelper.GetDpi(this).DpiScaleX);

    private bool IsOverZoomBox(int screenX, int screenY)
    {
        if (_zoomBox is null || !_zoomBox.IsVisible) return false;

        Point topLeft = _zoomBox.PointToScreen(new Point(0, 0));
        Point bottomRight = _zoomBox.PointToScreen(
            new Point(_zoomBox.ActualWidth, _zoomBox.ActualHeight));

        return screenX >= topLeft.X && screenX < bottomRight.X
            && screenY >= topLeft.Y && screenY < bottomRight.Y;
    }

    private void ToggleZoom() => WindowState =
        WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

    /// <summary>Hover feedback for a button WPF cannot see, because the hit-test
    /// reports it as non-client.</summary>
    private void SetZoomHighlight(IntPtr hwnd, bool highlighted)
    {
        if (highlighted && !_trackingNonClientMouse)
        {
            var track = new NativeMethods.TRACKMOUSEEVENT
            {
                cbSize = (uint)Marshal.SizeOf<NativeMethods.TRACKMOUSEEVENT>(),
                dwFlags = NativeMethods.TME_LEAVE | NativeMethods.TME_NONCLIENT,
                hwndTrack = hwnd,
                dwHoverTime = 0,
            };
            _trackingNonClientMouse = NativeMethods.TrackMouseEvent(ref track);
        }
        else if (!highlighted)
        {
            _trackingNonClientMouse = false;
        }

        SetValue(IsZoomBoxHighlightedPropertyKey, highlighted);
    }
}
