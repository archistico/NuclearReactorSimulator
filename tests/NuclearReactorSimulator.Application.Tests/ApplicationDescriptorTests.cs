using NuclearReactorSimulator.Application;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests;

public sealed class ApplicationDescriptorTests
{
    [Fact]
    public void Current_DescribesM1094CandidateOnValidatedM1093Baseline()
    {
        var descriptor = ApplicationDescriptor.Current;

        Assert.Equal("Nuclear Reactor Simulator", descriptor.ProductName);
        Assert.Contains("M10.9.4", descriptor.Milestone);
        Assert.Contains("Implementation candidate", descriptor.Status);
        Assert.Contains("validated M10.9.3 baseline", descriptor.Status);
        Assert.Contains("subsystem engineering schematics", descriptor.Status);
        Assert.Contains("long-gameplay acceptance tests", descriptor.Status);
        Assert.Contains("without moving plant topology", descriptor.Status);
    }
}
