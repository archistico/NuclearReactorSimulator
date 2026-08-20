using NuclearReactorSimulator.Application;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests;

public sealed class ApplicationDescriptorTests
{
    [Fact]
    public void Current_DescribesI4KnownLimitationsAndLegacyRetirementReviewCandidate()
    {
        var descriptor = ApplicationDescriptor.Current;

        Assert.Equal("Nuclear Reactor Simulator", descriptor.ProductName);
        Assert.Contains("M10.9.4.1-I.4", descriptor.Milestone, StringComparison.Ordinal);
        Assert.Contains("Known Limitations", descriptor.Milestone, StringComparison.Ordinal);
        Assert.Contains("Legacy Retirement Review", descriptor.Milestone, StringComparison.Ordinal);
        Assert.Contains("I.3 Hotfix 2 is validated", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("exact v3 FourNodeBranchContinuityCorrectedCommitOptIn", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("exact v2 ExplicitCommittedState", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("seven final-window slopes", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("19 regression budgets", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("DeterministicHybridSemiImplicit", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("FourNodeBranchContinuityShadowIntegrated", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("source removal is deferred", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("bounded-but-costly", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("10 ms", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("Gameplay/Evidence", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("eng/frozen-evidence/ordinary", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("eng/evidence-manifests", descriptor.Status, StringComparison.Ordinal);
    }
}
