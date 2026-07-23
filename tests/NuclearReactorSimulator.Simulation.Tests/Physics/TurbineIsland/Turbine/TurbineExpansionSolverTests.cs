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
using NuclearReactorSimulator.Domain.Physics.TurbineIsland.Turbine;
using NuclearReactorSimulator.Domain.Plant;
using NuclearReactorSimulator.Simulation.Physics.Fluids;
using NuclearReactorSimulator.Simulation.Physics.Reactor.Core;
using NuclearReactorSimulator.Simulation.Physics.Reactor.PrimaryCircuit.Boundaries;
using NuclearReactorSimulator.Simulation.Physics.Reactor.PrimaryCircuit.Integration;
using NuclearReactorSimulator.Simulation.Physics.TurbineIsland.MainSteam;
using NuclearReactorSimulator.Simulation.Physics.TurbineIsland.Turbine;
using Xunit;

namespace NuclearReactorSimulator.Simulation.Tests.Physics.TurbineIsland.Turbine;

public sealed class TurbineExpansionSolverTests
{
    [Fact]
    public void Step_TransfersSteamToExhaustExtractsShaftPowerAndClosesBothAudits()
    {
        var ratedSpeed = AngularSpeed.FromRevolutionsPerMinute(3_000d);
        var expectedTorque = Torque.FromNewtonMetres(400_000d / ratedSpeed.RadiansPerSecond);
        var fixture = CreateFixture(3_000d, expectedTorque, tripCommand: false);
        var solver = new TurbineExpansionSolver(fixture.Definition, new PreservingThermodynamicModel());

        var result = solver.Step(fixture.PlantState, fixture.TurbineState, fixture.Inputs, TimeSpan.FromMilliseconds(1d));
        var stage = Assert.Single(result.Snapshot.StageGroups);
        var rotor = Assert.Single(result.Snapshot.Rotors);

        Assert.Equal(1d, stage.EffectiveMassFlowRate.KilogramsPerSecond, 12);
        Assert.Equal(400d, stage.ExtractedSpecificWork.KilojoulesPerKilogram, 9);
        Assert.Equal(400d, stage.ShaftPower.Kilowatts, 9);
        Assert.Equal(1_600d, stage.ExhaustSpecificInternalEnergy.KilojoulesPerKilogram, 9);
        Assert.Equal(1_600d, stage.ExhaustEnergyFlowRate.Kilowatts, 9);
        Assert.Equal(3_000d, rotor.FinalAngularSpeed.RevolutionsPerMinute, 8);
        Assert.Equal(-400d, result.Snapshot.ThermofluidAudit.SupplementalExternalPower.Kilowatts, 9);
        Assert.InRange(Math.Abs(result.Snapshot.ThermofluidAudit.BalanceMassRateResidualKilogramsPerSecond), 0d, 1e-12d);
        Assert.InRange(Math.Abs(result.Snapshot.ThermofluidAudit.BalancePowerResidualWatts), 0d, 1e-6d);
        Assert.InRange(Math.Abs(result.Snapshot.MechanicalAudit.MechanicalEnergyClosureResidualJoules), 0d, 1e-6d);
        Assert.Equal(0d, result.MainSteamStep.Snapshot.TotalTurbineAdmissionMassFlowRate.KilogramsPerSecond, 12);
    }

    [Fact]
    public void Step_FromRestAcceleratesRotorWithDeterministicTorqueEnergyClosure()
    {
        var fixture = CreateFixture(0d, Torque.Zero, tripCommand: false);
        var solver = new TurbineExpansionSolver(fixture.Definition, new PreservingThermodynamicModel());

        var result = solver.Step(fixture.PlantState, fixture.TurbineState, fixture.Inputs, TimeSpan.FromMilliseconds(100d));
        var rotor = Assert.Single(result.Snapshot.Rotors);

        Assert.True(rotor.FinalAngularSpeed > AngularSpeed.Zero);
        Assert.True(rotor.ShaftPower > Power.Zero);
        Assert.True(rotor.FinalKineticEnergy > rotor.InitialKineticEnergy);
        Assert.InRange(Math.Abs(result.Snapshot.MechanicalAudit.MechanicalEnergyClosureResidualJoules), 0d, 1e-6d);
    }

    [Fact]
    public void Step_ExcessiveLoadTorqueStopsAtZeroWithoutNumericalReverseRotation()
    {
        var fixture = CreateFixture(10d, Torque.FromNewtonMetres(1_000_000d), tripCommand: false);
        var solver = new TurbineExpansionSolver(fixture.Definition, new PreservingThermodynamicModel());

        var result = solver.Step(fixture.PlantState, fixture.TurbineState, fixture.Inputs, TimeSpan.FromSeconds(1d));
        var rotor = Assert.Single(result.Snapshot.Rotors);

        Assert.Equal(AngularSpeed.Zero, rotor.FinalAngularSpeed);
        Assert.True(rotor.ExternalLoadTorqueLimitedAtZeroSpeed);
        Assert.True(rotor.EffectiveExternalLoadTorque < rotor.CommandedExternalLoadTorque);
        Assert.InRange(Math.Abs(result.Snapshot.MechanicalAudit.MechanicalEnergyClosureResidualJoules), 0d, 1e-6d);
    }

    [Fact]
    public void Step_IsDeterministicForIdenticalCommittedPlantAndRotorState()
    {
        var fixture = CreateFixture(2_500d, Torque.FromNewtonMetres(500d), tripCommand: false);
        var solver = new TurbineExpansionSolver(fixture.Definition, new PreservingThermodynamicModel());

        var left = solver.Step(fixture.PlantState, fixture.TurbineState, fixture.Inputs, TimeSpan.FromMilliseconds(10d));
        var right = solver.Step(fixture.PlantState, fixture.TurbineState, fixture.Inputs, TimeSpan.FromMilliseconds(10d));

        Assert.Equal(left.CandidateTurbineState.GetRotor("rotor"), right.CandidateTurbineState.GetRotor("rotor"));
        Assert.Equal(left.Snapshot.TotalShaftPower, right.Snapshot.TotalShaftPower);
        Assert.Equal(left.Snapshot.MechanicalAudit, right.Snapshot.MechanicalAudit);
        Assert.Equal(left.CandidatePlantState.GetFluidNode("turbine-inlet").Inventory, right.CandidatePlantState.GetFluidNode("turbine-inlet").Inventory);
        Assert.Equal(left.CandidatePlantState.GetFluidNode("exhaust").Inventory, right.CandidatePlantState.GetFluidNode("exhaust").Inventory);
    }

    [Fact]
    public void Step_TripCommandBlocksExpansionWithoutInventingAutomaticProtectionState()
    {
        var fixture = CreateFixture(3_000d, Torque.Zero, tripCommand: true);
        var solver = new TurbineExpansionSolver(fixture.Definition, new PreservingThermodynamicModel());

        var result = solver.Step(fixture.PlantState, fixture.TurbineState, fixture.Inputs, TimeSpan.FromMilliseconds(1d));
        var stage = Assert.Single(result.Snapshot.StageGroups);
        var rotor = Assert.Single(result.Snapshot.Rotors);

        Assert.True(stage.TripBlocked);
        Assert.Equal(MassFlowRate.Zero, stage.EffectiveMassFlowRate);
        Assert.Equal(Power.Zero, stage.ShaftPower);
        Assert.True(rotor.TripCommandActive);
    }

    [Fact]
    public void Step_OverspeedIsObservableButDoesNotAutoTrip()
    {
        var fixture = CreateFixture(3_400d, Torque.Zero, tripCommand: false);
        var solver = new TurbineExpansionSolver(fixture.Definition, new PreservingThermodynamicModel());

        var result = solver.Step(fixture.PlantState, fixture.TurbineState, fixture.Inputs, TimeSpan.FromMilliseconds(1d));
        var stage = Assert.Single(result.Snapshot.StageGroups);
        var rotor = Assert.Single(result.Snapshot.Rotors);

        Assert.True(rotor.OverspeedDetectedAtStart);
        Assert.False(rotor.TripCommandActive);
        Assert.Equal(1d, stage.EffectiveMassFlowRate.KilogramsPerSecond, 12);
    }

    [Fact]
    public void Step_ThermodynamicWorkFallsAsExhaustBackpressureApproachesInletPressure()
    {
        var work = CurrentThermodynamicWork();
        var lowBackpressure = CreateFixture(
            3_000d,
            Torque.Zero,
            tripCommand: false,
            thermodynamicWork: work,
            inletPressureMegapascals: 5d,
            exhaustPressureMegapascals: 0.1d);
        var highBackpressure = CreateFixture(
            3_000d,
            Torque.Zero,
            tripCommand: false,
            thermodynamicWork: work,
            inletPressureMegapascals: 5d,
            exhaustPressureMegapascals: 4.9d);

        var lowResult = new TurbineExpansionSolver(lowBackpressure.Definition, new PreservingThermodynamicModel())
            .Step(lowBackpressure.PlantState, lowBackpressure.TurbineState, lowBackpressure.Inputs, TimeSpan.FromMilliseconds(1d));
        var highResult = new TurbineExpansionSolver(highBackpressure.Definition, new PreservingThermodynamicModel())
            .Step(highBackpressure.PlantState, highBackpressure.TurbineState, highBackpressure.Inputs, TimeSpan.FromMilliseconds(1d));

        var lowStage = Assert.Single(lowResult.Snapshot.StageGroups);
        var highStage = Assert.Single(highResult.Snapshot.StageGroups);

        Assert.True(lowStage.ThermodynamicWorkModelActive);
        Assert.Equal(500d, lowStage.EffectiveIdealSpecificWork.KilojoulesPerKilogram, 9);
        Assert.InRange(lowStage.ExtractedSpecificWork.KilojoulesPerKilogram, 399.9d, 400.1d);
        Assert.False(lowStage.ThermodynamicWorkLimited);
        Assert.True(highStage.ThermodynamicWorkModelActive);
        Assert.True(highStage.ThermodynamicWorkLimited);
        Assert.True(highStage.EffectiveIdealSpecificWork < lowStage.EffectiveIdealSpecificWork);
        Assert.True(highStage.ExtractedSpecificWork < lowStage.ExtractedSpecificWork);
        Assert.True(highStage.ExhaustEnergyFlowRate >= Power.Zero);
    }

    [Fact]
    public void Step_ThermodynamicWorkDegradesToZeroForLiquidAdmissionWithoutThrowing()
    {
        var fixture = CreateFixture(
            3_000d,
            Torque.Zero,
            tripCommand: false,
            thermodynamicWork: CurrentThermodynamicWork(),
            inletPhase: FluidPhase.SubcooledLiquid);
        var solver = new TurbineExpansionSolver(fixture.Definition, new PreservingThermodynamicModel());

        var result = solver.Step(
            fixture.PlantState,
            fixture.TurbineState,
            fixture.Inputs,
            TimeSpan.FromMilliseconds(1d));
        var stage = Assert.Single(result.Snapshot.StageGroups);

        Assert.True(stage.ThermodynamicWorkModelActive);
        Assert.True(stage.ThermodynamicWorkLimited);
        Assert.Equal(SpecificEnergy.Zero, stage.PressureTemperatureAvailableSpecificWork);
        Assert.Equal(SpecificEnergy.Zero, stage.EffectiveIdealSpecificWork);
        Assert.Equal(SpecificEnergy.Zero, stage.ExtractedSpecificWork);
        Assert.Equal(Power.Zero, stage.ShaftPower);
        Assert.Equal(stage.InletEnergyFlowRate, stage.ExhaustEnergyFlowRate);
    }

    [Fact]
    public void Step_ThermodynamicWorkBoundsLowEnergyAdmissionInsteadOfThrowing()
    {
        var ratedSpeed = AngularSpeed.FromRevolutionsPerMinute(3_000d);
        var expectedTorque = Torque.FromNewtonMetres(64_000d / ratedSpeed.RadiansPerSecond);
        var fixture = CreateFixture(
            3_000d,
            expectedTorque,
            tripCommand: false,
            thermodynamicWork: CurrentThermodynamicWork(),
            inletSpecificInternalEnergyKilojoulesPerKilogram: 100d);
        var solver = new TurbineExpansionSolver(fixture.Definition, new PreservingThermodynamicModel());

        var result = solver.Step(
            fixture.PlantState,
            fixture.TurbineState,
            fixture.Inputs,
            TimeSpan.FromMilliseconds(1d));
        var stage = Assert.Single(result.Snapshot.StageGroups);

        Assert.True(stage.ThermodynamicWorkLimited);
        Assert.Equal(80d, stage.InletEnergyBoundedSpecificWork.KilojoulesPerKilogram, 9);
        Assert.Equal(80d, stage.EffectiveIdealSpecificWork.KilojoulesPerKilogram, 9);
        Assert.Equal(64d, stage.ExtractedSpecificWork.KilojoulesPerKilogram, 9);
        Assert.Equal(36d, stage.ExhaustSpecificInternalEnergy.KilojoulesPerKilogram, 9);
        Assert.True(stage.ExhaustEnergyFlowRate >= Power.Zero);
    }

    [Fact]
    public void Inputs_RejectNonZeroM41TerminalBoundaryWhileM42OwnsExpansion()
    {
        var fixture = CreateFixture(3_000d, Torque.Zero, tripCommand: false);
        var mainSteam = fixture.Definition.MainSteamNetwork;
        var nonZeroMainSteamInputs = new MainSteamNetworkInputs(
            mainSteam,
            fixture.Inputs.MainSteamInputs.PrimaryCircuitInputs,
            new[] { new TurbineAdmissionBoundaryInput("turbine-boundary", MassFlowRate.FromKilogramsPerSecond(1d)) });

        Assert.Throws<ArgumentException>(() => new TurbineExpansionInputs(
            fixture.Definition,
            nonZeroMainSteamInputs,
            fixture.Inputs.StageGroupInputs,
            fixture.Inputs.RotorInputs));
    }

    private static Fixture CreateFixture(
        double rotorSpeedRpm,
        Torque loadTorque,
        bool tripCommand,
        TurbineThermodynamicWorkDefinition? thermodynamicWork = null,
        double inletPressureMegapascals = 5d,
        double exhaustPressureMegapascals = 0.1d,
        FluidPhase inletPhase = FluidPhase.SuperheatedVapor,
        VaporQuality? inletVaporQuality = null,
        double inletSpecificInternalEnergyKilojoulesPerKilogram = 2_000d)
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
                Node("header"), Node("stop-out"), Node("control-out"), Node("turbine-inlet"), Node("exhaust"),
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
            VaporQuality? vaporQuality = null,
            double specificInternalEnergyKilojoulesPerKilogram = 2_000d)
        {
            var mass = Mass.FromKilograms(10_000d);
            return new FluidNodeState(
                plant.GetFluidNode(id),
                new FluidNodeInventory(
                    mass,
                    SpecificEnergy.FromKilojoulesPerKilogram(specificInternalEnergyKilojoulesPerKilogram) * mass),
                new FluidThermodynamicState(
                    Pressure.FromMegapascals(pressureMegapascals),
                    Temperature.FromDegreesCelsius(280d),
                    phase,
                    vaporQuality));
        }

        var plantState = new PlantState(
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
                Fluid(
                    "turbine-inlet",
                    inletPressureMegapascals,
                    inletPhase,
                    inletVaporQuality,
                    inletSpecificInternalEnergyKilojoulesPerKilogram),
                Fluid("exhaust", exhaustPressureMegapascals, FluidPhase.SaturatedMixture, VaporQuality.FromPercent(90d)),
            },
            new[]
            {
                new ValveState("stop", ValvePosition.FullyOpen),
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
        var rotor = new TurbineRotorDefinition(
            "rotor",
            MomentOfInertia.FromKilogramSquareMetres(1_000d),
            AngularSpeed.FromRevolutionsPerMinute(3_000d),
            AngularSpeed.FromRevolutionsPerMinute(3_300d));
        var definition = new TurbineExpansionSystemDefinition(
            "turbine",
            mainSteam,
            new[] { rotor },
            new[]
            {
                new TurbineStageGroupDefinition(
                    "stage", "turbine-boundary", "exhaust", "rotor",
                    SpecificEnergy.FromKilojoulesPerKilogram(500d),
                    TurbineEfficiency.FromPercent(80d),
                    expansionResistance: null,
                    thermodynamicWork: thermodynamicWork),
            });

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
        var inputs = new TurbineExpansionInputs(
            definition,
            mainSteamInputs,
            new[] { new TurbineStageGroupInput("stage", MassFlowRate.FromKilogramsPerSecond(1d)) },
            new[] { new TurbineRotorInput("rotor", loadTorque, tripCommand) });
        var turbineState = new TurbineExpansionState(
            definition,
            new[] { new TurbineRotorState("rotor", AngularSpeed.FromRevolutionsPerMinute(rotorSpeedRpm)) });

        return new Fixture(definition, plantState, turbineState, inputs);
    }


    private static TurbineThermodynamicWorkDefinition CurrentThermodynamicWork()
        => new(
            SpecificHeatCapacity.FromKilojoulesPerKilogramKelvin(2.1d),
            heatCapacityRatio: 1.3d,
            maximumInletInternalEnergyExtractionFraction: 0.8d);

    private sealed record Fixture(
        TurbineExpansionSystemDefinition Definition,
        PlantState PlantState,
        TurbineExpansionState TurbineState,
        TurbineExpansionInputs Inputs);

    private sealed class PreservingThermodynamicModel : IFluidThermodynamicModel
    {
        public FluidThermodynamicState Resolve(
            FluidNodeDefinition definition,
            FluidNodeInventory inventory,
            FluidThermodynamicState previousState)
            => previousState;
    }
}
