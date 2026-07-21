using NuclearReactorSimulator.Domain.Physics.Quantities;

namespace NuclearReactorSimulator.Domain.Physics.Fluids;

/// <summary>
/// Intensive thermodynamic closure variables for a fluid control volume.
/// </summary>
public sealed record FluidThermodynamicState
{
    public FluidThermodynamicState(Pressure pressure, Temperature temperature)
        : this(pressure, temperature, FluidPhase.Unspecified, null)
    {
    }

    public FluidThermodynamicState(
        Pressure pressure,
        Temperature temperature,
        FluidPhase phase,
        VaporQuality? vaporQuality)
    {
        if (pressure <= Pressure.Vacuum)
        {
            throw new ArgumentOutOfRangeException(nameof(pressure), pressure, "A populated fluid node must have positive absolute pressure.");
        }

        if (temperature <= Temperature.AbsoluteZero)
        {
            throw new ArgumentOutOfRangeException(nameof(temperature), temperature, "A populated fluid node must have temperature above absolute zero.");
        }

        if (!Enum.IsDefined(phase))
        {
            throw new ArgumentOutOfRangeException(nameof(phase), phase, "Unknown fluid phase.");
        }

        if (phase == FluidPhase.SaturatedMixture && vaporQuality is null)
        {
            throw new ArgumentException("A saturated mixture must define vapor quality.", nameof(vaporQuality));
        }

        if (phase != FluidPhase.SaturatedMixture && vaporQuality is not null)
        {
            throw new ArgumentException("Vapor quality is defined only for a saturated mixture.", nameof(vaporQuality));
        }

        Pressure = pressure;
        Temperature = temperature;
        Phase = phase;
        VaporQuality = vaporQuality;
    }

    public Pressure Pressure { get; }

    public Temperature Temperature { get; }

    public FluidPhase Phase { get; }

    public VaporQuality? VaporQuality { get; }

    public double? VaporMassFraction => Phase switch
    {
        FluidPhase.SubcooledLiquid => 0d,
        FluidPhase.SaturatedMixture => VaporQuality?.Fraction,
        FluidPhase.SuperheatedVapor => 1d,
        _ => null,
    };
}
