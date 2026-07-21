using Xunit;

namespace NuclearReactorSimulator.Application.Tests;

public sealed class ApplicationDescriptorTests
{
    [Fact]
    public void CurrentDescriptor_IdentifiesM81CandidateAfterValidatedM77AndClosedM7Gate()
    {
        var descriptor = global::NuclearReactorSimulator.Application.ApplicationDescriptor.Current;

        Assert.Equal("Nuclear Reactor Simulator", descriptor.ProductName);
        Assert.Contains("M8.1", descriptor.Milestone);
        Assert.Contains("Baseline candidate", descriptor.Status);
        Assert.Contains("M7.7 validated", descriptor.Status);
        Assert.Contains("M7 complete", descriptor.Status);
        Assert.True(descriptor.Status.Contains("fault", StringComparison.OrdinalIgnoreCase));
    }
}
