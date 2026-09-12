using System.Windows;
using System.Windows.Input;
using TurbolandTheme.Wpf.Controls;

namespace SampleApp;

/// <summary>
/// The demonstration IDE shell: menu bar, editor, tool panel and status line inside a
/// <see cref="TurbolandWindow"/>, with in-client dialogs on a <see cref="TurbolandDialogHost"/>.
/// </summary>
/// <remarks>
/// This app exists to consume the theme the way a real application would, so it is
/// deliberately built out of stock WPF controls: <c>Menu</c>, <c>TextBox</c>,
/// <c>GroupBox</c>, <c>ListBox</c> and <c>StatusBar</c> are all untouched types picking
/// up implicit styles. The only library types referenced are the three that exist
/// because Windows offers no equivalent - the frame, the dialog and its host.
/// </remarks>
public partial class MainWindow : TurbolandWindow
{
    /// <summary>The 80x25 text mode the IDE was designed for.</summary>
    private const int GridColumns = 80;
    private const int GridRows = 25;

    private static readonly string SampleSource = string.Join(Environment.NewLine,
        "program Hello;",
        "",
        "Procedure Square(Index : Integer; Var Result : Integer);",
        "Begin",
        "    Result := Index * Index;",
        "End;",
        "",
        "Var",
        "    Res : Integer;",
        "",
        "begin",
        "    writeln('----- SQUARE OF 7 -----');",
        "    Square(7, Res);",
        "    writeln(RES);",
        "    readln;",
        "end.");

    public MainWindow()
    {
        InitializeComponent();

        Editor.Text = SampleSource;
        SizeToCellGrid();
    }

    /// <summary>
    /// Sizes the shell to 80x25 character cells plus the frame it is drawn in.
    /// </summary>
    /// <remarks>
    /// Read from the resolved resources rather than written as a pixel literal, because
    /// the metric tokens are multiplied by the active integer scale factor: the cell is
    /// 9x16 at 1x and 18x32 at 2x, so a fixed size would be right at exactly one scale.
    /// The cell is not square, so the two axes are computed separately.
    /// </remarks>
    private void SizeToCellGrid()
    {
        double cellWidth = (double)FindResource("Turboland.Metric.CellWidth");
        double cellHeight = (double)FindResource("Turboland.Metric.CellHeight");

        // The frame costs one cell on each edge; the menu and status line one row each.
        Width = (GridColumns + 2) * cellWidth;
        Height = (GridRows + 4) * cellHeight;
        MinWidth = 40 * cellWidth;
        MinHeight = 10 * cellHeight;
    }

    private void OnPreferences(object sender, ExecutedRoutedEventArgs e)
    {
        // Opening the same dialog twice should raise the existing one rather than
        // stack a duplicate on the host - the behavior a real modeless IDE dialog has.
        PreferencesDialog? existing = Dialogs.Dialogs.OfType<PreferencesDialog>().FirstOrDefault();
        if (existing is not null)
        {
            Dialogs.BringToFront(existing);
            existing.FocusFirstControl();
            return;
        }

        Dialogs.Show(new PreferencesDialog());
    }

    private void OnHelp(object sender, ExecutedRoutedEventArgs e) =>
        ShowMessage("About", "Turboland theme for WPF." + Environment.NewLine +
                             "A demonstration of TurbolandWindow, TurbolandDialog and the" + Environment.NewLine +
                             "re-templated stock WPF controls.");

    private void OnSave(object sender, ExecutedRoutedEventArgs e) =>
        ShowMessage("Save", "HELLO.PAS saved.");

    private void OnOpen(object sender, ExecutedRoutedEventArgs e) =>
        ShowMessage("Open", "No other files in C:\\TP\\BIN.");

    private void OnCompile(object sender, ExecutedRoutedEventArgs e) =>
        ShowMessage("Compile", "Compilation successful:" + Environment.NewLine +
                               $"{Editor.LineCount} lines, 0 errors.");

    private void OnRun(object sender, ExecutedRoutedEventArgs e) =>
        ShowMessage("Run", "----- SQUARE OF 7 -----" + Environment.NewLine + "49");

    private void OnExit(object sender, ExecutedRoutedEventArgs e) => Close();

    /// <summary>
    /// Shows the same About message as a true dialog window instead of an in-client one.
    /// Owner is set by <c>ShowDialog(Window)</c>, so the OS keeps it above the shell -
    /// the sorting behavior you need when the content behind it is a WebView2 or other
    /// airspace-sensitive control the in-client host cannot float over.
    /// </summary>
    private void OnAboutFloating(object sender, RoutedEventArgs e) =>
        new FloatingMessageDialog(
            "About",
            "Turboland theme for WPF." + Environment.NewLine +
            "A TurbolandFloatingDialog: its own HWND, owned by this window," + Environment.NewLine +
            "with the hard shadow drawn inside the transparent window surface.")
            .ShowDialog(this);

    /// <summary>
    /// Shows a plain message dialog on the host. Note this is not a modal call: the
    /// host's modality is soft, so the main window stays readable behind it -
    /// which is the point, since the dialog is usually asking about text on screen.
    /// </summary>
    private void ShowMessage(string title, string message) =>
        Dialogs.Show(new MessageDialog(title, message));

    private void OnEditorSelectionChanged(object sender, RoutedEventArgs e)
    {
        // GetLineIndexFromCharacterIndex returns -1 until the box has laid out, which
        // is exactly when the initial Text assignment raises this event. The guard
        // avoids reporting a bogus caret position before the first layout pass.
        int line = Editor.GetLineIndexFromCharacterIndex(Editor.CaretIndex);
        if (line < 0)
            return;

        int column = Editor.CaretIndex - Editor.GetCharacterIndexFromLineIndex(line);
        Position.Text = $"{line + 1}:{column + 1}";
    }
}
