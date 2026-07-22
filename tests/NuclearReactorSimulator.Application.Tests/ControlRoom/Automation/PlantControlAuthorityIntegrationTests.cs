using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.ControlRoom.Automation;
using NuclearReactorSimulator.Application.Scenarios;
using NuclearReactorSimulator.Application.Scenarios.Training;
using NuclearReactorSimulator.Domain.Physics.Control;
using NuclearReactorSimulator.Domain.Physics.Control.Protection;
using NuclearReactorSimulator.Domain.Physics.Control.ReactorPrimary;
using NuclearReactorSimulator.Domain.Physics.Control.Supervisory;
using NuclearReactorSimulator.Domain.Physics.Instrumentation;
using NuclearReactorSimulator.Simulation.Physics.Control.Protection;
using NuclearReactorSimulator.Simulation.Physics.Control.Supervisory;
using NuclearReactorSimulator.Simulation.Physics.Instrumentation;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.ControlRoom.Automation;

public sealed class PlantControlAuthorityIntegrationTests
{
    [Fact]
    public void ManualTakeover_UsesCanonicalControllerStateAndLeavesAllLocalControllersManual()
    {
        var session = CreateSession();
        Assert.True(session.PlantControlAuthority.CurrentAutomation.IsAvailable);

        session.PlantControlAuthority.RequestAuthority(PlantControlAuthorityMode.Manual);
        session.CommandDispatcher.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.SingleStep));

        var automation = session.PlantControlAuthority.CurrentAutomation;
        Assert.Equal(PlantControlAuthorityMode.Manual, automation.RequestedAuthority);
        Assert.Equal(PlantControlAuthorityMode.Manual, automation.EffectiveAuthority);
        Assert.Equal(PlantControlAuthorityHealth.Normal, automation.Health);
        Assert.All(automation.ControllerModes, static controller => Assert.Equal(ControllerMode.Manual, controller.Mode));
    }

    [Fact]
    public void HoldCurrentOperatingPoint_FromManual_ActivatesOnlyCanonicalSupervisedLoopsAndRemainsDeterministic()
    {
        var left = CreateSession();
        var right = CreateSession();

        foreach (var session in new[] { left, right })
        {
            session.PlantControlAuthority.RequestAuthority(PlantControlAuthorityMode.Manual);
            session.CommandDispatcher.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.SingleStep));
            session.PlantControlAuthority.RequestSupervisoryObjective(SupervisoryObjectiveRequest.HoldCurrentOperatingPoint());
            session.PlantControlAuthority.RequestAuthority(PlantControlAuthorityMode.SupervisoryAutomatic);
            session.CommandDispatcher.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.SingleStep));
        }

        var leftAutomation = left.PlantControlAuthority.CurrentAutomation;
        var rightAutomation = right.PlantControlAuthority.CurrentAutomation;
        Assert.Equal(PlantControlAuthorityMode.SupervisoryAutomatic, leftAutomation.EffectiveAuthority);
        Assert.Equal(PlantControlAuthorityHealth.Normal, leftAutomation.Health);
        Assert.Contains("HOLD OPERATING POINT", leftAutomation.ObjectiveText, StringComparison.Ordinal);
        Assert.Equal(2, leftAutomation.ControllerModes.Count(static controller => controller.Mode == ControllerMode.Automatic));
        Assert.Equal(
            leftAutomation.ControllerModes.Select(static controller => (controller.ControllerId, controller.Mode, controller.Setpoint)).ToArray(),
            rightAutomation.ControllerModes.Select(static controller => (controller.ControllerId, controller.Mode, controller.Setpoint)).ToArray());
        Assert.Equal(left.Coordinator.Current.LogicalStep, right.Coordinator.Current.LogicalStep);
    }

    [Fact]
    public void ManualTakeover_ClearsPreviousSupervisoryObjectiveSoReentryCannotResumeStaleTarget()
    {
        var session = CreateSession();
        session.PlantControlAuthority.RequestSupervisoryObjective(SupervisoryObjectiveRequest.HoldCurrentOperatingPoint());
        session.PlantControlAuthority.RequestAuthority(PlantControlAuthorityMode.SupervisoryAutomatic);
        session.CommandDispatcher.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.SingleStep));
        Assert.Contains("HOLD OPERATING POINT", session.PlantControlAuthority.CurrentAutomation.ObjectiveText, StringComparison.Ordinal);

        session.PlantControlAuthority.RequestAuthority(PlantControlAuthorityMode.Manual);
        session.CommandDispatcher.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.SingleStep));
        Assert.Equal("NONE", session.PlantControlAuthority.CurrentAutomation.ObjectiveText);

        session.PlantControlAuthority.RequestAuthority(PlantControlAuthorityMode.SupervisoryAutomatic);
        session.CommandDispatcher.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.SingleStep));

        var automation = session.PlantControlAuthority.CurrentAutomation;
        Assert.Equal(PlantControlAuthorityMode.SupervisoryAutomatic, automation.RequestedAuthority);
        Assert.Equal(PlantControlAuthorityMode.Assisted, automation.EffectiveAuthority);
        Assert.Equal(PlantControlAuthorityHealth.Degraded, automation.Health);
        Assert.Equal("NONE", automation.ObjectiveText);
        Assert.Contains("objective", automation.DegradationReason!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ActiveProtection_SuspendsSupervisoryDecisionOnTheNextCommittedStepWithoutResettingProtection()
    {
        var session = CreateSession();
        session.PlantControlAuthority.RequestSupervisoryObjective(SupervisoryObjectiveRequest.HoldCurrentOperatingPoint());
        session.PlantControlAuthority.RequestAuthority(PlantControlAuthorityMode.SupervisoryAutomatic);
        session.CommandDispatcher.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.SingleStep));
        Assert.Equal(PlantControlAuthorityMode.SupervisoryAutomatic, session.PlantControlAuthority.CurrentAutomation.EffectiveAuthority);

        session.CommandDispatcher.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.ReactorScram));
        session.CommandDispatcher.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.SingleStep));
        Assert.True(session.Coordinator.Current.ReactorScramActive);

        session.CommandDispatcher.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.SingleStep));
        var automation = session.PlantControlAuthority.CurrentAutomation;
        Assert.Equal(PlantControlAuthorityMode.SupervisoryAutomatic, automation.RequestedAuthority);
        Assert.Equal(PlantControlAuthorityMode.Assisted, automation.EffectiveAuthority);
        Assert.Equal(PlantControlAuthorityHealth.SuspendedByProtection, automation.Health);
        Assert.NotNull(automation.DegradationReason);
        Assert.Contains("SCRAM", automation.DegradationReason!, StringComparison.OrdinalIgnoreCase);
        Assert.True(session.Coordinator.Current.ReactorScramActive);
    }

    [Fact]
    public void InvalidRequiredMeasurement_DegradesFailClosedWithoutTrueStateFallback()
    {
        var engine = Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(
            new DesktopIntegratedOperationsInitialConditionFactory().CreateRuntimeEngine());
        var state = engine.CurrentState;
        var inputs = engine.PersistentInputs;
        var powerLoop = inputs.ReactorPrimaryInputs.Definition.Loops
            .Single(static loop => loop.Kind == ReactorPrimaryControlLoopKind.ReactorPowerRodRegulation);
        var powerController = inputs.ReactorPrimaryInputs.Controllers.Definition.GetController(powerLoop.ControllerId);
        var invalidSignals = new MeasuredSignalFrame(
            state.MeasuredSignals.Definition,
            state.MeasuredSignals.Signals.Select(signal => string.Equals(signal.ChannelId, powerController.MeasurementChannelId, StringComparison.Ordinal)
                ? signal with
                {
                    EngineeringValue = null,
                    ScaledValue = null,
                    Validity = SignalValidity.Invalid,
                    Quality = SignalQuality.Bad,
                }
                : signal));
        var protection = new ProtectionSystemSnapshot(
            state.ProtectionState.Definition,
            Array.Empty<ProtectionFunctionSnapshot>(),
            Array.Empty<ProtectionInterlockSnapshot>(),
            Array.Empty<ProtectionPermissiveSnapshot>(),
            ProtectionAction.None,
            ProtectionInterlockAction.None,
            resetRequested: false,
            resetAccepted: false);
        var coordinator = new SupervisoryOperationCoordinator();
        var objective = SupervisoryOperatingObjective.HoldReactorPower(10_000_000d);

        var result = coordinator.Step(
            SupervisoryOperationState.CreateInitial(PlantControlAuthorityMode.Assisted),
            PlantControlAuthorityMode.SupervisoryAutomatic,
            objective,
            invalidSignals,
            protection,
            state.ReactorPrimaryControlState,
            state.TurbineSecondaryControlState,
            inputs.ReactorPrimaryInputs,
            inputs.TurbineSecondaryInputs);

        Assert.Equal(PlantControlAuthorityMode.Assisted, result.CandidateState.EffectiveAuthority);
        Assert.Equal(PlantControlAuthorityHealth.Degraded, result.CandidateState.Health);
        Assert.Contains(powerController.MeasurementChannelId, result.CandidateState.DegradationReason!, StringComparison.Ordinal);
        Assert.Same(inputs.ReactorPrimaryInputs, result.ReactorPrimaryInputs);
        Assert.Same(inputs.TurbineSecondaryInputs, result.TurbineSecondaryInputs);
        Assert.False(result.AppliedDecision);
    }

    private static ScenarioSession CreateSession()
        => new ScenarioSessionFactory(new VersionedInitialConditionRegistry(new IVersionedInitialConditionFactory[]
        {
            new DesktopIntegratedOperationsInitialConditionFactory(),
        })).Load(DesktopIntegratedOperationsProgram.Scenario);
}
