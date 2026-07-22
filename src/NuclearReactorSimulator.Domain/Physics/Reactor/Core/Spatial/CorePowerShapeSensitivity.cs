namespace NuclearReactorSimulator.Domain.Physics.Reactor.Core.Spatial;

/// <summary>
/// Configured quasi-spatial power-shape sensitivity expressed as exponential log-weight change per pcm of
/// zone-local reactivity deviation from the power-weighted core mean.
/// </summary>
public readonly record struct CorePowerShapeSensitivity : IComparable<CorePowerShapeSensitivity>
{
    private CorePowerShapeSensitivity(double perPcm)
    {
        if (!double.IsFinite(perPcm) || perPcm < 0d)
        {
            throw new ArgumentOutOfRangeException(nameof(perPcm), perPcm, "Core power-shape sensitivity must be finite and non-negative.");
        }

        PerPcm = perPcm == 0d ? 0d : perPcm;
    }

    public double PerPcm { get; }

    public static CorePowerShapeSensitivity Zero { get; } = FromPerPcm(0d);

    public static CorePowerShapeSensitivity FromPerPcm(double value) => new(value);

    public int CompareTo(CorePowerShapeSensitivity other) => PerPcm.CompareTo(other.PerPcm);
}
