using NuclearReactorSimulator.Application;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests;

public sealed class ApplicationDescriptorTests
{
    [Fact]
    public void Current_DescribesM10941H18TurbineInletContinuityResidualFloorSplitCandidate()
    {
        var descriptor = ApplicationDescriptor.Current;

        Assert.Equal("Nuclear Reactor Simulator", descriptor.ProductName);
        Assert.Contains("M10.9.4.1-H.18", descriptor.Milestone);
        Assert.Contains("user-validated H.17 Hotfix 6", descriptor.Status);
        Assert.Contains("ExplicitCommittedState", descriptor.Status);
        Assert.Contains("30,000-interval", descriptor.Status);
        Assert.Contains("3,046 P060/F040 trigger intervals", descriptor.Status);
        Assert.Contains("92 episodes", descriptor.Status);
        Assert.Contains("473 deterministic representatives", descriptor.Status);
        Assert.Contains("228/473", descriptor.Status);
        Assert.Contains("245/473", descriptor.Status);
        Assert.Contains("120 of the 245 failures", descriptor.Status);
        Assert.Contains("125 failures", descriptor.Status);
        Assert.Contains("turbine-inlet", descriptor.Status);
        Assert.Contains("steam/stop-out/header/turbine-inlet", descriptor.Status);
        Assert.Contains("5→0→5 MWe", descriptor.Status);
        Assert.Contains("2% pressure / 5 K", descriptor.Status);
        Assert.Contains("H.9 Jacobian corrector", descriptor.Status);
        Assert.Contains("mapped-minus-applied node residual ranking", descriptor.Status);
        Assert.Contains("residual-floor evidence", descriptor.Status);
        Assert.Contains("candidate-vs-explicit inverse-branch disagreement", descriptor.Status);
        Assert.Contains("SimplifiedWaterSteamThermodynamicModel.Resolve()", descriptor.Status);
        Assert.Contains("ThermodynamicBranchContinuityModel", descriptor.Status);
        Assert.Contains("P060/F040", descriptor.Status);
        Assert.Contains("PlantNetworkOrchestrator", descriptor.Status);
        Assert.Contains("No shadow state is committed", descriptor.Status);
        Assert.Contains("hysteresis-limit retuning", descriptor.Status);
        Assert.Contains("physical coefficient retuning", descriptor.Status);
        Assert.Contains("hidden flow filtering", descriptor.Status);
        Assert.Contains("thermodynamic clamping", descriptor.Status);
        Assert.Contains("10 ms", descriptor.Status);
        Assert.Contains("Phase H remains open", descriptor.Status);
        Assert.Contains("Phase I", descriptor.Status);
    }
}
