using NuclearReactorSimulator.Domain.Physics.Quantities;
using Xunit;

namespace NuclearReactorSimulator.Domain.Tests.Physics;

public sealed class AbsoluteAndDifferenceQuantityTests
{
    [Fact]
    public void TemperatureDifference_CanMoveAbsoluteTemperatureBothDirections()
    {
        var initial = Temperature.FromDegreesCelsius(280d);
        var rise = TemperatureDifference.FromDegreesCelsius(15d);

        Assert.Equal(295d, (initial + rise).DegreesCelsius, 12);
        Assert.Equal(265d, (initial - rise).DegreesCelsius, 12);
    }

    [Fact]
    public void TemperatureOperation_CannotCrossAbsoluteZero()
    {
        var temperature = Temperature.FromKelvins(5d);
        var drop = TemperatureDifference.FromKelvins(6d);

        Assert.Throws<ArgumentOutOfRangeException>(() => _ = temperature - drop);
    }

    [Fact]
    public void PressureDifference_CanMoveAbsolutePressureBothDirections()
    {
        var initial = Pressure.FromBar(70d);
        var change = PressureDifference.FromBar(2.5d);

        Assert.Equal(72.5d, (initial + change).Bar, 12);
        Assert.Equal(67.5d, (initial - change).Bar, 12);
    }

    [Fact]
    public void PressureDifference_IsSignedWhenSubtractingAbsolutePressures()
    {
        var upstream = Pressure.FromBar(65d);
        var downstream = Pressure.FromBar(70d);

        var difference = upstream - downstream;

        Assert.Equal(-5d, difference.Bar, 12);
    }

    [Fact]
    public void PressureOperation_CannotCrossVacuum()
    {
        var pressure = Pressure.FromBar(1d);
        var drop = PressureDifference.FromBar(2d);

        Assert.Throws<ArgumentOutOfRangeException>(() => _ = pressure - drop);
    }
}
