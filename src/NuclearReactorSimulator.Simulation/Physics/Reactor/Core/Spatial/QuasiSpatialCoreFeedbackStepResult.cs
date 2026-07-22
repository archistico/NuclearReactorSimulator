using NuclearReactorSimulator.Domain.Physics.Reactor.Core;

namespace NuclearReactorSimulator.Simulation.Physics.Reactor.Core.Spatial;

public sealed record QuasiSpatialCoreFeedbackStepResult(
    AggregatedCoreState CandidateState,
    QuasiSpatialCoreFeedbackSnapshot Snapshot);
