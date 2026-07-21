namespace NuclearReactorSimulator.Domain.Physics.Quantities;

public readonly record struct Pressure : IComparable<Pressure>
{
    private const double PascalsPerKilopascal = 1_000d;
    private const double PascalsPerMegapascal = 1_000_000d;
    private const double PascalsPerBar = 100_000d;
    private const double PascalsPerStandardAtmosphere = 101_325d;

    private Pressure(double pascals)
    {
        Pascals = QuantityGuard.NonNegativeFinite(pascals, nameof(pascals));
    }

    public double Pascals { get; }

    public double Kilopascals => Pascals / PascalsPerKilopascal;

    public double Megapascals => Pascals / PascalsPerMegapascal;

    public double Bar => Pascals / PascalsPerBar;

    public double StandardAtmospheres => Pascals / PascalsPerStandardAtmosphere;

    public static Pressure Vacuum { get; } = FromPascals(0d);

    public static Pressure StandardAtmosphere { get; } = FromPascals(PascalsPerStandardAtmosphere);

    public static Pressure FromPascals(double value) => new(value);

    public static Pressure FromKilopascals(double value) => new(value * PascalsPerKilopascal);

    public static Pressure FromMegapascals(double value) => new(value * PascalsPerMegapascal);

    public static Pressure FromBar(double value) => new(value * PascalsPerBar);

    public static Pressure FromStandardAtmospheres(double value) => new(value * PascalsPerStandardAtmosphere);

    public int CompareTo(Pressure other) => Pascals.CompareTo(other.Pascals);

    public static Pressure operator +(Pressure pressure, PressureDifference difference) => FromPascals(pressure.Pascals + difference.Pascals);

    public static Pressure operator +(PressureDifference difference, Pressure pressure) => pressure + difference;

    public static Pressure operator -(Pressure pressure, PressureDifference difference) => FromPascals(pressure.Pascals - difference.Pascals);

    public static PressureDifference operator -(Pressure left, Pressure right) => PressureDifference.FromPascals(left.Pascals - right.Pascals);

    public static bool operator <(Pressure left, Pressure right) => left.Pascals < right.Pascals;

    public static bool operator >(Pressure left, Pressure right) => left.Pascals > right.Pascals;

    public static bool operator <=(Pressure left, Pressure right) => left.Pascals <= right.Pascals;

    public static bool operator >=(Pressure left, Pressure right) => left.Pascals >= right.Pascals;
}
