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
using NuclearReactorSimulator.Domain.Physics.TurbineIsland.MainSteam;
using NuclearReactorSimulator.Domain.Plant;
using NuclearReactorSimulator.Simulation.Physics.Fluids;
using NuclearReactorSimulator.Simulation.Physics.Reactor.PrimaryCircuit.Boundaries;
using NuclearReactorSimulator.Simulation.Physics.Reactor.PrimaryCircuit.Integration;
using NuclearReactorSimulator.Simulation.Physics.TurbineIsland.MainSteam;
using Xunit;

namespace NuclearReactorSimulator.Simulation.Tests.Physics.TurbineIsland.MainSteam;

public sealed class MainSteamNetworkSolverTests
{
    [Fact]
    public void Step_TransportsSteamToReplaceableTurbineBoundaryWithSingleConservationAudit()
    {
        var fixture = CreateFixture();
        var solver = new MainSteamNetworkSolver(fixture.Definition, new PreservingThermodynamicModel());

        var result = solver.Step(fixture.State, fixture.Inputs, TimeSpan.FromMilliseconds(1d));

        var line = Assert.Single(result.Snapshot.SteamLines);
        var train = Assert.Single(result.Snapshot.AdmissionTrains);
        var boundary = Assert.Single(result.Snapshot.TurbineAdmissionBoundaries);

        Assert.True(line.MassFlowRate > MassFlowRate.Zero);
        Assert.True(train.StopValve.MassFlowRate > MassFlowRate.Zero);
        Assert.True(train.ControlValve.MassFlowRate > MassFlowRate.Zero);
        Assert.True(train.AdmissionValve.MassFlowRate > MassFlowRate.Zero);
        Assert.Equal(1d, boundary.MassFlowRate.KilogramsPerSecond, 12);
        Assert.Equal(0d, result.Snapshot.PrimaryCircuit.TotalSteamExportMassFlowRate.KilogramsPerSecond, 12);
        Assert.Equal(-1d, result.Snapshot.Audit.ExpectedExternalMassFlowRate.KilogramsPerSecond, 12);
        Assert.Equal(-2d, result.Snapshot.Audit.ExpectedExternalPower.Megawatts, 9);
        Assert.InRange(Math.Abs(result.Snapshot.Audit.BalanceMassRateResidualKilogramsPerSecond), 0d, 1e-12d);
        Assert.InRange(Math.Abs(result.Snapshot.Audit.BalancePowerResidualWatts), 0d, 1e-6d);
        Assert.InRange(Math.Abs(result.Snapshot.Audit.MassClosureResidualKilograms), 0d, 1e-9d);
        Assert.InRange(Math.Abs(result.Snapshot.Audit.EnergyClosureResidualJoules), 0d, 1e-3d);
        Assert.Same(result.CandidateState, result.PrimaryCircuitStep.CandidateState);
    }

    [Fact]
    public void Inputs_RejectNonZeroLegacyM3SteamExportWhileMainSteamNetworkOwnsThePath()
    {
        var fixture = CreateFixture();
        var boundaries = fixture.Definition.PrimaryCircuit.BoundarySystem;
        var nonZeroLegacyBoundaryInputs = new PrimaryCircuitBoundaryInputs(
            boundaries,
            new[] { new FeedwaterBoundaryInput("feed", MassFlowRate.Zero, SpecificEnergy.Zero) },
            new[] { new SteamExportBoundaryInput("export", MassFlowRate.FromKilogramsPerSecond(1d)) });
        var primaryInputs = new IntegratedPrimaryCircuitInputs(
            fixture.Definition.PrimaryCircuit,
            fixture.Inputs.PrimaryCircuitInputs.CoreState,
            Power.Zero,
            Power.Zero,
            nonZeroLegacyBoundaryInputs);

        Assert.Throws<ArgumentException>(() => new MainSteamNetworkInputs(
            fixture.Definition,
            primaryInputs,
            new[] { new TurbineAdmissionBoundaryInput("turbine-boundary", MassFlowRate.Zero) }));
    }

    [Fact]
    public void Step_ValveFailSafeIsVisibleInCommittedStateAdmissionDiagnostics()
    {
        var fixture = CreateFixture(stopValveFailSafeActive: true);
        var solver = new MainSteamNetworkSolver(fixture.Definition, new PreservingThermodynamicModel());

        var result = solver.Step(fixture.State, fixture.Inputs, TimeSpan.FromMilliseconds(1d));
        var train = Assert.Single(result.Snapshot.AdmissionTrains);

        Assert.Equal(ValvePosition.Closed, train.StopValve.EffectivePosition);
        Assert.Equal(MassFlowRate.Zero, train.StopValve.MassFlowRate);
    }

    [Fact]
    public void Step_CirculationDemandBalanced_ReplenishesMainSteamLineFromDrumConservatively()
    {
        var fixture = CreateFixture(circulationDemandBalanced: true);
        var solver = new MainSteamNetworkSolver(fixture.Definition, new PreservingThermodynamicModel());
        var initialDrum = fixture.State.GetFluidNode("drum");
        var initialSteam = fixture.State.GetFluidNode("steam");
        var deltaTime = TimeSpan.FromMilliseconds(1d);

        var result = solver.Step(fixture.State, fixture.Inputs, deltaTime);

        var line = Assert.Single(result.Snapshot.SteamLines);
        var candidateDrum = result.CandidateState.GetFluidNode("drum");
        var candidateSteam = result.CandidateState.GetFluidNode("steam");
        var transferredMass = line.MassFlowRate.KilogramsPerSecond * deltaTime.TotalSeconds;
        var transferredEnergy = initialSteam.SpecificInternalEnergy.JoulesPerKilogram * transferredMass;

        Assert.True(line.MassFlowRate > MassFlowRate.Zero);
        Assert.Equal(initialSteam.Mass.Kilograms, candidateSteam.Mass.Kilograms, 9);
        Assert.Equal(initialSteam.InternalEnergy.Joules, candidateSteam.InternalEnergy.Joules, 3);
        Assert.Equal(initialDrum.Mass.Kilograms - transferredMass, candidateDrum.Mass.Kilograms, 9);
        Assert.Equal(initialDrum.InternalEnergy.Joules - transferredEnergy, candidateDrum.InternalEnergy.Joules, 3);
        Assert.InRange(Math.Abs(result.Snapshot.Audit.BalanceMassRateResidualKilogramsPerSecond), 0d, 1e-12d);
        Assert.InRange(Math.Abs(result.Snapshot.Audit.BalancePowerResidualWatts), 0d, 1e-6d);
    }

    [Fact]
    public void Step_LegacyReturnSplit_DoesNotApplyDemandBalancedSteamSupply()
    {
        var fixture = CreateFixture();
        var solver = new MainSteamNetworkSolver(fixture.Definition, new PreservingThermodynamicModel());
        var initialSteamMass = fixture.State.GetFluidNode("steam").Mass.Kilograms;
        var deltaTime = TimeSpan.FromMilliseconds(1d);

        var result = solver.Step(fixture.State, fixture.Inputs, deltaTime);

        var line = Assert.Single(result.Snapshot.SteamLines);
        Assert.Equal(
            initialSteamMass - (line.MassFlowRate.KilogramsPerSecond * deltaTime.TotalSeconds),
            result.CandidateState.GetFluidNode("steam").Mass.Kilograms,
            9);
    }

    private static Fixture CreateFixture(
        bool stopValveFailSafeActive = false,
        bool circulationDemandBalanced = false)
    {
        FluidNodeDefinition Node(string id) => new(id, Volume.FromCubicMetres(10d));
        PipeDefinition Pipe(string id, string from, string to) => new(
            id,
            from,
            to,
            QuadraticHydraulicResistance.FromPascalSecondsSquaredPerKilogramSquared(100_000d));
        ValveDefinition Valve(string id, string from, string to) => new(
            id,
            Pipe($"{id}-path", from, to),
            ValveCharacteristic.Linear,
            ValveFailSafeAction.FailClosed);

        var plant = new PlantDefinition(
            "plant",
            new[]
            {
                Node("suction"), Node("pressure"), Node("outlet"), Node("drum"), Node("steam"),
                Node("header"), Node("stop-out"), Node("control-out"), Node("turbine-inlet"),
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
                    "pump",
                    Pipe("pump-path", "suction", "pressure"),
                    PressureDifference.FromMegapascals(1d),
                    QuadraticHydraulicResistance.FromPascalSecondsSquaredPerKilogramSquared(50_000d),
                    PumpEfficiency.FromPercent(80d)),
            },
            new[]
            {
                new ThermalBodyDefinition("fuel", HeatCapacity.FromJoulesPerKelvin(10_000_000d)),
                new ThermalBodyDefinition("structure", HeatCapacity.FromJoulesPerKelvin(20_000_000d)),
            },
            Array.Empty<HeatTransferDefinition>(),
            Array.Empty<HeatSourceDefinition>());

        FluidNodeState Fluid(string id, double pressureMegapascals, FluidPhase phase)
            => new(
                plant.GetFluidNode(id),
                new FluidNodeInventory(Mass.FromKilograms(10_000d), Energy.FromMegajoules(20_000d)),
                new FluidThermodynamicState(
                    Pressure.FromMegapascals(pressureMegapascals),
                    Temperature.FromDegreesCelsius(280d),
                    phase,
                    null));

        var state = new PlantState(
            plant,
            new[]
            {
                Fluid("suction", 6d, FluidPhase.SubcooledLiquid),
                Fluid("pressure", 6d, FluidPhase.SubcooledLiquid),
                Fluid("outlet", 6d, FluidPhase.SubcooledLiquid),
                Fluid("drum", 6d, FluidPhase.SubcooledLiquid),
                Fluid("steam", 7d, FluidPhase.SuperheatedVapor),
                Fluid("header", 6.5d, FluidPhase.SuperheatedVapor),
                Fluid("stop-out", 6d, FluidPhase.SuperheatedVapor),
                Fluid("control-out", 5.5d, FluidPhase.SuperheatedVapor),
                Fluid("turbine-inlet", 5d, FluidPhase.SuperheatedVapor),
            },
            new[]
            {
                new ValveState("stop", ValvePosition.FullyOpen, stopValveFailSafeActive),
                new ValveState("control", ValvePosition.FullyOpen),
                new ValveState("admission", ValvePosition.FullyOpen),
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
                    HeatDepositionFraction.FromPercent(70d),
                    HeatDepositionFraction.FromPercent(10d),
                    HeatDepositionFraction.FromPercent(20d)),
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
            "drums",
            circulation,
            new[]
            {
                new SteamDrumDefinition(
                    "drum-a",
                    "loop",
                    "drum",
                    "steam",
                    circulationDemandBalanced
                        ? SteamDrumLiquidRecirculationMode.CirculationDemandBalanced
                        : SteamDrumLiquidRecirculationMode.LegacyReturnSplit),
            });
        var boundaries = new PrimaryCircuitBoundarySystemDefinition(
            "boundaries",
            drums,
            new[] { new FeedwaterBoundaryDefinition("feed", "drum-a", "drum") },
            new[] { new SteamExportBoundaryDefinition("export", "drum-a", "steam") });
        var primary = new IntegratedPrimaryCircuitDefinition("primary", boundaries);
        var definition = new MainSteamNetworkDefinition(
            "main-steam",
            primary,
            new[] { new MainSteamLineDefinition("line-a", "export", "main-steam-line", "header") },
            new[] { new TurbineAdmissionTrainDefinition("train-a", "header", "stop", "control", "admission", "turbine-inlet") },
            new[] { new TurbineAdmissionBoundaryDefinition("turbine-boundary", "train-a", "turbine-inlet") });

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
        var inputs = new MainSteamNetworkInputs(
            definition,
            primaryInputs,
            new[] { new TurbineAdmissionBoundaryInput("turbine-boundary", MassFlowRate.FromKilogramsPerSecond(1d)) });

        return new Fixture(definition, state, inputs);
    }

    private sealed record Fixture(
        MainSteamNetworkDefinition Definition,
        PlantState State,
        MainSteamNetworkInputs Inputs);

    private sealed class PreservingThermodynamicModel : IFluidThermodynamicModel
    {
        public FluidThermodynamicState Resolve(
            FluidNodeDefinition definition,
            FluidNodeInventory inventory,
            FluidThermodynamicState previousState)
            => previousState;
    }
}
