# Controls

## Stock WPF controls

These are themed automatically by implicit styles once `Apply` has run. Use them exactly
as you always have - no attached properties, no wrapper types, no markup changes.

The implicit style **is** the entry point. There is no keyed variant such as
`TurbolandTabControlStyle`, and asking for one with `DynamicResource` silently yields
nothing - the control drops back to stock chrome with no error. To reuse a themed style
as the base of your own, key it off the type:

```xml
<Style TargetType="TabControl"
       BasedOn="{StaticResource {x:Type TabControl}}">
```

| Control | Notes |
|---|---|
| `Button` | Green face, hard black shadow, white focus ring painted outside the layout box |
| `CheckBox`, `RadioButton` | Drawn with CP437 glyphs, not vector paths |
| `TextBox`, `PasswordBox` | Cyan field, one row tall unless multi-line |
| `Menu`, `MenuItem`, `Separator` | Text-mode menu bar and drop-downs |
| `ListBox`, `ListBoxItem` | Focus ring drawn *inside* the row, so first and last rows survive scrolling |
| `ComboBox`, `ComboBoxItem` | Two-cell drop-down button |
| `TreeView`, `TreeViewItem` | Glyph expanders, cell-aligned indentation |
| `TabControl`, `TabItem` | |
| `GroupBox` | Header straddles the frame line |
| `ScrollBar`, `Thumb`, `RepeatButton` | One cell wide / one row tall; two-color track dither, recolorable via `ScrollTrackBase` / `ScrollTrackStipple` |
| `StatusBar`, `StatusBarItem` | A `Separator` child renders as the CP437 vertical bar |
| `Window` | Blue desktop or light-gray dialog face - see `ApplyTo` |

Access keys still work the standard way: `Content="_OK"` declares `Alt+O` and renders the
`O` in the accelerator color.

Every one of these controls renders both its enabled and its disabled depiction from the
same templates: set `IsEnabled="False"` (on the control or, for item controls, on an
individual item) and nothing else. Disabled ink is `Turboland.Brush.DisabledForeground`;
a disabled push button swaps its face to `Turboland.Brush.ButtonDisabledFace`; disabled
fields, lists and buttons keep a dim outline so they still read as their control type;
nested parts that fix their own color - the combo drop arrow, the tree expander marker,
the status-bar hint key - dim with the control too. A disabled accelerator never keeps
its hot-key color: a key that cannot be pressed is not advertised. The Gallery shows an
enabled and a disabled instance of every control side by side.

---

## `TurbolandWindow`

A top-level window that draws the Turbo Vision frame - box-drawing border, title on the
top frame line, close box at the left, zoom box at the right - **inside its own client
area**, while keeping every native Windows behavior.

```xml
<ctl:TurbolandWindow x:Class="MyApp.MainWindow"
                   xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                   xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                   xmlns:ctl="clr-namespace:TurbolandTheme.Wpf.Controls;assembly=TurbolandTheme.Wpf"
                   Title="TURBOLAND SAMPLE"
                   WindowStartupLocation="CenterScreen">
    <DockPanel>
        ...
    </DockPanel>
</ctl:TurbolandWindow>
```

Do not set `WindowStyle`, `WindowChrome` or the text-rendering options yourself - they
all arrive with the style, which the control assigns in its own constructor. A window
using `TurbolandWindow` does not need `TurbolandTheme.ApplyTo`.

What still works, exactly as for any other window: drag to move, drag any edge to
resize, double-click the title to maximise, Aero Snap, Snap Layouts on hover, the
taskbar, and Alt+Space.

`WindowChrome` alone gets none of that right on a custom-drawn frame, so the control
applies four corrections: it clamps a maximised window to the work area, answers the
resize band before any caption control can swallow it, reports the zoom box as
`HTMAXBUTTON` so Snap Layouts appears, and paints that button's hover state by hand
(because a non-client button receives no WPF mouse events).

Active and inactive frames differ by **line weight**: double-line glyphs when active,
single-line when not.

| Member | Purpose |
|---|---|
| `IsZoomBoxHighlighted` | Read-only; true while the pointer is over the zoom box |
| `ZoomBoxPartName` | Template part name the hit-test looks for |
| `StyleKey` | `Turboland.Style.TurbolandWindow` |

### Sizing the shell in cells

```xml
<ctl:TurbolandWindow ctl:CellSize.Columns="80" ctl:CellSize.Rows="25">
```

That gives an exact 80x25 text screen at any scale factor.

---

## `TurbolandDialog` and `TurbolandDialogHost`

Dialogs are **regions inside the main window**, not separate HWNDs - which is what
Turbo Vision did, and what the hard one-cell shadow requires. `TurbolandWindow` needs
`AllowsTransparency="False"` to keep native resize and Snap behavior, and that means
nothing can paint outside the window rectangle.

If the content behind the dialog is a WebView2, a `D3DImage` or anything else with its
own HWND, an in-client overlay cannot sort against it. Use
[`TurbolandFloatingDialog`](#turbolandfloatingdialog) instead: a real window whose owner
relationship resolves the sorting for you.

### Hosting

Drop a host over your content. It is invisible and click-through until a dialog opens:

```xml
<Grid>
    <Grid>
        <!-- your main content -->
    </Grid>
    <ctl:TurbolandDialogHost x:Name="Dialogs"/>
</Grid>
```

Placing the host as a sibling in the same grid cell - rather than wrapping the whole
window - keeps dialogs above the content but below the menu and status bar.

### Showing a dialog

`TurbolandDialog` is a `ContentControl`, so it needs no XAML file of its own:

```csharp
var dialog = new TurbolandDialog { Title = "Confirm" };
dialog.Content = new TextBlock { Text = "Save changes?" };
dialog.Closed += (_, _) => { /* read dialog.DialogResult */ };

Dialogs.Show(dialog);              // centred
Dialogs.Show(dialog, new Point(100, 50));   // explicit position
```

Or subclass it for anything reusable.

### Modality

Modality is **soft**. While a dialog is open the host swallows mouse input aimed at the
content beneath, but stays transparent so the main window remains readable - which is
the point, since a dialog is usually asking about something on screen behind it.

`Show` does not block. Use the `Closed` event, or check `DialogResult`.

### Moving

A dialog that covers the text it is asking about is a usability failure, so every dialog
is movable:

| Gesture | Action |
|---|---|
| Drag the title bar | Move (pixel-precise) |
| `Ctrl+F5`, then arrow keys | Move mode, quantised to whole cells |
| `Enter` / `Esc` in move mode | Commit / revert |
| Double-click the title bar | Shade - collapse to the frame lines |
| `Esc` | Close with `false` |
| `Enter` | Default button |

A dialog can be dragged almost off the host, but never entirely: the clamp keeps at
least four cells of the **title bar** reachable, because an in-client dialog has no
taskbar to recover it from.

### API

| Member | Purpose |
|---|---|
| `Title` | Caption drawn on the top frame line |
| `Left`, `Top` | Position within the host |
| `IsShaded`, `ToggleShade()` | Collapse to the frame |
| `IsMoveMode`, `BeginMove()`, `EndMove(bool)` | Keyboard move mode |
| `IsActive` | True for the topmost dialog |
| `DialogResult` | Set by `Close(bool?)` |
| `Close(bool? result = null)` | Closes and raises `Closed` |
| `FocusFirstControl()` | Moves focus into the content |
| `Host` | The `TurbolandDialogHost` this dialog is on, if any |
| `MoveCommand`, `ShadeCommand` | Routed commands for menus or bindings |

Host members: `Show`, `Remove`, `BringToFront`, `Dialogs`, `ActiveDialog`.

To raise an existing dialog rather than stacking a duplicate:

```csharp
var existing = Dialogs.Dialogs.OfType<PreferencesDialog>().FirstOrDefault();
if (existing is not null)
    Dialogs.BringToFront(existing);
else
    Dialogs.Show(new PreferencesDialog());
```

---

## `TurbolandFloatingDialog`

A **true top-level dialog window** in the same visual style - gray face, double-line
frame, close box, hard shadow - but with its own HWND. Use it instead of the in-client
`TurbolandDialog` when the content behind the dialog is a WebView2, a `D3DImage` or any
other airspace-sensitive control: an owned window sorts above its owner by OS rule,
which no in-window overlay can do.

```csharp
var dialog = new TurbolandFloatingDialog { Title = "Confirm" };
dialog.Content = new TextBlock { Text = "Save changes?" };

dialog.ShowDialog(this);   // modal, owned, centred on the owner
```

`ShowDialog(Window)` sets `Owner` for you; for a modeless dialog set `Owner` and call
`Show()`. An owned window always stays above its owner, so z-order needs no management,
and `ShowDialog` gives **real** modality - the call blocks and returns the dialog
result, unlike the host's soft modality.

The shadow is the artistic payoff of the transparent window:
`WindowStyle=None` + `AllowsTransparency=True` + a transparent `Background`, with the
one-cell hard shadow painted as ordinary content in the margin the face reserves to its
right and bottom. The main window avoids transparency to keep native resize and Snap
Layouts; a dialog has neither, so it can afford the look.

The class sets the transparency invariants, `ResizeMode.NoResize`, `ShowInTaskbar=False`,
`SizeToContent=WidthAndHeight` and `WindowStartupLocation=CenterOwner` itself. Do not
set `WindowStyle`, `AllowsTransparency` or the text-rendering options - like
`TurbolandWindow` it assigns its own style, so `TurbolandTheme.ApplyTo` is not needed.

| Gesture / key | Action |
|---|---|
| Drag the title bar | Move (native) |
| `Ctrl+F5`, then arrow keys | Move mode, quantised to whole cells; `Enter` commits, `Esc` reverts |
| `Esc` | Invokes the `IsCancel` button; closes the dialog if there is none |
| `Enter` | Default button (WPF's own, since this dialog is a real focus-scope root) |

Put `IsCancel` only on a button that actually cancels. WPF answers a click on a cancel
button by setting `DialogResult=false` itself, so a `Click` handler that calls
`Close(true)` on the same button is fighting the framework. A confirm-style OK button
gets `IsDefault` alone; `Esc` still closes both dialog types (via the cancel button if
there is one, otherwise with `false` / by closing).

| Member | Purpose |
|---|---|
| `Title` | Caption drawn on the top frame line (the `Window.Title` property) |
| `ShowDialog(Window owner)` | Sets `Owner`, shows modally, returns the result |
| `Close(bool? result)` | Sets `DialogResult` when modal, skips it silently when modeless |
| `IsMoveMode`, `BeginMove()`, `EndMove(bool)` | Keyboard move mode |
| `FocusFirstControl()` | Moves focus into the content |
| `StyleKey` | `Turboland.Style.TurbolandFloatingDialog` |

Subclass it for anything reusable - the SampleApp's `FloatingMessageDialog` is a
`TurbolandDialog`-shaped message box built exactly this way.

---

## `StatusBarHint`

A function-key hint: a colored key name followed by a black label.

```xml
<StatusBar DockPanel.Dock="Bottom">
    <ctl:StatusBarHint Key="F1" Label="Help"/>
    <Separator/>
    <ctl:StatusBarHint Key="Alt+X" Label="Exit"/>
    <StatusBarItem DockPanel.Dock="Right">
        <TextBlock x:Name="Position" Text="1:1"/>
    </StatusBarItem>
</StatusBar>
```

`Key` is a caption, not a `KeyGesture` - forms like `Alt+X` render exactly as written.

Spacing is fixed by the template (one blank cell, key, blank cell, label, blank cell),
so hints cannot drift apart from one another. `StatusBar`'s default items panel is a
`DockPanel`, so `DockPanel.Dock="Right"` works with no extra markup, and a `Separator`
child automatically renders as the CP437 vertical bar.

---

## `CellSize`

Attached properties that size any element in character cells:

```xml
<ListBox ctl:CellSize.Rows="8"/>
<TextBox ctl:CellSize.Columns="40"/>
```

XAML has no arithmetic on resource references, so without this an application laying out
on the grid would have to write the pixel product - a magic number, and wrong at every
scale but 1x.

The cell is **not square** (9 x 16 at 1x), so rows and columns are separate properties
reading separate tokens. There is no single "cells" property.

Values track the live token, so a later `Apply` at a new scale re-runs the
multiplication.

---

## `AcceleratorText`

The `TextBlock` subclass the templates use to draw access keys. You rarely instantiate
it, but its attached properties let you tune any templated control:

| Property | Effect |
|---|---|
| `AcceleratorBrush` | Color of the marked character |
| `ShowUnderline` | Underline in addition to coloring |
| `InheritTextColor` | Take the surrounding text color instead of a hue |

All three inherit down the tree, so setting one on a container covers everything in it.

WPF's built-in `AccessText` can only *underline* the access key - it holds the caption
in one unsplit run, so its color cannot be overridden per character. Replacing it would
normally destroy the access key, since WPF registers access keys from string content;
`AcceleratorText` re-registers the key itself against the owning control via
`AccessKeyManager`, which resolves the correct scope automatically (the window for a
button, the `Menu` for a menu item).

`AcceleratorTemplateSelector` is the hook that applies it to `ContentPresenter` content,
available as `Turboland.Selector.Accelerator`.

---

## `GlyphFill`

Fills its bounds by repeating a single CP437 glyph, so frames are drawn from real
box-drawing characters rather than approximated rectangles.

```xml
<ctl:GlyphFill Glyph="═" Orientation="Horizontal"
               Foreground="{DynamicResource Turboland.Brush.FrameActive}"/>
```

A `TextBlock` cannot do this: the repeat count depends on the final arranged width,
which is not known until after layout. The count is derived from the glyph's *measured
advance*, so a mismatch between the font and the cell token can never open a seam.

Properties: `Glyph`, `Orientation`, `Foreground`, `FontFamily`, `FontSize`.

---

## `DialogGeometry`

Placement arithmetic, free of WPF plumbing so it can be used or tested without a window:
`ShadowOffset`, `ClampPosition`, `MoveByCells`, `SnapToCell`, plus the constants
`ShadowCellsX`, `ShadowRowsY` and `MinVisibleCells`.
