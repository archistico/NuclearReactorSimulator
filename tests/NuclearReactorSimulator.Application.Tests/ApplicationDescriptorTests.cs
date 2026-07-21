using Xunit;

namespace NuclearReactorSimulator.Application.Tests;

public sealed class ApplicationDescriptorTests
{
    [Fact]
    public void CurrentDescriptor_IdentifiesM73CandidateAfterValidatedM72()
    {
        var descriptor = global::NuclearReactorSimulator.Application.ApplicationDescriptor.Current;

        Assert.Equal("Nuclear Reactor Simulator", descriptor.ProductName);
        Assert.Contains("M7.3", descriptor.Milestone);
        Assert.Contains("Baseline candidate", descriptor.Status);
        Assert.Contains("M7.2 validated", descriptor.Status);
        Assert.Contains("source-range", descriptor.Status);
    }
}
