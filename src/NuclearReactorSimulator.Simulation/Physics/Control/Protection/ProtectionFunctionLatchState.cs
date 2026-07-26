namespace NuclearReactorSimulator.Simulation.Physics.Control.Protection;

/// <summary>Committed latch and pickup-timer state for one deterministic protection function.</summary>
public sealed record ProtectionFunctionLatchState
{
    public ProtectionFunctionLatchState(
        string functionId,
        bool isLatched,
        TimeSpan pickupElapsed = default)
    {
        if (string.IsNullOrWhiteSpace(functionId))
        {
            throw new ArgumentException("Protection-function latch id cannot be empty or whitespace.", nameof(functionId));
        }
        if (pickupElapsed < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(pickupElapsed), pickupElapsed, "Protection pickup elapsed time cannot be negative.");
        }

        FunctionId = functionId.Trim();
        IsLatched = isLatched;
        PickupElapsed = pickupElapsed;
    }

    public string FunctionId { get; }
    public bool IsLatched { get; }
    public TimeSpan PickupElapsed { get; }
}
