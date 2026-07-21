namespace NuclearReactorSimulator.Domain.Physics.Instrumentation;

/// <summary>Finite inclusive engineering range for one measured scalar signal.</summary>
public readonly record struct SignalRange
{
    public SignalRange(double minimum, double maximum)
    {
        if (!double.IsFinite(minimum))
        {
            throw new ArgumentOutOfRangeException(nameof(minimum), minimum, "Signal-range minimum must be finite.");
        }

        if (!double.IsFinite(maximum))
        {
            throw new ArgumentOutOfRangeException(nameof(maximum), maximum, "Signal-range maximum must be finite.");
        }

        if (maximum <= minimum)
        {
            throw new ArgumentOutOfRangeException(nameof(maximum), maximum, "Signal-range maximum must be greater than minimum.");
        }

        Minimum = minimum;
        Maximum = maximum;
    }

    public double Minimum { get; }

    public double Maximum { get; }

    public double Span => Maximum - Minimum;

    public bool Contains(double value) => double.IsFinite(value) && value >= Minimum && value <= Maximum;

    public double Clamp(double value)
    {
        if (!double.IsFinite(value))
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "Signal value must be finite.");
        }

        return Math.Clamp(value, Minimum, Maximum);
    }
}
