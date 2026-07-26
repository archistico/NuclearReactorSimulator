using NuclearReactorSimulator.Application;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests;

public sealed class ApplicationDescriptorTests
{
    [Fact]
    public void Current_DescribesM10941D41TurbineValveHardeningCandidateOnValidatedD4Baseline()
    {
        var descriptor = ApplicationDescriptor.Current;

        Assert.Equal("Nuclear Reactor Simulator", descriptor.ProductName);
        Assert.Contains("M10.9.4.1-D.4.1", descriptor.Milestone);
        Assert.Contains("fully validated D.4 baseline", descriptor.Status);
        Assert.Contains("STOP valve", descriptor.Status);
        Assert.Contains("full replay", descriptor.Status);
        Assert.Contains("in-flight checkpoint", descriptor.Status);
        Assert.Contains("turbine-trip reset", descriptor.Status);
        Assert.Contains("without hidden repair", descriptor.Status);
    }
}
