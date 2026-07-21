using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.Thermal;

namespace NuclearReactorSimulator.Simulation.Physics.Thermal;

/// <summary>
/// Stateless deterministic lumped heat-transfer solver using Qdot = G * (Tfrom - Tto).
/// </summary>
public sealed class HeatTransferSolver
{
    public HeatTransferResult Solve(
        HeatTransferDefinition definition,
        Temperature fromTemperature,
        Temperature toTemperature)
    {
        ArgumentNullException.ThrowIfNull(definition);

        var temperatureDifference = fromTemperature - toTemperature;
        var heatFlowRate = definition.Conductance * temperatureDifference;

        return new HeatTransferResult(
            temperatureDifference,
            heatFlowRate,
            new ThermalEnergyBalance(-heatFlowRate),
            new ThermalEnergyBalance(heatFlowRate));
    }
}
