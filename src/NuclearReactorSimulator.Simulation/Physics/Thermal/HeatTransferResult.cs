using NuclearReactorSimulator.Domain.Physics.Quantities;

namespace NuclearReactorSimulator.Simulation.Physics.Thermal;

/// <summary>
/// Signed result of one lumped heat-transfer evaluation.
/// Positive heat flow follows the definition's From -> To convention.
/// </summary>
public sealed record HeatTransferResult(
    TemperatureDifference TemperatureDifference,
    Power HeatFlowRate,
    ThermalEnergyBalance FromDomainBalance,
    ThermalEnergyBalance ToDomainBalance);
