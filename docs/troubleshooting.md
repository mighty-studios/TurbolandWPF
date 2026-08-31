# Troubleshooting

## The window is white with Segoe UI, but the controls inside it are themed

You skipped `TurbolandTheme.ApplyTo(window)`.

WPF matches implicit styles on an element's **exact** runtime type. The theme's
`TargetType="Window"` style therefore never reaches `MainWindow : Window`. Controls
inside are unaffected because they *are* the exact types the theme targets.

```csharp
var window = new MainWindow();
TurbolandTheme.ApplyTo(window);
window.Show();
```

`ApplyTo` skips assignment if you already set a `Style`, so it will not clobber your own.
Windows rooted on `ctl:TurbolandWindow` do not need the call - that control assigns its own
style in its constructor.

## Nothing is themed at all

`Apply` was never called, or was called after the window was constructed. It must run
first - window chrome metrics are read at load time.

```csharp
protected override void OnStartup(StartupEventArgs e)
{
    base.OnStartup(e);
    TurbolandTheme.Apply(this, TurbolandTheme.Core.ThemeMode.Authentic);   // before any window
    ...
}
```

## `ThemeMode` does not compile, or resolves to the wrong type

Your `App` derives from `Application`, which has a nested
`System.Windows.Application.ThemeMode`. Member lookup beats `using` directives, so an
unqualified `ThemeMode` inside `App` finds that one.

Fully qualify it:

```csharp
TurbolandTheme.Core.ThemeMode.Authentic
```

## Everything is 1x on a high-DPI monitor

Check that the app is per-monitor DPI aware, then either let the theme resolve the
factor from system DPI (pass `null`) or set it explicitly:

```csharp
TurbolandTheme.Apply(this, ThemeMode.Authentic, scaleFactor: 2);
```

## Sizes did not change after switching scale

Two likely causes.

**`StaticResource`.** The scaled tokens are layered in *after* the base dictionary
loads. A static reference captures the 1x value and never updates. Use
`DynamicResource` for every `Turboland.*` reference.

**A detached element.** An element not in a window's visual tree resolves tokens once
from `Application.Resources`, but never hears about later changes - WPF pushes resource
invalidation down from the application's own windows. Attach the element, or re-create
it after re-applying.

## A custom token of mine is not scaling

Scaling is driven entirely by the token **name prefix** and value type:

| Prefix | Required type |
|---|---|
| `Turboland.Metric.*` | `double` |
| `Turboland.Size.*` | `Thickness` |
| `Turboland.Font.Size.*` | `double` |
| `Turboland.Grid.*` | absolute `GridLength` |

A `Turboland.Metric.*` key holding a `Thickness` will not scale. Define the value at 1x -
the multiplication happens on top.

## My override is being ignored

Merge your dictionary **after** `Apply`, since the last merged dictionary wins. Also
re-merge after any later `Apply` call: it rebuilds its own wrapper from scratch.

If you are trying to *remove* a value, `x:Null` will not work. WPF cannot distinguish a
null entry from a missing one, so lookup falls through to the earlier dictionary and the
original value wins. Set a real value, or use a flag token.

## A focus ring is missing one edge

The control sits flush against a clipping ancestor - usually a `ScrollViewer`. Focus
rings are painted outside the layout box, so the overhang is being cut away.

Add the inset to the container's **content**:

```xml
<ScrollViewer>
    <Grid Margin="{DynamicResource Turboland.Size.ChromeInset}">
```

Not to the container's `Padding` - the stock `ScrollViewer` template applies `Padding`
to the `ScrollContentPresenter`, which moves the clip along with the content.

See [Layout on the character grid](character-grid.md).

## A list box always slices its last row

Its height is not a whole number of rows. Replace the pixel literal:

```xml
<ListBox ctl:CellSize.Rows="8"/>
```

## Clicking below the last line of a multi-line `TextBox` does nothing

Set `VerticalContentAlignment="Stretch"`.

The theme's `TextBox` template template-binds the content host's `VerticalAlignment` to
that property. WPF's single-line default of `Center` stops the host filling the box, so
clicks below the text never reach it.

```xml
<TextBox AcceptsReturn="True" VerticalContentAlignment="Stretch"/>
```

## Text looks blurry or has color fringing

`ApplyTo` sets `TextFormattingMode.Display`, `TextRenderingMode.Grayscale` and
`UseLayoutRounding` on the window. If you build a visual tree outside a themed window -
a popup with its own root, say - set them yourself.

Also confirm the scale factor is an integer. It is clamped to 1-4, but a custom
`LayoutTransform` or `ScaleTransform` in your own markup can reintroduce a fractional
scale.

## Some characters render in the wrong font

The bundled face covers code page 437. Anything outside it falls back to
`Turboland.Font.Fallback` (Consolas, Courier New, monospace) - expected behavior for
Unicode content, localisation and assistive technology.

## An access key stopped working

If you replaced a templated control's content presenter with your own `TextBlock`, the
access key is gone: WPF registers access keys from string content, and arbitrary inlines
register nothing.

Use `AcceleratorText`, which re-registers the key against the owning control via
`AccessKeyManager`, or apply `Turboland.Selector.Accelerator` as your
`ContentTemplateSelector`.

## A dialog opened off-screen or cannot be recovered

It cannot. `TurbolandDialogHost` clamps every dialog so at least four cells of its title
bar stay inside the host. If a dialog appears missing, it is probably shaded
(double-click toggles) or behind another - call `BringToFront`.

## A dialog does not block, and code after `Show` runs immediately

By design. Host modality is soft: input to the content beneath is swallowed, but the
main window stays readable and `Show` does not block. Use the `Closed` event or check
`DialogResult`.

## Snap Layouts does not appear over the zoom box

Snap Layouts requires a hit-test returning `HTMAXBUTTON`, which `TurbolandWindow` provides.
If you re-templated the window, keep the zoom box named `PART_ZoomBox` - the hit-test
locates it by that name.

Note that a button reported as non-client receives **no** WPF mouse events, which is why
its hover state is painted by hand. A custom template must do the same or the button
will look inert.

## Maximising covers the taskbar

That is the stock `WindowChrome` behavior a plain `Window` shows. `TurbolandWindow`
clamps to the work area via `WM_GETMINMAXINFO`. Use `TurbolandWindow` as your root, or
reuse `FrameGeometry.MaximisedPlacement` in your own window procedure.

## Still stuck

The `TurbolandTheme.Gallery` project renders every themed control on one screen and takes
`[authentic|accessible] [1-4]` on the command line - the fastest way to compare your
result against a known-good reference:

```powershell
dotnet run --project src/TurbolandTheme.Gallery -- accessible 2
```
