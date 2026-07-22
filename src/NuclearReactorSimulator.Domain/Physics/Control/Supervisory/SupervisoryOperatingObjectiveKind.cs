namespace NuclearReactorSimulator.Domain.Physics.Control.Supervisory;

/// <summary>Bounded high-level objectives supported by the initial M10.6 deterministic supervisor.</summary>
public enum SupervisoryOperatingObjectiveKind
{
    HoldReactorPower = 0,
    HoldTurbineSpeed = 1,
    HoldOperatingPoint = 2,
}
