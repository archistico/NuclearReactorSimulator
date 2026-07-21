namespace NuclearReactorSimulator.Domain.Physics.Quantities;

/// <summary>
/// Signed mechanical torque in newton metres.
/// </summary>
public readonly record struct Torque : IComparable<Torque>
{
    private Torque(double newtonMetres)
    {
        NewtonMetres = QuantityGuard.Finite(newtonMetres, nameof(newtonMetres));
    }

    public double NewtonMetres { get; }

    public static Torque Zero { get; } = FromNewtonMetres(0d);

    public static Torque FromNewtonMetres(double value) => new(value);

    public Power At(AngularSpeed angularSpeed)
        => Power.FromWatts(NewtonMetres * angularSpeed.RadiansPerSecond);

    public int CompareTo(Torque other) => NewtonMetres.CompareTo(other.NewtonMetres);

    public static Torque operator +(Torque left, Torque right) => FromNewtonMetres(left.NewtonMetres + right.NewtonMetres);

    public static Torque operator -(Torque left, Torque right) => FromNewtonMetres(left.NewtonMetres - right.NewtonMetres);

    public static Torque operator -(Torque value) => FromNewtonMetres(-value.NewtonMetres);

    public static Torque operator *(Torque value, double scalar)
        => FromNewtonMetres(value.NewtonMetres * QuantityGuard.Finite(scalar, nameof(scalar)));

    public static bool operator <(Torque left, Torque right) => left.NewtonMetres < right.NewtonMetres;

    public static bool operator >(Torque left, Torque right) => left.NewtonMetres > right.NewtonMetres;

    public static bool operator <=(Torque left, Torque right) => left.NewtonMetres <= right.NewtonMetres;

    public static bool operator >=(Torque left, Torque right) => left.NewtonMetres >= right.NewtonMetres;
}
