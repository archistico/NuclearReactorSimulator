using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.TurbineIsland.MainSteam;
using Xunit;

namespace NuclearReactorSimulator.Domain.Tests.Physics.TurbineIsland.MainSteam;

public sealed class CompressibleSteamFlowDefinitionTests
{
    [Fact]
    public void Definition_PublishesValidatedIdealVaporNozzleParameters()
    {
        var definition = CreateDefinition();

        Assert.Equal(100d, definition.FullOpenThroatArea.SquareMillimetres, 12);
        Assert.Equal(0.95d, definition.DischargeCoefficient, 12);
        Assert.Equal(461.526d, definition.SpecificGasConstant.JoulesPerKilogramKelvin, 12);
        Assert.Equal(1.3d, definition.HeatCapacityRatio, 12);
        Assert.Equal(0.545727733814065d, definition.CriticalDownstreamToUpstreamPressureRatio, 12);
    }

    [Fact]
    public void Definition_RejectsInvalidAreaCoefficientGasConstantAndHeatCapacityRatio()
    {
        var area = Area.FromSquareMillimetres(100d);
        var gasConstant = SpecificGasConstant.FromJoulesPerKilogramKelvin(461.526d);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new CompressibleSteamFlowDefinition(Area.Zero, 0.95d, gasConstant, 1.3d));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new CompressibleSteamFlowDefinition(area, 0d, gasConstant, 1.3d));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new CompressibleSteamFlowDefinition(area, 1.01d, gasConstant, 1.3d));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new CompressibleSteamFlowDefinition(area, 0.95d, SpecificGasConstant.Zero, 1.3d));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new CompressibleSteamFlowDefinition(area, 0.95d, gasConstant, 1d));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new CompressibleSteamFlowDefinition(area, 0.95d, gasConstant, 2.01d));
    }

    [Fact]
    public void CriticalPressureRatio_RemainsCanonicalForImmutableDefinition()
    {
        var definition = CreateDefinition();
        var expected = Math.Pow(
            2d / (definition.HeatCapacityRatio + 1d),
            definition.HeatCapacityRatio / (definition.HeatCapacityRatio - 1d));

        Assert.Equal(expected, definition.CriticalDownstreamToUpstreamPressureRatio, 15);
        Assert.Equal(
            definition.CriticalDownstreamToUpstreamPressureRatio,
            definition.CriticalDownstreamToUpstreamPressureRatio);
    }

    private static CompressibleSteamFlowDefinition CreateDefinition()
        => new(
            Area.FromSquareMillimetres(100d),
            dischargeCoefficient: 0.95d,
            specificGasConstant: SpecificGasConstant.FromJoulesPerKilogramKelvin(461.526d),
            heatCapacityRatio: 1.3d);
}
