using NuclearReactorSimulator.Domain.Physics.Quantities;

namespace NuclearReactorSimulator.Simulation.Physics.Electrical;

/// <summary>
/// Explicit M4.5 signed shaft-to-grid power reconciliation. Positive export and negative import share the same closure equation; losses remain non-negative.
/// </summary>
public sealed record GeneratorElectricalAudit(
    Power MechanicalInputPower,
    Power ElectricalExportPower,
    Power ConversionLossPower,
    double PowerClosureResidualWatts);
