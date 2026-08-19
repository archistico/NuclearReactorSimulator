using NuclearReactorSimulator.Application;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests;

public sealed class ApplicationDescriptorTests
{
    [Fact]
    public void Current_DescribesI2AuditConsolidationCiBaselineCandidate()
    {
        var descriptor = ApplicationDescriptor.Current;

        Assert.Equal("Nuclear Reactor Simulator", descriptor.ProductName);
        Assert.Contains("M10.9.4.1-I.2", descriptor.Milestone);
        Assert.Contains("Audit Consolidation", descriptor.Milestone);
        Assert.Contains("CI Baseline", descriptor.Milestone);
        Assert.Contains("I.1 Hotfix 1 is user-validated", descriptor.Status);
        Assert.Contains("H.30 remains closed as OPT-IN ONLY", descriptor.Status);
        Assert.Contains("exact v2 ExplicitCommittedState", descriptor.Status);
        Assert.Contains("exact v3 FourNodeBranchContinuityCorrectedCommitOptIn", descriptor.Status);
        Assert.Contains("ordinary/current-evidence/scheduled-long/historical-frozen", descriptor.Status);
        Assert.Contains("H.5 hybrid", descriptor.Status);
        Assert.Contains("H.21 shadow", descriptor.Status);
        Assert.Contains("10 ms fixed step", descriptor.Status);
        Assert.Contains("remain unchanged", descriptor.Status);
    }
}
