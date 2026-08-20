namespace NuclearReactorSimulator.Application.Scenarios.Challenges.Demand;

/// <summary>Pure logical-step evaluation of one external-demand profile.</summary>
public sealed record ExternalEnergyDemandProfileEvaluation(
    long OffsetLogicalStep,
    double DemandMegawatts,
    int CurrentControlPointIndex,
    ExternalEnergyDemandInterpolationMode InterpolationMode,
    ExternalEnergyDemandControlPoint? NextControlPoint);
