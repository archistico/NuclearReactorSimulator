using System.Security.Cryptography;
using System.Text;
using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios.Recording;
using NuclearReactorSimulator.Application.Scenarios.Training;
using NuclearReactorSimulator.Domain.Physics.Control.Protection;
using NuclearReactorSimulator.Domain.Plant;
using NuclearReactorSimulator.Simulation.Physics.Control.Protection;
using NuclearReactorSimulator.Simulation.Plant;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay;

/// <summary>
/// M10.9.4.1-H.25 targeted committed protection/operational-transient matrix over the unchanged H.22/H.23/H.24
/// corrected-commit runtime. This is intentionally a short qualification gate; H.24 remains the rare 30,000-step soak.
/// </summary>
public sealed class FourNodeCommittedProtectionOperationalTransientMatrixAuditTests
{
    private static readonly TimeSpan Step = TimeSpan.FromMilliseconds(10d);
    private static readonly Encoding Utf8WithoutBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
    private const int WarmupSteps = 64;
    private const int ReversePowerSearchLimit = 400;
    private const int BreakerOpenCoastdownSteps = 128;
    private const double MaximumMassClosureResidualKilograms = 1e-6d;
    private const double MaximumEnergyClosureResidualJoules = 1e-2d;
    private const double MaximumBalanceMassRateResidualKilogramsPerSecond = 1e-8d;
    private const double MaximumBalancePowerResidualWatts = 1e-3d;

    private static readonly IReadOnlyDictionary<string, string> FrozenH24Fingerprints =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["H24_ValidatedCommittedLongHorizonSummary.txt"] = "63A2F0F09E7111E3994FD4CE6AB3E076117F789E64B04AABA9C0A42F2D0415DC",
            ["H24_ValidatedProfileQualificationMetrics.csv"] = "EE700B48F53D5DD39B419A2294C61655F9E48944A82FF5B058CFD4ABAE68F974",
            ["H24_ValidatedCommittedLongHorizonMetrics.csv"] = "00A9D48C28E93FE052B2E2844DF0D63960AC78411D190DE63AE3946F92D0002A",
            ["H24_ValidatedEvidenceManifest.txt"] = "ECAB3C86BE7959AFA09A7B37507952D3A2F5334A26C300C235CBE635E92698FE",
        };

    [Fact]
    public void FrozenH24Evidence_RetainsValidatedCommittedLongHorizonQualification()
    {
        var evidenceDirectory = EvidenceDirectory();
        foreach (var expected in FrozenH24Fingerprints)
        {
            var path = Path.Combine(evidenceDirectory, expected.Key);
            Assert.True(File.Exists(path), $"Frozen H.24 evidence file is missing: {expected.Key}");
            Assert.Equal(expected.Value, CanonicalSha256(path));
        }

        var summary = File.ReadAllText(Path.Combine(evidenceDirectory, "H24_ValidatedCommittedLongHorizonSummary.txt"));
        Assert.Contains("qualification-intervals=30000", summary, StringComparison.Ordinal);
        Assert.Contains("committed-runtime-steps=30008", summary, StringComparison.Ordinal);
        Assert.Contains("corrected-candidates-committed=9626", summary, StringComparison.Ordinal);
        Assert.Contains("H20-rollbacks=0", summary, StringComparison.Ordinal);
        Assert.Contains("fallback-commit-violations=0", summary, StringComparison.Ordinal);
        Assert.Contains("unsafe-corrected-commits=0", summary, StringComparison.Ordinal);
        Assert.Contains("untargeted-branch-disagreements=0", summary, StringComparison.Ordinal);
        Assert.Contains("four-node-committed-long-horizon-cross-profile-qualification-passes=True", summary, StringComparison.Ordinal);
        Assert.Contains("h24-audit-passes=True", summary, StringComparison.Ordinal);

        var manifest = File.ReadAllText(Path.Combine(evidenceDirectory, "H24_ValidatedEvidenceManifest.txt"));
        Assert.Contains("full-telemetry-canonical-sha256=8D077BC89D0DBD539476BC33483C0B734F74E84D5BC20D4FE4D55D6A1B4344FA", manifest, StringComparison.Ordinal);
        Assert.Contains("full-telemetry-data-rows=30008", manifest, StringComparison.Ordinal);
        Assert.Contains("focused-gate-duration=4h31m55s", manifest, StringComparison.Ordinal);
    }

    [Fact]
    public void CorrectedCommitCurrentV2_RetainsExpectedProtectionFunctionCatalogue()
    {
        var engine = CreateEngine();
        var definition = CurrentProtection(engine).Definition;

        AssertProtection(definition.GetTripFunction("very-high-pressure"), ProtectionAction.ReactorScram);
        AssertProtection(definition.GetTripFunction("turbine-overspeed"), ProtectionAction.TurbineTrip | ProtectionAction.GeneratorTrip);
        AssertProtection(definition.GetTripFunction("condenser-high-backpressure"), ProtectionAction.TurbineTrip | ProtectionAction.GeneratorTrip);
        AssertProtection(definition.GetTripFunction("generator-overfrequency"), ProtectionAction.GeneratorTrip);
        AssertProtection(definition.GetTripFunction("steam-drum-low-low-level"), ProtectionAction.ReactorScram | ProtectionAction.TurbineTrip | ProtectionAction.GeneratorTrip);
        AssertProtection(definition.GetTripFunction("generator-reverse-power"), ProtectionAction.GeneratorTrip);
        AssertProtection(definition.GetTripFunction("generator-underfrequency"), ProtectionAction.GeneratorTrip);
        AssertProtection(definition.GetTripFunction("generator-loss-of-synchronism"), ProtectionAction.GeneratorTrip);

        foreach (var id in new[] { "generator-reverse-power", "generator-underfrequency", "generator-loss-of-synchronism" })
        {
            var function = definition.GetTripFunction(id);
            Assert.NotNull(function.Supervision);
            Assert.Equal("generator-breaker-closed", function.Supervision!.MeasurementChannelId);
        }
    }

    [Fact(Explicit = true)]
    [Trait("Category", "FourNodeCommittedProtectionOperationalTransientMatrixAudit")]
    public void OptInCommittedRuntime_PreservesProtectionActionsSupervisionAndFailClosedOwnershipAcrossTargetedMatrix()
    {
        ResetProgress();
        var results = new List<MatrixResult>
        {
            RunNormalLoadManeuver(),
            RunManualReactorScram(),
            RunManualGeneratorTrip(),
            RunReversePowerAutomaticGeneratorTrip(),
            RunBreakerOpenTurbineCoastdownSupervision(),
        };

        var rows = results.SelectMany(static result => result.Rows).ToArray();
        var commits = rows.Count(static row => row.CorrectedCandidateCommitted);
        var rollbacks = rows.Count(static row => row.RollbackRequired);
        var safeFallbacks = rows.Count(static row => row.TriggerObserved && !row.CorrectedCandidateCommitted);
        var fallbackCommitViolations = rows.Count(static row => (!row.H20CandidateEligible || row.RollbackRequired) && row.CorrectedCandidateCommitted);
        var unsafeCommits = rows.Count(static row => row.CorrectedCandidateCommitted && !CommitIsQualified(row));
        var maximumMassClosure = rows.Max(static row => row.MassClosureResidualKilograms);
        var maximumEnergyClosure = rows.Max(static row => row.EnergyClosureResidualJoules);
        var maximumBalanceMassRate = rows.Max(static row => row.BalanceMassRateResidualKilogramsPerSecond);
        var maximumBalancePower = rows.Max(static row => row.BalancePowerResidualWatts);
        var fingerprint = Fingerprint(rows);

        Assert.All(results, static result => Assert.True(result.ExpectedOutcomeSatisfied, $"H.25 scenario '{result.ScenarioId}' did not reach its expected protection/transient outcome."));
        Assert.True(commits > 0, "H.25 targeted matrix observed no corrected commit.");
        Assert.Equal(0, fallbackCommitViolations);
        Assert.Equal(0, unsafeCommits);
        Assert.InRange(maximumMassClosure, 0d, MaximumMassClosureResidualKilograms);
        Assert.InRange(maximumEnergyClosure, 0d, MaximumEnergyClosureResidualJoules);
        Assert.InRange(maximumBalanceMassRate, 0d, MaximumBalanceMassRateResidualKilogramsPerSecond);
        Assert.InRange(maximumBalancePower, 0d, MaximumBalancePowerResidualWatts);

        var defaultEngine = Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(
            new DesktopSustainedGenerationInitialConditionFactory().CreateRuntimeEngine());
        var defaultMode = CurrentHydraulics(defaultEngine).Mode;
        Assert.Equal(HydraulicNumericalCouplingMode.ExplicitCommittedState, defaultMode);

        var passes = results.All(static result => result.ExpectedOutcomeSatisfied)
            && commits > 0
            && fallbackCommitViolations == 0
            && unsafeCommits == 0
            && maximumMassClosure <= MaximumMassClosureResidualKilograms
            && maximumEnergyClosure <= MaximumEnergyClosureResidualJoules
            && maximumBalanceMassRate <= MaximumBalanceMassRateResidualKilogramsPerSecond
            && maximumBalancePower <= MaximumBalancePowerResidualWatts
            && defaultMode == HydraulicNumericalCouplingMode.ExplicitCommittedState;
        Assert.True(passes);

        WriteReports(results, rows, commits, rollbacks, safeFallbacks, fallbackCommitViolations, unsafeCommits,
            maximumMassClosure, maximumEnergyClosure, maximumBalanceMassRate, maximumBalancePower, fingerprint, defaultMode, passes);
    }

    private static MatrixResult RunNormalLoadManeuver()
    {
        var probe = CreateProbe("normal-load-maneuver");
        probe.Advance(WarmupSteps, "steady");
        QueueGeneratorCommand(probe.Engine, ControlRoomCommandKind.GeneratorLoadLower);
        probe.Advance(64, "load-lower");
        QueueGeneratorCommand(probe.Engine, ControlRoomCommandKind.GeneratorLoadRaise);
        probe.Advance(64, "load-raise");
        var presentation = probe.Engine.CreatePresentationSnapshot(ControlRoomRunState.Running);
        var satisfied = !presentation.AnyTripActive;
        Assert.True(satisfied);
        return probe.Complete(satisfied, "no-trip load maneuver");
    }

    private static MatrixResult RunManualReactorScram()
    {
        var probe = CreateProbe("manual-reactor-scram");
        probe.Advance(WarmupSteps, "steady");
        probe.Engine.QueueOperatorCommand(new ControlRoomCommand(ControlRoomCommandKind.ReactorScram));
        probe.Advance(8, "post-scram");
        var protection = CurrentProtection(probe.Engine);
        var satisfied = protection.ReactorScramActive;
        Assert.True(satisfied);
        return probe.Complete(satisfied, "reactor scram latched");
    }

    private static MatrixResult RunManualGeneratorTrip()
    {
        var probe = CreateProbe("manual-generator-trip");
        probe.Advance(WarmupSteps, "steady");
        probe.Engine.QueueOperatorCommand(new ControlRoomCommand(ControlRoomCommandKind.GeneratorTrip));
        probe.Advance(8, "post-generator-trip");
        var protection = CurrentProtection(probe.Engine);
        var satisfied = protection.GeneratorTripActive && !GeneratorBreakerClosed(probe.Engine);
        Assert.True(satisfied);
        return probe.Complete(satisfied, "generator trip latched and breaker open");
    }

    private static MatrixResult RunReversePowerAutomaticGeneratorTrip()
    {
        var probe = CreateProbe("turbine-trip-reverse-power");
        probe.Advance(WarmupSteps, "steady");
        probe.Engine.QueueOperatorCommand(new ControlRoomCommand(ControlRoomCommandKind.TurbineTrip));
        QueueGeneratorCommand(probe.Engine, ControlRoomCommandKind.GeneratorLoadLower);

        var searchSteps = 0;
        while (!CurrentProtection(probe.Engine).GeneratorTripActive && searchSteps < ReversePowerSearchLimit)
        {
            probe.Advance(1, "reverse-power-pickup");
            searchSteps++;
        }

        var protection = CurrentProtection(probe.Engine);
        var reversePower = protection.Functions.Single(static item => item.FunctionId == "generator-reverse-power");
        var satisfied = protection.TurbineTripActive
            && protection.GeneratorTripActive
            && reversePower.IsLatched
            && !GeneratorBreakerClosed(probe.Engine);
        Assert.True(satisfied, $"Reverse-power generator trip was not reached within {ReversePowerSearchLimit} steps.");
        return probe.Complete(satisfied, $"reverse-power automatic generator trip in {searchSteps} steps");
    }

    private static MatrixResult RunBreakerOpenTurbineCoastdownSupervision()
    {
        var probe = CreateProbe("breaker-open-turbine-coastdown");
        probe.Advance(WarmupSteps, "steady");
        QueueGeneratorCommand(probe.Engine, ControlRoomCommandKind.GeneratorBreakerOpen);
        probe.Advance(4, "breaker-open");
        Assert.False(GeneratorBreakerClosed(probe.Engine));
        AssertElectricalGeneratorProtectionsUnsupervised(probe.Engine);

        probe.Engine.QueueOperatorCommand(new ControlRoomCommand(ControlRoomCommandKind.TurbineTrip));
        probe.Advance(BreakerOpenCoastdownSteps, "breaker-open-coastdown");
        var protection = CurrentProtection(probe.Engine);
        AssertElectricalGeneratorProtectionsUnsupervised(probe.Engine);
        var electricalFunctionsLatched = protection.Functions
            .Where(static item => item.FunctionId is "generator-reverse-power" or "generator-underfrequency" or "generator-loss-of-synchronism")
            .Any(static item => item.IsLatched);
        var satisfied = protection.TurbineTripActive && !protection.GeneratorTripActive && !electricalFunctionsLatched;
        Assert.True(satisfied);
        return probe.Complete(satisfied, "breaker-open electrical supervision blocks generator trip eligibility");
    }

    private static MatrixProbe CreateProbe(string scenarioId)
    {
        var engine = CreateEngine();
        Assert.Equal(HydraulicNumericalCouplingMode.FourNodeBranchContinuityCorrectedCommitOptIn, CurrentHydraulics(engine).Mode);
        return new MatrixProbe(scenarioId, engine);
    }

    private static IntegratedAutomaticOperationRuntimeEngine CreateEngine()
        => Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(
            DesktopSustainedGenerationInitialConditionFactory.CreateFourNodeCorrectedCommitEvidenceRuntimeEngine(Step));

    private static void QueueGeneratorCommand(IntegratedAutomaticOperationRuntimeEngine engine, ControlRoomCommandKind commandKind)
    {
        var generator = Assert.Single(engine.CurrentState.PlantDefinition.GeneratorGridSystem.Generators);
        var (targetId, targetKind) = commandKind switch
        {
            ControlRoomCommandKind.GeneratorBreakerOpen or ControlRoomCommandKind.GeneratorBreakerClose
                => (generator.BreakerId, ControlRoomCommandTargetKind.Breaker),
            ControlRoomCommandKind.GeneratorLoadRaise or ControlRoomCommandKind.GeneratorLoadLower
                => (generator.Id, ControlRoomCommandTargetKind.Generator),
            _ => throw new ArgumentOutOfRangeException(nameof(commandKind)),
        };
        engine.QueueOperatorCommand(new ControlRoomCommand(commandKind, targetId, targetKind));
    }

    private static void AssertElectricalGeneratorProtectionsUnsupervised(IntegratedAutomaticOperationRuntimeEngine engine)
    {
        var functions = CurrentProtection(engine).Functions;
        foreach (var id in new[] { "generator-reverse-power", "generator-underfrequency", "generator-loss-of-synchronism" })
        {
            var function = functions.Single(item => string.Equals(item.FunctionId, id, StringComparison.Ordinal));
            Assert.False(function.SupervisionActive);
        }
    }

    private static void AssertProtection(ProtectionFunctionDefinition function, ProtectionAction expectedActions)
        => Assert.Equal(expectedActions, function.Actions);

    private static ProtectionSystemSnapshot CurrentProtection(IntegratedAutomaticOperationRuntimeEngine engine)
        => engine.LatestCanonicalSnapshot.Control.ProtectedControl.Protection;

    private static bool GeneratorBreakerClosed(IntegratedAutomaticOperationRuntimeEngine engine)
        => Assert.Single(engine.LatestCanonicalSnapshot.Control.ProtectedControl.FullPlant.IntegratedCycle.GeneratorGrid.Generators).BreakerFinallyClosed;

    private static PlantNetworkHydraulicNumericalSnapshot CurrentHydraulics(IntegratedAutomaticOperationRuntimeEngine engine)
        => engine.LatestCanonicalSnapshot.Control.ProtectedControl.FullPlant.IntegratedCycle.PrimaryCircuit.HydraulicNumerics;

    private static PlantNetworkAudit CurrentAudit(IntegratedAutomaticOperationRuntimeEngine engine)
        => engine.LatestCanonicalSnapshot.Control.ProtectedControl.FullPlant.IntegratedCycle.PrimaryCircuit.Audit;

    private static MatrixStepRow CaptureRow(string scenarioId, string phase, int stepIndex, IntegratedAutomaticOperationRuntimeEngine engine)
    {
        var numerics = CurrentHydraulics(engine);
        var telemetry = Assert.IsType<FourNodeBranchContinuityIntegrationTelemetry>(numerics.FourNodeBranchContinuity);
        var audit = CurrentAudit(engine);
        var protection = CurrentProtection(engine);
        var presentation = engine.CreatePresentationSnapshot(ControlRoomRunState.Running);
        return new MatrixStepRow(
            scenarioId,
            phase,
            stepIndex,
            ControlRoomSnapshotFingerprint.Compute(presentation),
            telemetry.TriggerObserved,
            telemetry.ShadowCorrectedCandidateEligible,
            telemetry.RollbackRequired,
            telemetry.CorrectedCommitAuthorized,
            telemetry.CorrectedCandidateCommitted,
            telemetry.UntargetedBranchDisagreementDetected,
            telemetry.ShadowCorrectionEvaluated,
            telemetry.ShadowConverged,
            telemetry.ShadowLineSearchExhausted,
            telemetry.ShadowMaximumRelativePressureResidual,
            telemetry.ShadowMaximumAbsoluteFlowResidualKilogramsPerSecond,
            telemetry.ShadowMassClosureKilogramsPerSecond,
            telemetry.ShadowEnergyOwnershipResidualWatts,
            telemetry.ProposedAuthority,
            telemetry.Reason,
            telemetry.CorrectedCommitReason,
            Math.Abs(audit.MassClosureResidualKilograms),
            Math.Abs(audit.EnergyClosureResidualJoules),
            Math.Abs(audit.BalanceMassRateResidualKilogramsPerSecond),
            Math.Abs(audit.BalancePowerResidualWatts),
            protection.LatchedActions.ToString().Replace(", ", "|", StringComparison.Ordinal),
            string.Join("|", protection.Functions.Where(static item => item.IsLatched).Select(static item => item.FunctionId)),
            GeneratorBreakerClosed(engine));
    }

    private static void AssertFailClosedSafety(MatrixStepRow row)
    {
        Assert.InRange(row.MassClosureResidualKilograms, 0d, MaximumMassClosureResidualKilograms);
        Assert.InRange(row.EnergyClosureResidualJoules, 0d, MaximumEnergyClosureResidualJoules);
        Assert.InRange(row.BalanceMassRateResidualKilogramsPerSecond, 0d, MaximumBalanceMassRateResidualKilogramsPerSecond);
        Assert.InRange(row.BalancePowerResidualWatts, 0d, MaximumBalancePowerResidualWatts);
        if (row.RollbackRequired)
        {
            Assert.False(row.CorrectedCandidateCommitted);
        }
        if (row.CorrectedCandidateCommitted)
        {
            Assert.True(CommitIsQualified(row));
        }
    }

    private static bool CommitIsQualified(MatrixStepRow row)
        => row.H20CandidateEligible
            && row.CorrectedCommitAuthorized
            && !row.RollbackRequired
            && !row.UntargetedBranchDisagreementDetected
            && row.ShadowCorrectionEvaluated
            && row.ShadowConverged
            && !row.ShadowLineSearchExhausted
            && row.ShadowMaximumRelativePressureResidual <= 1e-5d
            && row.ShadowMaximumAbsoluteFlowResidualKilogramsPerSecond <= 1e-2d
            && row.ShadowMassClosureKilogramsPerSecond <= 1e-8d
            && row.ShadowEnergyOwnershipResidualWatts <= 1e-3d
            && row.ProposedAuthority == FourNodeBranchContinuityProposedAuthority.CorrectedCandidate
            && row.ActivationReason == FourNodeBranchContinuityActivationReason.QualifiedTriggeredCorrection
            && row.CommitReason == FourNodeBranchContinuityCorrectedCommitReason.QualifiedH20Authority;

    private static string Fingerprint(IReadOnlyList<MatrixStepRow> rows)
    {
        var canonical = string.Join("||", rows.Select(static row => FormattableString.Invariant(
            $"{row.ScenarioId}:{row.Phase}:{row.StepIndex}:{row.PresentationFingerprint}:{row.TriggerObserved}:{row.H20CandidateEligible}:{row.RollbackRequired}:{row.CorrectedCommitAuthorized}:{row.CorrectedCandidateCommitted}:{row.UntargetedBranchDisagreementDetected}:{row.LatchedActions}:{row.LatchedFunctions}:{row.GeneratorBreakerClosed}:{row.MassClosureResidualKilograms:G17}:{row.EnergyClosureResidualJoules:G17}:{row.BalanceMassRateResidualKilogramsPerSecond:G17}:{row.BalancePowerResidualWatts:G17}")));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
    }

    private static void WriteReports(
        IReadOnlyList<MatrixResult> results,
        IReadOnlyList<MatrixStepRow> rows,
        int commits,
        int rollbacks,
        int safeFallbacks,
        int fallbackCommitViolations,
        int unsafeCommits,
        double maximumMassClosure,
        double maximumEnergyClosure,
        double maximumBalanceMassRate,
        double maximumBalancePower,
        string fingerprint,
        HydraulicNumericalCouplingMode defaultMode,
        bool passes)
    {
        var directory = ReportDirectory();
        Directory.CreateDirectory(directory);
        var summary = new List<string>
        {
            "=== 01-current-v2-four-node-committed-protection-operational-transient-matrix ===",
            "H.25 keeps the H.22/H.23/H.24 corrected-commit runtime unchanged and applies a targeted short matrix across normal load manoeuvring, manual reactor/generator protection actions, automatic reverse-power generator trip and breaker-open electrical supervision. H.24 remains the rare long-horizon qualification gate and is not rerun.",
            FormattableString.Invariant($"matrix-scenarios={results.Count}; runtime-steps={rows.Count}; production-fixed-step=10.000 ms; corrected-candidates-committed={commits}; H20-rollbacks={rollbacks}; safe-fallbacks={safeFallbacks}; fallback-commit-violations={fallbackCommitViolations}; unsafe-corrected-commits={unsafeCommits};"),
            FormattableString.Invariant($"max-network-mass-closure-kg={maximumMassClosure:G17}; max-network-energy-closure-j={maximumEnergyClosure:G17}; max-network-balance-mass-rate-kg-s={maximumBalanceMassRate:G17}; max-network-balance-power-w={maximumBalancePower:G17}; telemetry-protection-matrix-fingerprint={fingerprint};"),
            FormattableString.Invariant($"default-current-v2-mode={defaultMode}; opt-in-corrected-commit-mode=FourNodeBranchContinuityCorrectedCommitOptIn; H24-prerequisite-frozen=True; H22-runtime-changed=False; H20-contract-replaced=False; H19-target-set-changed=False; H9-tolerances-retuned=False; P060-F040-retuned=False; bounded-hysteresis-limits-changed=False; physical-coefficient-retuning=False;"),
            FormattableString.Invariant($"four-node-committed-protection-operational-transient-matrix-passes={passes}; h25-audit-passes={passes};"),
            "H.25 recommendation: if green, keep default current-v2 explicit and move next to H.26 integrated rollback/fail-closed stress. Do not rerun the 4.5-hour H.24 gate unless the committed numerical runtime changes or a later closure gate explicitly requires it.",
        };
        foreach (var result in results)
        {
            summary.Insert(summary.Count - 2, FormattableString.Invariant(
                $"scenario={result.ScenarioId}; runtime-steps={result.Rows.Count}; triggers={result.Rows.Count(static row => row.TriggerObserved)}; commits={result.Rows.Count(static row => row.CorrectedCandidateCommitted)}; rollbacks={result.Rows.Count(static row => row.RollbackRequired)}; expected-outcome={result.ExpectedOutcome}; expected-outcome-satisfied={result.ExpectedOutcomeSatisfied};"));
        }
        File.WriteAllLines(Path.Combine(directory, "01-four-node-committed-protection-transient-matrix.summary.txt"), summary, Utf8WithoutBom);

        var csv = new List<string>
        {
            "scenario_id,phase,step,presentation_fingerprint,trigger_observed,h20_candidate_eligible,h20_rollback_required,h22_commit_authorized,corrected_candidate_committed,untargeted_branch_disagreement,latched_actions,latched_functions,generator_breaker_closed,mass_closure_kg,energy_closure_j,balance_mass_rate_kg_s,balance_power_w",
        };
        csv.AddRange(rows.Select(static row => FormattableString.Invariant(
            $"{row.ScenarioId},{row.Phase},{row.StepIndex},{row.PresentationFingerprint},{row.TriggerObserved},{row.H20CandidateEligible},{row.RollbackRequired},{row.CorrectedCommitAuthorized},{row.CorrectedCandidateCommitted},{row.UntargetedBranchDisagreementDetected},{row.LatchedActions},{row.LatchedFunctions},{row.GeneratorBreakerClosed},{row.MassClosureResidualKilograms:G17},{row.EnergyClosureResidualJoules:G17},{row.BalanceMassRateResidualKilogramsPerSecond:G17},{row.BalancePowerResidualWatts:G17}")));
        File.WriteAllLines(Path.Combine(directory, "02-protection-transient-matrix-step-telemetry.csv"), csv, Utf8WithoutBom);

        File.WriteAllLines(Path.Combine(directory, "03-protection-transient-matrix-metrics.csv"),
            new[]
            {
                "metric,value",
                $"matrix_scenarios,{results.Count}",
                $"runtime_steps,{rows.Count}",
                $"corrected_commits,{commits}",
                $"h20_rollbacks,{rollbacks}",
                $"safe_fallbacks,{safeFallbacks}",
                $"fallback_commit_violations,{fallbackCommitViolations}",
                $"unsafe_commits,{unsafeCommits}",
                FormattableString.Invariant($"max_mass_closure_kg,{maximumMassClosure:G17}"),
                FormattableString.Invariant($"max_energy_closure_j,{maximumEnergyClosure:G17}"),
                FormattableString.Invariant($"max_balance_mass_rate_kg_s,{maximumBalanceMassRate:G17}"),
                FormattableString.Invariant($"max_balance_power_w,{maximumBalancePower:G17}"),
                $"matrix_fingerprint,{fingerprint}",
                $"h25_audit_passes,{passes}",
            }, Utf8WithoutBom);
    }

    private static string EvidenceDirectory()
        => Path.Combine(FindRepositoryRoot(), "tests", "NuclearReactorSimulator.Application.Tests", "Scenarios", "Gameplay", "Evidence");

    private static string ReportDirectory()
        => Path.Combine(FindRepositoryRoot(), "artifacts", "h25-four-node-committed-protection-operational-transient-matrix");

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "NuclearReactorSimulator.sln")))
            {
                return current.FullName;
            }
            current = current.Parent;
        }
        throw new DirectoryNotFoundException("Could not locate NuclearReactorSimulator.sln from the test output directory.");
    }

    private static string CanonicalSha256(string path)
    {
        var text = File.ReadAllText(path).Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
    }

    private static void ResetProgress()
    {
        var directory = ReportDirectory();
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }
        Directory.CreateDirectory(directory);
        WriteProgress("H.25 committed protection/operational-transient matrix started");
    }

    private static void WriteProgress(string message)
        => File.WriteAllText(Path.Combine(ReportDirectory(), "00-progress.txt"), $"{DateTimeOffset.UtcNow:O} {message}{Environment.NewLine}", Utf8WithoutBom);

    private sealed class MatrixProbe
    {
        private readonly List<MatrixStepRow> _rows = new();
        private int _stepIndex;

        public MatrixProbe(string scenarioId, IntegratedAutomaticOperationRuntimeEngine engine)
        {
            ScenarioId = scenarioId;
            Engine = engine;
            WriteProgress($"scenario-start id={scenarioId}");
        }

        public string ScenarioId { get; }
        public IntegratedAutomaticOperationRuntimeEngine Engine { get; }

        public void Advance(int steps, string phase)
        {
            for (var step = 0; step < steps; step++)
            {
                Engine.Step(ControlRoomRunState.Running);
                _stepIndex++;
                var row = CaptureRow(ScenarioId, phase, _stepIndex, Engine);
                AssertFailClosedSafety(row);
                _rows.Add(row);
            }
        }

        public MatrixResult Complete(bool expectedOutcomeSatisfied, string expectedOutcome)
        {
            WriteProgress($"scenario-complete id={ScenarioId} steps={_rows.Count} commits={_rows.Count(static row => row.CorrectedCandidateCommitted)} rollbacks={_rows.Count(static row => row.RollbackRequired)} outcome={expectedOutcomeSatisfied}");
            return new MatrixResult(ScenarioId, _rows.ToArray(), expectedOutcome, expectedOutcomeSatisfied);
        }
    }

    private sealed record MatrixResult(string ScenarioId, IReadOnlyList<MatrixStepRow> Rows, string ExpectedOutcome, bool ExpectedOutcomeSatisfied);

    private sealed record MatrixStepRow(
        string ScenarioId,
        string Phase,
        int StepIndex,
        string PresentationFingerprint,
        bool TriggerObserved,
        bool H20CandidateEligible,
        bool RollbackRequired,
        bool CorrectedCommitAuthorized,
        bool CorrectedCandidateCommitted,
        bool UntargetedBranchDisagreementDetected,
        bool ShadowCorrectionEvaluated,
        bool ShadowConverged,
        bool ShadowLineSearchExhausted,
        double ShadowMaximumRelativePressureResidual,
        double ShadowMaximumAbsoluteFlowResidualKilogramsPerSecond,
        double ShadowMassClosureKilogramsPerSecond,
        double ShadowEnergyOwnershipResidualWatts,
        FourNodeBranchContinuityProposedAuthority ProposedAuthority,
        FourNodeBranchContinuityActivationReason ActivationReason,
        FourNodeBranchContinuityCorrectedCommitReason CommitReason,
        double MassClosureResidualKilograms,
        double EnergyClosureResidualJoules,
        double BalanceMassRateResidualKilogramsPerSecond,
        double BalancePowerResidualWatts,
        string LatchedActions,
        string LatchedFunctions,
        bool GeneratorBreakerClosed);
}
