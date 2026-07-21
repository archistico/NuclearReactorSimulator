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
using NuclearReactorSimulator.Domain.Plant;
using NuclearReactorSimulator.Simulation.Physics.Fluids;
using NuclearReactorSimulator.Simulation.Physics.Reactor.Core;
using NuclearReactorSimulator.Simulation.Physics.Reactor.Core.Channels;
using NuclearReactorSimulator.Simulation.Physics.Reactor.PrimaryCircuit.Boundaries;
using NuclearReactorSimulator.Simulation.Physics.Reactor.PrimaryCircuit.Circulation;
using NuclearReactorSimulator.Simulation.Physics.Reactor.PrimaryCircuit.Integration;
using NuclearReactorSimulator.Simulation.Physics.Reactor.PrimaryCircuit.SteamDrums;
using NuclearReactorSimulator.Simulation.Plant;
using Xunit;

namespace NuclearReactorSimulator.Simulation.Tests.Physics.Reactor.PrimaryCircuit.Integration;

public sealed class IntegratedPrimaryCircuitSolverTests
{
    [Fact]
    public void Step_ComposesCoreChannelsCirculationDrumsAndBoundariesBeforeSingleIntegration()
    {
        var fixture = CreateFixture(Power.FromMegawatts(100d), Power.FromMegawatts(5d));
        var solver = new IntegratedPrimaryCircuitSolver(fixture.Definition, new SimplifiedWaterSteamThermodynamicModel());

        var result = solver.Step(fixture.State, fixture.Inputs, TimeSpan.FromMilliseconds(20d));

        Assert.Same(fixture.Definition.CoreDefinition, result.Snapshot.Core.Definition);
        Assert.Equal(105d, result.Snapshot.ChannelGroups.TotalNuclearHeatPower.Megawatts, 12);
        Assert.Single(result.Snapshot.MainCirculation.Loops);
        Assert.Single(result.Snapshot.SteamDrums.Drums);
        Assert.Single(result.Snapshot.Boundaries.FeedwaterBoundaries);
        Assert.Single(result.Snapshot.Boundaries.SteamExportBoundaries);
        Assert.Same(result.CandidateState, result.NetworkStep.CandidateState);
        Assert.Equal(105d, result.NetworkStep.Audit.SupplementalExternalPower.Megawatts, 9);
        Assert.InRange(Math.Abs(result.NetworkStep.Audit.BalanceMassRateResidualKilogramsPerSecond), 0d, 1e-12d);
        Assert.InRange(Math.Abs(result.NetworkStep.Audit.BalancePowerResidualWatts), 0d, 1e-3d);
        Assert.InRange(Math.Abs(result.NetworkStep.Audit.EnergyClosureResidualJoules), 0d, 10d);
        Assert.Equal(result.NetworkStep.Audit.FinalTotalMass, result.Snapshot.TotalPlantMass);
        Assert.Equal(result.NetworkStep.Audit.FinalTotalStoredEnergy, result.Snapshot.TotalStoredEnergy);
    }

    [Fact]
    public void CompleteM3SourceTermComposition_IsIndependentOfContributionOrder()
    {
        var fixture = CreateFixture(Power.FromMegawatts(100d), Power.FromMegawatts(5d));
        var core = new AggregatedCorePowerSolver(fixture.Definition.CoreDefinition)
            .Solve(fixture.Inputs.CoreState, fixture.Inputs.TotalFissionThermalPower, fixture.State);
        var channels = new FuelChannelGroupSolver(fixture.Definition.ChannelGroups)
            .Solve(core, fixture.Inputs.TotalDecayHeatPower, fixture.State);
        var circulation = new MainCirculationSystemSolver(fixture.Definition.MainCirculationSystem)
            .Solve(fixture.State);
        var drums = new SteamDrumSeparationSolver(fixture.Definition.SteamDrumSystem)
            .Solve(fixture.State, circulation);
        var boundaries = new PrimaryCircuitBoundarySolver(fixture.Definition.BoundarySystem)
            .Solve(fixture.State, fixture.Inputs.BoundaryInputs);

        var leftTerms = PlantNetworkSourceTerms.Combine(channels.SourceTerms, drums.SourceTerms, boundaries.SourceTerms);
        var rightTerms = PlantNetworkSourceTerms.Combine(boundaries.SourceTerms, drums.SourceTerms, channels.SourceTerms);
        var left = new PlantNetworkOrchestrator(new SimplifiedWaterSteamThermodynamicModel())
            .Step(fixture.State, TimeSpan.FromMilliseconds(20d), leftTerms);
        var right = new PlantNetworkOrchestrator(new SimplifiedWaterSteamThermodynamicModel())
            .Step(fixture.State, TimeSpan.FromMilliseconds(20d), rightTerms);

        Assert.Equal(left.Audit, right.Audit);
        foreach (var leftNode in left.CandidateState.FluidNodes)
        {
            var rightNode = right.CandidateState.GetFluidNode(leftNode.Id);
            Assert.Equal(leftNode.Inventory, rightNode.Inventory);
            Assert.Equal(leftNode.Thermodynamics, rightNode.Thermodynamics);
        }

        foreach (var leftBody in left.CandidateState.ThermalBodies)
        {
            Assert.Equal(leftBody, right.CandidateState.GetThermalBody(leftBody.Id));
        }
    }

    [Fact]
    public void LongRun_ZeroNetSourceReferencePoint_IsDeterministicAndDriftFree()
    {
        var left = CreateFixture(Power.Zero, Power.Zero);
        var right = CreateFixture(Power.Zero, Power.Zero);
        var leftRunner = new PrimaryCircuitLongRunRunner(left.Definition, new SimplifiedWaterSteamThermodynamicModel());
        var rightRunner = new PrimaryCircuitLongRunRunner(right.Definition, new SimplifiedWaterSteamThermodynamicModel());
        var leftOperatingPoint = new PrimaryCircuitReferenceOperatingPoint(
            "equilibrium",
            left.Definition,
            left.State,
            left.Inputs,
            TimeSpan.FromMilliseconds(20d));
        var rightOperatingPoint = new PrimaryCircuitReferenceOperatingPoint(
            "equilibrium",
            right.Definition,
            right.State,
            right.Inputs,
            TimeSpan.FromMilliseconds(20d));

        var leftResult = leftRunner.Run(leftOperatingPoint, 1_000);
        var rightResult = rightRunner.Run(rightOperatingPoint, 1_000);

        Assert.Equal(TimeSpan.FromSeconds(20d), leftResult.SimulatedDuration);
        Assert.Equal(0d, leftResult.MassInventoryDriftKilograms, 12);
        Assert.Equal(0d, leftResult.StoredEnergyDriftJoules, 6);
        Assert.Equal(0d, leftResult.MaximumAbsoluteBalanceMassRateResidualKilogramsPerSecond, 12);
        Assert.Equal(0d, leftResult.MaximumAbsoluteMassClosureResidualKilograms, 12);
        Assert.Equal(0d, leftResult.MaximumAbsoluteBalancePowerResidualWatts, 6);
        Assert.Equal(0d, leftResult.MaximumAbsoluteEnergyClosureResidualJoules, 6);
        Assert.Equal(leftResult.FinalTotalMass, rightResult.FinalTotalMass);
        Assert.Equal(leftResult.FinalTotalStoredEnergy, rightResult.FinalTotalStoredEnergy);

        foreach (var leftNode in leftResult.FinalStep.CandidateState.FluidNodes)
        {
            var rightNode = rightResult.FinalStep.CandidateState.GetFluidNode(leftNode.Id);
            Assert.Equal(leftNode.Mass, rightNode.Mass);
            Assert.Equal(leftNode.InternalEnergy, rightNode.InternalEnergy);
            Assert.Equal(leftNode.Pressure, rightNode.Pressure);
            Assert.Equal(leftNode.Temperature, rightNode.Temperature);
        }
    }

    [Fact]
    public void Inputs_RejectDefinitionsFromAnotherIntegratedCircuit()
    {
        var left = CreateFixture(Power.Zero, Power.Zero);
        var right = CreateFixture(Power.Zero, Power.Zero);

        Assert.Throws<ArgumentException>(() => new IntegratedPrimaryCircuitInputs(
            left.Definition,
            right.Inputs.CoreState,
            Power.Zero,
            Power.Zero,
            left.Inputs.BoundaryInputs));
    }

    private static Fixture CreateFixture(Power fissionPower, Power decayHeatPower)
    {
        var thermodynamicModel = new SimplifiedWaterSteamThermodynamicModel();
        var referenceTemperature = Temperature.FromDegreesCelsius(250d);
        var saturation = thermodynamicModel.GetSaturationProperties(referenceTemperature);
        const double fluidMassKilograms = 1_000d;
        var referenceDensity = saturation.SaturatedLiquidDensity.KilogramsPerCubicMetre * 1.002d;
        var nodeVolume = Volume.FromCubicMetres(fluidMassKilograms / referenceDensity);
        var nodeEnergy = Energy.FromJoules(saturation.SaturatedLiquidInternalEnergy.JoulesPerKilogram * fluidMassKilograms);

        FluidNodeDefinition Node(string id) => new(id, nodeVolume);
        PipeDefinition Pipe(string id, string from, string to) => new(
            id,
            from,
            to,
            QuadraticHydraulicResistance.FromPascalSecondsSquaredPerKilogramSquared(100_000d));

        var plant = new PlantDefinition(
            "plant",
            new[] { Node("suction"), Node("pressure"), Node("outlet"), Node("drum"), Node("steam") },
            new[]
            {
                Pipe("channel", "pressure", "outlet"),
                Pipe("return", "outlet", "drum"),
            },
            Array.Empty<ValveDefinition>(),
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

        FluidNodeState Fluid(string id)
        {
            var definition = plant.GetFluidNode(id);
            var inventory = new FluidNodeInventory(Mass.FromKilograms(fluidMassKilograms), nodeEnergy);
            var thermodynamics = thermodynamicModel.Resolve(
                definition,
                inventory,
                new FluidThermodynamicState(Pressure.StandardAtmosphere, Temperature.FromDegreesCelsius(20d)));
            return new FluidNodeState(definition, inventory, thermodynamics);
        }

        var state = new PlantState(
            plant,
            new[] { Fluid("suction"), Fluid("pressure"), Fluid("outlet"), Fluid("drum"), Fluid("steam") },
            Array.Empty<ValveState>(),
            new[] { new PumpState("pump", PumpSpeed.Stopped, isRunning: false) },
            new[]
            {
                ThermalBodyState.FromTemperature(plant.GetThermalBody("fuel"), Temperature.FromDegreesCelsius(500d)),
                ThermalBodyState.FromTemperature(plant.GetThermalBody("structure"), Temperature.FromDegreesCelsius(350d)),
            },
            Array.Empty<HeatSourceState>());

        var core = AggregatedCoreDefinition.CreateSingleZone(
            "core",
            plant,
            "zone",
            "fuel",
            "structure",
            "outlet");
        var groups = new FuelChannelGroupSetDefinition(
            "groups",
            core,
            new[]
            {
                new FuelChannelGroupDefinition(
                    "group",
                    "zone",
                    100,
                    CoreZonePowerFraction.Full,
                    "channel",
                    "pressure",
                    "outlet",
                    "fuel",
                    "structure",
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
                    "loop",
                    "suction",
                    "pressure",
                    "drum",
                    new[] { "pump" },
                    new[] { new MainCirculationBranchDefinition("group", "return") }),
            });
        var drums = new SteamDrumSystemDefinition(
            "drums",
            circulation,
            new[] { new SteamDrumDefinition("drum-a", "loop", "drum", "steam") });
        var boundaries = new PrimaryCircuitBoundarySystemDefinition(
            "boundaries",
            drums,
            new[] { new FeedwaterBoundaryDefinition("feed", "drum-a", "drum") },
            new[] { new SteamExportBoundaryDefinition("export", "drum-a", "steam") });
        var definition = new IntegratedPrimaryCircuitDefinition("primary", boundaries);
        var boundaryInputs = new PrimaryCircuitBoundaryInputs(
            boundaries,
            new[] { new FeedwaterBoundaryInput("feed", MassFlowRate.Zero, SpecificEnergy.Zero) },
            new[] { new SteamExportBoundaryInput("export", MassFlowRate.Zero) });
        var inputs = new IntegratedPrimaryCircuitInputs(
            definition,
            AggregatedCoreState.CreateNominal(core),
            fissionPower,
            decayHeatPower,
            boundaryInputs);

        return new Fixture(definition, state, inputs);
    }

    private sealed record Fixture(
        IntegratedPrimaryCircuitDefinition Definition,
        PlantState State,
        IntegratedPrimaryCircuitInputs Inputs);

}
