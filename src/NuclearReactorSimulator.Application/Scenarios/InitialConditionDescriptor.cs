namespace NuclearReactorSimulator.Application.Scenarios;

/// <summary>Human-readable metadata for one immutable initial-condition version.</summary>
public sealed record InitialConditionDescriptor
{
    public InitialConditionDescriptor(
        InitialConditionReference reference,
        string displayName,
        string description)
    {
        Reference = reference ?? throw new ArgumentNullException(nameof(reference));
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        DisplayName = displayName;
        Description = description;
    }

    public InitialConditionReference Reference { get; }

    public string DisplayName { get; }

    public string Description { get; }
}
