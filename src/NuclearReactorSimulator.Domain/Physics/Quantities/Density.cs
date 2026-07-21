namespace NuclearReactorSimulator.Domain.Physics.Quantities;

public readonly record struct Density : IComparable<Density>
{
    private Density(double kilogramsPerCubicMetre)
    {
        KilogramsPerCubicMetre = QuantityGuard.NonNegativeFinite(kilogramsPerCubicMetre, nameof(kilogramsPerCubicMetre));
    }

    public double KilogramsPerCubicMetre { get; }

    public static Density Zero { get; } = FromKilogramsPerCubicMetre(0d);

    public static Density FromKilogramsPerCubicMetre(double value) => new(value);

    public int CompareTo(Density other) => KilogramsPerCubicMetre.CompareTo(other.KilogramsPerCubicMetre);

    public static Mass operator *(Density density, Volume volume) => Mass.FromKilograms(density.KilogramsPerCubicMetre * volume.CubicMetres);

    public static Mass operator *(Volume volume, Density density) => density * volume;

    public static bool operator <(Density left, Density right) => left.KilogramsPerCubicMetre < right.KilogramsPerCubicMetre;

    public static bool operator >(Density left, Density right) => left.KilogramsPerCubicMetre > right.KilogramsPerCubicMetre;

    public static bool operator <=(Density left, Density right) => left.KilogramsPerCubicMetre <= right.KilogramsPerCubicMetre;

    public static bool operator >=(Density left, Density right) => left.KilogramsPerCubicMetre >= right.KilogramsPerCubicMetre;
}
