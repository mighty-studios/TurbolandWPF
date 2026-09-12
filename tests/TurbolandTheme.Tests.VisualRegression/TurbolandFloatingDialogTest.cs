using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using TurbolandTheme.Wpf.Controls;
using Xunit;
using Theme = TurbolandTheme.Wpf.TurbolandTheme;
using ThemeMode = TurbolandTheme.Core.ThemeMode;

namespace TurbolandTheme.Tests.VisualRegression;

/// <summary>
/// Guards <see cref="TurbolandFloatingDialog"/>: the true-window counterpart of the
/// in-client dialog. Its whole reason for existing is the transparent-window shadow
/// trick and the owner-based sorting, and both fail silently - a wrong margin lands the
/// shadow off-grid, and a wrong Background fills the shadow room with an opaque box.
/// </summary>
[Collection("Wpf")]
public class TurbolandFloatingDialogTest
{
    private readonly Application _app;

    public TurbolandFloatingDialogTest(WpfApplicationFixture fixture) => _app = fixture.App;

    [Fact]
    public void FloatingDialog_ResolvesItsThemeStyleAndTemplate()
    {
        StaThread.Run(() =>
        {
            Theme.Apply(_app, ThemeMode.Authentic, 1);
            TurbolandFloatingDialog dialog = new();

            Assert.NotNull(dialog.Style);
            Assert.NotNull(dialog.Template);
        });
    }

    [Fact]
    public void FloatingDialog_IsATransparentBorderlessOwnedWindow()
    {
        StaThread.Run(() =>
        {
            Theme.Apply(_app, ThemeMode.Authentic, 1);
            TurbolandFloatingDialog dialog = new();

            // The transparent-window trick: AllowsTransparency requires WindowStyle.None,
            // and a non-transparent Background would fill the shadow's reserved margin
            // with an opaque rectangle. These are local values in the constructor, so a
            // subclass or stray style cannot quietly undo them.
            Assert.Equal(WindowStyle.None, dialog.WindowStyle);
            Assert.True(dialog.AllowsTransparency);
            Assert.Equal(Brushes.Transparent, dialog.Background);

            // Dialog identity: no resize, no taskbar entry, sized to its content,
            // centred on its owner.
            Assert.Equal(ResizeMode.NoResize, dialog.ResizeMode);
            Assert.False(dialog.ShowInTaskbar);
            Assert.Equal(SizeToContent.WidthAndHeight, dialog.SizeToContent);
            Assert.Equal(WindowStartupLocation.CenterOwner, dialog.WindowStartupLocation);
        });
    }

    [Fact]
    public void FloatingDialog_ExposesItsTitleBarAndCloseBoxAsTemplateParts()
    {
        StaThread.Run(() =>
        {
            Theme.Apply(_app, ThemeMode.Authentic, 1);
            TurbolandFloatingDialog dialog = new() { Title = "Confirm" };
            dialog.Content = new TextBlock { Text = "x" };
            dialog.ApplyTemplate();

            // Dragging and closing are wired to these by name; a rename in the template
            // would leave a dialog that cannot be moved or dismissed.
            Assert.NotNull(dialog.Template.FindName(TurbolandFloatingDialog.TitleBarPart, dialog));
            Assert.NotNull(dialog.Template.FindName(TurbolandFloatingDialog.CloseBoxPart, dialog));
        });
    }

    [Fact]
    public void FloatingDialog_TakesItsCellMetricsFromTheScaledTheme()
    {
        StaThread.Run(() =>
        {
            Theme.Apply(_app, ThemeMode.Authentic, 2);
            TurbolandFloatingDialog dialog = new();
            dialog.ApplyTemplate();

            // Keyboard movement quantises by these, so at 2x it must step 18x32,
            // not 9x16.
            Assert.Equal(18, dialog.CellWidth);
            Assert.Equal(32, dialog.CellHeight);

            Theme.Apply(_app, ThemeMode.Authentic, 1);
        });
    }

    [Fact]
    public void FloatingDialog_ShadowIsContentOffsetTwoCellsRightAndOneRowDown()
    {
        StaThread.Run(() =>
        {
            Theme.Apply(_app, ThemeMode.Authentic, 1);

            // Shown, not just measured: a Window lays nothing out until it has an HWND,
            // and showing is also the only way to prove the transparent-window
            // combination (None + AllowsTransparency + transparent Background) actually
            // creates a source instead of throwing at Show time.
            TurbolandFloatingDialog dialog = new()
            {
                Title = "Confirm",
                Content = new TextBlock { Text = "content" },
                ShowActivated = false,
            };

            ShowAndPump(dialog);

            var shadow = (FrameworkElement)dialog.Template.FindName("shadow", dialog);
            var face = (FrameworkElement)dialog.Template.FindName("face", dialog);

            // Same rectangle translated by the shadow vector - the same 2-cell/1-row
            // offset as the in-client dialog, but INSIDE the window this time, because
            // the transparent HWND gives the shadow somewhere to be.
            Assert.True(face.ActualWidth > 0);
            Assert.Equal(face.ActualWidth, shadow.ActualWidth);
            Assert.Equal(face.ActualHeight, shadow.ActualHeight);

            Point offset = shadow.TranslatePoint(new Point(0, 0), face);
            Assert.Equal(2 * dialog.CellWidth, offset.X);
            Assert.Equal(dialog.CellHeight, offset.Y);

            // The window is the face plus the shadow room it reserves. If the shadow
            // ever stops contributing to the measure, it is being clipped by the HWND
            // edge and the dialog renders without one.
            Assert.Equal(face.ActualWidth + 2 * dialog.CellWidth, dialog.ActualWidth);
            Assert.Equal(face.ActualHeight + dialog.CellHeight, dialog.ActualHeight);

            dialog.Close();
        });
    }

    [Fact]
    public void FloatingDialog_ResizesToContentPlusShadowRoom()
    {
        StaThread.Run(() =>
        {
            Theme.Apply(_app, ThemeMode.Authentic, 2);

            TurbolandFloatingDialog dialog = new()
            {
                Title = "Confirm",
                Content = new TextBlock { Text = "content" },
                ShowActivated = false,
            };

            ShowAndPump(dialog);

            // SizeToContent must include the shadow margins, or the 2x shadow is
            // clipped to nothing at the window edge.
            var face = (FrameworkElement)dialog.Template.FindName("face", dialog);
            Assert.Equal(face.ActualWidth + 2 * dialog.CellWidth, dialog.ActualWidth);
            Assert.Equal(face.ActualHeight + dialog.CellHeight, dialog.ActualHeight);

            dialog.Close();
            Theme.Apply(_app, ThemeMode.Authentic, 1);
        });
    }

    /// <summary>Shows a window and waits for its first render pass to complete.</summary>
    private static void ShowAndPump(Window window)
    {
        DispatcherFrame frame = new();
        window.ContentRendered += (_, _) => frame.Continue = false;

        // Belt and braces: with no interactive desktop the render pass may never run,
        // and a hanging test suite is worse than one that fails on the next assert.
        DispatcherTimer timeout = new() { Interval = TimeSpan.FromSeconds(5) };
        timeout.Tick += (_, _) => frame.Continue = false;
        timeout.Start();

        window.Show();
        Dispatcher.PushFrame(frame);
        timeout.Stop();
    }

    [Fact]
    public void FloatingDialog_BindsCtrlF5ToMoveMode()
    {
        StaThread.Run(() =>
        {
            Theme.Apply(_app, ThemeMode.Authentic, 1);
            TurbolandFloatingDialog dialog = new();

            KeyBinding binding = dialog.InputBindings
                .OfType<KeyBinding>()
                .Single(b => b.Key == Key.F5);

            Assert.Equal(ModifierKeys.Control, binding.Modifiers);
            Assert.Same(TurbolandFloatingDialog.MoveCommand, binding.Command);
        });
    }

    [Fact]
    public void FloatingDialog_MovesByWholeCellsInKeyboardMoveMode()
    {
        StaThread.Run(() =>
        {
            Theme.Apply(_app, ThemeMode.Authentic, 1);
            TurbolandFloatingDialog dialog = new();

            dialog.Left = 100;
            dialog.Top = 96;

            TurbolandFloatingDialog.MoveCommand.Execute(null, dialog);
            Assert.True(dialog.IsMoveMode);

            SendKey(dialog, Key.Right);
            SendKey(dialog, Key.Down);

            // Entering move mode snaps onto the grid first: on the 9px grid 100 rounds
            // to 99, the nearest column, so the step starts there.
            Assert.Equal(99 + dialog.CellWidth, dialog.Left);
            Assert.Equal(96 + dialog.CellHeight, dialog.Top);

            SendKey(dialog, Key.Enter);
            Assert.False(dialog.IsMoveMode);
        });
    }

    [Fact]
    public void FloatingDialog_RevertsAKeyboardMoveOnEscape()
    {
        StaThread.Run(() =>
        {
            Theme.Apply(_app, ThemeMode.Authentic, 1);
            TurbolandFloatingDialog dialog = new();

            dialog.Left = 104;
            dialog.Top = 96;

            dialog.BeginMove();
            SendKey(dialog, Key.Right);
            SendKey(dialog, Key.Right);
            SendKey(dialog, Key.Escape);

            // Esc in move mode reverts the move and must NOT close the dialog.
            Assert.False(dialog.IsMoveMode);
            Assert.Equal(104, dialog.Left);
            Assert.Equal(96, dialog.Top);
        });
    }

    [Fact]
    public void FloatingDialog_CloseResultIsSkippedWhenNotShownModally()
    {
        StaThread.Run(() =>
        {
            Theme.Apply(_app, ThemeMode.Authentic, 1);
            TurbolandFloatingDialog dialog = new();

            // Window.DialogResult throws unless the window was shown with ShowDialog.
            // Close(bool?) is the API subclasses reach for from button handlers, so it
            // has to no-op the result on a modeless window instead of throwing.
            dialog.Close(false);
            Assert.Null(dialog.DialogResult);
        });
    }

    private static void SendKey(TurbolandFloatingDialog dialog, Key key, bool preview = true)
    {
        // KeyEventArgs demands a live PresentationSource, so a hidden HwndSource stands
        // in - same trick as TurbolandDialogTest.
        using HwndSource source = new(new HwndSourceParameters("floating-dialog-test")
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
