namespace NuclearReactorSimulator.Domain.Physics.Quantities;

public readonly record struct Area : IComparable<Area>
{
    private const double SquareCentimetresPerSquareMetre = 10_000d;
    private const double SquareMillimetresPerSquareMetre = 1_000_000d;

    private Area(double squareMetres)
    {
        SquareMetres = QuantityGuard.NonNegativeFinite(squareMetres, nameof(squareMetres));
    }

    public double SquareMetres { get; }

    public double SquareCentimetres => SquareMetres * SquareCentimetresPerSquareMetre;

    public double SquareMillimetres => SquareMetres * SquareMillimetresPerSquareMetre;

    public static Area Zero { get; } = FromSquareMetres(0d);

    public static Area FromSquareMetres(double value) => new(value);

    public static Area FromSquareCentimetres(double value) => new(value / SquareCentimetresPerSquareMetre);

    public static Area FromSquareMillimetres(double value) => new(value / SquareMillimetresPerSquareMetre);

    public int CompareTo(Area other) => SquareMetres.CompareTo(other.SquareMetres);

    public static Area operator +(Area left, Area right) => FromSquareMetres(left.SquareMetres + right.SquareMetres);

    public static Area operator -(Area left, Area right) => FromSquareMetres(left.SquareMetres - right.SquareMetres);

    public static Area operator *(Area value, double scalar) => FromSquareMetres(value.SquareMetres * QuantityGuard.Finite(scalar, nameof(scalar)));

    public static Area operator *(double scalar, Area value) => value * scalar;

    public static Area operator /(Area value, double divisor) => FromSquareMetres(value.SquareMetres / QuantityGuard.PositiveFinite(divisor, nameof(divisor)));

    public static Volume operator *(Area area, Length length) => Volume.FromCubicMetres(area.SquareMetres * length.Metres);

    public static Volume operator *(Length length, Area area) => area * length;

    public static bool operator <(Area left, Area right) => left.SquareMetres < right.SquareMetres;

    public static bool operator >(Area left, Area right) => left.SquareMetres > right.SquareMetres;

    public static bool operator <=(Area left, Area right) => left.SquareMetres <= right.SquareMetres;

    public static bool operator >=(Area left, Area right) => left.SquareMetres >= right.SquareMetres;
}
