namespace NuclearReactorSimulator.Application.ControlRoom.OperatorComputer;

public sealed record OperatorComputerGuidanceStepSnapshot(
    string StepId,
    int Sequence,
    string Title,
    string Instruction,
    OperatorComputerGuidanceStepState State);
