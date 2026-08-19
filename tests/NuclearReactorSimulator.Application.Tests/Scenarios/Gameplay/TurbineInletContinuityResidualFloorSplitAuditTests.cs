using System.Globalization;
using System.Text;
using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios.Faults.SecondaryTransients;
using NuclearReactorSimulator.Application.Scenarios.Training;
using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Plant;
using NuclearReactorSimulator.Simulation.Physics.Fluids;
using NuclearReactorSimulator.Simulation.Plant;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay;

/// <summary>
/// M10.9.4.1-H.18 split diagnosis over the validated H.17 Hotfix 6 representative evidence.
/// The experiment adds turbine-inlet only to the shadow bounded-hysteresis target set and then
/// diagnoses every H.17 failure that remains. Production stays explicit and unchanged.
/// </summary>
public sealed class TurbineInletContinuityResidualFloorSplitAuditTests
{
    private static readonly TimeSpan Step = TimeSpan.FromMilliseconds(10d);
    private static readonly Encoding Utf8WithoutBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
    private static readonly string[] H18TargetNodeIds = { "steam", "stop-out", "header", "turbine-inlet" };
    private const int SuccessControlsPerProfile = 4;
    private const int DeterminismSentinelsPerClass = 2;
    private const int ObservationStride = 10;

    private static readonly ProfileDefinition[] Profiles =
    {
        new("steady-long", 12_000, ProfileKind.Steady),
        new("load-pulse", 6_000, ProfileKind.LoadPulse),
        new("cooling-pulse", 6_000, ProfileKind.CoolingPulse),
        new("combined-load-cooling", 6_000, ProfileKind.CombinedLoadCooling),
    };

    [Fact]
    public void FrozenH17Evidence_RetainsValidatedSplitContract()
    {
        var evidence = LoadFrozenH17Evidence();

        Assert.Equal(473, evidence.Count);
        Assert.Equal(245, evidence.Count(static item => !item.H17Converged));
        Assert.Equal(228, evidence.Count(static item => item.H17Converged));
        Assert.Equal(120, evidence.Count(static item => !item.H17Converged && item.TurbineInletPhaseMismatch));
        Assert.Equal(125, evidence.Count(static item => !item.H17Converged && !item.TurbineInletPhaseMismatch));
        Assert.Equal(1, evidence.Count(static item => item.H17Converged && item.TurbineInletPhaseMismatch));
        Assert.Equal(1, evidence.Count(static item => !item.H17Converged && item.TurbineInletCandidateOnlyLateShadow));
    }

    [Fact(Explicit = true)]
    [Trait("Category", "TurbineInletContinuityResidualFloorSplitAudit")]
    public void FrozenH17Failures_CompareFourNodeContinuityAndDiagnoseResidualFloorSplit()
    {
        var productionThermodynamics = new SimplifiedWaterSteamThermodynamicModel();
        var prototype = new SemiImplicitHydraulicPrototypeSolver(productionThermodynamics);
        var evidence = LoadFrozenH17Evidence();
        var h17Failures = evidence.Where(static item => !item.H17Converged).ToArray();
        var successControls = SelectSuccessControls(evidence);
        var selectedEvidence = h17Failures
            .Concat(successControls)
            .OrderBy(static item => item.ProfileId, StringComparer.Ordinal)
            .ThenBy(static item => item.IntervalIndex)
            .ToArray();

        Assert.Equal(245, h17Failures.Length);
        Assert.Equal(Profiles.Length * SuccessControlsPerProfile, successControls.Count);
        Assert.Equal(261, selectedEvidence.Length);

        ResetProgress();
        WriteProgress($"frozen-evidence-loaded representatives={evidence.Count} failures={h17Failures.Length} success-controls={successControls.Count}");

        var selectedKeys = selectedEvidence
            .Select(static item => (item.ProfileId, item.IntervalIndex))
            .ToHashSet();
        var referenceIntervals = new Dictionary<(string ProfileId, int IntervalIndex), ReferenceInterval>();
        var committedRows = new List<CommittedTurbineObservationRow>();
        foreach (var profile in Profiles)
        {
            WriteProgress($"reference-start profile={profile.Id} intervals={profile.IntervalCount}");
            BuildReferenceSubset(
                profile,
                selectedKeys,
                prototype,
                productionThermodynamics,
                referenceIntervals,
                committedRows);
            WriteProgress($"reference-complete profile={profile.Id} selected={referenceIntervals.Keys.Count(key => string.Equals(key.ProfileId, profile.Id, StringComparison.Ordinal))}");
        }

        Assert.Equal(selectedEvidence.Length, referenceIntervals.Count);
        var committedTransparent = committedRows.All(static item => !item.SelectionDiffersFromProduction);
        var committedTransitions = committedRows.Count(static item => item.CommittedPhaseTransition);

        WriteProgress($"four-node-policy-start samples={selectedEvidence.Length}");
        var results = RunFourNodePolicy(selectedEvidence, referenceIntervals, productionThermodynamics);
        WriteProgress($"four-node-policy-complete samples={results.Count} converged={results.Count(static item => item.Result.Converged)}");

        var mismatchFailures = results.Where(static item => !item.Evidence.H17Converged && item.Evidence.TurbineInletPhaseMismatch).ToArray();
        var noMismatchFailures = results.Where(static item => !item.Evidence.H17Converged && !item.Evidence.TurbineInletPhaseMismatch).ToArray();
        var controlResults = results.Where(static item => item.Evidence.H17Converged).ToArray();
        Assert.Equal(120, mismatchFailures.Length);
        Assert.Equal(125, noMismatchFailures.Length);
        Assert.Equal(successControls.Count, controlResults.Length);

        var recoveredMismatchFailures = mismatchFailures.Count(static item => item.Result.Converged);
        var recoveredNoMismatchFailures = noMismatchFailures.Count(static item => item.Result.Converged);
        var preservedSuccessControls = controlResults.Count(static item => item.Result.Converged);
        var turbineInletOverrides = results.Sum(static item => item.Decisions.Count(static decision =>
            string.Equals(decision.NodeId, "turbine-inlet", StringComparison.Ordinal)
            && decision.SelectionDiffersFromProduction));
        var remainingFailures = results.Where(static item => !item.Result.Converged).ToArray();

        WriteProgress($"remaining-failure-diagnosis-start failures={remainingFailures.Length}");
        var residualRows = BuildResidualRows(remainingFailures);
        var inverseRows = BuildRemainingFailureInverseRows(remainingFailures, referenceIntervals, productionThermodynamics);
        WriteProgress($"remaining-failure-diagnosis-complete residual-rows={residualRows.Count} inverse-rows={inverseRows.Count}");

        var deterministicSentinels = SelectDeterminismSentinels(results);
        var repeat = RunFourNodePolicy(
            deterministicSentinels.Select(static item => item.Evidence).ToArray(),
            referenceIntervals,
            productionThermodynamics);
        var deterministicRepeat = string.Equals(
            PolicyFingerprint(deterministicSentinels),
            PolicyFingerprint(repeat),
            StringComparison.Ordinal);
        Assert.True(deterministicRepeat, "H.18 four-node sentinel policy was not exactly deterministic.");

        var newUntargetedLateShadowNodes = inverseRows
            .Where(static item => item.CandidateOnlyLateShadow && !item.Targeted)
            .Select(static item => item.NodeId)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();
        var newUntargetedPhaseMismatchNodes = inverseRows
            .Where(static item => item.CandidatePhaseMismatch && !item.Targeted)
            .Select(static item => item.NodeId)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();

        var fourNodeExtensionQualifies = recoveredMismatchFailures == mismatchFailures.Length
            && preservedSuccessControls == controlResults.Length
            && committedTransparent;
        var residualSplitDiagnosticPasses = deterministicRepeat
            && h17Failures.Length == 245
            && mismatchFailures.Length == 120
            && noMismatchFailures.Length == 125
            && residualRows.Count == remainingFailures.Length * referenceIntervals.First().Value.Start.FluidNodes.Count;

        WriteAuditReports(
            evidence,
            selectedEvidence,
            results,
            committedRows,
            residualRows,
            inverseRows,
            deterministicSentinels.Count,
            deterministicRepeat,
            recoveredMismatchFailures,
            recoveredNoMismatchFailures,
            preservedSuccessControls,
            turbineInletOverrides,
            committedTransparent,
            committedTransitions,
            newUntargetedLateShadowNodes,
            newUntargetedPhaseMismatchNodes,
            fourNodeExtensionQualifies,
            residualSplitDiagnosticPasses);

        Assert.True(residualSplitDiagnosticPasses, "H.18 split diagnosis did not complete deterministically over the frozen H.17 evidence contract.");
    }

    private static IReadOnlyList<FrozenH17Evidence> LoadFrozenH17Evidence()
    {
        var path = Path.Combine(
            FindRepositoryRoot(),
            "eng",
            "frozen-evidence",
            "ordinary",
            "H17_Hotfix6_FrozenQualifiedRepresentativeEvidence.csv");
        var lines = File.ReadAllLines(path);
        Assert.True(lines.Length > 1, "Frozen H.17 evidence CSV is empty.");
        Assert.Equal(
            "profile,interval,selection_reasons,h17_converged,h17_line_search_exhausted,h17_pressure_residual,h17_flow_residual_kg_s,h17_normalized_merit,turbine_inlet_candidate_branch,turbine_inlet_candidate_phase,turbine_inlet_explicit_branch,turbine_inlet_explicit_phase,turbine_inlet_phase_mismatch,turbine_inlet_candidate_only_late_shadow",
            lines[0]);

        return lines
            .Skip(1)
            .Where(static line => !string.IsNullOrWhiteSpace(line))
            .Select(static line =>
            {
                var fields = line.Split(',');
                if (fields.Length != 14)
                {
                    throw new InvalidDataException($"Unexpected H.17 evidence column count {fields.Length}: {line}");
                }

                return new FrozenH17Evidence(
                    fields[0],
                    int.Parse(fields[1], CultureInfo.InvariantCulture),
                    fields[2],
                    bool.Parse(fields[3]),
                    bool.Parse(fields[4]),
                    double.Parse(fields[5], CultureInfo.InvariantCulture),
                    double.Parse(fields[6], CultureInfo.InvariantCulture),
                    double.Parse(fields[7], CultureInfo.InvariantCulture),
                    fields[8],
                    fields[9],
                    fields[10],
                    fields[11],
                    bool.Parse(fields[12]),
                    bool.Parse(fields[13]));
            })
            .ToArray();
    }

    private static IReadOnlyList<FrozenH17Evidence> SelectSuccessControls(IReadOnlyList<FrozenH17Evidence> evidence)
    {
        var selected = new List<FrozenH17Evidence>(Profiles.Length * SuccessControlsPerProfile);
        foreach (var profile in Profiles)
        {
            var successes = evidence
                .Where(item => item.H17Converged && string.Equals(item.ProfileId, profile.Id, StringComparison.Ordinal))
                .OrderBy(static item => item.IntervalIndex)
                .ToArray();
            Assert.True(successes.Length >= SuccessControlsPerProfile);
            for (var index = 0; index < SuccessControlsPerProfile; index++)
            {
                var position = SuccessControlsPerProfile == 1
                    ? 0
                    : (int)Math.Round(
                        index * (successes.Length - 1d) / (SuccessControlsPerProfile - 1d),
                        MidpointRounding.AwayFromZero);
                selected.Add(successes[position]);
            }
        }

        return selected
            .DistinctBy(static item => (item.ProfileId, item.IntervalIndex))
            .OrderBy(static item => item.ProfileId, StringComparer.Ordinal)
            .ThenBy(static item => item.IntervalIndex)
            .ToArray();
    }

    private static void BuildReferenceSubset(
        ProfileDefinition profile,
        IReadOnlySet<(string ProfileId, int IntervalIndex)> selectedKeys,
        SemiImplicitHydraulicPrototypeSolver solver,
        SimplifiedWaterSteamThermodynamicModel productionThermodynamics,
        IDictionary<(string ProfileId, int IntervalIndex), ReferenceInterval> references,
        ICollection<CommittedTurbineObservationRow> committedRows)
    {
        var engine = Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(
            DesktopSustainedGenerationInitialConditionFactory.CreateNumericalStiffnessEvidenceRuntimeEngine(Step));
        var initialPresentation = engine.CreatePresentationSnapshot(ControlRoomRunState.Running);
        var generatorId = Assert.Single(initialPresentation.Electrical.Generators).GeneratorId;
        var coolingTarget = (ISecondaryTransientFaultTarget)engine;
        FluidPhase? previousTurbineInletPhase = null;

        for (var index = 1; index <= profile.IntervalCount; index++)
        {
            if (ApplyProfileAction(profile.Kind, index, engine, generatorId, coolingTarget))
            {
                var transition = engine.Step(ControlRoomRunState.Running);
                Assert.False(transition.AnyTripActive, $"Unexpected transition-step trip in H.18 profile {profile.Id} before interval {index}.");
            }

            var start = ToPlantState(engine.LatestCanonicalSnapshot.Control.ProtectedControl.FullPlant.CandidatePlant);
            var turbineInlet = start.GetFluidNode("turbine-inlet");
            var phaseTransition = previousTurbineInletPhase.HasValue && previousTurbineInletPhase.Value != turbineInlet.Phase;
            previousTurbineInletPhase = turbineInlet.Phase;
            if (index == 1 || index % ObservationStride == 0 || phaseTransition)
            {
                var shadow = new ThermodynamicBranchContinuityModel(
                    productionThermodynamics,
                    productionThermodynamics,
                    ThermodynamicBranchContinuityOptions.H13BoundedHysteresis,
                    H18TargetNodeIds);
                var selected = shadow.Resolve(turbineInlet.Definition, turbineInlet.Inventory, turbineInlet.Thermodynamics);
                var decision = Assert.Single(shadow.Decisions);
                committedRows.Add(new CommittedTurbineObservationRow(
                    profile.Id,
                    index,
                    turbineInlet.Phase.ToString(),
                    decision.ProductionPhase,
                    selected.Phase.ToString(),
                    decision.DecisionKind,
                    decision.SelectionDiffersFromProduction,
                    phaseTransition,
                    decision.PreviousPhaseRelativePressureDrift,
                    decision.PreviousPhaseTemperatureDriftKelvins));
            }

            var presentation = engine.Step(ControlRoomRunState.Running);
            Assert.False(presentation.AnyTripActive, $"Unexpected reference trip in H.18 profile {profile.Id} interval {index}.");

            if (selectedKeys.Contains((profile.Id, index)))
            {
                var end = ToPlantState(engine.LatestCanonicalSnapshot.Control.ProtectedControl.FullPlant.CandidatePlant);
                var hydraulic = solver.Evaluate(start);
                var totalBalances = DeriveInventoryBalances(start, end, Step);
                var frozen = start.FluidNodes.ToDictionary(
                    static node => node.Id,
                    node => totalBalances[node.Id] - hydraulic.FluidNodeBalances[node.Id],
                    StringComparer.Ordinal);
                references.Add((profile.Id, index), new ReferenceInterval(profile.Id, index, start, end, frozen));
            }

            if (index % 1_000 == 0 || index == profile.IntervalCount)
            {
                WriteProgress($"reference-progress profile={profile.Id} interval={index}/{profile.IntervalCount}");
            }
        }
    }

    private static bool ApplyProfileAction(
        ProfileKind kind,
        int intervalIndex,
        IControlRoomRuntimeEngine engine,
        string generatorId,
        ISecondaryTransientFaultTarget coolingTarget)
    {
        switch (kind)
        {
            case ProfileKind.Steady:
                return false;
            case ProfileKind.LoadPulse:
                if (intervalIndex == 501)
                {
                    QueueGeneratorLoad(engine, generatorId, ControlRoomCommandKind.GeneratorLoadLower);
                    return true;
                }
                if (intervalIndex == 3_501)
                {
                    QueueGeneratorLoad(engine, generatorId, ControlRoomCommandKind.GeneratorLoadRaise);
                    return true;
                }
                return false;
            case ProfileKind.CoolingPulse:
                if (intervalIndex == 501)
                {
                    coolingTarget.ActivateCondenserCoolingDegradation("h18-cooling-pulse", "cooling", 0.75d);
                    return true;
                }
                if (intervalIndex == 3_501)
                {
                    coolingTarget.ClearSecondaryTransientFault("h18-cooling-pulse");
                    return true;
                }
                return false;
            case ProfileKind.CombinedLoadCooling:
                if (intervalIndex == 501)
                {
                    QueueGeneratorLoad(engine, generatorId, ControlRoomCommandKind.GeneratorLoadLower);
                    return true;
                }
                if (intervalIndex == 1_001)
                {
                    coolingTarget.ActivateCondenserCoolingDegradation("h18-combined-cooling", "cooling", 0.75d);
                    return true;
                }
                if (intervalIndex == 3_501)
                {
                    QueueGeneratorLoad(engine, generatorId, ControlRoomCommandKind.GeneratorLoadRaise);
                    return true;
                }
                if (intervalIndex == 4_001)
                {
                    coolingTarget.ClearSecondaryTransientFault("h18-combined-cooling");
                    return true;
                }
                return false;
            default:
                throw new ArgumentOutOfRangeException(nameof(kind));
        }
    }

    private static void QueueGeneratorLoad(IControlRoomRuntimeEngine engine, string generatorId, ControlRoomCommandKind kind)
        => engine.QueueOperatorCommand(new ControlRoomCommand(kind, generatorId, ControlRoomCommandTargetKind.Generator));

    private static IReadOnlyList<FourNodeResult> RunFourNodePolicy(
        IReadOnlyList<FrozenH17Evidence> selectedEvidence,
        IReadOnlyDictionary<(string ProfileId, int IntervalIndex), ReferenceInterval> intervals,
        SimplifiedWaterSteamThermodynamicModel productionThermodynamics)
    {
        var results = new List<FourNodeResult>(selectedEvidence.Count);
        var completed = 0;
        foreach (var evidence in selectedEvidence)
        {
            var interval = intervals[(evidence.ProfileId, evidence.IntervalIndex)];
            var shadow = new ThermodynamicBranchContinuityModel(
                productionThermodynamics,
                productionThermodynamics,
                ThermodynamicBranchContinuityOptions.H13BoundedHysteresis,
                H18TargetNodeIds);
            var solver = new JacobianHydraulicCorrectorSolver(shadow);
            var result = solver.Step(
                interval.Start,
                Step,
                interval.FrozenNonHydraulicBalances,
                JacobianHydraulicCorrectorOptions.H9AuditDefault);
            results.Add(new FourNodeResult(evidence, result, shadow.Decisions.ToArray()));
            completed++;
            if (selectedEvidence.Count >= 50 && (completed % 20 == 0 || completed == selectedEvidence.Count))
            {
                WriteProgress($"four-node-policy-progress completed={completed}/{selectedEvidence.Count}");
            }
        }

        return results;
    }

    private static IReadOnlyList<FourNodeResult> SelectDeterminismSentinels(IReadOnlyList<FourNodeResult> results)
    {
        var selected = new Dictionary<(string ProfileId, int IntervalIndex), FourNodeResult>();
        AddClassSentinels(results.Where(static item => !item.Evidence.H17Converged && item.Evidence.TurbineInletPhaseMismatch), selected);
        AddClassSentinels(results.Where(static item => !item.Evidence.H17Converged && !item.Evidence.TurbineInletPhaseMismatch), selected);
        AddClassSentinels(results.Where(static item => item.Evidence.H17Converged), selected);
        return selected.Values
            .OrderBy(static item => item.Evidence.ProfileId, StringComparer.Ordinal)
            .ThenBy(static item => item.Evidence.IntervalIndex)
            .ToArray();
    }

    private static void AddClassSentinels(
        IEnumerable<FourNodeResult> source,
        IDictionary<(string ProfileId, int IntervalIndex), FourNodeResult> selected)
    {
        foreach (var group in source.GroupBy(static item => item.Evidence.ProfileId, StringComparer.Ordinal))
        {
            var ordered = group.OrderBy(static item => item.Evidence.IntervalIndex).ToArray();
            if (ordered.Length == 0)
            {
                continue;
            }
            var count = Math.Min(DeterminismSentinelsPerClass, ordered.Length);
            for (var index = 0; index < count; index++)
            {
                var position = count == 1
                    ? 0
                    : (int)Math.Round(index * (ordered.Length - 1d) / (count - 1d), MidpointRounding.AwayFromZero);
                var item = ordered[position];
                selected[(item.Evidence.ProfileId, item.Evidence.IntervalIndex)] = item;
            }
        }
    }

    private static IReadOnlyList<NodeResidualRow> BuildResidualRows(IReadOnlyList<FourNodeResult> remainingFailures)
    {
        var rows = new List<NodeResidualRow>();
        foreach (var item in remainingFailures)
        {
            var residuals = item.Result.CandidateState.FluidNodes
                .OrderBy(static node => node.Id, StringComparer.Ordinal)
                .Select(node =>
                {
                    var mapped = item.Result.HydraulicEvaluation.FluidNodeBalances[node.Id];
                    var applied = item.Result.AppliedHydraulicBalances[node.Id];
                    return new NodeResidual(
                        node.Id,
                        mapped.NetMassFlowRate.KilogramsPerSecond - applied.NetMassFlowRate.KilogramsPerSecond,
                        mapped.NetEnergyRate.Watts - applied.NetEnergyRate.Watts);
                })
                .ToArray();
            var massRanks = residuals
                .OrderByDescending(static residual => Math.Abs(residual.MassResidualKilogramsPerSecond))
                .Select((residual, index) => (residual.NodeId, Rank: index + 1))
                .ToDictionary(static pair => pair.NodeId, static pair => pair.Rank, StringComparer.Ordinal);
            var energyRanks = residuals
                .OrderByDescending(static residual => Math.Abs(residual.EnergyResidualWatts))
                .Select((residual, index) => (residual.NodeId, Rank: index + 1))
                .ToDictionary(static pair => pair.NodeId, static pair => pair.Rank, StringComparer.Ordinal);
            foreach (var residual in residuals)
            {
                rows.Add(new NodeResidualRow(
                    item.Evidence.ProfileId,
                    item.Evidence.IntervalIndex,
                    item.Evidence.TurbineInletPhaseMismatch,
                    residual.NodeId,
                    residual.MassResidualKilogramsPerSecond,
                    residual.EnergyResidualWatts,
                    massRanks[residual.NodeId],
                    energyRanks[residual.NodeId],
                    item.Result.MaximumRelativePressureFixedPointResidual,
                    item.Result.MaximumAbsoluteFlowFixedPointResidualKilogramsPerSecond,
                    item.Result.NormalizedMeritResidual,
                    item.Result.Iterations.Count == 0 ? double.NaN : item.Result.Iterations[0].NormalizedMeritResidual,
                    item.Result.Iterations.Count < 2 ? double.NaN : item.Result.Iterations[^2].NormalizedMeritResidual,
                    item.Result.Iterations.Count == 0 ? double.NaN : item.Result.Iterations[^1].NormalizedMeritResidual,
                    item.Result.MinimumAcceptedRelaxationFactor));
            }
        }
        return rows;
    }

    private static IReadOnlyList<RemainingFailureInverseRow> BuildRemainingFailureInverseRows(
        IReadOnlyList<FourNodeResult> remainingFailures,
        IReadOnlyDictionary<(string ProfileId, int IntervalIndex), ReferenceInterval> intervals,
        SimplifiedWaterSteamThermodynamicModel productionThermodynamics)
    {
        var rows = new List<RemainingFailureInverseRow>();
        foreach (var item in remainingFailures)
        {
            var reference = intervals[(item.Evidence.ProfileId, item.Evidence.IntervalIndex)];
            var explicitNodes = reference.End.FluidNodes.ToDictionary(static node => node.Id, StringComparer.Ordinal);
            foreach (var candidateNode in item.Result.CandidateState.FluidNodes.OrderBy(static node => node.Id, StringComparer.Ordinal))
            {
                var explicitNode = explicitNodes[candidateNode.Id];
                var candidate = productionThermodynamics.DiagnoseInverseBranchSelection(
                    candidateNode.Definition,
                    candidateNode.Inventory,
                    candidateNode.Thermodynamics);
                var explicitEnd = productionThermodynamics.DiagnoseInverseBranchSelection(
                    explicitNode.Definition,
                    explicitNode.Inventory,
                    explicitNode.Thermodynamics);
                var targeted = H18TargetNodeIds.Contains(candidateNode.Id, StringComparer.Ordinal);
                rows.Add(new RemainingFailureInverseRow(
                    item.Evidence.ProfileId,
                    item.Evidence.IntervalIndex,
                    candidateNode.Id,
                    targeted,
                    candidate.ProductionSelectedBranch,
                    candidate.ProductionSelectedPhase,
                    explicitEnd.ProductionSelectedBranch,
                    explicitEnd.ProductionSelectedPhase,
                    !string.Equals(candidate.ProductionSelectedPhase, explicitEnd.ProductionSelectedPhase, StringComparison.Ordinal),
                    candidate.LateBoundarySaturatedShadowedByEarlierSuperheated
                        && !explicitEnd.LateBoundarySaturatedShadowedByEarlierSuperheated));
            }
        }
        return rows;
    }

    private static IReadOnlyDictionary<string, FluidNodeBalance> DeriveInventoryBalances(
        PlantState start,
        PlantState end,
        TimeSpan deltaTime)
    {
        var endNodes = end.FluidNodes.ToDictionary(static node => node.Id, StringComparer.Ordinal);
        var seconds = deltaTime.TotalSeconds;
        return start.FluidNodes.ToDictionary(
            static node => node.Id,
            node => new FluidNodeBalance(
                MassFlowRate.FromKilogramsPerSecond((endNodes[node.Id].Mass.Kilograms - node.Mass.Kilograms) / seconds),
                Power.FromWatts((endNodes[node.Id].InternalEnergy.Joules - node.InternalEnergy.Joules) / seconds)),
            StringComparer.Ordinal);
    }

    private static PlantState ToPlantState(PlantSnapshot snapshot)
        => new(snapshot.Definition, snapshot.FluidNodes, snapshot.Valves, snapshot.Pumps, snapshot.ThermalBodies, snapshot.HeatSources);

    private static string PolicyFingerprint(IReadOnlyList<FourNodeResult> results)
        => string.Join(
            "||",
            results
                .OrderBy(static item => item.Evidence.ProfileId, StringComparer.Ordinal)
                .ThenBy(static item => item.Evidence.IntervalIndex)
                .Select(item => string.Join(
                    "|",
                    item.Evidence.ProfileId,
                    item.Evidence.IntervalIndex,
                    item.Result.Converged,
                    item.Result.LineSearchExhausted,
                    item.Result.MaximumRelativePressureFixedPointResidual.ToString("G17", CultureInfo.InvariantCulture),
                    item.Result.MaximumAbsoluteFlowFixedPointResidualKilogramsPerSecond.ToString("G17", CultureInfo.InvariantCulture),
                    item.Result.NormalizedMeritResidual.ToString("G17", CultureInfo.InvariantCulture),
                    item.Result.HydraulicEvaluationCount,
                    string.Join(";", item.Decisions.Select(static decision => FormattableString.Invariant(
                        $"{decision.NodeId}:{decision.DecisionKind}:{decision.ProductionPhase}:{decision.SelectedPhase}"))))));

    private static void WriteAuditReports(
        IReadOnlyList<FrozenH17Evidence> frozenEvidence,
        IReadOnlyList<FrozenH17Evidence> selectedEvidence,
        IReadOnlyList<FourNodeResult> results,
        IReadOnlyList<CommittedTurbineObservationRow> committedRows,
        IReadOnlyList<NodeResidualRow> residualRows,
        IReadOnlyList<RemainingFailureInverseRow> inverseRows,
        int determinismSentinels,
        bool deterministicRepeat,
        int recoveredMismatchFailures,
        int recoveredNoMismatchFailures,
        int preservedSuccessControls,
        int turbineInletOverrides,
        bool committedTransparent,
        int committedTransitions,
        IReadOnlyList<string> newUntargetedLateShadowNodes,
        IReadOnlyList<string> newUntargetedPhaseMismatchNodes,
        bool fourNodeExtensionQualifies,
        bool residualSplitDiagnosticPasses)
    {
        var directory = Path.Combine(FindRepositoryRoot(), "artifacts", "h18-turbine-inlet-continuity-residual-floor-split");
        Directory.CreateDirectory(directory);

        var frozenRows = new List<string>
        {
            "profile,interval,h17_converged,h17_turbine_inlet_phase_mismatch,h17_turbine_inlet_candidate_only_late_shadow,selected_for_h18,selection_class",
        };
        var selectedKeys = selectedEvidence.Select(static item => (item.ProfileId, item.IntervalIndex)).ToHashSet();
        foreach (var item in frozenEvidence)
        {
            var selected = selectedKeys.Contains((item.ProfileId, item.IntervalIndex));
            var selectionClass = !item.H17Converged
                ? item.TurbineInletPhaseMismatch ? "h17-failure-turbine-inlet-mismatch" : "h17-failure-no-turbine-inlet-mismatch"
                : selected ? "h17-success-control" : "not-selected";
            frozenRows.Add(FormattableString.Invariant(
                $"{item.ProfileId},{item.IntervalIndex},{item.H17Converged},{item.TurbineInletPhaseMismatch},{item.TurbineInletCandidateOnlyLateShadow},{selected},{selectionClass}"));
        }
        File.WriteAllLines(Path.Combine(directory, "02-frozen-h17-evidence-selection.csv"), frozenRows, Utf8WithoutBom);

        var policyRows = new List<string>
        {
            "profile,interval,h17_converged,h17_turbine_inlet_phase_mismatch,h18_converged,line_search_exhausted,recovered_h17_failure,pressure_residual,flow_residual_kg_s,normalized_merit,hydraulic_evaluations,turbine_inlet_overrides,total_branch_overrides,previous_phase_holds,hysteresis_releases,min_accepted_relaxation",
        };
        foreach (var item in results)
        {
            var decisions = item.Decisions;
            policyRows.Add(FormattableString.Invariant(
                $"{item.Evidence.ProfileId},{item.Evidence.IntervalIndex},{item.Evidence.H17Converged},{item.Evidence.TurbineInletPhaseMismatch},{item.Result.Converged},{item.Result.LineSearchExhausted},{(!item.Evidence.H17Converged && item.Result.Converged)},{item.Result.MaximumRelativePressureFixedPointResidual:G17},{item.Result.MaximumAbsoluteFlowFixedPointResidualKilogramsPerSecond:G17},{item.Result.NormalizedMeritResidual:G17},{item.Result.HydraulicEvaluationCount},{decisions.Count(static decision => string.Equals(decision.NodeId, "turbine-inlet", StringComparison.Ordinal) && decision.SelectionDiffersFromProduction)},{decisions.Count(static decision => decision.SelectionDiffersFromProduction)},{decisions.Count(static decision => decision.SelectedPreviousPhase)},{decisions.Count(static decision => string.Equals(decision.DecisionKind, "production-hysteresis-release", StringComparison.Ordinal))},{item.Result.MinimumAcceptedRelaxationFactor:G17}"));
        }
        File.WriteAllLines(Path.Combine(directory, "03-four-node-policy-results.csv"), policyRows, Utf8WithoutBom);

        var recoveryRows = new List<string>
        {
            "class,total,h18_converged,recovered_or_preserved,h18_failed",
            $"h17-failure-turbine-inlet-mismatch,120,{results.Count(static item => !item.Evidence.H17Converged && item.Evidence.TurbineInletPhaseMismatch && item.Result.Converged)},{recoveredMismatchFailures},{120 - recoveredMismatchFailures}",
            $"h17-failure-no-turbine-inlet-mismatch,125,{results.Count(static item => !item.Evidence.H17Converged && !item.Evidence.TurbineInletPhaseMismatch && item.Result.Converged)},{recoveredNoMismatchFailures},{125 - recoveredNoMismatchFailures}",
            $"h17-success-control,{selectedEvidence.Count(static item => item.H17Converged)},{preservedSuccessControls},{preservedSuccessControls},{selectedEvidence.Count(static item => item.H17Converged) - preservedSuccessControls}",
        };
        File.WriteAllLines(Path.Combine(directory, "04-four-node-recovery-matrix.csv"), recoveryRows, Utf8WithoutBom);

        var residualCsv = new List<string>
        {
            "profile,interval,h17_turbine_inlet_phase_mismatch,node,mapped_minus_applied_mass_kg_s,mapped_minus_applied_energy_w,abs_mass_rank,abs_energy_rank,pressure_residual,flow_residual_kg_s,normalized_merit,first_merit,penultimate_merit,final_merit,min_accepted_relaxation",
        };
        residualCsv.AddRange(residualRows.Select(static item => FormattableString.Invariant(
            $"{item.ProfileId},{item.IntervalIndex},{item.H17TurbineInletPhaseMismatch},{item.NodeId},{item.MassResidualKilogramsPerSecond:G17},{item.EnergyResidualWatts:G17},{item.MassRank},{item.EnergyRank},{item.PressureResidual:G17},{item.FlowResidualKilogramsPerSecond:G17},{item.NormalizedMerit:G17},{item.FirstMerit:G17},{item.PenultimateMerit:G17},{item.FinalMerit:G17},{item.MinimumAcceptedRelaxation:G17}")));
        File.WriteAllLines(Path.Combine(directory, "05-remaining-failure-residual-floor-ranking.csv"), residualCsv, Utf8WithoutBom);

        var inverseCsv = new List<string>
        {
            "profile,interval,node,targeted,candidate_branch,candidate_phase,explicit_branch,explicit_phase,candidate_phase_mismatch,candidate_only_late_shadow",
        };
        inverseCsv.AddRange(inverseRows.Select(static item =>
            $"{item.ProfileId},{item.IntervalIndex},{item.NodeId},{item.Targeted},{item.CandidateBranch},{item.CandidatePhase},{item.ExplicitBranch},{item.ExplicitPhase},{item.CandidatePhaseMismatch},{item.CandidateOnlyLateShadow}"));
        File.WriteAllLines(Path.Combine(directory, "06-remaining-failure-inverse-branch-scan.csv"), inverseCsv, Utf8WithoutBom);

        var committedCsv = new List<string>
        {
            "profile,interval,committed_phase,production_phase,selected_phase,decision_kind,selection_differs_from_production,committed_phase_transition,relative_pressure_drift,temperature_drift_k",
        };
        committedCsv.AddRange(committedRows.Select(static item => FormattableString.Invariant(
            $"{item.ProfileId},{item.IntervalIndex},{item.CommittedPhase},{item.ProductionPhase},{item.SelectedPhase},{item.DecisionKind},{item.SelectionDiffersFromProduction},{item.CommittedPhaseTransition},{item.RelativePressureDrift:G17},{item.TemperatureDriftKelvins:G17}")));
        File.WriteAllLines(Path.Combine(directory, "07-turbine-inlet-committed-transparency.csv"), committedCsv, Utf8WithoutBom);

        var remainingResults = results.Where(static item => !item.Result.Converged).ToArray();
        var remainingPressure = remainingResults.Select(static item => item.Result.MaximumRelativePressureFixedPointResidual).ToArray();
        var remainingFlow = remainingResults.Select(static item => item.Result.MaximumAbsoluteFlowFixedPointResidualKilogramsPerSecond).ToArray();
        var dominantMassCounts = residualRows
            .Where(static item => item.MassRank == 1)
            .GroupBy(static item => item.NodeId, StringComparer.Ordinal)
            .OrderByDescending(static group => group.Count())
            .ThenBy(static group => group.Key, StringComparer.Ordinal)
            .Select(static group => $"{group.Key}:{group.Count()}")
            .ToArray();
        var dominantEnergyCounts = residualRows
            .Where(static item => item.EnergyRank == 1)
            .GroupBy(static item => item.NodeId, StringComparer.Ordinal)
            .OrderByDescending(static group => group.Count())
            .ThenBy(static group => group.Key, StringComparer.Ordinal)
            .Select(static group => $"{group.Key}:{group.Count()}")
            .ToArray();

        var metrics = new List<string>
        {
            "metric,value",
            $"frozen_h17_representatives,{frozenEvidence.Count}",
            $"h17_failures,245",
            $"h17_turbine_inlet_mismatch_failures,120",
            $"h17_no_turbine_inlet_mismatch_failures,125",
            $"h18_success_controls,{selectedEvidence.Count(static item => item.H17Converged)}",
            $"h18_evaluated_samples,{results.Count}",
            $"h18_converged,{results.Count(static item => item.Result.Converged)}",
            $"h18_remaining_failures,{remainingResults.Length}",
            $"recovered_turbine_inlet_mismatch_failures,{recoveredMismatchFailures}",
            $"recovered_no_mismatch_failures,{recoveredNoMismatchFailures}",
            $"preserved_success_controls,{preservedSuccessControls}",
            $"turbine_inlet_branch_overrides,{turbineInletOverrides}",
            $"committed_turbine_inlet_observations,{committedRows.Count}",
            $"committed_turbine_inlet_transitions,{committedTransitions}",
            $"committed_turbine_inlet_transparent,{committedTransparent}",
            $"determinism_sentinels,{determinismSentinels}",
            $"deterministic_repeat,{deterministicRepeat}",
            $"new_untargeted_late_shadow_nodes,{JoinOrNone(newUntargetedLateShadowNodes)}",
            $"new_untargeted_phase_mismatch_nodes,{JoinOrNone(newUntargetedPhaseMismatchNodes)}",
            $"four_node_extension_qualifies,{fourNodeExtensionQualifies}",
            $"residual_floor_split_diagnostic_passes,{residualSplitDiagnosticPasses}",
        };
        File.WriteAllLines(Path.Combine(directory, "08-h18-split-diagnosis-metrics.csv"), metrics, Utf8WithoutBom);

        var recommendation = remainingResults.Length == 0
            ? "H.18 recommendation: the turbine-inlet target extension resolves every frozen H.17 failure in the split set; keep production explicit and re-run a bounded long-horizon qualification with the four-node target set before any activation design."
            : newUntargetedLateShadowNodes.Count > 0 || newUntargetedPhaseMismatchNodes.Count > 0
                ? "H.18 recommendation: the turbine-inlet extension recovers part of the H.17 failure class, but remaining failures expose additional untargeted thermodynamic branch disagreement; keep production explicit and localize those nodes before changing solver or hysteresis limits."
                : "H.18 recommendation: the turbine-inlet extension separates the branch-continuity failure class, while remaining failures show no new untargeted branch disagreement in the audited candidates; keep production explicit and proceed to fixed-point residual-floor / solution-existence analysis of the remaining class before changing solver complexity.";

        var summary = new[]
        {
            "================================================================================",
            "M10.9.4.1-H.18 TURBINE-INLET CONTINUITY EXTENSION & RESIDUAL-FLOOR SPLIT DIAGNOSIS SUMMARY",
            "================================================================================",
            "=== 01-current-v2-four-node-continuity-residual-floor-split ===",
            "Shadow-only A/B split diagnosis over the validated H.17 Hotfix 6 representative evidence. Production Resolve(), explicit integration, H.9, P060/F040 and 2%/5 K hysteresis limits remain unchanged; no shadow candidate is committed.",
            FormattableString.Invariant($"H17-frozen-representatives={frozenEvidence.Count}; H17-failures=245; turbine-inlet-phase-mismatch-failures=120; no-turbine-inlet-mismatch-failures=125; H18-success-controls={selectedEvidence.Count(static item => item.H17Converged)}; H18-evaluated-samples={results.Count};"),
            FormattableString.Invariant($"H18-targets=steam|stop-out|header|turbine-inlet; converged={results.Count(static item => item.Result.Converged)}/{results.Count}; remaining-failures={remainingResults.Length}; recovered-turbine-inlet-mismatch={recoveredMismatchFailures}/120; recovered-no-mismatch={recoveredNoMismatchFailures}/125; preserved-success-controls={preservedSuccessControls}/{selectedEvidence.Count(static item => item.H17Converged)}; turbine-inlet-overrides={turbineInletOverrides}; four-node-extension-qualifies={fourNodeExtensionQualifies};"),
            FormattableString.Invariant($"remaining-pressure-residual-range={RangeOrNone(remainingPressure)}; remaining-flow-residual-range-kg-s={RangeOrNone(remainingFlow)}; dominant-mass-residual-nodes={JoinOrNone(dominantMassCounts)}; dominant-energy-residual-nodes={JoinOrNone(dominantEnergyCounts)};"),
            FormattableString.Invariant($"remaining-untargeted-candidate-only-late-shadow-nodes={JoinOrNone(newUntargetedLateShadowNodes)}; remaining-untargeted-candidate-vs-explicit-phase-mismatch-nodes={JoinOrNone(newUntargetedPhaseMismatchNodes)};"),
            FormattableString.Invariant($"committed-turbine-inlet-observations={committedRows.Count}; committed-turbine-inlet-phase-transitions={committedTransitions}; committed-selection-transparent={committedTransparent}; deterministic-sentinels={determinismSentinels}; deterministic-repeat={deterministicRepeat}; residual-floor-split-diagnostic-passes={residualSplitDiagnosticPasses}; h18-audit-passes={residualSplitDiagnosticPasses};"),
            "production-resolve-order-changed=False; production-previous-state-hysteresis-introduced=False; production-hybrid-active=False; production-fixed-step=10.000 ms; shadow-candidates-committed=False; H9-corrector-replaced=False; plant-network-orchestrator-routing-changed=False; trigger-retuning=False; hysteresis-limit-retuning=False; physical-coefficient-retuning=False; hidden-flow-filtering=False;",
            recommendation,
            FormattableString.Invariant($"Detailed CSV files: \"{directory}\""),
        };
        File.WriteAllLines(Path.Combine(directory, "01-current-v2-turbine-inlet-continuity-residual-floor-split.summary.txt"), summary, Utf8WithoutBom);
        WriteProgress($"audit-complete remaining-failures={remainingResults.Length} four-node-qualifies={fourNodeExtensionQualifies} diagnostic-passes={residualSplitDiagnosticPasses}");
    }

    private static string RangeOrNone(IReadOnlyList<double> values)
    {
        if (values.Count == 0)
        {
            return "none";
        }
        return FormattableString.Invariant($"{values.Min():G17}..{values.Max():G17}");
    }

    private static string JoinOrNone(IEnumerable<string> values)
    {
        var materialized = values.ToArray();
        return materialized.Length == 0 ? "none" : string.Join('|', materialized);
    }

    private static void ResetProgress()
    {
        var directory = Path.Combine(FindRepositoryRoot(), "artifacts", "h18-turbine-inlet-continuity-residual-floor-split");
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, "00-progress.txt"), "H.18 split diagnosis started." + Environment.NewLine, Utf8WithoutBom);
    }

    private static void WriteProgress(string message)
    {
        var directory = Path.Combine(FindRepositoryRoot(), "artifacts", "h18-turbine-inlet-continuity-residual-floor-split");
        Directory.CreateDirectory(directory);
        File.AppendAllText(
            Path.Combine(directory, "00-progress.txt"),
            FormattableString.Invariant($"{DateTime.UtcNow:O} {message}{Environment.NewLine}"),
            Utf8WithoutBom);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "NuclearReactorSimulator.sln")))
            {
                return directory.FullName;
            }
            directory = directory.Parent;
        }
        throw new DirectoryNotFoundException("Could not locate repository root containing NuclearReactorSimulator.sln.");
    }

    private enum ProfileKind
    {
        Steady = 0,
        LoadPulse = 1,
        CoolingPulse = 2,
        CombinedLoadCooling = 3,
    }

    private sealed record ProfileDefinition(string Id, int IntervalCount, ProfileKind Kind);
    private sealed record ReferenceInterval(
        string ProfileId,
        int IntervalIndex,
        PlantState Start,
        PlantState End,
        IReadOnlyDictionary<string, FluidNodeBalance> FrozenNonHydraulicBalances);
    private sealed record FrozenH17Evidence(
        string ProfileId,
        int IntervalIndex,
        string SelectionReasons,
        bool H17Converged,
        bool H17LineSearchExhausted,
        double H17PressureResidual,
        double H17FlowResidualKilogramsPerSecond,
        double H17NormalizedMerit,
        string TurbineInletCandidateBranch,
        string TurbineInletCandidatePhase,
        string TurbineInletExplicitBranch,
        string TurbineInletExplicitPhase,
        bool TurbineInletPhaseMismatch,
        bool TurbineInletCandidateOnlyLateShadow);
    private sealed record FourNodeResult(
        FrozenH17Evidence Evidence,
        JacobianHydraulicCorrectorStepResult Result,
        IReadOnlyList<ThermodynamicBranchContinuityDecision> Decisions);
    private sealed record NodeResidual(string NodeId, double MassResidualKilogramsPerSecond, double EnergyResidualWatts);
    private sealed record NodeResidualRow(
        string ProfileId,
        int IntervalIndex,
        bool H17TurbineInletPhaseMismatch,
        string NodeId,
        double MassResidualKilogramsPerSecond,
        double EnergyResidualWatts,
        int MassRank,
        int EnergyRank,
        double PressureResidual,
        double FlowResidualKilogramsPerSecond,
        double NormalizedMerit,
        double FirstMerit,
        double PenultimateMerit,
        double FinalMerit,
        double MinimumAcceptedRelaxation);
    private sealed record RemainingFailureInverseRow(
        string ProfileId,
        int IntervalIndex,
        string NodeId,
        bool Targeted,
        string CandidateBranch,
        string CandidatePhase,
        string ExplicitBranch,
        string ExplicitPhase,
        bool CandidatePhaseMismatch,
        bool CandidateOnlyLateShadow);
    private sealed record CommittedTurbineObservationRow(
        string ProfileId,
        int IntervalIndex,
        string CommittedPhase,
        string ProductionPhase,
        string SelectedPhase,
        string DecisionKind,
        bool SelectionDiffersFromProduction,
        bool CommittedPhaseTransition,
        double RelativePressureDrift,
        double TemperatureDriftKelvins);
}
