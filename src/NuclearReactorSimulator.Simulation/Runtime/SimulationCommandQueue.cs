namespace NuclearReactorSimulator.Simulation.Runtime;

/// <summary>
/// Thread-safe FIFO command queue. Commands are drained only at fixed-timestep boundaries.
/// Failed steps restore their drained commands to the front of the queue in original order.
/// </summary>
public sealed class SimulationCommandQueue<TCommand>
    where TCommand : notnull
{
    private readonly object _syncRoot = new();
    private readonly Queue<QueuedSimulationCommand<TCommand>> _commands = new();
    private long _nextSequence;

    public int Count
    {
        get
        {
            lock (_syncRoot)
            {
                return _commands.Count;
            }
        }
    }

    public long Enqueue(TCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        lock (_syncRoot)
        {
            var sequence = checked(++_nextSequence);
            _commands.Enqueue(new QueuedSimulationCommand<TCommand>(sequence, command));
            return sequence;
        }
    }

    internal IReadOnlyList<QueuedSimulationCommand<TCommand>> Drain()
    {
        lock (_syncRoot)
        {
            if (_commands.Count == 0)
            {
                return Array.Empty<QueuedSimulationCommand<TCommand>>();
            }

            var drained = _commands.ToArray();
            _commands.Clear();
            return drained;
        }
    }

    internal void RestoreToFront(IReadOnlyList<QueuedSimulationCommand<TCommand>> commands)
    {
        ArgumentNullException.ThrowIfNull(commands);

        if (commands.Count == 0)
        {
            return;
        }

        lock (_syncRoot)
        {
            var commandsQueuedAfterDrain = _commands.ToArray();
            _commands.Clear();

            foreach (var command in commands)
            {
                _commands.Enqueue(command);
            }

            foreach (var command in commandsQueuedAfterDrain)
            {
                _commands.Enqueue(command);
            }
        }
    }
}
