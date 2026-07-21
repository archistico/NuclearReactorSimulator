using NuclearReactorSimulator.Domain.Physics.Reactor.ControlRods;

namespace NuclearReactorSimulator.Simulation.Physics.Control;

public sealed record RodActuatorCommand(string ActuatorId, ControlRodCommand Command);
