using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using Xunit;
using Theme = TurbolandTheme.Wpf.TurbolandTheme;
using ThemeMode = TurbolandTheme.Core.ThemeMode;

namespace TurbolandTheme.Tests.VisualRegression;

/// <summary>
/// Asserts what Accessible mode is *for* as arithmetic over the live theme, which is the
/// only form that cannot drift from the dictionaries.
///
/// <para>
/// A pixel diff cannot see contrast: two modes can render byte-identical on a subject
/// that contains no selection yet differ sharply on the brushes Accessible mode
/// overrides, and a color change that improves one pair can quietly worsen another
/// sharing the same surface (recoloring the selection blue lifts the selected label but
/// drops the red accelerator on that bar from 2.49:1 to 1.71:1). Contrast ratios are
/// therefore scored per pair here rather than inferred from a render.
/// </para>
///
/// <para>
/// Accessible mode must reach WCAG AA on every pair the theme renders. Authentic must
/// not, and is not asked to - fidelity is its entire purpose - but its failures are
/// enumerated, so a *new* one cannot appear unnoticed.
/// </para>
/// </summary>
[Collection("Wpf")]
public class ContrastTest
{
    private const double AaNormalText = 4.5;

    private readonly Application _app;

    public ContrastTest(WpfApplicationFixture fixture) => _app = fixture.App;

    /// <summary>
    /// A foreground/background pair the theme actually puts on screen.
    /// </summary>
    /// <param name="What">Human-readable, because a failure names the pair not the key.</param>
    /// <param name="Foreground">Brush key for the ink.</param>
    /// <param name="Background">Brush key for the surface behind it.</param>
    /// <param name="InheritsFrom">
    /// Where the ink comes from when the theme tells accelerators to take the
    /// surrounding text color. Accessible mode sets
    /// <c>Turboland.Flag.AcceleratorInheritColor</c> so the hot key inherits instead of
    /// fixing a hue of its own; that is a real rendering outcome and has to be scored
    /// as one.
    /// </param>
    private sealed record Pair(
        string What,
        string Foreground,
        string Background,
        string? InheritsFrom = null);

    /// <summary>
    /// Every pair the templates produce. Deliberately excluded:
    /// <c>Turboland.Brush.ErrorForeground</c>, which no template references and which has
    /// no defined surface to sit on - scoring it would mean inventing the pair. Its
    /// constraint is recorded in Tokens.Colors.xaml instead.
    /// </summary>
    private static readonly Pair[] Pairs =
    [
        new("desktop text",              "Turboland.Brush.DesktopForeground",        "Turboland.Brush.DesktopBackground"),
        new("dialog/window text",        "Turboland.Brush.WindowForeground",         "Turboland.Brush.WindowBackground"),
        new("menu text",                 "Turboland.Brush.MenuForeground",           "Turboland.Brush.MenuBackground"),
        new("menu text, selected",       "Turboland.Brush.MenuSelectionForeground",  "Turboland.Brush.MenuSelectionBackground"),
        new("menu accelerator",          "Turboland.Brush.MenuAccelerator",          "Turboland.Brush.MenuBackground",          "Turboland.Brush.MenuForeground"),
        new("menu accelerator, selected","Turboland.Brush.MenuAccelerator",          "Turboland.Brush.MenuSelectionBackground", "Turboland.Brush.MenuSelectionForeground"),
        new("status bar text",           "Turboland.Brush.StatusBarForeground",      "Turboland.Brush.StatusBarBackground"),
        // No InheritsFrom: StatusBarHint draws the key as a fixed Run, not an
        // AcceleratorText, so the inherit flag does not reach it. Scoring it as though
        // it did would credit Accessible mode with a fix it never renders.
        new("status bar accelerator",    "Turboland.Brush.StatusBarAccelerator",     "Turboland.Brush.StatusBarBackground"),
        new("field text",                "Turboland.Brush.FieldForeground",          "Turboland.Brush.FieldBackground"),
        new("field text, focused",       "Turboland.Brush.FieldFocusedForeground",   "Turboland.Brush.FieldBackground"),
        new("field text, selected",      "Turboland.Brush.FieldSelectionForeground", "Turboland.Brush.FieldSelectionBackground"),
        new("field accelerator",         "Turboland.Brush.FieldAccelerator",         "Turboland.Brush.FieldBackground",         "Turboland.Brush.FieldForeground"),
        new("button label",              "Turboland.Brush.ButtonForeground",         "Turboland.Brush.ButtonFace"),
        new("button accelerator",        "Turboland.Brush.ButtonAccelerator",        "Turboland.Brush.ButtonFace",              "Turboland.Brush.ButtonForeground"),
        new("button label, disabled",    "Turboland.Brush.DisabledForeground",       "Turboland.Brush.ButtonDisabledFace"),
        new("disabled text on a dialog", "Turboland.Brush.DisabledForeground",       "Turboland.Brush.WindowBackground"),
        // A disabled check box or radio button sits on the field color, not the
        // dialog face - the toggles baseline shows it directly.
        new("disabled text on a field",  "Turboland.Brush.DisabledForeground",       "Turboland.Brush.FieldBackground"),
        new("selected item",             "Turboland.Brush.ControlSelectedForeground","Turboland.Brush.ControlSelected"),
        new("editor text",               "Turboland.Brush.EditorForeground",         "Turboland.Brush.EditorBackground"),
        new("editor keyword",            "Turboland.Brush.EditorKeywordForeground",  "Turboland.Brush.EditorBackground"),
        new("editor selection",          "Turboland.Brush.EditorSelectionForeground","Turboland.Brush.EditorSelectionBackground"),
        // Not rendered by any control yet, but tokenised; scored so Accessible mode is
        // already correct when the editor gutter is built.
        new("editor gutter",             "Turboland.Brush.EditorGutterForeground",   "Turboland.Brush.EditorGutterBackground"),
    ];

    /// <summary>
    /// Authentic mode's known, accepted AA failures - each one an authentic Turboland
    /// color. Listed exhaustively so the assertion is "exactly these", not "at most this
    /// many": a new low-contrast pair is a regression even while the old ones stand.
    /// </summary>
    private static readonly string[] AuthenticKnownFailures =
    [
        "menu accelerator",             // red on light gray      3.34
        "menu accelerator, selected",   // red on green           2.49
        "status bar accelerator",       // red on light gray      3.34
        "field text, focused",          // white on cyan          2.86
        "field text, selected",         // white on green         3.11
        "field accelerator",            // yellow on cyan         2.69
        "button accelerator",           // yellow on green        2.92
        "button label, disabled",       // dark gray on green     2.40
        "disabled text on a dialog",    // dark gray on light gray 3.21
        "disabled text on a field",     // dark gray on cyan      2.60
        "selected item",                // white on green         3.11
        "editor gutter",                // dark gray on black     2.82
    ];

    /// <summary>
    /// The contract: in Accessible mode every rendered text pair reaches WCAG AA.
    /// </summary>
    /// <remarks>
    /// Run at both scales even though color cannot depend on scale - if it ever does,
    /// that is a bug worth a failing test rather than an assumption worth trusting.
    /// </remarks>
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void AccessibleMode_MeetsAaOnEveryRenderedPair(int scale)
    {
        StaThread.Run(() =>
        {
            Theme.Apply(_app, ThemeMode.Accessible, scale);

            List<string> failures = [];
            foreach (Pair pair in Pairs)
            {
                double ratio = Ratio(pair);
                if (ratio < AaNormalText)
                    failures.Add($"{pair.What}: {ratio:0.00}:1");
            }

            Assert.True(
                failures.Count == 0,
                "Accessible mode must reach WCAG AA (4.5:1) on every pair it renders. "
                + $"Failing: {string.Join("; ", failures)}");
        });
    }

    /// <summary>
    /// Authentic mode's deviations are exactly the documented set - no more, no fewer.
    /// </summary>
    /// <remarks>
    /// "No fewer" matters as much as "no more". If a pair silently starts passing, an
    /// authentic color has been changed, and this theme's whole claim is that its colors
    /// are the authentic Turboland palette.
    /// </remarks>
    [Fact]
    public void AuthenticMode_DeviatesOnlyWhereDocumented()
    {
        StaThread.Run(() =>
        {
            Theme.Apply(_app, ThemeMode.Authentic, 1);

            string[] actual = Pairs
                .Where(p => Ratio(p) < AaNormalText)
                .Select(p => p.What)
                .OrderBy(x => x, StringComparer.Ordinal)
                .ToArray();

            string[] expected = AuthenticKnownFailures
                .OrderBy(x => x, StringComparer.Ordinal)
                .ToArray();

            Assert.Equal(expected, actual);
        });
    }

    /// <summary>
    /// Accessible mode may not make any pair worse than Authentic already had it.
    /// </summary>
    /// <remarks>
    /// This guards the failure mode where an override aimed at one pair silently degrades
    /// another. Re-coloring the selection bar blue improves the selected *label* (6.75 ->
    /// 13.29) while taking the red accelerator on that same bar from 2.49 to 1.71; the two
    /// share a surface, so a gain on one is a loss on the other.
    /// </remarks>
    [Fact]
    public void AccessibleMode_NeverLowersContrast()
    {
        StaThread.Run(() =>
        {
            Dictionary<string, double> authentic = [];
            Theme.Apply(_app, ThemeMode.Authentic, 1);
            foreach (Pair pair in Pairs)
                authentic[pair.What] = Ratio(pair);

            Theme.Apply(_app, ThemeMode.Accessible, 1);

            List<string> regressions = [];
            foreach (Pair pair in Pairs)
            {
                double now = Ratio(pair);
                if (now < authentic[pair.What] - 0.005)
                    regressions.Add($"{pair.What}: {authentic[pair.What]:0.00} -> {now:0.00}");
            }

            Assert.True(
                regressions.Count == 0,
                "Accessible mode lowered contrast on: " + string.Join("; ", regressions));
        });
    }

    private double Ratio(Pair pair)
    {
        // An accelerator with InheritsFrom is scored against whatever ink the theme
        // will actually put on screen: its own hue in Authentic, the label's color in
        // Accessible. Reading the flag rather than the brush is what makes this the
        // rendered result and not a guess about it.
        bool inherits =
            pair.InheritsFrom != null
            && _app.Resources["Turboland.Flag.AcceleratorInheritColor"] is true;

        string inkKey = inherits ? pair.InheritsFrom! : pair.Foreground;

        Color ink = Resolve(inkKey)
                    ?? throw new InvalidOperationException(
                        $"'{inkKey}' did not resolve to a brush.");
        Color surface = Resolve(pair.Background)
                        ?? throw new InvalidOperationException(
                            $"'{pair.Background}' did not resolve to a brush.");
        return ContrastRatio(ink, surface);
    }

    private Color? Resolve(string key) =>
        key != null && _app.Resources[key] is SolidColorBrush brush ? brush.Color : null;

    /// <summary>WCAG 2.x relative luminance.</summary>
    private static double Luminance(Color c)
    {
        static double Channel(byte raw)
        {
            double v = raw / 255.0;
            return v <= 0.03928 ? v / 12.92 : Math.Pow((v + 0.055) / 1.055, 2.4);
        }

        return 0.2126 * Channel(c.R) + 0.7152 * Channel(c.G) + 0.0722 * Channel(c.B);
    }

    private static double ContrastRatio(Color a, Color b)
    {
        double la = Luminance(a);
        double lb = Luminance(b);
        (double lighter, double darker) = la >= lb ? (la, lb) : (lb, la);
        return (lighter + 0.05) / (darker + 0.05);
    }
}
