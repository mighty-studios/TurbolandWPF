# Layout on the character grid

The theme is built on a single premise: the screen is a grid of **9 x 16 pixel character
cells** (multiplied by the scale factor). Every measurement in the theme is a whole
number of cells or rows. An application that respects that looks right automatically;
one that does not accumulates small, hard-to-diagnose misalignments.

There are only two rules.

## Rule 1 - size in cells, never in pixels

Pixel literals are wrong twice over: they are magic numbers, and they are wrong at every
scale factor but 1x.

```xml
<!-- Don't -->
<ListBox Height="120"/>

<!-- Do -->
<ListBox ctl:CellSize.Rows="8"/>
```

`120` is 7.5 rows. A list box given that height is *structurally incapable* of showing a
whole number of rows and will always slice the last one - no matter how the template is
written.

For spacing, use the size tokens rather than inventing values:

```xml
<StackPanel Margin="{DynamicResource Turboland.Size.ColumnGutter}">
    <TextBlock Text="Program name"/>          <!-- no margin: a caption sits on the row above -->
    <TextBox/>
    <TextBlock Text="Options"
               Margin="{DynamicResource Turboland.Size.SectionMargin}"/>  <!-- one blank row -->
</StackPanel>
```

The convention is: **a caption carries no margin** - on a character grid a label sits
directly on the row above its control. Separation between *groups* is a whole row
(`SectionMargin`), never a few pixels.

Some things are legitimately off-grid - a tab strip, for instance. Let those size to
content rather than inventing a row count that is a fiction.

## Rule 2 - reserve space for chrome that paints outside the box

This is the rule that is easy to miss, because it is invisible until two conditions
coincide.

Turboland focus rings and button shadows are painted **outside** the control's layout box,
using equal and opposite margins so they cost nothing in measure. That is deliberate: a
2 px focus ring in Accessible mode would otherwise resize every focused control.

But a container that **clips to its bounds** - a `ScrollViewer` is the common case -
will cut that overhang away. If a control sits flush against the clipping edge, its
chrome is silently truncated on that side.

### The symptom

A focus ring missing exactly one edge, with the two perpendicular edges each short by
exactly one corner pixel. That signature means clipping, not a color or thickness
fault.

### The fix

Give the clipped container's **content** a `ChromeInset` margin:

```xml
<ScrollViewer VerticalScrollBarVisibility="Auto">
    <Grid Margin="{DynamicResource Turboland.Size.ChromeInset}">
        ...
    </Grid>
</ScrollViewer>
```

`Turboland.Size.ChromeInset` is `1,1,9,7` at 1x, and `2,2,9,7` in Accessible mode where
the ring is 2 px thick. It is asymmetric on purpose: left and top only need to clear the
focus ring, while right and bottom must also clear the button shadow.

> **Put it on the content, not on the container's `Padding`.** The stock `ScrollViewer`
> template applies `Padding` to the `ScrollContentPresenter` itself, which moves the clip
> along with the content - the ring is still cut. A content `Margin` is what works.

### When you need it

Only when both conditions hold:

- an ancestor clips (`ScrollViewer`, or anything with `ClipToBounds="True"`), **and**
- a themed control sits flush against that edge

Neither alone shows anything, which is precisely why this is worth knowing in advance.

`ListBoxItem` is the one template that draws its focus ring *inside* the row, so list
items survive their own scroll viewer without help. Everything else relies on the
consumer reserving the space.

## Sizing a window to an exact text screen

```xml
<ctl:TurbolandWindow ctl:CellSize.Columns="80" ctl:CellSize.Rows="25">
```

Or from code, before any visual tree exists:

```csharp
var metrics = CellMetrics.ForScale(scale);
Width  = 80 * metrics.CellWidth;
Height = 25 * metrics.CellHeight;
```

## Why integers only

The font is a pixel-outline face: its glyphs are rectangles aligned to a 9 x 16 grid.
Scale by a non-integer and glyph edges land between physical pixels, the rasteriser
antialiases them, and the whole point of the theme is lost.

This is also why Accessible mode changes no measurement. Legibility comes from the
color axis; size comes from the scale axis. Mixing them would produce fractional cells.

## Checklist

- [ ] No pixel literals for width, height, margin or padding - use `CellSize` or a token
- [ ] `DynamicResource`, never `StaticResource`, for any `Turboland.*` reference
- [ ] Captions have no bottom margin; groups are separated by `SectionMargin`
- [ ] Every clipping container's content carries `Turboland.Size.ChromeInset`
- [ ] Custom tokens named `Turboland.Metric.*` / `Turboland.Size.*` are defined at 1x
