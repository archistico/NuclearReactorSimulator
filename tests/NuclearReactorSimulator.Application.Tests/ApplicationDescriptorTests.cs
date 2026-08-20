using NuclearReactorSimulator.Application;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests;

public sealed class ApplicationDescriptorTests
{
    [Fact]
    public void Current_DescribesM10962DeterministicExternalEnergyDemandCandidate()
    {
        var descriptor = ApplicationDescriptor.Current;

        Assert.Equal("Nuclear Reactor Simulator", descriptor.ProductName);
        Assert.Contains("M10.9.6.2", descriptor.Milestone, StringComparison.Ordinal);
        Assert.Contains("External Energy-Demand", descriptor.Milestone, StringComparison.Ordinal);
        Assert.Contains("M10.9.6.1", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("integrated-operations-desktop-stable@4", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("EXTERNAL GRID DEMAND", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("generator requested load", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("actual electrical output", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("logical-step-only", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("Demand is unavailable", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("future-schedule visibility is definition-owned", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("no dispatcher", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("No score arithmetic", descriptor.Status, StringComparison.Ordinal);
    }
}
