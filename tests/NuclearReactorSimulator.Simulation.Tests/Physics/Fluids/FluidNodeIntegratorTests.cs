using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Simulation.Physics.Fluids;
using Xunit;

namespace NuclearReactorSimulator.Simulation.Tests.Physics.Fluids;

public sealed class FluidNodeIntegratorTests
{
    [Fact]
    public void ZeroBalance_PreservesConservedInventoryAndResolvesThermodynamics()
    {
        var model = new RecordingThermodynamicModel();
        var integrator = new FluidNodeIntegrator(model);
        var initial = CreateState();

        var result = integrator.Step(initial, FluidNodeBalance.Zero, TimeSpan.FromSeconds(1d));

        Assert.Equal(initial.Mass, result.Mass);
        Assert.Equal(initial.InternalEnergy, result.InternalEnergy);
        Assert.Equal(model.ReturnedState, result.Thermodynamics);
        Assert.Equal(1, model.ResolveCount);
    }

    [Fact]
    public void PositiveNetRates_AddMassAndInternalEnergy()
    {
        var integrator = new FluidNodeIntegrator(new RecordingThermodynamicModel());
        var initial = CreateState();
        var balance = new FluidNodeBalance(
            MassFlowRate.FromKilogramsPerSecond(25d),
            Power.FromMegawatts(4d));

        var result = integrator.Step(initial, balance, TimeSpan.FromSeconds(2d));

        Assert.Equal(1_050d, result.Mass.Kilograms, 12);
        Assert.Equal(108d, result.InternalEnergy.Megajoules, 12);
        Assert.Equal(525d, result.Density.KilogramsPerCubicMetre, 12);
        Assert.Equal(1_000d, initial.Mass.Kilograms, 12);
        Assert.Equal(100d, initial.InternalEnergy.Megajoules, 12);
    }

    [Fact]
    public void NegativeNetRates_RemoveMassAndInternalEnergy()
    {
        var integrator = new FluidNodeIntegrator(new RecordingThermodynamicModel());
        var initial = CreateState();
        var balance = new FluidNodeBalance(
            MassFlowRate.FromKilogramsPerSecond(-10d),
            Power.FromMegawatts(-2d));

        var result = integrator.Step(initial, balance, TimeSpan.FromSeconds(5d));

        Assert.Equal(950d, result.Mass.Kilograms, 12);
        Assert.Equal(90d, result.InternalEnergy.Megajoules, 12);
    }

    [Fact]
    public void ThermodynamicModel_ReceivesCandidateInventoryAndPreviousState()
    {
        var model = new RecordingThermodynamicModel();
        var integrator = new FluidNodeIntegrator(model);
        var initial = CreateState();

        var result = integrator.Step(
            initial,
            new FluidNodeBalance(
                MassFlowRate.FromKilogramsPerSecond(3d),
                Power.FromKilowatts(500d)),
            TimeSpan.FromSeconds(4d));

        Assert.Equal(initial.Definition, model.LastDefinition);
        Assert.Equal(initial.Thermodynamics, model.LastPreviousState);
        Assert.NotNull(model.LastInventory);
        Assert.Equal(1_012d, model.LastInventory!.Mass.Kilograms, 12);
        Assert.Equal(102d, model.LastInventory.InternalEnergy.Megajoules, 12);
        Assert.Equal(model.ReturnedState, result.Thermodynamics);
    }

    [Fact]
    public void DepletingNode_FailsBeforeThermodynamicResolution()
    {
        var model = new RecordingThermodynamicModel();
        var integrator = new FluidNodeIntegrator(model);
        var initial = CreateState();

        var exception = Assert.Throws<FluidNodeDepletionException>(() => integrator.Step(
            initial,
            new FluidNodeBalance(
                MassFlowRate.FromKilogramsPerSecond(-500d),
                Power.Zero),
            TimeSpan.FromSeconds(2d)));

        Assert.Equal("test-node", exception.NodeId);
        Assert.Equal(0d, exception.CandidateMassKilograms, 12);
        Assert.Equal(0, model.ResolveCount);
        Assert.Equal(1_000d, initial.Mass.Kilograms, 12);
    }

    [Fact]
    public void NonPositiveIntegrationDuration_IsRejected()
    {
        var integrator = new FluidNodeIntegrator(new RecordingThermodynamicModel());
        var initial = CreateState();

        Assert.Throws<ArgumentOutOfRangeException>(() => integrator.Step(initial, FluidNodeBalance.Zero, TimeSpan.Zero));
        Assert.Throws<ArgumentOutOfRangeException>(() => integrator.Step(initial, FluidNodeBalance.Zero, TimeSpan.FromTicks(-1)));
    }

    [Fact]
    public void SameInitialStateAndBalances_ProduceSameResult()
    {
        var leftIntegrator = new FluidNodeIntegrator(new DeterministicThermodynamicModel());
        var rightIntegrator = new FluidNodeIntegrator(new DeterministicThermodynamicModel());
        var left = CreateState();
        var right = CreateState();
        var balances = new[]
        {
            new FluidNodeBalance(MassFlowRate.FromKilogramsPerSecond(4d), Power.FromMegawatts(1.2d)),
            new FluidNodeBalance(MassFlowRate.FromKilogramsPerSecond(-1.5d), Power.FromMegawatts(-0.4d)),
            FluidNodeBalance.Zero,
            new FluidNodeBalance(MassFlowRate.FromKilogramsPerSecond(2d), Power.FromMegawatts(0.1d)),
        };

        foreach (var balance in balances)
        {
            left = leftIntegrator.Step(left, balance, TimeSpan.FromMilliseconds(20d));
            right = rightIntegrator.Step(right, balance, TimeSpan.FromMilliseconds(20d));
        }

        Assert.Equal(left, right);
    }

    private static FluidNodeState CreateState()
    {
        return new FluidNodeState(
            new FluidNodeDefinition("test-node", Volume.FromCubicMetres(2d)),
            new FluidNodeInventory(
                Mass.FromKilograms(1_000d),
                Energy.FromMegajoules(100d)),
            new FluidThermodynamicState(
                Pressure.FromMegapascals(5d),
                Temperature.FromDegreesCelsius(250d)));
    }

    private sealed class RecordingThermodynamicModel : IFluidThermodynamicModel
    {
        public FluidThermodynamicState ReturnedState { get; } = new(
            Pressure.FromMegapascals(5.2d),
            Temperature.FromDegreesCelsius(252d));

        public int ResolveCount { get; private set; }

        public FluidNodeDefinition? LastDefinition { get; private set; }

        public FluidNodeInventory? LastInventory { get; private set; }

        public FluidThermodynamicState? LastPreviousState { get; private set; }

        public FluidThermodynamicState Resolve(
            FluidNodeDefinition definition,
            FluidNodeInventory inventory,
            FluidThermodynamicState previousState)
        {
            ResolveCount++;
            LastDefinition = definition;
            LastInventory = inventory;
            LastPreviousState = previousState;
            return ReturnedState;
        }
    }

    private sealed class DeterministicThermodynamicModel : IFluidThermodynamicModel
    {
        public FluidThermodynamicState Resolve(
            FluidNodeDefinition definition,
            FluidNodeInventory inventory,
            FluidThermodynamicState previousState)
        {
            var densityRatio = inventory.Mass.Kilograms / definition.Volume.CubicMetres;
            var specificEnergy = inventory.SpecificInternalEnergy.JoulesPerKilogram;

            return new FluidThermodynamicState(
                Pressure.FromPascals(100_000d + (densityRatio * 2_000d)),
                Temperature.FromKelvins(250d + (specificEnergy / 1_000_000d)));
        }
    }
}
