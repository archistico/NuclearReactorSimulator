using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.Reactor;
using NuclearReactorSimulator.Simulation.Physics.Reactor;
using Xunit;

namespace NuclearReactorSimulator.Simulation.Tests.Physics.Reactor;

public sealed class ReactivityModelTests
{
    [Fact]
    public void EmptyBreakdown_HasExactlyZeroTotal()
    {
        var snapshot = new ReactivityModel().Evaluate([]);

        Assert.Equal(Reactivity.Zero, snapshot.Total);
        Assert.Empty(snapshot.Contributions);
    }

    [Fact]
    public void SignedContributions_AreSummedIntoTotalReactivity()
    {
        var snapshot = new ReactivityModel().Evaluate(
        [
            Contribution("rods", ReactivityContributionKind.ControlRods, 500d),
            Contribution("fuel-temp", ReactivityContributionKind.FuelTemperature, -300d),
            Contribution("xenon", ReactivityContributionKind.Xenon, -150d),
        ]);

        Assert.Equal(50d, snapshot.Total.Pcm, 10);
        Assert.Equal(500d, snapshot.TotalFor(ReactivityContributionKind.ControlRods).Pcm, 10);
        Assert.Equal(-300d, snapshot.TotalFor(ReactivityContributionKind.FuelTemperature).Pcm, 10);
        Assert.Equal(-150d, snapshot.TotalFor(ReactivityContributionKind.Xenon).Pcm, 10);
        Assert.Equal(Reactivity.Zero, snapshot.TotalFor(ReactivityContributionKind.Void));
    }

    [Fact]
    public void InputPermutation_ProducesSameCanonicalDiagnosticOrderAndTotal()
    {
        var left = new ReactivityModel().Evaluate(
        [
            Contribution("xenon", ReactivityContributionKind.Xenon, -120d),
            Contribution("rod-b", ReactivityContributionKind.ControlRods, 200d),
            Contribution("rod-a", ReactivityContributionKind.ControlRods, 100d),
            Contribution("void", ReactivityContributionKind.Void, 75d),
        ]);

        var right = new ReactivityModel().Evaluate(
        [
            Contribution("void", ReactivityContributionKind.Void, 75d),
            Contribution("rod-a", ReactivityContributionKind.ControlRods, 100d),
            Contribution("xenon", ReactivityContributionKind.Xenon, -120d),
            Contribution("rod-b", ReactivityContributionKind.ControlRods, 200d),
        ]);

        Assert.Equal(left.Total, right.Total);
        Assert.Equal(
            left.Contributions.Select(static contribution => contribution.Id),
            right.Contributions.Select(static contribution => contribution.Id));
        Assert.Equal(
            new[] { "rod-a", "rod-b", "void", "xenon" },
            left.Contributions.Select(static contribution => contribution.Id));
    }

    [Fact]
    public void DuplicateContributionId_IsRejected()
    {
        var model = new ReactivityModel();

        var exception = Assert.Throws<ArgumentException>(() => model.Evaluate(
        [
            Contribution("same", ReactivityContributionKind.ControlRods, 100d),
            Contribution("same", ReactivityContributionKind.Void, -25d),
        ]));

        Assert.Contains("Duplicate", exception.Message);
    }

    [Fact]
    public void Snapshot_DoesNotAliasCallerCollection()
    {
        var source = new[]
        {
            Contribution("rods", ReactivityContributionKind.ControlRods, 100d),
        };
        var snapshot = new ReactivityModel().Evaluate(source);

        source[0] = Contribution("replacement", ReactivityContributionKind.Other, 9_999d);

        Assert.Single(snapshot.Contributions);
        Assert.Equal("rods", snapshot.Contributions[0].Id);
        Assert.Equal(100d, snapshot.Total.Pcm, 12);
    }

    [Fact]
    public void SameInputs_ProduceSameDiagnosticProjection()
    {
        var contributions = new[]
        {
            Contribution("coolant", ReactivityContributionKind.CoolantTemperature, -75d),
            Contribution("void", ReactivityContributionKind.Void, 250d),
            Contribution("xenon", ReactivityContributionKind.Xenon, -130d),
        };
        var model = new ReactivityModel();

        var first = model.Evaluate(contributions);
        var second = model.Evaluate(contributions);

        Assert.Equal(first.Total, second.Total);
        Assert.Equal(
            first.Contributions.Select(static contribution => contribution),
            second.Contributions.Select(static contribution => contribution));
    }

    private static ReactivityContribution Contribution(
        string id,
        ReactivityContributionKind kind,
        double pcm)
    {
        return new ReactivityContribution(id, kind, Reactivity.FromPcm(pcm));
    }
}
