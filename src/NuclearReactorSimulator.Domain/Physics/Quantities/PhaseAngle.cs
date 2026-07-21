namespace NuclearReactorSimulator.Domain.Physics.Quantities;

/// <summary>
/// Canonical electrical phase angle normalized to [0, 2π).
/// </summary>
public readonly record struct PhaseAngle
{
    private const double FullTurnRadians = 2d * Math.PI;

    private PhaseAngle(double radians)
    {
        radians = QuantityGuard.Finite(radians, nameof(radians));
        var normalized = radians % FullTurnRadians;
        if (normalized < 0d)
        {
            normalized += FullTurnRadians;
        }

        Radians = normalized == FullTurnRadians || normalized == 0d ? 0d : normalized;
    }

    public double Radians { get; }

    public double Degrees => Radians * 180d / Math.PI;

    public static PhaseAngle Zero { get; } = FromRadians(0d);

    public static PhaseAngle FromRadians(double value) => new(value);

    public static PhaseAngle FromDegrees(double value) => new(value * Math.PI / 180d);

    public PhaseAngle Advance(double radians) => FromRadians(Radians + QuantityGuard.Finite(radians, nameof(radians)));

    public double ShortestAbsoluteDifferenceRadians(PhaseAngle other)
    {
        var difference = Math.Abs(Radians - other.Radians) % FullTurnRadians;
        return difference > Math.PI ? FullTurnRadians - difference : difference;
    }

    public double ShortestAbsoluteDifferenceDegrees(PhaseAngle other)
        => ShortestAbsoluteDifferenceRadians(other) * 180d / Math.PI;

    public PhaseAngleDifference ShortestDifference(PhaseAngle other)
        => PhaseAngleDifference.FromRadians(ShortestAbsoluteDifferenceRadians(other));
}
