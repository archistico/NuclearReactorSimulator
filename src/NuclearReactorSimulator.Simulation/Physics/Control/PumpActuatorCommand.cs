using NuclearReactorSimulator.Domain.Physics.Fluids;

namespace NuclearReactorSimulator.Simulation.Physics.Control;

public sealed record PumpActuatorCommand(string ActuatorId, string PumpId, PumpSpeed RequestedSpeed, bool RunCommand);
