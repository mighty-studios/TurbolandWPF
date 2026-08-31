# Getting started

## Requirements

- Windows
- .NET 9 SDK
- A WPF project (`net9.0-windows` with `<UseWPF>true</UseWPF>`)

## 1. Reference the theme

Add project references to both libraries:

```xml
<ItemGroup>
  <ProjectReference Include="..\TurbolandTheme.Wpf\TurbolandTheme.Wpf.csproj" />
</ItemGroup>
```

`TurbolandTheme.Wpf` already references `TurbolandTheme.Core`, so one reference is enough.
The font is embedded in `TurbolandTheme.Wpf` as a resource - nothing needs installing on
the target machine.

## 2. Apply the theme at startup

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
        Theme.ApplyTo(window);
        window.Show();
    }
}
```

> **Fully qualify `ThemeMode`.** Your `App` derives from `Application`, which has its own
> nested `System.Windows.Application.ThemeMode`. Member lookup beats `using` aliases, so
> an unqualified `ThemeMode` resolves to the wrong type.

### What each call does

**`Apply`** merges the theme dictionary into `Application.Resources` and returns the
scale factor it resolved. Call it *before* any window is constructed - window chrome
metrics are read at load time. Calling it again swaps mode or scale cleanly.

**`ApplyTo`** styles one window and turns on crisp text rendering
(`TextFormattingMode.Display`, `TextRenderingMode.Grayscale`, layout rounding).

`ApplyTo` is **not optional**. WPF matches implicit styles on an element's *exact*
runtime type, so the theme's `TargetType="Window"` style never reaches
`MainWindow : Window`. Skip the call and you get a white Segoe UI window containing
correctly themed controls. Pass `isDialog: true` for a light-gray dialog face instead of
the blue IDE desktop.

## 3. Write ordinary XAML

Stock controls need no markup changes:

```xml
<StackPanel>
    <TextBlock Text="Program name"/>
    <TextBox Text="HELLO.PAS"/>
    <CheckBox Content="_Optimise"/>
    <Button Content="_OK" IsDefault="True"/>
</StackPanel>
```

Underscores in captions still declare access keys, exactly as in stock WPF. The theme
renders them the way Turbo Vision did - by color in Authentic mode, color plus
underline in Accessible mode.

## 4. Reach for the extras when you need them

Two namespaces are worth importing:

```xml
xmlns:ctl="clr-namespace:TurbolandTheme.Wpf.Controls;assembly=TurbolandTheme.Wpf"
```

- Use `ctl:TurbolandWindow` as your window root to get the box-drawing frame with a title
  and close box drawn inside the client area.
- Use `ctl:StatusBarHint` for `F1 Help` style function-key hints.
- Use `ctl:CellSize.Rows` / `ctl:CellSize.Columns` to size things in characters rather
  than pixels.

```xml
<ctl:TurbolandWindow ... Title="TURBOLAND SAMPLE">
    <DockPanel>
        <Menu DockPanel.Dock="Top"> ... </Menu>
        <StatusBar DockPanel.Dock="Bottom">
            <ctl:StatusBarHint Key="F1" Label="Help"/>
            <Separator/>
            <ctl:StatusBarHint Key="Alt+X" Label="Exit"/>
        </StatusBar>
        <ListBox ctl:CellSize.Rows="8"/>
    </DockPanel>
</ctl:TurbolandWindow>
```

`TurbolandWindow` assigns its own style in its constructor, so a window using it does not
need `ApplyTo`.

See [Controls](controls.md) for the full set.

## 5. Run the demos

The two demo applications are the fastest reference for real usage:

```powershell
dotnet run --project src/TurbolandTheme.Gallery      # every themed control, side by side
dotnet run --project src/SampleApp                 # a working IDE shell with dialogs
```

Both accept `[authentic|accessible] [1-4]` on the command line, so you can see any
mode and scale combination immediately.

## Where to go next

- [Modes and scaling](modes-and-scaling.md) - choosing a mode and controlling size
- [Design tokens](design-tokens.md) - the colors and measurements you can reuse
- [Layout on the character grid](character-grid.md) - the one layout rule that matters
- [Troubleshooting](troubleshooting.md) - when something looks wrong
