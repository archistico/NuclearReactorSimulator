using NuclearReactorSimulator.Domain.Physics.Control.Supervisory;

namespace NuclearReactorSimulator.Application.ControlRoom.Automation;

/// <summary>Application intent/status boundary for M10.5/M10.6 plant-control authority. It owns no control algorithm.</summary>
public interface IPlantControlAuthorityDispatcher
{
    event EventHandler<PlantControlAuthorityChangedEventArgs>? AuthorityChanged;

    PlantControlAuthorityPresentationSnapshot CurrentAutomation { get; }

    void RequestAuthority(PlantControlAuthorityMode mode);

    void RequestSupervisoryObjective(SupervisoryObjectiveRequest objective);
}
