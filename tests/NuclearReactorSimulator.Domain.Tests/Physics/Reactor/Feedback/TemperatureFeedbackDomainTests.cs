using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.Reactor;
using NuclearReactorSimulator.Domain.Physics.Reactor.Feedback;
using Xunit;

namespace NuclearReactorSimulator.Domain.Tests.Physics.Reactor.Feedback;

public sealed class TemperatureFeedbackDomainTests
{
    [Fact]
    public void TemperatureReactivityCoefficient_StoresCanonicalUnitsAndConvertsPcmPerKelvin()
    {
        var coefficient = TemperatureReactivityCoefficient.FromPcmPerKelvin(-2.5d);

        Assert.Equal(-0.000025d, coefficient.DeltaKOverKPerKelvin, 12);
        Assert.Equal(-2.5d, coefficient.PcmPerKelvin, 12);
    }

    [Fact]
    public void TemperatureReactivityCoefficient_MultipliedByTemperatureDifference_ProducesReactivity()
    {
        var coefficient = TemperatureReactivityCoefficient.FromPcmPerKelvin(-2d);
        var delta = TemperatureDifference.FromKelvins(15d);

        Assert.Equal(-30d, (coefficient * delta).Pcm, 12);
        Assert.Equal(-30d, (delta * coefficient).Pcm, 12);
    }

    [Fact]
    public void TemperatureReactivityCoefficient_IsSignedAndRejectsNonFiniteValues()
    {
        Assert.Equal(2d, TemperatureReactivityCoefficient.FromPcmPerKelvin(2d).PcmPerKelvin, 12);
        Assert.Equal(-2d, TemperatureReactivityCoefficient.FromPcmPerKelvin(-2d).PcmPerKelvin, 12);
        Assert.Equal(TemperatureReactivityCoefficient.Zero, TemperatureReactivityCoefficient.FromPcmPerKelvin(-0d));
        Assert.Throws<ArgumentOutOfRangeException>(() => TemperatureReactivityCoefficient.FromPcmPerKelvin(double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => TemperatureReactivityCoefficient.FromDeltaKOverKPerKelvin(double.PositiveInfinity));
    }

    [Theory]
    [InlineData(ReactivityContributionKind.FuelTemperature)]
    [InlineData(ReactivityContributionKind.CoolantTemperature)]
    public void FeedbackDefinition_AcceptsOnlyTemperatureCategories(ReactivityContributionKind kind)
    {
        var definition = new TemperatureReactivityFeedbackDefinition(
            "feedback",
            kind,
            Temperature.FromDegreesCelsius(300d),
            TemperatureReactivityCoefficient.FromPcmPerKelvin(-1d));

        Assert.Equal(kind, definition.Kind);
    }

    [Fact]
    public void FeedbackDefinition_RejectsNonTemperatureCategoryAndEmptyId()
    {
        Assert.Throws<ArgumentException>(() => new TemperatureReactivityFeedbackDefinition(
            " ",
            ReactivityContributionKind.FuelTemperature,
            Temperature.FromDegreesCelsius(700d),
            TemperatureReactivityCoefficient.Zero));

        Assert.Throws<ArgumentOutOfRangeException>(() => new TemperatureReactivityFeedbackDefinition(
            "invalid",
            ReactivityContributionKind.Void,
            Temperature.FromDegreesCelsius(700d),
            TemperatureReactivityCoefficient.Zero));
    }
}
