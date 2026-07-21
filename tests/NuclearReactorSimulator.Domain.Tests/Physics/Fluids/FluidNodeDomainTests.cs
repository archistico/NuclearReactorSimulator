using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using Xunit;

namespace NuclearReactorSimulator.Domain.Tests.Physics.Fluids;

public sealed class FluidNodeDomainTests
{
    [Fact]
    public void Definition_RequiresIdentifierAndPositiveControlVolume()
    {
        Assert.Throws<ArgumentException>(() => new FluidNodeDefinition(" ", Volume.FromCubicMetres(1d)));
        Assert.Throws<ArgumentOutOfRangeException>(() => new FluidNodeDefinition("node", Volume.Zero));
    }

    [Fact]
    public void Definition_TrimsIdentifierAndPreservesVolume()
    {
        var definition = new FluidNodeDefinition("  core-inlet  ", Volume.FromCubicMetres(12.5d));

        Assert.Equal("core-inlet", definition.Id);
        Assert.Equal(12.5d, definition.Volume.CubicMetres, 12);
    }

    [Fact]
    public void Inventory_RequiresStrictlyPositiveMass()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new FluidNodeInventory(Mass.Zero, Energy.Zero));
    }

    [Fact]
    public void Inventory_DerivesSpecificInternalEnergyFromConservedValues()
    {
        var inventory = new FluidNodeInventory(
            Mass.FromKilograms(500d),
            Energy.FromMegajoules(25d));

        Assert.Equal(50d, inventory.SpecificInternalEnergy.KilojoulesPerKilogram, 12);
    }

    [Fact]
    public void ThermodynamicState_RequiresPositiveAbsolutePressureAndTemperature()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new FluidThermodynamicState(
            Pressure.Vacuum,
            Temperature.FromDegreesCelsius(20d)));

        Assert.Throws<ArgumentOutOfRangeException>(() => new FluidThermodynamicState(
            Pressure.StandardAtmosphere,
            Temperature.AbsoluteZero));
    }

    [Fact]
    public void NodeState_DerivesDensityWithoutDuplicatingStoredState()
    {
        var state = CreateState(
            volumeCubicMetres: 2d,
            massKilograms: 1_900d,
            energyMegajoules: 100d);

        Assert.Equal("test-node", state.Id);
        Assert.Equal(950d, state.Density.KilogramsPerCubicMetre, 12);
        Assert.Equal(100d / 1.9d, state.SpecificInternalEnergy.KilojoulesPerKilogram, 12);
        Assert.Equal(7d, state.Pressure.Megapascals, 12);
        Assert.Equal(280d, state.Temperature.DegreesCelsius, 10);
    }

    [Fact]
    public void EquivalentFluidNodeStates_HaveValueEquality()
    {
        var left = CreateState(2d, 1_900d, 100d);
        var right = CreateState(2d, 1_900d, 100d);

        Assert.Equal(left, right);
        Assert.NotSame(left, right);
    }

    private static FluidNodeState CreateState(
        double volumeCubicMetres,
        double massKilograms,
        double energyMegajoules)
    {
        return new FluidNodeState(
            new FluidNodeDefinition("test-node", Volume.FromCubicMetres(volumeCubicMetres)),
            new FluidNodeInventory(
                Mass.FromKilograms(massKilograms),
                Energy.FromMegajoules(energyMegajoules)),
            new FluidThermodynamicState(
                Pressure.FromMegapascals(7d),
                Temperature.FromDegreesCelsius(280d)));
    }
}
