# Modes and scaling

The theme has two independent axes. **Mode** decides which colors are used. **Scale**
decides how large everything is. They do not interact, apart from Accessible mode
enforcing a minimum scale.

```csharp
int scale = TurbolandTheme.Apply(app, ThemeMode.Authentic, scaleFactor: 2);
```

`Apply` returns the scale factor it actually resolved.

## Modes

```csharp
public enum ThemeMode { Authentic, Accessible }
```

### Authentic

The original appearance, reproduced from the standard DOS VGA palette: blue desktop,
light-gray dialogs, green buttons with a hard black shadow, cyan input fields, access
keys marked in red or yellow with no underline.

### Accessible

A **legibility variant, not a layout variant**. Same token names, same 16 colors, no
new hues - only re-mappings, plus flags that move a cue from color to chrome.

Its contract is:

- every text pair the theme renders reaches WCAG AA (4.5:1)
- no meaning is carried by color alone (WCAG 1.4.1)
- focus is unmistakable

What changes:

| Area | Authentic | Accessible | Why |
|---|---|---|---|
| Selection | Green | Blue with white text | White on green is 3.11:1; white on blue is 13.29:1 |
| Access keys | Red / yellow, no underline | Inherit the label color, underlined | All three hues fail AA, and hue alone fails 1.4.1 |
| Status-bar key | Red | Black | Red on light gray is 3.34:1; black is 9.04:1 |
| Focus ring | 1 px white | 2 px yellow | Yellow is never a face color, so a ring cannot blend in |
| Focused field text | White | Black | White on cyan is 2.86:1; black is 7.33:1 |
| Disabled text | Dark gray | Blue | Dark gray on light gray is 3.21:1; blue is 5.72:1 |
| Disabled button | Dimmed label | Light-gray face | Black is the only AA color on green, and it is already the enabled label |

Accessible mode does **not** change any measurement. Larger controls come from the scale
axis. Mixing the two would produce non-integer cells, which is the one thing the
character grid exists to prevent.

The palette is a hard constraint here, and a tight one. Of the 16 colors, the pairs
reaching AA are:

- on Light Gray: Black (9.04), Blue (5.72) - and nothing else
- on Green: Black (6.75) - and nothing else
- on Cyan: Black (7.33), Blue (4.64)

Every override above is forced by that table rather than chosen from preference.

## Scaling

Everything in the theme is a whole number of **9x16 pixel character cells**. The scale
factor multiplies that cell by an integer between 1 and 4.

| Scale | Cell | Font size |
|---|---|---|
| 1x | 9 x 16 | 16 |
| 2x | 18 x 32 | 32 |
| 3x | 27 x 48 | 48 |
| 4x | 36 x 64 | 64 |

Only integers are allowed. A fractional factor would place glyph edges between physical
pixels, and the bitmap-derived font would go soft - exactly the artefact the theme
exists to avoid.

### Automatic resolution

Pass `null` (the default) and the factor is derived from the primary monitor's physical
DPI:

```csharp
TurbolandTheme.Apply(app, ThemeMode.Authentic);   // scale from system DPI
```

96 DPI gives 1x, 144 and 192 give 2x, and so on. Accessible mode floors the result at
2x. Use `ResolveScaleFactor` to ask what a mode would choose without applying anything:

```csharp
int scale = TurbolandTheme.ResolveScaleFactor(ThemeMode.Accessible, requested: null);
```

### How it works

`Apply` reads the loaded 1x dictionary and layers a set of scaled overrides in front of
it. The multiplication is driven entirely by the **token name prefix**:

| Prefix | Type | Scaled |
|---|---|---|
| `Turboland.Metric.*` | `double` | Yes |
| `Turboland.Size.*` | `Thickness` | Yes, component-wise |
| `Turboland.Font.Size.*` | `double` | Yes |
| `Turboland.Grid.*` | `GridLength` | Yes, if absolute |
| `Turboland.Brush.*`, `Turboland.Color.*` | brushes, colors | No |
| `Turboland.Flag.*` | `bool` | No |

Two consequences worth knowing:

- **A new token scales automatically** if you name it with a scaling prefix. Define it
  at its 1x value and the machinery does the rest.
- **Flags are deliberately not named `Turboland.Metric.*`**, so a boolean is never
  multiplied by 2.

Per-mode overrides are written at their **pre-scale** values; scaling is applied after
the mode is resolved.

### Switching at run time

`Apply` removes any previously applied wrapper before adding the new one, so calling it
again is safe:

```csharp
TurbolandTheme.Apply(this, ThemeMode.Accessible, 2);
```

Live elements pick up the change because the templates use `DynamicResource`
throughout.

One limit: an element that is not in a window's visual tree resolves tokens once from
`Application.Resources` but never hears about later changes, because WPF pushes resource
invalidation down from the application's own windows. A disconnected element therefore
keeps its original size across a re-apply. In practice this only affects code that
builds controls without a parent.

## Using the values from code

`TurbolandTheme.Core` exposes the same numbers without any WPF dependency:

```csharp
var metrics = CellMetrics.ForScale(2);
double w = metrics.CellWidth;    // 18
double h = metrics.CellHeight;   // 32

var type = TypographyTokens.ForScale(2);
double size = type.PrimaryFontSize;   // 32
```

Useful for sizing a window to an exact `80 x 25` grid, or for any layout maths that
happens before a visual tree exists.
