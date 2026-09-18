using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace TurbolandTheme.Wpf.Controls;

/// <summary>
/// Paints an element's Background as a two-color checkerboard dither: the
/// stippled trough of a Borland Vision scroll bar, where the whole bar is
/// drawn from exactly one pair of colors.
/// <para>
/// The tile is built in code rather than declared as a shared DrawingBrush
/// resource on purpose. A Freezable resolves its own DynamicResource
/// references in the context of the dictionary that hosts it, never the
/// context of the element that later uses it, so a brush declared in the
/// theme would bake in the theme's default pair and an application could
/// not recolor it. Attached properties set on the element itself resolve
/// through the element's context, which is what makes
/// <c>Turboland.Brush.ScrollTrackBase</c> and
/// <c>Turboland.Brush.ScrollTrackStipple</c> overridable by the application
/// (or by any scope between the element and the application).
/// </para>
/// <para>
/// The checker is one device pixel per dot, tiled and clipped to whatever
/// the element's bounds are. It deliberately does NOT follow the metric
/// scale factor: a dither is a texture, not a layout measure, and keeping
/// it at native pixel size is what preserves the VGA look at every scale.
/// </para>
/// </summary>
public static class StippleFill
{
    /// <summary>Side of the repeating tile, in device-independent pixels.</summary>
    public const double TileSize = 2;

    public static readonly DependencyProperty BaseBrushProperty =
        DependencyProperty.RegisterAttached(
            "BaseBrush",
            typeof(Brush),
            typeof(StippleFill),
            new FrameworkPropertyMetadata(null, OnStippleBrushChanged));

    public static readonly DependencyProperty DotBrushProperty =
        DependencyProperty.RegisterAttached(
            "DotBrush",
            typeof(Brush),
            typeof(StippleFill),
            new FrameworkPropertyMetadata(null, OnStippleBrushChanged));

    /// <summary>
    /// The solid color the dither sits on: also the face of the arrow
    /// buttons and the thumb that ride above it.
    /// </summary>
    public static Brush? GetBaseBrush(DependencyObject element) =>
        (Brush?)element.GetValue(BaseBrushProperty);

    public static void SetBaseBrush(DependencyObject element, Brush? value) =>
        element.SetValue(BaseBrushProperty, value);

    /// <summary>The color of the checker dots.</summary>
    public static Brush? GetDotBrush(DependencyObject element) =>
        (Brush?)element.GetValue(DotBrushProperty);

    public static void SetDotBrush(DependencyObject element, Brush? value) =>
        element.SetValue(DotBrushProperty, value);

    private static void OnStippleBrushChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not FrameworkElement element)
            return;

        Brush? background = GetBaseBrush(element);
        Brush? dots = GetDotBrush(element);

        // Until both halves of the pair resolve, leave whatever Background the
        // element already has; a half-painted dither would read as a bug.
        if (background is null || dots is null)
            return;

        Brush brush = CreateTile(background, dots);
        switch (element)
        {
            case Panel panel:
                panel.Background = brush;
                break;
            case Control control:
                control.Background = brush;
                break;
        }
    }

    /// <summary>
    /// Builds the 2x2 checker: base fills the tile, dots land on the two
    /// diagonal cells, so tiling alternates every pixel on both axes - the
    /// same 50 percent dither the VGA originals rasterized.
    /// </summary>
    public static DrawingBrush CreateTile(Brush background, Brush dots)
    {
        DrawingGroup drawing = new();
        drawing.Children.Add(new GeometryDrawing(
            background,
            null,
            new RectangleGeometry(new Rect(0, 0, TileSize, TileSize))));

        GeometryGroup dotCells = new();
        dotCells.Children.Add(new RectangleGeometry(new Rect(0, 0, 1, 1)));
        dotCells.Children.Add(new RectangleGeometry(new Rect(1, 1, 1, 1)));
        drawing.Children.Add(new GeometryDrawing(dots, null, dotCells));

        return new DrawingBrush(drawing)
        {
            TileMode = TileMode.Tile,
            // Absolute units: the tile is a fixed pixel texture, so it must
            // not stretch with the element it fills.
            Viewport = new Rect(0, 0, TileSize, TileSize),
            ViewportUnits = BrushMappingMode.Absolute,
            Viewbox = new Rect(0, 0, TileSize, TileSize),
            ViewboxUnits = BrushMappingMode.Absolute,
            AlignmentX = AlignmentX.Left,
            AlignmentY = AlignmentY.Top,
        };
    }
}
