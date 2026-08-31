using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using TurbolandTheme.Core;
using ThemeMode = TurbolandTheme.Core.ThemeMode;

namespace TurbolandTheme.Wpf;

/// <summary>
/// Entry point for applying the Turboland theme to a WPF application.
///
/// Usage:
///   protected override void OnStartup(StartupEventArgs e)
///   {
///       base.OnStartup(e);
///       TurbolandTheme.Apply(this, ThemeMode.Authentic);
///       var window = new MainWindow();
///       TurbolandTheme.ApplyTo(window);
///       window.Show();
///   }
///
/// The dictionary is merged into Application.Resources under a known key
/// (so re-applying swaps mode cleanly) and - when the integer scale factor
/// is greater than 1 - a set of metric overrides is layered in front of the
/// base dictionary so every Turboland.Metric.* / Turboland.Size.* /
/// Turboland.Font.Size.* / Turboland.Grid.* resource renders at the scaled value.
/// </summary>
public static class TurbolandTheme
{
    /// <summary>Application.Resources key holding the active theme wrapper.</summary>
    public const string ThemeDictionaryKey = "TurbolandTheme.ActiveDictionary";

    private const string AuthenticUri =
        "pack://application:,,,/TurbolandTheme.Wpf;component/Themes/Turboland.Authentic.xaml";
    private const string AccessibleUri =
        "pack://application:,,,/TurbolandTheme.Wpf;component/Themes/Turboland.Accessible.xaml";

    /// <summary>
    /// Applies the theme to the application. Must be called before windows
    /// are created (window chrome metrics are read at load time).
    /// </summary>
    /// <param name="application">The running application.</param>
    /// <param name="mode">Authentic (1x default) or Accessible (min 2x).</param>
    /// <param name="scaleFactor">
    /// Explicit integer scale factor (1-4). When null it is derived from the
    /// primary monitor's physical DPI (96 -> 1x, 144 -> 2x, 192 -> 2x, ...).
    /// </param>
    public static int Apply(Application application, ThemeMode mode, int? scaleFactor = null)
    {
        Require.NotNull(application);

        // Force a synchronous load of the aggregator (and its merged token +
        // control dictionaries) by probing a key that only resolves once the
        // whole tree is loaded. WPF loads Source dictionaries lazily, so
        // without this MergedDictionaries is empty and the scale overrides
        // below would be built from nothing.
        ResourceDictionary aggregator = Load(
            mode == ThemeMode.Accessible ? AccessibleUri : AuthenticUri,
            "Turboland.Color.Palette.0");

        int scale = ResolveScaleFactor(mode, scaleFactor);
        ResourceDictionary wrapper = new();
        wrapper.MergedDictionaries.Add(aggregator);
        if (scale > 1)
            // Merged AFTER the aggregator so the scaled tokens win over the
            // 1x values in the base metrics/typography dictionaries
            // (WPF resource lookup: last merged dictionary wins).
            wrapper.MergedDictionaries.Add(BuildScaleOverrides(aggregator, scale));

        RemoveExisting(application);
        application.Resources.Add(ThemeDictionaryKey, wrapper);
        application.Resources.MergedDictionaries.Add(wrapper);
        return scale;
    }

    /// <summary>
    /// Creates a ResourceDictionary from a pack URI and forces a synchronous
    /// load by probing a key that only resolves once the dictionary (and its
    /// merged children) are fully loaded.
    /// </summary>
    private static ResourceDictionary Load(string packUri, string probeKey)
    {
        var dict = new ResourceDictionary { Source = new Uri(packUri, UriKind.Absolute) };
        _ = dict[probeKey]; // forces the lazy load
        return dict;
    }

    /// <summary>
    /// Applies the Turboland window style and crisp, integer-pixel text
    /// rendering to a window: Display text formatting, grayscale rendering
    /// (no ClearType fringing) and layout rounding.
    ///
    /// Assigning the style here is required, not cosmetic: WPF matches
    /// implicit styles on an element's *exact* runtime type, so the
    /// theme's implicit <c>TargetType="Window"</c> style never reaches a
    /// derived shell such as <c>MainWindow : Window</c>. Without this call
    /// such a window keeps the default white background and Segoe UI.
    /// </summary>
    /// <param name="window">The window to style.</param>
    /// <param name="isDialog">
    /// True for a light-gray dialog face; false (default) for the blue
    /// IDE desktop shell.
    /// </param>
    public static void ApplyTo(Window window, bool isDialog = false)
    {
        Require.NotNull(window);

        // Don't clobber a style the caller (or an implicit style) already set.
        if (window.Style is null)
            window.SetResourceReference(
                FrameworkElement.StyleProperty,
                isDialog ? DialogWindowStyleKey : WindowShellStyleKey);

        TextOptions.SetTextFormattingMode(window, TextFormattingMode.Display);
        TextOptions.SetTextRenderingMode(window, TextRenderingMode.Grayscale);
        window.UseLayoutRounding = true;
        window.SnapsToDevicePixels = true;
    }

    /// <summary>Resource key for the blue desktop shell window style.</summary>
    public const string WindowShellStyleKey = "Turboland.Style.WindowShell";

    /// <summary>Resource key for the light-gray dialog window style.</summary>
    public const string DialogWindowStyleKey = "Turboland.Style.DialogWindow";

    /// <summary>Resolves the effective integer scale factor.</summary>
    public static int ResolveScaleFactor(ThemeMode mode, int? requested)
    {
        if (requested is int explicitScale)
            return Math.Clamp(explicitScale, 1, 4);

        double physicalDpi = GetSystemDpi();
        int scale = CellMetrics.ScaleFactorForPhysicalDpi(physicalDpi);
        if (mode == ThemeMode.Accessible)
            scale = Math.Max(2, scale);
        return scale;
    }

    private static void RemoveExisting(Application application)
    {
        if (application.Resources[ThemeDictionaryKey] is not ResourceDictionary wrapper)
            return;
        int index = application.Resources.MergedDictionaries.IndexOf(wrapper);
        if (index >= 0)
            application.Resources.MergedDictionaries.RemoveAt(index);
        application.Resources.Remove(ThemeDictionaryKey);
    }

    /// <summary>
    /// Builds the scaled copies of every metric/size/font-size token. Read
    /// from the loaded (frozen) base dictionary so the XAML stays the single
    /// source of 1x values.
    /// </summary>
    /// <remarks>
    /// Key names are gathered from the merged children but every <em>value</em> is
    /// resolved through the aggregator, and assignment uses the indexer rather than
    /// <c>Add</c>. Both matter: a token may legitimately be defined in more than one
    /// child, because a per-mode dictionary overrides a base one. Reading each
    /// child's own value and adding it would raise "Item has already been added" for
    /// any such token, and would scale from the wrong definition. Resolving through
    /// the aggregator instead scales from whichever value WPF would actually hand to
    /// a <c>DynamicResource</c> - last merged wins, the same rule as everywhere else.
    /// </remarks>
    private static ResourceDictionary BuildScaleOverrides(ResourceDictionary aggregator, int scale)
    {
        var overrides = new ResourceDictionary();
        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (ResourceDictionary child in aggregator.MergedDictionaries)
        {
            foreach (var key in child.Keys)
            {
                if (key is string name)
                    names.Add(name);
            }
        }

        foreach (string name in names)
        {
            object? value = aggregator[name];

            if (name.StartsWith("Turboland.Metric.", StringComparison.Ordinal)
                && value is double d)
            {
                overrides[name] = d * scale;
            }
            else if (name.StartsWith("Turboland.Size.", StringComparison.Ordinal)
                     && value is Thickness thickness)
            {
                overrides[name] = new Thickness(
                    thickness.Left * scale,
                    thickness.Top * scale,
                    thickness.Right * scale,
                    thickness.Bottom * scale);
            }
            else if (name.StartsWith("Turboland.Font.Size.", StringComparison.Ordinal)
                     && value is double fontSize)
            {
                overrides[name] = fontSize * scale;
            }
            else if (name.StartsWith("Turboland.Grid.", StringComparison.Ordinal)
                     && value is GridLength grid && grid.IsAbsolute)
            {
                // RowDefinition.Height / ColumnDefinition.Width are GridLength,
                // not double, so fixed grid tracks need their own token type.
                // Star and Auto lengths are already resolution-independent.
                overrides[name] = new GridLength(grid.Value * scale, GridUnitType.Pixel);
            }
        }
        return overrides;
    }

    /// <summary>
    /// System (primary-monitor) physical DPI: 96 at 100%, 144 at 150%,
    /// 192 at 200% on Windows 11. Read directly so the scale can be resolved
    /// before any window exists.
    /// </summary>
    private static double GetSystemDpi() => GetDpiForSystem();

    [DllImport("user32.dll", ExactSpelling = true)]
    private static extern int GetDpiForSystem();

    private static class Require
    {
        public static T NotNull<T>(T value) where T : class
        {
            return value ?? throw new ArgumentNullException();
        }
    }
}
