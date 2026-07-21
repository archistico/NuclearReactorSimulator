using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.Thermal;
using NuclearReactorSimulator.Simulation.Physics.Thermal;
using Xunit;

namespace NuclearReactorSimulator.Simulation.Tests.Physics.Thermal;

public sealed class HeatSourceSolverTests
{
    [Fact]
    public void EnabledSource_AppliesRatedThermalPower()
    {
        var source = new HeatSourceDefinition("heater", "wall", Power.FromMegawatts(5d));

        var result = new HeatSourceSolver().Solve(
            source,
            new HeatSourceState(source.Id, true));

        Assert.Equal(5d, result.NetHeatRate.Megawatts, 12);
    }

    [Fact]
    public void DisabledSource_AppliesZeroThermalPower()
    {
        var source = new HeatSourceDefinition("heater", "wall", Power.FromMegawatts(5d));

        var result = new HeatSourceSolver().Solve(
            source,
            new HeatSourceState(source.Id, false));

        Assert.Equal(ThermalEnergyBalance.Zero, result);
    }

    [Fact]
    public void StateForDifferentSource_IsRejected()
    {
        var source = new HeatSourceDefinition("heater", "wall", Power.FromMegawatts(5d));

        Assert.Throws<ArgumentException>(() => new HeatSourceSolver().Solve(
            source,
            new HeatSourceState("other")));
    }
}
