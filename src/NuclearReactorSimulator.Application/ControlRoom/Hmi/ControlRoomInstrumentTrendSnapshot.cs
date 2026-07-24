namespace NuclearReactorSimulator.Application.ControlRoom.Hmi;

/// <summary>
/// Presentation-only deterministic trend derived from two published logical-step samples.
/// It is not a physics derivative and never consults wall clock time.
/// </summary>
public sealed class ControlRoomInstrumentTrendSnapshot
{
    public ControlRoomInstrumentTrendSnapshot(
        ControlRoomInstrumentTrendDirection direction,
        double? ratePerLogicalStep,
        string unit)
    {
        if (!Enum.IsDefined(direction))
        {
            throw new ArgumentOutOfRangeException(nameof(direction));
        }
        if (ratePerLogicalStep is not null && !double.IsFinite(ratePerLogicalStep.Value))
        {
            throw new ArgumentOutOfRangeException(nameof(ratePerLogicalStep));
        }

        Direction = direction;
        RatePerLogicalStep = ratePerLogicalStep;
        Unit = unit?.Trim() ?? string.Empty;
    }

    public static ControlRoomInstrumentTrendSnapshot Unavailable { get; } =
        new(ControlRoomInstrumentTrendDirection.Unavailable, null, string.Empty);

    public ControlRoomInstrumentTrendDirection Direction { get; }
    public double? RatePerLogicalStep { get; }
    public string Unit { get; }

    public string ArrowText => Direction switch
    {
        ControlRoomInstrumentTrendDirection.FallingRapidly => "↓↓",
        ControlRoomInstrumentTrendDirection.Falling => "↓",
        ControlRoomInstrumentTrendDirection.Steady => "→",
        ControlRoomInstrumentTrendDirection.Rising => "↑",
        ControlRoomInstrumentTrendDirection.RisingRapidly => "↑↑",
        _ => "—",
    };

    public string DirectionText => Direction switch
    {
        ControlRoomInstrumentTrendDirection.FallingRapidly => "FALLING RAPIDLY",
        ControlRoomInstrumentTrendDirection.Falling => "FALLING",
        ControlRoomInstrumentTrendDirection.Steady => "STEADY",
        ControlRoomInstrumentTrendDirection.Rising => "RISING",
        ControlRoomInstrumentTrendDirection.RisingRapidly => "RISING RAPIDLY",
        _ => "TREND —",
    };

    public string RateText => RatePerLogicalStep.HasValue
        ? FormattableString.Invariant($"{RatePerLogicalStep.Value:+0.###;-0.###;0} {Unit}/step").Trim()
        : "—";

    public static ControlRoomInstrumentTrendSnapshot Between(
        long previousLogicalStep,
        double? previousValue,
        long currentLogicalStep,
        double? currentValue,
        double steadyTolerance,
        string unit,
        double? rapidTolerance = null)
    {
        if (previousLogicalStep < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(previousLogicalStep));
        }
        if (currentLogicalStep < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(currentLogicalStep));
        }
        if (!double.IsFinite(steadyTolerance) || steadyTolerance < 0d)
        {
            throw new ArgumentOutOfRangeException(nameof(steadyTolerance));
        }
        if (rapidTolerance.HasValue
            && (!double.IsFinite(rapidTolerance.Value) || rapidTolerance.Value <= steadyTolerance))
        {
            throw new ArgumentOutOfRangeException(
                nameof(rapidTolerance),
                rapidTolerance,
                "Rapid-trend tolerance must be finite and greater than steady-trend tolerance.");
        }
        if (!previousValue.HasValue
            || !currentValue.HasValue
            || !double.IsFinite(previousValue.Value)
            || !double.IsFinite(currentValue.Value)
            || currentLogicalStep <= previousLogicalStep)
        {
            return Unavailable;
        }

        var rate = (currentValue.Value - previousValue.Value) / (currentLogicalStep - previousLogicalStep);
        var absoluteRate = Math.Abs(rate);
        var direction = absoluteRate <= steadyTolerance
            ? ControlRoomInstrumentTrendDirection.Steady
            : rapidTolerance.HasValue && absoluteRate >= rapidTolerance.Value
                ? rate > 0d
                    ? ControlRoomInstrumentTrendDirection.RisingRapidly
                    : ControlRoomInstrumentTrendDirection.FallingRapidly
                : rate > 0d
                ? ControlRoomInstrumentTrendDirection.Rising
                : ControlRoomInstrumentTrendDirection.Falling;

        return new ControlRoomInstrumentTrendSnapshot(direction, rate, unit);
    }
}
