using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.Reactor.Core;
using NuclearReactorSimulator.Domain.Physics.Reactor.Core.Channels;
using NuclearReactorSimulator.Domain.Physics.Reactor.ThermalPower;
using NuclearReactorSimulator.Domain.Physics.Thermal;
using NuclearReactorSimulator.Domain.Plant;
using NuclearReactorSimulator.Simulation.Physics.Fluids;
using NuclearReactorSimulator.Simulation.Physics.Reactor.Core;
using NuclearReactorSimulator.Simulation.Physics.Reactor.Core.Channels;
using NuclearReactorSimulator.Simulation.Plant;
using Xunit;

namespace NuclearReactorSimulator.Simulation.Tests.Physics.Reactor.Core.Channels;

public sealed class FuelChannelGroupSolverTests
{
    [Fact]
    public void Solve_PartitionsZonePowerAcrossGroupsAndClosesExactly()
    {
        var fixture = CreateFixture(reverseGroupOrder: true);
        var result = fixture.ChannelSolver.Solve(fixture.CoreSnapshot, fixture.PlantState);

        Assert.Equal(400d, result.Snapshot.GetGroup("group-a").FissionThermalPower.Megawatts, 9);
        Assert.Equal(600d, result.Snapshot.GetGroup("group-b").FissionThermalPower.Megawatts, 9);
        Assert.Equal(100d, result.Snapshot.GetGroup("group-a").PerChannelFissionThermalPower.Megawatts, 9);
        Assert.Equal(100d, result.Snapshot.GetGroup("group-b").PerChannelFissionThermalPower.Megawatts, 9);
        Assert.Equal(
            result.Snapshot.TotalFissionThermalPower.Watts,
            result.Snapshot.Groups.Sum(static group => group.FissionThermalPower.Watts));
    }

    [Fact]
    public void Solve_CanRouteGlobalDecayHeatThroughTheSameChannelGroupSourceTermBoundary()
    {
        var fixture = CreateFixture();
        var result = fixture.ChannelSolver.Solve(fixture.CoreSnapshot, Power.FromMegawatts(50), fixture.PlantState);

        Assert.Equal(50d, result.Snapshot.TotalDecayHeatPower.Megawatts, 9);
        Assert.Equal(1_050d, result.Snapshot.TotalNuclearHeatPower.Megawatts, 9);
        Assert.Equal(20d, result.Snapshot.GetGroup("group-a").DecayHeatPower.Megawatts, 9);
        Assert.Equal(30d, result.Snapshot.GetGroup("group-b").DecayHeatPower.Megawatts, 9);
        Assert.Equal(1_050d, result.SourceTerms.ExternalPower.Megawatts, 9);
        Assert.Equal(1_050d,
            result.SourceTerms.ThermalBodyBalances.Values.Sum(static balance => balance.NetHeatRate.Megawatts)
            + result.SourceTerms.FluidNodeBalances.Values.Sum(static balance => balance.NetEnergyRate.Megawatts),
            9);
    }

    [Fact]
    public void Solve_ProducesConservativeNamedHeatSourceTerms()
    {
        var fixture = CreateFixture();
        var result = fixture.ChannelSolver.Solve(fixture.CoreSnapshot, fixture.PlantState);

        Assert.Equal(700d, result.SourceTerms.ThermalBodyBalances["fuel"].NetHeatRate.Megawatts, 9);
        Assert.Equal(100d, result.SourceTerms.ThermalBodyBalances["structure"].NetHeatRate.Megawatts, 9);
        Assert.Equal(200d, result.SourceTerms.FluidNodeBalances["outlet"].NetEnergyRate.Megawatts, 9);
        Assert.Equal(0d, result.SourceTerms.FluidNodeBalances["outlet"].NetMassFlowRate.KilogramsPerSecond, 12);
        Assert.Equal(1_000d, result.SourceTerms.ExternalPower.Megawatts, 9);
    }

    [Fact]
    public void Solve_ObservesCanonicalHydraulicPathsFromCommittedState()
    {
        var fixture = CreateFixture();
        var result = fixture.ChannelSolver.Solve(fixture.CoreSnapshot, fixture.PlantState);
        var expected = new PipeFlowSolver().Solve(
            fixture.PlantState.Definition.GetPipe("pipe-a"),
            fixture.PlantState.GetFluidNode("inlet-a"),
            fixture.PlantState.GetFluidNode("outlet"));
        var group = result.Snapshot.GetGroup("group-a");

        Assert.Equal(expected.MassFlowRate, group.MassFlowRate);
        Assert.True(group.MassFlowRate > MassFlowRate.Zero);
        Assert.Equal(expected.MassFlowRate / 4d, group.PerChannelMassFlowRate);
        Assert.Equal(FluidPhase.SubcooledLiquid, group.OutletCoolantPhase);
        Assert.NotNull(group.OutletVoidFraction);
        Assert.Equal(0d, group.OutletVoidFraction.Value.Fraction, 12);
    }

    [Fact]
    public void SourceTerms_ComposeWithPlantNetworkOrchestratorAndAuditExternalFissionPower()
    {
        var fixture = CreateFixture();
        var channelResult = fixture.ChannelSolver.Solve(fixture.CoreSnapshot, fixture.PlantState);
        var network = new PlantNetworkOrchestrator(new PreservingThermodynamicModel())
            .Step(fixture.PlantState, TimeSpan.FromMilliseconds(20), channelResult.SourceTerms);

        Assert.Equal(1_000d, network.Audit.SupplementalExternalPower.Megawatts, 9);
        Assert.Equal(
            network.Audit.PumpHydraulicPowerExchange.Watts
                + network.Audit.HeatSourcePower.Watts
                + network.Audit.SupplementalExternalPower.Watts,
            network.Audit.ExpectedExternalPower.Watts,
            6);
        Assert.True(network.CandidateState.GetThermalBody("fuel").StoredThermalEnergy > fixture.PlantState.GetThermalBody("fuel").StoredThermalEnergy);
        Assert.True(network.CandidateState.GetThermalBody("structure").StoredThermalEnergy > fixture.PlantState.GetThermalBody("structure").StoredThermalEnergy);
        Assert.True(network.CandidateState.GetFluidNode("outlet").InternalEnergy > fixture.PlantState.GetFluidNode("outlet").InternalEnergy);
        Assert.InRange(Math.Abs(network.Audit.MassClosureResidualKilograms), 0d, 1e-6d);
        Assert.InRange(Math.Abs(network.Audit.BalancePowerResidualWatts), 0d, 1e-3d);
        Assert.InRange(Math.Abs(network.Audit.EnergyClosureResidualJoules), 0d, 10d);
    }

    [Fact]
    public void Solve_IsIndependentFromCallerGroupOrder()
    {
        var left = CreateFixture(reverseGroupOrder: false);
        var right = CreateFixture(reverseGroupOrder: true);

        var leftResult = left.ChannelSolver.Solve(left.CoreSnapshot, left.PlantState);
        var rightResult = right.ChannelSolver.Solve(right.CoreSnapshot, right.PlantState);

        Assert.Equal(leftResult.Snapshot.Groups.ToArray(), rightResult.Snapshot.Groups.ToArray());
        Assert.Equal(leftResult.SourceTerms.FluidNodeBalances.ToArray(), rightResult.SourceTerms.FluidNodeBalances.ToArray());
        Assert.Equal(leftResult.SourceTerms.ThermalBodyBalances.ToArray(), rightResult.SourceTerms.ThermalBodyBalances.ToArray());
    }

    private static Fixture CreateFixture(bool reverseGroupOrder = false)
    {
        var plant = new PlantDefinition(
            "plant",
            new[]
            {
                new FluidNodeDefinition("inlet-a", Volume.FromCubicMetres(10)),
                new FluidNodeDefinition("inlet-b", Volume.FromCubicMetres(10)),
                new FluidNodeDefinition("outlet", Volume.FromCubicMetres(10)),
            },
            new[]
            {
                new PipeDefinition("pipe-a", "inlet-a", "outlet", QuadraticHydraulicResistance.FromPascalSecondsSquaredPerKilogramSquared(100_000)),
                new PipeDefinition("pipe-b", "inlet-b", "outlet", QuadraticHydraulicResistance.FromPascalSecondsSquaredPerKilogramSquared(120_000)),
            },
            Array.Empty<ValveDefinition>(),
            Array.Empty<PumpDefinition>(),
            new[]
            {
                new ThermalBodyDefinition("fuel", HeatCapacity.FromJoulesPerKelvin(10_000_000)),
                new ThermalBodyDefinition("structure", HeatCapacity.FromJoulesPerKelvin(20_000_000)),
            },
            Array.Empty<HeatTransferDefinition>(),
            Array.Empty<HeatSourceDefinition>());

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
                Fluid("inlet-a", 7.2, 270),
                Fluid("inlet-b", 7.1, 272),
                Fluid("outlet", 6.8, 285),
            },
            Array.Empty<ValveState>(),
            Array.Empty<PumpState>(),
            new[]
            {
                ThermalBodyState.FromTemperature(plant.GetThermalBody("fuel"), Temperature.FromDegreesCelsius(700)),
                ThermalBodyState.FromTemperature(plant.GetThermalBody("structure"), Temperature.FromDegreesCelsius(500)),
            },
            Array.Empty<HeatSourceState>());

        var core = AggregatedCoreDefinition.CreateSingleZone("core", plant, "zone-a", "fuel", "structure", "outlet");
        var coreSnapshot = new AggregatedCorePowerSolver(core).Solve(
            AggregatedCoreState.CreateNominal(core),
            Power.FromMegawatts(1_000),
            plantState);

        FuelChannelGroupDefinition Group(string id, string pipeId, string inletId, int count, double share)
            => new(
                id,
                "zone-a",
                count,
                CoreZonePowerFraction.FromPercent(share),
                pipeId,
                inletId,
                "outlet",
                "fuel",
                "structure",
                HeatDepositionFraction.FromPercent(70),
                HeatDepositionFraction.FromPercent(10),
                HeatDepositionFraction.FromPercent(20));

        var groups = reverseGroupOrder
            ? new[] { Group("group-b", "pipe-b", "inlet-b", 6, 60), Group("group-a", "pipe-a", "inlet-a", 4, 40) }
            : new[] { Group("group-a", "pipe-a", "inlet-a", 4, 40), Group("group-b", "pipe-b", "inlet-b", 6, 60) };
        var channelDefinition = new FuelChannelGroupSetDefinition("channels", core, groups);

        return new Fixture(
            plantState,
            coreSnapshot,
            new FuelChannelGroupSolver(channelDefinition));
    }

    private sealed record Fixture(
        PlantState PlantState,
        AggregatedCoreSnapshot CoreSnapshot,
        FuelChannelGroupSolver ChannelSolver);

    private sealed class PreservingThermodynamicModel : IFluidThermodynamicModel
    {
        public FluidThermodynamicState Resolve(
            FluidNodeDefinition definition,
            FluidNodeInventory inventory,
            FluidThermodynamicState previousState)
            => previousState;
    }
}
