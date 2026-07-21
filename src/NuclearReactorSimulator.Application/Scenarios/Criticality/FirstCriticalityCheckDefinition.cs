namespace NuclearReactorSimulator.Application.Scenarios.Criticality;

public sealed record FirstCriticalityCheckDefinition
{
    public FirstCriticalityCheckDefinition(
        string checkId,
        string title,
        string description,
        FirstCriticalityCheckCondition condition)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(checkId);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        if (!Enum.IsDefined(condition))
        {
            throw new ArgumentOutOfRangeException(nameof(condition));
        }

        CheckId = checkId;
        Title = title;
        Description = description;
        Condition = condition;
    }

    public string CheckId { get; }
    public string Title { get; }
    public string Description { get; }
    public FirstCriticalityCheckCondition Condition { get; }
}
