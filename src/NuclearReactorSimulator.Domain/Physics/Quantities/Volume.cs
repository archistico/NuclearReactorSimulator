namespace NuclearReactorSimulator.Domain.Physics.Quantities;

public readonly record struct Volume : IComparable<Volume>
{
    private const double LitresPerCubicMetre = 1_000d;

    private Volume(double cubicMetres)
    {
        CubicMetres = QuantityGuard.NonNegativeFinite(cubicMetres, nameof(cubicMetres));
    }

    public double CubicMetres { get; }

    public double Litres => CubicMetres * LitresPerCubicMetre;

    public static Volume Zero { get; } = FromCubicMetres(0d);

    public static Volume FromCubicMetres(double value) => new(value);

    public static Volume FromLitres(double value) => new(value / LitresPerCubicMetre);

    public int CompareTo(Volume other) => CubicMetres.CompareTo(other.CubicMetres);

    public static Volume operator +(Volume left, Volume right) => FromCubicMetres(left.CubicMetres + right.CubicMetres);

    public static Volume operator -(Volume left, Volume right) => FromCubicMetres(left.CubicMetres - right.CubicMetres);

    public static Volume operator *(Volume value, double scalar) => FromCubicMetres(value.CubicMetres * QuantityGuard.Finite(scalar, nameof(scalar)));

    public static Volume operator *(double scalar, Volume value) => value * scalar;

    public static Volume operator /(Volume value, double divisor) => FromCubicMetres(value.CubicMetres / QuantityGuard.PositiveFinite(divisor, nameof(divisor)));

    public static bool operator <(Volume left, Volume right) => left.CubicMetres < right.CubicMetres;

    public static bool operator >(Volume left, Volume right) => left.CubicMetres > right.CubicMetres;

    public static bool operator <=(Volume left, Volume right) => left.CubicMetres <= right.CubicMetres;

    public static bool operator >=(Volume left, Volume right) => left.CubicMetres >= right.CubicMetres;
}
