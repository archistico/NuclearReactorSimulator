using NuclearReactorSimulator.Application.ControlRoom;

namespace NuclearReactorSimulator.App.ControlRoom;

/// <summary>
/// Fail-closed no-session fallback retained for tests/tooling. The M7.7 desktop composition now loads the integrated normal-operations training session through the validated M7.1 session boundary and overlays observational training evaluation only.
/// </summary>
internal sealed class ShellControlRoomCommandDispatcher : IControlRoomCommandDispatcher
{
    public ControlRoomCommand? LastCommand { get; private set; }

    public void Dispatch(ControlRoomCommand command)
    {
        LastCommand = command ?? throw new ArgumentNullException(nameof(command));
    }
}
