using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.Reactor;
using NuclearReactorSimulator.Domain.Physics.Reactor.Feedback;
using NuclearReactorSimulator.Simulation.Physics.Reactor.Feedback;
using Xunit;

namespace NuclearReactorSimulator.Simulation.Tests.Physics.Reactor.Feedback;

public sealed class TemperatureFeedbackSolverTests
{
    private readonly TemperatureFeedbackSolver _solver = new();

    [Fact]
    public void ReferenceTemperature_ProducesZeroReactivity()
    {
        var definition = FuelDefinition(-2d, 700d);

        var snapshot = _solver.Evaluate(new TemperatureFeedbackInput(
            definition,
            Temperature.FromDegreesCelsius(700d)));

        Assert.Equal(0d, snapshot.TemperatureDifference.Kelvins, 12);
        Assert.Equal(0d, snapshot.Reactivity.Pcm, 12);
        Assert.Equal(ReactivityContributionKind.FuelTemperature, snapshot.ToContribution().Kind);
    }

    [Fact]
    public void NegativeFuelCoefficient_AddsNegativeReactivityAboveReference()
    {
        var snapshot = _solver.Evaluate(new TemperatureFeedbackInput(
            FuelDefinition(-2.5d, 700d),
            Temperature.FromDegreesCelsius(720d)));

        Assert.Equal(20d, snapshot.TemperatureDifference.Kelvins, 12);
        Assert.Equal(-50d, snapshot.Reactivity.Pcm, 10);
    }

    [Fact]
    public void NegativeFuelCoefficient_AddsPositiveReactivityBelowReference()
    {
        var snapshot = _solver.Evaluate(new TemperatureFeedbackInput(
            FuelDefinition(-2.5d, 700d),
            Temperature.FromDegreesCelsius(680d)));

        Assert.Equal(-20d, snapshot.TemperatureDifference.Kelvins, 12);
        Assert.Equal(50d, snapshot.Reactivity.Pcm, 10);
    }

    [Fact]
    public void PositiveCoolantCoefficient_IsSupportedWithoutHardcodedReactorAssumption()
    {
        var definition = new TemperatureReactivityFeedbackDefinition(
            "coolant-temperature/core",
            ReactivityContributionKind.CoolantTemperature,
            Temperature.FromDegreesCelsius(280d),
            TemperatureReactivityCoefficient.FromPcmPerKelvin(1.5d));

        var snapshot = _solver.Evaluate(new TemperatureFeedbackInput(
            definition,
            Temperature.FromDegreesCelsius(290d)));

        Assert.Equal(15d, snapshot.Reactivity.Pcm, 10);
    }

    [Fact]
    public void EvaluateSet_CanonicalizesByKindThenIdAndComposesTotals()
    {
        var coolant = new TemperatureFeedbackInput(
            new TemperatureReactivityFeedbackDefinition(
                "coolant-b",
                ReactivityContributionKind.CoolantTemperature,
                Temperature.FromDegreesCelsius(280d),
                TemperatureReactivityCoefficient.FromPcmPerKelvin(1d)),
            Temperature.FromDegreesCelsius(290d));
        var fuelB = new TemperatureFeedbackInput(FuelDefinition(-2d, 700d, "fuel-b"), Temperature.FromDegreesCelsius(710d));
        var fuelA = new TemperatureFeedbackInput(FuelDefinition(-1d, 700d, "fuel-a"), Temperature.FromDegreesCelsius(710d));

        var snapshot = _solver.Evaluate([coolant, fuelB, fuelA]);

        Assert.Equal(new[] { "fuel-a", "fuel-b", "coolant-b" }, snapshot.Feedbacks.Select(item => item.Id).ToArray());
        Assert.Equal(-20d, snapshot.ReactivityBreakdown.Total.Pcm, 10);
        Assert.Equal(-30d, snapshot.ReactivityBreakdown.TotalFor(ReactivityContributionKind.FuelTemperature).Pcm, 10);
        Assert.Equal(10d, snapshot.ReactivityBreakdown.TotalFor(ReactivityContributionKind.CoolantTemperature).Pcm, 10);
    }

    [Fact]
    public void TemperatureContribution_ComposesWithExistingReactivitySources()
    {
        var feedback = _solver.Evaluate(new TemperatureFeedbackInput(
            FuelDefinition(-2d, 700d),
            Temperature.FromDegreesCelsius(710d)));

        var breakdown = new NuclearReactorSimulator.Simulation.Physics.Reactor.ReactivityModel().Evaluate(
        [
            new ReactivityContribution(
                "control-rods/group-a",
                ReactivityContributionKind.ControlRods,
                Reactivity.FromPcm(120d)),
            feedback.ToContribution(),
        ]);

        Assert.Equal(100d, breakdown.Total.Pcm, 10);
        Assert.Equal(120d, breakdown.TotalFor(ReactivityContributionKind.ControlRods).Pcm, 10);
        Assert.Equal(-20d, breakdown.TotalFor(ReactivityContributionKind.FuelTemperature).Pcm, 10);
    }

    [Fact]
    public void EvaluateSet_RejectsDuplicateIds()
    {
        var definitionA = FuelDefinition(-1d, 700d, "same");
        var definitionB = new TemperatureReactivityFeedbackDefinition(
            "same",
            ReactivityContributionKind.CoolantTemperature,
            Temperature.FromDegreesCelsius(280d),
            TemperatureReactivityCoefficient.FromPcmPerKelvin(1d));

        Assert.Throws<ArgumentException>(() => _solver.Evaluate(
        [
            new TemperatureFeedbackInput(definitionA, Temperature.FromDegreesCelsius(700d)),
            new TemperatureFeedbackInput(definitionB, Temperature.FromDegreesCelsius(280d)),
        ]));
    }

    [Fact]
    public void IdenticalInputs_ProduceIdenticalSnapshot()
    {
        var input = new TemperatureFeedbackInput(FuelDefinition(-2d, 700d), Temperature.FromDegreesCelsius(715d));

        var left = _solver.Evaluate(input);
        var right = _solver.Evaluate(input);

        Assert.Equal(left, right);
    }

    private static TemperatureReactivityFeedbackDefinition FuelDefinition(
        double pcmPerKelvin,
        double referenceCelsius,
        string id = "fuel-temperature/core")
        => new(
            id,
            ReactivityContributionKind.FuelTemperature,
            Temperature.FromDegreesCelsius(referenceCelsius),
            TemperatureReactivityCoefficient.FromPcmPerKelvin(pcmPerKelvin));
}
