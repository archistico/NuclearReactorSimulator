namespace NuclearReactorSimulator.Simulation.Runtime;

/// <summary>
/// Tracks logical simulation time. It has no dependency on the operating-system clock.
/// </summary>
public sealed class SimulationClock
{
    public SimulationClock(TimeSpan fixedTimeStep)
    {
        if (fixedTimeStep <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(fixedTimeStep), "The fixed timestep must be positive.");
        }

        FixedTimeStep = fixedTimeStep;
    }

    public TimeSpan FixedTimeStep { get; }

    public long StepIndex { get; private set; }

    public TimeSpan ElapsedSimulationTime { get; private set; }

    internal SimulationStepContext CreateNextStepContext()
    {
        var nextStepIndex = checked(StepIndex + 1);
        var endTime = ElapsedSimulationTime + FixedTimeStep;

        return new SimulationStepContext(
            nextStepIndex,
            ElapsedSimulationTime,
            endTime,
            FixedTimeStep);
    }

    internal void CommitStep(SimulationStepContext context)
    {
        if (context.StepIndex != checked(StepIndex + 1))
        {
            throw new InvalidOperationException("The step context is not the next logical simulation step.");
        }

        if (context.StartTime != ElapsedSimulationTime || context.DeltaTime != FixedTimeStep)
        {
            throw new InvalidOperationException("The step context does not match the current simulation clock.");
        }

        StepIndex = context.StepIndex;
        ElapsedSimulationTime = context.EndTime;
    }
}
