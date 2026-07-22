using NuclearReactorSimulator.Application;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests;

public sealed class ApplicationDescriptorTests
{
    [Fact]
    public void Current_DescribesM1071CandidateOnValidatedM107Baseline()
    {
        var descriptor = ApplicationDescriptor.Current;

        Assert.Equal("Nuclear Reactor Simulator", descriptor.ProductName);
        Assert.Contains("M10.7.1", descriptor.Milestone);
        Assert.Contains("Implementation candidate", descriptor.Status);
        Assert.Contains("validated M10.7 baseline", descriptor.Status);
        Assert.Contains("validated", descriptor.Status.ToLowerInvariant());
        Assert.Contains("protection-reset", descriptor.Status.ToLowerInvariant());
        Assert.Contains("synchronization", descriptor.Status.ToLowerInvariant());
    }
}
