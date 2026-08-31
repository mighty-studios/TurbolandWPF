using System.Windows;
using Xunit;

namespace TurbolandTheme.Tests.VisualRegression;

/// <summary>
/// Empirically determines WPF resource-dictionary precedence so that
/// TurbolandTheme.Apply can layer overrides in a direction-independent way.
/// These are probes: the assertions encode the hypothesis, and a failure
/// message reveals the actual behavior.
/// </summary>
public class PrecedenceProbe
{
    [Fact]
    public void Probe_MergedDictionaryDirection()
    {
        var baseDict = new ResourceDictionary();
        baseDict["ProbeKey"] = "BASE";

        var overrideDict = new ResourceDictionary();
        overrideDict["ProbeKey"] = "OVERRIDE";

        var parent = new ResourceDictionary();
        parent.MergedDictionaries.Add(baseDict);
        parent.MergedDictionaries.Add(overrideDict);

        object? result = parent["ProbeKey"];
        // Hypothesis: the LAST merged dictionary wins (standard override pattern).
        Assert.Equal("OVERRIDE", result);
    }

    [Fact]
    public void Probe_OwnEntriesBeatMerged()
    {
        var merged = new ResourceDictionary();
        merged["ProbeKey"] = "MERGED";

        var parent = new ResourceDictionary();
        parent["ProbeKey"] = "OWN";
        parent.MergedDictionaries.Add(merged);

        object? result = parent["ProbeKey"];
        // Hypothesis: a dictionary's own entries always beat its merged dictionaries.
        Assert.Equal("OWN", result);
    }
}
