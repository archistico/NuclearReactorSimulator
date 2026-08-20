using NuclearReactorSimulator.Application;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests;

public sealed class ApplicationDescriptorTests
{
    [Fact]
    public void Current_DescribesM10964InitialOperationalChallengePackCandidate()
    {
        var descriptor = ApplicationDescriptor.Current;

        Assert.Equal("Nuclear Reactor Simulator", descriptor.ProductName);
        Assert.Contains("M10.9.6.4", descriptor.Milestone, StringComparison.Ordinal);
        Assert.Contains("Initial Challenge Packs", descriptor.Milestone, StringComparison.Ordinal);
        Assert.Contains("M10.9.6.3", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("integrated-operations-desktop-stable@4", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("six versioned", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("5-to-10-to-5", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("Only bounded demand-following exposes", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("synchronization owns no demand profile", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("never writes generator requested load", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("no score arithmetic", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("generator trip is required evidence", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("No hard failure deadlines", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("no dispatcher", descriptor.Status, StringComparison.Ordinal);
        Assert.Contains("no new fault or physics", descriptor.Status, StringComparison.Ordinal);
    }
}
