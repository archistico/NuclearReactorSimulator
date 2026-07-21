namespace NuclearReactorSimulator.Application.ControlRoom;

/// <summary>
/// Stable semantic catalog for the reusable M6.2 presentation primitives.
/// It documents interaction contracts without coupling Application to Avalonia types.
/// </summary>
public static class ControlRoomComponentCatalog
{
    public static IReadOnlyList<ControlRoomComponentDescriptor> Default { get; } =
    [
        new(
            ControlRoomComponentKind.NumericIndicator,
            "Numeric indicator",
            ControlRoomInteractionMode.DisplayOnly,
            "Pointer does not alter state.",
            "Not focusable for operator action."),
        new(
            ControlRoomComponentKind.Meter,
            "Meter",
            ControlRoomInteractionMode.DisplayOnly,
            "Pointer does not alter state.",
            "Not focusable for operator action."),
        new(
            ControlRoomComponentKind.StatusLamp,
            "Status lamp",
            ControlRoomInteractionMode.DisplayOnly,
            "Pointer does not alter state.",
            "Not focusable for operator action."),
        new(
            ControlRoomComponentKind.ToggleSwitch,
            "Toggle switch",
            ControlRoomInteractionMode.Toggle,
            "Primary click toggles the command state.",
            "Space toggles while focused; Tab participates in focus navigation."),
        new(
            ControlRoomComponentKind.Selector,
            "Selector",
            ControlRoomInteractionMode.Selection,
            "Primary click opens/selects an available position.",
            "Arrow keys change selection while focused; Tab participates in focus navigation."),
        new(
            ControlRoomComponentKind.PushButton,
            "Pushbutton",
            ControlRoomInteractionMode.MomentaryCommand,
            "Primary click invokes the bound application command once.",
            "Enter or Space invokes the bound application command while focused."),
    ];
}
