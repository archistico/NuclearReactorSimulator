namespace NuclearReactorSimulator.Domain.Physics.Control.Supervisory;

/// <summary>Global normal-operation authority requested by the operator. Protection remains superior in every mode.</summary>
public enum PlantControlAuthorityMode
{
    Manual = 0,
    Assisted = 1,
    SupervisoryAutomatic = 2,
}
