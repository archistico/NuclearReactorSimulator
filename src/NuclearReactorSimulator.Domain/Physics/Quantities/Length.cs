namespace NuclearReactorSimulator.Domain.Physics.Quantities;

public readonly record struct Length : IComparable<Length>
{
    private const double MillimetresPerMetre = 1_000d;
    private const double CentimetresPerMetre = 100d;

    private Length(double metres)
    {
        Metres = QuantityGuard.NonNegativeFinite(metres, nameof(metres));
    }

    public double Metres { get; }

    public double Centimetres => Metres * CentimetresPerMetre;

    public double Millimetres => Metres * MillimetresPerMetre;

    public static Length Zero { get; } = FromMetres(0d);

    public static Length FromMetres(double value) => new(value);

    public static Length FromCentimetres(double value) => new(value / CentimetresPerMetre);

    public static Length FromMillimetres(double value) => new(value / MillimetresPerMetre);

    public int CompareTo(Length other) => Metres.CompareTo(other.Metres);

    public static Length operator +(Length left, Length right) => FromMetres(left.Metres + right.Metres);

    public static Length operator -(Length left, Length right) => FromMetres(left.Metres - right.Metres);

    public static Length operator *(Length value, double scalar) => FromMetres(value.Metres * QuantityGuard.Finite(scalar, nameof(scalar)));

    public static Length operator *(double scalar, Length value) => value * scalar;

    public static Length operator /(Length value, double divisor) => FromMetres(value.Metres / QuantityGuard.PositiveFinite(divisor, nameof(divisor)));

    public static Area operator *(Length left, Length right) => Area.FromSquareMetres(left.Metres * right.Metres);

    public static bool operator <(Length left, Length right) => left.Metres < right.Metres;

    public static bool operator >(Length left, Length right) => left.Metres > right.Metres;

    public static bool operator <=(Length left, Length right) => left.Metres <= right.Metres;

    public static bool operator >=(Length left, Length right) => left.Metres >= right.Metres;
}
