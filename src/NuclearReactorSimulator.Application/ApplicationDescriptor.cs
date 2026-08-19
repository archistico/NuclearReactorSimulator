namespace NuclearReactorSimulator.Application;

/// <summary>
/// Describes the currently composed application baseline without coupling the UI to build-time constants.
/// </summary>
public sealed record ApplicationDescriptor(string ProductName, string Milestone, string Status)
{
    public static ApplicationDescriptor Current { get; } = new(
        "Nuclear Reactor Simulator",
        "M10.9.4.1-H.28 Requalification 2 — Performance, Cost & Long-Running Operational Soak",
        "H.28 Requalification 2 reruns the original unchanged H.28 relative-cost and operational-soak ceilings over the user-validated H.28.1-G optimized corrected path. H.28.1-G preserved the exact deterministic trajectory, 35 logical hydraulic evaluations, 32 finite-difference probes and Jacobian dimension 32 while reducing the triggered p95 to 79.7023 ms, below the unchanged H.28 readiness threshold of 88.3812 ms derived from the prior requalification evidence. This package changes no numerical runtime code. P060/F040 thresholds, residual definitions, H.9 tolerances, 2%/5 K branch-continuity limits, the steam|stop-out|header|turbine-inlet target set, H.20 authority, H.22 commit ownership, physical coefficients and the 10 ms simulated fixed step remain unchanged. The original H.28 hard ceilings remain median wall ratio <= 8, p95 wall ratio <= 12 and median allocation ratio <= 16. Standard factories remain ExplicitCommittedState. H.29 default activation remains blocked unless this H.28 requalification passes and the required H.24 long-horizon requalification is subsequently repeated once before activation review. Phase H remains open and Phase I remains deferred.");
}
