using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.Reactor;
using NuclearReactorSimulator.Domain.Physics.Reactor.ControlRods;
using NuclearReactorSimulator.Simulation.Physics.Reactor.ControlRods;
using Xunit;

namespace NuclearReactorSimulator.Simulation.Tests.Physics.Reactor.ControlRods;

public sealed class ControlRodWorthSolverTests
{
    [Fact]
    public void LinearCurve_InterpolatesWorthBetweenExplicitEndpoints()
    {
        var definition = Definition(ControlRodWorthCurveKind.Linear);
        var solver = new ControlRodWorthSolver();

        var inserted = solver.Evaluate(definition, State(0d));
        var half = solver.Evaluate(definition, State(0.5d));
        var withdrawn = solver.Evaluate(definition, State(1d));

        Assert.Equal(-2_000d, inserted.Value.Pcm, 10);
        Assert.Equal(-1_000d, half.Value.Pcm, 10);
        Assert.Equal(0d, withdrawn.Value.Pcm, 10);
        Assert.Equal(ReactivityContributionKind.ControlRods, half.Kind);
        Assert.Equal("control-rods/rod-a", half.Id);
    }

    [Fact]
    public void SmoothStepCurve_PreservesEndpointsAndProvidesNonLinearIntegralWorth()
    {
        var definition = Definition(ControlRodWorthCurveKind.SmoothStep);
        var solver = new ControlRodWorthSolver();

        var quarter = solver.Evaluate(definition, State(0.25d));
        var threeQuarter = solver.Evaluate(definition, State(0.75d));

        Assert.Equal(-1_687.5d, quarter.Value.Pcm, 8);
        Assert.Equal(-312.5d, threeQuarter.Value.Pcm, 8);
        Assert.Equal(-2_000d, solver.Evaluate(definition, State(0d)).Value.Pcm, 10);
        Assert.Equal(0d, solver.Evaluate(definition, State(1d)).Value.Pcm, 10);
    }

    [Fact]
    public void SameInput_ProducesSameContribution()
    {
        var definition = Definition(ControlRodWorthCurveKind.SmoothStep);
        var state = State(0.42d);
        var solver = new ControlRodWorthSolver();

        Assert.Equal(solver.Evaluate(definition, state), solver.Evaluate(definition, state));
    }

    private static ControlRodDefinition Definition(ControlRodWorthCurveKind kind)
    {
        return new ControlRodDefinition(
            "rod-a",
            "bank-a",
            ControlRodTravelRate.FromFractionPerSecond(0.1d),
            Reactivity.FromPcm(-2_000d),
            Reactivity.Zero,
            kind);
    }

    private static ControlRodState State(double fractionWithdrawn)
        => new("rod-a", ControlRodPosition.FromFractionWithdrawn(fractionWithdrawn));
}
