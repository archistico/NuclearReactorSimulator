using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Simulation.Physics.Fluids;
using Xunit;

namespace NuclearReactorSimulator.Simulation.Tests.Physics.Fluids;

public sealed class ThermodynamicBranchContinuityModelTests
{
    private const double SteamMassKilograms = 3322.9485347676582d;
    private const double SteamEnergyJoules = 8238192716.5426521d;
    private const double SteamEnergyProbeJoules = 2059.5481791356628d;
    private const double StopOutMassKilograms = 3165.1742481741885d;
    private const double StopOutEnergyJoules = 7863528392.8413477d;
    private const double StopOutEnergyProbeJoules = 1965.8820982103368d;

    [Fact]
    public void PreviousPhaseContinuity_HoldsSaturatedSteamWhenProductionCoarseSelectionJumpsSuperheated()
    {
        var production = new SimplifiedWaterSteamThermodynamicModel();
        var shadow = new ThermodynamicBranchContinuityModel(
            production,
            production,
            ThermodynamicBranchContinuityOptions.H13PreviousPhaseContinuity);
        var definition = new FluidNodeDefinition("steam", Volume.FromCubicMetres(100d));
        var previous = new FluidThermodynamicState(
            Pressure.FromPascals(6362325.9673817037d),
            Temperature.FromKelvins(552.58890484070866d),
            FluidPhase.SaturatedMixture,
            VaporQuality.FromFraction(0.98827242641541357d));
        var inventory = new FluidNodeInventory(
            Mass.FromKilograms(SteamMassKilograms),
            Energy.FromJoules(SteamEnergyJoules + SteamEnergyProbeJoules));

        var productionResult = production.Resolve(definition, inventory, previous);
        var shadowResult = shadow.Resolve(definition, inventory, previous);

        Assert.Equal(FluidPhase.SuperheatedVapor, productionResult.Phase);
        Assert.Equal(FluidPhase.SaturatedMixture, shadowResult.Phase);
        var decision = Assert.Single(shadow.Decisions);
        Assert.True(decision.SelectionDiffersFromProduction);
        Assert.True(decision.SelectedPreviousPhase);
        Assert.Equal("hold-previous-phase-continuity", decision.DecisionKind);
        Assert.InRange(decision.AvoidedProductionPressureDifferencePascals, 2_700_000d, 2_900_000d);
    }

    [Fact]
    public void PreviousPhaseContinuity_HoldsSuperheatedStopOutWhenProductionCoarseSelectionJumpsSaturated()
    {
        var production = new SimplifiedWaterSteamThermodynamicModel();
        var shadow = new ThermodynamicBranchContinuityModel(
            production,
            production,
            ThermodynamicBranchContinuityOptions.H13PreviousPhaseContinuity);
        var definition = new FluidNodeDefinition("stop-out", Volume.FromCubicMetres(100d));
        var previous = new FluidThermodynamicState(
            Pressure.FromPascals(8601730.4979163781d),
            Temperature.FromKelvins(588.83285718179309d),
            FluidPhase.SuperheatedVapor,
            null);
        var inventory = new FluidNodeInventory(
            Mass.FromKilograms(StopOutMassKilograms),
            Energy.FromJoules(StopOutEnergyJoules - StopOutEnergyProbeJoules));

        var productionResult = production.Resolve(definition, inventory, previous);
        var shadowResult = shadow.Resolve(definition, inventory, previous);

        Assert.Equal(FluidPhase.SaturatedMixture, productionResult.Phase);
        Assert.Equal(FluidPhase.SuperheatedVapor, shadowResult.Phase);
        Assert.True(Assert.Single(shadow.Decisions).SelectionDiffersFromProduction);
    }

    [Fact]
    public void BoundedHysteresis_HoldsOnlyWhilePreviousPhaseRootRemainsNearPreviousState()
    {
        var production = new SimplifiedWaterSteamThermodynamicModel();
        var shadow = new ThermodynamicBranchContinuityModel(
            production,
            production,
            ThermodynamicBranchContinuityOptions.H13BoundedHysteresis);
        var definition = new FluidNodeDefinition("steam", Volume.FromCubicMetres(100d));
        var previous = new FluidThermodynamicState(
            Pressure.FromPascals(6362325.9673817037d),
            Temperature.FromKelvins(552.58890484070866d),
            FluidPhase.SaturatedMixture,
            VaporQuality.FromFraction(0.98827242641541357d));
        var inventory = new FluidNodeInventory(
            Mass.FromKilograms(SteamMassKilograms),
            Energy.FromJoules(SteamEnergyJoules + SteamEnergyProbeJoules));

        var result = shadow.Resolve(definition, inventory, previous);

        Assert.Equal(FluidPhase.SaturatedMixture, result.Phase);
        var decision = Assert.Single(shadow.Decisions);
        Assert.Equal("hold-previous-phase-hysteresis", decision.DecisionKind);
        Assert.InRange(decision.PreviousPhaseRelativePressureDrift, 0d, 0.001d);
        Assert.InRange(decision.PreviousPhaseTemperatureDriftKelvins, 0d, 0.1d);
    }
    [Fact]
    public void BoundedHysteresis_ReleasesWhenPreviousPhaseRootLeavesTheExplicitContinuityBand()
    {
        var production = new SimplifiedWaterSteamThermodynamicModel();
        var shadow = new ThermodynamicBranchContinuityModel(
            production,
            production,
            ThermodynamicBranchContinuityOptions.H13BoundedHysteresis);
        var definition = new FluidNodeDefinition("steam", Volume.FromCubicMetres(100d));
        var deliberatelyDistantPrevious = new FluidThermodynamicState(
            Pressure.FromPascals(5_000_000d),
            Temperature.FromKelvins(530d),
            FluidPhase.SaturatedMixture,
            VaporQuality.FromFraction(0.95d));
        var inventory = new FluidNodeInventory(
            Mass.FromKilograms(SteamMassKilograms),
            Energy.FromJoules(SteamEnergyJoules + SteamEnergyProbeJoules));

        var productionResult = production.Resolve(definition, inventory, deliberatelyDistantPrevious);
        var result = shadow.Resolve(definition, inventory, deliberatelyDistantPrevious);

        Assert.Equal(FluidPhase.SuperheatedVapor, productionResult.Phase);
        Assert.Equal(productionResult, result);
        var decision = Assert.Single(shadow.Decisions);
        Assert.Equal("production-hysteresis-release", decision.DecisionKind);
        Assert.True(decision.PreviousPhaseRelativePressureDrift > 0.02d);
        Assert.True(decision.PreviousPhaseTemperatureDriftKelvins > 5d);
    }

    [Fact]
    public void TargetRestriction_LeavesUntargetedNodeOnProductionSelection()
    {
        var production = new SimplifiedWaterSteamThermodynamicModel();
        var shadow = new ThermodynamicBranchContinuityModel(
            production,
            production,
            ThermodynamicBranchContinuityOptions.H13PreviousPhaseContinuity,
            new[] { "stop-out" });
        var definition = new FluidNodeDefinition("steam", Volume.FromCubicMetres(100d));
        var previous = new FluidThermodynamicState(
            Pressure.FromPascals(6362325.9673817037d),
            Temperature.FromKelvins(552.58890484070866d),
            FluidPhase.SaturatedMixture,
            VaporQuality.FromFraction(0.98827242641541357d));
        var inventory = new FluidNodeInventory(
            Mass.FromKilograms(SteamMassKilograms),
            Energy.FromJoules(SteamEnergyJoules + SteamEnergyProbeJoules));

        var result = shadow.Resolve(definition, inventory, previous);

        Assert.Equal(FluidPhase.SuperheatedVapor, result.Phase);
        Assert.Empty(shadow.Decisions);
    }

}
