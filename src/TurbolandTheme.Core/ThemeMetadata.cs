namespace TurbolandTheme.Core;

/// <summary>Identity metadata for the theme package.</summary>
public static class ThemeMetadata
{
    public const string Name = "TurbolandTheme";
    public const string Version = "0.1.0";
    public const string Description =
        "WPF theme emulating the classic DOS IDE aesthetic. " +
        "VGA 16-color palette, 437 bitmap typography, character-cell metrics.";

    public static bool IsSupported(ThemeMode mode) =>
        mode == ThemeMode.Authentic || mode == ThemeMode.Accessible;
}
