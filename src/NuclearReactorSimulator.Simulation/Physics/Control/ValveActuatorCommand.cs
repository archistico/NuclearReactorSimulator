using NuclearReactorSimulator.Domain.Physics.Fluids;

namespace NuclearReactorSimulator.Simulation.Physics.Control;

public sealed record ValveActuatorCommand(string ActuatorId, string ValveId, ValvePosition RequestedPosition);
