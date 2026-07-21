using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.Reactor;
using NuclearReactorSimulator.Domain.Physics.Reactor.Neutronics;
using NuclearReactorSimulator.Simulation.Physics.Reactor;
using NuclearReactorSimulator.Simulation.Physics.Reactor.Neutronics;
using Xunit;

namespace NuclearReactorSimulator.Simulation.Tests.Physics.Reactor.Neutronics;

public sealed class PointKineticsSolverTests
{
    [Fact]
    public void CriticalEquilibrium_RemainsStableAtZeroReactivity()
    {
        var parameters = Parameters();
        var solver = new PointKineticsSolver(parameters);
        var initial = PointKineticsState.CreateCriticalEquilibrium(parameters, NeutronPopulation.Reference);

        var result = solver.Step(initial, Reactivity.Zero, TimeSpan.FromSeconds(2d));
        var snapshot = solver.CreateSnapshot(result, Reactivity.Zero);

        Assert.Equal(1d, result.NeutronPopulation.Relative, 10);
        Assert.Equal(initial.GetGroup("fast").PrecursorPopulation.Relative, result.GetGroup("fast").PrecursorPopulation.Relative, 10);
        Assert.Equal(initial.GetGroup("slow").PrecursorPopulation.Relative, result.GetGroup("slow").PrecursorPopulation.Relative, 10);
        Assert.Null(snapshot.ReactorPeriodSeconds);
        Assert.Equal(0d, snapshot.LogarithmicNeutronPopulationRatePerSecond!.Value, 10);
    }

    [Fact]
    public void PositiveSubPromptReactivity_IncreasesNeutronPopulation()
    {
        var parameters = Parameters();
        var solver = new PointKineticsSolver(parameters);
        var initial = PointKineticsState.CreateCriticalEquilibrium(parameters, NeutronPopulation.Reference);

        var result = solver.Step(initial, Reactivity.FromPcm(100d), TimeSpan.FromSeconds(1d));

        Assert.True(result.NeutronPopulation > initial.NeutronPopulation);
        Assert.True(result.GetGroup("fast").PrecursorPopulation.Relative > initial.GetGroup("fast").PrecursorPopulation.Relative);
        Assert.True(result.GetGroup("slow").PrecursorPopulation.Relative > initial.GetGroup("slow").PrecursorPopulation.Relative);
    }

    [Fact]
    public void NegativeReactivity_DecreasesNeutronPopulation()
    {
        var parameters = Parameters();
        var solver = new PointKineticsSolver(parameters);
        var initial = PointKineticsState.CreateCriticalEquilibrium(parameters, NeutronPopulation.Reference);

        var result = solver.Step(initial, Reactivity.FromPcm(-100d), TimeSpan.FromSeconds(1d));

        Assert.True(result.NeutronPopulation < initial.NeutronPopulation);
    }

    [Fact]
    public void ReactivityModelTotal_IsAComposableInputToPointKinetics()
    {
        var parameters = Parameters();
        var kinetics = new PointKineticsSolver(parameters);
        var reactivityModel = new ReactivityModel();
        var initial = PointKineticsState.CreateCriticalEquilibrium(parameters, NeutronPopulation.Reference);
        var total = reactivityModel.Evaluate(
        [
            new ReactivityContribution("control-rods", ReactivityContributionKind.ControlRods, Reactivity.FromPcm(250d)),
            new ReactivityContribution("xenon", ReactivityContributionKind.Xenon, Reactivity.FromPcm(-150d)),
        ]).Total;

        var result = kinetics.Step(initial, total, TimeSpan.FromSeconds(1d));

        Assert.Equal(100d, total.Pcm, 10);
        Assert.True(result.NeutronPopulation > initial.NeutronPopulation);
    }

    [Fact]
    public void Snapshot_ReportsPromptCriticalMarginDollarsAndSignedPeriod()
    {
        var parameters = Parameters();
        var solver = new PointKineticsSolver(parameters);
        var initial = PointKineticsState.CreateCriticalEquilibrium(parameters, NeutronPopulation.Reference);
        var positiveState = solver.Step(initial, Reactivity.FromPcm(100d), TimeSpan.FromSeconds(0.5d));
        var negativeState = solver.Step(initial, Reactivity.FromPcm(-100d), TimeSpan.FromSeconds(0.5d));

        var positive = solver.CreateSnapshot(positiveState, Reactivity.FromPcm(100d));
        var negative = solver.CreateSnapshot(negativeState, Reactivity.FromPcm(-100d));
        var promptCritical = solver.CreateSnapshot(initial, Reactivity.FromPcm(650d));

        Assert.False(positive.IsPromptCritical);
        Assert.Equal(-550d, positive.PromptCriticalMargin.Pcm, 8);
        Assert.Equal(0.001d / 0.0065d, positive.ReactivityDollars, 10);
        Assert.True(positive.ReactorPeriodSeconds > 0d);
        Assert.True(negative.ReactorPeriodSeconds < 0d);
        Assert.True(promptCritical.IsPromptCritical);
        Assert.Equal(0d, promptCritical.PromptCriticalMargin.Pcm, 8);
        Assert.Equal(1d, promptCritical.ReactivityDollars, 10);
        Assert.Equal(100d, promptCritical.ReactivityCents, 8);
    }

    [Fact]
    public void PromptSupercriticalStep_RemainsFiniteAndNonNegativeWithinSupportedEnvelope()
    {
        var parameters = Parameters();
        var solver = new PointKineticsSolver(parameters);
        var initial = PointKineticsState.CreateCriticalEquilibrium(parameters, NeutronPopulation.Reference);

        var result = solver.Step(initial, Reactivity.FromPcm(800d), TimeSpan.FromMilliseconds(100d));

        Assert.True(double.IsFinite(result.NeutronPopulation.Relative));
        Assert.True(result.NeutronPopulation.Relative > 1d);
        Assert.All(result.DelayedNeutronGroups, static group => Assert.True(group.PrecursorPopulation.Relative >= 0d));
    }

    [Fact]
    public void StateCoverageMustMatchCanonicalParameterSet()
    {
        var parameters = Parameters();
        var solver = new PointKineticsSolver(parameters);
        var wrongState = new PointKineticsState(
            NeutronPopulation.Reference,
            [new DelayedNeutronGroupState("wrong", DelayedNeutronPrecursorPopulation.FromRelative(1d))]);

        Assert.Throws<ArgumentException>(() => solver.Step(wrongState, Reactivity.Zero, TimeSpan.FromMilliseconds(20d)));
    }

    [Fact]
    public void SameInputs_ProduceSameStateAndDiagnostics()
    {
        var parameters = Parameters();
        var solver = new PointKineticsSolver(parameters);
        var initial = PointKineticsState.CreateCriticalEquilibrium(parameters, NeutronPopulation.Reference);
        var reactivity = Reactivity.FromPcm(175d);

        var left = solver.Step(initial, reactivity, TimeSpan.FromMilliseconds(250d));
        var right = solver.Step(initial, reactivity, TimeSpan.FromMilliseconds(250d));
        var leftSnapshot = solver.CreateSnapshot(left, reactivity);
        var rightSnapshot = solver.CreateSnapshot(right, reactivity);

        Assert.Equal(left.NeutronPopulation, right.NeutronPopulation);
        Assert.Equal(
            left.DelayedNeutronGroups.Select(static group => group.PrecursorPopulation),
            right.DelayedNeutronGroups.Select(static group => group.PrecursorPopulation));
        Assert.Equal(leftSnapshot.ReactorPeriodSeconds, rightSnapshot.ReactorPeriodSeconds);
        Assert.Equal(leftSnapshot.ReactivityDollars, rightSnapshot.ReactivityDollars);
    }

    private static PointKineticsParameters Parameters()
        => new(
            TimeSpan.FromMilliseconds(5d),
            [
                new DelayedNeutronGroupDefinition(
                    "slow",
                    DelayedNeutronFraction.FromFraction(0.004d),
                    DecayConstant.FromPerSecond(0.08d)),
                new DelayedNeutronGroupDefinition(
                    "fast",
                    DelayedNeutronFraction.FromFraction(0.0025d),
                    DecayConstant.FromPerSecond(0.8d)),
            ]);
}
