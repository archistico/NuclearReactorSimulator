using NuclearReactorSimulator.Application;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests;

public sealed class ApplicationDescriptorTests
{
    [Fact]
    public void Current_DescribesM10941F2ConservativeHeaderReliefCandidate()
    {
        var descriptor = ApplicationDescriptor.Current;

        Assert.Equal("Nuclear Reactor Simulator", descriptor.ProductName);
        Assert.Contains("M10.9.4.1-F.2", descriptor.Milestone);
        Assert.Contains("validated F.1", descriptor.Status);
        Assert.Contains("pressure-actuated", descriptor.Status);
        Assert.Contains("main-steam header relief", descriptor.Status);
        Assert.Contains("vapor availability", descriptor.Status);
        Assert.Contains("exactly once", descriptor.Status);
        Assert.Contains("turbine bypass", descriptor.Status);
        Assert.Contains("enthalpy migration", descriptor.Status);
    }
}
