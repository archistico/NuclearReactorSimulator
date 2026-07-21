using NuclearReactorSimulator.Application.Scenarios.Faults;
using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios;
using NuclearReactorSimulator.Application.Scenarios.Faults.Hydraulics;
using NuclearReactorSimulator.Application.Scenarios.Operations;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.Scenarios.Faults;

public sealed class HydraulicComponentFaultTests
{
    [Fact]
    public void PumpTrip_OverridesNormalControlBeforeCanonicalPhysicalStep()
    {
        var engine = CreateEngine();
        var target = (IHydraulicComponentFaultTarget)engine;

        Assert.True(engine.CurrentState.PlantState.PlantState.GetPump("pump").IsRunning);
        target.ActivatePumpTrip("trip", "pump");

        engine.Step(ControlRoomRunState.Paused);

        var pump = engine.CurrentState.PlantState.PlantState.GetPump("pump");
        Assert.False(pump.IsRunning);
        Assert.Equal(0d, pump.Speed.Fraction, 12);
    }

    [Fact]
    public void PumpDegradation_ClampsEffectiveSpeedWithoutSecondHydraulicIntegrator()
    {
        var reference = CreateEngine();
        var degraded = CreateEngine();
        ((IHydraulicComponentFaultTarget)degraded).ActivatePumpDegradation("degraded", "pump", 0.40d);

        reference.Step(ControlRoomRunState.Paused);
        degraded.Step(ControlRoomRunState.Paused);

        var referencePump = reference.CurrentState.PlantState.PlantState.GetPump("pump");
        var degradedPump = degraded.CurrentState.PlantState.PlantState.GetPump("pump");
        Assert.True(degradedPump.IsRunning);
        Assert.Equal(referencePump.Speed.Fraction * 0.40d, degradedPump.Speed.Fraction, 12);
    }

    [Fact]
    public void ValveFailureAndPathRestriction_ClampCanonicalValveState()
    {
        var failClosed = CreateEngine();
        ((IHydraulicComponentFaultTarget)failClosed).ActivateValveFailClosed("closed", "stop");
        failClosed.Step(ControlRoomRunState.Paused);
        Assert.True(failClosed.CurrentState.PlantState.PlantState.GetValve("stop").Position.IsClosed);

        var restricted = CreateEngine();
        ((IHydraulicComponentFaultTarget)restricted).ActivatePathRestriction("restricted", "stop", 0.25d);
        restricted.Step(ControlRoomRunState.Paused);
        Assert.InRange(
            restricted.CurrentState.PlantState.PlantState.GetValve("stop").Position.Fraction,
            0.249999999999d,
            0.250000000001d);
    }

    [Fact]
    public void ValveStuck_CapturesCommittedPositionAtActivation()
    {
        var engine = CreateEngine();
        var before = engine.CurrentState.PlantState.PlantState.GetValve("control").Position;
        var target = (IHydraulicComponentFaultTarget)engine;
        target.ActivateValveStuck("stuck", "control");
        engine.QueueOperatorCommand(new ControlRoomCommand(
            ControlRoomCommandKind.TurbineSpeedRaise,
            "rotor",
            ControlRoomCommandTargetKind.TurbineRotor));

        engine.Step(ControlRoomRunState.Paused);

        Assert.Equal(before, engine.CurrentState.PlantState.PlantState.GetValve("control").Position);
    }

    [Fact]
    public void SelectedLeak_RemovesMassThroughSingleAuditedPlantNetworkBoundary()
    {
        var reference = CreateEngine();
        var leaking = CreateEngine();
        ((IHydraulicComponentFaultTarget)leaking).ActivateLeak(
            "leak",
            "drum",
            MassFlowRate.FromKilogramsPerSecond(0.01d));

        reference.Step(ControlRoomRunState.Paused);
        leaking.Step(ControlRoomRunState.Paused);

        var referenceMass = reference.CurrentState.PlantState.PlantState.FluidNodes.Sum(static x => x.Mass.Kilograms);
        var leakingMass = leaking.CurrentState.PlantState.PlantState.FluidNodes.Sum(static x => x.Mass.Kilograms);
        Assert.True(leakingMass < referenceMass);
    }

    [Fact]
    public void BuiltInScenarioPack_BindsAllHydraulicFaultTypesWithoutCustomRegistry()
    {
        var registry = new VersionedInitialConditionRegistry(new IVersionedInitialConditionFactory[]
        {
            new PowerManoeuvringInitialConditionFactory(),
        });

        var session = new ScenarioSessionFactory(registry).Load(HydraulicComponentFaultScenarioPack.Demonstration);

        Assert.Equal(8, session.Scenario.Faults.Count);
        Assert.All(session.Scenario.Faults, static fault => Assert.Contains(fault.FaultTypeId, HydraulicFaultTypeIds.All));
        Assert.Equal(8, session.Coordinator.Current.Faults.PendingCount);
    }

    [Fact]
    public void InvalidHydraulicTarget_FailsClosedAtActivationBoundary()
    {
        var registry = new VersionedInitialConditionRegistry(new IVersionedInitialConditionFactory[]
        {
            new PowerManoeuvringInitialConditionFactory(),
        });
        var scenario = new ScenarioDefinition(
            "invalid-hydraulic-target",
            "Invalid hydraulic target",
            "Verifies fail-closed binding to canonical plant IDs.",
            PowerManoeuvringNormalShutdownProgram.InitialCondition,
            faults: new[]
            {
                new ScenarioFaultDefinition(
                    "bad",
                    HydraulicFaultTypeIds.PumpTrip,
                    "missing-pump",
                    ScenarioFaultTriggerDefinition.AtLogicalStep(1)),
            });
        var session = new ScenarioSessionFactory(registry).Load(scenario);

        Assert.Throws<KeyNotFoundException>(() =>
            session.Coordinator.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.SingleStep)));
    }

    private static IntegratedAutomaticOperationRuntimeEngine CreateEngine()
        => Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(
            new PowerManoeuvringInitialConditionFactory().CreateRuntimeEngine());
}
