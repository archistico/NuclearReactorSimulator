namespace NuclearReactorSimulator.Domain.Physics.Fluids;

/// <summary>
/// Immutable valve definition composed over an existing passive hydraulic path.
/// The wrapped pipe resistance represents the fully-open hydraulic resistance.
/// </summary>
public sealed record ValveDefinition
{
    public ValveDefinition(
        string id,
        PipeDefinition pipe,
        ValveCharacteristic characteristic,
        ValveFailSafeAction failSafeAction)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("A valve identifier cannot be empty or whitespace.", nameof(id));
        }

        ArgumentNullException.ThrowIfNull(pipe);
        ArgumentNullException.ThrowIfNull(characteristic);

        if (!Enum.IsDefined(typeof(ValveFailSafeAction), failSafeAction))
        {
            throw new ArgumentOutOfRangeException(nameof(failSafeAction), failSafeAction, "Unknown valve fail-safe action.");
        }

        Id = id.Trim();
        Pipe = pipe;
        Characteristic = characteristic;
        FailSafeAction = failSafeAction;
    }

    public string Id { get; }

    public PipeDefinition Pipe { get; }

    public ValveCharacteristic Characteristic { get; }

    public ValveFailSafeAction FailSafeAction { get; }
}
