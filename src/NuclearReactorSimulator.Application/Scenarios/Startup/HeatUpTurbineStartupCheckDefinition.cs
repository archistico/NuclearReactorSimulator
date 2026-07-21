namespace NuclearReactorSimulator.Application.Scenarios.Startup;

public sealed record HeatUpTurbineStartupCheckDefinition
{
    public HeatUpTurbineStartupCheckDefinition(
        string checkId,
        string title,
        string description,
        HeatUpTurbineStartupCheckCondition condition)
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
    public HeatUpTurbineStartupCheckCondition Condition { get; }
}
