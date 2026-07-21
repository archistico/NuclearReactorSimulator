using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.Reactor;
using NuclearReactorSimulator.Domain.Physics.Reactor.Feedback;
using NuclearReactorSimulator.Simulation.Physics.Reactor.Feedback;
using Xunit;

namespace NuclearReactorSimulator.Simulation.Tests.Physics.Reactor.Feedback;

public sealed class VoidFeedbackSolverTests
{
    [Fact]
    public void PositiveCoefficient_IncreasingVoidAddsPositiveReactivity()
    {
        var solver = new VoidFeedbackSolver();
        var definition = new VoidReactivityFeedbackDefinition(
            "void/core",
            VoidFraction.FromPercent(20d),
            VoidReactivityCoefficient.FromPcmPerPercentVoid(4d));

        var snapshot = solver.Evaluate(new VoidFeedbackInput(
            definition,
            VoidFraction.FromPercent(35d)));

        Assert.Equal(15d, snapshot.VoidFractionDifference.PercentagePoints, 12);
        Assert.Equal(60d, snapshot.Reactivity.Pcm, 9);
        Assert.Equal(ReactivityContributionKind.Void, snapshot.Kind);
        Assert.Equal(snapshot.Reactivity, snapshot.ToContribution().Value);
    }

    [Fact]
    public void NegativeCoefficient_IncreasingVoidAddsNegativeReactivity()
    {
        var solver = new VoidFeedbackSolver();
        var definition = new VoidReactivityFeedbackDefinition(
            "void/core",
            VoidFraction.FromPercent(10d),
            VoidReactivityCoefficient.FromPcmPerPercentVoid(-3d));

        var snapshot = solver.Evaluate(new VoidFeedbackInput(
            definition,
            VoidFraction.FromPercent(25d)));

        Assert.Equal(-45d, snapshot.Reactivity.Pcm, 9);
    }

    [Fact]
    public void ReferenceVoid_ProducesZeroReactivity()
    {
        var solver = new VoidFeedbackSolver();
        var reference = VoidFraction.FromPercent(42d);
        var definition = new VoidReactivityFeedbackDefinition(
            "void/core",
            reference,
            VoidReactivityCoefficient.FromPcmPerPercentVoid(8d));

        var snapshot = solver.Evaluate(new VoidFeedbackInput(definition, reference));

        Assert.Equal(Reactivity.Zero, snapshot.Reactivity);
        Assert.Equal(VoidFractionDifference.Zero, snapshot.VoidFractionDifference);
    }

    [Fact]
    public void MultipleInputs_AreCanonicalAndComposeThroughReactivityModel()
    {
        var solver = new VoidFeedbackSolver();
        var inputs = new[]
        {
            Create("void/b", 10d, 20d, 2d),
            Create("void/a", 30d, 20d, 1d),
        };

        var snapshot = solver.Evaluate(inputs);

        Assert.Equal(new[] { "void/a", "void/b" }, snapshot.Feedbacks.Select(item => item.Id).ToArray());
        Assert.Equal(-10d, snapshot.ReactivityBreakdown.Total.Pcm, 9);
        Assert.Equal(-10d, snapshot.ReactivityBreakdown.TotalFor(ReactivityContributionKind.Void).Pcm, 9);
    }

    [Fact]
    public void DuplicateIds_FailFast()
    {
        var solver = new VoidFeedbackSolver();

        Assert.Throws<ArgumentException>(() => solver.Evaluate([
            Create("void/core", 10d, 0d, 1d),
            Create("void/core", 20d, 0d, 1d),
        ]));
    }

    private static VoidFeedbackInput Create(
        string id,
        double measuredPercent,
        double referencePercent,
        double pcmPerPercentVoid)
        => new(
            new VoidReactivityFeedbackDefinition(
                id,
                VoidFraction.FromPercent(referencePercent),
                VoidReactivityCoefficient.FromPcmPerPercentVoid(pcmPerPercentVoid)),
            VoidFraction.FromPercent(measuredPercent));
}
