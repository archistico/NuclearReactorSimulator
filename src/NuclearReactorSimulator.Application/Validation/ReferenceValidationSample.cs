using System.Collections.ObjectModel;

namespace NuclearReactorSimulator.Application.Validation;

/// <summary>Immutable set of already-observed presentation metrics at one exact deterministic logical step.</summary>
public sealed class ReferenceValidationSample
{
    public ReferenceValidationSample(long logicalStep, IReadOnlyDictionary<string, double?> metrics)
    {
        if (logicalStep < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(logicalStep));
        }
        ArgumentNullException.ThrowIfNull(metrics);
        if (metrics.Keys.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException("Reference validation metric IDs cannot be blank.", nameof(metrics));
        }
        if (metrics.Values.Any(static value => value.HasValue && !double.IsFinite(value.Value)))
        {
            throw new ArgumentException("Reference validation metric values must be finite when available.", nameof(metrics));
        }

        var copy = new Dictionary<string, double?>(StringComparer.Ordinal);
        foreach (var pair in metrics)
        {
            copy.Add(pair.Key, pair.Value);
        }

        LogicalStep = logicalStep;
        Metrics = new ReadOnlyDictionary<string, double?>(copy);
    }

    public long LogicalStep { get; }

    public IReadOnlyDictionary<string, double?> Metrics { get; }
}
