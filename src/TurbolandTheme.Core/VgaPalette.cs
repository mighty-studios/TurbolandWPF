using System;
using System.Collections.Generic;

namespace TurbolandTheme.Core;

/// <summary>
/// The standard DOS VGA 16-color palette. These values are fixed by the VGA hardware;
/// they are the theme's entire color vocabulary and must not be invented or tuned.
/// </summary>
public static class VgaPalette
{
    public static readonly VgaColor Black = new(0x00, 0x00, 0x00);
    public static readonly VgaColor Blue = new(0x00, 0x00, 0xAA);
    public static readonly VgaColor Green = new(0x00, 0xAA, 0x00);
    public static readonly VgaColor Cyan = new(0x00, 0xAA, 0xAA);
    public static readonly VgaColor Red = new(0xAA, 0x00, 0x00);
    public static readonly VgaColor Magenta = new(0xAA, 0x00, 0xAA);
    public static readonly VgaColor Brown = new(0xAA, 0x55, 0x00);
    public static readonly VgaColor LightGray = new(0xAA, 0xAA, 0xAA);
    public static readonly VgaColor DarkGray = new(0x55, 0x55, 0x55);
    public static readonly VgaColor LightBlue = new(0x55, 0x55, 0xFF);
    public static readonly VgaColor LightGreen = new(0x55, 0xFF, 0x55);
    public static readonly VgaColor LightCyan = new(0x55, 0xFF, 0xFF);
    public static readonly VgaColor LightRed = new(0xFF, 0x55, 0x55);
    public static readonly VgaColor Pink = new(0xFF, 0x55, 0xFF);
    public static readonly VgaColor Yellow = new(0xFF, 0xFF, 0x55);
    public static readonly VgaColor White = new(0xFF, 0xFF, 0xFF);

    /// <summary>All 16 VGA colors in palette order (indices 0-15).</summary>
    public static readonly IReadOnlyList<VgaColor> All = new[]
    {
        Black, Blue, Green, Cyan, Red, Magenta, Brown, LightGray,
        DarkGray, LightBlue, LightGreen, LightCyan, LightRed, Pink, Yellow, White
    };

    public static VgaColor ByIndex(int index) =>
        index >= 0 && index < All.Count
            ? All[index]
            : throw new ArgumentOutOfRangeException(nameof(index), index, "VGA palette indices are 0-15.");
}
