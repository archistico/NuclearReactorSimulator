namespace NuclearReactorSimulator.Simulation.Plant;

/// <summary>
/// H.28.1-A diagnostic-only attribution for one H.9 corrector invocation. These measurements describe
/// implementation cost only; they do not participate in numerical decisions or plant physics.
/// </summary>
internal sealed record JacobianHydraulicCorrectorPerformanceAttribution(
    long TotalElapsedTicks,
    long TotalAllocatedBytes,
    long CoordinateLayoutElapsedTicks,
    long CoordinateLayoutAllocatedBytes,
    long InitialResidualElapsedTicks,
    long InitialResidualAllocatedBytes,
    long JacobianBuildElapsedTicks,
    long JacobianBuildAllocatedBytes,
    long NewtonLineSearchElapsedTicks,
    long NewtonLineSearchAllocatedBytes,
    long ResidualFallbackElapsedTicks,
    long ResidualFallbackAllocatedBytes,
    long OtherElapsedTicks,
    long OtherAllocatedBytes)
{
    /// <summary>H.28.1-D exact-reference reuse count for the first fluid integration of finite-difference probes.</summary>
    public int ProbeAppliedFluidNodeReuseCount { get; init; }

    /// <summary>H.28.1-D total fluid-node opportunities in the first integration of finite-difference probes.</summary>
    public int ProbeAppliedFluidNodeCount { get; init; }

    /// <summary>H.28.1-D exact-reference reuse count for mapped fixed-point fluid integrations inside probes.</summary>
    public int ProbeMappedFluidNodeReuseCount { get; init; }

    /// <summary>H.28.1-D total fluid-node opportunities in mapped fixed-point fluid integrations inside probes.</summary>
    public int ProbeMappedFluidNodeCount { get; init; }

    /// <summary>H.28.1-E exact-reference reuse count for pipe/valve/pump component results inside probe hydraulic maps.</summary>
    public int ProbeHydraulicComponentReuseCount { get; init; }

    /// <summary>H.28.1-E total pipe/valve/pump component opportunities inside probe hydraulic maps.</summary>
    public int ProbeHydraulicComponentCount { get; init; }
}
