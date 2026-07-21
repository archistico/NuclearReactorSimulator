namespace NuclearReactorSimulator.Application.ControlRoom;

/// <summary>
/// Cooperative accelerated-execution budget. A host may run many batches, but each call is bounded so the presentation
/// host can regain control between batches. This budget never changes the deterministic simulation timestep.
/// </summary>
public sealed record ControlRoomRuntimeExecutionBudget
{
    public ControlRoomRuntimeExecutionBudget(int maximumSimulationStepsPerBatch)
    {
        if (maximumSimulationStepsPerBatch <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumSimulationStepsPerBatch));
        }

        MaximumSimulationStepsPerBatch = maximumSimulationStepsPerBatch;
    }

    public static ControlRoomRuntimeExecutionBudget DesktopDefault { get; } = new(256);

    public int MaximumSimulationStepsPerBatch { get; }
}
