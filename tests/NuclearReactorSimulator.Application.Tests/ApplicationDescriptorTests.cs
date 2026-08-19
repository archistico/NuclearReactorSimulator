using NuclearReactorSimulator.Application;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests;

public sealed class ApplicationDescriptorTests
{
    [Fact]
    public void Current_DescribesI3Hotfix5CorrectedThreeHundredSecondRequalificationCandidate()
    {
        var descriptor = ApplicationDescriptor.Current;

        Assert.Equal("Nuclear Reactor Simulator", descriptor.ProductName);
        Assert.Contains("M10.9.4.1-I.3", descriptor.Milestone);
        Assert.Contains("Reference Trajectories", descriptor.Milestone);
        Assert.Contains("Conservation/Inventory", descriptor.Milestone);
        Assert.Contains("Tolerance Budgets", descriptor.Milestone);
        Assert.Contains("Hotfix 5", descriptor.Milestone);
        Assert.Contains("Corrected 300 s Healthy Reference Requalification", descriptor.Milestone);
        Assert.Contains("I.2 is user-validated", descriptor.Status);
        Assert.Contains("H.30 remains closed as OPT-IN ONLY", descriptor.Status);
        Assert.Contains("exact v2 ExplicitCommittedState", descriptor.Status);
        Assert.Contains("exact v3 FourNodeBranchContinuityCorrectedCommitOptIn", descriptor.Status);
        Assert.Contains("338/338 exact-v2 generation drops", descriptor.Status);
        Assert.Contains("zero drops and zero targeted-train reverse-flow steps", descriptor.Status);
        Assert.Contains("full 300-second healthy reference horizon", descriptor.Status);
        Assert.Contains("every 10 ms step", descriptor.Status);
        Assert.Contains("final-window slopes", descriptor.Status);
        Assert.Contains("I.3 tolerance budgets remain unfrozen", descriptor.Status);
        Assert.Contains("separate H.30 production-policy re-review", descriptor.Status);
        Assert.Contains("H.24/H.28 are not rerun", descriptor.Status);
        Assert.Contains("10 ms fixed step", descriptor.Status);
        Assert.Contains("remain unchanged", descriptor.Status);
    }
}
