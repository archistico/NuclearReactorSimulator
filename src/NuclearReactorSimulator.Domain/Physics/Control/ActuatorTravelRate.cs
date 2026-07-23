namespace NuclearReactorSimulator.Domain.Physics.Control;

/// <summary>
/// Normalized physical actuator travel/ramp rate expressed as full-scale fraction per second.
/// </summary>
public readonly record struct ActuatorTravelRate
{
    private ActuatorTravelRate(double fractionPerSecond)
    {
        if (!double.IsFinite(fractionPerSecond) || fractionPerSecond <= 0d)
        {
            throw new ArgumentOutOfRangeException(
                nameof(fractionPerSecond),
                fractionPerSecond,
                "Actuator travel rate must be finite and positive.");
        }

        FractionPerSecond = fractionPerSecond;
    }

    public double FractionPerSecond { get; }

    public static ActuatorTravelRate FromFractionPerSecond(double value) => new(value);

    public static ActuatorTravelRate FromFullTravelTime(TimeSpan fullTravelTime)
    {
        if (fullTravelTime <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(fullTravelTime),
                fullTravelTime,
                "Full travel time must be greater than zero.");
        }

        return new ActuatorTravelRate(1d / fullTravelTime.TotalSeconds);
    }

    public TimeSpan FullTravelTime
    {
        get
        {
            var seconds = 1d / FractionPerSecond;
            return seconds >= TimeSpan.MaxValue.TotalSeconds
                ? TimeSpan.MaxValue
                : TimeSpan.FromSeconds(seconds);
        }
    }
}
