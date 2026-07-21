using System.Collections.ObjectModel;
using NuclearReactorSimulator.Domain.Physics.Instrumentation;

namespace NuclearReactorSimulator.Simulation.Physics.Instrumentation;

/// <summary>Immutable M5.1 instrumentation result separating controller-facing measurements from diagnostic true values.</summary>
public sealed class InstrumentationSnapshot
{
    public InstrumentationSnapshot(
        InstrumentationSystemDefinition definition,
        MeasuredSignalFrame measuredSignals,
        IEnumerable<InstrumentChannelDiagnosticSnapshot> diagnostics)
    {
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        MeasuredSignals = measuredSignals ?? throw new ArgumentNullException(nameof(measuredSignals));
        ArgumentNullException.ThrowIfNull(diagnostics);

        if (!ReferenceEquals(measuredSignals.Definition, definition))
        {
            throw new ArgumentException("Measured-signal frame does not use the instrumentation snapshot's canonical definition.", nameof(measuredSignals));
        }

        var canonical = diagnostics.OrderBy(static diagnostic => diagnostic.ChannelId, StringComparer.Ordinal).ToArray();
        var expected = definition.Channels.Select(static channel => channel.Id).OrderBy(static id => id, StringComparer.Ordinal).ToArray();
        var actual = canonical.Select(static diagnostic => diagnostic.ChannelId).OrderBy(static id => id, StringComparer.Ordinal).ToArray();
        if (actual.Distinct(StringComparer.Ordinal).Count() != actual.Length || !expected.SequenceEqual(actual, StringComparer.Ordinal))
        {
            throw new ArgumentException("Instrumentation diagnostics must contain exactly one item per channel.", nameof(diagnostics));
        }

        Diagnostics = new ReadOnlyCollection<InstrumentChannelDiagnosticSnapshot>(canonical);
    }

    public InstrumentationSystemDefinition Definition { get; }

    public MeasuredSignalFrame MeasuredSignals { get; }

    public IReadOnlyList<InstrumentChannelDiagnosticSnapshot> Diagnostics { get; }

    public InstrumentChannelDiagnosticSnapshot GetDiagnostic(string channelId)
        => Diagnostics.FirstOrDefault(diagnostic => string.Equals(diagnostic.ChannelId, channelId, StringComparison.Ordinal))
            ?? throw new KeyNotFoundException($"Unknown instrument diagnostic '{channelId}'.");
}
