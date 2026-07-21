using NuclearReactorSimulator.Domain.Physics.Control.Protection;

namespace NuclearReactorSimulator.Simulation.Physics.Control.Protection;

/// <summary>Explicit operator/manual M5.5 trip and reset commands. Scenario scheduling remains outside protection physics.</summary>
public sealed class ProtectionSystemInputs
{
    public ProtectionSystemInputs(
        ProtectionSystemDefinition definition,
        bool manualReactorScram = false,
        bool manualTurbineTrip = false,
        bool manualGeneratorTrip = false,
        bool resetRequested = false)
    {
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        ManualReactorScram = manualReactorScram;
        ManualTurbineTrip = manualTurbineTrip;
        ManualGeneratorTrip = manualGeneratorTrip;
        ResetRequested = resetRequested;
    }

    public ProtectionSystemDefinition Definition { get; }
    public bool ManualReactorScram { get; }
    public bool ManualTurbineTrip { get; }
    public bool ManualGeneratorTrip { get; }
    public bool ResetRequested { get; }
}
