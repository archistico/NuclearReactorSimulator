using NuclearReactorSimulator.Application;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests;

public sealed class ApplicationDescriptorTests
{
    [Fact]
    public void Current_DescribesM10941AExtendedAuditOnValidatedM1094Baseline()
    {
        var descriptor = ApplicationDescriptor.Current;

        Assert.Equal("Nuclear Reactor Simulator", descriptor.ProductName);
        Assert.Contains("M10.9.4.1-A", descriptor.Milestone);
        Assert.Contains("Audit candidate", descriptor.Status);
        Assert.Contains("validated M10.9.4 baseline", descriptor.Status);
        Assert.Contains("300-second steady", descriptor.Status);
        Assert.Contains("replay/checkpoint", descriptor.Status);
        Assert.Contains("without changing production physics", descriptor.Status);
    }
}
