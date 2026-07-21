namespace NuclearReactorSimulator.Domain.Physics.Reactor.IodineXenon;

/// <summary>
/// Non-negative normalized I-135 inventory. Units are model-relative and must be used consistently with the configured production rates.
/// </summary>
public readonly record struct IodineInventory : IComparable<IodineInventory>
{
    private IodineInventory(double relative)
    {
        if (!double.IsFinite(relative) || relative < 0d)
        {
            throw new ArgumentOutOfRangeException(nameof(relative), relative, "Iodine inventory must be finite and non-negative.");
        }

        Relative = relative == 0d ? 0d : relative;
    }

    public double Relative { get; }

    public static IodineInventory Zero { get; } = FromRelative(0d);

    public static IodineInventory FromRelative(double value) => new(value);

    public int CompareTo(IodineInventory other) => Relative.CompareTo(other.Relative);
}
