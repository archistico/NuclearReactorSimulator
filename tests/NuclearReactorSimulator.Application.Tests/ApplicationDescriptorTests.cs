using NuclearReactorSimulator.Application;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests;

public sealed class ApplicationDescriptorTests
{
    [Fact]
    public void Current_DescribesM10941F3Hotfix1ConservativeTurbineBypassCandidate()
    {
        var descriptor = ApplicationDescriptor.Current;

        Assert.Equal("Nuclear Reactor Simulator", descriptor.ProductName);
        Assert.Contains("M10.9.4.1-F.3 Hotfix 1", descriptor.Milestone);
        Assert.Contains("Hotfix 1 candidate", descriptor.Status);
        Assert.Contains("validated F.2", descriptor.Status);
        Assert.Contains("pressure-actuated", descriptor.Status);
        Assert.Contains("turbine-bypass", descriptor.Status);
        Assert.Contains("condenser backpressure", descriptor.Status);
        Assert.Contains("exactly once", descriptor.Status);
        Assert.Contains("zero external exchange", descriptor.Status);
        Assert.Contains("enthalpy migration", descriptor.Status);
    }
}
