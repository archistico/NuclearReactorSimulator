using System.Collections.ObjectModel;
using NuclearReactorSimulator.Domain.Physics.Instrumentation;

namespace NuclearReactorSimulator.Domain.Physics.Control;

/// <summary>Canonical M5.2 controller set bound only to the measured-signal definition.</summary>
public sealed class ControlSystemDefinition
{
    public ControlSystemDefinition(
        string id,
        InstrumentationSystemDefinition instrumentation,
        IEnumerable<PidControllerDefinition> controllers)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Control-system id cannot be empty or whitespace.", nameof(id));
        }

        Instrumentation = instrumentation ?? throw new ArgumentNullException(nameof(instrumentation));
        ArgumentNullException.ThrowIfNull(controllers);

        var canonical = controllers
            .Select(item => item ?? throw new ArgumentException("Controller definitions cannot contain null entries.", nameof(controllers)))
            .OrderBy(static item => item.Id, StringComparer.Ordinal)
            .ToArray();
        if (canonical.Length == 0)
        {
            throw new ArgumentException("A control system must contain at least one controller.", nameof(controllers));
        }

        if (canonical.Select(static item => item.Id).Distinct(StringComparer.Ordinal).Count() != canonical.Length)
        {
            throw new ArgumentException("Controller ids must be unique.", nameof(controllers));
        }

        var channelIds = instrumentation.Channels.Select(static channel => channel.Id).ToHashSet(StringComparer.Ordinal);
        foreach (var controller in canonical)
        {
            if (!channelIds.Contains(controller.MeasurementChannelId))
            {
                throw new ArgumentException(
                    $"Controller '{controller.Id}' references unknown measured channel '{controller.MeasurementChannelId}'.",
                    nameof(controllers));
            }
        }

        Id = id.Trim();
        Controllers = new ReadOnlyCollection<PidControllerDefinition>(canonical);
    }

    public string Id { get; }
    public InstrumentationSystemDefinition Instrumentation { get; }
    public IReadOnlyList<PidControllerDefinition> Controllers { get; }

    public PidControllerDefinition GetController(string id)
        => Controllers.FirstOrDefault(item => string.Equals(item.Id, id, StringComparison.Ordinal))
            ?? throw new KeyNotFoundException($"Unknown controller '{id}'.");
}
