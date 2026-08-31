using System.IO;
using System.Text.RegularExpressions;
using Xunit;

namespace TurbolandTheme.Tests.VisualRegression;

/// <summary>
/// Every size in the theme must come from a scalable token
/// (Turboland.Metric.* / Turboland.Size.* / Turboland.Grid.* / Turboland.Font.Size.*),
/// because TurbolandTheme.Apply only rewrites those prefixes. A hardcoded pixel
/// literal silently stays at 1x while the fonts around it double, producing clipped
/// rows, truncated button labels and detached scroll bars at 2x.
///
/// Zero is allowed: 0 * scale == 0, so it is scale-invariant.
/// </summary>
public class NoHardcodedMetricsTest
{
    /// <summary>Attributes whose value is a device-independent pixel measure.</summary>
    private static readonly Regex SizeAttribute = new(
        "(?<!\\w)(Width|Height|MinWidth|MinHeight|MaxWidth|MaxHeight|Margin|Padding"
        + "|BorderThickness|StrokeThickness|CornerRadius|FontSize|RadiusX|RadiusY)"
        + "\\s*=\\s*\"(?<v>[^\"]*)\"",
        RegexOptions.Compiled);

    /// <summary>Setters carry the same measures indirectly: Property=".." Value="..".</summary>
    private static readonly Regex SizeSetter = new(
        "Property\\s*=\\s*\"(?<p>Width|Height|MinWidth|MinHeight|MaxWidth|MaxHeight|Margin"
        + "|Padding|BorderThickness|StrokeThickness|CornerRadius|FontSize)\""
        + "\\s+Value\\s*=\\s*\"(?<v>[^\"]*)\"",
        RegexOptions.Compiled);

    /// <summary>Geometry cannot scale from a resource at all, so it is banned outright.</summary>
    private static readonly Regex PathData = new(
        "<Path\\b[^>]*?\\bData\\s*=\\s*\"[^\"]+\"",
        RegexOptions.Compiled | RegexOptions.Singleline);

    [Fact]
    public void ThemeXaml_HasNoUnscalablePixelLiterals()
    {
        var offenders = new List<string>();

        foreach (string file in ThemeFiles())
        {
            string[] lines = File.ReadAllLines(file);
            string name = Path.GetFileName(file);

            for (int i = 0; i < lines.Length; i++)
            {
                foreach (Match m in SizeAttribute.Matches(lines[i]))
                    if (IsUnscalableLiteral(m.Groups["v"].Value))
                        offenders.Add($"{name}:{i + 1}  {m.Value.Trim()}");

                foreach (Match m in SizeSetter.Matches(lines[i]))
                    if (IsUnscalableLiteral(m.Groups["v"].Value))
                        offenders.Add($"{name}:{i + 1}  {m.Value.Trim()}");
            }
        }

        Assert.True(offenders.Count == 0,
            "Hardcoded pixel literals do not scale. Replace each with a "
            + "{DynamicResource Turboland.Metric.* / Turboland.Size.* / Turboland.Grid.*} token:\n  "
            + string.Join("\n  ", offenders));
    }

    [Fact]
    public void ThemeXaml_UsesGlyphsNotPathGeometry()
    {
        var offenders = new List<string>();

        foreach (string file in ThemeFiles())
        {
            string text = File.ReadAllText(file);
            string name = Path.GetFileName(file);

            foreach (Match m in PathData.Matches(text))
            {
                int line = text.Take(m.Index).Count(c => c == '\n') + 1;
                offenders.Add($"{name}:{line}");
            }
        }

        Assert.True(offenders.Count == 0,
            "Path geometry cannot be scaled by a resource, and the bundled font "
            + "carries the full CP437 symbol set. Use a TextBlock glyph instead:\n  "
            + string.Join("\n  ", offenders));
    }

    /// <summary>
    /// True when the value is a numeric measure that is not entirely zero.
    /// Resource references, Auto/*, and all-zero values are fine.
    /// </summary>
    private static bool IsUnscalableLiteral(string value)
    {
        value = value.Trim();

        if (value.Length == 0 || value.StartsWith('{'))
            return false;

        string[] parts = value.Split(',', StringSplitOptions.RemoveEmptyEntries);

        foreach (string part in parts)
        {
            if (!double.TryParse(part.Trim(),
                    System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out double d))
                return false;   // "Auto", "*", a color, an enum - not a literal measure.

            if (d != 0)
                return true;
        }

        return false;   // Every component was zero, which scales correctly.
    }

    /// <summary>
    /// Locates src\TurbolandTheme.Wpf\Themes by walking up from the test binary,
    /// so the test works from bin\Debug\net9.0-windows without copying the XAML.
    /// </summary>
    private static IEnumerable<string> ThemeFiles()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);

        while (dir is not null)
        {
            string candidate = Path.Combine(dir.FullName, "src", "TurbolandTheme.Wpf", "Themes");
            if (Directory.Exists(candidate))
                return Directory.GetFiles(candidate, "*.xaml");

            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException(
            "Could not locate src\\TurbolandTheme.Wpf\\Themes above " + AppContext.BaseDirectory);
    }
}
