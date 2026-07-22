using NuclearReactorSimulator.Domain.Physics.Control;

namespace NuclearReactorSimulator.Application.ControlRoom.Automation;

public sealed record PlantControllerModePresentationSnapshot(
    string ControllerId,
    string Area,
    ControllerMode Mode,
    double Setpoint,
    string SetpointUnit);
