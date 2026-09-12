# Turboland Theme documentation

Start here if you are new: **[Getting started](getting-started.md)**.

## Guides

**[Getting started](getting-started.md)**
Requirements, referencing the library, the two startup calls, and your first themed
window.

**[Modes and scaling](modes-and-scaling.md)**
`Authentic` vs `Accessible`, what each mode changes and why, integer scale factors, DPI
resolution, and how the scaling machinery decides what to multiply.

**[Design tokens](design-tokens.md)**
Reference for every color, metric, size, grid length, font and flag token, plus how to
override one.

**[Controls](controls.md)**
The stock controls that are themed automatically, and the additions the theme provides:
`TurbolandWindow`, `TurbolandDialog` / `TurbolandDialogHost`, `TurbolandFloatingDialog`,
`StatusBarHint`, `CellSize`, `AcceleratorText`, `GlyphFill`, `DialogGeometry`.

**[Layout on the character grid](character-grid.md)**
The two layout rules that matter: size in cells, and reserve space for chrome that
paints outside the layout box.

**[Troubleshooting](troubleshooting.md)**
Symptoms, causes and fixes for the traps that are easy to hit and hard to diagnose.

## The short version

```csharp
TurbolandTheme.Apply(this, TurbolandTheme.Core.ThemeMode.Authentic);
var window = new MainWindow();
TurbolandTheme.ApplyTo(window);
window.Show();
```

- Reference `Turboland.*` resources with **`DynamicResource`**, never `StaticResource`.
- Size things with **`ctl:CellSize.Rows` / `.Columns`**, never pixel literals.
- Give any **clipping container's content** a `Turboland.Size.ChromeInset` margin.
- Call **`ApplyTo`** on every window that is not a `TurbolandWindow`.

## Learning by example

Two runnable applications ship with the repository, and both accept
`[authentic|accessible] [1-4]`:

```powershell
dotnet run --project src/TurbolandTheme.Gallery -- accessible 2
dotnet run --project src/SampleApp
```

`TurbolandTheme.Gallery` puts every themed control on one screen - the quickest way to see
what a mode or scale factor actually does. `SampleApp` is a working Turboland style
IDE shell and is the better reference for real integration: a `TurbolandWindow` root, a
menu, a status bar, an editor surface and in-client dialogs.


![sample](./screenshots/gallery.png)

![sample](./screenshots/sample_app.png)
