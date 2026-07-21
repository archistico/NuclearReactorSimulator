using NuclearReactorSimulator.Domain.Physics.Quantities;

namespace NuclearReactorSimulator.Simulation.Physics.Electrical;

/// <summary>
/// Explicit M4.5 shaft-to-grid power reconciliation.
/// </summary>
public sealed record GeneratorElectricalAudit(
    Power MechanicalInputPower,
    Power ElectricalExportPower,
    Power ConversionLossPower,
    double PowerClosureResidualWatts);
