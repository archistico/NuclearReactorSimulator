using NuclearReactorSimulator.Application.ControlRoom;

namespace NuclearReactorSimulator.Application.ControlRoom.Hmi;

public sealed record ControlRoomPlantMimicElementSnapshot(
    string ElementId,
    string DisplayName,
    ControlRoomPlantMimicElementKind Kind,
    double X,
    double Y,
    double Width,
    double Height,
    ControlRoomVisualState State,
    string StatusText,
    string PrimaryValueText,
    string SecondaryValueText,
    string InputText,
    string OutputText,
    string DetailText,
    ControlRoomWorkspaceId DrillDownWorkspaceId);
