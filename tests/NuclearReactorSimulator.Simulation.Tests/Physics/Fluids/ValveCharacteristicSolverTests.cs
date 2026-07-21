using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Simulation.Physics.Fluids;
using Xunit;

namespace NuclearReactorSimulator.Simulation.Tests.Physics.Fluids;

public sealed class ValveCharacteristicSolverTests
{
    private readonly ValveCharacteristicSolver _solver = new();

    [Fact]
    public void ClosedAndOpenEndpoints_AreExactForEveryCharacteristic()
    {
        foreach (var characteristic in new[]
        {
            ValveCharacteristic.Linear,
            ValveCharacteristic.QuickOpening,
            ValveCharacteristic.EqualPercentage(50d),
        })
        {
            Assert.Equal(ValveFlowCoefficient.Closed, _solver.Evaluate(characteristic, ValvePosition.Closed));
            Assert.Equal(ValveFlowCoefficient.FullyOpen, _solver.Evaluate(characteristic, ValvePosition.FullyOpen));
        }
    }

    [Fact]
    public void LinearCharacteristic_MapsPositionDirectlyToCapacity()
    {
        var coefficient = _solver.Evaluate(
            ValveCharacteristic.Linear,
            ValvePosition.FromPercent(25d));

        Assert.Equal(0.25d, coefficient.Fraction, 12);
    }

    [Fact]
    public void QuickOpeningCharacteristic_ProvidesMoreEarlyCapacityThanLinear()
    {
        var position = ValvePosition.FromPercent(25d);
        var linear = _solver.Evaluate(ValveCharacteristic.Linear, position);
        var quick = _solver.Evaluate(ValveCharacteristic.QuickOpening, position);

        Assert.Equal(0.5d, quick.Fraction, 12);
        Assert.True(quick.Fraction > linear.Fraction);
    }

    [Fact]
    public void EqualPercentageCharacteristic_ProvidesLessEarlyCapacityThanLinear()
    {
        var position = ValvePosition.FromPercent(50d);
        var linear = _solver.Evaluate(ValveCharacteristic.Linear, position);
        var equalPercentage = _solver.Evaluate(ValveCharacteristic.EqualPercentage(50d), position);

        Assert.True(equalPercentage.Fraction > 0d);
        Assert.True(equalPercentage.Fraction < linear.Fraction);
    }

    [Theory]
    [InlineData(0.1d, 0.2d)]
    [InlineData(0.2d, 0.5d)]
    [InlineData(0.5d, 0.9d)]
    public void Characteristics_AreMonotonic(double lower, double upper)
    {
        foreach (var characteristic in new[]
        {
            ValveCharacteristic.Linear,
            ValveCharacteristic.QuickOpening,
            ValveCharacteristic.EqualPercentage(50d),
        })
        {
            var lowerCoefficient = _solver.Evaluate(characteristic, ValvePosition.FromFraction(lower));
            var upperCoefficient = _solver.Evaluate(characteristic, ValvePosition.FromFraction(upper));

            Assert.True(upperCoefficient > lowerCoefficient);
        }
    }
}
