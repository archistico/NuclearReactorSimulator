namespace NuclearReactorSimulator.Domain.Physics.Reactor.IodineXenon;

/// <summary>
/// Non-negative normalized Xe-135 inventory. Units are model-relative and must be used consistently with the configured reactivity coefficient.
/// </summary>
public readonly record struct XenonInventory : IComparable<XenonInventory>
{
    private XenonInventory(double relative)
    {
        if (!double.IsFinite(relative) || relative < 0d)
        {
            throw new ArgumentOutOfRangeException(nameof(relative), relative, "Xenon inventory must be finite and non-negative.");
        }

        Relative = relative == 0d ? 0d : relative;
    }

    public double Relative { get; }

    public static XenonInventory Zero { get; } = FromRelative(0d);

    public static XenonInventory FromRelative(double value) => new(value);

    public int CompareTo(XenonInventory other) => Relative.CompareTo(other.Relative);
}
