using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Simulation.Physics.Fluids;
using Xunit;

namespace NuclearReactorSimulator.Simulation.Tests.Physics.Fluids;

public sealed class ValveFlowSolverTests
{
    [Fact]
    public void FullyClosedValve_BlocksFlowDespitePressureDifference()
    {
        var result = Solve(ValvePosition.Closed);

        Assert.Equal(0.4d, result.PressureDifference.Megapascals, 12);
        Assert.Equal(MassFlowRate.Zero, result.MassFlowRate);
        Assert.Equal(Power.Zero, result.InternalEnergyFlowRate);
        Assert.Equal(FluidNodeBalance.Zero, result.FromNodeBalance);
        Assert.Equal(FluidNodeBalance.Zero, result.ToNodeBalance);
        Assert.Equal(ValveFlowCoefficient.Closed, result.FlowCoefficient);
    }

    [Fact]
    public void FullyOpenValve_MatchesUnderlyingPipeFlow()
    {
        var valve = CreateValve(ValveCharacteristic.Linear, ValveFailSafeAction.FailClosed);
        var from = CreateNode("from", 5.4d, 1_000d, 100d);
        var to = CreateNode("to", 5.0d, 800d, 120d);
        var valveFlow = new ValveFlowSolver().Solve(
            valve,
            new ValveState(valve.Id, ValvePosition.FullyOpen),
            from,
            to);
        var pipeFlow = new PipeFlowSolver().Solve(valve.Pipe, from, to);

        Assert.Equal(pipeFlow, valveFlow.HydraulicFlow);
        Assert.Equal(ValveFlowCoefficient.FullyOpen, valveFlow.FlowCoefficient);
    }

    [Fact]
    public void LinearHalfOpenValve_ProducesHalfOfFullyOpenMassFlow()
    {
        var halfOpen = Solve(ValvePosition.FromPercent(50d));
        var fullyOpen = Solve(ValvePosition.FullyOpen);

        Assert.Equal(fullyOpen.MassFlowRate.KilogramsPerSecond / 2d, halfOpen.MassFlowRate.KilogramsPerSecond, 12);
    }

    [Fact]
    public void IncreasingLinearOpening_IncreasesFlowMonotonically()
    {
        var quarter = Solve(ValvePosition.FromPercent(25d));
        var half = Solve(ValvePosition.FromPercent(50d));
        var threeQuarter = Solve(ValvePosition.FromPercent(75d));

        Assert.True(quarter.MassFlowRate < half.MassFlowRate);
        Assert.True(half.MassFlowRate < threeQuarter.MassFlowRate);
    }

    [Fact]
    public void PressureReversal_ReversesValveFlowAndUpstreamEnergyAdvection()
    {
        var valve = CreateValve(ValveCharacteristic.Linear, ValveFailSafeAction.HoldLastPosition);
        var result = new ValveFlowSolver().Solve(
            valve,
            new ValveState(valve.Id, ValvePosition.FullyOpen),
            CreateNode("from", 5.0d, 1_000d, 100d),
            CreateNode("to", 5.4d, 800d, 120d));

        Assert.Equal(-2d, result.MassFlowRate.KilogramsPerSecond, 12);
        Assert.Equal(-0.3d, result.InternalEnergyFlowRate.Megawatts, 12);
    }

    [Fact]
    public void EndpointBalances_RemainExactlyConservative()
    {
        var result = Solve(ValvePosition.FromPercent(37d));
        var total = result.FromNodeBalance + result.ToNodeBalance;

        Assert.Equal(MassFlowRate.Zero, total.NetMassFlowRate);
        Assert.Equal(Power.Zero, total.NetEnergyRate);
    }

    [Fact]
    public void FailClosed_OverridesLastPosition()
    {
        var valve = CreateValve(ValveCharacteristic.Linear, ValveFailSafeAction.FailClosed);
        var result = Solve(
            valve,
            new ValveState(valve.Id, ValvePosition.FullyOpen, true));

        Assert.Equal(ValvePosition.Closed, result.EffectivePosition);
        Assert.Equal(MassFlowRate.Zero, result.MassFlowRate);
    }

    [Fact]
    public void FailOpen_OverridesLastPosition()
    {
        var valve = CreateValve(ValveCharacteristic.Linear, ValveFailSafeAction.FailOpen);
        var result = Solve(
            valve,
            new ValveState(valve.Id, ValvePosition.FromPercent(10d), true));

        Assert.Equal(ValvePosition.FullyOpen, result.EffectivePosition);
        Assert.Equal(2d, result.MassFlowRate.KilogramsPerSecond, 12);
    }

    [Fact]
    public void HoldLastPosition_PreservesMechanicalPositionDuringFailSafe()
    {
        var valve = CreateValve(ValveCharacteristic.Linear, ValveFailSafeAction.HoldLastPosition);
        var result = Solve(
            valve,
            new ValveState(valve.Id, ValvePosition.FromPercent(40d), true));

        Assert.Equal(0.4d, result.EffectivePosition.Fraction, 12);
        Assert.Equal(0.8d, result.MassFlowRate.KilogramsPerSecond, 12);
    }

    [Fact]
    public void StateForDifferentValve_IsRejected()
    {
        var valve = CreateValve(ValveCharacteristic.Linear, ValveFailSafeAction.FailClosed);
        var solver = new ValveFlowSolver();

        Assert.Throws<ArgumentException>(() => solver.Solve(
            valve,
            new ValveState("other-valve", ValvePosition.FullyOpen),
            CreateNode("from", 5.4d, 1_000d, 100d),
            CreateNode("to", 5.0d, 800d, 120d)));
    }

    [Fact]
    public void SameInputs_ProduceSameValveFlowResult()
    {
        var valve = CreateValve(ValveCharacteristic.EqualPercentage(50d), ValveFailSafeAction.FailClosed);
        var state = new ValveState(valve.Id, ValvePosition.FromPercent(63d));
        var from = CreateNode("from", 5.4d, 1_000d, 100d);
        var to = CreateNode("to", 5.0d, 800d, 120d);
        var solver = new ValveFlowSolver();

        Assert.Equal(
            solver.Solve(valve, state, from, to),
            solver.Solve(valve, state, from, to));
    }

    private static ValveFlowResult Solve(ValvePosition position)
    {
        var valve = CreateValve(ValveCharacteristic.Linear, ValveFailSafeAction.FailClosed);
        return Solve(valve, new ValveState(valve.Id, position));
    }

    private static ValveFlowResult Solve(ValveDefinition valve, ValveState state)
    {
        return new ValveFlowSolver().Solve(
            valve,
            state,
            CreateNode("from", 5.4d, 1_000d, 100d),
            CreateNode("to", 5.0d, 800d, 120d));
    }

    private static ValveDefinition CreateValve(
        ValveCharacteristic characteristic,
        ValveFailSafeAction failSafeAction)
    {
        return new ValveDefinition(
            "valve",
            new PipeDefinition(
                "pipe",
                "from",
                "to",
                QuadraticHydraulicResistance.FromPascalSecondsSquaredPerKilogramSquared(100_000d)),
            characteristic,
            failSafeAction);
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
