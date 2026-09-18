namespace TurbolandTheme.Core;

/// <summary>
/// Semantic color tokens. Every value resolves to an entry of <see cref="VgaPalette"/>,
/// so the theme can only ever paint with the 16 colors the original hardware had.
/// <para>
/// Consumers bind to the meaning ("button face") rather than the hue ("green"), which
/// is what lets a mode swap a color without touching a control template.
/// </para>
/// <para>
/// This mirrors <c>Themes/Tokens.Colors.xaml</c>. The two must stay in step - the XAML
/// is what actually renders; this class exists for code that needs the same values
/// without a resource lookup.
/// </para>
/// </summary>
public sealed class ColorTokens
{
    private ColorTokens()
    {
    }

    /// <summary>Desktop / application background behind windows. VGA 1 (Blue).</summary>
    public VgaColor DesktopBackground => VgaPalette.Blue;

    /// <summary>Window and dialog face. VGA 7 (Light Gray).</summary>
    public VgaColor WindowBackground => VgaPalette.LightGray;

    /// <summary>Default text on window faces. VGA 0 (Black).</summary>
    public VgaColor WindowForeground => VgaPalette.Black;

    /// <summary>Bright border of the active window frame. VGA 15 (White).</summary>
    public VgaColor FrameActive => VgaPalette.White;

    /// <summary>
    /// Dim border of the inactive window frame. VGA 7 (Light Gray).
    /// <para>
    /// A frame also changes line weight between states (double when active, single
    /// when not), so activation stays legible without relying on hue alone.
    /// </para>
    /// </summary>
    public VgaColor FrameInactive => VgaPalette.LightGray;

    /// <summary>Menu bar and dropdown face. VGA 7 (Light Gray).</summary>
    public VgaColor MenuBackground => VgaPalette.LightGray;

    /// <summary>Menu bar text. VGA 0 (Black).</summary>
    public VgaColor MenuForeground => VgaPalette.Black;

    /// <summary>Highlighted menu item background. VGA 2 (Green).</summary>
    public VgaColor MenuSelectionBackground => VgaPalette.Green;

    /// <summary>
    /// Highlighted menu item text. VGA 0 (Black) - the same ink as an unhighlighted
    /// item, so only the background changes as the selection moves.
    /// </summary>
    public VgaColor MenuSelectionForeground => VgaPalette.Black;

    /// <summary>
    /// Underlined menu accelerator letter. VGA 4 (Red). Applied whether or not the
    /// item is highlighted, so the hot key is always locatable.
    /// </summary>
    public VgaColor MenuAccelerator => VgaPalette.Red;

    /// <summary>Status bar face. VGA 7 (Light Gray).</summary>
    public VgaColor StatusBarBackground => VgaPalette.LightGray;

    /// <summary>Status bar text. VGA 0 (Black).</summary>
    public VgaColor StatusBarForeground => VgaPalette.Black;

    /// <summary>Status bar function-key label. VGA 4 (Red).</summary>
    public VgaColor StatusBarAccelerator => VgaPalette.Red;

    /// <summary>
    /// Status bar segment separator. VGA 0 (Black) - the same ink as the labels,
    /// because the separator is a drawn CP437 0xB3 glyph occupying a character cell
    /// rather than a rule drawn between cells.
    /// </summary>
    public VgaColor StatusBarSeparator => VgaPalette.Black;

    /// <summary>Input field and option-panel background. VGA 3 (Cyan).</summary>
    public VgaColor FieldBackground => VgaPalette.Cyan;

    /// <summary>Input field text. VGA 0 (Black).</summary>
    public VgaColor FieldForeground => VgaPalette.Black;

    /// <summary>
    /// Text of the focused row inside an option panel. VGA 15 (White). The panel face
    /// is unchanged by focus; only the text is recolored.
    /// </summary>
    public VgaColor FieldFocusedForeground => VgaPalette.White;

    /// <summary>Selection inside fields and lists. VGA 2 (Green).</summary>
    public VgaColor FieldSelectionBackground => VgaPalette.Green;

    /// <summary>Selected field text. VGA 15 (White).</summary>
    public VgaColor FieldSelectionForeground => VgaPalette.White;

    /// <summary>Accelerator letter inside a dialog option panel. VGA 14 (Yellow).</summary>
    public VgaColor FieldAccelerator => VgaPalette.Yellow;

    /// <summary>Push-button face. VGA 2 (Green).</summary>
    public VgaColor ButtonFace => VgaPalette.Green;

    /// <summary>Push-button text. VGA 0 (Black).</summary>
    public VgaColor ButtonForeground => VgaPalette.Black;

    /// <summary>Push-button accelerator letter. VGA 14 (Yellow).</summary>
    public VgaColor ButtonAccelerator => VgaPalette.Yellow;

    /// <summary>Push-button hard shadow strip. VGA 0 (Black).</summary>
    public VgaColor ButtonShadow => VgaPalette.Black;

    /// <summary>
    /// Face of a disabled push button. VGA 2 (Green) in Authentic mode - the same
    /// green as an enabled button, so a disabled button still reads as a button and
    /// only its label dims; Accessible mode overrides the resource to VGA 7 (Light
    /// Gray) because black is the only ink reaching AA on green and is already the
    /// enabled label. Mirrors <c>Turboland.Brush.ButtonDisabledFace</c>.
    /// </summary>
    public VgaColor ButtonDisabledFace => VgaPalette.Green;

    /// <summary>Code editor background. VGA 1 (Blue), not black.</summary>
    public VgaColor EditorBackground => VgaPalette.Blue;

    /// <summary>Code editor identifier text. VGA 14 (Yellow).</summary>
    public VgaColor EditorForeground => VgaPalette.Yellow;

    /// <summary>Code editor reserved word. VGA 15 (White).</summary>
    public VgaColor EditorKeywordForeground => VgaPalette.White;

    /// <summary>Editor line-number gutter background. VGA 0 (Black).</summary>
    public VgaColor EditorGutterBackground => VgaPalette.Black;

    /// <summary>Editor line-number gutter text. VGA 8 (Dark Gray).</summary>
    public VgaColor EditorGutterForeground => VgaPalette.DarkGray;

    /// <summary>
    /// Selection inside the editor. VGA 7 (Light Gray) - a light selection is needed
    /// because the editor face is itself blue, against which a blue selection would
    /// be invisible.
    /// </summary>
    public VgaColor EditorSelectionBackground => VgaPalette.LightGray;

    /// <summary>Selected editor text. VGA 0 (Black), for contrast against the light selection.</summary>
    public VgaColor EditorSelectionForeground => VgaPalette.Black;

    /// <summary>
    /// Focus outline color. White reads as a highlight against every face the theme
    /// paints; the accessible mode overrides it to yellow for stronger contrast.
    /// </summary>
    public VgaColor FocusOutline => VgaPalette.White;

    /// <summary>Hard drop shadow behind windows and buttons. VGA 0 (Black).</summary>
    public VgaColor Shadow => VgaPalette.Black;

    /// <summary>Error / diagnostic text. VGA 12 (Light Red).</summary>
    public VgaColor ErrorForeground => VgaPalette.LightRed;

    /// <summary>Disabled control text. VGA 8 (Dark Gray).</summary>
    public VgaColor DisabledForeground => VgaPalette.DarkGray;

    /// <summary>
    /// Face of the gray controls - scroll bars, combo drop buttons, tab strip.
    /// Deliberately distinct from <see cref="ButtonFace"/>: these are gray rather
    /// than green, so a scroll bar never reads as a pressable button.
    /// </summary>
    public VgaColor ControlFace => VgaPalette.LightGray;

    /// <summary>Outline / shadow strip color of the gray controls. VGA 0 (Black).</summary>
    public VgaColor ControlShadow => VgaPalette.Black;

    /// <summary>
    /// Solid color of a scroll bar's arrow buttons and thumb, and the ground
    /// the track dither sits on. VGA 7 (Light Gray). Mirrors
    /// <c>Turboland.Brush.ScrollTrackBase</c>; an application overrides the
    /// resource to recolor its bars, the way the original IDEs gave the editor
    /// and debugger windows their own pairs.
    /// </summary>
    public VgaColor ScrollTrackBase => VgaPalette.LightGray;

    /// <summary>
    /// Checker-dot color of a scroll bar's stippled track, and the ink of its
    /// arrow glyphs and thumb frame. VGA 0 (Black). Mirrors
    /// <c>Turboland.Brush.ScrollTrackStipple</c>.
    /// </summary>
    public VgaColor ScrollTrackStipple => VgaPalette.Black;

    /// <summary>
    /// Shared selection accent. VGA 2 (Green) - the single highlight color used by
    /// menus, lists and fields alike, so selection looks the same everywhere.
    /// </summary>
    public VgaColor ControlSelected => VgaPalette.Green;

    /// <summary>Text on a selected control face. VGA 15 (White).</summary>
    public VgaColor ControlSelectedForeground => VgaPalette.White;

    /// <summary>
    /// Per-mode accessor. Both modes currently share one instance: a mode changes
    /// colors by overriding resources, not by supplying a different token set.
    /// </summary>
    public static ColorTokens For(ThemeMode mode) => Shared;

    private static readonly ColorTokens Shared = new();
}
