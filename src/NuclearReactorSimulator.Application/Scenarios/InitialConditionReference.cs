namespace NuclearReactorSimulator.Application.Scenarios;

/// <summary>
/// Stable reference to one exact version of an initial-condition recipe. Semantic revisions always create a new version;
/// an existing version is immutable so scenario loading and replay can resolve the same runtime seed deterministically.
/// </summary>
public sealed record InitialConditionReference
{
    public InitialConditionReference(string initialConditionId, int version)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(initialConditionId);
        if (version <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(version), "Initial-condition versions must be positive.");
        }

        InitialConditionId = initialConditionId;
        Version = version;
    }

    public string InitialConditionId { get; }

    public int Version { get; }
}
