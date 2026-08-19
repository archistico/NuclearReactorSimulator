using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Plant;
using NuclearReactorSimulator.Simulation.Physics.Fluids;

namespace NuclearReactorSimulator.Simulation.Plant;

/// <summary>
/// H.21/H.22 orchestrator-side evaluation of the user-validated H.19 four-node policy and unchanged H.20
/// fail-closed authority contract. H.21 observes the corrected result only; H.22 may pass it to a separately
/// reviewed commit seam while preserving immediate explicit fallback.
/// </summary>
public sealed class FourNodeBranchContinuityShadowIntegrationSolver
{
    private readonly IFluidThermodynamicModel _productionThermodynamics;
    private readonly IWaterSteamInverseBranchDiagnosticProvider _diagnosticProvider;
    private readonly HybridSemiImplicitHydraulicGateSolver _predictorGate;
    private readonly FourNodeBranchContinuityShadowActivationSupervisor _activationSupervisor = new();
    private readonly HashSet<string> _targetNodeIds;

    public FourNodeBranchContinuityShadowIntegrationSolver(
        IFluidThermodynamicModel productionThermodynamics,
        IWaterSteamInverseBranchDiagnosticProvider diagnosticProvider)
    {
        _productionThermodynamics = productionThermodynamics ?? throw new ArgumentNullException(nameof(productionThermodynamics));
        _diagnosticProvider = diagnosticProvider ?? throw new ArgumentNullException(nameof(diagnosticProvider));
        _predictorGate = new HybridSemiImplicitHydraulicGateSolver(productionThermodynamics);
        _targetNodeIds = new HashSet<string>(
            FourNodeBranchContinuityActivationOptions.H19QualifiedShadowOnly.TargetNodeIds,
            StringComparer.Ordinal);
    }

    public FourNodeBranchContinuityShadowIntegrationStepResult Step(
        PlantState committedState,
        TimeSpan deltaTime,
        IReadOnlyDictionary<string, FluidNodeBalance> frozenNonHydraulicBalances)
    {
        ValidateStepArguments(committedState, deltaTime, frozenNonHydraulicBalances);

        var totalStartedTicks = PerformanceAttributionMeasurement.ReadTimestamp();
        var totalAllocatedBefore = PerformanceAttributionMeasurement.ReadAllocatedBytes();
        var predictorStartedTicks = PerformanceAttributionMeasurement.ReadTimestamp();
        var predictorAllocatedBefore = PerformanceAttributionMeasurement.ReadAllocatedBytes();
        var predictor = _predictorGate.EvaluatePredictor(committedState, deltaTime, frozenNonHydraulicBalances);
        var predictorElapsedTicks = PerformanceAttributionMeasurement.ReadTimestamp() - predictorStartedTicks;
        var predictorAllocatedBytes = Math.Max(0L, PerformanceAttributionMeasurement.ReadAllocatedBytes() - predictorAllocatedBefore);

        return CompleteStep(
            committedState,
            deltaTime,
            frozenNonHydraulicBalances,
            predictor,
            totalStartedTicks,
            totalAllocatedBefore,
            predictorElapsedTicks,
            predictorAllocatedBytes,
            historicalPredictorFluidNodeReuseCount: 0,
            historicalPredictorFluidNodeCount: 0);
    }

    /// <summary>
    /// H.28.1-B path used by the real four-node orchestrator. It reuses the already-computed historical
    /// explicit fluid-node result only where the historical applied total balance exactly equals the canonical
    /// H.4 balance; any mismatch is reintegrated through the unchanged predictor path.
    /// </summary>
    internal FourNodeBranchContinuityShadowIntegrationStepResult StepWithHistoricalExplicitPredictorCandidate(
        PlantState committedState,
        TimeSpan deltaTime,
        IReadOnlyDictionary<string, FluidNodeBalance> frozenNonHydraulicBalances,
        SemiImplicitHydraulicEvaluation committedHydraulicEvaluation,
        PlantState historicalExplicitCandidateState,
        IReadOnlyDictionary<string, FluidNodeBalance> historicalAppliedTotalBalances)
    {
        ValidateStepArguments(committedState, deltaTime, frozenNonHydraulicBalances);
        ArgumentNullException.ThrowIfNull(committedHydraulicEvaluation);
        ArgumentNullException.ThrowIfNull(historicalExplicitCandidateState);
        ArgumentNullException.ThrowIfNull(historicalAppliedTotalBalances);

        var totalStartedTicks = PerformanceAttributionMeasurement.ReadTimestamp();
        var totalAllocatedBefore = PerformanceAttributionMeasurement.ReadAllocatedBytes();
        var predictorStartedTicks = PerformanceAttributionMeasurement.ReadTimestamp();
        var predictorAllocatedBefore = PerformanceAttributionMeasurement.ReadAllocatedBytes();
        var predictor = _predictorGate.EvaluatePredictorFromHistoricalExplicitCandidate(
            committedState,
            deltaTime,
            frozenNonHydraulicBalances,
            committedHydraulicEvaluation,
            historicalExplicitCandidateState,
            historicalAppliedTotalBalances,
            out var reusedFluidNodeCount);
        var predictorElapsedTicks = PerformanceAttributionMeasurement.ReadTimestamp() - predictorStartedTicks;
        var predictorAllocatedBytes = Math.Max(0L, PerformanceAttributionMeasurement.ReadAllocatedBytes() - predictorAllocatedBefore);

        return CompleteStep(
            committedState,
            deltaTime,
            frozenNonHydraulicBalances,
            predictor,
            totalStartedTicks,
            totalAllocatedBefore,
            predictorElapsedTicks,
            predictorAllocatedBytes,
            reusedFluidNodeCount,
            committedState.FluidNodes.Count);
    }

    private FourNodeBranchContinuityShadowIntegrationStepResult CompleteStep(
        PlantState committedState,
        TimeSpan deltaTime,
        IReadOnlyDictionary<string, FluidNodeBalance> frozenNonHydraulicBalances,
        HybridSemiImplicitHydraulicGateStepResult predictor,
        long totalStartedTicks,
        long totalAllocatedBefore,
        long predictorElapsedTicks,
        long predictorAllocatedBytes,
        int historicalPredictorFluidNodeReuseCount,
        int historicalPredictorFluidNodeCount)
    {
        var activationOptions = FourNodeBranchContinuityActivationOptions.H19QualifiedShadowOnly
            .WithActivationArmEnabled(true);
        var triggerObserved = predictor.PredictorMaximumFractionalSubcooledPressureChange
                >= activationOptions.PredictedPressureChangeTriggerFraction
            || predictor.PredictorMaximumAbsoluteHydraulicFlowChangeKilogramsPerSecond
                >= activationOptions.PredictedFlowChangeTriggerKilogramsPerSecond;

        if (!triggerObserved)
        {
            var observation = new FourNodeBranchContinuityActivationObservation(
                "h21-orchestrator-step",
                triggerObserved: false,
                qualificationEvidenceAccepted: true,
                correctorConverged: true,
                lineSearchExhausted: false,
                relativePressureResidual: 0d,
                absoluteFlowResidualKilogramsPerSecond: 0d,
                massClosureKilogramsPerSecond: 0d,
                energyOwnershipResidualWatts: 0d,
                untargetedBranchDisagreementDetected: false);
            var noTriggerAuthorityStartedTicks = PerformanceAttributionMeasurement.ReadTimestamp();
            var noTriggerAuthorityAllocatedBefore = PerformanceAttributionMeasurement.ReadAllocatedBytes();
            var decision = _activationSupervisor.Evaluate(observation, activationOptions);
            var noTriggerAuthorityElapsedTicks = PerformanceAttributionMeasurement.ReadTimestamp() - noTriggerAuthorityStartedTicks;
            var noTriggerAuthorityAllocatedBytes = Math.Max(0L, PerformanceAttributionMeasurement.ReadAllocatedBytes() - noTriggerAuthorityAllocatedBefore);
            var noTriggerSidecarElapsedTicks = PerformanceAttributionMeasurement.ReadTimestamp() - totalStartedTicks;
            var noTriggerSidecarAllocatedBytes = Math.Max(0L, PerformanceAttributionMeasurement.ReadAllocatedBytes() - totalAllocatedBefore);
            var noTriggerResult = new FourNodeBranchContinuityShadowIntegrationStepResult(
                predictor,
                decision,
                ShadowCorrectionEvaluated: false,
                UntargetedBranchDisagreementDetected: false,
                Array.Empty<string>(),
                BranchOverrideCount: 0,
                PreviousPhaseHoldCount: 0,
                HysteresisReleaseCount: 0,
                ShadowIterationCount: 0,
                ShadowConverged: true,
                ShadowLineSearchExhausted: false,
                ShadowMaximumRelativePressureResidual: 0d,
                ShadowMaximumAbsoluteFlowResidualKilogramsPerSecond: 0d,
                ShadowMassClosureKilogramsPerSecond: 0d,
                ShadowEnergyOwnershipResidualWatts: 0d);
            var noTriggerAttribution = new FourNodeBranchContinuityPerformanceAttribution(
                OrchestratorElapsedTicks: 0,
                OrchestratorAllocatedBytes: 0,
                HistoricalExplicitPreparationElapsedTicks: 0,
                HistoricalExplicitPreparationAllocatedBytes: 0,
                SidecarElapsedTicks: noTriggerSidecarElapsedTicks,
                SidecarAllocatedBytes: noTriggerSidecarAllocatedBytes,
                PredictorElapsedTicks: predictorElapsedTicks,
                PredictorAllocatedBytes: predictorAllocatedBytes,
                CorrectorElapsedTicks: 0,
                CorrectorAllocatedBytes: 0,
                UntargetedDisagreementScanElapsedTicks: 0,
                UntargetedDisagreementScanAllocatedBytes: 0,
                AuthorityEvaluationElapsedTicks: noTriggerAuthorityElapsedTicks,
                AuthorityEvaluationAllocatedBytes: noTriggerAuthorityAllocatedBytes,
                CommitAndAccountingElapsedTicks: 0,
                CommitAndAccountingAllocatedBytes: 0,
                HydraulicEvaluationCount: 0,
                ProbeEvaluationCount: 0,
                MaximumJacobianDimension: 0,
                JacobianBuildAttempts: 0,
                JacobianDirectionAcceptances: 0,
                JacobianRejectedCount: 0,
                ResidualFallbackAttempts: 0,
                ResidualFallbackAcceptances: 0,
                BacktrackingTrialCount: 0)
            {
                HistoricalPredictorFluidNodeReuseCount = historicalPredictorFluidNodeReuseCount,
                HistoricalPredictorFluidNodeCount = historicalPredictorFluidNodeCount,
            };
            FourNodeBranchContinuitySidecarPerformanceAttributionRegistry.Set(noTriggerResult, noTriggerAttribution);
            return noTriggerResult;
        }

        var correctorStartedTicks = PerformanceAttributionMeasurement.ReadTimestamp();
        var correctorAllocatedBefore = PerformanceAttributionMeasurement.ReadAllocatedBytes();
        var shadowThermodynamics = new ThermodynamicBranchContinuityModel(
            _productionThermodynamics,
            _diagnosticProvider,
            ThermodynamicBranchContinuityOptions.H13BoundedHysteresis,
            activationOptions.TargetNodeIds);
        var corrector = new JacobianHydraulicCorrectorSolver(shadowThermodynamics);
        var corrected = corrector.Step(
            committedState,
            deltaTime,
            frozenNonHydraulicBalances,
            JacobianHydraulicCorrectorOptions.H9AuditDefault);
        var correctorElapsedTicks = PerformanceAttributionMeasurement.ReadTimestamp() - correctorStartedTicks;
        var correctorAllocatedBytes = Math.Max(0L, PerformanceAttributionMeasurement.ReadAllocatedBytes() - correctorAllocatedBefore);

        var disagreementStartedTicks = PerformanceAttributionMeasurement.ReadTimestamp();
        var disagreementAllocatedBefore = PerformanceAttributionMeasurement.ReadAllocatedBytes();
        var untargetedNodes = FindUntargetedBranchDisagreementNodes(
            predictor.CandidateState,
            corrected.CandidateState);
        var disagreementElapsedTicks = PerformanceAttributionMeasurement.ReadTimestamp() - disagreementStartedTicks;
        var disagreementAllocatedBytes = Math.Max(0L, PerformanceAttributionMeasurement.ReadAllocatedBytes() - disagreementAllocatedBefore);
        var decisions = shadowThermodynamics.Decisions;
        var observationForAuthority = new FourNodeBranchContinuityActivationObservation(
            "h21-orchestrator-step",
            triggerObserved: true,
            qualificationEvidenceAccepted: true,
            correctorConverged: corrected.Converged,
            lineSearchExhausted: corrected.LineSearchExhausted,
            relativePressureResidual: corrected.MaximumRelativePressureFixedPointResidual,
            absoluteFlowResidualKilogramsPerSecond: corrected.MaximumAbsoluteFlowFixedPointResidualKilogramsPerSecond,
            massClosureKilogramsPerSecond: Math.Abs(corrected.AppliedHydraulicMassRateClosureResidualKilogramsPerSecond),
            energyOwnershipResidualWatts: Math.Abs(corrected.AppliedHydraulicEnergyOwnershipResidualWatts),
            untargetedBranchDisagreementDetected: untargetedNodes.Count != 0);
        var authorityStartedTicks = PerformanceAttributionMeasurement.ReadTimestamp();
        var authorityAllocatedBefore = PerformanceAttributionMeasurement.ReadAllocatedBytes();
        var authority = _activationSupervisor.Evaluate(observationForAuthority, activationOptions);
        var authorityElapsedTicks = PerformanceAttributionMeasurement.ReadTimestamp() - authorityStartedTicks;
        var authorityAllocatedBytes = Math.Max(0L, PerformanceAttributionMeasurement.ReadAllocatedBytes() - authorityAllocatedBefore);
        var sidecarElapsedTicks = PerformanceAttributionMeasurement.ReadTimestamp() - totalStartedTicks;
        var sidecarAllocatedBytes = Math.Max(0L, PerformanceAttributionMeasurement.ReadAllocatedBytes() - totalAllocatedBefore);
        JacobianHydraulicCorrectorPerformanceAttributionRegistry.TryGet(corrected, out var h9Attribution);

        var result = new FourNodeBranchContinuityShadowIntegrationStepResult(
            predictor,
            authority,
            ShadowCorrectionEvaluated: true,
            UntargetedBranchDisagreementDetected: untargetedNodes.Count != 0,
            untargetedNodes,
            BranchOverrideCount: decisions.Count(static decision => decision.SelectionDiffersFromProduction),
            PreviousPhaseHoldCount: decisions.Count(static decision => decision.SelectedPreviousPhase),
            HysteresisReleaseCount: decisions.Count(static decision =>
                string.Equals(decision.DecisionKind, "production-hysteresis-release", StringComparison.Ordinal)),
            ShadowIterationCount: corrected.IterationCount,
            ShadowConverged: corrected.Converged,
            ShadowLineSearchExhausted: corrected.LineSearchExhausted,
            ShadowMaximumRelativePressureResidual: corrected.MaximumRelativePressureFixedPointResidual,
            ShadowMaximumAbsoluteFlowResidualKilogramsPerSecond: corrected.MaximumAbsoluteFlowFixedPointResidualKilogramsPerSecond,
            ShadowMassClosureKilogramsPerSecond: Math.Abs(corrected.AppliedHydraulicMassRateClosureResidualKilogramsPerSecond),
            ShadowEnergyOwnershipResidualWatts: Math.Abs(corrected.AppliedHydraulicEnergyOwnershipResidualWatts))
        {
            CorrectedCandidate = corrected,
        };
        var attribution = new FourNodeBranchContinuityPerformanceAttribution(
            OrchestratorElapsedTicks: 0,
            OrchestratorAllocatedBytes: 0,
            HistoricalExplicitPreparationElapsedTicks: 0,
            HistoricalExplicitPreparationAllocatedBytes: 0,
            SidecarElapsedTicks: sidecarElapsedTicks,
            SidecarAllocatedBytes: sidecarAllocatedBytes,
            PredictorElapsedTicks: predictorElapsedTicks,
            PredictorAllocatedBytes: predictorAllocatedBytes,
            CorrectorElapsedTicks: correctorElapsedTicks,
            CorrectorAllocatedBytes: correctorAllocatedBytes,
            UntargetedDisagreementScanElapsedTicks: disagreementElapsedTicks,
            UntargetedDisagreementScanAllocatedBytes: disagreementAllocatedBytes,
            AuthorityEvaluationElapsedTicks: authorityElapsedTicks,
            AuthorityEvaluationAllocatedBytes: authorityAllocatedBytes,
            CommitAndAccountingElapsedTicks: 0,
            CommitAndAccountingAllocatedBytes: 0,
            HydraulicEvaluationCount: corrected.HydraulicEvaluationCount,
            ProbeEvaluationCount: corrected.ProbeEvaluationCount,
            MaximumJacobianDimension: corrected.MaximumJacobianDimension,
            JacobianBuildAttempts: corrected.JacobianBuildAttempts,
            JacobianDirectionAcceptances: corrected.JacobianDirectionAcceptances,
            JacobianRejectedCount: corrected.JacobianRejectedCount,
            ResidualFallbackAttempts: corrected.ResidualFallbackAttempts,
            ResidualFallbackAcceptances: corrected.ResidualFallbackAcceptances,
            BacktrackingTrialCount: corrected.BacktrackingTrialCount)
        {
            H9 = h9Attribution,
            HistoricalPredictorFluidNodeReuseCount = historicalPredictorFluidNodeReuseCount,
            HistoricalPredictorFluidNodeCount = historicalPredictorFluidNodeCount,
        };
        FourNodeBranchContinuitySidecarPerformanceAttributionRegistry.Set(result, attribution);
        return result;
    }

    private static void ValidateStepArguments(
        PlantState committedState,
        TimeSpan deltaTime,
        IReadOnlyDictionary<string, FluidNodeBalance> frozenNonHydraulicBalances)
    {
        ArgumentNullException.ThrowIfNull(committedState);
        ArgumentNullException.ThrowIfNull(frozenNonHydraulicBalances);
        if (deltaTime <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(deltaTime), deltaTime, "Plant-network step time must be greater than zero.");
        }
    }

    private IReadOnlyList<string> FindUntargetedBranchDisagreementNodes(
        PlantState explicitCandidate,
        PlantState correctedCandidate)
    {
        var explicitNodes = explicitCandidate.FluidNodes.ToDictionary(static node => node.Id, StringComparer.Ordinal);
        var disagreements = new SortedSet<string>(StringComparer.Ordinal);
        var simplifiedDiagnostic = _diagnosticProvider as SimplifiedWaterSteamThermodynamicModel;
        foreach (var correctedNode in correctedCandidate.FluidNodes)
        {
            if (_targetNodeIds.Contains(correctedNode.Id))
            {
                continue;
            }

            var explicitNode = explicitNodes[correctedNode.Id];
            bool phaseMismatch;
            bool candidateOnlyLateShadow;
            if (simplifiedDiagnostic is not null)
            {
                // H.28.1-G: the production water/steam diagnostic scan consumes only the selected phase and
                // the late-boundary-saturated-shadow flag. Use the concrete model's exact short diagnostic
                // that preserves those two outputs while avoiding branch solves and candidate allocations that
                // cannot affect the fail-closed disagreement decision. Non-standard providers keep the full
                // public diagnostic path below.
                var correctedDiagnostic = simplifiedDiagnostic.EvaluateBranchDisagreement(
                    correctedNode.Definition,
                    correctedNode.Inventory);
                var explicitDiagnostic = simplifiedDiagnostic.EvaluateBranchDisagreement(
                    explicitNode.Definition,
                    explicitNode.Inventory);
                phaseMismatch = correctedDiagnostic.ProductionSelectedPhase != explicitDiagnostic.ProductionSelectedPhase;
                candidateOnlyLateShadow = correctedDiagnostic.LateBoundarySaturatedShadowedByEarlierSuperheated
                    && !explicitDiagnostic.LateBoundarySaturatedShadowedByEarlierSuperheated;
            }
            else
            {
                var correctedDiagnostic = _diagnosticProvider.DiagnoseInverseBranchSelection(
                    correctedNode.Definition,
                    correctedNode.Inventory,
                    correctedNode.Thermodynamics);
                var explicitDiagnostic = _diagnosticProvider.DiagnoseInverseBranchSelection(
                    explicitNode.Definition,
                    explicitNode.Inventory,
                    explicitNode.Thermodynamics);
                phaseMismatch = !string.Equals(
                    correctedDiagnostic.ProductionSelectedPhase,
                    explicitDiagnostic.ProductionSelectedPhase,
                    StringComparison.Ordinal);
                candidateOnlyLateShadow = correctedDiagnostic.LateBoundarySaturatedShadowedByEarlierSuperheated
                    && !explicitDiagnostic.LateBoundarySaturatedShadowedByEarlierSuperheated;
            }

            if (phaseMismatch || candidateOnlyLateShadow)
            {
                disagreements.Add(correctedNode.Id);
            }
        }

        return disagreements.ToArray();
    }
}
