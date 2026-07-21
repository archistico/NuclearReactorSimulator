using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.TurbineIsland.Turbine;
using Xunit;

namespace NuclearReactorSimulator.Domain.Tests.Physics.TurbineIsland.Turbine;

public sealed class TurbineMechanicalQuantityTests
{
    [Fact]
    public void AngularSpeed_RoundTripsRevolutionsPerMinute()
    {
        var speed = AngularSpeed.FromRevolutionsPerMinute(3_000d);

        Assert.Equal(3_000d, speed.RevolutionsPerMinute, 10);
        Assert.Equal(100d * Math.PI, speed.RadiansPerSecond, 10);
    }

    [Fact]
    public void MomentOfInertia_ComputesRotationalKineticEnergy()
    {
        var inertia = MomentOfInertia.FromKilogramSquareMetres(10d);
        var speed = AngularSpeed.FromRadiansPerSecond(20d);

        Assert.Equal(2_000d, inertia.KineticEnergyAt(speed).Joules, 12);
    }

    [Fact]
    public void TurbineEfficiency_RejectsOutOfRangeValues()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => TurbineEfficiency.FromPercent(0d));
        Assert.Throws<ArgumentOutOfRangeException>(() => TurbineEfficiency.FromPercent(101d));
    }
}
