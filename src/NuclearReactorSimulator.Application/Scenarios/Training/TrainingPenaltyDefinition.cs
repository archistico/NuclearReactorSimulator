using NuclearReactorSimulator.Application.ControlRoom;

namespace NuclearReactorSimulator.Application.Scenarios.Training;

/// <summary>One deterministic scoring penalty triggered by the first accepted occurrence of a declared operator action.</summary>
public sealed record TrainingPenaltyDefinition
{
    public TrainingPenaltyDefinition(string penaltyId, string title, string description, ControlRoomCommandKind triggerAction, int points)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(penaltyId);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        if (!Enum.IsDefined(triggerAction))
        {
            throw new ArgumentOutOfRangeException(nameof(triggerAction));
        }
        if (points <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(points));
        }

        PenaltyId = penaltyId;
        Title = title;
        Description = description;
        TriggerAction = triggerAction;
        Points = points;
    }

    public string PenaltyId { get; }
    public string Title { get; }
    public string Description { get; }
    public ControlRoomCommandKind TriggerAction { get; }
    public int Points { get; }
}
