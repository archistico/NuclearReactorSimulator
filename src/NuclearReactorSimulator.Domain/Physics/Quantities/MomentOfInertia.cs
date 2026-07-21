namespace NuclearReactorSimulator.Domain.Physics.Quantities;

/// <summary>
/// Non-negative rotational moment of inertia in kilogram square metres. Physical rotor definitions require a positive value.
/// </summary>
public readonly record struct MomentOfInertia : IComparable<MomentOfInertia>
{
    private MomentOfInertia(double kilogramSquareMetres)
    {
        KilogramSquareMetres = QuantityGuard.NonNegativeFinite(kilogramSquareMetres, nameof(kilogramSquareMetres));
    }

    public double KilogramSquareMetres { get; }

    public static MomentOfInertia Zero { get; } = FromKilogramSquareMetres(0d);

    public static MomentOfInertia FromKilogramSquareMetres(double value) => new(value);

    public Energy KineticEnergyAt(AngularSpeed angularSpeed)
        => Energy.FromJoules(0.5d * KilogramSquareMetres * angularSpeed.RadiansPerSecond * angularSpeed.RadiansPerSecond);

    public int CompareTo(MomentOfInertia other) => KilogramSquareMetres.CompareTo(other.KilogramSquareMetres);

    public static bool operator <(MomentOfInertia left, MomentOfInertia right) => left.KilogramSquareMetres < right.KilogramSquareMetres;

    public static bool operator >(MomentOfInertia left, MomentOfInertia right) => left.KilogramSquareMetres > right.KilogramSquareMetres;

    public static bool operator <=(MomentOfInertia left, MomentOfInertia right) => left.KilogramSquareMetres <= right.KilogramSquareMetres;

    public static bool operator >=(MomentOfInertia left, MomentOfInertia right) => left.KilogramSquareMetres >= right.KilogramSquareMetres;
}
