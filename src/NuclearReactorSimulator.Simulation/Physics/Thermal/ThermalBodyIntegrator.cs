using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.Thermal;

namespace NuclearReactorSimulator.Simulation.Physics.Thermal;

/// <summary>
/// Deterministically integrates one lumped thermal body's conserved stored-energy balance.
/// </summary>
public sealed class ThermalBodyIntegrator
{
    public ThermalBodyState Step(
        ThermalBodyState state,
        ThermalEnergyBalance balance,
        TimeSpan deltaTime)
    {
        ArgumentNullException.ThrowIfNull(state);

        if (deltaTime <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(deltaTime), deltaTime, "Thermal integration time must be greater than zero.");
        }

        var candidateEnergyJoules = state.StoredThermalEnergy.Joules
            + balance.NetHeatRate.Over(deltaTime).Joules;

        if (!double.IsFinite(candidateEnergyJoules))
        {
            throw new ArithmeticException($"Thermal body '{state.Id}' energy integration produced a non-finite result.");
        }

        if (candidateEnergyJoules < 0d)
        {
            throw new ThermalBodyEnergyDepletionException(state.Id, candidateEnergyJoules);
        }

        return new ThermalBodyState(
            state.Definition,
            Energy.FromJoules(candidateEnergyJoules));
    }
}
