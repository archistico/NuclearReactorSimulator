using NuclearReactorSimulator.Application;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests;

public sealed class ApplicationDescriptorTests
{
    [Fact]
    public void Current_DescribesM10941E32Hotfix2EvidenceDerivedElectricalProtectionCandidate()
    {
        var descriptor = ApplicationDescriptor.Current;

        Assert.Equal("Nuclear Reactor Simulator", descriptor.ProductName);
        Assert.Contains("M10.9.4.1-E.3.2 Hotfix 2", descriptor.Milestone);
        Assert.Contains("validated E.3.1 Hotfix 1", descriptor.Status);
        Assert.Contains("logical-step-zero measured instrumentation", descriptor.Status);
        Assert.Contains("canonical grid nominal frequency", descriptor.Status);
        Assert.Contains("breaker-supervised", descriptor.Status);
        Assert.Contains("reverse-power", descriptor.Status);
        Assert.Contains("underfrequency", descriptor.Status);
        Assert.Contains("loss-of-synchronism", descriptor.Status);
        Assert.Contains("canonical M5.5", descriptor.Status);
    }
}
