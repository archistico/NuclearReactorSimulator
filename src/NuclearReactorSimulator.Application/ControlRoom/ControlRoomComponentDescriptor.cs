namespace NuclearReactorSimulator.Application.ControlRoom;

public sealed record ControlRoomComponentDescriptor(
    ControlRoomComponentKind Kind,
    string DisplayName,
    ControlRoomInteractionMode InteractionMode,
    string PointerRule,
    string KeyboardRule);
