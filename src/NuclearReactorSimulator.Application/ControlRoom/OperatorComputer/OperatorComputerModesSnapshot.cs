using NuclearReactorSimulator.Application.ControlRoom.Automation;
using NuclearReactorSimulator.Application.Scenarios.Training;

namespace NuclearReactorSimulator.Application.ControlRoom.OperatorComputer;

public sealed record OperatorComputerModesSnapshot(
    TrainingGuidanceMode TrainingAssistance,
    PlantControlAuthorityPresentationSnapshot PlantControlAuthority);
