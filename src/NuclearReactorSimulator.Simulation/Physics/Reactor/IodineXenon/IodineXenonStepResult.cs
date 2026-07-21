using NuclearReactorSimulator.Domain.Physics.Reactor.IodineXenon;

namespace NuclearReactorSimulator.Simulation.Physics.Reactor.IodineXenon;

public sealed record IodineXenonStepResult(
    IodineXenonState State,
    IodineXenonSnapshot Snapshot);
