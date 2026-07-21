using NuclearReactorSimulator.Application.ControlRoom;

namespace NuclearReactorSimulator.Application.Scenarios.Faults;

/// <summary>Exact fault-type registry. Missing effect handlers fail session loading closed.</summary>
public sealed class ScenarioFaultApplicatorRegistry
{
    private readonly IReadOnlyDictionary<string, IScenarioFaultApplicatorFactory> _factories;

    public ScenarioFaultApplicatorRegistry(IEnumerable<IScenarioFaultApplicatorFactory>? factories = null)
    {
        var byType = new Dictionary<string, IScenarioFaultApplicatorFactory>(StringComparer.Ordinal);
        foreach (var factory in factories ?? Array.Empty<IScenarioFaultApplicatorFactory>())
        {
            ArgumentNullException.ThrowIfNull(factory);
            ArgumentException.ThrowIfNullOrWhiteSpace(factory.FaultTypeId);
            if (!byType.TryAdd(factory.FaultTypeId, factory))
            {
                throw new ArgumentException($"Duplicate scenario fault applicator factory '{factory.FaultTypeId}'.", nameof(factories));
            }
        }

        _factories = byType;
    }

    public IReadOnlyDictionary<string, IScenarioFaultApplicator> Bind(
        IControlRoomRuntimeEngine runtimeEngine,
        IEnumerable<ScenarioFaultDefinition> faults)
    {
        ArgumentNullException.ThrowIfNull(runtimeEngine);
        ArgumentNullException.ThrowIfNull(faults);

        var byType = new Dictionary<string, IScenarioFaultApplicator>(StringComparer.Ordinal);
        foreach (var faultTypeId in faults.Select(static fault => fault.FaultTypeId).Distinct(StringComparer.Ordinal))
        {
            if (!_factories.TryGetValue(faultTypeId, out var factory))
            {
                throw new KeyNotFoundException($"No scenario fault applicator is registered for fault type '{faultTypeId}'.");
            }

            byType.Add(
                faultTypeId,
                factory.Create(runtimeEngine)
                    ?? throw new InvalidOperationException($"Fault applicator factory '{faultTypeId}' returned null."));
        }

        return byType;
    }

    public static ScenarioFaultApplicatorRegistry Empty { get; } = new();
}
