using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.Thermal;
using NuclearReactorSimulator.Domain.Plant;
using NuclearReactorSimulator.Simulation.Physics.Fluids;
using NuclearReactorSimulator.Simulation.Plant;
using NuclearReactorSimulator.Simulation.Runtime;
using Xunit;

namespace NuclearReactorSimulator.Simulation.Tests.Plant;

public sealed class PlantNetworkOrchestratorTests
{
    [Fact]
    public void ParallelComponents_ReadTheSameCommittedStateBeforeAnyInventoryIsIntegrated()
    {
        var definition = CreateDefinition(
            pipes:
            [
                Pipe("pipe-b"),
                Pipe("pipe-a"),
            ],
            valves: [],
            pumps: [],
            thermalBodies: [],
            heatTransfers: [],
            heatSources: []);
        var state = CreateState(definition);
        var expectedSingleFlow = new PipeFlowSolver().Solve(
            definition.GetPipe("pipe-a"),
            state.GetFluidNode("node-a"),
            state.GetFluidNode("node-b"));
        var thermodynamics = new CountingThermodynamicModel();
        var orchestrator = new PlantNetworkOrchestrator(thermodynamics);

        var result = orchestrator.Step(state, TimeSpan.FromSeconds(1d));

        var expectedFromBalance = expectedSingleFlow.FromNodeBalance + expectedSingleFlow.FromNodeBalance;
        var expectedToBalance = expectedSingleFlow.ToNodeBalance + expectedSingleFlow.ToNodeBalance;
        Assert.Equal(expectedFromBalance, result.FluidNodeBalances["node-a"]);
        Assert.Equal(expectedToBalance, result.FluidNodeBalances["node-b"]);
        Assert.Equal(2, thermodynamics.ResolveCount);
    }

    [Fact]
    public void Step_AccumulatesAllBalancesThenIntegratesEveryConservedInventoryExactlyOnce()
    {
        var definition = CreateDefinition();
        var state = CreateState(definition);
        var thermodynamics = new CountingThermodynamicModel();
        var orchestrator = new PlantNetworkOrchestrator(thermodynamics);

        var result = orchestrator.Step(state, TimeSpan.FromSeconds(1d));

        Assert.Equal(definition.FluidNodes.Count, thermodynamics.ResolveCount);
        Assert.Equal(definition.FluidNodes.Count, result.CandidateState.FluidNodes.Count);
        Assert.Equal(definition.ThermalBodies.Count, result.CandidateState.ThermalBodies.Count);
        Assert.NotEqual(state.GetFluidNode("node-a").Mass, result.CandidateState.GetFluidNode("node-a").Mass);
        Assert.True(result.CandidateState.GetThermalBody("wall-a").StoredThermalEnergy > state.GetThermalBody("wall-a").StoredThermalEnergy);
        Assert.Same(state.GetValve("valve-a"), result.CandidateState.GetValve("valve-a"));
        Assert.Same(state.GetPump("pump-a"), result.CandidateState.GetPump("pump-a"));
        Assert.Same(state.GetHeatSource("heater-a"), result.CandidateState.GetHeatSource("heater-a"));
    }

    [Fact]
    public void Audit_ClosesMassAndEnergyAgainstExplicitExternalBoundaryAccounting()
    {
        var definition = CreateDefinition();
        var state = CreateState(definition);
        var result = new PlantNetworkOrchestrator(new CountingThermodynamicModel())
            .Step(state, TimeSpan.FromSeconds(2d));

        Assert.Equal(0d, result.Audit.NetAccumulatedMassRate.KilogramsPerSecond, 10);
        Assert.Equal(MassFlowRate.Zero, result.Audit.ExpectedExternalMassFlowRate);
        Assert.Equal(MassFlowRate.Zero, result.Audit.SupplementalExternalMassFlowRate);
        Assert.InRange(Math.Abs(result.Audit.BalanceMassRateResidualKilogramsPerSecond), 0d, 1e-10d);
        Assert.InRange(Math.Abs(result.Audit.MassClosureResidualKilograms), 0d, 1e-6d);
        Assert.Equal(
            result.Audit.PumpHydraulicPowerExchange.Watts + result.Audit.HeatSourcePower.Watts,
            result.Audit.ExpectedExternalPower.Watts,
            9);
        Assert.InRange(Math.Abs(result.Audit.BalancePowerResidualWatts), 0d, 1e-6d);
        Assert.InRange(Math.Abs(result.Audit.EnergyClosureResidualJoules), 0d, 1d);
        Assert.True(result.Audit.IsBalanceMassRateClosedWithin(1e-10d));
        Assert.True(result.Audit.IsMassClosedWithin(1e-6d));
        Assert.True(result.Audit.IsBalancePowerClosedWithin(1e-6d));
        Assert.True(result.Audit.IsEnergyClosedWithin(1d));
    }

    [Fact]
    public void SourceTerms_Combine_IsIndependentFromCallerTermOrder()
    {
        var first = new PlantNetworkSourceTerms(
            new Dictionary<string, FluidNodeBalance>(StringComparer.Ordinal)
            {
                ["node-a"] = new FluidNodeBalance(MassFlowRate.FromKilogramsPerSecond(2d), Power.FromMegawatts(5d)),
            },
            new Dictionary<string, NuclearReactorSimulator.Simulation.Physics.Thermal.ThermalEnergyBalance>(StringComparer.Ordinal),
            MassFlowRate.FromKilogramsPerSecond(2d),
            Power.FromMegawatts(5d));
        var second = new PlantNetworkSourceTerms(
            new Dictionary<string, FluidNodeBalance>(StringComparer.Ordinal)
            {
                ["node-a"] = new FluidNodeBalance(MassFlowRate.FromKilogramsPerSecond(-1d), Power.FromMegawatts(-3d)),
            },
            new Dictionary<string, NuclearReactorSimulator.Simulation.Physics.Thermal.ThermalEnergyBalance>(StringComparer.Ordinal),
            MassFlowRate.FromKilogramsPerSecond(-1d),
            Power.FromMegawatts(-3d));

        var left = PlantNetworkSourceTerms.Combine(first, second);
        var right = PlantNetworkSourceTerms.Combine(second, first);

        Assert.Equal(left.FluidNodeBalances["node-a"], right.FluidNodeBalances["node-a"]);
        Assert.Equal(left.ExternalMassFlowRate, right.ExternalMassFlowRate);
        Assert.Equal(left.ExternalPower, right.ExternalPower);
        Assert.Equal(1d, left.ExternalMassFlowRate.KilogramsPerSecond, 12);
        Assert.Equal(2d, left.ExternalPower.Megawatts, 12);
    }

    [Fact]
    public void ReorderingEveryCallerRegistry_DoesNotChangeThePhysicalStep()
    {
        var leftDefinition = CreateDefinition(reverseCallerOrder: false);
        var rightDefinition = CreateDefinition(reverseCallerOrder: true);
        var left = new PlantNetworkOrchestrator(new CountingThermodynamicModel())
            .Step(CreateState(leftDefinition), TimeSpan.FromMilliseconds(20d));
        var right = new PlantNetworkOrchestrator(new CountingThermodynamicModel())
            .Step(CreateState(rightDefinition), TimeSpan.FromMilliseconds(20d));

        Assert.Equal(
            left.CandidateState.FluidNodes.Select(static item => (item.Id, item.Mass.Kilograms, item.InternalEnergy.Joules)),
            right.CandidateState.FluidNodes.Select(static item => (item.Id, item.Mass.Kilograms, item.InternalEnergy.Joules)));
        Assert.Equal(
            left.CandidateState.ThermalBodies.Select(static item => (item.Id, item.StoredThermalEnergy.Joules)),
            right.CandidateState.ThermalBodies.Select(static item => (item.Id, item.StoredThermalEnergy.Joules)));
        Assert.Equal(left.Audit.NetAccumulatedMassRate, right.Audit.NetAccumulatedMassRate);
        Assert.Equal(left.Audit.NetAccumulatedEnergyRate, right.Audit.NetAccumulatedEnergyRate);
        Assert.Equal(left.Audit.ExpectedExternalPower, right.Audit.ExpectedExternalPower);
    }

    [Fact]
    public void Orchestrator_ComposesWithFixedStepRuntimeAndPulseSegmentation()
    {
        var left = CreateRuntime();
        var right = CreateRuntime();
        left.Resume();
        right.Resume();

        left.Advance(TimeSpan.FromSeconds(2d));
        foreach (var pulse in new[] { 17, 83, 400, 7, 293, 1_200 })
        {
            right.Advance(TimeSpan.FromMilliseconds(pulse));
        }

        var leftSnapshot = left.GetSnapshot();
        var rightSnapshot = right.GetSnapshot();

        Assert.Equal(leftSnapshot, rightSnapshot);
        Assert.Equal(100L, leftSnapshot.Runtime.StepIndex);
        Assert.Equal(200_000d, leftSnapshot.State.TotalMassKilograms, 7);
        Assert.True(leftSnapshot.State.TotalStoredEnergyJoules > 100_000_000_000d);
        Assert.InRange(Math.Abs(leftSnapshot.State.LastMassResidualKilograms), 0d, 1e-6d);
        Assert.InRange(Math.Abs(leftSnapshot.State.LastEnergyResidualJoules), 0d, 1d);
    }

    private static SimulationRuntime<PlantState, NoCommand, NetworkSnapshot> CreateRuntime()
    {
        var definition = CreateDefinition();
        return new SimulationRuntime<PlantState, NoCommand, NetworkSnapshot>(
            TimeSpan.FromMilliseconds(20d),
            CreateState(definition),
            new NetworkKernel(new PlantNetworkOrchestrator(new CountingThermodynamicModel())));
    }

    private static PlantDefinition CreateDefinition(
        bool reverseCallerOrder = false,
        PipeDefinition[]? pipes = null,
        ValveDefinition[]? valves = null,
        PumpDefinition[]? pumps = null,
        ThermalBodyDefinition[]? thermalBodies = null,
        HeatTransferDefinition[]? heatTransfers = null,
        HeatSourceDefinition[]? heatSources = null)
    {
        var nodes = new[] { Node("node-a"), Node("node-b") };
        var actualPipes = pipes ?? [Pipe("pipe-b"), Pipe("pipe-a")];
        var actualValves = valves ??
        [
            new ValveDefinition(
                "valve-b",
                Pipe("valve-b-path"),
                ValveCharacteristic.QuickOpening,
                ValveFailSafeAction.FailOpen),
            new ValveDefinition(
                "valve-a",
                Pipe("valve-a-path"),
                ValveCharacteristic.Linear,
                ValveFailSafeAction.FailClosed),
        ];
        var actualPumps = pumps ??
        [
            new PumpDefinition(
                "pump-b",
                Pipe("pump-b-path"),
                PressureDifference.FromKilopascals(80d),
                Resistance(),
                PumpEfficiency.FromPercent(82d)),
            new PumpDefinition(
                "pump-a",
                Pipe("pump-a-path"),
                PressureDifference.FromKilopascals(100d),
                Resistance(),
                PumpEfficiency.FromPercent(80d)),
        ];
        var actualThermalBodies = thermalBodies ??
        [
            new ThermalBodyDefinition("wall-a", HeatCapacity.FromJoulesPerKelvin(1_000_000_000d)),
        ];
        var actualHeatTransfers = heatTransfers ??
        [
            new HeatTransferDefinition(
                "heat-link-b",
                "node-a",
                "wall-a",
                ThermalConductance.FromWattsPerKelvin(600d)),
            new HeatTransferDefinition(
                "heat-link-a",
                "wall-a",
                "node-b",
                ThermalConductance.FromWattsPerKelvin(1_000d)),
        ];
        var actualHeatSources = heatSources ??
        [
            new HeatSourceDefinition("heater-b", "node-a", Power.FromKilowatts(250d)),
            new HeatSourceDefinition("heater-a", "wall-a", Power.FromMegawatts(1d)),
        ];

        if (reverseCallerOrder)
        {
            Array.Reverse(nodes);
            Array.Reverse(actualPipes);
            Array.Reverse(actualValves);
            Array.Reverse(actualPumps);
            Array.Reverse(actualThermalBodies);
            Array.Reverse(actualHeatTransfers);
            Array.Reverse(actualHeatSources);
        }

        return new PlantDefinition(
            "network-test-plant",
            nodes,
            actualPipes,
            actualValves,
            actualPumps,
            actualThermalBodies,
            actualHeatTransfers,
            actualHeatSources);
    }

    private static PlantState CreateState(PlantDefinition definition)
        => new(
            definition,
            definition.FluidNodes.Select(definitionItem => CreateFluidState(definitionItem)),
            definition.Valves.Select(static item => new ValveState(item.Id, ValvePosition.FullyOpen)),
            definition.Pumps.Select(static item => new PumpState(item.Id, PumpSpeed.FromPercent(75d))),
            definition.ThermalBodies.Select(item => ThermalBodyState.FromTemperature(item, Temperature.FromDegreesCelsius(300d))),
            definition.HeatSources.Select(static item => new HeatSourceState(item.Id, true)));

    private static FluidNodeDefinition Node(string id) => new(id, Volume.FromCubicMetres(100d));

    private static PipeDefinition Pipe(string id)
        => new(id, "node-a", "node-b", Resistance());

    private static QuadraticHydraulicResistance Resistance()
        => QuadraticHydraulicResistance.FromPascalSecondsSquaredPerKilogramSquared(1_000_000_000d);

    private static FluidNodeState CreateFluidState(FluidNodeDefinition definition)
    {
        var isFromNode = string.Equals(definition.Id, "node-a", StringComparison.Ordinal);
        return new FluidNodeState(
            definition,
            new FluidNodeInventory(
                Mass.FromKilograms(100_000d),
                Energy.FromMegajoules(50_000d)),
            new FluidThermodynamicState(
                Pressure.FromMegapascals(isFromNode ? 5.2d : 5d),
                Temperature.FromDegreesCelsius(250d)));
    }

    private sealed record NoCommand();

    private sealed record NetworkSnapshot(
        double TotalMassKilograms,
        double TotalStoredEnergyJoules,
        double LastMassResidualKilograms,
        double LastEnergyResidualJoules);

    private sealed class NetworkKernel : ISimulationKernel<PlantState, NoCommand, NetworkSnapshot>
    {
        private readonly PlantNetworkOrchestrator _orchestrator;
        private PlantNetworkAudit? _lastAudit;

        public NetworkKernel(PlantNetworkOrchestrator orchestrator)
        {
            _orchestrator = orchestrator;
        }

        public PlantState Step(
            PlantState state,
            IReadOnlyList<QueuedSimulationCommand<NoCommand>> commands,
            SimulationStepContext context)
        {
            _ = commands;
            var result = _orchestrator.Step(state, context.DeltaTime);
            _lastAudit = result.Audit;
            return result.CandidateState;
        }

        public NetworkSnapshot CreateSnapshot(PlantState state)
        {
            return new NetworkSnapshot(
                state.FluidNodes.Sum(static item => item.Mass.Kilograms),
                state.FluidNodes.Sum(static item => item.InternalEnergy.Joules)
                    + state.ThermalBodies.Sum(static item => item.StoredThermalEnergy.Joules),
                _lastAudit?.MassClosureResidualKilograms ?? 0d,
                _lastAudit?.EnergyClosureResidualJoules ?? 0d);
        }
    }

    private sealed class CountingThermodynamicModel : IFluidThermodynamicModel
    {
        public int ResolveCount { get; private set; }

        public FluidThermodynamicState Resolve(
            FluidNodeDefinition definition,
            FluidNodeInventory inventory,
            FluidThermodynamicState previousState)
        {
            _ = definition;
            _ = inventory;
            ResolveCount++;
            return previousState;
        }
    }
}
