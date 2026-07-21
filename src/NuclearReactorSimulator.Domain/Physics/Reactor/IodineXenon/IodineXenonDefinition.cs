using NuclearReactorSimulator.Domain.Physics.Quantities;

namespace NuclearReactorSimulator.Domain.Physics.Reactor.IodineXenon;

/// <summary>
/// Immutable plant/configuration parameters for the reduced I-135 / Xe-135 inventory model.
/// No reactor-specific constants are embedded in the solver.
/// </summary>
public sealed record IodineXenonDefinition
{
    public IodineXenonDefinition(
        string id,
        Power referenceFissionPower,
        PoisonProductionRate iodineProductionAtReferencePower,
        PoisonProductionRate directXenonProductionAtReferencePower,
        DecayConstant iodineDecayConstant,
        DecayConstant xenonDecayConstant,
        XenonBurnupCoefficient xenonBurnupCoefficient,
        XenonReactivityCoefficient xenonReactivityCoefficient)
    {
        Id = string.IsNullOrWhiteSpace(id)
            ? throw new ArgumentException("Iodine/xenon definition id is required.", nameof(id))
            : id.Trim();

        if (referenceFissionPower <= Power.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(referenceFissionPower),
                referenceFissionPower,
                "Reference fission power must be greater than zero.");
        }

        ReferenceFissionPower = referenceFissionPower;
        IodineProductionAtReferencePower = iodineProductionAtReferencePower;
        DirectXenonProductionAtReferencePower = directXenonProductionAtReferencePower;
        IodineDecayConstant = iodineDecayConstant;
        XenonDecayConstant = xenonDecayConstant;
        XenonBurnupCoefficient = xenonBurnupCoefficient;
        XenonReactivityCoefficient = xenonReactivityCoefficient;
    }

    public string Id { get; }

    public Power ReferenceFissionPower { get; }

    public PoisonProductionRate IodineProductionAtReferencePower { get; }

    public PoisonProductionRate DirectXenonProductionAtReferencePower { get; }

    public DecayConstant IodineDecayConstant { get; }

    public DecayConstant XenonDecayConstant { get; }

    public XenonBurnupCoefficient XenonBurnupCoefficient { get; }

    public XenonReactivityCoefficient XenonReactivityCoefficient { get; }
}
