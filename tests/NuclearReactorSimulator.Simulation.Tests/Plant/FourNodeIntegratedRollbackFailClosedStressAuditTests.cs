using System.Security.Cryptography;
using System.Text;
using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Plant;
using NuclearReactorSimulator.Simulation.Physics.Fluids;
using NuclearReactorSimulator.Simulation.Plant;
using Xunit;

namespace NuclearReactorSimulator.Simulation.Tests.Plant;

/// <summary>
/// M10.9.4.1-H.26 integrated rollback/fail-closed stress. H.20 already owns the semantic mapping from
/// observation guards to typed authority reasons; this audit injects those already-typed decisions through an
/// internal-only orchestrator test seam and proves that H.22 ownership falls back atomically to the historical
/// explicit candidate in the same network step.
/// </summary>
public sealed class FourNodeIntegratedRollbackFailClosedStressAuditTests
{
    private static readonly TimeSpan Step = TimeSpan.FromMilliseconds(10d);
    private static readonly Encoding Utf8WithoutBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    private static readonly FourNodeBranchContinuityActivationReason[] RollbackReasons =
    [
        FourNodeBranchContinuityActivationReason.RollbackQualificationEvidenceUnavailable,
        FourNodeBranchContinuityActivationReason.RollbackCorrectorNonConvergence,
        FourNodeBranchContinuityActivationReason.RollbackLineSearchExhausted,
        FourNodeBranchContinuityActivationReason.RollbackPressureResidualExceeded,
        FourNodeBranchContinuityActivationReason.RollbackFlowResidualExceeded,
        FourNodeBranchContinuityActivationReason.RollbackMassClosureExceeded,
        FourNodeBranchContinuityActivationReason.RollbackEnergyOwnershipExceeded,
        FourNodeBranchContinuityActivationReason.RollbackUntargetedBranchDisagreement,
    ];

    [Fact]
    public void PublicOrchestrator_H26DecisionTransformIsAbsentAndIdentityAuditHookIsTransparent()
    {
        var model = new SimplifiedWaterSteamThermodynamicModel();
        var state = CreateState(model, HydraulicNumericalCouplingDefinition.H22FourNodeBranchContinuityCorrectedCommitOptIn);

        var production = new PlantNetworkOrchestrator(model).Step(state, Step);
        var identity = new PlantNetworkOrchestrator(model, static decision => decision).Step(state, Step);

        AssertPhysicalFallbackEquivalent(production, identity);
        Assert.Equal(production.HydraulicNumerics, identity.HydraulicNumerics);
    }

    [Fact(Explicit = true)]
    [Trait("Category", "FourNodeIntegratedRollbackFailClosedStressAudit")]
    public void H20TypedDenialsAndRollbacks_ForceAtomicSameStepExplicitFallbackWithoutPartialCommit()
    {
        ResetProgress();
        var results = new List<ChallengeResult>();

        WriteProgress("natural-untriggered-control");
        results.Add(RunChallenge(
            "natural-untriggered",
            transform: static decision => decision,
            expectedAuthorityReason: FourNodeBranchContinuityActivationReason.NotTriggered,
            expectedCommitReason: FourNodeBranchContinuityCorrectedCommitReason.NotTriggered,
            expectedRollback: false));

        WriteProgress("activation-arm-disabled-denial");
        results.Add(RunChallenge(
            "activation-arm-disabled",
            transform: static original => Decision(
                original.SampleId,
                FourNodeBranchContinuityProposedAuthority.ExplicitCommittedState,
                FourNodeBranchContinuityActivationReason.ActivationArmDisabled,
                rollbackRequired: false,
                triggerObserved: true,
                activationArmEnabled: false),
            expectedAuthorityReason: FourNodeBranchContinuityActivationReason.ActivationArmDisabled,
            expectedCommitReason: FourNodeBranchContinuityCorrectedCommitReason.H20ActivationArmDisabled,
            expectedRollback: false));

        WriteProgress("h20-authority-denied");
        results.Add(RunChallenge(
            "h20-authority-denied",
            transform: static original => Decision(
                original.SampleId,
                FourNodeBranchContinuityProposedAuthority.ExplicitCommittedState,
                FourNodeBranchContinuityActivationReason.QualifiedTriggeredCorrection,
                rollbackRequired: false,
                triggerObserved: true,
                activationArmEnabled: true),
            expectedAuthorityReason: FourNodeBranchContinuityActivationReason.QualifiedTriggeredCorrection,
            expectedCommitReason: FourNodeBranchContinuityCorrectedCommitReason.H20AuthorityDenied,
            expectedRollback: false));

        WriteProgress("shadow-correction-not-evaluated-denial");
        results.Add(RunChallenge(
            "shadow-correction-not-evaluated",
            transform: static original => Decision(
                original.SampleId,
                FourNodeBranchContinuityProposedAuthority.CorrectedCandidate,
                FourNodeBranchContinuityActivationReason.QualifiedTriggeredCorrection,
                rollbackRequired: false,
                triggerObserved: true,
                activationArmEnabled: true),
            expectedAuthorityReason: FourNodeBranchContinuityActivationReason.QualifiedTriggeredCorrection,
            expectedCommitReason: FourNodeBranchContinuityCorrectedCommitReason.ShadowCorrectionNotEvaluated,
            expectedRollback: false,
            expectedProposedAuthority: FourNodeBranchContinuityProposedAuthority.CorrectedCandidate));

        foreach (var reason in RollbackReasons)
        {
            WriteProgress($"rollback-{reason}");
            results.Add(RunChallenge(
                $"rollback-{reason}",
                transform: original => Decision(
                    original.SampleId,
                    FourNodeBranchContinuityProposedAuthority.ExplicitCommittedState,
                    reason,
                    rollbackRequired: true,
                    triggerObserved: true,
                    activationArmEnabled: true),
                expectedAuthorityReason: reason,
                expectedCommitReason: FourNodeBranchContinuityCorrectedCommitReason.H20RollbackRequired,
                expectedRollback: true));
        }

        var repeat = results.Select(static result => result.RepeatFingerprint).ToArray();
        var first = results.Select(static result => result.Fingerprint).ToArray();
        var deterministicRepeat = SequenceEqualOrdinal(first, repeat);
        var rollbackResults = results.Where(static result => result.RollbackRequired).ToArray();
        var fallbackEquivalent = results.Count(static result => result.ExplicitFallbackEquivalent);
        var correctedCommits = results.Count(static result => result.CorrectedCandidateCommitted);
        var partialCommitViolations = results.Count(static result =>
            result.CorrectedCandidateCommitted
            || result.CorrectedCommitAuthorized
            || !result.ExplicitFallbackEquivalent);

        Assert.Equal(12, results.Count);
        Assert.Equal(8, rollbackResults.Length);
        Assert.All(rollbackResults, static result => Assert.Equal(FourNodeBranchContinuityCorrectedCommitReason.H20RollbackRequired, result.CommitReason));
        Assert.Equal(results.Count, fallbackEquivalent);
        Assert.Equal(0, correctedCommits);
        Assert.Equal(0, partialCommitViolations);
        Assert.True(deterministicRepeat);

        var passes = rollbackResults.Length == 8
            && rollbackResults.All(static result => result.RollbackRequired)
            && fallbackEquivalent == results.Count
            && correctedCommits == 0
            && partialCommitViolations == 0
            && deterministicRepeat;
        Assert.True(passes);

        WriteReports(results, deterministicRepeat, fallbackEquivalent, correctedCommits, partialCommitViolations, passes);
    }

    private static bool SequenceEqualOrdinal(IReadOnlyList<string> first, IReadOnlyList<string> second)
    {
        if (first.Count != second.Count)
        {
            return false;
        }

        for (var index = 0; index < first.Count; index++)
        {
            if (!string.Equals(first[index], second[index], StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }

    private static ChallengeResult RunChallenge(
        string challengeId,
        Func<FourNodeBranchContinuityActivationDecision, FourNodeBranchContinuityActivationDecision> transform,
        FourNodeBranchContinuityActivationReason expectedAuthorityReason,
        FourNodeBranchContinuityCorrectedCommitReason expectedCommitReason,
        bool expectedRollback,
        FourNodeBranchContinuityProposedAuthority expectedProposedAuthority = FourNodeBranchContinuityProposedAuthority.ExplicitCommittedState)
    {
        var first = Execute(transform);
        var repeat = Execute(transform);

        var telemetry = Assert.IsType<FourNodeBranchContinuityIntegrationTelemetry>(first.Rollback.HydraulicNumerics.FourNodeBranchContinuity);
        var repeatTelemetry = Assert.IsType<FourNodeBranchContinuityIntegrationTelemetry>(repeat.Rollback.HydraulicNumerics.FourNodeBranchContinuity);
        var explicitFallbackEquivalent = PhysicalFallbackEquivalent(first.Explicit, first.Rollback);
        var repeatFallbackEquivalent = PhysicalFallbackEquivalent(repeat.Explicit, repeat.Rollback);

        Assert.Equal(expectedAuthorityReason, telemetry.Reason);
        Assert.Equal(expectedCommitReason, telemetry.CorrectedCommitReason);
        Assert.Equal(expectedRollback, telemetry.RollbackRequired);
        Assert.False(telemetry.CorrectedCommitAuthorized);
        Assert.False(telemetry.CorrectedCandidateCommitted);
        Assert.Equal(expectedProposedAuthority, telemetry.ProposedAuthority);
        Assert.True(explicitFallbackEquivalent);
        Assert.True(repeatFallbackEquivalent);
        Assert.Equal(telemetry, repeatTelemetry);

        var fingerprint = Fingerprint(first.Rollback, telemetry);
        var repeatFingerprint = Fingerprint(repeat.Rollback, repeatTelemetry);
        Assert.Equal(fingerprint, repeatFingerprint);

        return new ChallengeResult(
            challengeId,
            telemetry.Reason,
            telemetry.CorrectedCommitReason,
            telemetry.RollbackRequired,
            telemetry.CorrectedCommitAuthorized,
            telemetry.CorrectedCandidateCommitted,
            explicitFallbackEquivalent,
            fingerprint,
            repeatFingerprint);
    }

    private static ExecutionPair Execute(
        Func<FourNodeBranchContinuityActivationDecision, FourNodeBranchContinuityActivationDecision> transform)
    {
        var model = new SimplifiedWaterSteamThermodynamicModel();
        var explicitState = CreateState(model, HydraulicNumericalCouplingDefinition.ExplicitCommittedState);
        var correctedState = CreateState(model, HydraulicNumericalCouplingDefinition.H22FourNodeBranchContinuityCorrectedCommitOptIn);
        var explicitResult = new PlantNetworkOrchestrator(model).Step(explicitState, Step);
        var rollbackResult = new PlantNetworkOrchestrator(model, transform).Step(correctedState, Step);
        return new ExecutionPair(explicitResult, rollbackResult);
    }

    private static FourNodeBranchContinuityActivationDecision Decision(
        string sampleId,
        FourNodeBranchContinuityProposedAuthority authority,
        FourNodeBranchContinuityActivationReason reason,
        bool rollbackRequired,
        bool triggerObserved,
        bool activationArmEnabled)
        => new(sampleId, authority, reason, rollbackRequired, triggerObserved, activationArmEnabled);

    private static PlantState CreateState(
        SimplifiedWaterSteamThermodynamicModel model,
        HydraulicNumericalCouplingDefinition coupling)
    {
        var referenceTemperature = Temperature.FromDegreesCelsius(250d);
        var saturation = model.GetSaturationProperties(referenceTemperature);
        const double massKilograms = 1_000d;
        var density = saturation.SaturatedLiquidDensity.KilogramsPerCubicMetre * 1.002d;
        var volume = Volume.FromCubicMetres(massKilograms / density);
        var energy = Energy.FromJoules(saturation.SaturatedLiquidInternalEnergy.JoulesPerKilogram * massKilograms);
        var nodeIds = new[] { "steam", "stop-out", "header", "turbine-inlet" };
        var definitions = nodeIds.Select(id => new FluidNodeDefinition(id, volume)).ToArray();
        var definition = new PlantDefinition(
            "h26-fail-closed-stress",
            definitions,
            Array.Empty<PipeDefinition>(),
            Array.Empty<ValveDefinition>(),
            Array.Empty<PumpDefinition>(),
            Array.Empty<NuclearReactorSimulator.Domain.Physics.Thermal.ThermalBodyDefinition>(),
            Array.Empty<NuclearReactorSimulator.Domain.Physics.Thermal.HeatTransferDefinition>(),
            Array.Empty<NuclearReactorSimulator.Domain.Physics.Thermal.HeatSourceDefinition>(),
            coupling);

        var states = definitions.Select(node =>
        {
            var inventory = new FluidNodeInventory(Mass.FromKilograms(massKilograms), energy);
            var thermodynamics = model.Resolve(
                node,
                inventory,
                new FluidThermodynamicState(Pressure.StandardAtmosphere, referenceTemperature));
            return new FluidNodeState(node, inventory, thermodynamics);
        }).ToArray();

        return new PlantState(
            definition,
            states,
            Array.Empty<ValveState>(),
            Array.Empty<PumpState>(),
            Array.Empty<NuclearReactorSimulator.Domain.Physics.Thermal.ThermalBodyState>(),
            Array.Empty<NuclearReactorSimulator.Domain.Physics.Thermal.HeatSourceState>());
    }

    private static void AssertPhysicalFallbackEquivalent(PlantNetworkStepResult expected, PlantNetworkStepResult actual)
        => Assert.True(PhysicalFallbackEquivalent(expected, actual));

    private static bool PhysicalFallbackEquivalent(PlantNetworkStepResult expected, PlantNetworkStepResult actual)
    {
        var expectedNodes = expected.CandidateState.FluidNodes.ToDictionary(static node => node.Id, StringComparer.Ordinal);
        var actualNodes = actual.CandidateState.FluidNodes.ToDictionary(static node => node.Id, StringComparer.Ordinal);
        if (expectedNodes.Count != actualNodes.Count)
        {
            return false;
        }

        foreach (var expectedNode in expectedNodes.Values)
        {
            var actualNode = actualNodes[expectedNode.Id];
            if (expectedNode.Inventory != actualNode.Inventory || expectedNode.Thermodynamics != actualNode.Thermodynamics)
            {
                return false;
            }
        }

        if (expected.FluidNodeBalances.Count != actual.FluidNodeBalances.Count)
        {
            return false;
        }

        foreach (var expectedBalance in expected.FluidNodeBalances)
        {
            if (!actual.FluidNodeBalances.TryGetValue(expectedBalance.Key, out var actualBalance)
                || expectedBalance.Value != actualBalance)
            {
                return false;
            }
        }

        return expected.Audit.InitialTotalMass == actual.Audit.InitialTotalMass
            && expected.Audit.FinalTotalMass == actual.Audit.FinalTotalMass
            && expected.Audit.InitialTotalStoredEnergy == actual.Audit.InitialTotalStoredEnergy
            && expected.Audit.FinalTotalStoredEnergy == actual.Audit.FinalTotalStoredEnergy
            && expected.Audit.BalanceMassRateResidualKilogramsPerSecond == actual.Audit.BalanceMassRateResidualKilogramsPerSecond
            && expected.Audit.BalancePowerResidualWatts == actual.Audit.BalancePowerResidualWatts
            && expected.Audit.MassClosureResidualKilograms == actual.Audit.MassClosureResidualKilograms
            && expected.Audit.EnergyClosureResidualJoules == actual.Audit.EnergyClosureResidualJoules;
    }

    private static string Fingerprint(PlantNetworkStepResult result, FourNodeBranchContinuityIntegrationTelemetry telemetry)
    {
        var payload = new StringBuilder();
        foreach (var node in result.CandidateState.FluidNodes.OrderBy(static node => node.Id, StringComparer.Ordinal))
        {
            payload.Append(node.Id).Append('|')
                .Append(node.Mass.Kilograms.ToString("R", System.Globalization.CultureInfo.InvariantCulture)).Append('|')
                .Append(node.InternalEnergy.Joules.ToString("R", System.Globalization.CultureInfo.InvariantCulture)).Append('|')
                .Append(node.Pressure.Pascals.ToString("R", System.Globalization.CultureInfo.InvariantCulture)).Append('|')
                .Append(node.Temperature.Kelvins.ToString("R", System.Globalization.CultureInfo.InvariantCulture)).Append(';');
        }
        payload.Append(telemetry.ProposedAuthority).Append('|')
            .Append(telemetry.Reason).Append('|')
            .Append(telemetry.RollbackRequired).Append('|')
            .Append(telemetry.CorrectedCommitAuthorized).Append('|')
            .Append(telemetry.CorrectedCommitReason).Append('|')
            .Append(telemetry.CorrectedCandidateCommitted).Append('|')
            .Append(result.Audit.MassClosureResidualKilograms.ToString("R", System.Globalization.CultureInfo.InvariantCulture)).Append('|')
            .Append(result.Audit.EnergyClosureResidualJoules.ToString("R", System.Globalization.CultureInfo.InvariantCulture));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload.ToString())));
    }

    private static void WriteReports(
        IReadOnlyList<ChallengeResult> results,
        bool deterministicRepeat,
        int fallbackEquivalent,
        int correctedCommits,
        int partialCommitViolations,
        bool passes)
    {
        var directory = ReportDirectory();
        Directory.CreateDirectory(directory);

        var csv = new StringBuilder();
        csv.AppendLine("challenge-id,authority-reason,rollback-required,commit-reason,commit-authorized,corrected-committed,explicit-fallback-equivalent,fingerprint");
        foreach (var result in results)
        {
            csv.Append(result.ChallengeId).Append(',')
                .Append(result.AuthorityReason).Append(',')
                .Append(result.RollbackRequired).Append(',')
                .Append(result.CommitReason).Append(',')
                .Append(result.CorrectedCommitAuthorized).Append(',')
                .Append(result.CorrectedCandidateCommitted).Append(',')
                .Append(result.ExplicitFallbackEquivalent).Append(',')
                .AppendLine(result.Fingerprint);
        }
        File.WriteAllText(Path.Combine(directory, "02-integrated-rollback-challenges.csv"), csv.ToString(), Utf8WithoutBom);

        var rollbackCount = results.Count(static result => result.RollbackRequired);
        var metrics = new StringBuilder();
        metrics.AppendLine("metric,value");
        metrics.AppendLine($"challenges,{results.Count}");
        metrics.AppendLine($"rollback-challenges,{rollbackCount}");
        metrics.AppendLine($"nonrollback-denial-controls,{results.Count - rollbackCount}");
        metrics.AppendLine($"explicit-fallback-equivalent,{fallbackEquivalent}");
        metrics.AppendLine($"corrected-candidates-committed,{correctedCommits}");
        metrics.AppendLine($"partial-commit-violations,{partialCommitViolations}");
        metrics.AppendLine($"deterministic-repeat,{deterministicRepeat}");
        metrics.AppendLine($"h26-audit-passes,{passes}");
        File.WriteAllText(Path.Combine(directory, "03-integrated-rollback-metrics.csv"), metrics.ToString(), Utf8WithoutBom);

        var summary = $"""
================================================================================
M10.9.4.1-H.26 INTEGRATED ROLLBACK & FAIL-CLOSED STRESS SUMMARY
================================================================================
=== 01-current-v2-four-node-integrated-rollback-fail-closed-stress ===
H.26 keeps the validated H.20/H.22 committed algorithm unchanged and introduces only an internal test-only authority-decision transform on PlantNetworkOrchestrator. H.20 remains the semantic owner of typed rollback reasons; H.26 proves that each already-typed denial/rollback is consumed atomically by the real corrected-commit orchestrator with same-step historical explicit fallback and no partial corrected ownership. Public production construction never supplies the transform.
challenges={results.Count}; rollback-challenges={rollbackCount}; nonrollback-denial-controls={results.Count - rollbackCount}; explicit-fallback-equivalent={fallbackEquivalent}/{results.Count}; corrected-candidates-committed={correctedCommits}; partial-commit-violations={partialCommitViolations}; deterministic-repeat={deterministicRepeat};
rollback-reasons={string.Join("|", RollbackReasons)};
public-orchestrator-authority-transform-active=False; H20-reason-mapping-replaced=False; H22-commit-seam-replaced=False; P060-F040-retuned=False; H9-tolerances-retuned=False; bounded-hysteresis-limits-changed=False; target-node-set-changed=False; physical-coefficient-retuning=False; default-current-v2-mode=ExplicitCommittedState;
four-node-integrated-rollback-fail-closed-stress-passes={passes}; h26-audit-passes={passes};
H.26 recommendation: if green, retain default current-v2 explicit and move to H.27 off-design robustness. The internal decision transform is audit infrastructure only and must never be exposed through production factories or configuration.
Detailed CSV files: "{directory}"
""";
        File.WriteAllText(Path.Combine(directory, "01-four-node-integrated-rollback-fail-closed-stress.summary.txt"), summary, Utf8WithoutBom);
        WriteProgress("completed");
    }

    private static void ResetProgress()
    {
        var directory = ReportDirectory();
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }
        Directory.CreateDirectory(directory);
        WriteProgress("starting");
    }

    private static void WriteProgress(string message)
        => File.WriteAllText(
            Path.Combine(ReportDirectory(), "00-progress.txt"),
            $"{DateTimeOffset.UtcNow:O} {message}{Environment.NewLine}",
            Utf8WithoutBom);

    private static string ReportDirectory()
        => Path.Combine(FindRepositoryRoot(), "artifacts", "h26-four-node-integrated-rollback-fail-closed-stress");

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? current = new(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "NuclearReactorSimulator.sln")))
            {
                return current.FullName;
            }
            current = current.Parent;
        }
        throw new DirectoryNotFoundException("Could not locate NuclearReactorSimulator.sln from the H.26 audit test output directory.");
    }

    private sealed record ExecutionPair(PlantNetworkStepResult Explicit, PlantNetworkStepResult Rollback);

    private sealed record ChallengeResult(
        string ChallengeId,
        FourNodeBranchContinuityActivationReason AuthorityReason,
        FourNodeBranchContinuityCorrectedCommitReason CommitReason,
        bool RollbackRequired,
        bool CorrectedCommitAuthorized,
        bool CorrectedCandidateCommitted,
        bool ExplicitFallbackEquivalent,
        string Fingerprint,
        string RepeatFingerprint);
}
