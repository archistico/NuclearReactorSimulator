using NuclearReactorSimulator.Application.ControlRoom;

namespace NuclearReactorSimulator.Application.Scenarios;

/// <summary>
/// Reconstructs a fresh deterministic control-room runtime from one immutable initial-condition version. Concrete factories
/// own canonical lower-layer composition; scenario/session code must never synthesize or patch physical state itself.
/// </summary>
public interface IVersionedInitialConditionFactory
{
    InitialConditionDescriptor Descriptor { get; }

    IControlRoomRuntimeEngine CreateRuntimeEngine();
}
