using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.TurbineIsland.MainSteam;
using Xunit;

namespace NuclearReactorSimulator.Domain.Tests.Physics.TurbineIsland.MainSteam;

public sealed class MainSteamReliefBoundaryDefinitionTests
{
    [Fact]
    public void Definition_PublishesExplicitExternalReceiverAndLinearPressureLiftContract()
    {
        var definition = CreateDefinition();

        Assert.Equal("header-relief", definition.Id);
        Assert.Equal("header", definition.SourceHeaderNodeId);
        Assert.Equal("atmospheric-relief-receiver", definition.ReceiverBoundaryId);
        Assert.Equal(Pressure.StandardAtmosphere, definition.ReceiverPressure);
        Assert.Equal(6.5d, definition.SetPressure.Megapascals, 12);
        Assert.Equal(6.7d, definition.FullLiftPressure.Megapascals, 12);
        Assert.Equal(1_600d, definition.FlowDefinition.FullOpenThroatArea.SquareMillimetres, 12);
        Assert.Equal(0d, definition.CalculateLiftFraction(Pressure.FromMegapascals(6.5d)), 12);
        Assert.Equal(0.5d, definition.CalculateLiftFraction(Pressure.FromMegapascals(6.6d)), 12);
        Assert.Equal(1d, definition.CalculateLiftFraction(Pressure.FromMegapascals(6.7d)), 12);
        Assert.Equal(1d, definition.CalculateLiftFraction(Pressure.FromMegapascals(7d)), 12);
    }

    [Fact]
    public void Definition_RejectsInvalidIdsAndPressureOrdering()
    {
        var flow = CreateFlowDefinition();

        Assert.Throws<ArgumentException>(() => new MainSteamReliefBoundaryDefinition(
            " ", "header", "receiver", Pressure.StandardAtmosphere,
            Pressure.FromMegapascals(6.5d), Pressure.FromMegapascals(6.7d), flow));
        Assert.Throws<ArgumentException>(() => new MainSteamReliefBoundaryDefinition(
            "relief", "header", "receiver", Pressure.FromMegapascals(6.5d),
            Pressure.FromMegapascals(6.5d), Pressure.FromMegapascals(6.7d), flow));
        Assert.Throws<ArgumentException>(() => new MainSteamReliefBoundaryDefinition(
            "relief", "header", "receiver", Pressure.StandardAtmosphere,
            Pressure.FromMegapascals(6.5d), Pressure.FromMegapascals(6.5d), flow));
    }

    private static MainSteamReliefBoundaryDefinition CreateDefinition()
        => new(
            "header-relief",
            "header",
            "atmospheric-relief-receiver",
            Pressure.StandardAtmosphere,
            Pressure.FromMegapascals(6.5d),
            Pressure.FromMegapascals(6.7d),
            CreateFlowDefinition());

    private static CompressibleSteamFlowDefinition CreateFlowDefinition()
        => new(
            Area.FromSquareMillimetres(1_600d),
            dischargeCoefficient: 0.95d,
            specificGasConstant: SpecificGasConstant.FromJoulesPerKilogramKelvin(461.526d),
            heatCapacityRatio: 1.3d);
}
