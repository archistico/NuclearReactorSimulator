using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.Reactor.Core;
using NuclearReactorSimulator.Domain.Physics.Reactor.Core.Channels;
using NuclearReactorSimulator.Domain.Physics.Reactor.ThermalPower;
using NuclearReactorSimulator.Domain.Physics.Reactor.PrimaryCircuit.Circulation;
using NuclearReactorSimulator.Domain.Physics.Thermal;
using NuclearReactorSimulator.Domain.Plant;
using NuclearReactorSimulator.Simulation.Physics.Fluids;
using NuclearReactorSimulator.Simulation.Physics.Reactor.PrimaryCircuit.Circulation;
using NuclearReactorSimulator.Simulation.Plant;
using Xunit;

namespace NuclearReactorSimulator.Simulation.Tests.Physics.Reactor.PrimaryCircuit.Circulation;

public sealed class MainCirculationSystemSolverTests
{
    [Fact]
    public void Solve_UsesCanonicalPumpAndBranchPhysicsFromCommittedState()
    {
        var fixture = CreateFixture();
        var snapshot = fixture.Solver.Solve(fixture.PlantState);
        var loop = snapshot.GetLoop("loop");
        var expectedPump = new PumpFlowSolver().Solve(
            fixture.Plant.GetPump("mcp-1"),
            fixture.PlantState.GetPump("mcp-1"),
            fixture.PlantState.GetFluidNode("suction"),
            fixture.PlantState.GetFluidNode("pressure"));
        var expectedChannel = new PipeFlowSolver().Solve(
            fixture.Plant.GetPipe("channel-a"),
            fixture.PlantState.GetFluidNode("pressure"),
            fixture.PlantState.GetFluidNode("outlet-a"));

        Assert.Equal(expectedPump.MassFlowRate, loop.GetPump("mcp-1").MassFlowRate);
        Assert.Equal(expectedPump.ActivePressureBoost, loop.GetPump("mcp-1").ActivePressureBoost);
        Assert.Equal(expectedChannel.MassFlowRate, loop.GetBranch("group-a").ChannelMassFlowRate);
        Assert.Equal(fixture.PlantState.GetFluidNode("pressure").Pressure - fixture.PlantState.GetFluidNode("suction").Pressure, loop.HeaderPressureRise);
    }

    [Fact]
    public void Solve_HigherResistanceChannelCarriesLowerFlowAtSameHeaderConditions()
    {
        var fixture = CreateFixture();
        var loop = fixture.Solver.Solve(fixture.PlantState).GetLoop("loop");

        Assert.True(loop.GetBranch("group-a").ChannelMassFlowRate > loop.GetBranch("group-b").ChannelMassFlowRate);
        Assert.True(loop.GetBranch("group-a").PerChannelMassFlowRate > loop.GetBranch("group-b").PerChannelMassFlowRate);
    }

    [Fact]
    public void Solve_ReportsCommittedStateContinuityResidualsWithoutCorrectingThem()
    {
        var fixture = CreateFixture();
        var loop = fixture.Solver.Solve(fixture.PlantState).GetLoop("loop");

        Assert.Equal(loop.TotalPumpMassFlowRate - loop.TotalChannelMassFlowRate, loop.PumpToChannelContinuityResidual);
        Assert.Equal(loop.TotalChannelMassFlowRate - loop.TotalReturnMassFlowRate, loop.ChannelToReturnContinuityResidual);
        Assert.Contains(loop.Branches, static branch => branch.BranchContinuityResidual != MassFlowRate.Zero);
    }

    [Fact]
    public void Solve_ExposesOutletPhaseQualityAndVoidWithoutDuplicatingFluidState()
    {
        var fixture = CreateFixture();
        var branch = fixture.Solver.Solve(fixture.PlantState).GetLoop("loop").GetBranch("group-a");

        Assert.Equal(FluidPhase.SubcooledLiquid, branch.OutletPhase);
        Assert.Null(branch.OutletVaporQuality);
        Assert.NotNull(branch.OutletVoidFraction);
        Assert.Equal(0d, branch.OutletVoidFraction.Value.Fraction, 12);
    }

    [Fact]
    public void Solve_IsIndependentFromCallerOrderingUsedToBuildSemanticDefinitions()
    {
        var left = CreateFixture(reverseSemanticOrder: false);
        var right = CreateFixture(reverseSemanticOrder: true);

        var leftSnapshot = left.Solver.Solve(left.PlantState);
        var rightSnapshot = right.Solver.Solve(right.PlantState);

        Assert.Equal(leftSnapshot.Loops.Select(static loop => loop.LoopId), rightSnapshot.Loops.Select(static loop => loop.LoopId));
        Assert.Equal(leftSnapshot.GetLoop("loop").Pumps.ToArray(), rightSnapshot.GetLoop("loop").Pumps.ToArray());
        Assert.Equal(leftSnapshot.GetLoop("loop").Branches.ToArray(), rightSnapshot.GetLoop("loop").Branches.ToArray());
    }

    [Fact]
    public void NetworkOrchestrator_RemainsTheOnlyIntegrationBoundaryForTheClosedCirculationTopology()
    {
        var fixture = CreateFixture();
        var suctionMassBeforeDiagnostics = fixture.PlantState.GetFluidNode("suction").Mass;
        _ = fixture.Solver.Solve(fixture.PlantState);
        Assert.Equal(suctionMassBeforeDiagnostics, fixture.PlantState.GetFluidNode("suction").Mass);

        var result = new PlantNetworkOrchestrator(new PreservingThermodynamicModel())
            .Step(fixture.PlantState, TimeSpan.FromMilliseconds(20));

        Assert.InRange(Math.Abs(result.Audit.MassClosureResidualKilograms), 0d, 1e-6d);
        Assert.True(result.Audit.PumpHydraulicPowerExchange > Power.Zero);
        Assert.NotEqual(suctionMassBeforeDiagnostics, result.CandidateState.GetFluidNode("suction").Mass);
    }

    private static Fixture CreateFixture(bool reverseSemanticOrder = false)
    {
        var plant = BuildPlant();
        FluidNodeState Fluid(string id, double pressureMpa, double temperatureCelsius)
            => new(
                plant.GetFluidNode(id),
                new FluidNodeInventory(Mass.FromKilograms(8_000), Energy.FromMegajoules(5_000)),
                new FluidThermodynamicState(
                    Pressure.FromMegapascals(pressureMpa),
                    Temperature.FromDegreesCelsius(temperatureCelsius),
                    FluidPhase.SubcooledLiquid,
                    null));

        var plantState = new PlantState(
            plant,
            new[]
            {
                Fluid("suction", 6.4, 270),
                Fluid("pressure", 7.2, 272),
                Fluid("outlet-a", 6.8, 285),
                Fluid("outlet-b", 6.8, 286),
            },
            Array.Empty<ValveState>(),
            new[]
            {
                new PumpState("mcp-1", PumpSpeed.Rated),
                new PumpState("mcp-2", PumpSpeed.FromPercent(80)),
            },
            new[]
            {
                ThermalBodyState.FromTemperature(plant.GetThermalBody("fuel-a"), Temperature.FromDegreesCelsius(700)),
                ThermalBodyState.FromTemperature(plant.GetThermalBody("structure-a"), Temperature.FromDegreesCelsius(500)),
                ThermalBodyState.FromTemperature(plant.GetThermalBody("fuel-b"), Temperature.FromDegreesCelsius(710)),
                ThermalBodyState.FromTemperature(plant.GetThermalBody("structure-b"), Temperature.FromDegreesCelsius(510)),
            },
            Array.Empty<HeatSourceState>());

        var core = new AggregatedCoreDefinition(
            "core",
            plant,
            new[]
            {
                new CoreZoneDefinition("zone-a", new CoreZoneCoordinate(0, 0), CoreZonePowerFraction.FromPercent(50), "fuel-a", "structure-a", "outlet-a"),
                new CoreZoneDefinition("zone-b", new CoreZoneCoordinate(0, 1), CoreZonePowerFraction.FromPercent(50), "fuel-b", "structure-b", "outlet-b"),
            });

        FuelChannelGroupDefinition Group(string id, string zone, string pipe, string outlet, string fuel, string structure)
            => new(
                id,
                zone,
                100,
                CoreZonePowerFraction.FromPercent(100),
                pipe,
                "pressure",
                outlet,
                fuel,
                structure,
                HeatDepositionFraction.FromPercent(70),
                HeatDepositionFraction.FromPercent(10),
                HeatDepositionFraction.FromPercent(20));

        var groupArray = reverseSemanticOrder
            ? new[]
            {
                Group("group-b", "zone-b", "channel-b", "outlet-b", "fuel-b", "structure-b"),
                Group("group-a", "zone-a", "channel-a", "outlet-a", "fuel-a", "structure-a"),
            }
            : new[]
            {
                Group("group-a", "zone-a", "channel-a", "outlet-a", "fuel-a", "structure-a"),
                Group("group-b", "zone-b", "channel-b", "outlet-b", "fuel-b", "structure-b"),
            };
        var groups = new FuelChannelGroupSetDefinition("channels", core, groupArray);

        var pumpIds = reverseSemanticOrder ? new[] { "mcp-2", "mcp-1" } : new[] { "mcp-1", "mcp-2" };
        var branches = reverseSemanticOrder
            ? new[]
            {
                new MainCirculationBranchDefinition("group-b", "return-b"),
                new MainCirculationBranchDefinition("group-a", "return-a"),
            }
            : new[]
            {
                new MainCirculationBranchDefinition("group-a", "return-a"),
                new MainCirculationBranchDefinition("group-b", "return-b"),
            };
        var system = new MainCirculationSystemDefinition(
            "mcs",
            groups,
            new[] { new MainCirculationLoopDefinition("loop", "suction", "pressure", pumpIds, branches) });

        return new Fixture(plant, plantState, new MainCirculationSystemSolver(system));
    }

    private static PlantDefinition BuildPlant()
    {
        FluidNodeDefinition Node(string id) => new(id, Volume.FromCubicMetres(10));
        PipeDefinition Pipe(string id, string from, string to, double resistance) => new(
            id,
            from,
            to,
            QuadraticHydraulicResistance.FromPascalSecondsSquaredPerKilogramSquared(resistance));
        PumpDefinition Pump(string id, string pathId) => new(
            id,
            Pipe(pathId, "suction", "pressure", 80_000),
            PressureDifference.FromMegapascals(1.8),
            QuadraticHydraulicResistance.FromPascalSecondsSquaredPerKilogramSquared(40_000),
            PumpEfficiency.FromPercent(82));

        return new PlantDefinition(
            "plant",
            new[] { Node("suction"), Node("pressure"), Node("outlet-a"), Node("outlet-b") },
            new[]
            {
                Pipe("channel-a", "pressure", "outlet-a", 100_000),
                Pipe("channel-b", "pressure", "outlet-b", 400_000),
                Pipe("return-a", "outlet-a", "suction", 150_000),
                Pipe("return-b", "outlet-b", "suction", 150_000),
            },
            Array.Empty<ValveDefinition>(),
            new[] { Pump("mcp-1", "mcp-1-path"), Pump("mcp-2", "mcp-2-path") },
            new[]
            {
                new ThermalBodyDefinition("fuel-a", HeatCapacity.FromJoulesPerKelvin(10_000_000)),
                new ThermalBodyDefinition("structure-a", HeatCapacity.FromJoulesPerKelvin(20_000_000)),
                new ThermalBodyDefinition("fuel-b", HeatCapacity.FromJoulesPerKelvin(10_000_000)),
                new ThermalBodyDefinition("structure-b", HeatCapacity.FromJoulesPerKelvin(20_000_000)),
            },
            Array.Empty<HeatTransferDefinition>(),
            Array.Empty<HeatSourceDefinition>());
    }

    private sealed record Fixture(
        PlantDefinition Plant,
        PlantState PlantState,
        MainCirculationSystemSolver Solver);

    private sealed class PreservingThermodynamicModel : IFluidThermodynamicModel
    {
        public FluidThermodynamicState Resolve(
            FluidNodeDefinition definition,
            FluidNodeInventory inventory,
            FluidThermodynamicState previousState)
            => previousState;
    }
}
