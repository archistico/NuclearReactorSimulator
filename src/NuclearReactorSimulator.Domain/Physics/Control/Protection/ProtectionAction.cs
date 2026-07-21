namespace NuclearReactorSimulator.Domain.Physics.Control.Protection;

[Flags]
public enum ProtectionAction
{
    None = 0,
    ReactorScram = 1,
    TurbineTrip = 2,
    GeneratorTrip = 4,
}
