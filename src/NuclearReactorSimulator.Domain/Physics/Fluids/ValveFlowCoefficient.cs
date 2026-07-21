using NuclearReactorSimulator.Domain.Physics.Quantities;

namespace NuclearReactorSimulator.Domain.Physics.Fluids;

/// <summary>
/// Normalized valve flow-capacity coefficient in the [0, 1] interval.
/// It scales mass-flow capacity relative to the fully-open hydraulic path.
/// </summary>
public readonly record struct ValveFlowCoefficient : IComparable<ValveFlowCoefficient>
{
    private ValveFlowCoefficient(double fraction)
    {
        fraction = QuantityGuard.Finite(fraction, nameof(fraction));

        if (fraction < 0d || fraction > 1d)
        {
            throw new ArgumentOutOfRangeException(nameof(fraction), fraction, "Valve flow coefficient must be between zero and one inclusive.");
        }

        Fraction = fraction;
    }

    public double Fraction { get; }

    public bool IsClosed => Fraction == 0d;

    public static ValveFlowCoefficient Closed => new(0d);

    public static ValveFlowCoefficient FullyOpen => new(1d);

    public static ValveFlowCoefficient FromFraction(double fraction) => new(fraction);

    public int CompareTo(ValveFlowCoefficient other) => Fraction.CompareTo(other.Fraction);

    public static bool operator <(ValveFlowCoefficient left, ValveFlowCoefficient right) => left.Fraction < right.Fraction;

    public static bool operator >(ValveFlowCoefficient left, ValveFlowCoefficient right) => left.Fraction > right.Fraction;

    public static bool operator <=(ValveFlowCoefficient left, ValveFlowCoefficient right) => left.Fraction <= right.Fraction;

    public static bool operator >=(ValveFlowCoefficient left, ValveFlowCoefficient right) => left.Fraction >= right.Fraction;
}

