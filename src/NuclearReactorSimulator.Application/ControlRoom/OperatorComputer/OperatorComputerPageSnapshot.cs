namespace NuclearReactorSimulator.Application.ControlRoom.OperatorComputer;

public sealed record OperatorComputerPageSnapshot(
    OperatorComputerPageId Id,
    string MenuLabel,
    string Title,
    string Description,
    OperatorComputerPageContentState ContentState);
