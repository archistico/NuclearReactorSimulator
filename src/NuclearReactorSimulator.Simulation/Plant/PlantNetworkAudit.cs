using NuclearReactorSimulator.Domain.Physics.Quantities;

namespace NuclearReactorSimulator.Simulation.Plant;

/// <summary>
/// Immutable conservation audit for one composed-plant network step.
/// Residuals are signed raw SI values so tiny floating-point closure errors remain observable.
/// </summary>
public sealed record PlantNetworkAudit(
    Mass InitialTotalMass,
    Mass FinalTotalMass,
    MassFlowRate NetAccumulatedMassRate,
    MassFlowRate ExpectedExternalMassFlowRate,
    MassFlowRate SupplementalExternalMassFlowRate,
    double BalanceMassRateResidualKilogramsPerSecond,
    double MassClosureResidualKilograms,
    Energy InitialTotalStoredEnergy,
    Energy FinalTotalStoredEnergy,
    Power NetAccumulatedEnergyRate,
    Power ExpectedExternalPower,
    Power PumpHydraulicPowerExchange,
    Power HeatSourcePower,
    Power SupplementalExternalPower,
    double BalancePowerResidualWatts,
    double EnergyClosureResidualJoules)
{
    public bool IsBalanceMassRateClosedWithin(double toleranceKilogramsPerSecond)
        => Math.Abs(BalanceMassRateResidualKilogramsPerSecond)
            <= ValidateTolerance(toleranceKilogramsPerSecond, nameof(toleranceKilogramsPerSecond));

    public bool IsMassClosedWithin(double toleranceKilograms)
        => Math.Abs(MassClosureResidualKilograms) <= ValidateTolerance(toleranceKilograms, nameof(toleranceKilograms));

    public bool IsBalancePowerClosedWithin(double toleranceWatts)
        => Math.Abs(BalancePowerResidualWatts) <= ValidateTolerance(toleranceWatts, nameof(toleranceWatts));

    public bool IsEnergyClosedWithin(double toleranceJoules)
        => Math.Abs(EnergyClosureResidualJoules) <= ValidateTolerance(toleranceJoules, nameof(toleranceJoules));

    private static double ValidateTolerance(double value, string parameterName)
    {
        if (!double.IsFinite(value) || value < 0d)
        {
            throw new ArgumentOutOfRangeException(parameterName, value, "Conservation tolerance must be finite and non-negative.");
        }

        return value;
    }
}
