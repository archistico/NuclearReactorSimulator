namespace NuclearReactorSimulator.Application.ControlRoom.OperatorComputer;

public sealed record OperatorComputerPageDescriptor(
    OperatorComputerPageId Id,
    string MenuLabel,
    string Title,
    string Description);
