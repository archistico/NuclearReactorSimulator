using NuclearReactorSimulator.Application.ControlRoom;

namespace NuclearReactorSimulator.Application.Scenarios.Criticality;

public sealed class FirstCriticalityStepDefinition
{
    private readonly IReadOnlyList<string> _requiredCheckIds;

    public FirstCriticalityStepDefinition(
        string stepId,
        int sequence,
        string title,
        string instruction,
        IEnumerable<string> requiredCheckIds,
        ControlRoomCommandKind? suggestedOperatorAction = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(stepId);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(instruction);
        ArgumentNullException.ThrowIfNull(requiredCheckIds);
        if (sequence <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sequence));
        }
        if (suggestedOperatorAction.HasValue && !Enum.IsDefined(suggestedOperatorAction.Value))
        {
            throw new ArgumentOutOfRangeException(nameof(suggestedOperatorAction));
        }

        var checkIds = requiredCheckIds.Select(static id =>
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(id);
            return id;
        }).ToArray();
        if (checkIds.Distinct(StringComparer.Ordinal).Count() != checkIds.Length)
        {
            throw new ArgumentException("Required check IDs must be unique within a guidance step.", nameof(requiredCheckIds));
        }

        StepId = stepId;
        Sequence = sequence;
        Title = title;
        Instruction = instruction;
        _requiredCheckIds = Array.AsReadOnly(checkIds);
        SuggestedOperatorAction = suggestedOperatorAction;
    }

    public string StepId { get; }
    public int Sequence { get; }
    public string Title { get; }
    public string Instruction { get; }
    public IReadOnlyList<string> RequiredCheckIds => _requiredCheckIds;
    public ControlRoomCommandKind? SuggestedOperatorAction { get; }
}
