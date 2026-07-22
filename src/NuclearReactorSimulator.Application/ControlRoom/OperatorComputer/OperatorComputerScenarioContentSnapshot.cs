namespace NuclearReactorSimulator.Application.ControlRoom.OperatorComputer;

public sealed record OperatorComputerScenarioContentSnapshot(
    OperatorComputerGuidanceSnapshot Guidance,
    OperatorComputerDiagnosticsSnapshot Diagnostics);
