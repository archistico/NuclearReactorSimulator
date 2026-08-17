using NuclearReactorSimulator.Application;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests;

public sealed class ApplicationDescriptorTests
{
    [Fact]
    public void Current_DescribesM10941H19FourNodeLongHorizonCrossProfileQualificationCandidate()
    {
        var descriptor = ApplicationDescriptor.Current;

        Assert.Equal("Nuclear Reactor Simulator", descriptor.ProductName);
        Assert.Contains("M10.9.4.1-H.19", descriptor.Milestone);
        Assert.Contains("user-validated H.18 Hotfix 1", descriptor.Status);
        Assert.Contains("ExplicitCommittedState", descriptor.Status);
        Assert.Contains("261/261", descriptor.Status);
        Assert.Contains("120/120", descriptor.Status);
        Assert.Contains("125/125", descriptor.Status);
        Assert.Contains("16/16", descriptor.Status);
        Assert.Contains("steam/stop-out/header/turbine-inlet", descriptor.Status);
        Assert.Contains("30,000 explicit reference intervals", descriptor.Status);
        Assert.Contains("3,046 trigger intervals", descriptor.Status);
        Assert.Contains("92 episodes", descriptor.Status);
        Assert.Contains("473 deterministic representatives", descriptor.Status);
        Assert.Contains("frozen H.17 evidence", descriptor.Status);
        Assert.Contains("245 H.17 failures", descriptor.Status);
        Assert.Contains("228 H.17 successes", descriptor.Status);
        Assert.Contains("120,000 committed target phase-state checks", descriptor.Status);
        Assert.Contains("5→0→5 MWe", descriptor.Status);
        Assert.Contains("2% pressure / 5 K", descriptor.Status);
        Assert.Contains("H.9 Jacobian corrector", descriptor.Status);
        Assert.Contains("candidate-vs-explicit inverse-branch evidence", descriptor.Status);
        Assert.Contains("selected-phase mismatch", descriptor.Status);
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
