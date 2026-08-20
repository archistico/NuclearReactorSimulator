using NuclearReactorSimulator.Application;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests;

public sealed class ApplicationDescriptorTests
{
    [Fact]
    public void Current_DescribesI5FinalRepairedV4PhaseIClosure()
    {
        var descriptor = ApplicationDescriptor.Current;

        Assert.Equal("Nuclear Reactor Simulator", descriptor.ProductName);
        Assert.Contains("M10.9.4.1-I.5", descriptor.Milestone, StringComparison.Ordinal);
        Assert.Contains("Repaired-v4", descriptor.Milestone, StringComparison.Ordinal);
        Assert.Contains("Phase-I Closure", descriptor.Milestone, StringComparison.Ordinal);
        Assert.Contains("production activation is validated", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("integrated-operations-desktop-stable@4", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("CorrelationConsistentInverseDomain", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("FourNodeBranchContinuityCorrectedCommitOptIn", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("Exact v3", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("exact v2", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("pre-synchronization-grid-loading", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("10 s stabilization", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("20-60 s sustained", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("300-second evidence", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("19 frozen budgets", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("10 ms", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("Final Phase-I closure", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("unchanged budgets", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("GameplayLong", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("OperationalEnvelope", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("ReferencePlant", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("Gameplay/Evidence", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("eng/frozen-evidence/ordinary", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("eng/evidence-manifests", descriptor.Status, StringComparison.Ordinal);
    }
}
