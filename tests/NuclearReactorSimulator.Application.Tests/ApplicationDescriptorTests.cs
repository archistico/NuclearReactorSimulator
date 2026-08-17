using NuclearReactorSimulator.Application;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests;

public sealed class ApplicationDescriptorTests
{
    [Fact]
    public void Current_DescribesM10941H20FourNodeActivationRollbackContractCandidate()
    {
        var descriptor = ApplicationDescriptor.Current;

        Assert.Equal("Nuclear Reactor Simulator", descriptor.ProductName);
        Assert.Contains("M10.9.4.1-H.20", descriptor.Milestone);
        Assert.Contains("user-validated H.19", descriptor.Status);
        Assert.Contains("ExplicitCommittedState", descriptor.Status);
        Assert.Contains("30,000-interval/four-profile", descriptor.Status);
        Assert.Contains("3,046 trigger intervals", descriptor.Status);
        Assert.Contains("92 episodes", descriptor.Status);
        Assert.Contains("473 frozen representative keys", descriptor.Status);
        Assert.Contains("473/473", descriptor.Status);
        Assert.Contains("245 H.17 failures", descriptor.Status);
        Assert.Contains("228 H.17 successes", descriptor.Status);
        Assert.Contains("120,000 committed phase-state checks", descriptor.Status);
        Assert.Contains("zero committed-selection overrides", descriptor.Status);
        Assert.Contains("steam/stop-out/header/turbine-inlet", descriptor.Status);
        Assert.Contains("2% pressure / 5 K", descriptor.Status);
        Assert.Contains("fail-closed", descriptor.Status);
        Assert.Contains("default activation arm is disabled", descriptor.Status);
        Assert.Contains("line-search exhaustion", descriptor.Status);
        Assert.Contains("1e-5 / 1e-2 kg/s", descriptor.Status);
        Assert.Contains("1e-8 kg/s / 1e-3 W", descriptor.Status);
        Assert.Contains("typed reason", descriptor.Status);
        Assert.Contains("cannot authorize production commit", descriptor.Status);
        Assert.Contains("not wired into PlantNetworkOrchestrator", descriptor.Status);
        Assert.Contains("P060/F040", descriptor.Status);
        Assert.Contains("production Resolve()", descriptor.Status);
        Assert.Contains("10 ms", descriptor.Status);
        Assert.Contains("Phase H remains open", descriptor.Status);
        Assert.Contains("Phase I", descriptor.Status);
    }
}
