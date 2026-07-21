namespace NuclearReactorSimulator.Domain.Physics.Control;

/// <summary>Finite ordered output range for a controller or actuator command seam.</summary>
public readonly record struct ControllerOutputRange
{
    public ControllerOutputRange(double minimum, double maximum)
    {
        if (!double.IsFinite(minimum) || !double.IsFinite(maximum) || maximum <= minimum)
        {
            throw new ArgumentOutOfRangeException(nameof(maximum), maximum, "Controller output range must use finite ordered bounds.");
        }

        Minimum = minimum;
        Maximum = maximum;
    }

    public double Minimum { get; }

    public double Maximum { get; }

    public double Span => Maximum - Minimum;

    public double Clamp(double value)
    {
        if (!double.IsFinite(value))
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "Controller output values must be finite.");
        }

        return Math.Clamp(value, Minimum, Maximum);
    }

    public double Normalize(double value) => (Clamp(value) - Minimum) / Span;
}
