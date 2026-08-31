using System.Windows.Input;

namespace SampleApp;

/// <summary>
/// The classic DOS IDE gestures, declared as commands rather than handled in a
/// key switch, so each one is inspectable and testable and so a single declaration
/// drives both the menu item and the accelerator.
/// </summary>
/// <remarks>
/// Each command carries its own <see cref="InputGesture"/>, and that is what makes the
/// shortcut text appear beside the menu item: WPF derives
/// <c>MenuItem.InputGestureText</c> from the bound command's gestures, and the themed
/// template renders that standard property. No caller repeats a shortcut as a string.
/// </remarks>
public static class IdeCommands
{
    public static readonly RoutedUICommand Help = Create(nameof(Help), "Help", Key.F1, ModifierKeys.None);
    public static readonly RoutedUICommand Save = Create(nameof(Save), "Save", Key.F2, ModifierKeys.None);
    public static readonly RoutedUICommand Open = Create(nameof(Open), "Open", Key.F3, ModifierKeys.None);
    public static readonly RoutedUICommand Make = Create(nameof(Make), "Make", Key.F9, ModifierKeys.None);
    public static readonly RoutedUICommand Compile = Create(nameof(Compile), "Compile", Key.F9, ModifierKeys.Alt);
    public static readonly RoutedUICommand Run = Create(nameof(Run), "Run", Key.F9, ModifierKeys.Control);
    public static readonly RoutedUICommand Exit = Create(nameof(Exit), "Exit", Key.X, ModifierKeys.Alt);

    /// <summary>Reached from the Options menu only, exactly as in the IDE.</summary>
    public static readonly RoutedUICommand Preferences =
        new("Preferences", nameof(Preferences), typeof(IdeCommands));

    private static RoutedUICommand Create(string name, string text, Key key, ModifierKeys modifiers)
    {
        InputGestureCollection gestures = new() { new KeyGesture(key, modifiers) };
        return new RoutedUICommand(text, name, typeof(IdeCommands), gestures);
    }
}
