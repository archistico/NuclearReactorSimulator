using NuclearReactorSimulator.Application.ControlRoom;

namespace NuclearReactorSimulator.Application.Scenarios.PreStartup;

/// <summary>
/// Declarative operator guidance. A step may suggest an operator action, but guidance never dispatches it automatically.
/// Completion is based only on named observational readiness checks.
/// </summary>
public sealed record PreStartupPreparationStepDefinition
{
    private readonly IReadOnlyList<string> _requiredCheckIds;

    public PreStartupPreparationStepDefinition(
        string stepId,
        int sequence,
        string title,
        string instruction,
        IEnumerable<string> requiredCheckIds,
        ControlRoomCommandKind? suggestedOperatorAction = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(stepId);
        if (sequence <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sequence));
        }
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(instruction);
        ArgumentNullException.ThrowIfNull(requiredCheckIds);
        if (suggestedOperatorAction.HasValue && !Enum.IsDefined(suggestedOperatorAction.Value))
        {
            throw new ArgumentOutOfRangeException(nameof(suggestedOperatorAction));
        }

        var ids = requiredCheckIds.Select(static id =>
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(id);
            return id;
        }).Distinct(StringComparer.Ordinal).ToArray();

        StepId = stepId;
        Sequence = sequence;
        Title = title;
        Instruction = instruction;
        _requiredCheckIds = Array.AsReadOnly(ids);
        SuggestedOperatorAction = suggestedOperatorAction;
    }

    public string StepId { get; }
    public int Sequence { get; }
    public string Title { get; }
    public string Instruction { get; }
    public IReadOnlyList<string> RequiredCheckIds => _requiredCheckIds;
    public ControlRoomCommandKind? SuggestedOperatorAction { get; }
}
