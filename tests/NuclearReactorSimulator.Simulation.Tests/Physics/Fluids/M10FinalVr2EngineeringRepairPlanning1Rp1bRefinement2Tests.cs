using System.Diagnostics;
using System.Globalization;
using System.Text;
using NuclearReactorSimulator.Simulation.Tests.Physics.Fluids.Reference;
using Xunit;

namespace NuclearReactorSimulator.Simulation.Tests.Physics.Fluids;

/// <summary>
/// VR2 Engineering Repair Planning 1 / RP1B Refinement 2. Compares versioned C3 and D3 test-only
/// candidates against the immutable RP1A corpus after the returned C2/D2 refinement matrix. Candidate
/// qualification is evidence only; this gate does not select, activate or authorize production thermodynamics.
/// </summary>
public sealed class M10FinalVr2EngineeringRepairPlanning1Rp1bRefinement2Tests
{
    private const string OptInEnvironmentVariable = "NRS_M10_FINAL_VR2_REPAIR_PLANNING1_RP1B_REFINEMENT2";
    private const string ArtifactDirectoryRelative = "artifacts/m10-final-physical-reference-vr2-engineering-repair-planning1-rp1b-refinement2";
    private const string FrozenRp1aRelative = "eng/frozen-evidence/ordinary/M10FinalVR2EngineeringRepairPlanning1_RP1A_Artifacts";
    private const string FrozenRp1bRelative = "eng/frozen-evidence/ordinary/M10FinalVR2EngineeringRepairPlanning1_RP1B_Artifacts";
    private const string FrozenRefinement1Relative = "eng/frozen-evidence/ordinary/M10FinalVR2EngineeringRepairPlanning1_RP1B_Refinement1_Artifacts";
    private const int CandidateWarmupPasses = 2;
    private const int CandidateMeasuredPasses = 8;
    private const double ExistingVr2BlockingCeilingFraction = 0.25d;
    private const double PlanningTargetFraction = 0.10d;
    private static readonly UTF8Encoding Utf8WithoutBom = new(encoderShouldEmitUTF8Identifier: false);

    [Fact(Explicit = true)]
    [Trait("Category", "M10FinalVr2EngineeringRepairPlanning1Rp1bRefinement2")]
    public void Rp1bRefinement2_ProducesC3D3ShadowEvidenceAgainstFrozenRp1aCorpus()
    {
        RequireOptIn();
        var root = FindRepositoryRoot();
        var artifactDirectory = ResetArtifactDirectory(root);
        var frozenDirectory = Path.Combine(root, FrozenRp1aRelative.Replace('/', Path.DirectorySeparatorChar));
        var frozenRp1bDirectory = Path.Combine(root, FrozenRp1bRelative.Replace('/', Path.DirectorySeparatorChar));
        var frozenRp1bSummary = File.ReadAllText(Path.Combine(frozenRp1bDirectory, "09-rp1b-summary.txt"), Encoding.UTF8);
        Assert.True(frozenRp1bSummary.Contains("status=PASS-EVIDENCE-MATRIX-COMPLETE", StringComparison.Ordinal));
        Assert.True(frozenRp1bSummary.Contains("c1-tabulated-surrogate-rp1c-selection-eligible=False", StringComparison.Ordinal));
        Assert.True(frozenRp1bSummary.Contains("d1-bounded-if97-subset-rp1c-selection-eligible=False", StringComparison.Ordinal));

        var frozenRefinement1Directory = Path.Combine(root, FrozenRefinement1Relative.Replace('/', Path.DirectorySeparatorChar));
        var frozenRefinement1Summary = File.ReadAllText(Path.Combine(frozenRefinement1Directory, "09-rp1b-refinement1-summary.txt"), Encoding.UTF8);
        Assert.True(frozenRefinement1Summary.Contains("status=PASS-EVIDENCE-MATRIX-COMPLETE", StringComparison.Ordinal));
        Assert.True(frozenRefinement1Summary.Contains("c2-extended-tabulated-surrogate-rp1c-selection-eligible=False", StringComparison.Ordinal));
        Assert.True(frozenRefinement1Summary.Contains("d2-seam-complete-if97-comparator-rp1c-selection-eligible=False", StringComparison.Ordinal));

        var vr2Rows = LoadVr2Rows(Path.Combine(frozenDirectory, "02-vr2-reference-point-corpus.csv"));
        var nodeRows = LoadNodeRows(Path.Combine(frozenDirectory, "03-exact-v9-node-corpus.csv"));
        var hydraulicRows = LoadHydraulicRows(Path.Combine(frozenDirectory, "04-hydraulic-context.csv"));
        var seamRows = LoadSeamRows(Path.Combine(frozenDirectory, "05-seam-probe-map.csv"));
        var ceilings = LoadPerformanceCeilings(Path.Combine(frozenDirectory, "06-performance-baseline.csv"));
        var rp1aSummary = File.ReadAllText(Path.Combine(frozenDirectory, "07-rp1a-summary.txt"), Encoding.UTF8);

        Assert.Equal(40, vr2Rows.Count);
        Assert.Equal(39, vr2Rows.Count(static row => row.InverseApplicable));
        var boundaryOnlyVr2 = Assert.Single(vr2Rows, static row => !row.InverseApplicable);
        Assert.Equal("VR2-SAT-360C-PONLY", boundaryOnlyVr2.PointId);
        Assert.Equal("SATURATION-PRESSURE-ONLY", boundaryOnlyVr2.SourceFamily);
        Assert.Equal(360, nodeRows.Count);
        Assert.Equal(288, hydraulicRows.Count);
        Assert.Equal(1_280, seamRows.Count);
        ValidateFrozenHydraulicReplayLaw(nodeRows, hydraulicRows);
        Assert.True(rp1aSummary.Contains("status=PASS", StringComparison.Ordinal));
        Assert.True(rp1aSummary.Contains("candidate-timing-inspected=False", StringComparison.Ordinal));

        WriteContractAndProvenance(artifactDirectory, ceilings);

        var factories = new CandidateFactory[]
        {
            new("C3-VAPOR-SEAM-COMPLETE-SURROGATE", static () => new Rp1bVaporSeamCompleteTabulatedSurrogateCandidate()),
            new("D3-VAPOR-SEAM-COMPLETE-IF97-COMPARATOR", static () => new Rp1bVaporSeamCompleteIf97ComparatorCandidate()),
        };

        var candidateRuns = new List<CandidateRun>();
        foreach (var factory in factories)
        {
            var initialization = MeasureInitialization(factory);
            var candidate = initialization.Candidate;
            Assert.Equal(factory.ExpectedCandidateId, candidate.CandidateId);

            var vr2Evaluations = EvaluateVr2(candidate, vr2Rows);
            var nodeEvaluations = EvaluateNodes(candidate, nodeRows);
            var seamEvaluations = EvaluateSeams(candidate, seamRows);
            var hydraulicEvaluations = EvaluateHydraulics(candidate.CandidateId, hydraulicRows, nodeEvaluations);
            var performance = MeasureCandidatePerformance(candidate, nodeRows, seamRows, ceilings, initialization);

            var deterministicRepeat = EvaluateDeterministicRepeat(factory, vr2Rows, nodeRows, seamRows);
            var summary = BuildCandidateSummary(
                candidate,
                vr2Rows,
                nodeRows,
                seamRows,
                vr2Evaluations,
                nodeEvaluations,
                seamEvaluations,
                hydraulicEvaluations,
                performance,
                deterministicRepeat);

            candidateRuns.Add(new CandidateRun(
                candidate.CandidateId,
                candidate.FamilyId,
                vr2Evaluations,
                nodeEvaluations,
                seamEvaluations,
                hydraulicEvaluations,
                performance,
                summary,
                candidate.InitializationReferencePointCount,
                candidate.MaximumIterativeSolveIterations,
                candidate.UsesDirectIf97AtResolveTime));
        }

        WriteVr2ErrorMap(artifactDirectory, candidateRuns);
        WriteNodeMap(artifactDirectory, candidateRuns);
        WriteSeamMap(artifactDirectory, candidateRuns);
        WriteHydraulicReplay(artifactDirectory, candidateRuns);
        WritePerformance(artifactDirectory, candidateRuns);
        WriteComplexity(artifactDirectory, candidateRuns);
        WriteCandidateSummary(artifactDirectory, candidateRuns);

        var evidenceComplete = candidateRuns.Count == 2
            && candidateRuns.Select(static run => run.CandidateId).Distinct(StringComparer.Ordinal).Count() == 2
            && candidateRuns.All(run => run.Vr2Evaluations.Count == vr2Rows.Count)
            && candidateRuns.All(run => run.NodeEvaluations.Count == nodeRows.Count)
            && candidateRuns.All(run => run.SeamEvaluations.Count == seamRows.Count)
            && candidateRuns.All(run => run.HydraulicEvaluations.Count == hydraulicRows.Count)
            && candidateRuns.All(static run => run.Performance.MeasuredResolveCallCount == 360 * CandidateMeasuredPasses)
            && candidateRuns.All(static run => run.Performance.MeasuredSeamCallCount == 1_280)
            && candidateRuns.All(static run => run.Summary.DeterministicRepeat)
            && candidateRuns.All(static run => run.Performance.AllTimingValuesFinite);

        WriteRp1bSummary(artifactDirectory, candidateRuns, evidenceComplete);

        Assert.True(evidenceComplete, "RP1B Refinement 2 C3/D3 evidence generation did not meet the frozen observation-only execution contract.");
    }

    private static InitializationMeasurement MeasureInitialization(CandidateFactory factory)
    {
        var beforeBytes = GC.GetAllocatedBytesForCurrentThread();
        var start = Stopwatch.GetTimestamp();
        var candidate = factory.Create();
        var end = Stopwatch.GetTimestamp();
        var afterBytes = GC.GetAllocatedBytesForCurrentThread();
        return new InitializationMeasurement(
            candidate,
            TicksToMicroseconds(end - start),
            Math.Max(0L, afterBytes - beforeBytes));
    }

    private static IReadOnlyList<Vr2Evaluation> EvaluateVr2(IRp1bShadowThermodynamicCandidate candidate, IReadOnlyList<Vr2Row> rows)
        => rows.Select(row =>
        {
            if (!row.InverseApplicable)
            {
                return new Vr2Evaluation(
                    candidate.CandidateId,
                    row.PointId,
                    row.SourceFamily,
                    row.ReferenceRegion,
                    row.ReferencePhase,
                    row.ReferenceTemperatureCelsius,
                    row.ReferencePressureMegapascals,
                    row.ReferenceQuality,
                    false,
                    false,
                    "NOT-APPLICABLE",
                    "BOUNDARY-ONLY",
                    double.NaN,
                    double.NaN,
                    null,
                    false,
                    double.NaN,
                    double.NaN);
            }

            var resolved = candidate.TryResolve(row.SpecificVolume, row.SpecificEnergy, out var state);
            return new Vr2Evaluation(
                candidate.CandidateId,
                row.PointId,
                row.SourceFamily,
                row.ReferenceRegion,
                row.ReferencePhase,
                row.ReferenceTemperatureCelsius,
                row.ReferencePressureMegapascals,
                row.ReferenceQuality,
                true,
                resolved,
                resolved ? state.Region : "UNRESOLVED",
                resolved ? state.Phase : "Unspecified",
                resolved ? state.TemperatureCelsius : double.NaN,
                resolved ? state.PressureMegapascals : double.NaN,
                resolved ? state.VaporQuality : null,
                resolved && PhaseMatches(row.ReferencePhase, state.Phase),
                resolved ? RelativeError(state.PressureMegapascals, row.ReferencePressureMegapascals) : double.NaN,
                resolved ? RelativeKelvinError(state.TemperatureCelsius, row.ReferenceTemperatureCelsius) : double.NaN);
        }).ToArray();

    private static IReadOnlyList<NodeEvaluation> EvaluateNodes(IRp1bShadowThermodynamicCandidate candidate, IReadOnlyList<NodeRow> rows)
        => rows.Select(row =>
        {
            var resolved = candidate.TryResolve(row.SpecificVolume, row.SpecificEnergy, out var state);
            return new NodeEvaluation(
                candidate.CandidateId,
                row.ProbeId,
                row.LogicalStep,
                row.ElapsedSeconds,
                row.NodeId,
                row.ReferenceRegion,
                row.ReferencePhase,
                row.ReferenceTemperatureCelsius,
                row.ReferencePressureMegapascals,
                row.ReferenceQuality,
                resolved,
                resolved ? state.Region : "UNRESOLVED",
                resolved ? state.Phase : "Unspecified",
                resolved ? state.TemperatureCelsius : double.NaN,
                resolved ? state.PressureMegapascals : double.NaN,
                resolved ? state.VaporQuality : null,
                resolved && PhaseMatches(row.ReferencePhase, state.Phase),
                resolved ? RelativeError(state.PressureMegapascals, row.ReferencePressureMegapascals) : double.NaN,
                resolved ? RelativeKelvinError(state.TemperatureCelsius, row.ReferenceTemperatureCelsius) : double.NaN);
        }).ToArray();

    private static IReadOnlyList<SeamEvaluation> EvaluateSeams(IRp1bShadowThermodynamicCandidate candidate, IReadOnlyList<SeamRow> rows)
        => rows.Select(row =>
        {
            var resolved = candidate.TryResolve(row.SpecificVolume, row.SpecificEnergy, out var state);
            return new SeamEvaluation(
                candidate.CandidateId,
                row.BoundaryIndex,
                row.BoundaryTemperatureCelsius,
                row.ProbeSide,
                row.ReferenceRegion,
                row.ReferencePhase,
                row.ReferenceTemperatureCelsius,
                row.ReferencePressureMegapascals,
                row.ReferenceQuality,
                resolved,
                resolved ? state.Region : "UNRESOLVED",
                resolved ? state.Phase : "Unspecified",
                resolved ? state.TemperatureCelsius : double.NaN,
                resolved ? state.PressureMegapascals : double.NaN,
                resolved ? state.VaporQuality : null,
                resolved && PhaseMatches(row.ReferencePhase, state.Phase),
                resolved ? state.TemperatureCelsius - row.ReferenceTemperatureCelsius : double.NaN,
                resolved ? state.PressureMegapascals - row.ReferencePressureMegapascals : double.NaN);
        }).ToArray();

    private static IReadOnlyList<HydraulicEvaluation> EvaluateHydraulics(
        string candidateId,
        IReadOnlyList<HydraulicRow> rows,
        IReadOnlyList<NodeEvaluation> nodeEvaluations)
    {
        var pressures = nodeEvaluations
            .Where(static row => row.Resolved)
            .ToDictionary(
                static row => (row.ProbeId, row.LogicalStep, row.NodeId),
                static row => row.CandidatePressureMegapascals);

        return rows.Select(row =>
        {
            var fromFound = pressures.TryGetValue((row.ProbeId, row.LogicalStep, row.FromNodeId), out var fromPressure);
            var toFound = pressures.TryGetValue((row.ProbeId, row.LogicalStep, row.ToNodeId), out var toPressure);
            var resolved = fromFound && toFound;
            var candidateDriving = resolved
                ? ((fromPressure - toPressure) * 1_000_000d) + row.ActiveBoostPascals
                : double.NaN;
            var candidateFlow = resolved
                ? SolveQuadraticFlow(candidateDriving, row.Resistance, HasFrozenCheckValve(row.PathId))
                : double.NaN;
            var signChangedFromProduction = resolved
                && Math.Sign(candidateDriving) != Math.Sign(row.ProductionDrivingPascals)
                && Math.Abs(candidateDriving) > 1e-9d
                && Math.Abs(row.ProductionDrivingPascals) > 1e-9d;

            return new HydraulicEvaluation(
                candidateId,
                row.ProbeId,
                row.LogicalStep,
                row.ElapsedSeconds,
                row.PathId,
                row.FromNodeId,
                row.ToNodeId,
                resolved,
                candidateDriving,
                candidateFlow,
                row.CanonicalFlow,
                row.If97CounterfactualFlow,
                resolved ? Math.Abs(candidateFlow - row.CanonicalFlow) : double.NaN,
                resolved ? Math.Abs(candidateFlow - row.If97CounterfactualFlow) : double.NaN,
                signChangedFromProduction);
        }).ToArray();
    }

    private static PerformanceEvaluation MeasureCandidatePerformance(
        IRp1bShadowThermodynamicCandidate candidate,
        IReadOnlyList<NodeRow> nodeRows,
        IReadOnlyList<SeamRow> seamRows,
        PerformanceCeilings ceilings,
        InitializationMeasurement initialization)
    {
        for (var pass = 0; pass < CandidateWarmupPasses; pass++)
        {
            foreach (var row in nodeRows)
            {
                _ = candidate.TryResolve(row.SpecificVolume, row.SpecificEnergy, out _);
            }
        }

        var timings = new List<double>(nodeRows.Count * CandidateMeasuredPasses);
        var allocations = new List<double>(nodeRows.Count * CandidateMeasuredPasses);
        for (var pass = 0; pass < CandidateMeasuredPasses; pass++)
        {
            foreach (var row in nodeRows)
            {
                var beforeBytes = GC.GetAllocatedBytesForCurrentThread();
                var start = Stopwatch.GetTimestamp();
                _ = candidate.TryResolve(row.SpecificVolume, row.SpecificEnergy, out _);
                var end = Stopwatch.GetTimestamp();
                var afterBytes = GC.GetAllocatedBytesForCurrentThread();
                timings.Add(TicksToMicroseconds(end - start));
                allocations.Add(Math.Max(0L, afterBytes - beforeBytes));
            }
        }

        var seamTimings = new List<double>(seamRows.Count);
        foreach (var row in seamRows)
        {
            var start = Stopwatch.GetTimestamp();
            _ = candidate.TryResolve(row.SpecificVolume, row.SpecificEnergy, out _);
            var end = Stopwatch.GetTimestamp();
            seamTimings.Add(TicksToMicroseconds(end - start));
        }

        var median = Median(timings);
        var p95 = Percentile(timings, 0.95d);
        var maximum = timings.Count == 0 ? double.NaN : timings.Max();
        var medianAllocation = Median(allocations);
        var seamMaximum = seamTimings.Count == 0 ? double.NaN : seamTimings.Max();
        var allFinite = IsFiniteNonNegative(initialization.InitializationMicroseconds)
            && initialization.InitializationAllocatedBytes >= 0
            && IsFiniteNonNegative(median)
            && IsFiniteNonNegative(p95)
            && IsFiniteNonNegative(maximum)
            && IsFiniteNonNegative(seamMaximum)
            && IsFiniteNonNegative(medianAllocation);

        return new PerformanceEvaluation(
            candidate.CandidateId,
            initialization.InitializationMicroseconds,
            initialization.InitializationAllocatedBytes,
            timings.Count,
            seamTimings.Count,
            median,
            p95,
            maximum,
            seamMaximum,
            medianAllocation,
            ceilings.ResolveMedianMicroseconds,
            ceilings.ResolveP95Microseconds,
            ceilings.ResolveMaximumMicroseconds,
            ceilings.ResolveMedianAllocatedBytes,
            allFinite,
            allFinite
                && median <= ceilings.ResolveMedianMicroseconds
                && p95 <= ceilings.ResolveP95Microseconds
                && maximum <= ceilings.ResolveMaximumMicroseconds
                && medianAllocation <= ceilings.ResolveMedianAllocatedBytes);
    }

    private static bool EvaluateDeterministicRepeat(
        CandidateFactory factory,
        IReadOnlyList<Vr2Row> vr2Rows,
        IReadOnlyList<NodeRow> nodeRows,
        IReadOnlyList<SeamRow> seamRows)
    {
        var first = factory.Create();
        var second = factory.Create();
        return BuildDeterministicSignature(first, vr2Rows, nodeRows, seamRows)
            .SequenceEqual(BuildDeterministicSignature(second, vr2Rows, nodeRows, seamRows));
    }

    private static IReadOnlyList<DeterministicState> BuildDeterministicSignature(
        IRp1bShadowThermodynamicCandidate candidate,
        IReadOnlyList<Vr2Row> vr2Rows,
        IReadOnlyList<NodeRow> nodeRows,
        IReadOnlyList<SeamRow> seamRows)
    {
        var signature = new List<DeterministicState>(vr2Rows.Count + nodeRows.Count + seamRows.Count);
        foreach (var row in vr2Rows) AddState(signature, candidate, row.SpecificVolume, row.SpecificEnergy);
        foreach (var row in nodeRows) AddState(signature, candidate, row.SpecificVolume, row.SpecificEnergy);
        foreach (var row in seamRows) AddState(signature, candidate, row.SpecificVolume, row.SpecificEnergy);
        return signature;
    }

    private static void AddState(List<DeterministicState> signature, IRp1bShadowThermodynamicCandidate candidate, double volume, double energy)
    {
        var resolved = candidate.TryResolve(volume, energy, out var state);
        signature.Add(new DeterministicState(
            resolved,
            resolved ? state.Region : "UNRESOLVED",
            resolved ? state.Phase : "Unspecified",
            resolved ? BitConverter.DoubleToInt64Bits(state.TemperatureCelsius) : 0L,
            resolved ? BitConverter.DoubleToInt64Bits(state.PressureMegapascals) : 0L,
            resolved && state.VaporQuality.HasValue ? BitConverter.DoubleToInt64Bits(state.VaporQuality.Value) : 0L,
            resolved && state.VaporQuality.HasValue));
    }

    private static CandidateSummary BuildCandidateSummary(
        IRp1bShadowThermodynamicCandidate candidate,
        IReadOnlyList<Vr2Row> vr2Rows,
        IReadOnlyList<NodeRow> nodeRows,
        IReadOnlyList<SeamRow> seamRows,
        IReadOnlyList<Vr2Evaluation> vr2,
        IReadOnlyList<NodeEvaluation> nodes,
        IReadOnlyList<SeamEvaluation> seams,
        IReadOnlyList<HydraulicEvaluation> hydraulics,
        PerformanceEvaluation performance,
        bool deterministicRepeat)
    {
        var vr2InverseApplicable = vr2.Count(static row => row.InverseApplicable);
        var vr2Unresolved = vr2.Count(static row => row.InverseApplicable && !row.Resolved);
        var nodeUnresolved = nodes.Count(static row => !row.Resolved);
        var seamUnresolved = seams.Count(static row => !row.Resolved);
        var vr2PhaseMismatch = vr2.Count(static row => row.InverseApplicable && row.Resolved && !row.PhaseMatches);
        var nodePhaseMismatch = nodes.Count(static row => row.Resolved && !row.PhaseMatches);
        var seamPhaseMismatch = seams.Count(static row => row.Resolved && !row.PhaseMatches);
        var maxVr2PressureError = MaxFinite(vr2.Select(static row => row.PressureRelativeError));
        var maxNodePressureError = MaxFinite(nodes.Select(static row => row.PressureRelativeError));
        var maxCorePressureError = MaxFinite(vr2.Select(static row => row.PressureRelativeError).Concat(nodes.Select(static row => row.PressureRelativeError)));
        var maxCoreTemperatureError = MaxFinite(vr2.Select(static row => row.TemperatureRelativeKelvinError).Concat(nodes.Select(static row => row.TemperatureRelativeKelvinError)));
        var hotCorePressureError = MaxFinite(
            vr2.Where(static row => row.SourceFamily == "COMPRESSED-LIQUID").Select(static row => row.PressureRelativeError)
                .Concat(nodes.Select(static row => row.PressureRelativeError)));
        var exactNodePhaseAgreementPercent = nodes.Count == 0 ? double.NaN : 100d * (nodes.Count - nodePhaseMismatch - nodeUnresolved) / nodes.Count;
        var allFrozenResolved = vr2Unresolved == 0 && nodeUnresolved == 0 && seamUnresolved == 0;
        var noCoreWrongPhase = vr2PhaseMismatch == 0 && nodePhaseMismatch == 0;
        var vr2BlockingCeilingMet = double.IsFinite(maxCorePressureError)
            && double.IsFinite(maxCoreTemperatureError)
            && maxCorePressureError <= ExistingVr2BlockingCeilingFraction
            && maxCoreTemperatureError <= ExistingVr2BlockingCeilingFraction;
        var planningTargetMet = double.IsFinite(hotCorePressureError)
            && hotCorePressureError <= PlanningTargetFraction
            && exactNodePhaseAgreementPercent == 100d;
        var seamContinuity = BuildSeamContinuity(seams, seamRows);
        var meanHydraulicErrorToIf97 = MeanFinite(hydraulics.Select(static row => row.AbsoluteCandidateMinusIf97Flow));
        var meanHydraulicShiftFromCanonical = MeanFinite(hydraulics.Select(static row => row.AbsoluteCandidateMinusCanonicalFlow));
        var hydraulicResolved = hydraulics.Count(static row => row.Resolved);
        var vaporSeamCompletionMet = seamUnresolved == 0 && seamPhaseMismatch == 0;
        var selectionEligible = allFrozenResolved
            && vaporSeamCompletionMet
            && noCoreWrongPhase
            && vr2BlockingCeilingMet
            && planningTargetMet
            && deterministicRepeat
            && performance.WithinFrozenCeilings;

        return new CandidateSummary(
            candidate.CandidateId,
            candidate.FamilyId,
            vr2Rows.Count,
            vr2InverseApplicable,
            vr2Unresolved,
            vr2PhaseMismatch,
            maxVr2PressureError,
            nodeRows.Count,
            nodeUnresolved,
            nodePhaseMismatch,
            maxNodePressureError,
            exactNodePhaseAgreementPercent,
            seamRows.Count,
            seamUnresolved,
            seamPhaseMismatch,
            seamContinuity.MaximumLiquidBoundaryPressureJumpMegapascals,
            seamContinuity.MaximumLiquidBoundaryTemperatureJumpCelsius,
            seamContinuity.MaximumVaporBoundaryPressureJumpMegapascals,
            seamContinuity.MaximumVaporBoundaryTemperatureJumpCelsius,
            hydraulicResolved,
            meanHydraulicErrorToIf97,
            meanHydraulicShiftFromCanonical,
            maxCorePressureError,
            maxCoreTemperatureError,
            hotCorePressureError,
            allFrozenResolved,
            noCoreWrongPhase,
            vr2BlockingCeilingMet,
            planningTargetMet,
            deterministicRepeat,
            performance.WithinFrozenCeilings,
            vaporSeamCompletionMet,
            selectionEligible);
    }

    private static SeamContinuity BuildSeamContinuity(IReadOnlyList<SeamEvaluation> evaluations, IReadOnlyList<SeamRow> frozenRows)
    {
        var byBoundary = evaluations.GroupBy(static row => row.BoundaryIndex).ToDictionary(static group => group.Key);
        var frozenBoundaryCount = frozenRows.Select(static row => row.BoundaryIndex).Distinct().Count();
        Assert.Equal(frozenBoundaryCount, byBoundary.Count);
        var liquidPressureJumps = new List<double>();
        var liquidTemperatureJumps = new List<double>();
        var vaporPressureJumps = new List<double>();
        var vaporTemperatureJumps = new List<double>();

        foreach (var group in byBoundary.Values)
        {
            var r1 = group.Single(static row => row.ProbeSide == "R1-SIDE");
            var r4Liquid = group.Single(static row => row.ProbeSide == "R4-LIQUID-SIDE");
            var r4Vapor = group.Single(static row => row.ProbeSide == "R4-VAPOR-SIDE");
            var r2 = group.Single(static row => row.ProbeSide == "R2-SIDE");
            if (r1.Resolved && r4Liquid.Resolved)
            {
                liquidPressureJumps.Add(Math.Abs(r1.CandidatePressureMegapascals - r4Liquid.CandidatePressureMegapascals));
                liquidTemperatureJumps.Add(Math.Abs(r1.CandidateTemperatureCelsius - r4Liquid.CandidateTemperatureCelsius));
            }
            if (r4Vapor.Resolved && r2.Resolved)
            {
                vaporPressureJumps.Add(Math.Abs(r4Vapor.CandidatePressureMegapascals - r2.CandidatePressureMegapascals));
                vaporTemperatureJumps.Add(Math.Abs(r4Vapor.CandidateTemperatureCelsius - r2.CandidateTemperatureCelsius));
            }
        }

        return new SeamContinuity(
            MaxFinite(liquidPressureJumps),
            MaxFinite(liquidTemperatureJumps),
            MaxFinite(vaporPressureJumps),
            MaxFinite(vaporTemperatureJumps));
    }

    private static void WriteContractAndProvenance(string directory, PerformanceCeilings ceilings)
    {
        File.WriteAllLines(Path.Combine(directory, "01-contract-and-provenance.txt"),
        [
            "gate=VR2-ENGINEERING-REPAIR-PLANNING1-RP1B-REFINEMENT2",
            "scope=TEST-ONLY-C3-D3-VAPOR-SIDE-SEAM-COMPLETION-MATRIX",
            "rp1a-status=VALIDATED",
            "rp1a-corpus=IMMUTABLE",
            "rp1a-vr2-rows=40",
            "rp1a-vr2-inverse-applicable-rows=39",
            "rp1a-vr2-boundary-only-rows=1",
            "rp1a-vr2-boundary-only-point=VR2-SAT-360C-PONLY",
            "first-generation-rp1b=FROZEN-VALIDATED-EVIDENCE",
            "rp1b-refinement1=FROZEN-VALIDATED-EVIDENCE",
            "candidate-c3=C3-VAPOR-SEAM-COMPLETE-SURROGATE",
            "candidate-d3=D3-VAPOR-SEAM-COMPLETE-IF97-COMPARATOR",
            "candidate-prior-generation-retuning-in-place=FORBIDDEN",
            $"candidate-warmup-passes={CandidateWarmupPasses}",
            $"candidate-measured-passes={CandidateMeasuredPasses}",
            FormattableString.Invariant($"vr2-blocking-ceiling-fraction={ExistingVr2BlockingCeilingFraction:R}"),
            FormattableString.Invariant($"planning-target-fraction={PlanningTargetFraction:R}"),
            FormattableString.Invariant($"resolve-median-ceiling-us={ceilings.ResolveMedianMicroseconds:R}"),
            FormattableString.Invariant($"resolve-p95-ceiling-us={ceilings.ResolveP95Microseconds:R}"),
            FormattableString.Invariant($"resolve-max-ceiling-us={ceilings.ResolveMaximumMicroseconds:R}"),
            FormattableString.Invariant($"resolve-median-allocation-ceiling-bytes={ceilings.ResolveMedianAllocatedBytes:R}"),
            "candidate-qualification-is-evidence-not-rp1b-pass-criterion=True",
            "selection-requires-all-1280-seams-resolved=True",
            "selection-requires-zero-seam-phase-mismatch=True",
            "vr2-boundary-only-row-preserved-not-inverse-scored=True",
            "rp1c-selection-authorized=False",
            "production-src-change-authorized=False",
            "thermodynamic-repair-authorized=False",
            "thermodynamic-tolerance-change-authorized=False",
            "exact-v9-change-authorized=False",
            "vr3-authorized=False",
            "p3-r1-authorized=False",
            "second-replacement-long-authorized=False",
        ], Utf8WithoutBom);
    }

    private static void WriteVr2ErrorMap(string directory, IReadOnlyList<CandidateRun> runs)
    {
        var lines = new List<string>
        {
            "candidate_id,point_id,source_family,reference_region,reference_phase,reference_temperature_c,reference_pressure_mpa,reference_quality,inverse_applicable,candidate_resolved,candidate_region,candidate_phase,candidate_temperature_c,candidate_pressure_mpa,candidate_quality,phase_matches,pressure_relative_error,temperature_relative_kelvin_error"
        };
        foreach (var row in runs.SelectMany(static run => run.Vr2Evaluations))
        {
            lines.Add(string.Join(",", new[]
            {
                row.CandidateId, row.PointId, row.SourceFamily, row.ReferenceRegion, row.ReferencePhase,
                F(row.ReferenceTemperatureCelsius), F(row.ReferencePressureMegapascals), F(row.ReferenceQuality),
                B(row.InverseApplicable), B(row.Resolved), row.CandidateRegion, row.CandidatePhase, F(row.CandidateTemperatureCelsius),
                F(row.CandidatePressureMegapascals), F(row.CandidateQuality), B(row.PhaseMatches),
                F(row.PressureRelativeError), F(row.TemperatureRelativeKelvinError),
            }));
        }
        File.WriteAllLines(Path.Combine(directory, "02-candidate-vr2-error-map.csv"), lines, Utf8WithoutBom);
    }

    private static void WriteNodeMap(string directory, IReadOnlyList<CandidateRun> runs)
    {
        var lines = new List<string>
        {
            "candidate_id,probe_id,logical_step,elapsed_s,node_id,reference_region,reference_phase,reference_temperature_c,reference_pressure_mpa,reference_quality,candidate_resolved,candidate_region,candidate_phase,candidate_temperature_c,candidate_pressure_mpa,candidate_quality,phase_matches,pressure_relative_error,temperature_relative_kelvin_error"
        };
        foreach (var row in runs.SelectMany(static run => run.NodeEvaluations))
        {
            lines.Add(string.Join(",", new[]
            {
                row.CandidateId, row.ProbeId, row.LogicalStep.ToString(CultureInfo.InvariantCulture), F(row.ElapsedSeconds), row.NodeId,
                row.ReferenceRegion, row.ReferencePhase, F(row.ReferenceTemperatureCelsius), F(row.ReferencePressureMegapascals), F(row.ReferenceQuality),
                B(row.Resolved), row.CandidateRegion, row.CandidatePhase, F(row.CandidateTemperatureCelsius), F(row.CandidatePressureMegapascals),
                F(row.CandidateQuality), B(row.PhaseMatches), F(row.PressureRelativeError), F(row.TemperatureRelativeKelvinError),
            }));
        }
        File.WriteAllLines(Path.Combine(directory, "03-candidate-exact-v9-node-map.csv"), lines, Utf8WithoutBom);
    }

    private static void WriteSeamMap(string directory, IReadOnlyList<CandidateRun> runs)
    {
        var lines = new List<string>
        {
            "candidate_id,boundary_index,boundary_temperature_c,probe_side,reference_region,reference_phase,reference_temperature_c,reference_pressure_mpa,reference_quality,candidate_resolved,candidate_region,candidate_phase,candidate_temperature_c,candidate_pressure_mpa,candidate_quality,phase_matches,candidate_minus_reference_temperature_c,candidate_minus_reference_pressure_mpa"
        };
        foreach (var row in runs.SelectMany(static run => run.SeamEvaluations))
        {
            lines.Add(string.Join(",", new[]
            {
                row.CandidateId, row.BoundaryIndex.ToString(CultureInfo.InvariantCulture), F(row.BoundaryTemperatureCelsius), row.ProbeSide,
                row.ReferenceRegion, row.ReferencePhase, F(row.ReferenceTemperatureCelsius), F(row.ReferencePressureMegapascals), F(row.ReferenceQuality),
                B(row.Resolved), row.CandidateRegion, row.CandidatePhase, F(row.CandidateTemperatureCelsius), F(row.CandidatePressureMegapascals),
                F(row.CandidateQuality), B(row.PhaseMatches), F(row.CandidateMinusReferenceTemperatureCelsius), F(row.CandidateMinusReferencePressureMegapascals),
            }));
        }
        File.WriteAllLines(Path.Combine(directory, "04-candidate-seam-map.csv"), lines, Utf8WithoutBom);
    }

    private static void WriteHydraulicReplay(string directory, IReadOnlyList<CandidateRun> runs)
    {
        var lines = new List<string>
        {
            "candidate_id,probe_id,logical_step,elapsed_s,path_id,from_node,to_node,candidate_resolved,candidate_driving_pa,candidate_flow_kg_s,canonical_flow_kg_s,if97_counterfactual_flow_kg_s,abs_candidate_minus_canonical_flow_kg_s,abs_candidate_minus_if97_flow_kg_s,driving_sign_changed_from_production"
        };
        foreach (var row in runs.SelectMany(static run => run.HydraulicEvaluations))
        {
            lines.Add(string.Join(",", new[]
            {
                row.CandidateId, row.ProbeId, row.LogicalStep.ToString(CultureInfo.InvariantCulture), F(row.ElapsedSeconds), row.PathId,
                row.FromNodeId, row.ToNodeId, B(row.Resolved), F(row.CandidateDrivingPascals), F(row.CandidateFlowKilogramsPerSecond),
                F(row.CanonicalFlowKilogramsPerSecond), F(row.If97CounterfactualFlowKilogramsPerSecond),
                F(row.AbsoluteCandidateMinusCanonicalFlow), F(row.AbsoluteCandidateMinusIf97Flow), B(row.DrivingSignChangedFromProduction),
            }));
        }
        File.WriteAllLines(Path.Combine(directory, "05-candidate-hydraulic-replay.csv"), lines, Utf8WithoutBom);
    }

    private static void WritePerformance(string directory, IReadOnlyList<CandidateRun> runs)
    {
        var lines = new List<string>
        {
            "candidate_id,initialization_us,initialization_allocated_bytes,measured_resolve_calls,measured_seam_calls,resolve_median_us,resolve_p95_us,resolve_max_us,seam_max_us,resolve_median_allocated_bytes,median_ceiling_us,p95_ceiling_us,max_ceiling_us,median_allocation_ceiling_bytes,all_timing_values_finite,within_frozen_ceilings"
        };
        foreach (var run in runs)
        {
            var row = run.Performance;
            lines.Add(string.Join(",", new[]
            {
                row.CandidateId, F(row.InitializationMicroseconds), row.InitializationAllocatedBytes.ToString(CultureInfo.InvariantCulture),
                row.MeasuredResolveCallCount.ToString(CultureInfo.InvariantCulture), row.MeasuredSeamCallCount.ToString(CultureInfo.InvariantCulture),
                F(row.ResolveMedianMicroseconds), F(row.ResolveP95Microseconds), F(row.ResolveMaximumMicroseconds), F(row.SeamMaximumMicroseconds),
                F(row.ResolveMedianAllocatedBytes), F(row.ResolveMedianCeilingMicroseconds), F(row.ResolveP95CeilingMicroseconds),
                F(row.ResolveMaximumCeilingMicroseconds), F(row.ResolveMedianAllocatedBytesCeiling), B(row.AllTimingValuesFinite), B(row.WithinFrozenCeilings),
            }));
        }
        File.WriteAllLines(Path.Combine(directory, "06-candidate-performance.csv"), lines, Utf8WithoutBom);
    }

    private static void WriteComplexity(string directory, IReadOnlyList<CandidateRun> runs)
    {
        var lines = new List<string>
        {
            "candidate_id,family_id,initialization_reference_point_count,max_iterative_solve_iterations,direct_if97_at_resolve_time,algorithm_shape,production_code_dependency"
        };
        foreach (var run in runs)
        {
            var shape = run.CandidateId switch
            {
                "C3-VAPOR-SEAM-COMPLETE-SURROGATE" => "immutable-c2-core-plus-dense-saturation-vapor-boundary-energy-discriminator-and-near-vapor-mixture-fallback",
                "D3-VAPOR-SEAM-COMPLETE-IF97-COMPARATOR" => "immutable-d2-primary-path-plus-c3-phase-aware-seed-and-bounded-near-vapor-region4-if97-refinement",
                _ => "unknown",
            };
            lines.Add(string.Join(",", new[]
            {
                run.CandidateId, run.FamilyId, run.InitializationReferencePointCount.ToString(CultureInfo.InvariantCulture),
                run.MaximumIterativeSolveIterations.ToString(CultureInfo.InvariantCulture), B(run.UsesDirectIf97AtResolveTime), shape, "NONE-TEST-ONLY",
            }));
        }
        File.WriteAllLines(Path.Combine(directory, "07-candidate-complexity.csv"), lines, Utf8WithoutBom);
    }

    private static void WriteCandidateSummary(string directory, IReadOnlyList<CandidateRun> runs)
    {
        var lines = new List<string>
        {
            "candidate_id,family_id,vr2_rows,vr2_inverse_applicable_rows,vr2_unresolved,vr2_phase_mismatch,max_vr2_pressure_relative_error,exact_v9_rows,exact_v9_unresolved,exact_v9_phase_mismatch,max_exact_v9_pressure_relative_error,exact_v9_phase_agreement_percent,seam_rows,seam_unresolved,seam_phase_mismatch,max_liquid_seam_pressure_jump_mpa,max_liquid_seam_temperature_jump_c,max_vapor_seam_pressure_jump_mpa,max_vapor_seam_temperature_jump_c,hydraulic_resolved_rows,mean_abs_candidate_minus_if97_flow_kg_s,mean_abs_candidate_minus_canonical_flow_kg_s,max_core_pressure_relative_error,max_core_temperature_relative_kelvin_error,max_hot_core_pressure_relative_error,all_frozen_points_resolved,no_core_wrong_phase,vr2_blocking_ceiling_met,planning_target_met,deterministic_repeat,performance_ceiling_met,vapor_seam_completion_met,rp1c_selection_eligible"
        };
        foreach (var run in runs)
        {
            var row = run.Summary;
            lines.Add(string.Join(",", new[]
            {
                row.CandidateId, row.FamilyId, row.Vr2Rows.ToString(CultureInfo.InvariantCulture), row.Vr2InverseApplicableRows.ToString(CultureInfo.InvariantCulture),
                row.Vr2Unresolved.ToString(CultureInfo.InvariantCulture), row.Vr2PhaseMismatch.ToString(CultureInfo.InvariantCulture), F(row.MaximumVr2PressureRelativeError), row.ExactV9Rows.ToString(CultureInfo.InvariantCulture),
                row.ExactV9Unresolved.ToString(CultureInfo.InvariantCulture), row.ExactV9PhaseMismatch.ToString(CultureInfo.InvariantCulture),
                F(row.MaximumExactV9PressureRelativeError), F(row.ExactV9PhaseAgreementPercent), row.SeamRows.ToString(CultureInfo.InvariantCulture),
                row.SeamUnresolved.ToString(CultureInfo.InvariantCulture), row.SeamPhaseMismatch.ToString(CultureInfo.InvariantCulture),
                F(row.MaximumLiquidBoundaryPressureJumpMegapascals), F(row.MaximumLiquidBoundaryTemperatureJumpCelsius),
                F(row.MaximumVaporBoundaryPressureJumpMegapascals), F(row.MaximumVaporBoundaryTemperatureJumpCelsius),
                row.HydraulicResolvedRows.ToString(CultureInfo.InvariantCulture), F(row.MeanAbsoluteCandidateMinusIf97Flow),
                F(row.MeanAbsoluteCandidateMinusCanonicalFlow), F(row.MaximumCorePressureRelativeError), F(row.MaximumCoreTemperatureRelativeKelvinError),
                F(row.MaximumHotCorePressureRelativeError), B(row.AllFrozenPointsResolved), B(row.NoCoreWrongPhase), B(row.Vr2BlockingCeilingMet),
                B(row.PlanningTargetMet), B(row.DeterministicRepeat), B(row.PerformanceCeilingMet), B(row.VaporSeamCompletionMet), B(row.Rp1cSelectionEligible),
            }));
        }
        File.WriteAllLines(Path.Combine(directory, "08-candidate-summary.csv"), lines, Utf8WithoutBom);
    }

    private static void WriteRp1bSummary(string directory, IReadOnlyList<CandidateRun> runs, bool evidenceComplete)
    {
        var lines = new List<string>
        {
            "gate=VR2-ENGINEERING-REPAIR-PLANNING1-RP1B-REFINEMENT2",
            $"status={(evidenceComplete ? "PASS-EVIDENCE-MATRIX-COMPLETE" : "RED-EVIDENCE-MATRIX-INCOMPLETE")}",
            "rp1a-status=VALIDATED",
            "candidate-count=2",
            "candidate-qualification-is-evidence-not-rp1b-pass-criterion=True",
            "vr2-boundary-only-row-preserved-not-inverse-scored=True",
        };
        foreach (var run in runs)
        {
            var prefix = run.CandidateId.ToLowerInvariant();
            lines.Add($"{prefix}-vr2-inverse-applicable-rows={run.Summary.Vr2InverseApplicableRows}");
            lines.Add($"{prefix}-all-frozen-points-resolved={run.Summary.AllFrozenPointsResolved}");
            lines.Add($"{prefix}-exact-v9-phase-agreement-percent={F(run.Summary.ExactV9PhaseAgreementPercent)}");
            lines.Add($"{prefix}-vr2-blocking-ceiling-met={run.Summary.Vr2BlockingCeilingMet}");
            lines.Add($"{prefix}-planning-target-met={run.Summary.PlanningTargetMet}");
            lines.Add($"{prefix}-deterministic-repeat={run.Summary.DeterministicRepeat}");
            lines.Add($"{prefix}-performance-ceiling-met={run.Summary.PerformanceCeilingMet}");
            lines.Add($"{prefix}-seam-unresolved={run.Summary.SeamUnresolved}");
            lines.Add($"{prefix}-seam-phase-mismatch={run.Summary.SeamPhaseMismatch}");
            lines.Add($"{prefix}-vapor-seam-completion-met={run.Summary.VaporSeamCompletionMet}");
            lines.Add($"{prefix}-rp1c-selection-eligible={run.Summary.Rp1cSelectionEligible}");
        }
        lines.Add("rp1c-selection-performed=False");
        lines.Add("production-src-change-authorized=False");
        lines.Add("thermodynamic-repair-authorized=False");
        lines.Add("thermodynamic-tolerance-change-authorized=False");
        lines.Add("exact-v9-change-authorized=False");
        lines.Add("vr3-authorized=False");
        lines.Add("p3-r1-authorized=False");
        lines.Add("second-replacement-long-authorized=False");
        lines.Add("next-action=Return complete RP1B Refinement 2 artifact folder for engineering review before any RP1C selection gate implementation.");
        File.WriteAllLines(Path.Combine(directory, "09-rp1b-refinement2-summary.txt"), lines, Utf8WithoutBom);
    }

    private static IReadOnlyList<Vr2Row> LoadVr2Rows(string path)
    {
        const string header = "point_id,source_family,reference_region,reference_phase,reference_temperature_c,reference_pressure_mpa,specific_volume_m3_kg,specific_u_j_kg,reference_quality,production_resolved,production_phase,production_temperature_c,production_pressure_mpa";
        var lines = ReadCsvLines(path, header);
        return lines.Select(static line =>
        {
            var parts = line.Split(',');
            Assert.Equal(13, parts.Length);
            var specificVolume = DN(parts[6]);
            var specificEnergy = DN(parts[7]);
            var inverseApplicable = double.IsFinite(specificVolume) && specificVolume > 0d && double.IsFinite(specificEnergy);
            return new Vr2Row(parts[0], parts[1], parts[2], parts[3], D(parts[4]), D(parts[5]), specificVolume, specificEnergy, DN(parts[8]), inverseApplicable);
        }).ToArray();
    }

    private static IReadOnlyList<NodeRow> LoadNodeRows(string path)
    {
        const string header = "probe_id,logical_step,elapsed_s,node_id,production_phase,production_quality,production_density_kg_m3,production_u_j_kg,production_temperature_c,production_pressure_mpa,reference_resolved,reference_region,reference_phase,reference_temperature_c,reference_pressure_mpa,reference_quality,reference_minus_production_pressure_mpa";
        var lines = ReadCsvLines(path, header);
        return lines.Select(static line =>
        {
            var parts = line.Split(',');
            Assert.Equal(17, parts.Length);
            var density = D(parts[6]);
            Assert.True(density > 0d);
            Assert.True(bool.Parse(parts[10]));
            return new NodeRow(
                parts[0], long.Parse(parts[1], CultureInfo.InvariantCulture), D(parts[2]), parts[3], 1d / density, D(parts[7]),
                parts[11], parts[12], D(parts[13]), D(parts[14]), DN(parts[15]));
        }).ToArray();
    }

    private static IReadOnlyList<HydraulicRow> LoadHydraulicRows(string path)
    {
        const string header = "probe_id,logical_step,elapsed_s,path_id,from_node,to_node,resistance_pa_s2_kg2,active_boost_pa,production_driving_pa,if97_driving_pa,canonical_flow_kg_s,production_formula_flow_kg_s,if97_pressure_only_counterfactual_flow_kg_s,reference_resolved,driving_pressure_sign_changed,abs_counterfactual_flow_shift_kg_s";
        var lines = ReadCsvLines(path, header);
        return lines.Select(static line =>
        {
            var parts = line.Split(',');
            Assert.Equal(16, parts.Length);
            Assert.True(bool.Parse(parts[13]));
            return new HydraulicRow(
                parts[0], long.Parse(parts[1], CultureInfo.InvariantCulture), D(parts[2]), parts[3], parts[4], parts[5], D(parts[6]), D(parts[7]),
                D(parts[8]), D(parts[10]), D(parts[12]));
        }).ToArray();
    }

    private static IReadOnlyList<SeamRow> LoadSeamRows(string path)
    {
        const string header = "boundary_index,boundary_temperature_c,probe_side,reference_region,reference_phase,reference_pressure_mpa,specific_volume_m3_kg,specific_u_j_kg,reference_quality,production_resolved,production_phase,production_temperature_c,production_pressure_mpa,phase_matches,production_minus_reference_temperature_c,production_minus_reference_pressure_mpa";
        var lines = ReadCsvLines(path, header);
        return lines.Select(static line =>
        {
            var parts = line.Split(',');
            Assert.Equal(16, parts.Length);
            return new SeamRow(
                int.Parse(parts[0], CultureInfo.InvariantCulture), D(parts[1]), parts[2], parts[3], parts[4], D(parts[1]), D(parts[5]),
                D(parts[6]), D(parts[7]), DN(parts[8]));
        }).ToArray();
    }

    private static PerformanceCeilings LoadPerformanceCeilings(string path)
    {
        var values = File.ReadAllLines(path, Encoding.UTF8)
            .Skip(1)
            .Where(static line => line.Contains(',', StringComparison.Ordinal))
            .Select(static line => line.Split(',', 2))
            .Where(static parts => parts.Length == 2)
            .ToDictionary(static parts => parts[0], static parts => parts[1], StringComparer.Ordinal);
        return new PerformanceCeilings(
            D(values["rp1b_candidate_resolve_median_ceiling_us"]),
            D(values["rp1b_candidate_resolve_p95_ceiling_us"]),
            D(values["rp1b_candidate_resolve_max_ceiling_us"]),
            D(values["rp1b_candidate_resolve_median_allocated_bytes_ceiling"]));
    }

    private static IReadOnlyList<string> ReadCsvLines(string path, string expectedHeader)
    {
        Assert.True(File.Exists(path), $"Required frozen RP1A artifact missing: {path}");
        var lines = File.ReadAllLines(path, Encoding.UTF8);
        Assert.NotEmpty(lines);
        Assert.Equal(expectedHeader, lines[0]);
        return lines.Skip(1).Where(static line => !string.IsNullOrWhiteSpace(line)).ToArray();
    }

    private static void ValidateFrozenHydraulicReplayLaw(IReadOnlyList<NodeRow> nodeRows, IReadOnlyList<HydraulicRow> hydraulicRows)
    {
        var referencePressures = nodeRows.ToDictionary(
            static row => (row.ProbeId, row.LogicalStep, row.NodeId),
            static row => row.ReferencePressureMegapascals);
        var maximumFlowError = 0d;

        foreach (var row in hydraulicRows)
        {
            Assert.True(referencePressures.TryGetValue((row.ProbeId, row.LogicalStep, row.FromNodeId), out var fromPressure));
            Assert.True(referencePressures.TryGetValue((row.ProbeId, row.LogicalStep, row.ToNodeId), out var toPressure));
            var driving = ((fromPressure - toPressure) * 1_000_000d) + row.ActiveBoostPascals;
            var replayed = SolveQuadraticFlow(driving, row.Resistance, HasFrozenCheckValve(row.PathId));
            Assert.True(double.IsFinite(replayed));
            maximumFlowError = Math.Max(maximumFlowError, Math.Abs(replayed - row.If97CounterfactualFlow));
        }

        Assert.Equal(72, hydraulicRows.Count(static row => row.PathId == "MCP"));
        Assert.Equal(72, hydraulicRows.Count(static row => row.PathId == "CHANNEL"));
        Assert.Equal(72, hydraulicRows.Count(static row => row.PathId == "RETURN"));
        Assert.Equal(72, hydraulicRows.Count(static row => row.PathId == "FEEDWATER-PUMP"));
        Assert.True(
            maximumFlowError <= 1e-9d,
            $"Frozen RP1A hydraulic replay law drifted: maximum IF97 counterfactual flow reproduction error={maximumFlowError:R} kg/s.");
    }

    private static bool HasFrozenCheckValve(string pathId)
        => pathId switch
        {
            "MCP" => false,
            "CHANNEL" => false,
            "RETURN" => false,
            "FEEDWATER-PUMP" => true,
            _ => throw new InvalidOperationException($"Unexpected frozen RP1A hydraulic path: {pathId}"),
        };

    private static double SolveQuadraticFlow(double drivingPressurePascals, double resistance, bool hasCheckValve)
    {
        if (!double.IsFinite(drivingPressurePascals) || !double.IsFinite(resistance) || resistance <= 0d) return double.NaN;
        if (Math.Abs(drivingPressurePascals) <= 1e-15d) return 0d;
        var magnitude = Math.Sqrt(Math.Abs(drivingPressurePascals) / resistance);
        var signed = drivingPressurePascals > 0d ? magnitude : -magnitude;
        return hasCheckValve && signed < 0d ? 0d : signed;
    }

    private static bool PhaseMatches(string reference, string candidate) => string.Equals(reference, candidate, StringComparison.Ordinal);
    private static double RelativeError(double actual, double expected) => Math.Abs(actual - expected) / Math.Max(Math.Abs(expected), 1e-12d);
    private static double RelativeKelvinError(double actualCelsius, double expectedCelsius)
        => Math.Abs(actualCelsius - expectedCelsius) / Math.Max(Math.Abs(expectedCelsius + 273.15d), 1e-12d);
    private static double TicksToMicroseconds(long ticks) => ticks * 1_000_000d / Stopwatch.Frequency;
    private static bool IsFiniteNonNegative(double value) => double.IsFinite(value) && value >= 0d;

    private static double Median(IEnumerable<double> source)
    {
        var sorted = source.OrderBy(static value => value).ToArray();
        if (sorted.Length == 0) return double.NaN;
        var middle = sorted.Length / 2;
        return sorted.Length % 2 == 0 ? 0.5d * (sorted[middle - 1] + sorted[middle]) : sorted[middle];
    }

    private static double Percentile(IEnumerable<double> source, double percentile)
    {
        var sorted = source.OrderBy(static value => value).ToArray();
        if (sorted.Length == 0) return double.NaN;
        var index = (int)Math.Ceiling(percentile * sorted.Length) - 1;
        return sorted[Math.Clamp(index, 0, sorted.Length - 1)];
    }

    private static double MaxFinite(IEnumerable<double> source)
    {
        var values = source.Where(double.IsFinite).ToArray();
        return values.Length == 0 ? double.NaN : values.Max();
    }

    private static double MeanFinite(IEnumerable<double> source)
    {
        var values = source.Where(double.IsFinite).ToArray();
        return values.Length == 0 ? double.NaN : values.Average();
    }

    private static double D(string value) => double.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture);
    private static double DN(string value) => string.IsNullOrWhiteSpace(value) ? double.NaN : D(value);
    private static string F(double value) => double.IsFinite(value) ? value.ToString("R", CultureInfo.InvariantCulture) : string.Empty;
    private static string F(double? value) => value.HasValue && double.IsFinite(value.Value) ? value.Value.ToString("R", CultureInfo.InvariantCulture) : string.Empty;
    private static string B(bool value) => value ? "true" : "false";

    private static string ResetArtifactDirectory(string root)
    {
        var path = Path.Combine(root, ArtifactDirectoryRelative.Replace('/', Path.DirectorySeparatorChar));
        if (Directory.Exists(path)) Directory.Delete(path, recursive: true);
        Directory.CreateDirectory(path);
        return path;
    }

    private static void RequireOptIn() => Assert.Equal("1", Environment.GetEnvironmentVariable(OptInEnvironmentVariable));

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "NuclearReactorSimulator.sln"))) return current.FullName;
            current = current.Parent;
        }
        throw new InvalidOperationException("Could not locate NuclearReactorSimulator.sln from test base directory.");
    }

    private sealed record CandidateFactory(string ExpectedCandidateId, Func<IRp1bShadowThermodynamicCandidate> Create);
    private sealed record InitializationMeasurement(IRp1bShadowThermodynamicCandidate Candidate, double InitializationMicroseconds, long InitializationAllocatedBytes);
    private sealed record PerformanceCeilings(double ResolveMedianMicroseconds, double ResolveP95Microseconds, double ResolveMaximumMicroseconds, double ResolveMedianAllocatedBytes);

    private sealed record Vr2Row(string PointId, string SourceFamily, string ReferenceRegion, string ReferencePhase, double ReferenceTemperatureCelsius, double ReferencePressureMegapascals, double SpecificVolume, double SpecificEnergy, double ReferenceQuality, bool InverseApplicable);
    private sealed record NodeRow(string ProbeId, long LogicalStep, double ElapsedSeconds, string NodeId, double SpecificVolume, double SpecificEnergy, string ReferenceRegion, string ReferencePhase, double ReferenceTemperatureCelsius, double ReferencePressureMegapascals, double ReferenceQuality);
    private sealed record HydraulicRow(string ProbeId, long LogicalStep, double ElapsedSeconds, string PathId, string FromNodeId, string ToNodeId, double Resistance, double ActiveBoostPascals, double ProductionDrivingPascals, double CanonicalFlow, double If97CounterfactualFlow);
    private sealed record SeamRow(int BoundaryIndex, double BoundaryTemperatureCelsius, string ProbeSide, string ReferenceRegion, string ReferencePhase, double ReferenceTemperatureCelsius, double ReferencePressureMegapascals, double SpecificVolume, double SpecificEnergy, double ReferenceQuality);

    private sealed record Vr2Evaluation(string CandidateId, string PointId, string SourceFamily, string ReferenceRegion, string ReferencePhase, double ReferenceTemperatureCelsius, double ReferencePressureMegapascals, double ReferenceQuality, bool InverseApplicable, bool Resolved, string CandidateRegion, string CandidatePhase, double CandidateTemperatureCelsius, double CandidatePressureMegapascals, double? CandidateQuality, bool PhaseMatches, double PressureRelativeError, double TemperatureRelativeKelvinError);
    private sealed record NodeEvaluation(string CandidateId, string ProbeId, long LogicalStep, double ElapsedSeconds, string NodeId, string ReferenceRegion, string ReferencePhase, double ReferenceTemperatureCelsius, double ReferencePressureMegapascals, double ReferenceQuality, bool Resolved, string CandidateRegion, string CandidatePhase, double CandidateTemperatureCelsius, double CandidatePressureMegapascals, double? CandidateQuality, bool PhaseMatches, double PressureRelativeError, double TemperatureRelativeKelvinError);
    private sealed record SeamEvaluation(string CandidateId, int BoundaryIndex, double BoundaryTemperatureCelsius, string ProbeSide, string ReferenceRegion, string ReferencePhase, double ReferenceTemperatureCelsius, double ReferencePressureMegapascals, double ReferenceQuality, bool Resolved, string CandidateRegion, string CandidatePhase, double CandidateTemperatureCelsius, double CandidatePressureMegapascals, double? CandidateQuality, bool PhaseMatches, double CandidateMinusReferenceTemperatureCelsius, double CandidateMinusReferencePressureMegapascals);
    private sealed record HydraulicEvaluation(string CandidateId, string ProbeId, long LogicalStep, double ElapsedSeconds, string PathId, string FromNodeId, string ToNodeId, bool Resolved, double CandidateDrivingPascals, double CandidateFlowKilogramsPerSecond, double CanonicalFlowKilogramsPerSecond, double If97CounterfactualFlowKilogramsPerSecond, double AbsoluteCandidateMinusCanonicalFlow, double AbsoluteCandidateMinusIf97Flow, bool DrivingSignChangedFromProduction);
    private sealed record DeterministicState(bool Resolved, string Region, string Phase, long TemperatureBits, long PressureBits, long QualityBits, bool HasQuality);
    private sealed record SeamContinuity(double MaximumLiquidBoundaryPressureJumpMegapascals, double MaximumLiquidBoundaryTemperatureJumpCelsius, double MaximumVaporBoundaryPressureJumpMegapascals, double MaximumVaporBoundaryTemperatureJumpCelsius);

    private sealed record PerformanceEvaluation(
        string CandidateId,
        double InitializationMicroseconds,
        long InitializationAllocatedBytes,
        int MeasuredResolveCallCount,
        int MeasuredSeamCallCount,
        double ResolveMedianMicroseconds,
        double ResolveP95Microseconds,
        double ResolveMaximumMicroseconds,
        double SeamMaximumMicroseconds,
        double ResolveMedianAllocatedBytes,
        double ResolveMedianCeilingMicroseconds,
        double ResolveP95CeilingMicroseconds,
        double ResolveMaximumCeilingMicroseconds,
        double ResolveMedianAllocatedBytesCeiling,
        bool AllTimingValuesFinite,
        bool WithinFrozenCeilings);

    private sealed record CandidateSummary(
        string CandidateId,
        string FamilyId,
        int Vr2Rows,
        int Vr2InverseApplicableRows,
        int Vr2Unresolved,
        int Vr2PhaseMismatch,
        double MaximumVr2PressureRelativeError,
        int ExactV9Rows,
        int ExactV9Unresolved,
        int ExactV9PhaseMismatch,
        double MaximumExactV9PressureRelativeError,
        double ExactV9PhaseAgreementPercent,
        int SeamRows,
        int SeamUnresolved,
        int SeamPhaseMismatch,
        double MaximumLiquidBoundaryPressureJumpMegapascals,
        double MaximumLiquidBoundaryTemperatureJumpCelsius,
        double MaximumVaporBoundaryPressureJumpMegapascals,
        double MaximumVaporBoundaryTemperatureJumpCelsius,
        int HydraulicResolvedRows,
        double MeanAbsoluteCandidateMinusIf97Flow,
        double MeanAbsoluteCandidateMinusCanonicalFlow,
        double MaximumCorePressureRelativeError,
        double MaximumCoreTemperatureRelativeKelvinError,
        double MaximumHotCorePressureRelativeError,
        bool AllFrozenPointsResolved,
        bool NoCoreWrongPhase,
        bool Vr2BlockingCeilingMet,
        bool PlanningTargetMet,
        bool DeterministicRepeat,
        bool PerformanceCeilingMet,
        bool VaporSeamCompletionMet,
        bool Rp1cSelectionEligible);

    private sealed record CandidateRun(
        string CandidateId,
        string FamilyId,
        IReadOnlyList<Vr2Evaluation> Vr2Evaluations,
        IReadOnlyList<NodeEvaluation> NodeEvaluations,
        IReadOnlyList<SeamEvaluation> SeamEvaluations,
        IReadOnlyList<HydraulicEvaluation> HydraulicEvaluations,
        PerformanceEvaluation Performance,
        CandidateSummary Summary,
        int InitializationReferencePointCount,
        int MaximumIterativeSolveIterations,
        bool UsesDirectIf97AtResolveTime);
}
