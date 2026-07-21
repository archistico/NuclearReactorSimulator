using Xunit;

namespace NuclearReactorSimulator.Application.Tests;

public sealed class ApplicationDescriptorTests
{
    [Fact]
    public void CurrentDescriptor_IdentifiesM77CandidateAfterValidatedM76()
    {
        var descriptor = global::NuclearReactorSimulator.Application.ApplicationDescriptor.Current;

        Assert.Equal("Nuclear Reactor Simulator", descriptor.ProductName);
        Assert.Contains("M7.7", descriptor.Milestone);
        Assert.Contains("Baseline candidate", descriptor.Status);
        Assert.Contains("M7.6 validated", descriptor.Status);
        Assert.Contains("training evaluation", descriptor.Status);
    }
}
