namespace NuclearReactorSimulator.Domain.Physics.Quantities;

/// <summary>
/// Non-negative shortest electrical phase-angle separation in the closed interval [0, π].
/// </summary>
public readonly record struct PhaseAngleDifference : IComparable<PhaseAngleDifference>
{
    private PhaseAngleDifference(double radians)
    {
        radians = QuantityGuard.NonNegativeFinite(radians, nameof(radians));
        if (radians > Math.PI)
        {
            throw new ArgumentOutOfRangeException(nameof(radians), radians, "Shortest phase-angle difference cannot exceed π radians (180 degrees).");
        }

        Radians = radians;
    }

    public double Radians { get; }

    public double Degrees => Radians * 180d / Math.PI;

    public static PhaseAngleDifference Zero { get; } = FromRadians(0d);

    public static PhaseAngleDifference FromRadians(double value) => new(value);

    public static PhaseAngleDifference FromDegrees(double value) => new(value * Math.PI / 180d);

    public int CompareTo(PhaseAngleDifference other) => Radians.CompareTo(other.Radians);

    public static bool operator <(PhaseAngleDifference left, PhaseAngleDifference right) => left.Radians < right.Radians;

    public static bool operator >(PhaseAngleDifference left, PhaseAngleDifference right) => left.Radians > right.Radians;

    public static bool operator <=(PhaseAngleDifference left, PhaseAngleDifference right) => left.Radians <= right.Radians;

    public static bool operator >=(PhaseAngleDifference left, PhaseAngleDifference right) => left.Radians >= right.Radians;
}
