namespace NuclearReactorSimulator.Domain.Physics.Fluids;

/// <summary>
/// Immutable operational state of a pump. Speed is the commanded/mechanical speed;
/// when the pump is not running the effective hydraulic speed is zero.
/// </summary>
public sealed record PumpState
{
    public PumpState(string pumpId, PumpSpeed speed, bool isRunning = true)
    {
        if (string.IsNullOrWhiteSpace(pumpId))
        {
            throw new ArgumentException("A pump state identifier cannot be empty or whitespace.", nameof(pumpId));
        }

        PumpId = pumpId.Trim();
        Speed = speed;
        IsRunning = isRunning;
    }

    public string PumpId { get; }

    public PumpSpeed Speed { get; }

    public bool IsRunning { get; }
}
