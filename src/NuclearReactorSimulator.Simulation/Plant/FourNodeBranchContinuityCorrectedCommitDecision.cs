namespace NuclearReactorSimulator.Simulation.Plant;

/// <summary>
/// H.22 per-step corrected-candidate ownership decision. The H.20 activation decision remains unchanged;
/// this second seam is the only place where an explicitly opted-in H.22 path may authorize corrected-state ownership.
/// </summary>
public sealed record FourNodeBranchContinuityCorrectedCommitDecision(
    bool CommitArmEnabled,
    bool CorrectedCandidateAvailable,
    bool CommitAuthorized,
    FourNodeBranchContinuityCorrectedCommitReason Reason);
