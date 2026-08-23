using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Physics.Quantities;

namespace NuclearReactorSimulator.Domain.Physics.TurbineIsland.Turbine;

/// <summary>
/// Educational lumped expansion group connected to one M4.1 admission seam and one canonical exhaust node.
/// Nominal specific work is defined at rated rotor speed before efficiency is applied.
/// </summary>
public sealed class TurbineStageGroupDefinition
{
    public TurbineStageGroupDefinition(
        string id,
        string admissionBoundaryId,
        string exhaustNodeId,
        string rotorId,
        SpecificEnergy nominalSpecificWork,
        TurbineEfficiency efficiency,
        QuadraticHydraulicResistance? expansionResistance = null,
        TurbineThermodynamicWorkDefinition? thermodynamicWork = null,
        TurbineAdmissionPhasePolicy admissionPhasePolicy = TurbineAdmissionPhasePolicy.LegacyUnrestricted,
        FluidEnergyTransportMode energyTransportMode = FluidEnergyTransportMode.SpecificInternalEnergy,
        string? moistureDrainNodeId = null)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Turbine stage-group id cannot be empty or whitespace.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(admissionBoundaryId))
        {
            throw new ArgumentException("Admission-boundary id cannot be empty or whitespace.", nameof(admissionBoundaryId));
        }

        if (string.IsNullOrWhiteSpace(exhaustNodeId))
        {
            throw new ArgumentException("Turbine exhaust-node id cannot be empty or whitespace.", nameof(exhaustNodeId));
        }

        if (string.IsNullOrWhiteSpace(rotorId))
        {
            throw new ArgumentException("Turbine rotor id cannot be empty or whitespace.", nameof(rotorId));
        }

        if (nominalSpecificWork <= SpecificEnergy.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(nominalSpecificWork), nominalSpecificWork, "Nominal turbine specific work must be greater than zero.");
        }

        if (efficiency.Fraction <= 0d || efficiency.Fraction > 1d)
        {
            throw new ArgumentOutOfRangeException(nameof(efficiency), efficiency, "Turbine efficiency must be greater than zero and no greater than one.");
        }

        if (expansionResistance.HasValue
            && expansionResistance.GetValueOrDefault().PascalSecondsSquaredPerKilogramSquared <= 0d)
        {
            throw new ArgumentOutOfRangeException(
                nameof(expansionResistance),
                expansionResistance,
                "Turbine expansion resistance must be greater than zero when specified.");
        }

        if (!Enum.IsDefined(admissionPhasePolicy))
        {
            throw new ArgumentOutOfRangeException(nameof(admissionPhasePolicy), admissionPhasePolicy, "Unknown turbine admission phase policy.");
        }

        if (!Enum.IsDefined(energyTransportMode))
        {
            throw new ArgumentOutOfRangeException(nameof(energyTransportMode), energyTransportMode, "Unknown turbine energy-transport mode.");
        }

        var canonicalMoistureDrainNodeId = string.IsNullOrWhiteSpace(moistureDrainNodeId)
            ? null
            : moistureDrainNodeId.Trim();
        if (admissionPhasePolicy == TurbineAdmissionPhasePolicy.VaporMassFractionLimitedWithMoistureDrain)
        {
            if (canonicalMoistureDrainNodeId is null)
            {
                throw new ArgumentException(
                    "A moisture-drain turbine admission policy requires an explicit moisture-drain node id.",
                    nameof(moistureDrainNodeId));
            }
        }
        else if (canonicalMoistureDrainNodeId is not null)
        {
            throw new ArgumentException(
                "A turbine moisture-drain node may be specified only with VaporMassFractionLimitedWithMoistureDrain.",
                nameof(moistureDrainNodeId));
        }

        Id = id.Trim();
        AdmissionBoundaryId = admissionBoundaryId.Trim();
        ExhaustNodeId = exhaustNodeId.Trim();
        RotorId = rotorId.Trim();
        NominalSpecificWork = nominalSpecificWork;
        Efficiency = efficiency;
        ExpansionResistance = expansionResistance;
        ThermodynamicWork = thermodynamicWork;
        AdmissionPhasePolicy = admissionPhasePolicy;
        EnergyTransportMode = energyTransportMode;
        MoistureDrainNodeId = canonicalMoistureDrainNodeId;
    }

    public string Id { get; }

    public string AdmissionBoundaryId { get; }

    public string ExhaustNodeId { get; }

    public string RotorId { get; }

    public SpecificEnergy NominalSpecificWork { get; }

    public TurbineEfficiency Efficiency { get; }

    /// <summary>
    /// Optional pressure-driven hydraulic resistance for the physical expansion path from the admission-boundary source
    /// node to <see cref="ExhaustNodeId"/>. Null preserves the historical upstream-valve-minimum stage-flow law.
    /// </summary>
    public QuadraticHydraulicResistance? ExpansionResistance { get; }

    /// <summary>
    /// Optional current-model work closure. Null preserves the historical fixed nominal-specific-work law.
    /// </summary>
    public TurbineThermodynamicWorkDefinition? ThermodynamicWork { get; }

    /// <summary>
    /// Versioned admission-phase policy. Legacy definitions admit the historical total mixture; current-v2 may opt into
    /// vapor-mass-fraction-limited admission so liquid inventory cannot pass through the stage as a zero-work bypass.
    /// </summary>
    public TurbineAdmissionPhasePolicy AdmissionPhasePolicy { get; }

    /// <summary>
    /// Optional explicit owner for non-vapor admission mass rejected by a moisture-separating admission policy.
    /// Null for every historical admission policy.
    /// </summary>
    public string? MoistureDrainNodeId { get; }

    /// <summary>
    /// Versioned open-control-volume energy-advection convention. Historical definitions transfer specific internal
    /// energy; current-v2 may opt into specific enthalpy while shaft work remains a separate cross-domain transfer.
    /// </summary>
    public FluidEnergyTransportMode EnergyTransportMode { get; }
}
