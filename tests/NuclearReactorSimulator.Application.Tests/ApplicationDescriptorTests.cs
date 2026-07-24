using NuclearReactorSimulator.Application;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests;

public sealed class ApplicationDescriptorTests
{
    [Fact]
    public void Current_DescribesM10941B1InventoryClosureCandidateOnLocallyGreenA3Checkpoint()
    {
        var descriptor = ApplicationDescriptor.Current;

        Assert.Equal("Nuclear Reactor Simulator", descriptor.ProductName);
        Assert.Contains("M10.9.4.1-B.1", descriptor.Milestone);
        Assert.Contains("locally green A.3 checkpoint", descriptor.Status);
        Assert.Contains("manual-only game penalties", descriptor.Status);
        Assert.Contains("SPEED/LOAD reference steps", descriptor.Status);
        Assert.Contains("inventory-limits", descriptor.Status);
        Assert.Contains("legacy/v1 behavior", descriptor.Status);
    }
}
