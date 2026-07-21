using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.Reactor.ThermalPower;
using Xunit;

namespace NuclearReactorSimulator.Domain.Tests.Physics.Reactor.ThermalPower;

public sealed class FissionPowerDomainTests
{
    [Fact]
    public void HeatDepositionFraction_UsesExplicitFractionAndPercentViews()
    {
        var fraction = HeatDepositionFraction.FromPercent(37.5d);

        Assert.Equal(0.375d, fraction.Fraction, 12);
        Assert.Equal(37.5d, fraction.Percent, 12);
    }

    [Theory]
    [InlineData(-0.001d)]
    [InlineData(1.001d)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void HeatDepositionFraction_RejectsInvalidValues(double value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => HeatDepositionFraction.FromFraction(value));
    }

    [Fact]
    public void Calibration_RequiresPositiveReferencePopulationAndPower()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new FissionPowerCalibration(
            NeutronPopulation.Zero,
            Power.FromMegawatts(3_200d)));

        Assert.Throws<ArgumentOutOfRangeException>(() => new FissionPowerCalibration(
            NeutronPopulation.Reference,
            Power.Zero));

        Assert.Throws<ArgumentOutOfRangeException>(() => new FissionPowerCalibration(
            NeutronPopulation.Reference,
            Power.FromWatts(-1d)));
    }

    [Fact]
    public void Definition_CanonicalizesDestinationsAndIsIndependentFromCallerArray()
    {
        var source = new[]
        {
            Destination("structures", 0.1d),
            Destination("fuel", 0.7d),
            Destination("coolant", 0.2d),
        };
        var definition = new FissionPowerDefinition("core-fission", Calibration(), source);

        source[0] = Destination("replacement", 0.1d);

        Assert.Equal(new[] { "coolant", "fuel", "structures" }, definition.HeatDestinations.Select(static item => item.TargetDomainId));
        Assert.Equal(0.7d, definition.GetDestination("fuel").Fraction.Fraction, 12);
    }

    [Fact]
    public void Definition_RejectsDuplicateTargetsAndIncompletePartition()
    {
        Assert.Throws<ArgumentException>(() => new FissionPowerDefinition(
            "duplicate",
            Calibration(),
            [Destination("fuel", 0.5d), Destination("fuel", 0.5d)]));

        Assert.Throws<ArgumentException>(() => new FissionPowerDefinition(
            "incomplete",
            Calibration(),
            [Destination("fuel", 0.7d), Destination("coolant", 0.2d)]));
    }

    [Fact]
    public void Destination_RejectsZeroFractionAndEmptyTarget()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new FissionHeatDestinationDefinition(
            "fuel",
            HeatDepositionFraction.Zero));

        Assert.Throws<ArgumentException>(() => new FissionHeatDestinationDefinition(
            " ",
            HeatDepositionFraction.Full));
    }

    private static FissionPowerCalibration Calibration()
        => new(NeutronPopulation.Reference, Power.FromMegawatts(3_200d));

    private static FissionHeatDestinationDefinition Destination(string id, double fraction)
        => new(id, HeatDepositionFraction.FromFraction(fraction));
}
