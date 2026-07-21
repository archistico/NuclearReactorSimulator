namespace NuclearReactorSimulator.Domain.Physics.Control.ReactorPrimary;

/// <summary>Plant-specific M5.3 reactor/primary control-loop roles.</summary>
public enum ReactorPrimaryControlLoopKind
{
    ReactorPowerRodRegulation = 0,
    MainCirculationPumpFlow = 1,
    MainCirculationHeaderPressure = 2,
}
