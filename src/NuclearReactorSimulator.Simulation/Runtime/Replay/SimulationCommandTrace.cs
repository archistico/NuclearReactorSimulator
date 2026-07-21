namespace NuclearReactorSimulator.Simulation.Runtime.Replay;

/// <summary>
/// Immutable deterministic command script. It deliberately contains no wall-clock timestamps.
/// </summary>
public sealed class SimulationCommandTrace<TCommand>
    where TCommand : notnull
{
    private readonly IReadOnlyList<SimulationCommandTraceEntry<TCommand>> _entries;

    public SimulationCommandTrace(IEnumerable<SimulationCommandTraceEntry<TCommand>> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);

        var materialized = entries.ToArray();
        long previousStep = 0;

        foreach (var entry in materialized)
        {
            ArgumentNullException.ThrowIfNull(entry);
            ArgumentNullException.ThrowIfNull(entry.Command);

            if (entry.StepIndex <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(entries),
                    "Command trace step indexes must be positive.");
            }

            if (entry.StepIndex < previousStep)
            {
                throw new ArgumentException(
                    "Command trace entries must be ordered by nondecreasing step index.",
                    nameof(entries));
            }

            previousStep = entry.StepIndex;
        }

        _entries = Array.AsReadOnly(materialized);
    }

    public IReadOnlyList<SimulationCommandTraceEntry<TCommand>> Entries => _entries;

    public long LastStepIndex => _entries.Count == 0 ? 0 : _entries[^1].StepIndex;
}
