using NuclearReactorSimulator.Domain.Physics.Control.TurbineSecondary;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using Xunit;

namespace NuclearReactorSimulator.Domain.Tests.Physics.Control.TurbineSecondary;

public sealed class TurbineGovernorDroopDefinitionTests
{
    [Fact]
    public void Constructor_PreservesTypedFivePercentDroopReferenceRise()
    {
        var definition = new TurbineGovernorDroopDefinition(
            "speed-control",
            "generator",
            AngularSpeed.FromRevolutionsPerMinute(150d));

        Assert.Equal("speed-control", definition.SpeedControllerId);
        Assert.Equal("generator", definition.GeneratorId);
        Assert.Equal(150d, definition.FullLoadSpeedReferenceRise.RevolutionsPerMinute, 12);
    }

    [Fact]
    public void Constructor_RejectsZeroReferenceRise()
        => Assert.Throws<ArgumentOutOfRangeException>(() => new TurbineGovernorDroopDefinition(
            "speed-control",
            "generator",
            AngularSpeed.Zero));
}
