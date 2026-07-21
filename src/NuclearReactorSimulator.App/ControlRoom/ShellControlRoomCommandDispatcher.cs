using NuclearReactorSimulator.Application.ControlRoom;

namespace NuclearReactorSimulator.App.ControlRoom;

/// <summary>
/// Fail-closed no-session fallback retained for tests/tooling. The M8.1 desktop composition retains the validated M7.7 integrated training session and adds only deterministic scenario fault orchestration/state; concrete fault effects remain later M8 ownership.
/// </summary>
internal sealed class ShellControlRoomCommandDispatcher : IControlRoomCommandDispatcher
{
    public ControlRoomCommand? LastCommand { get; private set; }

    public void Dispatch(ControlRoomCommand command)
    {
        LastCommand = command ?? throw new ArgumentNullException(nameof(command));
    }
}
