namespace NuclearReactorSimulator.Domain.Physics.Fluids;

/// <summary>
/// Immutable operational state of a valve. Position is the last mechanical position;
/// when fail-safe is active the configured fail-safe action determines effective position.
/// </summary>
public sealed record ValveState
{
    public ValveState(string valveId, ValvePosition position, bool isFailSafeActive = false)
    {
        if (string.IsNullOrWhiteSpace(valveId))
        {
            throw new ArgumentException("A valve state identifier cannot be empty or whitespace.", nameof(valveId));
        }

        ValveId = valveId.Trim();
        Position = position;
        IsFailSafeActive = isFailSafeActive;
    }

    public string ValveId { get; }

    public ValvePosition Position { get; }

    public bool IsFailSafeActive { get; }
}
