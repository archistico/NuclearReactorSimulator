namespace NuclearReactorSimulator.Application.Scenarios.Training;

/// <summary>Historical plant-state checkpoint evaluated from immutable presentation snapshots.</summary>
public sealed record TrainingCheckpointDefinition
{
    private readonly IReadOnlyList<string> _requiredPriorCheckpointIds;

    public TrainingCheckpointDefinition(
        string checkpointId,
        string title,
        string description,
        string sourceCheckId,
        IEnumerable<string>? requiredPriorCheckpointIds = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(checkpointId);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceCheckId);
        var prerequisites = (requiredPriorCheckpointIds ?? Array.Empty<string>()).ToArray();
        if (prerequisites.Any(string.IsNullOrWhiteSpace)
            || prerequisites.Distinct(StringComparer.Ordinal).Count() != prerequisites.Length
            || prerequisites.Contains(checkpointId, StringComparer.Ordinal))
        {
            throw new ArgumentException("Checkpoint prerequisites must be non-empty, unique and may not reference the checkpoint itself.", nameof(requiredPriorCheckpointIds));
        }

        CheckpointId = checkpointId;
        Title = title;
        Description = description;
        SourceCheckId = sourceCheckId;
        _requiredPriorCheckpointIds = Array.AsReadOnly(prerequisites);
    }

    public string CheckpointId { get; }
    public string Title { get; }
    public string Description { get; }
    public string SourceCheckId { get; }
    public IReadOnlyList<string> RequiredPriorCheckpointIds => _requiredPriorCheckpointIds;
}
