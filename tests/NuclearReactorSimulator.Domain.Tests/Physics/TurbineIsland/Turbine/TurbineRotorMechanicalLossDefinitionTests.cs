using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.TurbineIsland.Turbine;
using Xunit;

namespace NuclearReactorSimulator.Domain.Tests.Physics.TurbineIsland.Turbine;

public sealed class TurbineRotorMechanicalLossDefinitionTests
{
    [Fact]
    public void Constructor_RejectsNonPositiveRatedSpeedLossPower()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new TurbineRotorMechanicalLossDefinition(Power.Zero));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TurbineRotorMechanicalLossDefinition(Power.FromWatts(-1d)));
    }

    [Fact]
    public void ResolveTorque_RejectsNonPositiveRatedSpeed()
    {
        var definition = new TurbineRotorMechanicalLossDefinition(Power.FromMegawatts(0.5d));

        Assert.Throws<ArgumentOutOfRangeException>(() => definition.ResolveTorque(
            AngularSpeed.FromRevolutionsPerMinute(3_000d),
            AngularSpeed.Zero));
    }

    [Fact]
    public void ResolveTorque_IsZeroAtRestAndLinearWithSpeed()
    {
        var definition = new TurbineRotorMechanicalLossDefinition(Power.FromMegawatts(0.5d));
        var rated = AngularSpeed.FromRevolutionsPerMinute(3_000d);

        var stopped = definition.ResolveTorque(AngularSpeed.Zero, rated);
        var half = definition.ResolveTorque(AngularSpeed.FromRevolutionsPerMinute(1_500d), rated);
        var full = definition.ResolveTorque(rated, rated);

        Assert.Equal(Torque.Zero, stopped);
        Assert.Equal(0.5d * full.NewtonMetres, half.NewtonMetres, 9);
        Assert.Equal(0.5d, full.At(rated).Megawatts, 9);
    }
}
