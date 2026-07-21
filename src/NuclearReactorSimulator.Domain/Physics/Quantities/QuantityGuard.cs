namespace NuclearReactorSimulator.Domain.Physics.Quantities;

internal static class QuantityGuard
{
    public static double Finite(double value, string parameterName)
    {
        if (!double.IsFinite(value))
        {
            throw new ArgumentOutOfRangeException(parameterName, value, "Physical quantities must be finite.");
        }

        return value == 0d ? 0d : value;
    }

    public static double NonNegativeFinite(double value, string parameterName)
    {
        value = Finite(value, parameterName);

        if (value < 0d)
        {
            throw new ArgumentOutOfRangeException(parameterName, value, "Physical quantity cannot be negative.");
        }

        return value;
    }

    public static double PositiveFinite(double value, string parameterName)
    {
        value = Finite(value, parameterName);

        if (value <= 0d)
        {
            throw new ArgumentOutOfRangeException(parameterName, value, "Value must be greater than zero.");
        }

        return value;
    }

    public static double PositiveSeconds(TimeSpan duration, string parameterName)
    {
        if (duration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(parameterName, duration, "Duration must be greater than zero.");
        }

        return duration.TotalSeconds;
    }
}
