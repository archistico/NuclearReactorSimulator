using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios.PreStartup;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.Scenarios.PreStartup;

public sealed class ColdShutdownPreStartupProgramTests
{
    [Fact]
    public void Scenario_PinsExactV1InitialConditionAndStopsBeforeCriticalityActions()
    {
        var scenario = ColdShutdownPreStartupProgram.Scenario;

        Assert.Equal("cold-shutdown-pre-start", scenario.InitialCondition.InitialConditionId);
        Assert.Equal(1, scenario.InitialCondition.Version);
        Assert.Contains(ControlRoomCommandKind.MainCirculationPumpStart, scenario.AllowedOperatorActions);
        Assert.Contains(ControlRoomCommandKind.ReactorScram, scenario.AllowedOperatorActions);
        Assert.DoesNotContain(ControlRoomCommandKind.ControlRodWithdraw, scenario.AllowedOperatorActions);
        Assert.DoesNotContain(ControlRoomCommandKind.GeneratorBreakerClose, scenario.AllowedOperatorActions);
    }

    [Fact]
    public void Guidance_IsOrderedDeclarativeAndRequiresCirculationBeforePrecriticalityHandoff()
    {
        var guidance = ColdShutdownPreStartupProgram.Guidance;

        Assert.Equal(new[] { 1, 2, 3, 4 }, guidance.Steps.Select(static step => step.Sequence));
        var circulation = guidance.Steps.Single(static step => step.StepId == "establish-main-circulation");
        Assert.Equal(ControlRoomCommandKind.MainCirculationPumpStart, circulation.SuggestedOperatorAction);
        Assert.Contains("mcp-running", circulation.RequiredCheckIds);

        var handoff = guidance.Steps.Single(static step => step.StepId == "precriticality-handoff");
        Assert.Contains("rods-inserted", handoff.RequiredCheckIds);
        Assert.Contains("mcp-running", handoff.RequiredCheckIds);
        Assert.Contains("breakers-open", handoff.RequiredCheckIds);
    }

    [Fact]
    public void ChecklistEvaluator_ObservesColdShutdownAndPreparedCirculationWithoutMutatingState()
    {
        var evaluator = new PreStartupChecklistEvaluator();
        var baseline = CreateSnapshot(pumpRunning: false);
        var prepared = CreateSnapshot(pumpRunning: true);

        Assert.True(evaluator.Evaluate(baseline, Check("reactor", PreStartupCheckCondition.ReactorShutdown)).IsSatisfied);
        Assert.True(evaluator.Evaluate(baseline, Check("rods", PreStartupCheckCondition.ControlRodsInserted)).IsSatisfied);
        Assert.True(evaluator.Evaluate(baseline, Check("pump-stop", PreStartupCheckCondition.MainCirculationPumpsStopped)).IsSatisfied);
        Assert.False(evaluator.Evaluate(baseline, Check("pump-run", PreStartupCheckCondition.MainCirculationPumpsRunning)).IsSatisfied);
        Assert.True(evaluator.Evaluate(prepared, Check("pump-run", PreStartupCheckCondition.MainCirculationPumpsRunning)).IsSatisfied);
        Assert.True(evaluator.Evaluate(baseline, Check("isolation", PreStartupCheckCondition.SteamIsolationClosed)).IsSatisfied);
        Assert.True(evaluator.Evaluate(baseline, Check("breaker", PreStartupCheckCondition.GeneratorBreakersOpen)).IsSatisfied);
    }

    private static PreStartupCheckDefinition Check(string id, PreStartupCheckCondition condition)
        => new(id, id, id, condition);

    private static ControlRoomSnapshot CreateSnapshot(bool pumpRunning)
    {
        var normal = new ControlRoomValueSnapshot("0", "", 0d, ControlRoomVisualState.Normal);
        var reactor = new ReactorCorePanelSnapshot(
            new ControlRoomValueSnapshot("0.000", "MWth", 0d, ControlRoomVisualState.Normal),
            ControlRoomValueSnapshot.Unavailable("s"),
            normal,
            normal,
            normal,
            new ControlRoomValueSnapshot("0.0", "%", 0d, ControlRoomVisualState.Normal),
            ControlRoomValueSnapshot.Unavailable("pcm"),
            Array.Empty<ReactorCoreZonePresentationSnapshot>(),
            new[] { new ReactorRodPresentationSnapshot("rod-1", 0d, "HOLD", ControlRoomVisualState.Normal) },
            Array.Empty<ReactorRodTargetPresentationSnapshot>(),
            false,
            false);
        var pump = new PrimaryCircuitPumpPresentationSnapshot(
            "pump", "loop", pumpRunning,
            new ControlRoomValueSnapshot(pumpRunning ? "100" : "0", "%", pumpRunning ? 100d : 0d, ControlRoomVisualState.Normal),
            normal,
            normal,
            true);
        var primary = new PrimaryCircuitPanelSnapshot(
            new[]
            {
                new PrimaryCircuitLoopPresentationSnapshot(
                    "loop", normal, normal, normal, normal, "STABLE", new[] { pump }, Array.Empty<PrimaryCircuitBranchPresentationSnapshot>()),
            },
            Array.Empty<PrimaryCircuitSteamDrumPresentationSnapshot>(),
            Array.Empty<PrimaryCircuitValvePresentationSnapshot>(),
            normal,
            normal,
            normal);
        var turbine = new TurbineSecondaryPanelSnapshot(
            Array.Empty<MainSteamLinePresentationSnapshot>(),
            new[]
            {
                new TurbineAdmissionTrainPresentationSnapshot(
                    "train", "header", "inlet", "stop", normal, "control", normal, "admission", normal,
                    normal, normal, normal, "VAPOR"),
            },
            new[] { new TurbineRotorPresentationSnapshot("rotor", new ControlRoomValueSnapshot("0", "rpm", 0d, ControlRoomVisualState.Normal), normal, normal, false, false) },
            Array.Empty<TurbineStageGroupPresentationSnapshot>(),
            Array.Empty<CondenserPresentationSnapshot>(),
            Array.Empty<FeedwaterTrainPresentationSnapshot>(),
            normal,
            normal,
            normal,
            false);
        var electrical = new ElectricalPanelSnapshot(
            ElectricalGridPresentationSnapshot.Unavailable,
            new[]
            {
                new GeneratorPresentationSnapshot(
                    "generator", "rotor", "breaker", normal, normal, normal, normal, normal, normal, normal,
                    false, false, false, false),
            },
            normal,
            false);

        return new ControlRoomSnapshot(
            0,
            ControlRoomRunState.Paused,
            6,
            0,
            0,
            0,
            false,
            false,
            false,
            reactor,
            primary,
            turbine,
            electrical,
            AlarmEventsPanelSnapshot.Unavailable);
    }
}
