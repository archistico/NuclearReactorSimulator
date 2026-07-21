using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Simulation.Runtime.Replay;

namespace NuclearReactorSimulator.Application.Scenarios;

/// <summary>
/// Deterministic M7.1 replay boundary over the existing M0 logical command-trace primitive. Commands are applied at the
/// declared logical step boundary, then exactly one paused fixed step is executed. Wall clock is never consulted.
/// </summary>
public sealed class ScenarioReplayRunner
{
    private readonly ScenarioSessionFactory _sessionFactory;

    public ScenarioReplayRunner(ScenarioSessionFactory sessionFactory)
    {
        _sessionFactory = sessionFactory ?? throw new ArgumentNullException(nameof(sessionFactory));
    }

    public ScenarioSession ReplayToStep(
        ScenarioDefinition scenario,
        SimulationCommandTrace<ControlRoomCommand> trace,
        long finalStepIndex)
    {
        ArgumentNullException.ThrowIfNull(scenario);
        ArgumentNullException.ThrowIfNull(trace);

        var session = _sessionFactory.Load(scenario);
        var initialStep = session.Coordinator.Current.LogicalStep;
        if (finalStepIndex < initialStep)
        {
            throw new ArgumentOutOfRangeException(nameof(finalStepIndex), "Replay target cannot precede the loaded initial condition.");
        }
        if (trace.LastStepIndex > finalStepIndex)
        {
            throw new ArgumentOutOfRangeException(nameof(finalStepIndex), "Replay target must include every command trace entry.");
        }
        if (trace.Entries.Any(entry => entry.StepIndex <= initialStep))
        {
            throw new ArgumentException("Replay commands must target steps after the loaded initial condition.", nameof(trace));
        }
        if (trace.Entries.Any(entry => ScenarioDefinition.IsRuntimeHostCommand(entry.Command.Kind)))
        {
            throw new ArgumentException("Replay traces may contain operator actions only, not run/pause/single-step host controls.", nameof(trace));
        }

        var traceIndex = 0;
        while (session.Coordinator.Current.LogicalStep < finalStepIndex)
        {
            var nextStep = checked(session.Coordinator.Current.LogicalStep + 1);
            while (traceIndex < trace.Entries.Count && trace.Entries[traceIndex].StepIndex == nextStep)
            {
                session.CommandDispatcher.Dispatch(trace.Entries[traceIndex].Command);
                traceIndex++;
            }

            session.CommandDispatcher.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.SingleStep));
        }

        return session;
    }
}
