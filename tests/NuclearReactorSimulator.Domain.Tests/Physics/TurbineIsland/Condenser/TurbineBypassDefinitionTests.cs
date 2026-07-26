using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.TurbineIsland.Condenser;
using NuclearReactorSimulator.Domain.Physics.TurbineIsland.MainSteam;
using Xunit;

namespace NuclearReactorSimulator.Domain.Tests.Physics.TurbineIsland.Condenser;

public sealed class TurbineBypassDefinitionTests
{
    [Fact]
    public void Definition_TrimsIdsAndCalculatesLinearPressureOpening()
    {
        var definition = CreateDefinition(" bypass ", " header ", " condenser ");

        Assert.Equal("bypass", definition.Id);
        Assert.Equal("header", definition.SourceHeaderNodeId);
        Assert.Equal("condenser", definition.CondenserId);
        Assert.Equal(0d, definition.CalculateOpenFraction(Pressure.FromMegapascals(6.4d)), 12);
        Assert.Equal(0.5d, definition.CalculateOpenFraction(Pressure.FromMegapascals(6.45d)), 12);
        Assert.Equal(1d, definition.CalculateOpenFraction(Pressure.FromMegapascals(6.5d)), 12);
    }

    [Fact]
    public void Definition_RejectsNonIncreasingPressureWindow()
    {
        Assert.Throws<ArgumentException>(() => new TurbineBypassDefinition(
            "bypass",
            "header",
            "condenser",
            Pressure.FromMegapascals(6.5d),
            Pressure.FromMegapascals(6.5d),
            CreateFlowDefinition()));
    }

    [Fact]
    public void Definition_RejectsEmptySemanticIds()
    {
        Assert.Throws<ArgumentException>(() => CreateDefinition(" ", "header", "condenser"));
        Assert.Throws<ArgumentException>(() => CreateDefinition("bypass", " ", "condenser"));
        Assert.Throws<ArgumentException>(() => CreateDefinition("bypass", "header", " "));
    }

    private static TurbineBypassDefinition CreateDefinition(string id, string source, string condenser)
        => new(
            id,
            source,
            condenser,
            Pressure.FromMegapascals(6.4d),
            Pressure.FromMegapascals(6.5d),
            CreateFlowDefinition());

    private static CompressibleSteamFlowDefinition CreateFlowDefinition()
        => new(
            Area.FromSquareMillimetres(1_600d),
            dischargeCoefficient: 0.95d,
            specificGasConstant: SpecificGasConstant.FromJoulesPerKilogramKelvin(461.526d),
            heatCapacityRatio: 1.3d);
}
