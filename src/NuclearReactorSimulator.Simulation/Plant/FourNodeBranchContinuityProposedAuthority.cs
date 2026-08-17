namespace NuclearReactorSimulator.Simulation.Plant;

/// <summary>
/// Shadow-only authority proposed by the H.20 activation contract. This value is observational;
/// PlantNetworkOrchestrator is not wired to consume it in H.20.
/// </summary>
public enum FourNodeBranchContinuityProposedAuthority
{
    ExplicitCommittedState = 0,
    CorrectedCandidate = 1,
}
