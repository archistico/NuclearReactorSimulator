using NuclearReactorSimulator.Application.ControlRoom;

namespace NuclearReactorSimulator.Application.Scenarios.Faults;

/// <summary>Creates one runtime-bound applicator for a stable fault-type identifier.</summary>
public interface IScenarioFaultApplicatorFactory
{
    string FaultTypeId { get; }

    IScenarioFaultApplicator Create(IControlRoomRuntimeEngine runtimeEngine);
}
