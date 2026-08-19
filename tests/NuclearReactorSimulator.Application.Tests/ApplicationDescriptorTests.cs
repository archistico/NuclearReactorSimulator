using NuclearReactorSimulator.Application;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests;

public sealed class ApplicationDescriptorTests
{
    [Fact]
    public void Current_DescribesI1ProfileCompatibilityLegacyRetirementInventoryCandidate()
    {
        var descriptor = ApplicationDescriptor.Current;

        Assert.Equal("Nuclear Reactor Simulator", descriptor.ProductName);
        Assert.Contains("M10.9.4.1-I.1", descriptor.Milestone);
        Assert.Contains("Profile Compatibility", descriptor.Milestone);
        Assert.Contains("Legacy Retirement Inventory", descriptor.Milestone);
        Assert.Contains("H.30 is user-validated", descriptor.Status);
        Assert.Contains("Phase H is closed", descriptor.Status);
        Assert.Contains("OPT-IN ONLY", descriptor.Status);
        Assert.Contains("exact v2 ExplicitCommittedState", descriptor.Status);
        Assert.Contains("exact v3 FourNodeBranchContinuityCorrectedCommitOptIn", descriptor.Status);
        Assert.Contains("12 exact-version", descriptor.Status);
        Assert.Contains("9 profile IDs", descriptor.Status);
        Assert.Contains("audit-only retirement candidates", descriptor.Status);
        Assert.Contains("10 ms fixed step", descriptor.Status);
        Assert.Contains("remain unchanged", descriptor.Status);
    }
}
