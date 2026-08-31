using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using TurbolandTheme.Wpf.Controls;
using Xunit;
using Theme = TurbolandTheme.Wpf.TurbolandTheme;
using ThemeMode = TurbolandTheme.Core.ThemeMode;

namespace TurbolandTheme.Tests.VisualRegression;

/// <summary>
/// Guards <see cref="TurbolandDialog"/> and its host. Everything the OS would normally do
/// for a dialog - z-order, modality, focus, keeping the window reachable - is ours here,
/// and all of it fails silently: the dialog still appears, it just stops being usable.
/// </summary>
[Collection("Wpf")]
public class TurbolandDialogTest
{
    private readonly Application _app;

    public TurbolandDialogTest(WpfApplicationFixture fixture) => _app = fixture.App;

    /// <summary>Builds a laid-out host so ActualWidth/Height and DesiredSize are real.</summary>
    private static TurbolandDialogHost LaidOutHost(double width = 800, double height = 600)
    {
        TurbolandDialogHost host = new();
        host.Measure(new Size(width, height));
        host.Arrange(new Rect(0, 0, width, height));
        return host;
    }

    private static void Relayout(TurbolandDialogHost host)
    {
        host.Measure(new Size(host.ActualWidth, host.ActualHeight));
        host.Arrange(new Rect(0, 0, host.ActualWidth, host.ActualHeight));
        host.UpdateLayout();
    }

    [Fact]
    public void Dialog_ResolvesItsThemeStyleAndTemplate()
    {
        StaThread.Run(() =>
        {
            Theme.Apply(_app, ThemeMode.Authentic, 1);
            TurbolandDialog dialog = new();

            Assert.NotNull(dialog.Style);
            Assert.NotNull(dialog.Template);
        });
    }

    [Fact]
    public void Dialog_ExposesItsTitleBarAndCloseBoxAsTemplateParts()
    {
        StaThread.Run(() =>
        {
            Theme.Apply(_app, ThemeMode.Authentic, 1);
            TurbolandDialogHost host = LaidOutHost();
            TurbolandDialog dialog = new() { Title = "Tools", Content = new TextBlock { Text = "x" } };

            host.Show(dialog);
            Relayout(host);

            // Dragging and closing are wired to these by name; a rename in the template
            // would leave a dialog that cannot be moved or dismissed.
            Assert.NotNull(dialog.Template.FindName(TurbolandDialog.TitleBarPart, dialog));
            Assert.NotNull(dialog.Template.FindName(TurbolandDialog.CloseBoxPart, dialog));
        });
    }

    [Fact]
    public void Dialog_TakesItsCellMetricsFromTheScaledTheme()
    {
        StaThread.Run(() =>
        {
            Theme.Apply(_app, ThemeMode.Authentic, 2);
            TurbolandDialog dialog = new();
            dialog.ApplyTemplate();

            // The drag clamp and the keyboard step both read these, so at 2x a dialog
            // must move in 18px columns, not 9px ones. Width and height scale to
            // different numbers because the character cell is 9x16, not square.
            Assert.Equal(18, dialog.CellWidth);
            Assert.Equal(32, dialog.CellHeight);
            Assert.Equal(32, dialog.TitleBarHeight);
            Assert.Equal(72, dialog.MinVisibleWidth);
        });
    }

    [Fact]
    public void Host_IsClickThroughUntilADialogIsShown()
    {
        StaThread.Run(() =>
        {
            Theme.Apply(_app, ThemeMode.Authentic, 1);
            TurbolandDialogHost host = LaidOutHost();

            // A null Background is not hit-testable, which is what lets the main content
            // below stay usable while no dialog is open.
            Assert.Null(host.Background);
            Assert.False(host.IsHitTestVisible);

            host.Show(new TurbolandDialog());

            // Soft modality: the host now swallows mouse input aimed at the content
            // beneath, but stays transparent so that content is still readable.
            Assert.Equal(Brushes.Transparent, host.Background);
            Assert.True(host.IsHitTestVisible);
        });
    }

    [Fact]
    public void Host_BecomesClickThroughAgainWhenTheLastDialogCloses()
    {
        StaThread.Run(() =>
        {
            Theme.Apply(_app, ThemeMode.Authentic, 1);
            TurbolandDialogHost host = LaidOutHost();
            TurbolandDialog dialog = new();

            host.Show(dialog);
            dialog.Close(true);

            Assert.Null(host.Background);
            Assert.False(host.IsHitTestVisible);
            Assert.Empty(host.Dialogs);
        });
    }

    [Fact]
    public void Host_TracksTheFrontmostDialogAsTheActiveOne()
    {
        StaThread.Run(() =>
        {
            Theme.Apply(_app, ThemeMode.Authentic, 1);
            TurbolandDialogHost host = LaidOutHost();
            TurbolandDialog first = new();
            TurbolandDialog second = new();

            host.Show(first);
            host.Show(second);

            Assert.Same(second, host.ActiveDialog);
            Assert.False(first.IsActive);
            Assert.True(second.IsActive);

            host.BringToFront(first);

            Assert.Same(first, host.ActiveDialog);
            Assert.True(first.IsActive);
            Assert.False(second.IsActive);
        });
    }

    [Fact]
    public void Host_RestoresActivationToWhatIsLeftWhenADialogCloses()
    {
        StaThread.Run(() =>
        {
            Theme.Apply(_app, ThemeMode.Authentic, 1);
            TurbolandDialogHost host = LaidOutHost();
            TurbolandDialog first = new();
            TurbolandDialog second = new();

            host.Show(first);
            host.Show(second);
            second.Close();

            Assert.Same(first, host.ActiveDialog);
            Assert.True(first.IsActive);
        });
    }

    [Fact]
    public void Host_CentresADialogGivenNoPosition()
    {
        StaThread.Run(() =>
        {
            Theme.Apply(_app, ThemeMode.Authentic, 1);
            TurbolandDialogHost host = LaidOutHost();
            TurbolandDialog dialog = new() { Content = new TextBlock { Text = "content" } };

            host.Show(dialog);
            Relayout(host);

            // Centring must wait for the first measure: a dialog sized to its content
            // has no useful size before then, and centring on zero puts it at the
            // middle of the host rather than around it.
            Assert.True(dialog.Left > 0, $"Left was {dialog.Left}");
            Assert.True(dialog.Top > 0, $"Top was {dialog.Top}");
            Assert.True(dialog.Left < host.ActualWidth - dialog.DesiredSize.Width);
        });
    }

    [Fact]
    public void Host_ReClampsDialogsWhenItShrinks()
    {
        StaThread.Run(() =>
        {
            Theme.Apply(_app, ThemeMode.Authentic, 1);
            TurbolandDialogHost host = LaidOutHost();
            TurbolandDialog dialog = new() { Content = new TextBlock { Text = "content" } };

            host.Show(dialog, new Point(700, 500));
            Relayout(host);

            // Shrinking the window must not strand a dialog outside the client area,
            // where an in-client overlay has no taskbar to recover it from.
            host.Measure(new Size(200, 120));
            host.Arrange(new Rect(0, 0, 200, 120));

            Assert.True(dialog.Top <= 120 - dialog.TitleBarHeight, $"Top was {dialog.Top}");
            Assert.True(dialog.Left <= 200 - dialog.MinVisibleWidth, $"Left was {dialog.Left}");
        });
    }

    [Fact]
    public void Dialog_ShadesAndUnshades()
    {
        StaThread.Run(() =>
        {
            Theme.Apply(_app, ThemeMode.Authentic, 1);
            TurbolandDialogHost host = LaidOutHost();
            TurbolandDialog dialog = new() { Content = new TextBlock { Text = "content" } };
            host.Show(dialog);
            Relayout(host);

            double full = dialog.DesiredSize.Height;
            dialog.ToggleShade();
            Relayout(host);
            double shaded = dialog.DesiredSize.Height;

            // Collapsing to the frame is the "minimise but stay bound to the window"
            // affordance - an in-client dialog has no taskbar to minimise to.
            Assert.True(dialog.IsShaded);
            Assert.True(shaded < full, $"shaded {shaded} was not smaller than {full}");

            dialog.ToggleShade();
            Relayout(host);

            Assert.False(dialog.IsShaded);
            Assert.Equal(full, dialog.DesiredSize.Height);
        });
    }

    [Fact]
    public void Dialog_BindsCtrlF5ToMoveMode()
    {
        StaThread.Run(() =>
        {
            Theme.Apply(_app, ThemeMode.Authentic, 1);
            TurbolandDialog dialog = new();

            // Turbo Vision's move gesture. Asserted on the binding rather than by
            // synthesising the keystroke, because Keyboard.Modifiers is device state
            // that cannot be faked - and a declared binding is the thing that can break.
            KeyBinding binding = dialog.InputBindings
                .OfType<KeyBinding>()
                .Single(b => b.Key == Key.F5);

            Assert.Equal(ModifierKeys.Control, binding.Modifiers);
            Assert.Same(TurbolandDialog.MoveCommand, binding.Command);
        });
    }

    [Fact]
    public void Dialog_MovesByWholeCellsInKeyboardMoveMode()
    {
        StaThread.Run(() =>
        {
            Theme.Apply(_app, ThemeMode.Authentic, 1);
            TurbolandDialogHost host = LaidOutHost();
            TurbolandDialog dialog = new() { Content = new TextBlock { Text = "content" } };
            host.Show(dialog, new Point(100, 96));
            Relayout(host);

            TurbolandDialog.MoveCommand.Execute(null, dialog);
            Assert.True(dialog.IsMoveMode);

            SendKey(dialog, Key.Right);
            SendKey(dialog, Key.Down);

            // Mouse dragging is continuous; keyboard movement is quantised, which is
            // what keeps a keyboard-placed dialog on the character grid. Entering move
            // mode also snaps first: on the 9px grid 100 rounds to 99, the nearest
            // column (100/9 = 11.1), so the step starts from there and not from 100.
            Assert.Equal(99 + dialog.CellWidth, dialog.Left);
            Assert.Equal(96 + dialog.CellHeight, dialog.Top);

            SendKey(dialog, Key.Enter);
            Assert.False(dialog.IsMoveMode);
        });
    }

    [Fact]
    public void Dialog_RevertsAKeyboardMoveOnEscape()
    {
        StaThread.Run(() =>
        {
            Theme.Apply(_app, ThemeMode.Authentic, 1);
            TurbolandDialogHost host = LaidOutHost();
            TurbolandDialog dialog = new() { Content = new TextBlock { Text = "content" } };
            host.Show(dialog, new Point(104, 96));
            Relayout(host);

            dialog.BeginMove();
            SendKey(dialog, Key.Right);
            SendKey(dialog, Key.Right);
            SendKey(dialog, Key.Escape);

            // Esc in move mode reverts the move; it must NOT also close the dialog,
            // which is what would happen if the key fell through to the Esc handler.
            Assert.False(dialog.IsMoveMode);
            Assert.Equal(104, dialog.Left);
            Assert.Equal(96, dialog.Top);
            Assert.Same(dialog, host.ActiveDialog);
        });
    }

    [Fact]
    public void Dialog_ClosesOnEscapeWhenNotMoving()
    {
        StaThread.Run(() =>
        {
            Theme.Apply(_app, ThemeMode.Authentic, 1);
            TurbolandDialogHost host = LaidOutHost();
            TurbolandDialog dialog = new() { Content = new TextBlock { Text = "content" } };
            bool closed = false;
            dialog.Closed += (_, _) => closed = true;

            host.Show(dialog);
            Relayout(host);
            SendKey(dialog, Key.Escape, preview: false);
            Assert.True(closed);
            Assert.False(dialog.DialogResult);
            Assert.Empty(host.Dialogs);
        });
    }

    [Fact]
    public void Dialog_FiresTheDefaultButtonOnEnter()
    {
        StaThread.Run(() =>
        {
            Theme.Apply(_app, ThemeMode.Authentic, 1);
            TurbolandDialogHost host = LaidOutHost();
            Button ok = new() { Content = "OK", IsDefault = true };
            bool clicked = false;
            ok.Click += (_, _) => clicked = true;

            TurbolandDialog dialog = new() { Content = ok };
            host.Show(dialog);
            Relayout(host);

            SendKey(dialog, Key.Enter, preview: false);

            // Button.IsDefault resolves against the focus scope root, which is normally a
            // Window - and this dialog is not one, so the dialog invokes it itself.
            Assert.True(clicked);
        });
    }

    [Fact]
    public void Dialog_FiresTheCancelButtonOnEscapeRatherThanBypassingIt()
    {
        StaThread.Run(() =>
        {
            Theme.Apply(_app, ThemeMode.Authentic, 1);
            TurbolandDialogHost host = LaidOutHost();
            Button cancel = new() { Content = "Cancel", IsCancel = true };
            bool clicked = false;
            cancel.Click += (_, _) => clicked = true;

            TurbolandDialog dialog = new() { Content = cancel };
            host.Show(dialog);
            Relayout(host);

            SendKey(dialog, Key.Escape, preview: false);

            // A Cancel button may carry cleanup of its own, so Esc must run it rather
            // than tearing the dialog down behind its back.
            Assert.True(clicked);
        });
    }

    [Fact]
    public void Dialog_KeepsItsShadowOutsideItsOwnBounds()
    {
        StaThread.Run(() =>
        {
            Theme.Apply(_app, ThemeMode.Authentic, 1);
            TurbolandDialogHost host = LaidOutHost();
            TurbolandDialog dialog = new() { Content = new TextBlock { Text = "content" } };
            host.Show(dialog, new Point(0, 0));
            Relayout(host);

            // The shadow must not inflate the dialog: it is drawn with a margin whose
            // right and bottom are negative, so it spills outside instead. If it ever
            // starts contributing width, the frame stops matching the character grid.
            var shadow = (FrameworkElement)dialog.Template.FindName("shadow", dialog);
            var face = (FrameworkElement)dialog.Template.FindName("face", dialog);

            Assert.Equal(face.ActualWidth, shadow.ActualWidth);
            Assert.Equal(face.ActualHeight, shadow.ActualHeight);

            Point offset = shadow.TranslatePoint(new Point(0, 0), face);
            Assert.Equal(2 * dialog.CellWidth, offset.X);
            Assert.Equal(dialog.CellHeight, offset.Y);
        });
    }

    [Fact]
    public void Dialog_DoesNotShowAZoomBox()
    {
        StaThread.Run(() =>
        {
            Theme.Apply(_app, ThemeMode.Authentic, 1);
            TurbolandDialogHost host = LaidOutHost();
            TurbolandDialog dialog = new();
            host.Show(dialog);
            Relayout(host);

            // A Turboland dialog has no zoom box: its top-right corner is plain frame,
            // and an in-client dialog has nothing to maximise against anyway.
            Assert.Null(dialog.Template.FindName("PART_ZoomBox", dialog));
        });
    }

    private static void SendKey(TurbolandDialog dialog, Key key, bool preview = true)
    {
        // KeyEventArgs demands a live PresentationSource, and PresentationSource.FromVisual
        // returns null without an HWND, so a hidden HwndSource stands in. The handlers only
        // read e.Key, so it needs to exist rather than to contain anything.
        using HwndSource source = new(new HwndSourceParameters("dialog-test")
        {
            Width = 1,
            Height = 1,
        });

        KeyEventArgs args = new(Keyboard.PrimaryDevice, source, 0, key)
        {
            RoutedEvent = preview ? Keyboard.PreviewKeyDownEvent : Keyboard.KeyDownEvent,
        };

        dialog.RaiseEvent(args);
    }
}
