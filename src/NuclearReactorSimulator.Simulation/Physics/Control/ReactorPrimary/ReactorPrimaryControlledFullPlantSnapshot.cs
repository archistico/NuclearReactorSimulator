using NuclearReactorSimulator.Simulation.Physics.TurbineIsland.Integration;

namespace NuclearReactorSimulator.Simulation.Physics.Control.ReactorPrimary;

/// <summary>M5.3 combined observation: validated M4.7 true plant result plus measured-signal-driven reactor/primary control diagnostics.</summary>
public sealed class ReactorPrimaryControlledFullPlantSnapshot
{
    public ReactorPrimaryControlledFullPlantSnapshot(
        FullPlantSnapshot truePlantState,
        ReactorPrimaryControlSnapshot control)
    {
        TruePlantState = truePlantState ?? throw new ArgumentNullException(nameof(truePlantState));
        Control = control ?? throw new ArgumentNullException(nameof(control));
    }

    public FullPlantSnapshot TruePlantState { get; }
    public ReactorPrimaryControlSnapshot Control { get; }
}
