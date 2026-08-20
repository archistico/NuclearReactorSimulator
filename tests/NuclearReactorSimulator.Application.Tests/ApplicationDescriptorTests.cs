using NuclearReactorSimulator.Application;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests;

public sealed class ApplicationDescriptorTests
{
    [Fact]
    public void Current_DescribesM10963MultidimensionalScoringCandidate()
    {
        var descriptor = ApplicationDescriptor.Current;

        Assert.Equal("Nuclear Reactor Simulator", descriptor.ProductName);
        Assert.Contains("M10.9.6.3", descriptor.Milestone, StringComparison.Ordinal);
        Assert.Contains("Scoring Contract", descriptor.Milestone, StringComparison.Ordinal);
        Assert.Contains("M10.9.6.2", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("integrated-operations-desktop-stable@4", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("general-operations@1", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("demand-following@1", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("SAFETY 45", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("DEMAND 15", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("60/75/90", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("39 percent", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("59 percent", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("not globally classified", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("neutral 1.00", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("no dispatcher", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("No challenge pack", descriptor.Status, StringComparison.Ordinal);
    }
}
