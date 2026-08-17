using NuclearReactorSimulator.Domain.Physics.Quantities;

namespace NuclearReactorSimulator.Simulation.Physics.Reactor.PrimaryCircuit.Boundaries;

/// <summary>
/// Per-step controllable feedwater boundary input.
/// The incoming thermodynamic condition always includes specific internal energy and may include explicit enthalpy for current-v2 open-control-volume transport.
/// </summary>
public sealed record FeedwaterBoundaryInput
{
    public FeedwaterBoundaryInput(
        string boundaryId,
        MassFlowRate massFlowRate,
        SpecificEnergy specificInternalEnergy,
        SpecificEnergy? specificEnthalpy = null)
    {
        if (string.IsNullOrWhiteSpace(boundaryId))
        {
            throw new ArgumentException("Feedwater boundary id cannot be empty or whitespace.", nameof(boundaryId));
        }

        if (massFlowRate < MassFlowRate.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(massFlowRate), massFlowRate, "Feedwater mass flow cannot be negative.");
        }

        if (specificInternalEnergy < SpecificEnergy.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(specificInternalEnergy),
                specificInternalEnergy,
                "Feedwater specific internal energy cannot be negative.");
        }

        if (specificEnthalpy is { } enthalpy && enthalpy < specificInternalEnergy)
        {
            throw new ArgumentOutOfRangeException(
                nameof(specificEnthalpy),
                specificEnthalpy,
                "Feedwater specific enthalpy cannot be below its specific internal energy.");
        }

        BoundaryId = boundaryId.Trim();
        MassFlowRate = massFlowRate;
        SpecificInternalEnergy = specificInternalEnergy;
        SpecificEnthalpy = specificEnthalpy;
    }

    public string BoundaryId { get; }

    public MassFlowRate MassFlowRate { get; }

    public SpecificEnergy SpecificInternalEnergy { get; }

    /// <summary>
    /// Optional externally supplied open-control-volume enthalpy. It is required only for positive-flow
    /// boundaries whose definition opts into <c>SpecificEnthalpy</c>; legacy and zero-flow inputs remain compatible.
    /// </summary>
    public SpecificEnergy? SpecificEnthalpy { get; }
}
