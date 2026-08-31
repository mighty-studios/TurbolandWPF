using System;
using System.Threading;
using System.Windows.Threading;

namespace TurbolandTheme.Tests.VisualRegression;

/// <summary>
/// Runs UI work on one shared STA thread with a live <see cref="Dispatcher"/>.
/// </summary>
/// <remarks>
/// <para>
/// WPF refuses to construct a <c>FrameworkElement</c> off an STA thread, and xunit
/// runs tests on MTA worker threads, so any test that instantiates a control has to
/// marshal. The thread is created once and reused: unfrozen <c>Freezable</c>s (every
/// brush the theme parses) have thread affinity, so parsing the theme on one thread
/// and building controls on another would fail at random.
/// </para>
/// <para>
/// Do <b>not</b> construct a fresh <c>Application</c> on the worker thread. WPF allows
/// only one per AppDomain and the second constructor throws. Use the collection
/// fixture's instance instead.
/// </para>
/// </remarks>
internal static class StaThread
{
    private static readonly object Gate = new();
    private static Dispatcher? _dispatcher;

    public static void Run(Action action)
    {
        var dispatcher = EnsureDispatcher();

        // Invoke marshals any exception back to the caller, so assertion
        // failures inside the action still fail the test properly.
        dispatcher.Invoke(action, DispatcherPriority.Normal);
    }

    private static Dispatcher EnsureDispatcher()
    {
        lock (Gate)
        {
            if (_dispatcher != null)
                return _dispatcher!;

            var ready = new ManualResetEventSlim(false);

            var thread = new Thread(() =>
            {
                _dispatcher = Dispatcher.CurrentDispatcher;
                ready.Set();
                Dispatcher.Run();
            })
            {
                Name = "wpf-sta-test",
                IsBackground = true,
            };

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();

            if (!ready.Wait(TimeSpan.FromSeconds(15)))
                throw new InvalidOperationException("STA test thread failed to start.");

            return _dispatcher!;
        }
    }
}
