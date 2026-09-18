# Turboland Theme for WPF

A reusable WPF theme that gives a modern .NET desktop application the look of the
classic DOS-era IDEs - VGA palette, box-drawing frames, hard
shadows, and a strict character grid - without giving up standard Windows behavior.

The theme re-templates stock WPF controls. It does not replace them.

![sample](./docs/screenshots/sample_app.png)

![sample](./docs/screenshots/gallery.png)

## Features

- **Complete control coverage** - Button, CheckBox, RadioButton, TextBox, PasswordBox,
  Menu, ListBox, ComboBox, TreeView, TabControl, GroupBox, ScrollBar, StatusBar and
  Window are all themed by implicit styles.
- **Two modes** - `Authentic` reproduces the original palette exactly. `Accessible`
  keeps the same 16 colors but re-maps them so every rendered text pair meets
  WCAG AA, and no cue is carried by color alone.
- **Integer scaling** - every measurement is a whole number of 9x16 character cells,
  scaled by 1x-4x. No fractional pixels, no blurry text.
- **Bundled bitmap-style font** - Px437 IBM VGA 9x16, with grayscale rendering and
  layout rounding applied for you.
- **Native window behavior** - the frame is drawn in the client area, but resize,
  maximise, Snap Layouts and the taskbar all still work.
- **Two dialog shapes** - `TurbolandDialog` floats inside the main window like a real
  text-mode dialog, and `TurbolandFloatingDialog` is a true owned window with emulated drop shadow.
- **No hardcoded values** - the theme is driven entirely by named design tokens you
  can read, reuse or override.

## Quick start

Reference `TurbolandTheme.Wpf`, then apply the theme at startup:

```csharp
using System.Windows;
using Theme = TurbolandTheme.Wpf.TurbolandTheme;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        Theme.Apply(this, TurbolandTheme.Core.ThemeMode.Authentic);

        var window = new MainWindow();
        Theme.ApplyTo(window);   // required: implicit styles do not reach a derived Window
        window.Show();
    }
}
```

That is the whole integration. Every stock control in `MainWindow` is now themed.

See **[docs/getting-started.md](docs/getting-started.md)** for the next steps.

## Documentation

| Guide | Covers |
|---|---|
| [Getting started](docs/getting-started.md) | Installing, applying the theme, your first window |
| [Modes and scaling](docs/modes-and-scaling.md) | Authentic vs Accessible, DPI and the scale factor |
| [Design tokens](docs/design-tokens.md) | Every color, metric, font and flag token |
| [Controls](docs/controls.md) | `TurbolandWindow`, `TurbolandDialog`, `StatusBarHint` and friends |
| [Layout on the character grid](docs/character-grid.md) | `CellSize`, spacing rules, chrome that paints out of bounds |
| [Troubleshooting](docs/troubleshooting.md) | Symptoms, causes and fixes for the common traps |

## Repository layout

```text
src/
  TurbolandTheme.Core/      Palette, cell metrics and typography constants (netstandard2.0)
  TurbolandTheme.Wpf/       Resource dictionaries, control templates, custom controls
  TurbolandTheme.Gallery/   Visual test app - every themed control on one screen
  SampleApp/              A working DOS style IDE shell
tests/
  TurbolandTheme.Tests.VisualRegression/
  TurbolandTheme.Tests.UiAutomation/
```

## Building

Requires the .NET 9 SDK and Windows.

```powershell
dotnet build
dotnet test
```

Both demo apps take an optional mode and scale factor on the command line:

```powershell
dotnet run --project src/TurbolandTheme.Gallery -- accessible 2
dotnet run --project src/SampleApp -- authentic 1
```

## Licensing

### Source code

Copyright 2026 Mighty Studios, LLC.  
All rights reserved.

Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. You may obtain a copy of the License at

   http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License.


### TL;DR

You are free to:
- Use this project commercially
- Use it in closed-source or open-source software
- Modify it
- Redistribute it
- Include it in your own products

You must:
- Include the Apache 2.0 license and copyright notices
- State significant changes if you redistribute a modified version

The license also includes an express patent grant from contributors.

This summary is provided for convenience only. See the full
See [LICENSE.md](LICENSE.md)

### Third-party font notice

This repository bundles the font **Px437 IBM VGA 9x16** by VileR.  
From [The Ultimate Oldschool PC Font Pack](https://int10h.org/oldschool-pc-fonts/),
used under [CC BY-SA 4.0](https://creativecommons.org/licenses/by-sa/4.0/).

The font is licensed separately under **CC BY-SA 4.0** and is not
covered by the Apache 2.0 license applied to this project's source code.

__Projects made using this Theme and the bundled Px437 IBM VGA 9x16 font should also credit VileR according to the CC BY-SA 4.0 terms__

## Artistic Dislaimer
This project is an independent, artistic tribute to the look of 1990s DOS text-mode IDEs. It is
not affiliated with nor endorsed by any IDE vendor who made similar looking commercial projects.

---  
  
>If you enjoy this project, please consider:

<a href="https://www.buymeacoffee.com/mighty_studios" target="_blank">
  <img src="https://cdn.buymeacoffee.com/buttons/default-yellow.png" alt="Buy Me A Coffee" height="41" width="174">
</a>

<small>(The joy I get from a free latte is incredible)</small> 