namespace NuclearReactorSimulator.Application.ControlRoom.OperatorComputer;

public sealed record OperatorComputerDiagnosticItemSnapshot(
    string CheckId,
    string Title,
    bool IsSatisfied,
    string Observation);
