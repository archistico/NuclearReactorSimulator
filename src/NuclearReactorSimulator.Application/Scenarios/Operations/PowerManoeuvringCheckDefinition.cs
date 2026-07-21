namespace NuclearReactorSimulator.Application.Scenarios.Operations;

public sealed record PowerManoeuvringCheckDefinition
{
    public PowerManoeuvringCheckDefinition(string checkId, string title, string description, PowerManoeuvringCheckCondition condition)
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
    public PowerManoeuvringCheckCondition Condition { get; }
}
