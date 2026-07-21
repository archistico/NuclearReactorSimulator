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
        TurbineEfficiency efficiency)
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

        Id = id.Trim();
        AdmissionBoundaryId = admissionBoundaryId.Trim();
        ExhaustNodeId = exhaustNodeId.Trim();
        RotorId = rotorId.Trim();
        NominalSpecificWork = nominalSpecificWork;
        Efficiency = efficiency;
    }

    public string Id { get; }

    public string AdmissionBoundaryId { get; }

    public string ExhaustNodeId { get; }

    public string RotorId { get; }

    public SpecificEnergy NominalSpecificWork { get; }

    public TurbineEfficiency Efficiency { get; }
}
