namespace NuclearReactorSimulator.Application.Scenarios;

/// <summary>Exact-version resolver for deterministic initial-condition factories.</summary>
public sealed class VersionedInitialConditionRegistry
{
    private readonly IReadOnlyDictionary<InitialConditionReference, IVersionedInitialConditionFactory> _factories;

    public VersionedInitialConditionRegistry(IEnumerable<IVersionedInitialConditionFactory> factories)
    {
        ArgumentNullException.ThrowIfNull(factories);

        var dictionary = new Dictionary<InitialConditionReference, IVersionedInitialConditionFactory>();
        foreach (var factory in factories)
        {
            ArgumentNullException.ThrowIfNull(factory);
            ArgumentNullException.ThrowIfNull(factory.Descriptor);
            ArgumentNullException.ThrowIfNull(factory.Descriptor.Reference);
            if (!dictionary.TryAdd(factory.Descriptor.Reference, factory))
            {
                throw new ArgumentException(
                    $"Duplicate initial-condition factory '{factory.Descriptor.Reference.InitialConditionId}' version {factory.Descriptor.Reference.Version}.",
                    nameof(factories));
            }
        }

        _factories = dictionary;
    }

    public IReadOnlyList<InitialConditionDescriptor> Descriptors =>
        _factories.Values
            .Select(static factory => factory.Descriptor)
            .OrderBy(static descriptor => descriptor.Reference.InitialConditionId, StringComparer.Ordinal)
            .ThenBy(static descriptor => descriptor.Reference.Version)
            .ToArray();

    public IVersionedInitialConditionFactory Resolve(InitialConditionReference reference)
    {
        ArgumentNullException.ThrowIfNull(reference);
        if (_factories.TryGetValue(reference, out var factory))
        {
            return factory;
        }

        throw new KeyNotFoundException(
            $"Initial condition '{reference.InitialConditionId}' version {reference.Version} is not registered. Exact-version loading is required.");
    }
}
