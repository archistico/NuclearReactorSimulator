using System.Security.Cryptography;
using System.Text;
using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios;
using NuclearReactorSimulator.Application.Scenarios.Recording;
using NuclearReactorSimulator.Application.Scenarios.Training;
using NuclearReactorSimulator.Simulation.Plant;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay;

/// <summary>
/// M10 Final exact-v9 production-activation candidate. Diagnostic 11 Hotfix 2 already qualified exact-v9 directly for
/// 600 s. This gate validates only deployment wiring: opt-in policy selection, scenario/registry identity, fail-closed
/// exact-v2 kill behavior, unchanged exact-v4 authoritative default, short policy-path health and deterministic identity.
/// </summary>
public sealed class M10FinalExactV9ProductionActivationCandidateTests
{
    private const string OptInEnvironmentVariable = "NRS_M10_FINAL_V9_ACTIVATION_CANDIDATE";
    private const string PrerequisitesEnvironmentVariable = "NRS_M10_FINAL_V9_PREREQUISITES_PASSED";
    private const int HealthSteps = 12_000;
    private const int DeterminismSteps = 128;
    private static readonly UTF8Encoding Utf8WithoutBom = new(encoderShouldEmitUTF8Identifier: false);

    [Fact]
    public void QualifiedExactV9_IsRegisteredAsOptInPolicyWithoutChangingExactV4AuthoritativeDefault()
    {
        Assert.Equal(
            DesktopHydraulicProductionPolicy.I5RepairedFourNodeCorrectedCommit,
            DesktopHydraulicProductionPolicySelector.AuthoritativeDefaultPolicy);
        Assert.Equal(
            DesktopHydraulicProductionPolicy.M10FinalExactV9QualifiedCandidate,
            DesktopHydraulicProductionPolicySelector.M10FinalQualifiedCandidatePolicy);

        var authoritative = DesktopHydraulicProductionPolicySelector.Resolve(
            DesktopHydraulicProductionPolicySelector.AuthoritativeDefaultPolicy);
        var candidate = DesktopHydraulicProductionPolicySelector.Resolve(
            DesktopHydraulicProductionPolicySelector.M10FinalQualifiedCandidatePolicy);
        var killed = DesktopHydraulicProductionPolicySelector.Resolve(
            DesktopHydraulicProductionPolicySelector.M10FinalQualifiedCandidatePolicy,
            explicitKillRequested: true);

        Assert.Equal(DesktopSustainedGenerationI5RepairedActivationCandidateInitialConditionFactory.Reference, authoritative.InitialCondition);
        Assert.Equal(4, authoritative.InitialCondition.Version);
        Assert.Equal(DesktopSustainedGenerationPostMoistureEquilibriumCandidateInitialConditionFactory.Reference, candidate.InitialCondition);
        Assert.Equal(9, candidate.InitialCondition.Version);
        Assert.Equal(DesktopSustainedGenerationInitialConditionFactory.Reference, killed.InitialCondition);
        Assert.Equal(2, killed.InitialCondition.Version);
        Assert.True(killed.ExplicitKillApplied);

        Assert.IsType<DesktopSustainedGenerationPostMoistureEquilibriumCandidateInitialConditionFactory>(
            DesktopHydraulicProductionPolicySelector.CreateFactory(candidate));
        Assert.Equal(
            DesktopIntegratedOperationsProductionProgram.RepairedProductionScenario,
            DesktopIntegratedOperationsProductionProgram.Scenario);
        Assert.Equal(
            DesktopIntegratedOperationsM10FinalV9ActivationCandidateProgram.Scenario,
            DesktopIntegratedOperationsProductionProgram.ResolveScenario(
                DesktopHydraulicProductionPolicySelector.M10FinalQualifiedCandidatePolicy));
    }

    [Fact(Explicit = true)]
    [Trait("Category", "M10FinalExactV9ProductionActivationCandidate")]
    public void QualifiedExactV9_PolicyPathPreservesHealthConservationMoistureOwnershipAndDeterministicIdentity()
    {
        RequireOptIn();
        ResetReportDirectory();

        var current = DesktopHydraulicProductionPolicySelector.Resolve(
            DesktopHydraulicProductionPolicySelector.AuthoritativeDefaultPolicy);
        var candidate = DesktopHydraulicProductionPolicySelector.Resolve(
            DesktopHydraulicProductionPolicySelector.M10FinalQualifiedCandidatePolicy);
        var rollback = DesktopHydraulicProductionPolicySelector.Resolve(
            DesktopHydraulicProductionPolicySelector.M10FinalQualifiedCandidatePolicy,
            explicitKillRequested: true);

        Assert.Equal(DesktopHydraulicProductionPolicy.I5RepairedFourNodeCorrectedCommit, current.EffectivePolicy);
        Assert.Equal(DesktopSustainedGenerationI5RepairedActivationCandidateInitialConditionFactory.Reference, current.InitialCondition);
        Assert.Equal(DesktopHydraulicProductionPolicy.M10FinalExactV9QualifiedCandidate, candidate.EffectivePolicy);
        Assert.Equal(DesktopSustainedGenerationPostMoistureEquilibriumCandidateInitialConditionFactory.Reference, candidate.InitialCondition);
        Assert.Equal(DesktopHydraulicProductionPolicy.ExplicitCommittedState, rollback.EffectivePolicy);
        Assert.Equal(DesktopSustainedGenerationInitialConditionFactory.Reference, rollback.InitialCondition);

        var engine = Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(
            DesktopHydraulicProductionPolicySelector.CreateFactory(candidate).CreateRuntimeEngine());
        Assert.Equal(TimeSpan.FromMilliseconds(10d), engine.FixedDeltaTime);

        var telemetryProbe = new DesktopHydraulicProductionTelemetryProbe();
        var tripSteps = 0;
        var breakerOpenSteps = 0;
        var nonFiniteSteps = 0;
        var minElectrical = double.PositiveInfinity;
        var maxElectrical = double.NegativeInfinity;
        var minPrimaryPump = double.PositiveInfinity;
        var maxPrimaryPump = double.NegativeInfinity;
        var minDrumLevel = double.PositiveInfinity;
        var maxDrumLevel = double.NegativeInfinity;
        var minGovernorOutput = double.PositiveInfinity;
        var maxGovernorOutput = double.NegativeInfinity;
        var maxMassClosure = 0d;
        var maxEnergyClosure = 0d;
        var maxBalanceMassRate = 0d;
        var maxBalancePower = 0d;
        var maxStageOwnershipResidual = 0d;
        var minimumMoistureDrain = double.PositiveInfinity;
        var maximumTransferMismatch = 0d;

        for (var step = 1; step <= HealthSteps; step++)
        {
            var snapshot = engine.Step(ControlRoomRunState.Running);
            telemetryProbe.Observe(engine);
            var fullPlant = engine.LatestCanonicalSnapshot.Control.ProtectedControl.FullPlant;
            var generator = Assert.Single(snapshot.Electrical.Generators);
            var drum = Assert.Single(fullPlant.IntegratedCycle.PrimaryCircuit.SteamDrums.Drums);
            var stage = Assert.Single(fullPlant.IntegratedCycle.TurbineExpansion.StageGroups);
            var speed = engine.LatestCanonicalSnapshot.Control.ProtectedControl.TurbineSecondary
                .ControlAndActuator.Controllers.GetDiagnostic("speed-control");

            var electrical = generator.ElectricalOutput.NumericValue ?? double.NaN;
            var primaryPump = fullPlant.IntegratedCycle.PrimaryCircuit.MainCirculation.TotalPumpMassFlowRate.KilogramsPerSecond;
            var drumLevel = drum.LiquidLevelFraction.Fraction;
            var governorOutput = speed.Output;
            var moistureDrain = stage.MoistureDrainMassFlowRate.KilogramsPerSecond;
            var transferMismatch = Math.Abs(
                stage.TotalTransferredMassFlowRate.KilogramsPerSecond - stage.CommandedMassFlowRate.KilogramsPerSecond);

            if (!double.IsFinite(electrical)
                || !double.IsFinite(primaryPump)
                || !double.IsFinite(drumLevel)
                || !double.IsFinite(governorOutput)
                || !double.IsFinite(moistureDrain)
                || !double.IsFinite(transferMismatch))
            {
                nonFiniteSteps++;
            }

            minElectrical = Math.Min(minElectrical, electrical);
            maxElectrical = Math.Max(maxElectrical, electrical);
            minPrimaryPump = Math.Min(minPrimaryPump, primaryPump);
            maxPrimaryPump = Math.Max(maxPrimaryPump, primaryPump);
            minDrumLevel = Math.Min(minDrumLevel, drumLevel);
            maxDrumLevel = Math.Max(maxDrumLevel, drumLevel);
            minGovernorOutput = Math.Min(minGovernorOutput, governorOutput);
            maxGovernorOutput = Math.Max(maxGovernorOutput, governorOutput);
            minimumMoistureDrain = Math.Min(minimumMoistureDrain, moistureDrain);
            maximumTransferMismatch = Math.Max(maximumTransferMismatch, transferMismatch);
            maxStageOwnershipResidual = Math.Max(maxStageOwnershipResidual, Math.Abs(stage.TurbineEnergyOwnershipResidual.Watts));

            maxMassClosure = Math.Max(maxMassClosure, Math.Abs(fullPlant.HeatBalance.MassClosureResidualKilograms));
            maxEnergyClosure = Math.Max(maxEnergyClosure, Math.Abs(fullPlant.HeatBalance.FullEnergyPathClosureResidualJoules));
            maxBalanceMassRate = Math.Max(maxBalanceMassRate, Math.Abs(fullPlant.IntegratedCycle.ThermofluidAudit.BalanceMassRateResidualKilogramsPerSecond));
            maxBalancePower = Math.Max(maxBalancePower, Math.Abs(fullPlant.IntegratedCycle.ThermofluidAudit.BalancePowerResidualWatts));

            if (snapshot.AnyTripActive)
            {
                tripSteps++;
            }
            if (!generator.BreakerClosed)
            {
                breakerOpenSteps++;
            }
        }

        var telemetry = telemetryProbe.Snapshot();
        Assert.Equal(0, tripSteps);
        Assert.Equal(0, breakerOpenSteps);
        Assert.Equal(0, nonFiniteSteps);
        Assert.Equal(0, telemetry.RollbackSteps);
        Assert.Equal(0, telemetry.FallbackCommitViolations);
        Assert.Equal(0, telemetry.UnsafeCommitViolations);
        Assert.Equal(0, telemetry.UntargetedBranchDisagreementSteps);

        Assert.InRange(minElectrical, 4.99d, 5.01d);
        Assert.InRange(maxElectrical, 4.99d, 5.01d);
        Assert.InRange(minPrimaryPump, 99.9d, 100.1d);
        Assert.InRange(maxPrimaryPump, 99.9d, 100.1d);
        Assert.InRange(minDrumLevel, 0.49d, 0.51d);
        Assert.InRange(maxDrumLevel, 0.49d, 0.51d);
        Assert.InRange(minGovernorOutput, 29.27d, 29.30d);
        Assert.InRange(maxGovernorOutput, 29.27d, 29.30d);
        Assert.True(minimumMoistureDrain > 0d);
        Assert.True(maximumTransferMismatch <= 1e-8d);
        Assert.True(maxStageOwnershipResidual <= 1e-3d);
        Assert.True(maxMassClosure <= 1e-6d);
        Assert.True(maxEnergyClosure <= 1e-2d);
        Assert.True(maxBalanceMassRate <= 1e-8d);
        Assert.True(maxBalancePower <= 1e-3d);

        var selectorFingerprint = DeterminismFingerprint(useSelector: true);
        var directFingerprint = DeterminismFingerprint(useSelector: false);
        Assert.Equal(directFingerprint, selectorFingerprint);

        WriteArtifacts(
            current,
            candidate,
            rollback,
            telemetry,
            tripSteps,
            breakerOpenSteps,
            minElectrical,
            maxElectrical,
            minPrimaryPump,
            maxPrimaryPump,
            minDrumLevel,
            maxDrumLevel,
            minGovernorOutput,
            maxGovernorOutput,
            minimumMoistureDrain,
            maximumTransferMismatch,
            maxStageOwnershipResidual,
            maxMassClosure,
            maxEnergyClosure,
            maxBalanceMassRate,
            maxBalancePower,
            selectorFingerprint);
    }

    private static string DeterminismFingerprint(bool useSelector)
    {
        var engine = useSelector
            ? Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(
                DesktopHydraulicProductionPolicySelector.CreateFactory(
                    DesktopHydraulicProductionPolicySelector.Resolve(
                        DesktopHydraulicProductionPolicySelector.M10FinalQualifiedCandidatePolicy)).CreateRuntimeEngine())
            : Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(
                new DesktopSustainedGenerationPostMoistureEquilibriumCandidateInitialConditionFactory().CreateRuntimeEngine());

        var builder = new StringBuilder();
        for (var step = 1; step <= DeterminismSteps; step++)
        {
            var snapshot = engine.Step(ControlRoomRunState.Running);
            builder.Append(FormattableString.Invariant(
                $"{step}:{ControlRoomSnapshotFingerprint.Compute(snapshot)}||"));
        }
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString())));
    }

    private static void WriteArtifacts(
        DesktopHydraulicProductionPolicyDecision current,
        DesktopHydraulicProductionPolicyDecision candidate,
        DesktopHydraulicProductionPolicyDecision rollback,
        FourNodeProductionActivationTelemetrySnapshot telemetry,
        int tripSteps,
        int breakerOpenSteps,
        double minElectrical,
        double maxElectrical,
        double minPrimaryPump,
        double maxPrimaryPump,
        double minDrumLevel,
        double maxDrumLevel,
        double minGovernorOutput,
        double maxGovernorOutput,
        double minimumMoistureDrain,
        double maximumTransferMismatch,
        double maxStageOwnershipResidual,
        double maxMassClosure,
        double maxEnergyClosure,
        double maxBalanceMassRate,
        double maxBalancePower,
        string deterministicFingerprint)
    {
        var directory = ReportDirectory();
        File.WriteAllLines(Path.Combine(directory, "01-v9-production-activation-candidate.summary.txt"), new[]
        {
            "scope=M10 Final exact-v9 qualified production-activation candidate wiring; Diagnostic 11 Hotfix 2 returned exact-v9 equilibrium qualification; exact-v4 remains authoritative default; no replacement-long authorization;",
            $"authoritative-default={Ref(current.InitialCondition)}; candidate={Ref(candidate.InitialCondition)}; rollback={Ref(rollback.InitialCondition)};",
            FormattableString.Invariant($"health-steps={HealthSteps}; trip-steps={tripSteps}; breaker-open-steps={breakerOpenSteps}; electrical-range-mwe={minElectrical:G17}..{maxElectrical:G17}; primary-pump-range-kg-s={minPrimaryPump:G17}..{maxPrimaryPump:G17}; drum-level-range={minDrumLevel:G17}..{maxDrumLevel:G17}; governor-output-range-percent={minGovernorOutput:G17}..{maxGovernorOutput:G17};"),
            FormattableString.Invariant($"minimum-moisture-drain-kg-s={minimumMoistureDrain:G17}; max-commanded-transfer-mismatch-kg-s={maximumTransferMismatch:G17}; max-stage-energy-ownership-residual-w={maxStageOwnershipResidual:G17};"),
            FormattableString.Invariant($"max-network-mass-closure-kg={maxMassClosure:G17}; max-network-energy-closure-j={maxEnergyClosure:G17}; max-network-balance-mass-rate-kg-s={maxBalanceMassRate:G17}; max-network-balance-power-w={maxBalancePower:G17};"),
            FormattableString.Invariant($"corrected-triggered={telemetry.TriggeredSteps}; corrected-committed={telemetry.CorrectedCommittedSteps}; rollbacks={telemetry.RollbackSteps}; fallback-commit-violations={telemetry.FallbackCommitViolations}; unsafe-commits={telemetry.UnsafeCommitViolations}; untargeted-disagreements={telemetry.UntargetedBranchDisagreementSteps};"),
            $"determinism-steps={DeterminismSteps}; selector-equals-direct-factory=True; fingerprint={deterministicFingerprint};",
            "exact-v9-qualified=True; exact-v9-policy-opt-in=True; exact-v4-authoritative-default-preserved=True; exact-v2-fail-closed-kill-preserved=True; exact-v3-v4-historical-identities-reinterpreted=False; production-activation=False; replacement-long-authorized=False;",
            "next-step=only after this candidate gate plus cumulative current-evidence and Diagnostic-11 requalification pass may a separate activation-decision candidate switch the authoritative default to exact-v9;",
        }, Utf8WithoutBom);

        File.WriteAllLines(Path.Combine(directory, "02-selector-matrix.csv"), new[]
        {
            "role,policy,initial_condition,authoritative",
            $"current,{current.EffectivePolicy},{Ref(current.InitialCondition)},true",
            $"qualified-candidate,{candidate.EffectivePolicy},{Ref(candidate.InitialCondition)},false",
            $"fail-closed-kill,{rollback.EffectivePolicy},{Ref(rollback.InitialCondition)},false",
        }, Utf8WithoutBom);
    }


    private static string Ref(InitialConditionReference reference)
        => $"{reference.InitialConditionId}@{reference.Version}";

    private static void RequireOptIn()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable(OptInEnvironmentVariable), "1", StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Set {OptInEnvironmentVariable}=1 to run the M10 Final exact-v9 production-activation candidate gate.");
        }
        if (!string.Equals(Environment.GetEnvironmentVariable(PrerequisitesEnvironmentVariable), "1", StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Set {PrerequisitesEnvironmentVariable}=1 only after current-evidence and Diagnostic-11 prerequisite gates pass.");
        }
    }

    private static string ReportDirectory()
        => Path.Combine(FindRepositoryRoot(), "artifacts", "m10-final-v9-production-activation-candidate");

    private static void ResetReportDirectory()
    {
        var directory = ReportDirectory();
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }
        Directory.CreateDirectory(directory);
        File.WriteAllText(
            Path.Combine(directory, "00-progress.txt"),
            $"M10 FINAL EXACT-V9 PRODUCTION ACTIVATION CANDIDATE STARTED{Environment.NewLine}",
            Utf8WithoutBom);
    }

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
}
