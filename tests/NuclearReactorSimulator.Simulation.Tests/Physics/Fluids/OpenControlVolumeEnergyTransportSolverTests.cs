using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Simulation.Physics.Fluids;
using Xunit;

namespace NuclearReactorSimulator.Simulation.Tests.Physics.Fluids;

public sealed class OpenControlVolumeEnergyTransportSolverTests
{
    [Fact]
    public void Solve_ResolvesSpecificEnthalpyAsInternalEnergyPlusPressureOverDensity()
    {
        var solver = new OpenControlVolumeEnergyTransportSolver();
        var from = CreateNode("from", massKilograms: 10d, volumeCubicMetres: 2d, pressureMegapascals: 5d, specificInternalEnergyMegajoulesPerKilogram: 2.5d);
        var to = CreateNode("to", massKilograms: 100d, volumeCubicMetres: 1d, pressureMegapascals: 1d, specificInternalEnergyMegajoulesPerKilogram: 1d);

        var result = solver.Solve(from, to, MassFlowRate.FromKilogramsPerSecond(2d));

        Assert.Equal(5d, result.UpstreamDensity.KilogramsPerCubicMetre, 12);
        Assert.Equal(1_000_000d, result.UpstreamSpecificFlowWork.JoulesPerKilogram, 6);
        Assert.Equal(3_500_000d, result.UpstreamSpecificEnthalpy.JoulesPerKilogram, 6);
        Assert.Equal(5d, result.SignedInternalEnergyAdvectionRate.Megawatts, 12);
        Assert.Equal(2d, result.SignedFlowWorkRate.Megawatts, 12);
        Assert.Equal(7d, result.SignedEnthalpyTransportRate.Megawatts, 12);
    }

    [Fact]
    public void Solve_UsesTheActualUpstreamStateForReverseReferenceFlow()
    {
        var solver = new OpenControlVolumeEnergyTransportSolver();
        var from = CreateNode("from", 100d, 1d, 1d, 1d);
        var to = CreateNode("to", 10d, 2d, 5d, 2.5d);

        var result = solver.Solve(from, to, MassFlowRate.FromKilogramsPerSecond(-2d));

        Assert.Equal("to", result.UpstreamNodeId);
        Assert.Equal("from", result.DownstreamNodeId);
        Assert.Equal(-5d, result.SignedInternalEnergyAdvectionRate.Megawatts, 12);
        Assert.Equal(-2d, result.SignedFlowWorkRate.Megawatts, 12);
        Assert.Equal(-7d, result.SignedEnthalpyTransportRate.Megawatts, 12);
    }

    [Fact]
    public void Solve_ProducesEqualAndOppositeBalancesForBothConventions()
    {
        var solver = new OpenControlVolumeEnergyTransportSolver();
        var from = CreateNode("from", 10d, 2d, 5d, 2.5d);
        var to = CreateNode("to", 100d, 1d, 1d, 1d);

        var result = solver.Solve(from, to, MassFlowRate.FromKilogramsPerSecond(2d));

        Assert.Equal(0d, result.LegacyFromNodeBalance.NetMassFlowRate.KilogramsPerSecond + result.LegacyToNodeBalance.NetMassFlowRate.KilogramsPerSecond, 12);
        Assert.Equal(0d, result.LegacyFromNodeBalance.NetEnergyRate.Watts + result.LegacyToNodeBalance.NetEnergyRate.Watts, 6);
        Assert.Equal(0d, result.EnthalpyFromNodeBalance.NetMassFlowRate.KilogramsPerSecond + result.EnthalpyToNodeBalance.NetMassFlowRate.KilogramsPerSecond, 12);
        Assert.Equal(0d, result.EnthalpyFromNodeBalance.NetEnergyRate.Watts + result.EnthalpyToNodeBalance.NetEnergyRate.Watts, 6);
    }

    [Fact]
    public void Solve_EnthalpyMinusLegacyTransportEqualsExplicitFlowWorkExactly()
    {
        var solver = new OpenControlVolumeEnergyTransportSolver();
        var from = CreateNode("from", 10d, 2d, 5d, 2.5d);
        var to = CreateNode("to", 100d, 1d, 1d, 1d);

        var result = solver.Solve(from, to, MassFlowRate.FromKilogramsPerSecond(2d));

        Assert.Equal(
            result.SignedFlowWorkRate.Watts,
            result.SignedEnthalpyTransportRate.Watts - result.SignedInternalEnergyAdvectionRate.Watts,
            6);
    }

    [Fact]
    public void Solve_ZeroFlowPublishesZeroPowerWithoutInventingBoundaryWork()
    {
        var solver = new OpenControlVolumeEnergyTransportSolver();
        var from = CreateNode("from", 10d, 2d, 5d, 2.5d);
        var to = CreateNode("to", 100d, 1d, 1d, 1d);

        var result = solver.Solve(from, to, MassFlowRate.Zero);

        Assert.Equal(Power.Zero, result.SignedInternalEnergyAdvectionRate);
        Assert.Equal(Power.Zero, result.SignedFlowWorkRate);
        Assert.Equal(Power.Zero, result.SignedEnthalpyTransportRate);
        Assert.Equal(FluidNodeBalance.Zero, result.LegacyFromNodeBalance);
        Assert.Equal(FluidNodeBalance.Zero, result.LegacyToNodeBalance);
        Assert.Equal(FluidNodeBalance.Zero, result.EnthalpyFromNodeBalance);
        Assert.Equal(FluidNodeBalance.Zero, result.EnthalpyToNodeBalance);
    }

    [Fact]
    public void Solve_LowDensitySteamCarriesLargerFlowWorkGapThanDenseLiquidAtTheSamePressure()
    {
        var solver = new OpenControlVolumeEnergyTransportSolver();
        var steam = CreateNode("steam", 10d, 2d, 5d, 2.5d);
        var liquid = CreateNode("liquid", 1_000d, 1d, 5d, 1d);
        var sink = CreateNode("sink", 100d, 1d, 1d, 1d);

        var steamResult = solver.Solve(steam, sink, MassFlowRate.FromKilogramsPerSecond(1d));
        var liquidResult = solver.Solve(liquid, sink, MassFlowRate.FromKilogramsPerSecond(1d));

        Assert.True(steamResult.UpstreamSpecificFlowWork > liquidResult.UpstreamSpecificFlowWork);
        Assert.Equal(200d, steamResult.UpstreamSpecificFlowWork.JoulesPerKilogram / liquidResult.UpstreamSpecificFlowWork.JoulesPerKilogram, 9);
    }

    [Fact]
    public void Solve_RejectsIdenticalControlVolumeIdentity()
    {
        var solver = new OpenControlVolumeEnergyTransportSolver();
        var first = CreateNode("same", 10d, 2d, 5d, 2.5d);
        var second = CreateNode("same", 100d, 1d, 1d, 1d);

        Assert.Throws<ArgumentException>(() => solver.Solve(
            first,
            second,
            MassFlowRate.FromKilogramsPerSecond(1d)));
    }

    [Fact]
    public void Solve_IsDeterministicForTheSameCommittedInputs()
    {
        var solver = new OpenControlVolumeEnergyTransportSolver();
        var from = CreateNode("from", 10d, 2d, 5d, 2.5d);
        var to = CreateNode("to", 100d, 1d, 1d, 1d);
        var flow = MassFlowRate.FromKilogramsPerSecond(2d);

        var first = solver.Solve(from, to, flow);
        var second = solver.Solve(from, to, flow);

        Assert.Equal(first, second);
    }

    private static FluidNodeState CreateNode(
        string id,
        double massKilograms,
        double volumeCubicMetres,
        double pressureMegapascals,
        double specificInternalEnergyMegajoulesPerKilogram)
    {
        var mass = Mass.FromKilograms(massKilograms);
        var specificInternalEnergy = SpecificEnergy.FromJoulesPerKilogram(
            specificInternalEnergyMegajoulesPerKilogram * 1_000_000d);
        var inventory = new FluidNodeInventory(mass, specificInternalEnergy * mass);
        var definition = new FluidNodeDefinition(id, Volume.FromCubicMetres(volumeCubicMetres));
        var thermodynamics = new FluidThermodynamicState(
            Pressure.FromMegapascals(pressureMegapascals),
            Temperature.FromDegreesCelsius(250d));
        return new FluidNodeState(definition, inventory, thermodynamics);
    }
}
