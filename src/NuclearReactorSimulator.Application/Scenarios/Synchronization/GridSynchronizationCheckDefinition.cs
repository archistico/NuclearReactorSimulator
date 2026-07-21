namespace NuclearReactorSimulator.Application.Scenarios.Synchronization;

public sealed record GridSynchronizationCheckDefinition
{
    public GridSynchronizationCheckDefinition(
        string checkId,
        string title,
        string description,
        GridSynchronizationCheckCondition condition)
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
    public GridSynchronizationCheckCondition Condition { get; }
}
