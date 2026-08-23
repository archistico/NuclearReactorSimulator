using System.Security.Cryptography;
using System.Text;
using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.ControlRoom.MissionPerformance;
using NuclearReactorSimulator.Application.Scenarios;
using NuclearReactorSimulator.Application.Scenarios.Challenges.Packs;
using NuclearReactorSimulator.Application.Scenarios.Recording;
using NuclearReactorSimulator.Application.Scenarios.Training;
using NuclearReactorSimulator.Simulation.Plant;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay;

/// <summary>
/// M10 Final authoritative exact-v9 activation decision. Diagnostic 11 Hotfix 2 and the separate opt-in activation
/// candidate already qualified the operating point and deployment path. This gate verifies only the default switch,
/// historical exact-version retention, production scenario/mission rebinding, short authoritative-path health,
/// fail-closed rollback and deterministic equivalence to the qualified direct exact-v9 factory.
/// </summary>
public sealed class M10FinalExactV9ProductionActivationDecisionTests
{
    private const string OptInEnvironmentVariable = "NRS_M10_FINAL_V9_ACTIVATION_DECISION";
    private const string PrerequisitesEnvironmentVariable = "NRS_M10_FINAL_V9_ACTIVATION_PREREQUISITES_PASSED";
    private const int HealthSteps = 12_000;
    private const int MissionSteps = 1_200;
    private const int DeterminismSteps = 128;
    private static readonly UTF8Encoding Utf8WithoutBom = new(encoderShouldEmitUTF8Identifier: false);

    [Fact]
    public void AuthoritativeExactV9_SwitchesDefaultPreservesHistoricalVersionsAndRebindsProductionMissionV3()
    {
        Assert.Equal(
            DesktopHydraulicProductionPolicy.M10FinalExactV9QualifiedCandidate,
            DesktopHydraulicProductionPolicySelector.AuthoritativeDefaultPolicy);
        Assert.Equal(
            DesktopHydraulicProductionPolicy.I5RepairedFourNodeCorrectedCommit,
            DesktopHydraulicProductionPolicySelector.I5RepairedProductionPolicy);
        Assert.Equal(
            DesktopHydraulicProductionPolicy.ExplicitCommittedState,
            DesktopHydraulicProductionPolicySelector.ExplicitRollbackPolicy);

        var current = DesktopHydraulicProductionPolicySelector.Resolve(
            DesktopHydraulicProductionPolicySelector.AuthoritativeDefaultPolicy);
        var historicalV4 = DesktopHydraulicProductionPolicySelector.Resolve(
            DesktopHydraulicProductionPolicySelector.I5RepairedProductionPolicy);
        var historicalV3 = DesktopHydraulicProductionPolicySelector.Resolve(
            DesktopHydraulicProductionPolicySelector.H29ActivationCandidatePolicy);
        var rollback = DesktopHydraulicProductionPolicySelector.Resolve(
            DesktopHydraulicProductionPolicySelector.AuthoritativeDefaultPolicy,
            explicitKillRequested: true);

        Assert.Equal(DesktopSustainedGenerationPostMoistureEquilibriumCandidateInitialConditionFactory.Reference, current.InitialCondition);
        Assert.Equal(9, current.InitialCondition.Version);
        Assert.Equal(DesktopSustainedGenerationI5RepairedActivationCandidateInitialConditionFactory.Reference, historicalV4.InitialCondition);
        Assert.Equal(4, historicalV4.InitialCondition.Version);
        Assert.Equal(DesktopSustainedGenerationH29ActivationCandidateInitialConditionFactory.Reference, historicalV3.InitialCondition);
        Assert.Equal(3, historicalV3.InitialCondition.Version);
        Assert.Equal(DesktopSustainedGenerationInitialConditionFactory.Reference, rollback.InitialCondition);
        Assert.Equal(2, rollback.InitialCondition.Version);
        Assert.True(rollback.ExplicitKillApplied);

        Assert.Equal(
            DesktopIntegratedOperationsProductionProgram.M10FinalExactV9ProductionScenario,
            DesktopIntegratedOperationsProductionProgram.Scenario);
        Assert.Equal(
            "integrated-normal-operations-training-m10-final-v9-production",
            DesktopIntegratedOperationsProductionProgram.Scenario.ScenarioId);
        Assert.Equal(
            DesktopSustainedGenerationPostMoistureEquilibriumCandidateInitialConditionFactory.Reference,
            DesktopIntegratedOperationsProductionProgram.Scenario.InitialCondition);

        Assert.Equal(
            "integrated-normal-operations-training-m10-final-v9-activation-candidate",
            DesktopIntegratedOperationsM10FinalV9ActivationCandidateProgram.Scenario.ScenarioId);
        Assert.Equal(
            DesktopSustainedGenerationPostMoistureEquilibriumCandidateInitialConditionFactory.Reference,
            DesktopIntegratedOperationsM10FinalV9ActivationCandidateProgram.Scenario.InitialCondition);
        Assert.NotEqual(
            DesktopIntegratedOperationsM10FinalV9ActivationCandidateProgram.Scenario.ScenarioId,
            DesktopIntegratedOperationsProductionProgram.Scenario.ScenarioId);

        var historicalPackV1 = InitialOperationalChallengePack.BoundedDemandFollowing;
        var historicalPackV2 = ProductionOperationalChallengePack.BoundedDemandFollowingV2;
        var productionPackV3 = ProductionOperationalChallengePack.BoundedDemandFollowing;

        Assert.Equal("bounded-demand-following-5-10-5@1", historicalPackV1.ExactId);
        Assert.Equal("bounded-demand-following-5-10-5@2", historicalPackV2.ExactId);
        Assert.Equal("bounded-demand-following-5-10-5@3", productionPackV3.ExactId);
        Assert.Equal(new InitialConditionReference("integrated-operations-desktop-stable", 4), historicalPackV2.Scenario.InitialCondition);
        Assert.Equal(new InitialConditionReference("integrated-operations-desktop-stable", 9), productionPackV3.Scenario.InitialCondition);
        Assert.Equal(DesktopIntegratedOperationsProductionProgram.RepairedProductionScenario, historicalPackV2.Scenario);
        Assert.Equal(DesktopIntegratedOperationsProductionProgram.M10FinalExactV9ProductionScenario, productionPackV3.Scenario);
        Assert.Equal(historicalPackV2.Challenge.ObjectiveId, productionPackV3.Challenge.ObjectiveId);
        Assert.Equal(historicalPackV2.Challenge.ExternalDemandProfile?.ExactId, productionPackV3.Challenge.ExternalDemandProfile?.ExactId);
        Assert.Same(historicalPackV2.ScoringPolicy, productionPackV3.ScoringPolicy);
        Assert.Same(historicalPackV2.ConditionEvaluator, productionPackV3.ConditionEvaluator);
        Assert.Equal(historicalPackV2.ScoreEvidenceBindings, productionPackV3.ScoreEvidenceBindings);
    }

    [Fact(Explicit = true)]
    [Trait("Category", "M10FinalExactV9ProductionActivationDecision")]
    public void AuthoritativeExactV9_DefaultAndMissionPathsRemainHealthyConservativeDeterministicAndFailClosed()
    {
        RequireOptIn();
        ResetReportDirectory();

        var current = DesktopHydraulicProductionPolicySelector.Resolve(
            DesktopHydraulicProductionPolicySelector.AuthoritativeDefaultPolicy);
        var historicalV4 = DesktopHydraulicProductionPolicySelector.Resolve(
            DesktopHydraulicProductionPolicySelector.I5RepairedProductionPolicy);
        var rollback = DesktopHydraulicProductionPolicySelector.Resolve(
            DesktopHydraulicProductionPolicySelector.AuthoritativeDefaultPolicy,
            explicitKillRequested: true);

        Assert.Equal(DesktopHydraulicProductionPolicy.M10FinalExactV9QualifiedCandidate, current.EffectivePolicy);
        Assert.Equal(DesktopSustainedGenerationPostMoistureEquilibriumCandidateInitialConditionFactory.Reference, current.InitialCondition);
        Assert.Equal(DesktopHydraulicProductionPolicy.I5RepairedFourNodeCorrectedCommit, historicalV4.EffectivePolicy);
        Assert.Equal(DesktopSustainedGenerationI5RepairedActivationCandidateInitialConditionFactory.Reference, historicalV4.InitialCondition);
        Assert.Equal(DesktopHydraulicProductionPolicy.ExplicitCommittedState, rollback.EffectivePolicy);
        Assert.Equal(DesktopSustainedGenerationInitialConditionFactory.Reference, rollback.InitialCondition);

        var engine = Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(
            DesktopHydraulicProductionPolicySelector.CreateFactory(current).CreateRuntimeEngine());
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
        var minimumMoistureDrain = double.PositiveInfinity;
        var maximumTransferMismatch = 0d;
        var maxStageOwnershipResidual = 0d;
        var maxMassClosure = 0d;
        var maxEnergyClosure = 0d;
        var maxBalanceMassRate = 0d;
        var maxBalancePower = 0d;

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
        Assert.Equal("7880AD580179B936C584EB0055BE663E0A1CFA65C5191B0DB8A7F3C514DB5418", selectorFingerprint);

        var mission = RunCurrentProductionMission();
        Assert.Equal(0, mission.TripSteps);
        Assert.Equal(0, mission.BreakerOpenSteps);
        Assert.Equal("bounded-demand-following-5-10-5@3", mission.PackExactId);
        Assert.Equal("integrated-normal-operations-training-m10-final-v9-production", mission.ScenarioId);

        WriteArtifacts(
            current,
            historicalV4,
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
            selectorFingerprint,
            mission);
    }

    private static MissionResult RunCurrentProductionMission()
    {
        var pack = ProductionOperationalChallengePack.BoundedDemandFollowing;
        var registry = new VersionedInitialConditionRegistry(new IVersionedInitialConditionFactory[]
        {
            new DesktopSustainedGenerationPostMoistureEquilibriumCandidateInitialConditionFactory(),
        });
        var session = new ScenarioSessionFactory(registry).Load(pack.Scenario);
        using var source = new MissionPerformanceLiveSnapshotSource(session, pack, TrainingGuidanceMode.Guided);

        session.CommandDispatcher.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.Run));
        var tripSteps = 0;
        var breakerOpenSteps = 0;
        var executed = 0;
        for (var batch = 0; batch < 12; batch++)
        {
            executed += session.Coordinator.AdvanceRunning(100, publicationStride: 100).ExecutedStepCount;
            if (session.Coordinator.Current.AnyTripActive)
            {
                tripSteps++;
            }
            if (!Assert.Single(session.Coordinator.Current.Electrical.Generators).BreakerClosed)
            {
                breakerOpenSteps++;
            }
        }

        Assert.Equal(MissionSteps, executed);
        Assert.Equal(MissionSteps, session.Coordinator.Current.LogicalStep);
        Assert.Equal(pack.ExactId, source.Current.PackExactId);
        Assert.Equal(pack.Scenario.ScenarioId, source.Current.ScenarioId);
        return new MissionResult(
            pack.ExactId,
            pack.Scenario.ScenarioId,
            tripSteps,
            breakerOpenSteps,
            source.Current.Score.FinalScore);
    }

    private static string DeterminismFingerprint(bool useSelector)
    {
        var engine = useSelector
            ? Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(
                DesktopHydraulicProductionPolicySelector.CreateFactory(
                    DesktopHydraulicProductionPolicySelector.Resolve(
                        DesktopHydraulicProductionPolicySelector.AuthoritativeDefaultPolicy)).CreateRuntimeEngine())
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
        DesktopHydraulicProductionPolicyDecision historicalV4,
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
        string deterministicFingerprint,
        MissionResult mission)
    {
        var directory = ReportDirectory();
        File.WriteAllLines(Path.Combine(directory, "01-v9-production-activation-decision.summary.txt"), new[]
        {
            "scope=M10 Final exact-v9 authoritative production activation decision; exact-v9 qualified by Diagnostic 11 Hotfix 2 and the separate opt-in activation candidate; exact-v4 is historical/replayable; exact-v2 remains fail-closed; replacement long is still not authorized by this gate;",
            $"authoritative-default={Ref(current.InitialCondition)}; historical-v4={Ref(historicalV4.InitialCondition)}; rollback={Ref(rollback.InitialCondition)};",
            FormattableString.Invariant($"health-steps={HealthSteps}; trip-steps={tripSteps}; breaker-open-steps={breakerOpenSteps}; electrical-range-mwe={minElectrical:G17}..{maxElectrical:G17}; primary-pump-range-kg-s={minPrimaryPump:G17}..{maxPrimaryPump:G17}; drum-level-range={minDrumLevel:G17}..{maxDrumLevel:G17}; governor-output-range-percent={minGovernorOutput:G17}..{maxGovernorOutput:G17};"),
            FormattableString.Invariant($"minimum-moisture-drain-kg-s={minimumMoistureDrain:G17}; max-commanded-transfer-mismatch-kg-s={maximumTransferMismatch:G17}; max-stage-energy-ownership-residual-w={maxStageOwnershipResidual:G17};"),
            FormattableString.Invariant($"max-network-mass-closure-kg={maxMassClosure:G17}; max-network-energy-closure-j={maxEnergyClosure:G17}; max-network-balance-mass-rate-kg-s={maxBalanceMassRate:G17}; max-network-balance-power-w={maxBalancePower:G17};"),
            FormattableString.Invariant($"corrected-triggered={telemetry.TriggeredSteps}; corrected-committed={telemetry.CorrectedCommittedSteps}; rollbacks={telemetry.RollbackSteps}; fallback-commit-violations={telemetry.FallbackCommitViolations}; unsafe-commits={telemetry.UnsafeCommitViolations}; untargeted-disagreements={telemetry.UntargetedBranchDisagreementSteps};"),
            $"determinism-steps={DeterminismSteps}; selector-equals-direct-factory=True; fingerprint={deterministicFingerprint};",
            FormattableString.Invariant($"mission-steps={MissionSteps}; mission-pack={mission.PackExactId}; mission-scenario={mission.ScenarioId}; mission-trip-steps={mission.TripSteps}; mission-breaker-open-steps={mission.BreakerOpenSteps}; mission-final-score={mission.FinalScore:G17};"),
            "exact-v9-authoritative=True; exact-v4-historical-retained=True; exact-v3-historical-retained=True; exact-v2-fail-closed-kill-preserved=True; production-mission-v3-authoritative=True; production-mission-v2-historical-retained=True; historical-identities-reinterpreted=False; production-activation=True; replacement-long-authorized=False;",
            "next-step=after this activation decision gate passes, freeze a new exact-v9 production baseline manifest and authorize only the redesigned replacement long campaign; do not reuse the failed exact-v4 long manifest;",
        }, Utf8WithoutBom);

        File.WriteAllLines(Path.Combine(directory, "02-selector-matrix.csv"), new[]
        {
            "role,policy,initial_condition,scenario,authoritative",
            $"authoritative,{current.EffectivePolicy},{Ref(current.InitialCondition)},{DesktopIntegratedOperationsProductionProgram.Scenario.ScenarioId},true",
            $"historical-i5,{historicalV4.EffectivePolicy},{Ref(historicalV4.InitialCondition)},{DesktopIntegratedOperationsProductionProgram.RepairedProductionScenario.ScenarioId},false",
            $"fail-closed-kill,{rollback.EffectivePolicy},{Ref(rollback.InitialCondition)},{DesktopIntegratedOperationsProgram.Scenario.ScenarioId},false",
        }, Utf8WithoutBom);

        File.WriteAllLines(Path.Combine(directory, "03-mission-pack-matrix.csv"), new[]
        {
            "role,pack_exact_id,scenario,initial_condition,authoritative",
            $"historical-v1,{InitialOperationalChallengePack.BoundedDemandFollowing.ExactId},{InitialOperationalChallengePack.BoundedDemandFollowing.Scenario.ScenarioId},{Ref(InitialOperationalChallengePack.BoundedDemandFollowing.Scenario.InitialCondition)},false",
            $"historical-v2,{ProductionOperationalChallengePack.BoundedDemandFollowingV2.ExactId},{ProductionOperationalChallengePack.BoundedDemandFollowingV2.Scenario.ScenarioId},{Ref(ProductionOperationalChallengePack.BoundedDemandFollowingV2.Scenario.InitialCondition)},false",
            $"authoritative-v3,{ProductionOperationalChallengePack.BoundedDemandFollowing.ExactId},{ProductionOperationalChallengePack.BoundedDemandFollowing.Scenario.ScenarioId},{Ref(ProductionOperationalChallengePack.BoundedDemandFollowing.Scenario.InitialCondition)},true",
        }, Utf8WithoutBom);
    }

    private static string Ref(InitialConditionReference reference)
        => $"{reference.InitialConditionId}@{reference.Version}";

    private static void RequireOptIn()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable(OptInEnvironmentVariable), "1", StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Set {OptInEnvironmentVariable}=1 to run the M10 Final exact-v9 authoritative activation decision gate.");
        }
        if (!string.Equals(Environment.GetEnvironmentVariable(PrerequisitesEnvironmentVariable), "1", StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Set {PrerequisitesEnvironmentVariable}=1 only after the exact-v9 qualification and opt-in activation candidate gates have passed.");
        }
    }

    private static string ReportDirectory()
        => Path.Combine(FindRepositoryRoot(), "artifacts", "m10-final-v9-production-activation-decision");

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
            $"M10 FINAL EXACT-V9 PRODUCTION ACTIVATION DECISION STARTED{Environment.NewLine}",
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

    private sealed record MissionResult(
        string PackExactId,
        string ScenarioId,
        int TripSteps,
        int BreakerOpenSteps,
        decimal? FinalScore);
}
