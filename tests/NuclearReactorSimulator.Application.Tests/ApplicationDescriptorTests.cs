using NuclearReactorSimulator.Application;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests;

public sealed class ApplicationDescriptorTests
{
    [Fact]
    public void Current_DescribesI3Hotfix4BranchDiscontinuityComparisonCandidate()
    {
        var descriptor = ApplicationDescriptor.Current;

        Assert.Equal("Nuclear Reactor Simulator", descriptor.ProductName);
        Assert.Contains("M10.9.4.1-I.3", descriptor.Milestone);
        Assert.Contains("Reference Trajectories", descriptor.Milestone);
        Assert.Contains("Conservation/Inventory", descriptor.Milestone);
        Assert.Contains("Tolerance Budgets", descriptor.Milestone);
        Assert.Contains("Hotfix 4", descriptor.Milestone);
        Assert.Contains("Explicit-vs-Corrected Branch Discontinuity Comparison", descriptor.Milestone);
        Assert.Contains("Classifier Fix 1", descriptor.Milestone);
        Assert.Contains("Targeted-Train Reverse-Flow Classification", descriptor.Milestone);
        Assert.Contains("I.2 is user-validated", descriptor.Status);
        Assert.Contains("H.30 remains closed as OPT-IN ONLY", descriptor.Status);
        Assert.Contains("exact v2 ExplicitCommittedState", descriptor.Status);
        Assert.Contains("exact v3 FourNodeBranchContinuityCorrectedCommitOptIn", descriptor.Status);
        Assert.Contains("300-second healthy reference journey", descriptor.Status);
        Assert.Contains("338 exact-v2 generation-drop steps", descriptor.Status);
        Assert.Contains("8 coincide with reverse stop-valve flow", descriptor.Status);
        Assert.Contains("330 with reverse admission flow", descriptor.Status);
        Assert.Contains("zero targeted-train reverse-flow steps", descriptor.Status);
        Assert.Contains("final-window slopes", descriptor.Status);
        Assert.Contains("H.24/H.28 are not rerun", descriptor.Status);
        Assert.Contains("10 ms fixed step", descriptor.Status);
        Assert.Contains("remain unchanged", descriptor.Status);
    }
}
