using NuclearReactorSimulator.Domain.Physics.Quantities;

namespace NuclearReactorSimulator.Simulation.Physics.TurbineIsland.MainSteam;

/// <summary>
/// Temporary M4.1 terminal demand applied at one turbine-inlet node.
/// M4.2 will replace this external sink with turbine expansion while preserving the upstream admission seam.
/// </summary>
public sealed record TurbineAdmissionBoundaryInput
{
    public TurbineAdmissionBoundaryInput(string boundaryId, MassFlowRate massFlowRate)
    {
        if (string.IsNullOrWhiteSpace(boundaryId))
        {
            throw new ArgumentException("Turbine-admission boundary id cannot be empty or whitespace.", nameof(boundaryId));
        }

        if (massFlowRate < MassFlowRate.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(massFlowRate), massFlowRate, "Turbine-admission boundary mass flow cannot be negative.");
        }

        BoundaryId = boundaryId.Trim();
        MassFlowRate = massFlowRate;
    }

    public string BoundaryId { get; }

    public MassFlowRate MassFlowRate { get; }
}
