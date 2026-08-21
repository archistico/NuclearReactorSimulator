using NuclearReactorSimulator.Application;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests;

public sealed class ApplicationDescriptorTests
{
    [Fact]
    public void Current_DescribesM10973Hotfix1Rev2LiveMissionPerformanceWorkspaceCandidate()
    {
        var descriptor = ApplicationDescriptor.Current;

        Assert.Equal("Nuclear Reactor Simulator", descriptor.ProductName);
        Assert.Contains("M10.9.7.3", descriptor.Milestone, StringComparison.Ordinal);
        Assert.Contains("Hotfix 1 REV2", descriptor.Milestone, StringComparison.Ordinal);
        Assert.Contains("Live Mission / Performance Historical Shell Contract Alignment", descriptor.Milestone, StringComparison.Ordinal);
        Assert.Contains("M10.9.7.2 Hotfix 3 REV1 are validated", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("MISSION / Mission & Performance", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("F1-F8", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("no F9", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("every deterministic step", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("presentation cadence", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("Explicit structural presentation comparison", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("GRID DEMAND, REQUESTED LOAD and ACTUAL OUTPUT remain separate", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("stale batch-presentation ordering defect", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("RuntimeProgressText", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("does not infer or invent a challenge pack", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("Archive-restored mission binding/timeline equivalence remains M10.9.7.4 scope", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("No new challenge definition, scoring arithmetic, protection authority, plant command authority or physics change", descriptor.Status, StringComparison.Ordinal);
    }
}
