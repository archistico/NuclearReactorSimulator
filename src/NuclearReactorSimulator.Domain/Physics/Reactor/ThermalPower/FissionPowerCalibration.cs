using NuclearReactorSimulator.Domain.Physics.Quantities;

namespace NuclearReactorSimulator.Domain.Physics.Reactor.ThermalPower;

/// <summary>
/// Explicit calibration between normalized neutron population and instantaneous fission thermal power.
/// </summary>
public sealed record FissionPowerCalibration
{
    public FissionPowerCalibration(
        NeutronPopulation referenceNeutronPopulation,
        Power referenceThermalPower)
    {
        if (referenceNeutronPopulation <= NeutronPopulation.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(referenceNeutronPopulation),
                referenceNeutronPopulation,
                "Reference neutron population must be greater than zero.");
        }

        if (referenceThermalPower <= Power.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(referenceThermalPower),
                referenceThermalPower,
                "Reference fission thermal power must be greater than zero.");
        }

        ReferenceNeutronPopulation = referenceNeutronPopulation;
        ReferenceThermalPower = referenceThermalPower;
    }

    public NeutronPopulation ReferenceNeutronPopulation { get; }

    public Power ReferenceThermalPower { get; }
}
