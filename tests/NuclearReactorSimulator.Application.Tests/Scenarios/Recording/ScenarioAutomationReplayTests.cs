using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.ControlRoom.Automation;
using NuclearReactorSimulator.Application.Scenarios;
using NuclearReactorSimulator.Application.Scenarios.Recording;
using NuclearReactorSimulator.Application.Scenarios.Training;
using NuclearReactorSimulator.Domain.Physics.Control.Supervisory;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.Scenarios.Recording;

public sealed class ScenarioAutomationReplayTests
{
    [Fact]
    public void AuthorityAndObjectiveIntents_AreRecordedAndReplayedAtTheSameNextStepBoundary()
    {
        var factory = CreateFactory();
        var session = factory.Load(DesktopIntegratedOperationsProgram.Scenario);
        ScenarioRecording recording;
        ScenarioCheckpoint checkpoint;
        using (var recorder = new ScenarioRecorder(session))
        {
            session.PlantControlAuthority.RequestAuthority(PlantControlAuthorityMode.Manual);
            session.CommandDispatcher.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.SingleStep));
            session.PlantControlAuthority.RequestSupervisoryObjective(SupervisoryObjectiveRequest.HoldCurrentOperatingPoint());
            session.PlantControlAuthority.RequestAuthority(PlantControlAuthorityMode.SupervisoryAutomatic);
            session.CommandDispatcher.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.SingleStep));
            checkpoint = recorder.CreateCheckpoint("supervisory-active");
            session.CommandDispatcher.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.SingleStep));
            recording = recorder.Complete();
        }

        Assert.Equal(3, recording.AutomationIntents.Count);
        Assert.Equal(ScenarioAutomationIntentKind.PlantControlAuthority, recording.AutomationIntents[0].Kind);
        Assert.Equal(PlantControlAuthorityMode.Manual, recording.AutomationIntents[0].Authority);
        Assert.Equal(ScenarioAutomationIntentKind.SupervisoryObjective, recording.AutomationIntents[1].Kind);
        Assert.True(recording.AutomationIntents[1].Objective!.CaptureCurrentOperatingPoint);
        Assert.Equal(PlantControlAuthorityMode.SupervisoryAutomatic, recording.AutomationIntents[2].Authority);

        var result = new ScenarioFullReplayRunner(factory).ReplayAndVerify(DesktopIntegratedOperationsProgram.Scenario, recording);
        Assert.Equal(recording.AutomationIntents.Count, result.ReplayedRecording.AutomationIntents.Count);
        for (var index = 0; index < recording.AutomationIntents.Count; index++)
        {
            Assert.Equal(recording.AutomationIntents[index], result.ReplayedRecording.AutomationIntents[index]);
        }
        Assert.Equal(PlantControlAuthorityMode.SupervisoryAutomatic, result.Session.PlantControlAuthority.CurrentAutomation.EffectiveAuthority);

        var restored = new ScenarioFullReplayRunner(factory).SeekAndVerify(
            DesktopIntegratedOperationsProgram.Scenario,
            recording,
            checkpoint);
        Assert.Equal(checkpoint.LogicalStep, restored.Coordinator.Current.LogicalStep);
        Assert.Equal(PlantControlAuthorityMode.SupervisoryAutomatic, restored.PlantControlAuthority.CurrentAutomation.EffectiveAuthority);
    }

    [Fact]
    public void RecorderCompletion_FailsClosedWhenAcceptedAutomationIntentHasNotReachedItsApplicationStep()
    {
        var session = CreateFactory().Load(DesktopIntegratedOperationsProgram.Scenario);
        using var recorder = new ScenarioRecorder(session);
        session.PlantControlAuthority.RequestAuthority(PlantControlAuthorityMode.Manual);

        Assert.Throws<InvalidOperationException>(() =>
        {
            recorder.Complete();
        });
    }

    [Fact]
    public void RecorderAttachment_FailsClosedWhenAutomationIntentWasAlreadyAccepted()
    {
        var session = CreateFactory().Load(DesktopIntegratedOperationsProgram.Scenario);
        session.PlantControlAuthority.RequestAuthority(PlantControlAuthorityMode.Manual);

        var exception = Assert.Throws<InvalidOperationException>(() => new ScenarioRecorder(session));
        Assert.Contains("automation intents", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static ScenarioSessionFactory CreateFactory()
        => new(new VersionedInitialConditionRegistry(new IVersionedInitialConditionFactory[]
        {
            new DesktopIntegratedOperationsInitialConditionFactory(),
        }));
}
