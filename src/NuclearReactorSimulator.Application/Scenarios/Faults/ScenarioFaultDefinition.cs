using System.Collections.ObjectModel;

namespace NuclearReactorSimulator.Application.Scenarios.Faults;

/// <summary>
/// Explicit versioned-scenario fault input. The framework treats fault type/target/parameters as declarative data; concrete
/// physical or instrumentation effects are supplied only by milestone-specific applicators registered for the fault type.
/// </summary>
public sealed class ScenarioFaultDefinition
{
    private readonly IReadOnlyDictionary<string, string> _parameters;

    public ScenarioFaultDefinition(
        string faultId,
        string faultTypeId,
        string targetId,
        ScenarioFaultTriggerDefinition activation,
        ScenarioFaultTriggerDefinition? deactivation = null,
        IReadOnlyDictionary<string, string>? parameters = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(faultId);
        ArgumentException.ThrowIfNullOrWhiteSpace(faultTypeId);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetId);

        FaultId = faultId.Trim();
        FaultTypeId = faultTypeId.Trim();
        TargetId = targetId.Trim();
        Activation = activation ?? throw new ArgumentNullException(nameof(activation));
        Deactivation = deactivation;

        if (activation.Kind == ScenarioFaultTriggerKind.LogicalStep
            && deactivation?.Kind == ScenarioFaultTriggerKind.LogicalStep
            && deactivation.LogicalStep <= activation.LogicalStep)
        {
            throw new ArgumentException(
                "A logical-step deactivation must occur after the activation step.",
                nameof(deactivation));
        }

        var canonical = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var pair in parameters ?? new Dictionary<string, string>())
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(pair.Key);
            if (pair.Value is null)
            {
                throw new ArgumentException("Fault parameter values cannot be null.", nameof(parameters));
            }

            canonical.Add(pair.Key.Trim(), pair.Value);
        }

        _parameters = new ReadOnlyDictionary<string, string>(canonical);
    }

    public string FaultId { get; }

    public string FaultTypeId { get; }

    public string TargetId { get; }

    public ScenarioFaultTriggerDefinition Activation { get; }

    public ScenarioFaultTriggerDefinition? Deactivation { get; }

    public IReadOnlyDictionary<string, string> Parameters => _parameters;
}
