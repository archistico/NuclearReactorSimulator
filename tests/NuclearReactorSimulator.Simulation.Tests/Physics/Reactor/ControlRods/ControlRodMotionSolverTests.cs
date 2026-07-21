using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.Reactor.ControlRods;
using NuclearReactorSimulator.Simulation.Physics.Reactor.ControlRods;
using Xunit;

namespace NuclearReactorSimulator.Simulation.Tests.Physics.Reactor.ControlRods;

public sealed class ControlRodMotionSolverTests
{
    [Fact]
    public void Withdraw_AdvancesAtConfiguredTravelRate()
    {
        var definition = Definition(ControlRodTravelRate.FromFractionPerSecond(0.1d));
        var state = new ControlRodState("rod-a", ControlRodPosition.FromPercentWithdrawn(20d), ControlRodMotion.Withdraw);

        var result = new ControlRodMotionSolver().Advance(definition, state, TimeSpan.FromSeconds(2d));

        Assert.Equal(40d, result.Position.PercentWithdrawn, 10);
        Assert.Equal(ControlRodMotion.Withdraw, result.Motion);
    }

    [Fact]
    public void Insert_ClampsAtMechanicalLimitAndAutomaticallyHolds()
    {
        var definition = Definition(ControlRodTravelRate.FromFractionPerSecond(0.5d));
        var state = new ControlRodState("rod-a", ControlRodPosition.FromPercentWithdrawn(20d), ControlRodMotion.Insert);

        var result = new ControlRodMotionSolver().Advance(definition, state, TimeSpan.FromSeconds(1d));

        Assert.Equal(ControlRodPosition.FullyInserted, result.Position);
        Assert.Equal(ControlRodMotion.Hold, result.Motion);
    }

    [Fact]
    public void Withdraw_ClampsAtMechanicalLimitAndAutomaticallyHolds()
    {
        var definition = Definition(ControlRodTravelRate.FromFractionPerSecond(0.5d));
        var state = new ControlRodState("rod-a", ControlRodPosition.FromPercentWithdrawn(90d), ControlRodMotion.Withdraw);

        var result = new ControlRodMotionSolver().Advance(definition, state, TimeSpan.FromSeconds(1d));

        Assert.Equal(ControlRodPosition.FullyWithdrawn, result.Position);
        Assert.Equal(ControlRodMotion.Hold, result.Motion);
    }

    [Fact]
    public void Hold_PreservesPosition()
    {
        var definition = Definition(ControlRodTravelRate.FromFractionPerSecond(0.5d));
        var state = new ControlRodState("rod-a", ControlRodPosition.FromPercentWithdrawn(37d), ControlRodMotion.Hold);

        var result = new ControlRodMotionSolver().Advance(definition, state, TimeSpan.FromSeconds(10d));

        Assert.Same(state, result);
    }

    [Fact]
    public void WrongStateIdentity_IsRejected()
    {
        var definition = Definition(ControlRodTravelRate.FromFractionPerSecond(0.1d));
        var state = new ControlRodState("other", ControlRodPosition.FullyInserted);

        Assert.Throws<ArgumentException>(() => new ControlRodMotionSolver().Advance(definition, state, TimeSpan.FromSeconds(1d)));
    }

    private static ControlRodDefinition Definition(ControlRodTravelRate travelRate)
    {
        return new ControlRodDefinition(
            "rod-a",
            "bank-a",
            travelRate,
            Reactivity.FromPcm(-1_000d),
            Reactivity.Zero);
    }
}
