using Xunit;

namespace NuclearReactorSimulator.Application.Tests;

public sealed class ApplicationDescriptorTests
{
    [Fact]
    public void CurrentDescriptor_IdentifiesM82CandidateAfterValidatedM81()
    {
        var descriptor = global::NuclearReactorSimulator.Application.ApplicationDescriptor.Current;

        Assert.Equal("Nuclear Reactor Simulator", descriptor.ProductName);
        Assert.Contains("M8.2", descriptor.Milestone);
        Assert.Contains("Baseline candidate", descriptor.Status);
        Assert.Contains("M8.1 validated", descriptor.Status);
        Assert.True(descriptor.Status.Contains("fault", StringComparison.OrdinalIgnoreCase));
    }
}
