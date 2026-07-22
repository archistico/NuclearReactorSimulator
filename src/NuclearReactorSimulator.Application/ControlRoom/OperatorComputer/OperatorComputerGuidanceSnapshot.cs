using System.Collections.ObjectModel;
using NuclearReactorSimulator.Application.Scenarios.Training;

namespace NuclearReactorSimulator.Application.ControlRoom.OperatorComputer;

public sealed record OperatorComputerGuidanceSnapshot
{
    public OperatorComputerGuidanceSnapshot(
        string procedureTitle,
        TrainingGuidanceMode guidanceMode,
        IEnumerable<OperatorComputerGuidanceStepSnapshot> steps,
        string summary,
        IEnumerable<OperatorComputerTrainingCheckpointSnapshot>? trainingCheckpoints = null,
        string? trainingScoreText = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(procedureTitle);
        if (!Enum.IsDefined(guidanceMode))
        {
            throw new ArgumentOutOfRangeException(nameof(guidanceMode));
        }
        ArgumentNullException.ThrowIfNull(steps);
        ArgumentException.ThrowIfNullOrWhiteSpace(summary);

        ProcedureTitle = procedureTitle;
        GuidanceMode = guidanceMode;
        Steps = new ReadOnlyCollection<OperatorComputerGuidanceStepSnapshot>(steps.ToArray());
        Summary = summary;
        TrainingCheckpoints = new ReadOnlyCollection<OperatorComputerTrainingCheckpointSnapshot>(
            (trainingCheckpoints ?? Array.Empty<OperatorComputerTrainingCheckpointSnapshot>()).ToArray());
        TrainingScoreText = trainingScoreText;
    }

    public string ProcedureTitle { get; }
    public TrainingGuidanceMode GuidanceMode { get; }
    public IReadOnlyList<OperatorComputerGuidanceStepSnapshot> Steps { get; }
    public string Summary { get; }
    public IReadOnlyList<OperatorComputerTrainingCheckpointSnapshot> TrainingCheckpoints { get; }
    public string? TrainingScoreText { get; }
    public bool IsStepByStepVisible => GuidanceMode == TrainingGuidanceMode.Guided;
    public bool IsChecklistVisible => GuidanceMode is TrainingGuidanceMode.Guided or TrainingGuidanceMode.ChecklistOnly;
}
