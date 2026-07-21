using NuclearReactorSimulator.Domain.Physics.Quantities;

namespace NuclearReactorSimulator.Domain.Physics.Reactor.IodineXenon;

/// <summary>
/// Signed reactivity per unit normalized Xe-135 inventory.
/// A poisoning configuration normally uses a negative value, but the generic engine does not hardcode the sign.
/// </summary>
public readonly record struct XenonReactivityCoefficient : IComparable<XenonReactivityCoefficient>
{
    private XenonReactivityCoefficient(double deltaKOverKPerRelativeInventory)
    {
        if (!double.IsFinite(deltaKOverKPerRelativeInventory))
        {
            throw new ArgumentOutOfRangeException(
                nameof(deltaKOverKPerRelativeInventory),
                deltaKOverKPerRelativeInventory,
                "Xenon reactivity coefficient must be finite.");
        }

        DeltaKOverKPerRelativeInventory = deltaKOverKPerRelativeInventory == 0d
            ? 0d
            : deltaKOverKPerRelativeInventory;
    }

    public double DeltaKOverKPerRelativeInventory { get; }

    public double PcmPerRelativeInventory => DeltaKOverKPerRelativeInventory * 100_000d;

    public static XenonReactivityCoefficient Zero { get; } = FromDeltaKOverKPerRelativeInventory(0d);

    public static XenonReactivityCoefficient FromDeltaKOverKPerRelativeInventory(double value) => new(value);

    public static XenonReactivityCoefficient FromPcmPerRelativeInventory(double value) => new(value / 100_000d);

    public Reactivity Apply(XenonInventory inventory)
        => Reactivity.FromDeltaKOverK(DeltaKOverKPerRelativeInventory * inventory.Relative);

    public int CompareTo(XenonReactivityCoefficient other)
        => DeltaKOverKPerRelativeInventory.CompareTo(other.DeltaKOverKPerRelativeInventory);

    public static Reactivity operator *(XenonReactivityCoefficient coefficient, XenonInventory inventory)
        => coefficient.Apply(inventory);
}
