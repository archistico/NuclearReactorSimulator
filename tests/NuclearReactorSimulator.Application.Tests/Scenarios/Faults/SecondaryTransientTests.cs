using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios;
using NuclearReactorSimulator.Application.Scenarios.Faults.SecondaryTransients;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.Scenarios.Faults;

public sealed class SecondaryTransientTests
{
    [Fact]
    public void InitialCondition_SeedsFiniteCanonicalCondenserCoolingCapacity()
    {
        var engine = CreateEngine();

        var cooling = engine.PersistentInputs.PlantInputs.GeneratorGridInputs
            .CondensateFeedwaterInputs.CondenserInputs.GetCoolingBoundaryInput("cooling");

        Assert.Equal(0.1d, cooling.AvailableHeatRejectionPower.Megawatts, 12);
    }

    [Fact]
    public void TurbineTrip_UsesCanonicalProtectionLatch()
    {
        var engine = CreateEngine();
        ((ISecondaryTransientFaultTarget)engine).ActivateTurbineTrip("trip", "rotor");

        var snapshot = engine.Step(ControlRoomRunState.Paused);

        Assert.True(snapshot.TurbineTripActive);
    }

    [Fact]
    public void GeneratorTrip_OpensCanonicalBreakerAndRejectsLoad()
    {
        var engine = CreateEngine();
        var initial = engine.CreatePresentationSnapshot(ControlRoomRunState.Paused);
        Assert.True(initial.Electrical.Generators.Single().BreakerClosed);

        ((ISecondaryTransientFaultTarget)engine).ActivateGeneratorTrip("trip", "generator");
        engine.Step(ControlRoomRunState.Paused);
        var snapshot = engine.Step(ControlRoomRunState.Paused);

        Assert.True(snapshot.GeneratorTripActive);
        Assert.False(snapshot.Electrical.Generators.Single().BreakerClosed);
    }

    [Fact]
    public void CondenserCoolingLoss_RemovesHeatRejectionCapacityThroughExistingBoundary()
    {
        var engine = CreateEngine();
        ((ISecondaryTransientFaultTarget)engine).ActivateCondenserCoolingLoss("cooling-loss", "cooling");

        var snapshot = engine.Step(ControlRoomRunState.Paused);
        var condenser = snapshot.TurbineSecondary.Condensers.Single();

        Assert.Equal(0d, condenser.HeatRejectionPower.NumericValue ?? double.NaN, 12);
    }

    [Fact]
    public void BuiltInScenarioPack_BindsAllM84ScenariosWithoutCustomRegistry()
    {
        var registry = new VersionedInitialConditionRegistry(new IVersionedInitialConditionFactory[]
        {
            new SecondaryTransientInitialConditionFactory(),
        });
        var factory = new ScenarioSessionFactory(registry);

        foreach (var scenario in SecondaryTransientScenarioPack.All)
        {
            var session = factory.Load(scenario);
            Assert.Equal(scenario.Faults.Count, session.Coordinator.Current.Faults.PendingCount);
        }
    }

    [Fact]
    public void UnknownCondenserCoolingBoundary_FailsClosedAtActivation()
    {
        var engine = CreateEngine();
        var target = (ISecondaryTransientFaultTarget)engine;

        Assert.Throws<KeyNotFoundException>(() =>
            target.ActivateCondenserCoolingLoss("bad", "missing-cooling-boundary"));
    }

    private static IntegratedAutomaticOperationRuntimeEngine CreateEngine()
        => Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(
            new SecondaryTransientInitialConditionFactory().CreateRuntimeEngine());
}
