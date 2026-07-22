namespace NuclearReactorSimulator.Domain.Physics.Control.Supervisory;

/// <summary>Health/effectiveness of the requested plant-control authority.</summary>
public enum PlantControlAuthorityHealth
{
    Normal = 0,
    Degraded = 1,
    SuspendedByProtection = 2,
}
