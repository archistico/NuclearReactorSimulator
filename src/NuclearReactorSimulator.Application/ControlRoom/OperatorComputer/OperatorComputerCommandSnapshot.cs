using NuclearReactorSimulator.Application.ControlRoom;

namespace NuclearReactorSimulator.Application.ControlRoom.OperatorComputer;

public sealed record OperatorComputerCommandSnapshot
{
    public OperatorComputerCommandSnapshot(
        string entryId,
        OperatorComputerCommandGroup group,
        string displayName,
        ControlRoomCommand command,
        OperatorComputerCommandAvailability availability,
        string currentState,
        string? blockReason = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entryId);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        ArgumentNullException.ThrowIfNull(command);
        ArgumentException.ThrowIfNullOrWhiteSpace(currentState);

        if (availability == OperatorComputerCommandAvailability.Available && !string.IsNullOrWhiteSpace(blockReason))
        {
            throw new ArgumentException("Available operator-computer commands cannot declare a blocking reason.", nameof(blockReason));
        }

        if (availability != OperatorComputerCommandAvailability.Available && string.IsNullOrWhiteSpace(blockReason))
        {
            throw new ArgumentException("Blocked/unavailable operator-computer commands must explain why dispatch is not currently offered.", nameof(blockReason));
        }

        EntryId = entryId;
        Group = group;
        DisplayName = displayName;
        Command = command;
        Availability = availability;
        CurrentState = currentState;
        BlockReason = blockReason;
    }

    public string EntryId { get; }

    public OperatorComputerCommandGroup Group { get; }

    public string DisplayName { get; }

    public ControlRoomCommand Command { get; }

    public OperatorComputerCommandAvailability Availability { get; }

    public string CurrentState { get; }

    public string? BlockReason { get; }

    public bool CanDispatch => Availability == OperatorComputerCommandAvailability.Available;

    public string GroupText => Group.ToString().ToUpperInvariant();

    public string AvailabilityText => Availability switch
    {
        OperatorComputerCommandAvailability.Available => "AVAILABLE",
        OperatorComputerCommandAvailability.Blocked => "BLOCKED",
        OperatorComputerCommandAvailability.Unavailable => "UNAVAILABLE",
        _ => "UNKNOWN",
    };

    public string TargetText => Command.TargetId is null
        ? "GLOBAL"
        : $"{Command.TargetKind?.ToString().ToUpperInvariant() ?? "TARGET"}:{Command.TargetId}";

    public string ConsoleLine =>
        $"[{AvailabilityText}] {DisplayName} · {TargetText} · {CurrentState}" +
        (BlockReason is null ? string.Empty : $" · {BlockReason}");
}
