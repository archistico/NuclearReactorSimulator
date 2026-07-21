using NuclearReactorSimulator.Application.ControlRoom;

namespace NuclearReactorSimulator.App.ControlRoom;

/// <summary>
/// Fail-closed no-session fallback retained for tests/tooling. The M7.4 desktop composition now loads the exact-version low-power steam-raising/turbine-startup session through the validated M7.1 session boundary.
/// </summary>
internal sealed class ShellControlRoomCommandDispatcher : IControlRoomCommandDispatcher
{
    public ControlRoomCommand? LastCommand { get; private set; }

    public void Dispatch(ControlRoomCommand command)
    {
        LastCommand = command ?? throw new ArgumentNullException(nameof(command));
    }
}
