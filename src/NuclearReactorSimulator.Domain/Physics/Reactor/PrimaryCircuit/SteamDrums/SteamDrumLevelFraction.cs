namespace NuclearReactorSimulator.Domain.Physics.Reactor.PrimaryCircuit.SteamDrums;

/// <summary>
/// Normalized liquid-level indication for an aggregated steam-drum control volume.
/// Zero means no liquid volume and one means the control volume is fully occupied by liquid.
/// </summary>
public readonly record struct SteamDrumLevelFraction : IComparable<SteamDrumLevelFraction>
{
    private SteamDrumLevelFraction(double fraction)
    {
        if (!double.IsFinite(fraction) || fraction < 0d || fraction > 1d)
        {
            throw new ArgumentOutOfRangeException(nameof(fraction), fraction, "Steam-drum level fraction must be finite and between zero and one inclusive.");
        }

        Fraction = fraction;
    }

    public double Fraction { get; }

    public double Percent => Fraction * 100d;

    public static SteamDrumLevelFraction Empty { get; } = FromFraction(0d);

    public static SteamDrumLevelFraction Full { get; } = FromFraction(1d);

    public static SteamDrumLevelFraction FromFraction(double value) => new(value);

    public static SteamDrumLevelFraction FromPercent(double value) => new(value / 100d);

    public int CompareTo(SteamDrumLevelFraction other) => Fraction.CompareTo(other.Fraction);

    public static bool operator <(SteamDrumLevelFraction left, SteamDrumLevelFraction right) => left.Fraction < right.Fraction;

    public static bool operator >(SteamDrumLevelFraction left, SteamDrumLevelFraction right) => left.Fraction > right.Fraction;

    public static bool operator <=(SteamDrumLevelFraction left, SteamDrumLevelFraction right) => left.Fraction <= right.Fraction;

    public static bool operator >=(SteamDrumLevelFraction left, SteamDrumLevelFraction right) => left.Fraction >= right.Fraction;
}
