namespace NuclearReactorSimulator.Simulation.Plant;

/// <summary>
/// Deterministic observational evidence about local branch switching and derivative scale sensitivity
/// in the hydraulic/thermodynamic map around one shadow state.
/// </summary>
public sealed record HydraulicMapSmoothnessReport(
    IReadOnlyList<HydraulicPathSmoothnessProbe> HydraulicPaths,
    IReadOnlyList<ThermodynamicNodeSmoothnessProbe> ThermodynamicNodes)
{
    public int HydraulicBranchSwitchCount => HydraulicPaths.Count(static item => item.BranchSwitchObserved);

    public int HydraulicNonSmoothEvidenceCount => HydraulicPaths.Count(static item => item.NonSmoothEvidence);

    public int ThermodynamicPhaseSwitchCount => ThermodynamicNodes.Count(static item => item.PhaseOrEnvelopeSwitchObserved);

    public int ThermodynamicNonSmoothEvidenceCount => ThermodynamicNodes.Count(static item => item.NonSmoothEvidence);

    public double MaximumHydraulicDerivativeScaleGrowth => HydraulicPaths.Count == 0
        ? 0d
        : HydraulicPaths.Max(static item => item.DerivativeScaleGrowth);

    public double MaximumHydraulicOneSidedSlopeAsymmetry => HydraulicPaths.Count == 0
        ? 0d
        : HydraulicPaths.Max(static item => item.OneSidedSlopeAsymmetry);

    public double MaximumThermodynamicDerivativeScaleGrowth => ThermodynamicNodes.Count == 0
        ? 0d
        : ThermodynamicNodes.Max(static item => item.MaximumDerivativeScaleGrowth);
}

public sealed record HydraulicPathSmoothnessProbe(
    string ComponentKind,
    string ComponentId,
    string FromNodeId,
    string ToNodeId,
    string BaseBranch,
    string CoarseMinusBranch,
    string CoarsePlusBranch,
    string FineMinusBranch,
    string FinePlusBranch,
    double BaseDrivingPressurePascals,
    double CoarsePressureProbePascals,
    double BaseMassFlowKilogramsPerSecond,
    double CoarseMinusMassFlowKilogramsPerSecond,
    double CoarsePlusMassFlowKilogramsPerSecond,
    double FineMinusMassFlowKilogramsPerSecond,
    double FinePlusMassFlowKilogramsPerSecond,
    double CoarseCentralSlopeKilogramsPerSecondPerPascal,
    double FineCentralSlopeKilogramsPerSecondPerPascal,
    double DerivativeScaleGrowth,
    double OneSidedSlopeAsymmetry,
    bool BranchSwitchObserved,
    bool NonSmoothEvidence);

public sealed record ThermodynamicNodeSmoothnessProbe(
    string NodeId,
    string BasePhase,
    string EnergyMinusPhase,
    string EnergyPlusPhase,
    string MassMinusPhase,
    string MassPlusPhase,
    bool EnergyMinusResolved,
    bool EnergyPlusResolved,
    bool MassMinusResolved,
    bool MassPlusResolved,
    double BasePressurePascals,
    double EnergyDerivativeScaleGrowth,
    double MassDerivativeScaleGrowth,
    bool PhaseOrEnvelopeSwitchObserved,
    bool NonSmoothEvidence)
{
    public double MaximumDerivativeScaleGrowth => Math.Max(EnergyDerivativeScaleGrowth, MassDerivativeScaleGrowth);
}
