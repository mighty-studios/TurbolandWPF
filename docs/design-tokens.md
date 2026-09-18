# Design tokens

Nothing in the theme is a literal. Every color, measurement and font is a named
resource, so your application can use the same values the templates do and stay
consistent at every mode and scale.

Reference them with `DynamicResource`, never `StaticResource` - the theme rewrites the
scaled tokens after load, and a static reference would freeze at the 1x value:

```xml
<Border Background="{DynamicResource Turboland.Brush.DesktopBackground}"
        Padding="{DynamicResource Turboland.Size.FieldPadding}"/>
```

From code:

```csharp
if (TryFindResource("Turboland.Metric.CellHeight") is double cellHeight)
    text.Margin = new Thickness(0, 0, 0, cellHeight);
```

## Colors

All colors come from the 16-color VGA palette, available directly as
`Turboland.Color.Palette.0` through `Turboland.Color.Palette.15`.

| Index | Name | RGB |
|---|---|---|
| 0 | Black | 0, 0, 0 |
| 1 | Blue | 0, 0, 170 |
| 2 | Green | 0, 170, 0 |
| 3 | Cyan | 0, 170, 170 |
| 4 | Red | 170, 0, 0 |
| 5 | Magenta | 170, 0, 170 |
| 6 | Brown | 170, 85, 0 |
| 7 | Light Gray | 170, 170, 170 |
| 8 | Dark Gray | 85, 85, 85 |
| 9 | Light Blue | 85, 85, 255 |
| 10 | Light Green | 85, 255, 85 |
| 11 | Light Cyan | 85, 255, 255 |
| 12 | Light Red | 255, 85, 85 |
| 13 | Pink | 255, 85, 255 |
| 14 | Yellow | 255, 255, 85 |
| 15 | White | 255, 255, 255 |

Prefer the **semantic** brushes below over a raw palette index. They are what the
Accessible mode re-maps, so an application built on them adapts for free.

### Surfaces

| Token | Authentic |
|---|---|
| `Turboland.Brush.DesktopBackground` | Blue |
| `Turboland.Brush.DesktopForeground` | Light Gray |
| `Turboland.Brush.WindowBackground` | Light Gray |
| `Turboland.Brush.WindowForeground` | Black |
| `Turboland.Brush.ControlFace` | Light Gray |
| `Turboland.Brush.ControlShadow` | Black |
| `Turboland.Brush.ControlSelected` | Green |
| `Turboland.Brush.ControlSelectedForeground` | White |
| `Turboland.Brush.Shadow` | Black |

### Window frame

| Token | Authentic |
|---|---|
| `Turboland.Brush.FrameActive` | White |
| `Turboland.Brush.FrameInactive` | Light Gray |
| `Turboland.Brush.FrameCloseMark` | Light Green |

Active and inactive frames differ by **line weight**, not color: the theme switches
between double-line and single-line box glyphs.

### Menus and status bar

| Token | Authentic |
|---|---|
| `Turboland.Brush.MenuBackground` | Light Gray |
| `Turboland.Brush.MenuForeground` | Black |
| `Turboland.Brush.MenuSelectionBackground` | Green |
| `Turboland.Brush.MenuSelectionForeground` | Black |
| `Turboland.Brush.MenuAccelerator` | Red |
| `Turboland.Brush.StatusBarBackground` | Light Gray |
| `Turboland.Brush.StatusBarForeground` | Black |
| `Turboland.Brush.StatusBarAccelerator` | Red |
| `Turboland.Brush.StatusBarSeparator` | Black |

### Input fields

| Token | Authentic |
|---|---|
| `Turboland.Brush.FieldBackground` | Cyan |
| `Turboland.Brush.FieldForeground` | Black |
| `Turboland.Brush.FieldFocusedForeground` | White |
| `Turboland.Brush.FieldSelectionBackground` | Green |
| `Turboland.Brush.FieldSelectionForeground` | White |
| `Turboland.Brush.FieldAccelerator` | Yellow |

### Buttons

| Token | Authentic |
|---|---|
| `Turboland.Brush.ButtonFace` | Green |
| `Turboland.Brush.ButtonForeground` | Black |
| `Turboland.Brush.ButtonAccelerator` | Yellow |
| `Turboland.Brush.ButtonShadow` | Black |
| `Turboland.Brush.ButtonDisabledFace` | Green |

### Scroll bar

A Borland Vision scroll bar is painted from exactly two colors: a solid base for the
arrow buttons and the thumb, and a second color dithered into the track as a one-pixel
checkerboard. The original IDEs picked the pair per window - blue and cyan on the editor
and debugger - so these are tokens an application is expected to override, at
application scope or in any narrower resource scope:

| Token | Authentic |
|---|---|
| `Turboland.Brush.ScrollTrackBase` | Light Gray |
| `Turboland.Brush.ScrollTrackStipple` | Black |

The dither tile is a fixed pixel texture: it tiles and clips to the track rather than
scaling with the metric factor, which is what keeps the VGA look at every scale.

### Editor

For text-editing surfaces that want the IDE's own colors rather than a dialog field:

`Turboland.Brush.EditorBackground`, `EditorForeground`, `EditorKeywordForeground`,
`EditorSelectionBackground`, `EditorSelectionForeground`, `EditorGutterBackground`,
`EditorGutterForeground`.

### State

| Token | Authentic |
|---|---|
| `Turboland.Brush.FocusOutline` | White |
| `Turboland.Brush.DisabledForeground` | Dark Gray |
| `Turboland.Brush.ErrorForeground` | Light Red |

## Metrics

`Turboland.Metric.*` tokens are `double` and are multiplied by the scale factor. Values
shown are 1x.

| Token | 1x | Meaning |
|---|---|---|
| `Turboland.Metric.CellWidth` | 9 | Base character cell width |
| `Turboland.Metric.CellHeight` | 16 | Base character cell height |
| `Turboland.Metric.BorderThickness` | 1 | Standard control border |
| `Turboland.Metric.DialogBorderThickness` | 2 | Double-line dialog frame |
| `Turboland.Metric.FocusBorder` | 1 | Focus ring thickness (2 in Accessible) |
| `Turboland.Metric.CornerRadius` | 0 | Always zero - nothing is rounded |
| `Turboland.Metric.ButtonShadow` | 2 | Button shadow depth |
| `Turboland.Metric.ButtonMinWidth` | 72 | 8 cells |
| `Turboland.Metric.DialogPadding` | 9 | One cell |
| `Turboland.Metric.WindowPadding` | 9 | One cell |
| `Turboland.Metric.ShadowOffsetX` | 18 | Two cells right |
| `Turboland.Metric.ShadowOffsetY` | 16 | One row down |
| `Turboland.Metric.MenuHeight` | 16 | One row |
| `Turboland.Metric.StatusBarHeight` | 16 | One row |
| `Turboland.Metric.TitleBarHeight` | 16 | One row |
| `Turboland.Metric.DialogMinVisible` | 36 | 4 cells that must stay reachable |
| `Turboland.Metric.ScrollBarWidth` | 9 | One cell |
| `Turboland.Metric.ScrollBarHeight` | 16 | One row |
| `Turboland.Metric.ComboButtonWidth` | 18 | Two cells |
| `Turboland.Metric.PopupMaxHeight` | 240 | 15 rows |
| `Turboland.Metric.SeparatorThickness` | 1 | |

## Sizes

`Turboland.Size.*` tokens are `Thickness` and scale component-wise. The ones an
application is most likely to want:

| Token | 1x | Use |
|---|---|---|
| `Turboland.Size.SectionMargin` | `0,0,0,16` | One blank row between groups of controls |
| `Turboland.Size.ColumnGutter` | `0,0,18,0` | Two cells between columns |
| `Turboland.Size.FieldPadding` | `9,0` | Inside an input field |
| `Turboland.Size.ButtonPadding` | `9,0` | Inside a button |
| `Turboland.Size.DesktopInset` | `9,16,27,32` | Framing the desktop surface |
| `Turboland.Size.ChromeInset` | `1,1,9,7` | Space a clipping container must reserve |
| `Turboland.Size.FocusBorder` | `1` | Focus ring thickness |
| `Turboland.Size.FocusRingOffset` | `-1` | Equal and opposite to the thickness |
| `Turboland.Size.FloatingDialogFaceMargin` | `0,0,18,16` | Shadow room reserved by a floating dialog's face |
| `Turboland.Size.FloatingDialogShadowMargin` | `18,16,0,0` | The same vector pushing the shadow out to meet it |

`ChromeInset` is asymmetric on purpose. See
[Layout on the character grid](character-grid.md).

The remaining `Turboland.Size.*` tokens are template internals - shadow offsets, tab
offsets, group-box header straddle, menu margins. They are public so a custom template
can match, but an application rarely needs them.

## Grid lengths

`RowDefinition.Height` and `ColumnDefinition.Width` are `GridLength`, not `double`, so
fixed tracks need their own token type: `Turboland.Grid.ScrollBarButtonWidth`,
`ScrollBarButtonHeight`, `MenuIconColumn`, `TreeIndent`. Star and Auto lengths are
already resolution-independent and need no token.

## Typography

| Token | Value |
|---|---|
| `Turboland.Font.Primary` | Px437 IBM VGA 9x16, embedded |
| `Turboland.Font.Fallback` | Consolas, Courier New, monospace |
| `Turboland.Font.Size.Primary` | 16 at 1x |
| `Turboland.Font.Size.Dialog` | 16 at 1x |
| `Turboland.Font.Size.StatusBar` | 16 at 1x |

The size is chosen so the face's 1.0em line height lands exactly on the 16 px cell,
which yields the font's native 9x16 pixel cell with no fractional advance.

WPF never uses a font's embedded bitmap strikes - a "bitmap font" is always
outline-rasterised - so crispness comes from the rendering settings, not the file.
`ApplyTo` sets them for you: `TextFormattingMode.Display`, `TextRenderingMode.Grayscale`
(no ClearType color fringing) and `UseLayoutRounding`.

The fallback family covers characters outside code page 437, for Unicode content,
localisation and assistive technology.

## Flags

Booleans that switch presentation without changing a measurement.

| Token | Authentic | Accessible |
|---|---|---|
| `Turboland.Flag.AcceleratorUnderline` | `False` | `True` |
| `Turboland.Flag.AcceleratorInheritColor` | `False` | `True` |

These are deliberately **not** named `Turboland.Metric.*`, because that prefix is what the
scaling machinery multiplies, and a boolean must never be scaled.

## Overriding a token

Merge your own dictionary after `Apply`. Last merged wins:

```csharp
TurbolandTheme.Apply(this, ThemeMode.Authentic);

Resources.MergedDictionaries.Add(new ResourceDictionary
{
    Source = new Uri("MyOverrides.xaml", UriKind.Relative)
});
```

Write overrides at their **1x** value if the token has a scaling prefix, and re-merge
after any later `Apply` call, which rebuilds its own wrapper.

Note that `x:Null` cannot express an override. WPF cannot distinguish a null dictionary
entry from a missing one, so the lookup falls through to the earlier value and the
override silently does nothing. That is why the accelerator behavior is controlled by
flags rather than by nulling a brush.

## Core constants without WPF

`TurbolandTheme.Core` targets `netstandard2.0` and exposes the same values as plain types:
`VgaPalette`, `VgaColor`, `ColorTokens`, `CellMetrics`, `TypographyTokens`,
`ThemeMode` and `ThemeMetadata`.
