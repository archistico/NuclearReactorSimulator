using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.Thermal;

namespace NuclearReactorSimulator.Simulation.Physics.Thermal;

/// <summary>
/// Resolves an external heat source into a signed thermal-energy balance.
/// </summary>
public sealed class HeatSourceSolver
{
    public ThermalEnergyBalance Solve(HeatSourceDefinition definition, HeatSourceState state)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(state);

        if (!string.Equals(definition.Id, state.HeatSourceId, StringComparison.Ordinal))
        {
            throw new ArgumentException(
                $"Heat source '{definition.Id}' received state for source '{state.HeatSourceId}'.",
                nameof(state));
        }

        return new ThermalEnergyBalance(state.IsEnabled ? definition.RatedThermalPower : Power.Zero);
    }
}
