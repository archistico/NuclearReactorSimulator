using NuclearReactorSimulator.Application;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests;

public sealed class ApplicationDescriptorTests
{
    [Fact]
    public void Current_DescribesM10954ObservedResponseEvidenceCandidate()
    {
        var descriptor = ApplicationDescriptor.Current;

        Assert.Equal("Nuclear Reactor Simulator", descriptor.ProductName);
        Assert.Contains("M10.9.5.4", descriptor.Milestone, StringComparison.Ordinal);
        Assert.Contains("Observed Response Evidence", descriptor.Milestone, StringComparison.Ordinal);
        Assert.Contains("Phase I is validated and closed", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("integrated-operations-desktop-stable@4", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("CorrelationConsistentInverseDomain", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("FourNodeBranchContinuityCorrectedCommitOptIn", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("exact @3 remains historical", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("exact @2 remains fail-closed", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("pre-synchronization-grid-loading@3", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("M10.9.5.1", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("M10.9.5.2", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("M10.9.5.3", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("500-logical-step", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("baseline/latest", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("Rejected commands show no fictional plant effects", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("JsonIgnored presentation evidence", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("M10.9.5.5", descriptor.Status, StringComparison.Ordinal);
    }
}
