using NuclearReactorSimulator.Application;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests;

public sealed class ApplicationDescriptorTests
{
    [Fact]
    public void Current_DescribesM10953ContextInspectorSchematicIntegrationCandidate()
    {
        var descriptor = ApplicationDescriptor.Current;

        Assert.Equal("Nuclear Reactor Simulator", descriptor.ProductName);
        Assert.Contains("M10.9.5.3", descriptor.Milestone, StringComparison.Ordinal);
        Assert.Contains("Contextual Command Consequence Model", descriptor.Milestone, StringComparison.Ordinal);
        Assert.Contains("Phase I is validated and closed", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("integrated-operations-desktop-stable@4", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("CorrelationConsistentInverseDomain", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("FourNodeBranchContinuityCorrectedCommitOptIn", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("exact @3 remains historical", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("exact @2 remains fail-closed", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("pre-synchronization-grid-loading@3", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("M10.9.5.1 and M10.9.5.2 are validated", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("DIRECT EFFECT", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("EXPECTED INFLUENCE", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("WHAT TO MONITOR", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("canonical whole-plant mimic", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("explicit ENTER/EXECUTE", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("M10.9.5.4", descriptor.Status, StringComparison.Ordinal);
    }
}
