using System.Windows;
using Xunit;

namespace TurbolandTheme.Tests.VisualRegression;

/// <summary>
/// WPF allows only one Application per AppDomain, and xunit runs every test
/// class in the same AppDomain. This fixture hands every test in the
/// collection the same Application instance. Parallelization is disabled in
/// xunit.runner.json so the single instance is never touched concurrently.
/// </summary>
public sealed class WpfApplicationFixture : IDisposable
{
    public Application App { get; } = new Application();

    public void Dispose()
    {
        // Intentionally not shutting down: the next test class in the same
        // AppDomain still needs a live Application.
    }
}

[CollectionDefinition("Wpf")]
public sealed class WpfCollection : ICollectionFixture<WpfApplicationFixture>
{
}
