using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios.Criticality;
using NuclearReactorSimulator.Application.Scenarios.Operations;
using NuclearReactorSimulator.Application.Scenarios.PreStartup;
using NuclearReactorSimulator.Application.Scenarios.Startup;
using NuclearReactorSimulator.Application.Scenarios.Synchronization;
using NuclearReactorSimulator.Application.Scenarios.Training;

namespace NuclearReactorSimulator.Application.ControlRoom.OperatorComputer;

public static class OperatorComputerScenarioContentProjector
{
    public static OperatorComputerScenarioContentSnapshot Project(
        ControlRoomSnapshot snapshot,
        PreStartupGuidancePlan plan,
        PreStartupChecklistEvaluator evaluator,
        TrainingGuidanceMode guidanceMode = TrainingGuidanceMode.Guided,
        ScenarioTrainingAssessment? trainingAssessment = null)
    {
        var results = evaluator.Evaluate(snapshot, plan.Checks);
        return Build(
            "PRE-STARTUP PREPARATION",
            guidanceMode,
            plan.Steps.Select(static step => new Step(step.StepId, step.Sequence, step.Title, step.Instruction, step.RequiredCheckIds)),
            results.Select(static result => new Check(result.Definition.CheckId, result.Definition.Title, result.IsSatisfied, result.Observation)),
            trainingAssessment);
    }

    public static OperatorComputerScenarioContentSnapshot Project(
        ControlRoomSnapshot snapshot,
        FirstCriticalityGuidancePlan plan,
        FirstCriticalityChecklistEvaluator evaluator,
        TrainingGuidanceMode guidanceMode = TrainingGuidanceMode.Guided,
        ScenarioTrainingAssessment? trainingAssessment = null)
    {
        var results = evaluator.Evaluate(snapshot, plan.Checks);
        return Build(
            "FIRST CRITICALITY / LOW POWER",
            guidanceMode,
            plan.Steps.Select(static step => new Step(step.StepId, step.Sequence, step.Title, step.Instruction, step.RequiredCheckIds)),
            results.Select(static result => new Check(result.Definition.CheckId, result.Definition.Title, result.IsSatisfied, result.Observation)),
            trainingAssessment);
    }

    public static OperatorComputerScenarioContentSnapshot Project(
        ControlRoomSnapshot snapshot,
        HeatUpTurbineStartupGuidancePlan plan,
        HeatUpTurbineStartupChecklistEvaluator evaluator,
        TrainingGuidanceMode guidanceMode = TrainingGuidanceMode.Guided,
        ScenarioTrainingAssessment? trainingAssessment = null)
    {
        var results = evaluator.Evaluate(snapshot, plan.Checks);
        return Build(
            "HEAT-UP / STEAM RAISING / TURBINE STARTUP",
            guidanceMode,
            plan.Steps.Select(static step => new Step(step.StepId, step.Sequence, step.Title, step.Instruction, step.RequiredCheckIds)),
            results.Select(static result => new Check(result.Definition.CheckId, result.Definition.Title, result.IsSatisfied, result.Observation)),
            trainingAssessment);
    }

    public static OperatorComputerScenarioContentSnapshot Project(
        ControlRoomSnapshot snapshot,
        GridSynchronizationGuidancePlan plan,
        GridSynchronizationChecklistEvaluator evaluator,
        TrainingGuidanceMode guidanceMode = TrainingGuidanceMode.Guided,
        ScenarioTrainingAssessment? trainingAssessment = null)
    {
        var results = evaluator.Evaluate(snapshot, plan.Checks);
        return Build(
            "GRID SYNCHRONIZATION / INITIAL LOADING",
            guidanceMode,
            plan.Steps.Select(static step => new Step(step.StepId, step.Sequence, step.Title, step.Instruction, step.RequiredCheckIds)),
            results.Select(static result => new Check(result.Definition.CheckId, result.Definition.Title, result.IsSatisfied, result.Observation)),
            trainingAssessment);
    }

    public static OperatorComputerScenarioContentSnapshot Project(
        ControlRoomSnapshot snapshot,
        PowerManoeuvringGuidancePlan plan,
        PowerManoeuvringChecklistEvaluator evaluator,
        TrainingGuidanceMode guidanceMode = TrainingGuidanceMode.Guided,
        ScenarioTrainingAssessment? trainingAssessment = null)
    {
        var results = evaluator.Evaluate(snapshot, plan.Checks);
        return Build(
            "POWER MANOEUVRING / NORMAL SHUTDOWN",
            guidanceMode,
            plan.Steps.Select(static step => new Step(step.StepId, step.Sequence, step.Title, step.Instruction, step.RequiredCheckIds)),
            results.Select(static result => new Check(result.Definition.CheckId, result.Definition.Title, result.IsSatisfied, result.Observation)),
            trainingAssessment);
    }

    private static OperatorComputerScenarioContentSnapshot Build(
        string procedureTitle,
        TrainingGuidanceMode guidanceMode,
        IEnumerable<Step> steps,
        IEnumerable<Check> checks,
        ScenarioTrainingAssessment? trainingAssessment)
    {
        var stepArray = steps.OrderBy(static step => step.Sequence).ToArray();
        var checkArray = checks.ToArray();
        var checkById = checkArray.ToDictionary(static check => check.Id, StringComparer.Ordinal);

        var completed = stepArray.Select(step => step.RequiredCheckIds.Count > 0
            && step.RequiredCheckIds.All(id => checkById.TryGetValue(id, out var result) && result.IsSatisfied)).ToArray();
        var currentIndex = Array.FindIndex(completed, static value => !value);
        if (currentIndex < 0 && completed.Length > 0)
        {
            currentIndex = completed.Length - 1;
        }

        var projectedSteps = guidanceMode == TrainingGuidanceMode.Guided
            ? stepArray.Select((step, index) => new OperatorComputerGuidanceStepSnapshot(
                step.Id,
                step.Sequence,
                step.Title,
                step.Instruction,
                completed[index]
                    ? OperatorComputerGuidanceStepState.Completed
                    : index == currentIndex
                        ? OperatorComputerGuidanceStepState.Current
                        : OperatorComputerGuidanceStepState.Pending)).ToArray()
            : Array.Empty<OperatorComputerGuidanceStepSnapshot>();

        var guidanceSummary = guidanceMode switch
        {
            TrainingGuidanceMode.Hidden => "Procedure guidance is hidden. Diagnostic evaluation remains observational and unchanged.",
            TrainingGuidanceMode.ChecklistOnly => "Checklist-only assistance is active. Step-by-step procedure text is intentionally suppressed.",
            TrainingGuidanceMode.Guided when stepArray.Length == 0 => "No procedure steps are defined for the active guidance plan.",
            TrainingGuidanceMode.Guided when completed.All(static value => value) => "All procedure-step conditions are currently satisfied.",
            TrainingGuidanceMode.Guided => $"Current procedure step: {stepArray[currentIndex].Sequence}. {stepArray[currentIndex].Title}",
            _ => "Guidance mode unavailable.",
        };

        var diagnostics = new OperatorComputerDiagnosticsSnapshot(
            procedureTitle,
            checkArray.Select(static check => new OperatorComputerDiagnosticItemSnapshot(check.Id, check.Title, check.IsSatisfied, check.Observation)));

        var trainingCheckpoints = trainingAssessment?.Checkpoints.Select(static checkpoint =>
            new OperatorComputerTrainingCheckpointSnapshot(
                checkpoint.Definition.CheckpointId,
                checkpoint.Definition.Title,
                checkpoint.IsSatisfied,
                checkpoint.FirstSatisfiedLogicalStep,
                checkpoint.Observation));
        var scoreText = trainingAssessment is null
            ? null
            : $"SCORE {trainingAssessment.TotalScore}/{trainingAssessment.MaximumScore} · PENALTIES {trainingAssessment.PenaltyPoints}";

        return new OperatorComputerScenarioContentSnapshot(
            new OperatorComputerGuidanceSnapshot(
                procedureTitle,
                guidanceMode,
                projectedSteps,
                guidanceSummary,
                trainingCheckpoints,
                scoreText),
            diagnostics);
    }

    private sealed record Step(string Id, int Sequence, string Title, string Instruction, IReadOnlyList<string> RequiredCheckIds);
    private sealed record Check(string Id, string Title, bool IsSatisfied, string Observation);
}
