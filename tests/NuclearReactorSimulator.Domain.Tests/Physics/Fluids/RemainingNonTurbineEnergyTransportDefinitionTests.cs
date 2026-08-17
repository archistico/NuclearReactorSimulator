using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Physics.Reactor.PrimaryCircuit.Boundaries;
using NuclearReactorSimulator.Domain.Physics.Reactor.PrimaryCircuit.SteamDrums;
using NuclearReactorSimulator.Domain.Physics.TurbineIsland.MainSteam;
using Xunit;

namespace NuclearReactorSimulator.Domain.Tests.Physics.Fluids;

public sealed class RemainingNonTurbineEnergyTransportDefinitionTests
{
    [Fact]
    public void NewTransportOwners_DefaultToHistoricalSpecificInternalEnergy()
    {
        Assert.Equal(
            FluidEnergyTransportMode.SpecificInternalEnergy,
            new FeedwaterBoundaryDefinition("feed", "drum", "inventory").EnergyTransportMode);
        Assert.Equal(
            FluidEnergyTransportMode.SpecificInternalEnergy,
            new SteamExportBoundaryDefinition("export", "drum", "steam").EnergyTransportMode);
        Assert.Equal(
            FluidEnergyTransportMode.SpecificInternalEnergy,
            new SteamDrumDefinition("drum", "loop", "inventory", "steam").EnergyTransportMode);
        Assert.Equal(
            FluidEnergyTransportMode.SpecificInternalEnergy,
            new TurbineAdmissionBoundaryDefinition("admission-boundary", "train", "inlet").EnergyTransportMode);
    }

    [Fact]
    public void NewTransportOwners_PreserveExplicitSpecificEnthalpyOptIn()
    {
        Assert.Equal(
            FluidEnergyTransportMode.SpecificEnthalpy,
            new FeedwaterBoundaryDefinition(
                "feed",
                "drum",
                "inventory",
                FluidEnergyTransportMode.SpecificEnthalpy).EnergyTransportMode);
        Assert.Equal(
            FluidEnergyTransportMode.SpecificEnthalpy,
            new SteamExportBoundaryDefinition(
                "export",
                "drum",
                "steam",
                FluidEnergyTransportMode.SpecificEnthalpy).EnergyTransportMode);
        Assert.Equal(
            FluidEnergyTransportMode.SpecificEnthalpy,
            new SteamDrumDefinition(
                "drum",
                "loop",
                "inventory",
                "steam",
                energyTransportMode: FluidEnergyTransportMode.SpecificEnthalpy).EnergyTransportMode);
        Assert.Equal(
            FluidEnergyTransportMode.SpecificEnthalpy,
            new TurbineAdmissionBoundaryDefinition(
                "admission-boundary",
                "train",
                "inlet",
                FluidEnergyTransportMode.SpecificEnthalpy).EnergyTransportMode);
    }

    [Fact]
    public void NewTransportOwners_RejectUndefinedEnergyTransportMode()
    {
        var unsupported = (FluidEnergyTransportMode)999;

        Assert.Throws<ArgumentOutOfRangeException>(
            () => new FeedwaterBoundaryDefinition("feed", "drum", "inventory", unsupported));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new SteamExportBoundaryDefinition("export", "drum", "steam", unsupported));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new SteamDrumDefinition(
                "drum",
                "loop",
                "inventory",
                "steam",
                energyTransportMode: unsupported));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new TurbineAdmissionBoundaryDefinition("admission-boundary", "train", "inlet", unsupported));
    }
}
