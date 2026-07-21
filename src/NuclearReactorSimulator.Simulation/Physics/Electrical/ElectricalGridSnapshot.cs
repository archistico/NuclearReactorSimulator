using NuclearReactorSimulator.Domain.Physics.Quantities;

namespace NuclearReactorSimulator.Simulation.Physics.Electrical;

public sealed record ElectricalGridSnapshot(
    string GridId,
    Frequency Frequency,
    ElectricPotential LineVoltage,
    PhaseAngle InitialPhaseAngle,
    PhaseAngle FinalPhaseAngle);
