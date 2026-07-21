using NuclearReactorSimulator.Domain.Physics.Electrical;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using Xunit;

namespace NuclearReactorSimulator.Domain.Tests.Physics.Electrical;

public sealed class ElectricalQuantityTests
{
    [Fact]
    public void FrequencyAndVoltage_PreserveCanonicalUnits()
    {
        Assert.Equal(50d, Frequency.FromHertz(50d).Hertz, 12);
        Assert.Equal(400d, ElectricPotential.FromKilovolts(400d).Kilovolts, 12);
    }

    [Fact]
    public void PhaseAngle_NormalizesAndComputesShortestDifferenceAcrossZero()
    {
        var left = PhaseAngle.FromDegrees(355d);
        var right = PhaseAngle.FromDegrees(5d);

        Assert.Equal(355d, left.Degrees, 10);
        Assert.Equal(10d, left.ShortestDifference(right).Degrees, 10);
        Assert.Equal(5d, PhaseAngle.FromDegrees(365d).Degrees, 10);
    }

    [Fact]
    public void SynchronousGeneratorDefinition_RejectsDefaultInvalidEfficiency()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new SynchronousGeneratorDefinition(
            "generator",
            "rotor",
            "breaker",
            polePairs: 1,
            ElectricPotential.FromKilovolts(400d),
            Power.FromMegawatts(1_000d),
            default,
            Frequency.FromHertz(0.2d),
            PhaseAngleDifference.FromDegrees(10d),
            ElectricPotential.FromKilovolts(10d)));
    }
}
