using NuclearReactorSimulator.Application;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests;

public sealed class ApplicationDescriptorTests
{
    [Fact]
    public void Current_DescribesM107CandidateOnValidatedM106Baseline()
    {
        var descriptor = ApplicationDescriptor.Current;

        Assert.Equal("Nuclear Reactor Simulator", descriptor.ProductName);
        Assert.Contains("M10.7", descriptor.Milestone);
        Assert.Contains("Implementation candidate", descriptor.Status);
        Assert.Contains("M10.6", descriptor.Status);
        Assert.Contains("validated", descriptor.Status.ToLowerInvariant());
        Assert.Contains("replay-backed", descriptor.Status.ToLowerInvariant());
        Assert.Contains("save/load", descriptor.Status.ToLowerInvariant());
    }
}
