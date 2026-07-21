using Xunit;

namespace NuclearReactorSimulator.Application.Tests;

public sealed class ApplicationDescriptorTests
{
    [Fact]
    public void CurrentDescriptor_IdentifiesM76CandidateAfterValidatedM75()
    {
        var descriptor = global::NuclearReactorSimulator.Application.ApplicationDescriptor.Current;

        Assert.Equal("Nuclear Reactor Simulator", descriptor.ProductName);
        Assert.Contains("M7.6", descriptor.Milestone);
        Assert.Contains("Baseline candidate", descriptor.Status);
        Assert.Contains("M7.5 validated", descriptor.Status);
        Assert.Contains("normal-shutdown", descriptor.Status);
    }
}
