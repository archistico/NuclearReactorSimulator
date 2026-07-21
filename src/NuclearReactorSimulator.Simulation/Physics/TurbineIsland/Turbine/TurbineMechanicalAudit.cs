using NuclearReactorSimulator.Domain.Physics.Quantities;

namespace NuclearReactorSimulator.Simulation.Physics.TurbineIsland.Turbine;

/// <summary>
/// Raw M4.2 mechanical-domain energy audit. No residual is hidden or corrected.
/// </summary>
public sealed record TurbineMechanicalAudit(
    Energy InitialRotorKineticEnergy,
    Energy FinalRotorKineticEnergy,
    Power TotalShaftPower,
    Power TotalExternalLoadPower,
    double MechanicalEnergyClosureResidualJoules)
{
    public bool IsEnergyClosedWithin(double toleranceJoules)
    {
        if (!double.IsFinite(toleranceJoules) || toleranceJoules < 0d)
        {
            throw new ArgumentOutOfRangeException(nameof(toleranceJoules), toleranceJoules, "Tolerance must be finite and non-negative.");
        }

        return Math.Abs(MechanicalEnergyClosureResidualJoules) <= toleranceJoules;
    }
}
