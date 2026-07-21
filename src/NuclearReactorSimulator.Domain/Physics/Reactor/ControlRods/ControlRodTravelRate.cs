namespace NuclearReactorSimulator.Domain.Physics.Reactor.ControlRods;

/// <summary>
/// Normalized control-rod travel rate expressed as full-stroke fraction per second.
/// </summary>
public readonly record struct ControlRodTravelRate
{
    private ControlRodTravelRate(double fractionPerSecond)
    {
        if (!double.IsFinite(fractionPerSecond) || fractionPerSecond < 0d)
        {
            throw new ArgumentOutOfRangeException(
                nameof(fractionPerSecond),
                fractionPerSecond,
                "Control-rod travel rate must be finite and non-negative.");
        }

        FractionPerSecond = fractionPerSecond == 0d ? 0d : fractionPerSecond;
    }

    public double FractionPerSecond { get; }

    public static ControlRodTravelRate Zero { get; } = FromFractionPerSecond(0d);

    public static ControlRodTravelRate FromFractionPerSecond(double value) => new(value);

    public static ControlRodTravelRate FromFullTravelTime(TimeSpan fullTravelTime)
    {
        if (fullTravelTime <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(fullTravelTime), fullTravelTime, "Full travel time must be greater than zero.");
        }

        return new ControlRodTravelRate(1d / fullTravelTime.TotalSeconds);
    }

    public TimeSpan FullTravelTime
    {
        get
        {
            if (FractionPerSecond <= 0d)
            {
                return TimeSpan.MaxValue;
            }

            var seconds = 1d / FractionPerSecond;
            return seconds >= TimeSpan.MaxValue.TotalSeconds
                ? TimeSpan.MaxValue
                : TimeSpan.FromSeconds(seconds);
        }
    }
}
