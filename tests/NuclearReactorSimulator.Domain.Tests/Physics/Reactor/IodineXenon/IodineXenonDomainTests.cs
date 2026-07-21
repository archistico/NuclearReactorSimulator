using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.Reactor.IodineXenon;
using Xunit;

namespace NuclearReactorSimulator.Domain.Tests.Physics.Reactor.IodineXenon;

public sealed class IodineXenonDomainTests
{
    [Fact]
    public void Inventories_RejectNegativeAndNonFiniteValues()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => IodineInventory.FromRelative(-0.1d));
        Assert.Throws<ArgumentOutOfRangeException>(() => XenonInventory.FromRelative(double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => XenonInventory.FromRelative(double.PositiveInfinity));
    }

    [Fact]
    public void ProductionAndBurnupRates_RejectInvalidValues()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => PoisonProductionRate.FromRelativePerSecond(-1d));
        Assert.Throws<ArgumentOutOfRangeException>(() => XenonBurnupCoefficient.FromPerSecondPerRelativeNeutronPopulation(-1d));
        Assert.Throws<ArgumentOutOfRangeException>(() => PoisonProductionRate.FromRelativePerSecond(double.NaN));
    }

    [Fact]
    public void XenonReactivityCoefficient_IsSignedAndComposable()
    {
        var coefficient = XenonReactivityCoefficient.FromPcmPerRelativeInventory(-1_000d);
        var inventory = XenonInventory.FromRelative(0.25d);

        var reactivity = coefficient * inventory;

        Assert.Equal(-250d, reactivity.Pcm, 12);
    }

    [Fact]
    public void Definition_RequiresPositiveReferencePower()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new IodineXenonDefinition(
            "core",
            Power.Zero,
            PoisonProductionRate.Zero,
            PoisonProductionRate.Zero,
            DecayConstant.FromPerSecond(0.1d),
            DecayConstant.FromPerSecond(0.01d),
            XenonBurnupCoefficient.Zero,
            XenonReactivityCoefficient.Zero));
    }

    [Fact]
    public void EquilibriumState_BalancesConfiguredSourcesAndRemoval()
    {
        var definition = Definition();
        var state = IodineXenonState.CreateEquilibrium(
            definition,
            Power.FromMegawatts(1_000d),
            NeutronPopulation.Reference);

        Assert.Equal(0.1d, state.Iodine.Relative, 12);
        Assert.Equal(0.05d, state.Xenon.Relative, 12);
    }

    [Fact]
    public void EmptyState_HasZeroInventories()
    {
        Assert.Equal(0d, IodineXenonState.Empty.Iodine.Relative);
        Assert.Equal(0d, IodineXenonState.Empty.Xenon.Relative);
    }

    private static IodineXenonDefinition Definition()
        => new(
            "core",
            Power.FromMegawatts(1_000d),
            PoisonProductionRate.FromRelativePerSecond(0.01d),
            PoisonProductionRate.FromRelativePerSecond(0.0005d),
            DecayConstant.FromPerSecond(0.1d),
            DecayConstant.FromPerSecond(0.01d),
            XenonBurnupCoefficient.FromPerSecondPerRelativeNeutronPopulation(0.2d),
            XenonReactivityCoefficient.FromPcmPerRelativeInventory(-1_000d));
}
