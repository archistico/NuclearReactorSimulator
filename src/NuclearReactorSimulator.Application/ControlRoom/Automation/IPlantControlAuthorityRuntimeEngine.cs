using NuclearReactorSimulator.Domain.Physics.Control.Supervisory;

namespace NuclearReactorSimulator.Application.ControlRoom.Automation;

/// <summary>Runtime-facing automation seam. Deterministic supervisory algorithms remain implemented in Simulation/M5.</summary>
public interface IPlantControlAuthorityRuntimeEngine
{
    PlantControlAuthorityPresentationSnapshot CreateAutomationSnapshot();

    void RequestPlantControlAuthority(PlantControlAuthorityMode mode);

    void RequestSupervisoryObjective(SupervisoryObjectiveRequest objective);
}
