namespace NuclearReactorSimulator.Application;

/// <summary>
/// Describes the currently composed application candidate without coupling the UI to build-time constants.
/// </summary>
public sealed record ApplicationDescriptor(string ProductName, string Milestone, string Status)
{
    public static ApplicationDescriptor Current { get; } = new(
        "Nuclear Reactor Simulator",
        "M10.9.7.2 Hotfix 3 REV1 — JsonDocument Parse Exception-Type Test Alignment",
        "M10.9.4.1 / Phase I, M10.9.5, M10.9.6, M10.9.7.1 Hotfix 3, M10.9.7.2 REV1, M10.9.7.2 Hotfix 1 REV1 and M10.9.7.2 Hotfix 2 REV1 are validated; M10.9.6 remains closed. Hotfix 3 REV1 is stacked exclusively on M10.9.7.2 Hotfix 2 REV1 VALIDATED; original Hotfix 3 is superseded/not validated after one Infrastructure test asserted the exact public JsonException type for malformed JSON parsed through JsonDocument.Parse. REV1 changes only that regression assertion to the public-contract-compatible assignable JsonException check; persistence runtime semantics remain identical to Hotfix 3, which restores ControlRoomCommand.NumericValue persistence in session-archive operator actions and recorder events, rejects incomplete manual-demand payloads and undefined persisted command/event enum values at the archive boundary, makes post-incident command persistence DTO-owned, and normalizes malformed scenario/checkpoint/post-incident JSON to InvalidDataException while preserving NotSupportedException for future schemas. Session archive schema v1 and its numeric enum representation remain unchanged and are frozen by regression tests; a string-enum schema migration and streaming persistence APIs remain separately deferred. Replay authority, validated hot-path semantics, F1-F8 navigation, UiRouteActivated=false, scoring, challenge definitions, protection, physics and plant command authority remain unchanged. M10.9.7.3 live Mission/Performance wiring remains blocked until Hotfix 3 REV1 validates."
    );
}
