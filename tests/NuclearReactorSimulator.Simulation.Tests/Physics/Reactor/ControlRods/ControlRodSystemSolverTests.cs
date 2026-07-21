using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.Reactor.ControlRods;
using NuclearReactorSimulator.Simulation.Physics.Reactor.ControlRods;
using Xunit;

namespace NuclearReactorSimulator.Simulation.Tests.Physics.Reactor.ControlRods;

public sealed class ControlRodSystemSolverTests
{
    [Fact]
    public void GroupCommand_MovesOnlyMembersOfTargetGroup()
    {
        var definition = Definition();
        var solver = new ControlRodSystemSolver(definition);
        var state = InitialState();

        var result = solver.Step(
            state,
            [new ControlRodCommand("bank-a", ControlRodCommandTargetKind.Group, ControlRodMotion.Withdraw)],
            TimeSpan.FromSeconds(1d));

        Assert.Equal(10d, result.GetRod("rod-a1").Position.PercentWithdrawn, 10);
        Assert.Equal(10d, result.GetRod("rod-a2").Position.PercentWithdrawn, 10);
        Assert.Equal(0d, result.GetRod("rod-b1").Position.PercentWithdrawn, 10);
    }

    [Fact]
    public void LaterIndividualCommand_OverridesEarlierGroupCommandInSameStep()
    {
        var solver = new ControlRodSystemSolver(Definition());

        var result = solver.Step(
            InitialState(),
            [
                new ControlRodCommand("bank-a", ControlRodCommandTargetKind.Group, ControlRodMotion.Withdraw),
                new ControlRodCommand("rod-a2", ControlRodCommandTargetKind.Rod, ControlRodMotion.Hold),
            ],
            TimeSpan.FromSeconds(1d));

        Assert.Equal(10d, result.GetRod("rod-a1").Position.PercentWithdrawn, 10);
        Assert.Equal(0d, result.GetRod("rod-a2").Position.PercentWithdrawn, 10);
    }

    [Fact]
    public void MotionCommand_PersistsAcrossStepsUntilChangedOrLimitReached()
    {
        var solver = new ControlRodSystemSolver(Definition());
        var state = solver.Step(
            InitialState(),
            [new ControlRodCommand("rod-b1", ControlRodCommandTargetKind.Rod, ControlRodMotion.Withdraw)],
            TimeSpan.FromSeconds(1d));

        state = solver.Step(state, [], TimeSpan.FromSeconds(1d));

        Assert.Equal(20d, state.GetRod("rod-b1").Position.PercentWithdrawn, 10);
        Assert.Equal(ControlRodMotion.Withdraw, state.GetRod("rod-b1").Motion);
    }

    [Fact]
    public void ReactivityBreakdown_ContainsOneCanonicalContributionPerRod()
    {
        var definition = Definition();
        var state = new ControlRodSystemState(
        [
            new ControlRodState("rod-a1", ControlRodPosition.FullyInserted),
            new ControlRodState("rod-a2", ControlRodPosition.FullyWithdrawn),
            new ControlRodState("rod-b1", ControlRodPosition.FromPercentWithdrawn(50d)),
        ]);

        var snapshot = new ControlRodReactivitySolver(definition).Evaluate(state);

        Assert.Equal(3, snapshot.Contributions.Count);
        Assert.Equal(new[] { "control-rods/rod-a1", "control-rods/rod-a2", "control-rods/rod-b1" }, snapshot.Contributions.Select(static item => item.Id));
        Assert.Equal(-1_500d, snapshot.Total.Pcm, 8);
    }

    [Fact]
    public void IncompleteState_IsRejectedBeforeAdvancement()
    {
        var solver = new ControlRodSystemSolver(Definition());
        var incomplete = new ControlRodSystemState([new ControlRodState("rod-a1", ControlRodPosition.FullyInserted)]);

        Assert.Throws<ArgumentException>(() => solver.Step(incomplete, [], TimeSpan.FromSeconds(1d)));
    }

    private static ControlRodSystemDefinition Definition()
    {
        var rate = ControlRodTravelRate.FromFractionPerSecond(0.1d);
        var rods = new[]
        {
            Rod("rod-a1", "bank-a", rate, -1_000d),
            Rod("rod-a2", "bank-a", rate, -1_000d),
            Rod("rod-b1", "bank-b", rate, -1_000d),
        };
        var groups = new[]
        {
            new ControlRodGroupDefinition("bank-a", ["rod-a1", "rod-a2"]),
            new ControlRodGroupDefinition("bank-b", ["rod-b1"]),
        };

        return new ControlRodSystemDefinition(rods, groups);
    }

    private static ControlRodSystemState InitialState()
        => new(
        [
            new ControlRodState("rod-a1", ControlRodPosition.FullyInserted),
            new ControlRodState("rod-a2", ControlRodPosition.FullyInserted),
            new ControlRodState("rod-b1", ControlRodPosition.FullyInserted),
        ]);

    private static ControlRodDefinition Rod(
        string id,
        string groupId,
        ControlRodTravelRate rate,
        double insertedPcm)
    {
        return new ControlRodDefinition(
            id,
            groupId,
            rate,
            Reactivity.FromPcm(insertedPcm),
            Reactivity.Zero,
            ControlRodWorthCurveKind.Linear);
    }
}
