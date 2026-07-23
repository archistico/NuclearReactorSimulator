using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using Xunit;

namespace NuclearReactorSimulator.Domain.Tests.Physics.Fluids;

public sealed class PumpDomainTests
{
    [Fact]
    public void PumpSpeed_ConvertsFractionAndPercent()
    {
        var speed = PumpSpeed.FromPercent(37.5d);

        Assert.Equal(0.375d, speed.Fraction, 12);
        Assert.Equal(37.5d, speed.Percent, 12);
        Assert.False(speed.IsStopped);
        Assert.False(speed.IsRatedSpeed);
    }

    [Theory]
    [InlineData(-0.01d)]
    [InlineData(1.01d)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void PumpSpeed_InvalidValuesAreRejected(double value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => PumpSpeed.FromFraction(value));
    }

    [Fact]
    public void PumpEfficiency_RequiresOpenClosedUnitInterval()
    {
        var efficiency = PumpEfficiency.FromPercent(80d);

        Assert.Equal(0.8d, efficiency.Fraction, 12);
        Assert.Equal(80d, efficiency.Percent, 12);
        Assert.Throws<ArgumentOutOfRangeException>(() => PumpEfficiency.FromFraction(0d));
        Assert.Throws<ArgumentOutOfRangeException>(() => PumpEfficiency.FromFraction(1.01d));
    }

    [Fact]
    public void PumpDefinition_PreservesHydraulicPathAndRatedParameters()
    {
        var pipe = CreatePipe();
        var definition = new PumpDefinition(
            " main-pump ",
            pipe,
            PressureDifference.FromMegapascals(0.4d),
            QuadraticHydraulicResistance.FromPascalSecondsSquaredPerKilogramSquared(50_000d),
            PumpEfficiency.FromPercent(80d));

        Assert.Equal("main-pump", definition.Id);
        Assert.Same(pipe, definition.Pipe);
        Assert.Equal(0.4d, definition.RatedPressureBoost.Megapascals, 12);
        Assert.Equal(50_000d, definition.InternalResistance.PascalSecondsSquaredPerKilogramSquared, 12);
        Assert.Equal(0.8d, definition.Efficiency.Fraction, 12);
        Assert.False(definition.HasDischargeCheckValve);
    }

    [Fact]
    public void PumpDefinition_OptInDischargeCheckValveIsPreserved()
    {
        var definition = new PumpDefinition(
            "pump",
            CreatePipe(),
            PressureDifference.FromMegapascals(0.4d),
            QuadraticHydraulicResistance.FromPascalSecondsSquaredPerKilogramSquared(50_000d),
            PumpEfficiency.FromPercent(80d),
            hasDischargeCheckValve: true);

        Assert.True(definition.HasDischargeCheckValve);
    }

    [Fact]
    public void PumpDefinition_RejectsDefaultInvalidPhysicalParameters()
    {
        var pipe = CreatePipe();
        var resistance = QuadraticHydraulicResistance.FromPascalSecondsSquaredPerKilogramSquared(50_000d);
        var efficiency = PumpEfficiency.FromPercent(80d);

        Assert.Throws<ArgumentOutOfRangeException>(() => new PumpDefinition(
            "pump",
            pipe,
            default,
            resistance,
            efficiency));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PumpDefinition(
            "pump",
            pipe,
            PressureDifference.FromMegapascals(0.4d),
            default,
            efficiency));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PumpDefinition(
            "pump",
            pipe,
            PressureDifference.FromMegapascals(0.4d),
            resistance,
            default));
    }

    [Fact]
    public void PumpState_KeepsCommandedSpeedWhenStopped()
    {
        var state = new PumpState(" pump ", PumpSpeed.FromPercent(72d), false);

        Assert.Equal("pump", state.PumpId);
        Assert.Equal(72d, state.Speed.Percent, 12);
        Assert.False(state.IsRunning);
    }

    private static PipeDefinition CreatePipe()
    {
        return new PipeDefinition(
            "path",
            "from",
            "to",
            QuadraticHydraulicResistance.FromPascalSecondsSquaredPerKilogramSquared(50_000d));
    }
}
