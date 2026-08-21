using NuclearReactorSimulator.Application;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests;

public sealed class ApplicationDescriptorTests
{
    [Fact]
    public void Current_DescribesM10974DeterministicMissionTimelineCandidate()
    {
        var descriptor = ApplicationDescriptor.Current;

        Assert.Equal("Nuclear Reactor Simulator", descriptor.ProductName);
        Assert.Contains("M10.9.7.4", descriptor.Milestone, StringComparison.Ordinal);
        Assert.Contains("Hotfix 1", descriptor.Milestone, StringComparison.Ordinal);
        Assert.Contains("Deterministic Mission Timeline", descriptor.Milestone, StringComparison.Ordinal);
        Assert.Contains("Hotfix 2 REV2 is VALIDATED", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("sha256-control-room-snapshot-v1", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("lifecycle spine", descriptor.Status, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("recent operational evidence", descriptor.Status, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("presentation-only drill-down", descriptor.Status, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("without plant-command authority", descriptor.Status, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("canonical verified recording prefix", descriptor.Status, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("explicit exact operational challenge pack binding", descriptor.Status, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("no pack is inferred from ScenarioId", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("Archive schema v1", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("F1-F8", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("no-F9", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("run-m10974-mission-performance-timeline-audit.cmd", descriptor.Status, StringComparison.Ordinal);
    }
}
