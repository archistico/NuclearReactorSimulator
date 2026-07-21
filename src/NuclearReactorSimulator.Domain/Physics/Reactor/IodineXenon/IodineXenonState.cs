using NuclearReactorSimulator.Domain.Physics.Quantities;

namespace NuclearReactorSimulator.Domain.Physics.Reactor.IodineXenon;

/// <summary>
/// Immutable normalized I-135 and Xe-135 inventories.
/// </summary>
public sealed record IodineXenonState(IodineInventory Iodine, XenonInventory Xenon)
{
    public static IodineXenonState Empty { get; } = new(IodineInventory.Zero, XenonInventory.Zero);

    public static IodineXenonState CreateEquilibrium(
        IodineXenonDefinition definition,
        Power fissionPower,
        NeutronPopulation neutronPopulation)
    {
        ArgumentNullException.ThrowIfNull(definition);

        if (fissionPower < Power.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(fissionPower), fissionPower, "Fission power cannot be negative.");
        }

        var powerRatio = fissionPower.Watts / definition.ReferenceFissionPower.Watts;
        var iodineSource = definition.IodineProductionAtReferencePower.RelativePerSecond * powerRatio;
        var xenonSource = definition.DirectXenonProductionAtReferencePower.RelativePerSecond * powerRatio;
        var iodine = iodineSource / definition.IodineDecayConstant.PerSecond;
        var xenonRemoval = definition.XenonDecayConstant.PerSecond
            + (definition.XenonBurnupCoefficient.PerSecondPerRelativeNeutronPopulation * neutronPopulation.Relative);
        var xenon = (xenonSource + iodineSource) / xenonRemoval;

        if (!double.IsFinite(iodine) || !double.IsFinite(xenon))
        {
            throw new InvalidOperationException("Iodine/xenon equilibrium exceeded the finite numerical envelope.");
        }

        return new IodineXenonState(
            IodineInventory.FromRelative(iodine),
            XenonInventory.FromRelative(xenon));
    }
}
