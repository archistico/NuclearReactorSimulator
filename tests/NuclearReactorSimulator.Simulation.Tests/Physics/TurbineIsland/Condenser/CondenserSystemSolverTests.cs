using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.Reactor.Core;
using NuclearReactorSimulator.Domain.Physics.Reactor.Core.Channels;
using NuclearReactorSimulator.Domain.Physics.Reactor.PrimaryCircuit.Boundaries;
using NuclearReactorSimulator.Domain.Physics.Reactor.PrimaryCircuit.Circulation;
using NuclearReactorSimulator.Domain.Physics.Reactor.PrimaryCircuit.Integration;
using NuclearReactorSimulator.Domain.Physics.Reactor.PrimaryCircuit.SteamDrums;
using NuclearReactorSimulator.Domain.Physics.Reactor.ThermalPower;
using NuclearReactorSimulator.Domain.Physics.Thermal;
using NuclearReactorSimulator.Domain.Physics.TurbineIsland.Condenser;
using NuclearReactorSimulator.Domain.Physics.TurbineIsland.MainSteam;
using NuclearReactorSimulator.Domain.Physics.TurbineIsland.Turbine;
using NuclearReactorSimulator.Domain.Plant;
using NuclearReactorSimulator.Simulation.Physics.Fluids;
using NuclearReactorSimulator.Simulation.Physics.Reactor.Core;
using NuclearReactorSimulator.Simulation.Physics.Reactor.PrimaryCircuit.Boundaries;
using NuclearReactorSimulator.Simulation.Physics.Reactor.PrimaryCircuit.Integration;
using NuclearReactorSimulator.Simulation.Physics.TurbineIsland.Condenser;
using NuclearReactorSimulator.Simulation.Physics.TurbineIsland.MainSteam;
using NuclearReactorSimulator.Simulation.Physics.TurbineIsland.Turbine;
using Xunit;

namespace NuclearReactorSimulator.Simulation.Tests.Physics.TurbineIsland.Condenser;

public sealed class CondenserSystemSolverTests
{
    [Fact]
    public void Step_CondensesSteamTransfersMassToHotwellAndRejectsHeatConservatively()
    {
        var fixture = CreateFixture(Power.FromMegawatts(3d), new PreservingThermodynamicModel());
        var solver = new CondenserSystemSolver(fixture.Definition, fixture.ThermodynamicModel);

        var result = solver.Step(
            fixture.PlantState,
            fixture.TurbineState,
            fixture.Inputs,
            TimeSpan.FromSeconds(1d));
        var condenser = Assert.Single(result.Snapshot.Condensers);

        Assert.Equal(2d, condenser.ActualCondensationMassFlowRate.KilogramsPerSecond, 12);
        Assert.Equal(3d, condenser.HeatRejectionPower.Megawatts, 9);
        Assert.Equal(9_998d, result.CandidatePlantState.GetFluidNode("exhaust").Mass.Kilograms, 9);
        Assert.Equal(10_002d, result.CandidatePlantState.GetFluidNode("hotwell").Mass.Kilograms, 9);
        Assert.Equal(-3d, result.Snapshot.ThermofluidAudit.SupplementalExternalPower.Megawatts, 9);
        Assert.InRange(Math.Abs(result.Snapshot.ThermofluidAudit.BalanceMassRateResidualKilogramsPerSecond), 0d, 1e-12d);
        Assert.InRange(Math.Abs(result.Snapshot.ThermofluidAudit.BalancePowerResidualWatts), 0d, 1e-6d);
        Assert.InRange(Math.Abs(result.Snapshot.ThermofluidAudit.EnergyClosureResidualJoules), 0d, 1e-3d);
    }


    [Fact]
    public void Step_PressureResolvedCondensateEnergyTransfersSaturatedLiquidEnergyIntoHotwell()
    {
        var model = new FixedSaturationPreservingThermodynamicModel(SpecificEnergy.FromKilojoulesPerKilogram(400d));
        var fixture = CreateFixture(
            Power.FromMegawatts(100d),
            model,
            condensateEnergyMode: CondenserCondensateEnergyMode.SaturatedLiquidAtSteamSpacePressure);
        var solver = new CondenserSystemSolver(fixture.Definition, fixture.ThermodynamicModel);

        var result = solver.Step(
            fixture.PlantState,
            fixture.TurbineState,
            fixture.Inputs,
            TimeSpan.FromSeconds(1d));
        var condenser = Assert.Single(result.Snapshot.Condensers);

        Assert.Equal(10d, condenser.ActualCondensationMassFlowRate.KilogramsPerSecond, 12);
        Assert.Equal(400d, condenser.CondensateSpecificInternalEnergy.KilojoulesPerKilogram, 12);
        Assert.Equal(1_600d, condenser.SpecificCondensationEnergyDrop.KilojoulesPerKilogram, 12);
        Assert.Equal(4d, condenser.HotwellEnergyAdditionRate.Megawatts, 12);
        Assert.Equal(16d, condenser.HeatRejectionPower.Megawatts, 12);
        Assert.NotNull(model.LastRequestedSaturationPressure);
        Assert.Equal(Pressure.FromMegapascals(0.1d), model.LastRequestedSaturationPressure.Value);
        Assert.True(condenser.MaximumFlowLimitActive);
        Assert.False(condenser.InventoryLimitActive);
        Assert.False(condenser.ThermalLimitActive);
        Assert.Equal("MAXIMUM FLOW", condenser.ActiveCondensationLimits);
        Assert.InRange(Math.Abs(result.Snapshot.ThermofluidAudit.BalancePowerResidualWatts), 0d, 1e-6d);
        Assert.InRange(Math.Abs(result.Snapshot.ThermofluidAudit.EnergyClosureResidualJoules), 0d, 1e-3d);
    }

    [Fact]
    public void Step_LegacyCondensateEnergyPreservesCommittedHotwellSpecificEnergy()
    {
        var fixture = CreateFixture(Power.FromMegawatts(100d), new PreservingThermodynamicModel());
        var solver = new CondenserSystemSolver(fixture.Definition, fixture.ThermodynamicModel);

        var result = solver.Step(
            fixture.PlantState,
            fixture.TurbineState,
            fixture.Inputs,
            TimeSpan.FromSeconds(1d));
        var condenser = Assert.Single(result.Snapshot.Condensers);

        Assert.Equal(CondenserCondensateEnergyMode.LegacyHotwellSpecificInternalEnergy,
            Assert.Single(fixture.Definition.Condensers).CondensateEnergyMode);
        Assert.Equal(500d, condenser.CondensateSpecificInternalEnergy.KilojoulesPerKilogram, 12);
        Assert.Equal(5d, condenser.HotwellEnergyAdditionRate.Megawatts, 12);
        Assert.Equal(15d, condenser.HeatRejectionPower.Megawatts, 12);
    }

    [Fact]
    public void Constructor_PressureResolvedCondensateEnergyRequiresSaturationPropertyProvider()
    {
        var fixture = CreateFixture(
            Power.FromMegawatts(100d),
            new PreservingThermodynamicModel(),
            condensateEnergyMode: CondenserCondensateEnergyMode.SaturatedLiquidAtSteamSpacePressure);

        var exception = Assert.Throws<ArgumentException>(() =>
            new CondenserSystemSolver(fixture.Definition, fixture.ThermodynamicModel));

        Assert.Contains("saturation-property provider", exception.Message);
    }

    [Fact]
    public void Step_CoolingBoundaryCapacityLimitsCondensationRate()
    {
        var fixture = CreateFixture(Power.FromMegawatts(1.5d), new PreservingThermodynamicModel());
        var solver = new CondenserSystemSolver(fixture.Definition, fixture.ThermodynamicModel);

        var result = solver.Step(
            fixture.PlantState,
            fixture.TurbineState,
            fixture.Inputs,
            TimeSpan.FromSeconds(1d));
        var condenser = Assert.Single(result.Snapshot.Condensers);
        var boundary = Assert.Single(result.Snapshot.CoolingBoundaries);

        Assert.Equal(1d, condenser.ThermalLimitedCondensationMassFlowRate.KilogramsPerSecond, 12);
        Assert.Equal(1d, condenser.ActualCondensationMassFlowRate.KilogramsPerSecond, 12);
        Assert.Equal(1.5d, boundary.UsedHeatRejectionPower.Megawatts, 9);
        Assert.Equal(Power.Zero, boundary.UnusedHeatRejectionPower);
        Assert.True(boundary.InstalledCoolingCapacityLimitActive);
        Assert.False(boundary.SurfaceHeatTransferLimitActive);
        Assert.Equal("INSTALLED CAPACITY", boundary.ActiveHeatRejectionLimits);
    }


    [Fact]
    public void Step_ExplicitInstalledCapacityCapsOtherwiseAvailableCoolingPower()
    {
        var fixture = CreateFixture(
            Power.FromMegawatts(10d),
            new PreservingThermodynamicModel(),
            installedHeatRejectionCapacity: Power.FromMegawatts(3d));
        var solver = new CondenserSystemSolver(fixture.Definition, fixture.ThermodynamicModel);

        var result = solver.Step(
            fixture.PlantState,
            fixture.TurbineState,
            fixture.Inputs,
            TimeSpan.FromSeconds(1d));
        var boundary = Assert.Single(result.Snapshot.CoolingBoundaries);

        Assert.Equal(3d, boundary.InstalledHeatRejectionCapacity.Megawatts, 12);
        Assert.Equal(10d, boundary.AvailableHeatRejectionPower.Megawatts, 12);
        Assert.Equal(3d, boundary.EffectiveHeatRejectionCapacity.Megawatts, 12);
        Assert.True(boundary.InstalledCoolingCapacityLimitActive);
        Assert.False(boundary.AvailableCoolingCapacityLimitActive);
        Assert.Equal("INSTALLED CAPACITY", boundary.ActiveHeatRejectionLimits);
    }

    [Fact]
    public void Step_RuntimeAvailabilityCanFallBelowInstalledCapacityWithoutChangingPlantDefinition()
    {
        var fixture = CreateFixture(
            Power.FromMegawatts(3d),
            new PreservingThermodynamicModel(),
            installedHeatRejectionCapacity: Power.FromMegawatts(10d));
        var solver = new CondenserSystemSolver(fixture.Definition, fixture.ThermodynamicModel);

        var result = solver.Step(
            fixture.PlantState,
            fixture.TurbineState,
            fixture.Inputs,
            TimeSpan.FromSeconds(1d));
        var boundary = Assert.Single(result.Snapshot.CoolingBoundaries);

        Assert.Equal(10d, boundary.InstalledHeatRejectionCapacity.Megawatts, 12);
        Assert.Equal(3d, boundary.AvailableHeatRejectionPower.Megawatts, 12);
        Assert.Equal(3d, boundary.EffectiveHeatRejectionCapacity.Megawatts, 12);
        Assert.False(boundary.InstalledCoolingCapacityLimitActive);
        Assert.True(boundary.AvailableCoolingCapacityLimitActive);
        Assert.Equal("AVAILABLE COOLING", boundary.ActiveHeatRejectionLimits);
    }

    [Fact]
    public void Step_SurfaceHeatTransferUaLimitsHeatRejectionBelowAvailableCoolingCapacity()
    {
        var fixture = CreateFixture(
            Power.FromMegawatts(10d),
            new PreservingThermodynamicModel(),
            ThermalConductance.FromMegawattsPerKelvin(0.005d),
            Temperature.FromDegreesCelsius(20d));
        var solver = new CondenserSystemSolver(fixture.Definition, fixture.ThermodynamicModel);

        var result = solver.Step(
            fixture.PlantState,
            fixture.TurbineState,
            fixture.Inputs,
            TimeSpan.FromSeconds(1d));
        var condenser = Assert.Single(result.Snapshot.Condensers);
        var boundary = Assert.Single(result.Snapshot.CoolingBoundaries);

        Assert.Equal(260d, condenser.SteamToCoolingTemperatureDifference.Kelvins, 9);
        Assert.Equal(1.3d, condenser.SurfaceHeatTransferLimitedPower.Megawatts, 9);
        Assert.Equal(1.3d, condenser.EffectiveHeatRejectionCapacity.Megawatts, 9);
        Assert.Equal(1.3d, boundary.SurfaceHeatTransferLimitedPower.Megawatts, 9);
        Assert.Equal(1.3d, boundary.EffectiveHeatRejectionCapacity.Megawatts, 9);
        Assert.False(boundary.InstalledCoolingCapacityLimitActive);
        Assert.True(boundary.SurfaceHeatTransferLimitActive);
        Assert.Equal("SURFACE UA", boundary.ActiveHeatRejectionLimits);
        Assert.Equal(1.3d / 1.5d, condenser.ThermalLimitedCondensationMassFlowRate.KilogramsPerSecond, 9);
        Assert.Equal(1.3d, condenser.HeatRejectionPower.Megawatts, 9);
    }


    [Fact]
    public void Step_SurfaceHeatTransferUaReducesCondensationAsSteamApproachesCoolantTemperature()
    {
        var warm = CreateFixture(
            Power.FromMegawatts(10d),
            new PreservingThermodynamicModel(),
            ThermalConductance.FromMegawattsPerKelvin(0.01d),
            Temperature.FromDegreesCelsius(20d),
            exhaustTemperatureCelsius: 120d);
        var cool = CreateFixture(
            Power.FromMegawatts(10d),
            new PreservingThermodynamicModel(),
            ThermalConductance.FromMegawattsPerKelvin(0.01d),
            Temperature.FromDegreesCelsius(20d),
            exhaustTemperatureCelsius: 40d);

        var warmResult = new CondenserSystemSolver(warm.Definition, warm.ThermodynamicModel).Step(
            warm.PlantState,
            warm.TurbineState,
            warm.Inputs,
            TimeSpan.FromSeconds(1d));
        var coolResult = new CondenserSystemSolver(cool.Definition, cool.ThermodynamicModel).Step(
            cool.PlantState,
            cool.TurbineState,
            cool.Inputs,
            TimeSpan.FromSeconds(1d));
        var warmCondenser = Assert.Single(warmResult.Snapshot.Condensers);
        var coolCondenser = Assert.Single(coolResult.Snapshot.Condensers);

        Assert.Equal(1d, warmCondenser.SurfaceHeatTransferLimitedPower.Megawatts, 9);
        Assert.Equal(0.2d, coolCondenser.SurfaceHeatTransferLimitedPower.Megawatts, 9);
        Assert.True(warmCondenser.ActualCondensationMassFlowRate > coolCondenser.ActualCondensationMassFlowRate);
    }

    [Fact]
    public void Step_SurfaceHeatTransferUaStopsCondensationWhenSteamIsNotHotterThanCoolant()
    {
        var fixture = CreateFixture(
            Power.FromMegawatts(10d),
            new PreservingThermodynamicModel(),
            ThermalConductance.FromMegawattsPerKelvin(1d),
            Temperature.FromDegreesCelsius(40d),
            exhaustTemperatureCelsius: 40d);
        var solver = new CondenserSystemSolver(fixture.Definition, fixture.ThermodynamicModel);

        var result = solver.Step(
            fixture.PlantState,
            fixture.TurbineState,
            fixture.Inputs,
            TimeSpan.FromSeconds(1d));
        var condenser = Assert.Single(result.Snapshot.Condensers);

        Assert.Equal(TemperatureDifference.Zero, condenser.SteamToCoolingTemperatureDifference);
        Assert.Equal(Power.Zero, condenser.SurfaceHeatTransferLimitedPower);
        Assert.Equal(Power.Zero, condenser.EffectiveHeatRejectionCapacity);
        Assert.Equal(MassFlowRate.Zero, condenser.ActualCondensationMassFlowRate);
        Assert.Equal(Power.Zero, condenser.HeatRejectionPower);
    }

    [Fact]
    public void Step_NoCoolingCapacityProducesNoCondensation()
    {
        var fixture = CreateFixture(Power.Zero, new PreservingThermodynamicModel());
        var solver = new CondenserSystemSolver(fixture.Definition, fixture.ThermodynamicModel);

        var result = solver.Step(
            fixture.PlantState,
            fixture.TurbineState,
            fixture.Inputs,
            TimeSpan.FromSeconds(1d));
        var condenser = Assert.Single(result.Snapshot.Condensers);

        Assert.Equal(MassFlowRate.Zero, condenser.ActualCondensationMassFlowRate);
        Assert.Equal(Power.Zero, condenser.HeatRejectionPower);
        Assert.Equal(fixture.PlantState.GetFluidNode("hotwell").Mass, result.CandidatePlantState.GetFluidNode("hotwell").Mass);
    }

    [Fact]
    public void Step_CondensationReducesSteamSpacePressureAndIncreasesVacuumWhenClosureRespondsToInventory()
    {
        var model = new ExhaustMassPressureThermodynamicModel();
        var fixture = CreateFixture(Power.FromMegawatts(3d), model);
        var solver = new CondenserSystemSolver(fixture.Definition, model);

        var result = solver.Step(
            fixture.PlantState,
            fixture.TurbineState,
            fixture.Inputs,
            TimeSpan.FromSeconds(1d));
        var condenser = Assert.Single(result.Snapshot.Condensers);

        Assert.True(condenser.FinalSteamSpacePressure < condenser.InitialSteamSpacePressure);
        Assert.True(condenser.FinalVacuumBelowAtmosphere > condenser.InitialVacuumBelowAtmosphere);
    }

    [Fact]
    public void Step_IsDeterministicForIdenticalCommittedStatesAndInputs()
    {
        var fixture = CreateFixture(Power.FromMegawatts(3d), new PreservingThermodynamicModel());
        var solver = new CondenserSystemSolver(fixture.Definition, fixture.ThermodynamicModel);

        var left = solver.Step(fixture.PlantState, fixture.TurbineState, fixture.Inputs, TimeSpan.FromSeconds(1d));
        var right = solver.Step(fixture.PlantState, fixture.TurbineState, fixture.Inputs, TimeSpan.FromSeconds(1d));

        Assert.Equal(left.CandidatePlantState.GetFluidNode("exhaust").Inventory, right.CandidatePlantState.GetFluidNode("exhaust").Inventory);
        Assert.Equal(left.CandidatePlantState.GetFluidNode("hotwell").Inventory, right.CandidatePlantState.GetFluidNode("hotwell").Inventory);
        Assert.Equal(left.Snapshot.TotalHeatRejectionPower, right.Snapshot.TotalHeatRejectionPower);
        Assert.Equal(left.Snapshot.GetCondenser("condenser"), right.Snapshot.GetCondenser("condenser"));
    }

    [Fact]
    public void Inputs_RequireExactCoolingBoundaryCoverage()
    {
        var fixture = CreateFixture(Power.FromMegawatts(3d), new PreservingThermodynamicModel());

        Assert.Throws<ArgumentException>(() => new CondenserSystemInputs(
            fixture.Definition,
            fixture.Inputs.TurbineExpansionInputs,
            Array.Empty<CondenserCoolingBoundaryInput>()));
    }


    [Fact]
    public void Step_TurbineBypassTransfersMassAndInternalEnergyWithoutExternalExchange()
    {
        var fixture = CreateFixture(
            Power.Zero,
            new PreservingThermodynamicModel(),
            includeTurbineBypass: true,
            headerPressureMegapascals: 6.5d,
            exhaustPressureMegapascals: 0.1d,
            isolateMainSteamFlows: true);
        var solver = new CondenserSystemSolver(fixture.Definition, fixture.ThermodynamicModel);
        var deltaTime = TimeSpan.FromMilliseconds(100d);
        var initialHeader = fixture.PlantState.GetFluidNode("header");
        var initialExhaust = fixture.PlantState.GetFluidNode("exhaust");

        var result = solver.Step(fixture.PlantState, fixture.TurbineState, fixture.Inputs, deltaTime);
        var bypass = Assert.Single(result.Snapshot.TurbineBypasses);
        var transferredMass = bypass.MassFlowRate.KilogramsPerSecond * deltaTime.TotalSeconds;
        var transferredEnergy = bypass.InternalEnergyTransferRate.Watts * deltaTime.TotalSeconds;

        Assert.True(bypass.IsChoked);
        Assert.True(bypass.MassFlowRate.KilogramsPerSecond > 12d);
        Assert.Equal(initialHeader.Mass.Kilograms - transferredMass, result.CandidatePlantState.GetFluidNode("header").Mass.Kilograms, 9);
        Assert.Equal(initialExhaust.Mass.Kilograms + transferredMass, result.CandidatePlantState.GetFluidNode("exhaust").Mass.Kilograms, 9);
        Assert.Equal(initialHeader.InternalEnergy.Joules - transferredEnergy, result.CandidatePlantState.GetFluidNode("header").InternalEnergy.Joules, 3);
        Assert.Equal(initialExhaust.InternalEnergy.Joules + transferredEnergy, result.CandidatePlantState.GetFluidNode("exhaust").InternalEnergy.Joules, 3);
        Assert.Equal(MassFlowRate.Zero, result.Snapshot.ThermofluidAudit.ExpectedExternalMassFlowRate);
        Assert.Equal(Power.Zero, result.Snapshot.ThermofluidAudit.SupplementalExternalPower);
        Assert.Equal(bypass.MassFlowRate, result.Snapshot.TotalTurbineBypassMassFlowRate);
        Assert.Equal(bypass.InternalEnergyTransferRate, result.Snapshot.TotalTurbineBypassInternalEnergyTransferRate);
        Assert.InRange(Math.Abs(result.Snapshot.ThermofluidAudit.BalanceMassRateResidualKilogramsPerSecond), 0d, 1e-9d);
        Assert.InRange(Math.Abs(result.Snapshot.ThermofluidAudit.BalancePowerResidualWatts), 0d, 1e-3d);
    }

    [Fact]
    public void Step_TurbineBypassUsesCommittedCondenserBackpressureAndBlocksReverseFlow()
    {
        var fixture = CreateFixture(
            Power.Zero,
            new PreservingThermodynamicModel(),
            includeTurbineBypass: true,
            headerPressureMegapascals: 6.5d,
            exhaustPressureMegapascals: 6.5d);
        var result = new CondenserSystemSolver(fixture.Definition, fixture.ThermodynamicModel)
            .Step(fixture.PlantState, fixture.TurbineState, fixture.Inputs, TimeSpan.FromMilliseconds(100d));
        var bypass = Assert.Single(result.Snapshot.TurbineBypasses);

        Assert.Equal(Pressure.FromMegapascals(6.5d), bypass.DestinationPressure);
        Assert.Equal(MassFlowRate.Zero, bypass.MassFlowRate);
        Assert.False(bypass.IsChoked);
        Assert.Equal(Power.Zero, bypass.InternalEnergyTransferRate);
    }

    [Fact]
    public void Step_TurbineBypassLimitsIdealVaporCapacityByCommittedVaporFraction()
    {
        var dryFixture = CreateFixture(
            Power.Zero,
            new PreservingThermodynamicModel(),
            includeTurbineBypass: true,
            headerPressureMegapascals: 6.5d,
            exhaustPressureMegapascals: 0.1d);
        var wetFixture = CreateFixture(
            Power.Zero,
            new PreservingThermodynamicModel(),
            includeTurbineBypass: true,
            headerPressureMegapascals: 6.5d,
            exhaustPressureMegapascals: 0.1d,
            headerPhase: FluidPhase.SaturatedMixture,
            headerVaporQuality: VaporQuality.FromFraction(0.25d));
        var dry = Assert.Single(new TurbineBypassSolver(dryFixture.Definition).Solve(dryFixture.PlantState).Snapshots);
        var wet = Assert.Single(new TurbineBypassSolver(wetFixture.Definition).Solve(wetFixture.PlantState).Snapshots);

        Assert.Equal(1d, dry.VaporAvailabilityFraction, 12);
        Assert.Equal(0.25d, wet.VaporAvailabilityFraction, 12);
        Assert.Equal(dry.MassFlowRate.KilogramsPerSecond * 0.25d, wet.MassFlowRate.KilogramsPerSecond, 10);
    }

    [Fact]
    public void Step_LegacyCondenserDefinitionPublishesNoTurbineBypass()
    {
        var fixture = CreateFixture(Power.Zero, new PreservingThermodynamicModel());
        var result = new CondenserSystemSolver(fixture.Definition, fixture.ThermodynamicModel)
            .Step(fixture.PlantState, fixture.TurbineState, fixture.Inputs, TimeSpan.FromMilliseconds(100d));

        Assert.Empty(fixture.Definition.TurbineBypasses);
        Assert.Empty(result.Snapshot.TurbineBypasses);
        Assert.Equal(MassFlowRate.Zero, result.Snapshot.TotalTurbineBypassMassFlowRate);
        Assert.Equal(Power.Zero, result.Snapshot.TotalTurbineBypassInternalEnergyTransferRate);
    }

    private static Fixture CreateFixture(
        Power coolingPower,
        IFluidThermodynamicModel thermodynamicModel,
        ThermalConductance? overallHeatTransferConductance = null,
        Temperature? coolantTemperature = null,
        Power? installedHeatRejectionCapacity = null,
        double exhaustTemperatureCelsius = 280d,
        CondenserCondensateEnergyMode condensateEnergyMode = CondenserCondensateEnergyMode.LegacyHotwellSpecificInternalEnergy,
        bool includeTurbineBypass = false,
        double headerPressureMegapascals = 6.5d,
        double exhaustPressureMegapascals = 0.1d,
        FluidPhase headerPhase = FluidPhase.SuperheatedVapor,
        VaporQuality? headerVaporQuality = null,
        bool isolateMainSteamFlows = false)
    {
        FluidNodeDefinition Node(string id) => new(id, Volume.FromCubicMetres(10d));
        PipeDefinition Pipe(string id, string from, string to) => new(
            id, from, to, QuadraticHydraulicResistance.FromPascalSecondsSquaredPerKilogramSquared(100_000d));
        ValveDefinition Valve(string id, string from, string to) => new(
            id, Pipe($"{id}-path", from, to), ValveCharacteristic.Linear, ValveFailSafeAction.FailClosed);

        var plant = new PlantDefinition(
            "plant",
            new[]
            {
                Node("suction"), Node("pressure"), Node("outlet"), Node("drum"), Node("steam"),
                Node("header"), Node("stop-out"), Node("control-out"), Node("turbine-inlet"), Node("exhaust"), Node("hotwell"),
            },
            new[]
            {
                Pipe("channel", "pressure", "outlet"),
                Pipe("return", "outlet", "drum"),
                Pipe("main-steam-line", "steam", "header"),
            },
            new[]
            {
                Valve("stop", "header", "stop-out"),
                Valve("control", "stop-out", "control-out"),
                Valve("admission", "control-out", "turbine-inlet"),
            },
            new[]
            {
                new PumpDefinition(
                    "pump", Pipe("pump-path", "suction", "pressure"), PressureDifference.FromMegapascals(1d),
                    QuadraticHydraulicResistance.FromPascalSecondsSquaredPerKilogramSquared(50_000d), PumpEfficiency.FromPercent(80d)),
            },
            new[]
            {
                new ThermalBodyDefinition("fuel", HeatCapacity.FromJoulesPerKelvin(10_000_000d)),
                new ThermalBodyDefinition("structure", HeatCapacity.FromJoulesPerKelvin(20_000_000d)),
            },
            Array.Empty<HeatTransferDefinition>(),
            Array.Empty<HeatSourceDefinition>());

        FluidNodeState Fluid(
            string id,
            double pressureMegapascals,
            FluidPhase phase,
            double specificEnergyKilojoulesPerKilogram,
            VaporQuality? vaporQuality = null,
            double temperatureCelsius = 280d)
        {
            var mass = Mass.FromKilograms(10_000d);
            var specificEnergy = SpecificEnergy.FromKilojoulesPerKilogram(specificEnergyKilojoulesPerKilogram);
            return new FluidNodeState(
                plant.GetFluidNode(id),
                new FluidNodeInventory(mass, specificEnergy * mass),
                new FluidThermodynamicState(
                    Pressure.FromMegapascals(pressureMegapascals),
                    Temperature.FromDegreesCelsius(temperatureCelsius),
                    phase,
                    vaporQuality));
        }

        var plantState = new PlantState(
            plant,
            new[]
            {
                Fluid("suction", 6d, FluidPhase.SubcooledLiquid, 2_000d),
                Fluid("pressure", 6d, FluidPhase.SubcooledLiquid, 2_000d),
                Fluid("outlet", 6d, FluidPhase.SubcooledLiquid, 2_000d),
                Fluid("drum", 6d, FluidPhase.SubcooledLiquid, 2_000d),
                Fluid("steam", isolateMainSteamFlows ? headerPressureMegapascals : 7d, FluidPhase.SuperheatedVapor, 2_000d),
                Fluid("header", headerPressureMegapascals, headerPhase, 2_000d, headerVaporQuality),
                Fluid("stop-out", isolateMainSteamFlows ? headerPressureMegapascals : 6d, FluidPhase.SuperheatedVapor, 2_000d),
                Fluid("control-out", isolateMainSteamFlows ? headerPressureMegapascals : 5.5d, FluidPhase.SuperheatedVapor, 2_000d),
                Fluid("turbine-inlet", isolateMainSteamFlows ? headerPressureMegapascals : 5d, FluidPhase.SuperheatedVapor, 2_000d),
                Fluid(
                    "exhaust",
                    exhaustPressureMegapascals,
                    FluidPhase.SaturatedMixture,
                    2_000d,
                    VaporQuality.FromPercent(90d),
                    exhaustTemperatureCelsius),
                Fluid("hotwell", 0.1d, FluidPhase.SubcooledLiquid, 500d),
            },
            new[]
            {
                new ValveState("stop", isolateMainSteamFlows ? ValvePosition.Closed : ValvePosition.FullyOpen),
                new ValveState("control", isolateMainSteamFlows ? ValvePosition.Closed : ValvePosition.FullyOpen),
                new ValveState("admission", isolateMainSteamFlows ? ValvePosition.Closed : ValvePosition.FullyOpen),
            },
            new[] { new PumpState("pump", PumpSpeed.Stopped, isRunning: false) },
            new[]
            {
                ThermalBodyState.FromTemperature(plant.GetThermalBody("fuel"), Temperature.FromDegreesCelsius(500d)),
                ThermalBodyState.FromTemperature(plant.GetThermalBody("structure"), Temperature.FromDegreesCelsius(350d)),
            },
            Array.Empty<HeatSourceState>());

        var core = AggregatedCoreDefinition.CreateSingleZone("core", plant, "zone", "fuel", "structure", "outlet");
        var groups = new FuelChannelGroupSetDefinition(
            "groups",
            core,
            new[]
            {
                new FuelChannelGroupDefinition(
                    "group", "zone", 100, CoreZonePowerFraction.Full, "channel", "pressure", "outlet", "fuel", "structure",
                    HeatDepositionFraction.FromPercent(70d), HeatDepositionFraction.FromPercent(10d), HeatDepositionFraction.FromPercent(20d)),
            });
        var circulation = new MainCirculationSystemDefinition(
            "circulation",
            groups,
            new[]
            {
                new MainCirculationLoopDefinition(
                    "loop", "suction", "pressure", "drum", new[] { "pump" },
                    new[] { new MainCirculationBranchDefinition("group", "return") }),
            });
        var drums = new SteamDrumSystemDefinition(
            "drums", circulation, new[] { new SteamDrumDefinition("drum-a", "loop", "drum", "steam") });
        var boundaries = new PrimaryCircuitBoundarySystemDefinition(
            "boundaries",
            drums,
            new[] { new FeedwaterBoundaryDefinition("feed", "drum-a", "drum") },
            new[] { new SteamExportBoundaryDefinition("export", "drum-a", "steam") });
        var primary = new IntegratedPrimaryCircuitDefinition("primary", boundaries);
        var mainSteam = new MainSteamNetworkDefinition(
            "main-steam",
            primary,
            new[] { new MainSteamLineDefinition("line-a", "export", "main-steam-line", "header") },
            new[] { new TurbineAdmissionTrainDefinition("train-a", "header", "stop", "control", "admission", "turbine-inlet") },
            new[] { new TurbineAdmissionBoundaryDefinition("turbine-boundary", "train-a", "turbine-inlet") });
        var turbine = new TurbineExpansionSystemDefinition(
            "turbine",
            mainSteam,
            new[]
            {
                new TurbineRotorDefinition(
                    "rotor",
                    MomentOfInertia.FromKilogramSquareMetres(1_000d),
                    AngularSpeed.FromRevolutionsPerMinute(3_000d),
                    AngularSpeed.FromRevolutionsPerMinute(3_300d)),
            },
            new[]
            {
                new TurbineStageGroupDefinition(
                    "stage", "turbine-boundary", "exhaust", "rotor",
                    SpecificEnergy.FromKilojoulesPerKilogram(500d), TurbineEfficiency.FromPercent(80d)),
            });
        var definition = new CondenserSystemDefinition(
            "condensers",
            turbine,
            new[]
            {
                new CondenserDefinition(
                    "condenser", "stage", "exhaust", "hotwell", "cooling",
                    MassFlowRate.FromKilogramsPerSecond(10d),
                    overallHeatTransferConductance,
                    condensateEnergyMode),
            },
            new[]
            {
                new CondenserCoolingBoundaryDefinition(
                    "cooling",
                    "condenser",
                    installedHeatRejectionCapacity),
            },
            includeTurbineBypass
                ? new[]
                {
                    new TurbineBypassDefinition(
                        "turbine-bypass",
                        "header",
                        "condenser",
                        Pressure.FromMegapascals(6.4d),
                        Pressure.FromMegapascals(6.5d),
                        new CompressibleSteamFlowDefinition(
                            Area.FromSquareMillimetres(1_600d),
                            dischargeCoefficient: 0.95d,
                            specificGasConstant: SpecificGasConstant.FromJoulesPerKilogramKelvin(461.526d),
                            heatCapacityRatio: 1.3d)),
                }
                : Array.Empty<TurbineBypassDefinition>());

        var primaryBoundaryInputs = new PrimaryCircuitBoundaryInputs(
            boundaries,
            new[] { new FeedwaterBoundaryInput("feed", MassFlowRate.Zero, SpecificEnergy.Zero) },
            new[] { new SteamExportBoundaryInput("export", MassFlowRate.Zero) });
        var primaryInputs = new IntegratedPrimaryCircuitInputs(
            primary,
            AggregatedCoreState.CreateNominal(core),
            Power.Zero,
            Power.Zero,
            primaryBoundaryInputs);
        var mainSteamInputs = new MainSteamNetworkInputs(
            mainSteam,
            primaryInputs,
            new[] { new TurbineAdmissionBoundaryInput("turbine-boundary", MassFlowRate.Zero) });
        var turbineInputs = new TurbineExpansionInputs(
            turbine,
            mainSteamInputs,
            new[] { new TurbineStageGroupInput("stage", MassFlowRate.Zero) },
            new[] { new TurbineRotorInput("rotor", Torque.Zero, tripCommand: false) });
        var inputs = new CondenserSystemInputs(
            definition,
            turbineInputs,
            new[] { new CondenserCoolingBoundaryInput("cooling", coolingPower, coolantTemperature) });
        var turbineState = new TurbineExpansionState(
            turbine,
            new[] { new TurbineRotorState("rotor", AngularSpeed.FromRevolutionsPerMinute(3_000d)) });

        return new Fixture(definition, plantState, turbineState, inputs, thermodynamicModel);
    }

    private sealed record Fixture(
        CondenserSystemDefinition Definition,
        PlantState PlantState,
        TurbineExpansionState TurbineState,
        CondenserSystemInputs Inputs,
        IFluidThermodynamicModel ThermodynamicModel);

    private sealed class PreservingThermodynamicModel : IFluidThermodynamicModel
    {
        public FluidThermodynamicState Resolve(
            FluidNodeDefinition definition,
            FluidNodeInventory inventory,
            FluidThermodynamicState previousState)
            => previousState;
    }



    private sealed class FixedSaturationPreservingThermodynamicModel : IFluidThermodynamicModel, IWaterSteamSaturationPropertyProvider
    {
        private readonly SpecificEnergy _saturatedLiquidInternalEnergy;

        public FixedSaturationPreservingThermodynamicModel(SpecificEnergy saturatedLiquidInternalEnergy)
        {
            _saturatedLiquidInternalEnergy = saturatedLiquidInternalEnergy;
        }

        public Pressure? LastRequestedSaturationPressure { get; private set; }

        public FluidThermodynamicState Resolve(
            FluidNodeDefinition definition,
            FluidNodeInventory inventory,
            FluidThermodynamicState previousState)
            => previousState;

        public WaterSteamSaturationProperties GetSaturationProperties(Temperature temperature)
            => CreateProperties(temperature, Pressure.FromMegapascals(0.1d));

        public WaterSteamSaturationProperties GetSaturationProperties(Pressure pressure)
        {
            LastRequestedSaturationPressure = pressure;
            return CreateProperties(Temperature.FromDegreesCelsius(100d), pressure);
        }

        private WaterSteamSaturationProperties CreateProperties(Temperature temperature, Pressure pressure)
            => new(
                temperature,
                pressure,
                Density.FromKilogramsPerCubicMetre(950d),
                Density.FromKilogramsPerCubicMetre(0.6d),
                _saturatedLiquidInternalEnergy,
                SpecificEnergy.FromKilojoulesPerKilogram(2_600d));
    }


    private sealed class ExhaustMassPressureThermodynamicModel : IFluidThermodynamicModel
    {
        public FluidThermodynamicState Resolve(
            FluidNodeDefinition definition,
            FluidNodeInventory inventory,
            FluidThermodynamicState previousState)
        {
            if (!string.Equals(definition.Id, "exhaust", StringComparison.Ordinal))
            {
                return previousState;
            }

            return new FluidThermodynamicState(
                Pressure.FromPascals(inventory.Mass.Kilograms * 10d),
                previousState.Temperature,
                previousState.Phase,
                previousState.VaporQuality);
        }
    }
}
