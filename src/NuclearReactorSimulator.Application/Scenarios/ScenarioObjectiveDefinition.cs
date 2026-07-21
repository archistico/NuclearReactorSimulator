namespace NuclearReactorSimulator.Application.Scenarios;

/// <summary>
/// Descriptive training objective. M7.1 stores objective metadata only; deterministic objective evaluation belongs to M7.7.
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
