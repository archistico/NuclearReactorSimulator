using NuclearReactorSimulator.Application;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests;

public sealed class ApplicationDescriptorTests
{
    [Fact]
    public void Current_DescribesM10941F1ChokedSteamFlowCapacityCandidate()
    {
        var descriptor = ApplicationDescriptor.Current;

        Assert.Equal("Nuclear Reactor Simulator", descriptor.ProductName);
        Assert.Contains("M10.9.4.1-F.1", descriptor.Milestone);
        Assert.Contains("validated E.3.2 Hotfix 3", descriptor.Status);
        Assert.Contains("ideal-vapor", descriptor.Status);
        Assert.Contains("compressible steam-flow", descriptor.Status);
        Assert.Contains("subcritical-to-choked", descriptor.Status);
        Assert.Contains("CSV/summary evidence", descriptor.Status);
        Assert.Contains("relief/bypass topology", descriptor.Status);
        Assert.Contains("runtime source-term integration unchanged", descriptor.Status);
    }
}
