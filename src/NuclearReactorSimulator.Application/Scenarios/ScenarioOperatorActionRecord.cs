using NuclearReactorSimulator.Application.ControlRoom;

namespace NuclearReactorSimulator.Application.Scenarios;

/// <summary>One operator action accepted by the scenario command gate, ordered only by deterministic logical sequence.</summary>
public sealed record ScenarioOperatorActionRecord
{
    public ScenarioOperatorActionRecord(long sequence, long logicalStep, ControlRoomCommand command)
    {
        if (sequence <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sequence));
        }
        if (logicalStep < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(logicalStep));
        }

        Sequence = sequence;
        LogicalStep = logicalStep;
        Command = command ?? throw new ArgumentNullException(nameof(command));
    }

    public long Sequence { get; }

    public long LogicalStep { get; }

    public ControlRoomCommand Command { get; }
}
