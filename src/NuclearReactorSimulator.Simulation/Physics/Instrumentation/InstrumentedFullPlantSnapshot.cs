using NuclearReactorSimulator.Simulation.Physics.TurbineIsland.Integration;

namespace NuclearReactorSimulator.Simulation.Physics.Instrumentation;

/// <summary>M5.1 snapshot exposing true plant state and measured state as deliberately separate boundaries.</summary>
public sealed class InstrumentedFullPlantSnapshot
{
    public InstrumentedFullPlantSnapshot(
        FullPlantSnapshot truePlantState,
        InstrumentationSnapshot instrumentation)
    {
        TruePlantState = truePlantState ?? throw new ArgumentNullException(nameof(truePlantState));
        Instrumentation = instrumentation ?? throw new ArgumentNullException(nameof(instrumentation));
    }

    /// <summary>Diagnostic/physics truth. Future controllers must not consume this directly.</summary>
    public FullPlantSnapshot TruePlantState { get; }

    /// <summary>Instrumentation snapshot containing the controller-facing measured-signal frame.</summary>
    public InstrumentationSnapshot Instrumentation { get; }

    public MeasuredSignalFrame MeasuredSignals => Instrumentation.MeasuredSignals;
}
