namespace TurbolandTheme.Core;

/// <summary>
/// The two presentation modes of the theme. Both share the same token names;
/// only values differ (authentic = original palette/metrics, accessible =
/// larger metrics, higher contrast, stronger focus indicators).
/// </summary>
public enum ThemeMode
{
    Authentic = 0,
    Accessible = 1
}
