using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios;
using NuclearReactorSimulator.Application.Scenarios.Faults.ElectricalLoss;
using NuclearReactorSimulator.Application.Scenarios.Faults.Hydraulics;
using NuclearReactorSimulator.Application.Scenarios.Faults.InstrumentationControl;
using NuclearReactorSimulator.Application.Scenarios.Operations;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.Scenarios.Faults;

public sealed class ElectricalLossScenarioTests
{
    [Fact]
    public void ExternalSupplyLoss_ForcesCanonicalBreakerOpenEvenAgainstCloseRequest()
    {
        var engine = CreateEngine();
        Assert.True(engine.CreatePresentationSnapshot(ControlRoomRunState.Paused).Electrical.Generators.Single().BreakerClosed);

        ((IElectricalLossFaultTarget)engine).ActivateExternalSupplyLoss("grid-loss", "grid");
        var opened = engine.Step(ControlRoomRunState.Paused);
        Assert.False(opened.Electrical.Generators.Single().BreakerClosed);

        engine.QueueOperatorCommand(new ControlRoomCommand(
            ControlRoomCommandKind.GeneratorBreakerClose,
            "breaker",
            ControlRoomCommandTargetKind.Breaker));
        var stillOpen = engine.Step(ControlRoomRunState.Paused);

        Assert.False(stillOpen.Electrical.Generators.Single().BreakerClosed);
    }

    [Fact]
    public void ClearingExternalSupplyLoss_DoesNotAutomaticallyReconnectGenerator()
    {
        var engine = CreateEngine();
        var target = (IElectricalLossFaultTarget)engine;
        target.ActivateExternalSupplyLoss("grid-loss", "grid");
        engine.Step(ControlRoomRunState.Paused);

        target.ClearElectricalLossFault("grid-loss");
        var snapshot = engine.Step(ControlRoomRunState.Paused);

        Assert.False(snapshot.Electrical.Generators.Single().BreakerClosed);
    }

    [Fact]
    public void UnknownExternalGrid_FailsClosedAtActivation()
    {
        var engine = CreateEngine();
        var target = (IElectricalLossFaultTarget)engine;

        Assert.Throws<KeyNotFoundException>(() => target.ActivateExternalSupplyLoss("bad", "missing-grid"));
    }

    [Fact]
    public void StationBlackoutClass_DeclaresExplicitPumpAndPoweredCommandConsequences()
    {
        var faults = ElectricalLossScenarioPack.StationBlackoutClass.Faults;

        Assert.Contains(faults, fault =>
            fault.FaultTypeId == ElectricalLossFaultTypeIds.ExternalSupplyLoss
            && fault.TargetId == "grid");
        Assert.Contains(faults, fault => fault.FaultTypeId == HydraulicFaultTypeIds.PumpTrip && fault.TargetId == "pump");
        Assert.Contains(faults, fault => fault.FaultTypeId == HydraulicFaultTypeIds.PumpTrip && fault.TargetId == "feedwater-pump");
        Assert.Contains(faults, fault => fault.FaultTypeId == HydraulicFaultTypeIds.PumpTrip && fault.TargetId == "condensate-pump");
        Assert.Contains(faults, fault =>
            fault.FaultTypeId == InstrumentationControlFaultTypeIds.ActuatorCommandFailLow
            && fault.TargetId == "mcp-actuator");
    }

    [Fact]
    public void BuiltInScenarioPack_BindsAllM86ScenariosWithoutCustomRegistry()
    {
        var registry = new VersionedInitialConditionRegistry(new IVersionedInitialConditionFactory[]
        {
            new PowerManoeuvringInitialConditionFactory(),
        });
        var factory = new ScenarioSessionFactory(registry);

        foreach (var scenario in ElectricalLossScenarioPack.All)
        {
            var session = factory.Load(scenario);
            Assert.Equal(scenario.Faults.Count, session.Coordinator.Current.Faults.PendingCount);
        }
    }

    private static IntegratedAutomaticOperationRuntimeEngine CreateEngine()
        => Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(
            new PowerManoeuvringInitialConditionFactory().CreateRuntimeEngine());
}
