using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.Reactor.DecayHeat;
using NuclearReactorSimulator.Domain.Physics.Reactor.ThermalPower;
using NuclearReactorSimulator.Simulation.Physics.Reactor.DecayHeat;
using Xunit;

namespace NuclearReactorSimulator.Simulation.Tests.Physics.Reactor.DecayHeat;

public sealed class DecayHeatSolverTests
{
    [Fact]
    public void EmptyInventoryAndZeroFission_ProduceZeroDecayHeat()
    {
        var solver = Solver();
        var state = DecayHeatState.CreateEmpty(solver.Definition);

        var result = solver.Step(state, Power.Zero, TimeSpan.FromSeconds(1d));

        Assert.Equal(Energy.Zero, result.State.TotalStoredDecayEnergy);
        Assert.Equal(Power.Zero, result.AverageDecayHeatPower);
        Assert.Equal(Power.Zero, result.Snapshot.TotalInstantaneousDecayHeatPower);
    }

    [Fact]
    public void EquilibriumInventory_RemainsSteadyAtConstantFissionPower()
    {
        var solver = Solver();
        var fissionPower = Power.FromMegawatts(1_000d);
        var state = DecayHeatState.CreateEquilibrium(solver.Definition, fissionPower);

        var result = solver.Step(state, fissionPower, TimeSpan.FromSeconds(5d));

        Assert.Equal(state.TotalStoredDecayEnergy.Joules, result.State.TotalStoredDecayEnergy.Joules, 3);
        Assert.Equal(60d, result.AverageDecayHeatPower.Megawatts, 9);
        Assert.Equal(60d, result.Snapshot.TotalInstantaneousDecayHeatPower.Megawatts, 9);
    }

    [Fact]
    public void SingleGroupShutdown_HalvesPowerAfterOneHalfLife()
    {
        var definition = new DecayHeatDefinition(
            "half-life",
            [new DecayHeatGroupDefinition(
                "g",
                DecayHeatGenerationFraction.FromFraction(0.06d),
                DecayConstant.FromHalfLife(TimeSpan.FromSeconds(10d)))],
            [Destination("fuel", 1d)]);
        var solver = new DecayHeatSolver(definition);
        var state = DecayHeatState.CreateEquilibrium(definition, Power.FromMegawatts(1_000d));

        var result = solver.Step(state, Power.Zero, TimeSpan.FromSeconds(10d));

        Assert.Equal(30d, result.Snapshot.TotalInstantaneousDecayHeatPower.Megawatts, 9);
        Assert.Equal(state.TotalStoredDecayEnergy.Joules * 0.5d, result.State.TotalStoredDecayEnergy.Joules, 3);
    }

    [Fact]
    public void BuildUpFromEmptyInventory_IncreasesStoredEnergyAndDecayPower()
    {
        var solver = Solver();
        var state = DecayHeatState.CreateEmpty(solver.Definition);

        var result = solver.Step(state, Power.FromMegawatts(1_000d), TimeSpan.FromSeconds(1d));

        Assert.True(result.State.TotalStoredDecayEnergy > Energy.Zero);
        Assert.True(result.Snapshot.TotalInstantaneousDecayHeatPower > Power.Zero);
        Assert.True(result.Snapshot.TotalInstantaneousDecayHeatPower < Power.FromMegawatts(60d));
    }

    [Fact]
    public void Step_ClosesLatentEnergyBalanceExactlyWithinFloatingPointArithmetic()
    {
        var solver = Solver();
        var initial = DecayHeatState.CreateEmpty(solver.Definition);
        var result = solver.Step(initial, Power.FromMegawatts(1_000d), TimeSpan.FromSeconds(2d));

        var left = initial.TotalStoredDecayEnergy.Joules + result.ProducedDecayEnergy.Joules;
        var right = result.State.TotalStoredDecayEnergy.Joules + result.EmittedDecayEnergy.Joules;

        Assert.Equal(left, right, 3);
        Assert.Equal(result.EmittedDecayEnergy.Joules, result.AverageDecayHeatPower.Over(TimeSpan.FromSeconds(2d)).Joules, 3);
    }

    [Fact]
    public void AverageHeatDistribution_PreservesEmittedPowerExactly()
    {
        var solver = Solver();
        var initial = DecayHeatState.CreateEquilibrium(solver.Definition, Power.FromMegawatts(1_000d));

        var result = solver.Step(initial, Power.Zero, TimeSpan.FromSeconds(1d));
        var sum = result.AverageHeatDepositions.Sum(static deposition => deposition.ThermalPower.Watts);

        Assert.Equal(result.AverageDecayHeatPower.Watts, sum);
        Assert.Equal(result.AverageDecayHeatPower.Watts * 0.2d, result.GetAverageDeposition("coolant").ThermalPower.Watts, 4);
    }

    [Fact]
    public void SnapshotPower_IsLambdaTimesStoredEnergyForEachGroup()
    {
        var solver = Solver();
        var state = DecayHeatState.CreateEquilibrium(solver.Definition, Power.FromMegawatts(1_000d));

        var snapshot = solver.CreateSnapshot(state);

        Assert.Equal(40d, snapshot.Groups.Single(group => group.GroupId == "fast").InstantaneousDecayHeatPower.Megawatts, 9);
        Assert.Equal(20d, snapshot.Groups.Single(group => group.GroupId == "slow").InstantaneousDecayHeatPower.Megawatts, 9);
    }

    [Fact]
    public void MismatchedStateGroupSet_FailsFast()
    {
        var solver = Solver();
        var state = new DecayHeatState([
            new DecayHeatGroupState("other", Energy.FromMegajoules(1d)),
            new DecayHeatGroupState("slow", Energy.FromMegajoules(1d)),
        ]);

        Assert.Throws<ArgumentException>(() => solver.CreateSnapshot(state));
    }

    [Fact]
    public void ExtremeEquilibriumOutsideFiniteRange_FailsFast()
    {
        var definition = new DecayHeatDefinition(
            "overflow",
            [new DecayHeatGroupDefinition(
                "g",
                DecayHeatGenerationFraction.Full,
                DecayConstant.FromPerSecond(double.Epsilon))],
            [Destination("fuel", 1d)]);

        Assert.Throws<InvalidOperationException>(() => DecayHeatState.CreateEquilibrium(definition, Power.FromWatts(double.MaxValue)));
    }

    [Fact]
    public void SameInputs_ProduceIdenticalResults()
    {
        var solver = Solver();
        var state = DecayHeatState.CreateEquilibrium(solver.Definition, Power.FromMegawatts(1_000d));

        var left = solver.Step(state, Power.FromMegawatts(450d), TimeSpan.FromMilliseconds(20d));
        var right = solver.Step(state, Power.FromMegawatts(450d), TimeSpan.FromMilliseconds(20d));

        Assert.Equal(left.State.TotalStoredDecayEnergy, right.State.TotalStoredDecayEnergy);
        Assert.Equal(left.ProducedDecayEnergy, right.ProducedDecayEnergy);
        Assert.Equal(left.EmittedDecayEnergy, right.EmittedDecayEnergy);
        Assert.Equal(left.AverageDecayHeatPower, right.AverageDecayHeatPower);
        Assert.Equal(left.Snapshot.TotalInstantaneousDecayHeatPower, right.Snapshot.TotalInstantaneousDecayHeatPower);
        Assert.Equal(
            left.AverageHeatDepositions.Select(static item => (item.TargetDomainId, item.ThermalPower)),
            right.AverageHeatDepositions.Select(static item => (item.TargetDomainId, item.ThermalPower)));
    }

    private static DecayHeatSolver Solver()
        => new(new DecayHeatDefinition(
            "core-decay",
            [
                new DecayHeatGroupDefinition(
                    "slow",
                    DecayHeatGenerationFraction.FromFraction(0.02d),
                    DecayConstant.FromPerSecond(0.01d)),
                new DecayHeatGroupDefinition(
                    "fast",
                    DecayHeatGenerationFraction.FromFraction(0.04d),
                    DecayConstant.FromPerSecond(0.2d)),
            ],
            [
                Destination("fuel", 0.8d),
                Destination("coolant", 0.2d),
            ]));

    private static DecayHeatDestinationDefinition Destination(string id, double fraction)
        => new(id, HeatDepositionFraction.FromFraction(fraction));
}
