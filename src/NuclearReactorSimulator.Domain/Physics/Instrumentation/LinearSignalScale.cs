namespace NuclearReactorSimulator.Domain.Physics.Instrumentation;

/// <summary>Deterministic linear mapping from a channel engineering range to an output/raw scale.</summary>
public readonly record struct LinearSignalScale
{
    public LinearSignalScale(double outputMinimum, double outputMaximum)
    {
        if (!double.IsFinite(outputMinimum))
        {
            throw new ArgumentOutOfRangeException(nameof(outputMinimum), outputMinimum, "Scale output minimum must be finite.");
        }

        if (!double.IsFinite(outputMaximum))
        {
            throw new ArgumentOutOfRangeException(nameof(outputMaximum), outputMaximum, "Scale output maximum must be finite.");
        }

        if (outputMaximum <= outputMinimum)
        {
            throw new ArgumentOutOfRangeException(nameof(outputMaximum), outputMaximum, "Scale output maximum must be greater than output minimum.");
        }

        OutputMinimum = outputMinimum;
        OutputMaximum = outputMaximum;
    }

    public double OutputMinimum { get; }

    public double OutputMaximum { get; }

    public double Map(double engineeringValue, SignalRange engineeringRange)
    {
        if (!double.IsFinite(engineeringValue))
        {
            throw new ArgumentOutOfRangeException(nameof(engineeringValue), engineeringValue, "Engineering value must be finite.");
        }

        var normalized = (engineeringValue - engineeringRange.Minimum) / engineeringRange.Span;
        return OutputMinimum + (normalized * (OutputMaximum - OutputMinimum));
    }

    public static LinearSignalScale NormalizedZeroToOne { get; } = new(0d, 1d);
}
