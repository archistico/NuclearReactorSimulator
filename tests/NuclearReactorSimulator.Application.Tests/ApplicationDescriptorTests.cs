using NuclearReactorSimulator.Application;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests;

public sealed class ApplicationDescriptorTests
{
    [Fact]
    public void Current_DescribesH30RequalificationProductionPolicyRereviewCandidate()
    {
        var descriptor = ApplicationDescriptor.Current;

        Assert.Equal("Nuclear Reactor Simulator", descriptor.ProductName);
        Assert.Contains("M10.9.4.1-H.30 Requalification 1", descriptor.Milestone, StringComparison.Ordinal);
        Assert.Contains("Production Policy Re-review", descriptor.Milestone, StringComparison.Ordinal);
        Assert.Contains("I.3 Continuity Evidence", descriptor.Milestone, StringComparison.Ordinal);
        Assert.Contains("I.2 remains the last fully validated", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("338/338 exact-v2 generation-drop steps", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("zero drops and zero targeted reverse flow", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("300-second / 30,000-step", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("3,757 corrected commits", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("proposes ACTIVATE", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("exact v3 FourNodeBranchContinuityCorrectedCommitOptIn", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("exact v2 ExplicitCommittedState", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("bounded-but-costly", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("10 ms fixed step", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("I.3 tolerance budgets remain unfrozen", descriptor.Status, StringComparison.Ordinal);
    }
}
