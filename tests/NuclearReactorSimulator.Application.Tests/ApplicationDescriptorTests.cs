using NuclearReactorSimulator.Application;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests;

public sealed class ApplicationDescriptorTests
{
    [Fact]
    public void Current_DescribesI3AuthoritativeProductionReferenceBaselineCandidate()
    {
        var descriptor = ApplicationDescriptor.Current;

        Assert.Equal("Nuclear Reactor Simulator", descriptor.ProductName);
        Assert.Contains("M10.9.4.1-I.3", descriptor.Milestone, StringComparison.Ordinal);
        Assert.Contains("Authoritative Production Reference Trajectory", descriptor.Milestone, StringComparison.Ordinal);
        Assert.Contains("Conservation/Inventory", descriptor.Milestone, StringComparison.Ordinal);
        Assert.Contains("Tolerance Baseline", descriptor.Milestone, StringComparison.Ordinal);
        Assert.Contains("H.30 Requalification 1 is validated with ACTIVATE", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("exact v3 FourNodeBranchContinuityCorrectedCommitOptIn", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("exact v2 ExplicitCommittedState", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("bounded-but-costly", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("10 ms", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("300 seconds / 30,000 steps", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("seven final-window slopes", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("19 versioned internal regression tolerance budgets", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("not retuned", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("Gameplay/Evidence", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("eng/frozen-evidence/ordinary", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("eng/frozen-evidence/large-payload-manifest.csv", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("eng/evidence-manifests", descriptor.Status, StringComparison.Ordinal);
    }
}
