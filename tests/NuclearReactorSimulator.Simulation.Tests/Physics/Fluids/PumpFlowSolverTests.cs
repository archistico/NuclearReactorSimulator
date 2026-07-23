using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Simulation.Physics.Fluids;
using Xunit;

namespace NuclearReactorSimulator.Simulation.Tests.Physics.Fluids;

public sealed class PumpFlowSolverTests
{
    [Fact]
    public void RatedPump_AtEqualNodePressureCreatesForwardFlow()
    {
        var result = Solve(PumpSpeed.Rated, true, 5d, 5d);

        Assert.Equal(0d, result.NodePressureDifference.Megapascals, 12);
        Assert.Equal(0.4d, result.ActivePressureBoost.Megapascals, 12);
        Assert.Equal(2d, result.MassFlowRate.KilogramsPerSecond, 12);
        Assert.Equal(0.2d, result.InternalPressureLoss.Megapascals, 12);
        Assert.Equal(0.2d, result.NetPumpPressureContribution.Megapascals, 12);
    }

    [Fact]
    public void HalfSpeed_UsesAffinityLawsForPressureFlowAndPower()
    {
        var rated = Solve(PumpSpeed.Rated, true, 5d, 5d);
        var half = Solve(PumpSpeed.FromPercent(50d), true, 5d, 5d);

        Assert.Equal(rated.ActivePressureBoost.Pascals / 4d, half.ActivePressureBoost.Pascals, 12);
        Assert.Equal(rated.MassFlowRate.KilogramsPerSecond / 2d, half.MassFlowRate.KilogramsPerSecond, 12);
        Assert.Equal(rated.ShaftPowerDemand.Watts / 8d, half.ShaftPowerDemand.Watts, 12);
    }

    [Fact]
    public void RatedPump_ReportsHydraulicExchangeAndShaftDemand()
    {
        var result = Solve(PumpSpeed.Rated, true, 5d, 5d);

        Assert.Equal(0.002d, result.VolumetricFlowRate.CubicMetresPerSecond, 12);
        Assert.Equal(800d, result.HydraulicPowerExchange.Watts, 12);
        Assert.Equal(1_000d, result.ShaftPowerDemand.Watts, 12);
    }

    [Fact]
    public void StoppedPump_AtEqualPressureProducesNoFlowOrPower()
    {
        var result = Solve(PumpSpeed.Rated, false, 5d, 5d);

        Assert.Equal(PumpSpeed.Stopped, result.EffectiveSpeed);
        Assert.Equal(PressureDifference.Zero, result.ActivePressureBoost);
        Assert.Equal(MassFlowRate.Zero, result.MassFlowRate);
        Assert.Equal(Power.Zero, result.HydraulicPowerExchange);
        Assert.Equal(Power.Zero, result.ShaftPowerDemand);
    }

    [Fact]
    public void StoppedPump_AllowsPassivePressureDrivenFlowThroughCombinedResistance()
    {
        var result = Solve(PumpSpeed.Rated, false, 5.4d, 5d);

        Assert.Equal(2d, result.MassFlowRate.KilogramsPerSecond, 12);
        Assert.Equal(Power.Zero, result.HydraulicPowerExchange);
        Assert.Equal(Power.Zero, result.ShaftPowerDemand);
    }

    [Fact]
    public void SufficientBackpressure_ReversesFlowWithoutRegenerativeShaftCredit()
    {
        var result = Solve(PumpSpeed.Rated, true, 5d, 5.9d);

        Assert.True(result.MassFlowRate.KilogramsPerSecond < 0d);
        Assert.True(result.HydraulicPowerExchange.Watts < 0d);
        Assert.Equal(Power.Zero, result.ShaftPowerDemand);
    }


    [Fact]
    public void DischargeCheckValve_BlocksReverseFlowWithoutMassOrEnergyTransfer()
    {
        var pump = CreatePump(hasDischargeCheckValve: true);
        var result = new PumpFlowSolver().Solve(
            pump,
            new PumpState(pump.Id, PumpSpeed.Rated),
            CreateNode("from", 5d, 1_000d, 100d),
            CreateNode("to", 5.9d, 800d, 120d));

        Assert.Equal(MassFlowRate.Zero, result.MassFlowRate);
        Assert.Equal(VolumetricFlowRate.Zero, result.VolumetricFlowRate);
        Assert.Equal(Power.Zero, result.HydraulicPowerExchange);
        Assert.Equal(Power.Zero, result.ShaftPowerDemand);
        Assert.Equal(MassFlowRate.Zero, result.FromNodeBalance.NetMassFlowRate);
        Assert.Equal(MassFlowRate.Zero, result.ToNodeBalance.NetMassFlowRate);
        Assert.Equal(Power.Zero, result.FromNodeBalance.NetEnergyRate);
        Assert.Equal(Power.Zero, result.ToNodeBalance.NetEnergyRate);
    }

    [Fact]
    public void StoppedPump_WithDischargeCheckValveBlocksPassiveReverseFlowButStillAllowsForwardFlow()
    {
        var pump = CreatePump(hasDischargeCheckValve: true);
        var solver = new PumpFlowSolver();

        var reverse = solver.Solve(
            pump,
            new PumpState(pump.Id, PumpSpeed.Rated, isRunning: false),
            CreateNode("from", 5d, 1_000d, 100d),
            CreateNode("to", 5.4d, 800d, 120d));
        var forward = solver.Solve(
            pump,
            new PumpState(pump.Id, PumpSpeed.Rated, isRunning: false),
            CreateNode("from", 5.4d, 1_000d, 100d),
            CreateNode("to", 5d, 800d, 120d));

        Assert.Equal(MassFlowRate.Zero, reverse.MassFlowRate);
        Assert.True(forward.MassFlowRate.KilogramsPerSecond > 0d);
    }

    [Fact]
    public void PumpBalances_ConserveMassAndExposeExternalEnergyInput()
    {
        var result = Solve(PumpSpeed.Rated, true, 5d, 5d);
        var total = result.FromNodeBalance + result.ToNodeBalance;

        Assert.Equal(MassFlowRate.Zero, total.NetMassFlowRate);
        Assert.Equal(result.HydraulicPowerExchange, total.NetEnergyRate);
    }

    [Fact]
    public void ForwardPumpWork_IsAppliedToActualDownstreamNode()
    {
        var result = Solve(PumpSpeed.Rated, true, 5d, 5d);

        Assert.Equal(-0.2d, result.FromNodeBalance.NetEnergyRate.Megawatts, 12);
        Assert.Equal(0.2008d, result.ToNodeBalance.NetEnergyRate.Megawatts, 12);
    }

    [Fact]
    public void StateForDifferentPump_IsRejected()
    {
        var pump = CreatePump();
        var solver = new PumpFlowSolver();

        Assert.Throws<ArgumentException>(() => solver.Solve(
            pump,
            new PumpState("other", PumpSpeed.Rated),
            CreateNode("from", 5d, 1_000d, 100d),
            CreateNode("to", 5d, 800d, 120d)));
    }

    [Fact]
    public void EndpointMismatch_IsRejected()
    {
        var pump = CreatePump();
        var solver = new PumpFlowSolver();

        Assert.Throws<ArgumentException>(() => solver.Solve(
            pump,
            new PumpState(pump.Id, PumpSpeed.Rated),
            CreateNode("wrong", 5d, 1_000d, 100d),
            CreateNode("to", 5d, 800d, 120d)));
    }

    [Fact]
    public void SameInputs_ProduceSamePumpFlowResult()
    {
        var pump = CreatePump();
        var state = new PumpState(pump.Id, PumpSpeed.FromPercent(73d));
        var from = CreateNode("from", 5.1d, 1_000d, 100d);
        var to = CreateNode("to", 5d, 800d, 120d);
        var solver = new PumpFlowSolver();

        Assert.Equal(
            solver.Solve(pump, state, from, to),
            solver.Solve(pump, state, from, to));
    }

    private static PumpFlowResult Solve(
        PumpSpeed speed,
        bool isRunning,
        double fromPressureMegapascals,
        double toPressureMegapascals)
    {
        var pump = CreatePump();
        return new PumpFlowSolver().Solve(
            pump,
            new PumpState(pump.Id, speed, isRunning),
            CreateNode("from", fromPressureMegapascals, 1_000d, 100d),
            CreateNode("to", toPressureMegapascals, 800d, 120d));
    }

    private static PumpDefinition CreatePump(bool hasDischargeCheckValve = false)
    {
        return new PumpDefinition(
            "pump",
            new PipeDefinition(
                "path",
                "from",
                "to",
                QuadraticHydraulicResistance.FromPascalSecondsSquaredPerKilogramSquared(50_000d)),
            PressureDifference.FromMegapascals(0.4d),
            QuadraticHydraulicResistance.FromPascalSecondsSquaredPerKilogramSquared(50_000d),
            PumpEfficiency.FromPercent(80d),
            hasDischargeCheckValve);
    }

    private static FluidNodeState CreateNode(
        string id,
        double pressureMegapascals,
        double massKilograms,
        double internalEnergyMegajoules)
    {
        return new FluidNodeState(
            new FluidNodeDefinition(id, Volume.FromCubicMetres(1d)),
            new FluidNodeInventory(
                Mass.FromKilograms(massKilograms),
                Energy.FromMegajoules(internalEnergyMegajoules)),
            new FluidThermodynamicState(
                Pressure.FromMegapascals(pressureMegapascals),
                Temperature.FromDegreesCelsius(250d)));
    }
}
