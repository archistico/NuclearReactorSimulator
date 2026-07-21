namespace NuclearReactorSimulator.Application.Scenarios;

/// <summary>
/// Descriptive training objective metadata introduced by M7.1. M7.7 maps these stable IDs to separate deterministic observational evaluation criteria without adding physics ownership.
/// </summary>
public sealed record ScenarioObjectiveDefinition
{
    public ScenarioObjectiveDefinition(string objectiveId, string title, string description)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(objectiveId);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        ObjectiveId = objectiveId;
        Title = title;
        Description = description;
    }

    public string ObjectiveId { get; }

    public string Title { get; }

    public string Description { get; }
}
