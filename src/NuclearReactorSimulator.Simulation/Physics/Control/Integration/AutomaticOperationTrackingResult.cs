namespace NuclearReactorSimulator.Simulation.Physics.Control.Integration;

public sealed record AutomaticOperationTrackingResult(
    string ChannelId,
    double TargetEngineeringValue,
    double? FinalMeasuredValue,
    double? AbsoluteFinalError,
    double MaximumAbsoluteAllowedError,
    bool Satisfied);
