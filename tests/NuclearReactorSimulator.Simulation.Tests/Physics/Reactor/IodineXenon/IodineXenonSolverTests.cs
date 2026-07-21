using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.Reactor;
using NuclearReactorSimulator.Domain.Physics.Reactor.IodineXenon;
using NuclearReactorSimulator.Simulation.Physics.Reactor;
using NuclearReactorSimulator.Simulation.Physics.Reactor.IodineXenon;
using Xunit;

namespace NuclearReactorSimulator.Simulation.Tests.Physics.Reactor.IodineXenon;

public sealed class IodineXenonSolverTests
{
    [Fact]
    public void EmptyInventoryWithoutFission_RemainsEmpty()
    {
        var solver = Solver();

        var result = solver.Step(
            IodineXenonState.Empty,
            Power.Zero,
            NeutronPopulation.Zero,
            TimeSpan.FromSeconds(10d));

        Assert.Equal(IodineInventory.Zero, result.State.Iodine);
        Assert.Equal(XenonInventory.Zero, result.State.Xenon);
        Assert.Equal(0d, result.Snapshot.XenonReactivity.Pcm);
    }

    [Fact]
    public void EquilibriumState_RemainsSteadyAtConstantPowerAndFlux()
    {
        var solver = Solver();
        var power = Power.FromMegawatts(1_000d);
        var initial = IodineXenonState.CreateEquilibrium(solver.Definition, power, NeutronPopulation.Reference);

        var result = solver.Step(initial, power, NeutronPopulation.Reference, TimeSpan.FromSeconds(25d));

        Assert.Equal(initial.Iodine.Relative, result.State.Iodine.Relative, 12);
        Assert.Equal(initial.Xenon.Relative, result.State.Xenon.Relative, 12);
    }

    [Fact]
    public void ShutdownFromEquilibrium_CanProduceInitialXenonBuildup()
    {
        var solver = Solver();
        var initial = IodineXenonState.CreateEquilibrium(
            solver.Definition,
            Power.FromMegawatts(1_000d),
            NeutronPopulation.Reference);

        var result = solver.Step(initial, Power.Zero, NeutronPopulation.Zero, TimeSpan.FromSeconds(2d));

        Assert.True(result.State.Iodine.Relative < initial.Iodine.Relative);
        Assert.True(result.State.Xenon.Relative > initial.Xenon.Relative);
        Assert.True(result.Snapshot.XenonReactivity < Reactivity.Zero);
    }

    [Fact]
    public void HigherNeutronPopulation_LowersEquilibriumXenonThroughBurnup()
    {
        var definition = Solver().Definition;
        var power = Power.FromMegawatts(1_000d);
        var lowFlux = IodineXenonState.CreateEquilibrium(
            definition,
            power,
            NeutronPopulation.FromRelative(0.25d));
        var highFlux = IodineXenonState.CreateEquilibrium(
            definition,
            power,
            NeutronPopulation.FromRelative(2d));

        Assert.True(highFlux.Xenon.Relative < lowFlux.Xenon.Relative);
    }

    [Fact]
    public void Snapshot_SeparatesProductionDecayAndBurnupRates()
    {
        var solver = Solver();
        var power = Power.FromMegawatts(1_000d);
        var state = IodineXenonState.CreateEquilibrium(solver.Definition, power, NeutronPopulation.Reference);

        var snapshot = solver.CreateSnapshot(state, power, NeutronPopulation.Reference);

        Assert.True(snapshot.IodineProductionRatePerSecond > 0d);
        Assert.True(snapshot.IodineDecayRatePerSecond > 0d);
        Assert.True(snapshot.XenonProductionFromIodineRatePerSecond > 0d);
        Assert.True(snapshot.XenonNaturalDecayRatePerSecond > 0d);
        Assert.True(snapshot.XenonBurnupRatePerSecond > 0d);
    }

    [Fact]
    public void XenonSnapshot_ComposesThroughValidatedReactivityModel()
    {
        var solver = Solver();
        var snapshot = solver.CreateSnapshot(
            new IodineXenonState(IodineInventory.Zero, XenonInventory.FromRelative(0.2d)),
            Power.Zero,
            NeutronPopulation.Zero);
        var model = new ReactivityModel();
        var total = model.Evaluate([
            snapshot.ToContribution(),
            new ReactivityContribution("control-rods/a", ReactivityContributionKind.ControlRods, Reactivity.FromPcm(50d)),
        ]);

        Assert.Equal(-150d, total.Total.Pcm, 9);
        Assert.Equal(-200d, total.TotalFor(ReactivityContributionKind.Xenon).Pcm, 9);
    }

    [Fact]
    public void SameInputs_ProduceIdenticalResults()
    {
        var solver = Solver();
        var state = new IodineXenonState(
            IodineInventory.FromRelative(0.08d),
            XenonInventory.FromRelative(0.03d));

        var left = solver.Step(state, Power.FromMegawatts(650d), NeutronPopulation.FromRelative(0.7d), TimeSpan.FromSeconds(3d));
        var right = solver.Step(state, Power.FromMegawatts(650d), NeutronPopulation.FromRelative(0.7d), TimeSpan.FromSeconds(3d));

        Assert.Equal(left, right);
    }

    [Fact]
    public void NegativeFissionPower_FailsFast()
    {
        var solver = Solver();

        Assert.Throws<ArgumentOutOfRangeException>(() => solver.Step(
            IodineXenonState.Empty,
            Power.FromMegawatts(-1d),
            NeutronPopulation.Zero,
            TimeSpan.FromSeconds(1d)));
    }

    private static IodineXenonSolver Solver()
        => new(new IodineXenonDefinition(
            "core",
            Power.FromMegawatts(1_000d),
            PoisonProductionRate.FromRelativePerSecond(0.01d),
            PoisonProductionRate.Zero,
            DecayConstant.FromPerSecond(0.1d),
            DecayConstant.FromPerSecond(0.01d),
            XenonBurnupCoefficient.FromPerSecondPerRelativeNeutronPopulation(0.19d),
            XenonReactivityCoefficient.FromPcmPerRelativeInventory(-1_000d)));
}
