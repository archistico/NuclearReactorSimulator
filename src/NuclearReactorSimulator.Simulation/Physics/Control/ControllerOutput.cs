namespace NuclearReactorSimulator.Simulation.Physics.Control;

public sealed record ControllerOutput(
    string ControllerId,
    double Output,
    double UnsaturatedOutput,
    bool IsSaturated,
    ControllerExecutionStatus Status);
