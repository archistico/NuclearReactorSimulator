namespace NuclearReactorSimulator.Domain.Physics.Quantities;

/// <summary>
/// Lumped quadratic hydraulic resistance R in the relation Δp = R · m_dot · |m_dot|.
/// Canonical SI storage unit: Pa·s²/kg².
/// </summary>
public readonly record struct QuadraticHydraulicResistance : IComparable<QuadraticHydraulicResistance>
{
    private QuadraticHydraulicResistance(double pascalSecondsSquaredPerKilogramSquared)
    {
        PascalSecondsSquaredPerKilogramSquared = QuantityGuard.PositiveFinite(
            pascalSecondsSquaredPerKilogramSquared,
            nameof(pascalSecondsSquaredPerKilogramSquared));
    }

    public double PascalSecondsSquaredPerKilogramSquared { get; }

    public static QuadraticHydraulicResistance FromPascalSecondsSquaredPerKilogramSquared(double value) => new(value);

    public int CompareTo(QuadraticHydraulicResistance other)
    {
        return PascalSecondsSquaredPerKilogramSquared.CompareTo(other.PascalSecondsSquaredPerKilogramSquared);
    }

    public static bool operator <(QuadraticHydraulicResistance left, QuadraticHydraulicResistance right)
    {
        return left.PascalSecondsSquaredPerKilogramSquared < right.PascalSecondsSquaredPerKilogramSquared;
    }

    public static bool operator >(QuadraticHydraulicResistance left, QuadraticHydraulicResistance right)
    {
        return left.PascalSecondsSquaredPerKilogramSquared > right.PascalSecondsSquaredPerKilogramSquared;
    }

    public static bool operator <=(QuadraticHydraulicResistance left, QuadraticHydraulicResistance right)
    {
        return left.PascalSecondsSquaredPerKilogramSquared <= right.PascalSecondsSquaredPerKilogramSquared;
    }

    public static bool operator >=(QuadraticHydraulicResistance left, QuadraticHydraulicResistance right)
    {
        return left.PascalSecondsSquaredPerKilogramSquared >= right.PascalSecondsSquaredPerKilogramSquared;
    }
}
