using NuclearReactorSimulator.Application;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests;

public sealed class ApplicationDescriptorTests
{
    [Fact]
    public void Current_DescribesM10961ChallengeLifecycleLogicalTimeCandidate()
    {
        var descriptor = ApplicationDescriptor.Current;

        Assert.Equal("Nuclear Reactor Simulator", descriptor.ProductName);
        Assert.Contains("M10.9.6.1", descriptor.Milestone, StringComparison.Ordinal);
        Assert.Contains("Challenge Lifecycle", descriptor.Milestone, StringComparison.Ordinal);
        Assert.Contains("Phase I and M10.9.5", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("integrated-operations-desktop-stable@4", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("CorrelationConsistentInverseDomain", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("FourNodeBranchContinuityCorrectedCommitOptIn", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("exact @3 remains historical", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("exact @2 remains fail-closed", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("pre-synchronization-grid-loading@3", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("read-only evidence seam", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("NOT STARTED -> READY -> ACTIVE", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("hard logical-step deadline", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("No external demand profile", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("score arithmetic", descriptor.Status, StringComparison.Ordinal);
    }
}
