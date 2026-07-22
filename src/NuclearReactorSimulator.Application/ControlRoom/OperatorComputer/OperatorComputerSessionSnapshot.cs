using System.Collections.ObjectModel;

namespace NuclearReactorSimulator.Application.ControlRoom.OperatorComputer;

public sealed record OperatorComputerSessionSnapshot
{
    public OperatorComputerSessionSnapshot(
        bool recorderActive,
        string scenarioId,
        string scenarioTitle,
        string initialConditionText,
        long logicalStep,
        int recordedFrameCount,
        IEnumerable<OperatorComputerSessionCheckpointSnapshot> checkpoints)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scenarioId);
        ArgumentException.ThrowIfNullOrWhiteSpace(scenarioTitle);
        ArgumentException.ThrowIfNullOrWhiteSpace(initialConditionText);
        if (logicalStep < 0 || recordedFrameCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(logicalStep));
        }

        RecorderActive = recorderActive;
        ScenarioId = scenarioId.Trim();
        ScenarioTitle = scenarioTitle.Trim();
        InitialConditionText = initialConditionText.Trim();
        LogicalStep = logicalStep;
        RecordedFrameCount = recordedFrameCount;
        Checkpoints = new ReadOnlyCollection<OperatorComputerSessionCheckpointSnapshot>(
            (checkpoints ?? throw new ArgumentNullException(nameof(checkpoints))).ToArray());
    }

    public bool RecorderActive { get; }
    public string ScenarioId { get; }
    public string ScenarioTitle { get; }
    public string InitialConditionText { get; }
    public long LogicalStep { get; }
    public int RecordedFrameCount { get; }
    public IReadOnlyList<OperatorComputerSessionCheckpointSnapshot> Checkpoints { get; }
    public string RecorderStateText => RecorderActive ? "ACTIVE" : "INACTIVE";
}
