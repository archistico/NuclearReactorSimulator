using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Simulation.Physics.Fluids;
using Xunit;

namespace NuclearReactorSimulator.Simulation.Tests.Physics.Fluids;

public sealed class PipeFlowSolverTests
{
    private static readonly QuadraticHydraulicResistance Resistance =
        QuadraticHydraulicResistance.FromPascalSecondsSquaredPerKilogramSquared(100_000d);

    [Fact]
    public void HigherFromPressure_ProducesPositiveReferenceFlow()
    {
        var result = Solve(
            CreateNode("from", 5.4d, 1_000d, 100d),
            CreateNode("to", 5.0d, 800d, 120d));

        Assert.Equal(0.4d, result.PressureDifference.Megapascals, 12);
        Assert.Equal(2d, result.MassFlowRate.KilogramsPerSecond, 12);
    }

    [Fact]
    public void HigherToPressure_ReversesFlow()
    {
        var result = Solve(
            CreateNode("from", 5.0d, 1_000d, 100d),
            CreateNode("to", 5.4d, 800d, 120d));

        Assert.Equal(-0.4d, result.PressureDifference.Megapascals, 12);
        Assert.Equal(-2d, result.MassFlowRate.KilogramsPerSecond, 12);
    }

    [Fact]
    public void HigherResistance_ReducesMassFlowMagnitude()
    {
        var from = CreateNode("from", 5.4d, 1_000d, 100d);
        var to = CreateNode("to", 5.0d, 800d, 120d);
        var solver = new PipeFlowSolver();
        var lowResistance = new PipeDefinition(
            "low-resistance",
            "from",
            "to",
            QuadraticHydraulicResistance.FromPascalSecondsSquaredPerKilogramSquared(100_000d));
        var highResistance = new PipeDefinition(
            "high-resistance",
            "from",
            "to",
            QuadraticHydraulicResistance.FromPascalSecondsSquaredPerKilogramSquared(400_000d));

        var lowResistanceFlow = solver.Solve(lowResistance, from, to);
        var highResistanceFlow = solver.Solve(highResistance, from, to);

        Assert.Equal(2d, lowResistanceFlow.MassFlowRate.KilogramsPerSecond, 12);
        Assert.Equal(1d, highResistanceFlow.MassFlowRate.KilogramsPerSecond, 12);
    }

    [Fact]
    public void EqualPressure_ProducesNoTransfer()
    {
        var result = Solve(
            CreateNode("from", 5.0d, 1_000d, 100d),
            CreateNode("to", 5.0d, 800d, 120d));

        Assert.Equal(PressureDifference.Zero, result.PressureDifference);
        Assert.Equal(MassFlowRate.Zero, result.MassFlowRate);
        Assert.Equal(Power.Zero, result.InternalEnergyFlowRate);
        Assert.Equal(FluidNodeBalance.Zero, result.FromNodeBalance);
        Assert.Equal(FluidNodeBalance.Zero, result.ToNodeBalance);
    }

    [Fact]
    public void ForwardFlow_AdvectsFromNodeSpecificInternalEnergy()
    {
        var result = Solve(
            CreateNode("from", 5.4d, 1_000d, 100d),
            CreateNode("to", 5.0d, 800d, 120d));

        Assert.Equal(0.2d, result.InternalEnergyFlowRate.Megawatts, 12);
    }

    [Fact]
    public void ReverseFlow_AdvectsToNodeSpecificInternalEnergy()
    {
        var result = Solve(
            CreateNode("from", 5.0d, 1_000d, 100d),
            CreateNode("to", 5.4d, 800d, 120d));

        Assert.Equal(-0.3d, result.InternalEnergyFlowRate.Megawatts, 12);
    }

    [Fact]
    public void EndpointBalances_AreExactlyConservative()
    {
        var result = Solve(
            CreateNode("from", 5.4d, 1_000d, 100d),
            CreateNode("to", 5.0d, 800d, 120d));

        var total = result.FromNodeBalance + result.ToNodeBalance;

        Assert.Equal(MassFlowRate.Zero, total.NetMassFlowRate);
        Assert.Equal(Power.Zero, total.NetEnergyRate);
        Assert.Equal(-result.MassFlowRate, result.FromNodeBalance.NetMassFlowRate);
        Assert.Equal(result.MassFlowRate, result.ToNodeBalance.NetMassFlowRate);
    }

    [Fact]
    public void EndpointMismatch_IsRejected()
    {
        var solver = new PipeFlowSolver();
        var pipe = new PipeDefinition("pipe", "from", "to", Resistance);
        var wrongFrom = CreateNode("wrong", 5.4d, 1_000d, 100d);
        var to = CreateNode("to", 5.0d, 800d, 120d);

        Assert.Throws<ArgumentException>(() => solver.Solve(pipe, wrongFrom, to));
    }

    [Fact]
    public void SameInputs_ProduceSameFlowResult()
    {
        var solver = new PipeFlowSolver();
        var pipe = new PipeDefinition("pipe", "from", "to", Resistance);
        var from = CreateNode("from", 5.4d, 1_000d, 100d);
        var to = CreateNode("to", 5.0d, 800d, 120d);

        var left = solver.Solve(pipe, from, to);
        var right = solver.Solve(pipe, from, to);

        Assert.Equal(left, right);
    }

    private static PipeFlowResult Solve(FluidNodeState from, FluidNodeState to)
    {
        return new PipeFlowSolver().Solve(
            new PipeDefinition("pipe", "from", "to", Resistance),
            from,
            to);
    }

    private static FluidNodeState CreateNode(
        string id,
        double pressureMegapascals,
        double massKilograms,
        double internalEnergyMegajoules)
    {
        return new FluidNodeState(
            new FluidNodeDefinition(id, Volume.FromCubicMetres(2d)),
            new FluidNodeInventory(
                Mass.FromKilograms(massKilograms),
                Energy.FromMegajoules(internalEnergyMegajoules)),
            new FluidThermodynamicState(
                Pressure.FromMegapascals(pressureMegapascals),
                Temperature.FromDegreesCelsius(250d)));
    }
}
