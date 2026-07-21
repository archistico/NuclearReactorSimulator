namespace NuclearReactorSimulator.Domain.Physics.Quantities;

public readonly record struct PressureDifference : IComparable<PressureDifference>
{
    private const double PascalsPerKilopascal = 1_000d;
    private const double PascalsPerMegapascal = 1_000_000d;
    private const double PascalsPerBar = 100_000d;

    private PressureDifference(double pascals)
    {
        Pascals = QuantityGuard.Finite(pascals, nameof(pascals));
    }

    public double Pascals { get; }

    public double Kilopascals => Pascals / PascalsPerKilopascal;

    public double Megapascals => Pascals / PascalsPerMegapascal;

    public double Bar => Pascals / PascalsPerBar;

    public static PressureDifference Zero { get; } = FromPascals(0d);

    public static PressureDifference FromPascals(double value) => new(value);

    public static PressureDifference FromKilopascals(double value) => new(value * PascalsPerKilopascal);

    public static PressureDifference FromMegapascals(double value) => new(value * PascalsPerMegapascal);

    public static PressureDifference FromBar(double value) => new(value * PascalsPerBar);

    public int CompareTo(PressureDifference other) => Pascals.CompareTo(other.Pascals);

    public static PressureDifference operator +(PressureDifference left, PressureDifference right) => FromPascals(left.Pascals + right.Pascals);

    public static PressureDifference operator -(PressureDifference left, PressureDifference right) => FromPascals(left.Pascals - right.Pascals);

    public static PressureDifference operator -(PressureDifference value) => FromPascals(-value.Pascals);

    public static PressureDifference operator *(PressureDifference value, double scalar) => FromPascals(value.Pascals * QuantityGuard.Finite(scalar, nameof(scalar)));

    public static PressureDifference operator *(double scalar, PressureDifference value) => value * scalar;

    public static PressureDifference operator /(PressureDifference value, double divisor) => FromPascals(value.Pascals / QuantityGuard.PositiveFinite(divisor, nameof(divisor)));

    public static Energy operator *(PressureDifference pressureDifference, Volume volume) => Energy.FromJoules(pressureDifference.Pascals * volume.CubicMetres);

    public static Energy operator *(Volume volume, PressureDifference pressureDifference) => pressureDifference * volume;

    public static Power operator *(PressureDifference pressureDifference, VolumetricFlowRate volumetricFlowRate)
        => Power.FromWatts(pressureDifference.Pascals * volumetricFlowRate.CubicMetresPerSecond);

    public static Power operator *(VolumetricFlowRate volumetricFlowRate, PressureDifference pressureDifference)
        => pressureDifference * volumetricFlowRate;

    public static bool operator <(PressureDifference left, PressureDifference right) => left.Pascals < right.Pascals;

    public static bool operator >(PressureDifference left, PressureDifference right) => left.Pascals > right.Pascals;

    public static bool operator <=(PressureDifference left, PressureDifference right) => left.Pascals <= right.Pascals;

    public static bool operator >=(PressureDifference left, PressureDifference right) => left.Pascals >= right.Pascals;
}
