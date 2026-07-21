namespace NuclearReactorSimulator.Application.Scenarios.PreStartup;

public sealed record PreStartupCheckDefinition
{
    public PreStartupCheckDefinition(
        string checkId,
        string title,
        string description,
        PreStartupCheckCondition condition)
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
    public PreStartupCheckCondition Condition { get; }
}
