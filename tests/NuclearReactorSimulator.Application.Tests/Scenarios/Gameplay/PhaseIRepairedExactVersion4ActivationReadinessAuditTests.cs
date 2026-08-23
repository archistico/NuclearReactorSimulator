using System.Globalization;
using System.Text;
using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios;
using NuclearReactorSimulator.Application.Scenarios.Training;
using NuclearReactorSimulator.Domain.Plant;
using NuclearReactorSimulator.Simulation.Plant;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay;

/// <summary>
/// I.5 REV1 Hotfix 15 narrow exact-v4 activation-readiness audit. Exact v4 is registered as a distinct repaired
/// candidate, while the production selector deliberately remains on exact v3 until the final activation/closure step.
/// </summary>
public sealed class PhaseIRepairedExactVersion4ActivationReadinessAuditTests
{
    private static readonly TimeSpan Step = TimeSpan.FromMilliseconds(10d);
    private static readonly UTF8Encoding Utf8WithoutBom = new(encoderShouldEmitUTF8Identifier: false);
    private const int StepsPerSecond = 100;
    private const int WarmupSteps = 10 * StepsPerSecond;
    private const int LoadSegmentSteps = 30 * StepsPerSecond;

    [Fact(Explicit = true)]
    [Trait("Category", "PhaseIRepairedExactVersion4ActivationReadinessAudit")]
    public void ExactVersion4_IsDistinctReplayableAndCompletesFrozenGapJourneyWithoutSwitchingProductionDefault()
    {
        ResetDirectory(ReportDirectory());

        var v1 = new DesktopIntegratedOperationsInitialConditionFactory();
        var v2 = new DesktopSustainedGenerationInitialConditionFactory();
        var v3 = new DesktopSustainedGenerationH29ActivationCandidateInitialConditionFactory();
        var v4 = new DesktopSustainedGenerationI5RepairedActivationCandidateInitialConditionFactory();
        var registry = new VersionedInitialConditionRegistry(new IVersionedInitialConditionFactory[] { v1, v2, v3, v4 });

        Assert.Same(v1, registry.Resolve(new InitialConditionReference("integrated-operations-desktop-stable", 1)));
        Assert.Same(v2, registry.Resolve(new InitialConditionReference("integrated-operations-desktop-stable", 2)));
        Assert.Same(v3, registry.Resolve(new InitialConditionReference("integrated-operations-desktop-stable", 3)));
        Assert.Same(v4, registry.Resolve(new InitialConditionReference("integrated-operations-desktop-stable", 4)));

        var currentProduction = DesktopHydraulicProductionPolicySelector.Resolve(
            DesktopHydraulicProductionPolicySelector.H29ActivationCandidatePolicy);
        var explicitRollback = DesktopHydraulicProductionPolicySelector.Resolve(
            DesktopHydraulicProductionPolicySelector.H29ActivationCandidatePolicy,
            explicitKillRequested: true);

        Assert.Equal(DesktopSustainedGenerationH29ActivationCandidateInitialConditionFactory.Reference, currentProduction.InitialCondition);
        Assert.Equal(DesktopSustainedGenerationInitialConditionFactory.Reference, explicitRollback.InitialCondition);
        Assert.Equal(DesktopHydraulicProductionPolicy.H29FourNodeCorrectedCommitCandidate, currentProduction.EffectivePolicy);
        Assert.Equal(DesktopHydraulicProductionPolicy.ExplicitCommittedState, explicitRollback.EffectivePolicy);

        Assert.Equal(
            new InitialConditionReference("integrated-operations-desktop-stable", 4),
            DesktopSustainedGenerationI5RepairedActivationCandidateInitialConditionFactory.Reference);
        Assert.NotEqual(v3.Descriptor.Reference, v4.Descriptor.Reference);
        Assert.Contains("CorrelationConsistentInverseDomain", v4.Descriptor.Description, StringComparison.Ordinal);
        Assert.True(DesktopIntegratedOperationsProductionProgram.IsDesktopTrainingScenario(
            DesktopIntegratedOperationsI5RepairedActivationCandidateProgram.Scenario.ScenarioId));
        Assert.Equal(
            DesktopIntegratedOperationsI5RepairedActivationCandidateProgram.Scenario.ScenarioId,
            DesktopIntegratedOperationsProductionProgram.ResolveTrainingPlan(
                DesktopIntegratedOperationsI5RepairedActivationCandidateProgram.Scenario.ScenarioId).ScenarioId);

        var engine = Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(v4.CreateRuntimeEngine());
        Assert.Equal(Step, engine.FixedDeltaTime);
        Assert.Equal(
            HydraulicNumericalCouplingMode.FourNodeBranchContinuityCorrectedCommitOptIn,
            CurrentHydraulics(engine).Mode);

        var checkpoints = new List<Checkpoint>();
        var counters = new Counters();
        var generator = Assert.Single(engine.CreatePresentationSnapshot(ControlRoomRunState.Paused).Electrical.Generators);

        Advance(engine, "steady", WarmupSteps, checkpoints, counters);
        engine.QueueOperatorCommand(new ControlRoomCommand(
            ControlRoomCommandKind.GeneratorLoadRaise,
            generator.GeneratorId,
            ControlRoomCommandTargetKind.Generator));
        Advance(engine, "load-raise-hold", LoadSegmentSteps, checkpoints, counters);
        engine.QueueOperatorCommand(new ControlRoomCommand(
            ControlRoomCommandKind.GeneratorLoadLower,
            generator.GeneratorId,
            ControlRoomCommandTargetKind.Generator));
        Advance(engine, "load-lower-hold", LoadSegmentSteps, checkpoints, counters);

        Assert.Equal(WarmupSteps + (2 * LoadSegmentSteps), counters.SuccessfulSteps);
        Assert.Equal(counters.Triggers, counters.Commits);
        Assert.Equal(0, counters.Rollbacks);
        Assert.Equal(0, counters.UnsafeCommits);
        Assert.Equal(0, counters.FallbackCommitViolations);
        Assert.Equal(0, counters.UntargetedDisagreements);

        WriteArtifacts(checkpoints, counters, currentProduction, explicitRollback);
    }

    private static void Advance(
        IntegratedAutomaticOperationRuntimeEngine engine,
        string segment,
        int count,
        List<Checkpoint> checkpoints,
        Counters counters)
    {
        for (var segmentStep = 1; segmentStep <= count; segmentStep++)
        {
            var presentation = engine.Step(ControlRoomRunState.Running);
            counters.SuccessfulSteps++;
            if (presentation.AnyTripActive)
            {
                counters.TripSteps++;
            }

            var telemetry = Assert.IsType<FourNodeBranchContinuityIntegrationTelemetry>(
                CurrentHydraulics(engine).FourNodeBranchContinuity);
            if (telemetry.TriggerObserved)
            {
                counters.Triggers++;
            }
            if (telemetry.CorrectedCandidateCommitted)
            {
                counters.Commits++;
            }
            if (telemetry.RollbackRequired)
            {
                counters.Rollbacks++;
            }
            if ((!telemetry.ShadowCorrectedCandidateEligible || telemetry.RollbackRequired)
                && telemetry.CorrectedCandidateCommitted)
            {
                counters.FallbackCommitViolations++;
            }
            if (telemetry.CorrectedCandidateCommitted && !CommitIsQualified(telemetry))
            {
                counters.UnsafeCommits++;
            }
            if (telemetry.UntargetedBranchDisagreementDetected)
            {
                counters.UntargetedDisagreements++;
            }

            if (segmentStep % StepsPerSecond != 0 && segmentStep != count)
            {
                continue;
            }

            var exhaust = engine.CurrentState.PlantState.PlantState.GetFluidNode("exhaust");
            var generator = Assert.Single(presentation.Electrical.Generators);
            var rotor = Assert.Single(presentation.TurbineSecondary.Rotors);
            checkpoints.Add(new Checkpoint(
                engine.LogicalStep,
                segment,
                segmentStep,
                exhaust.Phase.ToString(),
                exhaust.Volume.CubicMetres / exhaust.Mass.Kilograms,
                exhaust.SpecificInternalEnergy.JoulesPerKilogram,
                exhaust.Pressure.Kilopascals,
                exhaust.Temperature.DegreesCelsius,
                generator.RequestedElectricalPower.NumericValue ?? double.NaN,
                generator.ElectricalOutput.NumericValue ?? double.NaN,
                rotor.ShaftPower.NumericValue ?? double.NaN,
                rotor.Speed.NumericValue ?? double.NaN,
                telemetry.TriggerObserved,
                telemetry.CorrectedCandidateCommitted,
                telemetry.RollbackRequired));
        }
    }

    private static bool CommitIsQualified(FourNodeBranchContinuityIntegrationTelemetry telemetry)
        => telemetry.ShadowCorrectedCandidateEligible
            && telemetry.CorrectedCommitAuthorized
            && !telemetry.RollbackRequired
            && !telemetry.UntargetedBranchDisagreementDetected
            && telemetry.ShadowCorrectionEvaluated
            && telemetry.ShadowConverged
            && !telemetry.ShadowLineSearchExhausted
            && telemetry.ShadowMaximumRelativePressureResidual <= 1e-5d
            && telemetry.ShadowMaximumAbsoluteFlowResidualKilogramsPerSecond <= 1e-2d
            && telemetry.ShadowMassClosureKilogramsPerSecond <= 1e-8d
            && telemetry.ShadowEnergyOwnershipResidualWatts <= 1e-3d
            && telemetry.ProposedAuthority == FourNodeBranchContinuityProposedAuthority.CorrectedCandidate
            && telemetry.Reason == FourNodeBranchContinuityActivationReason.QualifiedTriggeredCorrection
            && telemetry.CorrectedCommitReason == FourNodeBranchContinuityCorrectedCommitReason.QualifiedH20Authority;

    private static PlantNetworkHydraulicNumericalSnapshot CurrentHydraulics(IntegratedAutomaticOperationRuntimeEngine engine)
        => engine.LatestCanonicalSnapshot.Control.ProtectedControl.FullPlant.IntegratedCycle.PrimaryCircuit.HydraulicNumerics;

    private static void WriteArtifacts(
        IReadOnlyList<Checkpoint> checkpoints,
        Counters counters,
        DesktopHydraulicProductionPolicyDecision currentProduction,
        DesktopHydraulicProductionPolicyDecision explicitRollback)
    {
        var directory = ReportDirectory();
        var csv = new List<string>
        {
            "logical_step,segment,segment_step,exhaust_phase,exhaust_v_m3_kg,exhaust_u_j_kg,exhaust_kpa,exhaust_c,request_mwe,gross_mwe,shaft_mw,rotor_rpm,trigger,commit,rollback",
        };
        csv.AddRange(checkpoints.Select(row => string.Join(",",
            row.LogicalStep,
            row.Segment,
            row.SegmentStep,
            row.ExhaustPhase,
            F(row.ExhaustSpecificVolume),
            F(row.ExhaustSpecificInternalEnergy),
            F(row.ExhaustPressureKilopascals),
            F(row.ExhaustTemperatureCelsius),
            F(row.RequestMegawatts),
            F(row.GrossMegawatts),
            F(row.ShaftMegawatts),
            F(row.RotorRpm),
            row.TriggerObserved,
            row.CorrectedCandidateCommitted,
            row.RollbackRequired)));
        File.WriteAllLines(Path.Combine(directory, "02-i5-repaired-v4-activation-readiness-checkpoints.csv"), csv, Utf8WithoutBom);

        var summary = new[]
        {
            "=== 01-i5-repaired-exact-v4-activation-readiness ===",
            "scope=new exact desktop @4 registration/readiness only; @2/@3 remain immutable; production selector deliberately remains exact @3 until final activation/closure candidate;",
            "exact-v1=integrated-operations-desktop-stable@1; exact-v2=integrated-operations-desktop-stable@2; exact-v3=integrated-operations-desktop-stable@3; exact-v4=integrated-operations-desktop-stable@4; all-four-resolve=True;",
            "v4-composition=CorrelationConsistentInverseDomain|FourNodeBranchContinuityCorrectedCommitOptIn|10ms; historical-v3-reinterpreted=False; historical-v2-reinterpreted=False;",
            FormattableString.Invariant($"current-production-before-final-activation={currentProduction.InitialCondition.InitialConditionId}@{currentProduction.InitialCondition.Version}|{currentProduction.EffectivePolicy}; explicit-rollback={explicitRollback.InitialCondition.InitialConditionId}@{explicitRollback.InitialCondition.Version}|{explicitRollback.EffectivePolicy};"),
            FormattableString.Invariant($"frozen-gap-journey=completed:True; successful-steps:{counters.SuccessfulSteps}; triggers:{counters.Triggers}; commits:{counters.Commits}; rollbacks:{counters.Rollbacks}; fallback-commit-violations:{counters.FallbackCommitViolations}; unsafe-commits:{counters.UnsafeCommits}; untargeted-disagreements:{counters.UntargetedDisagreements}; trip-steps:{counters.TripSteps};"),
            "activation-readiness-passes=True; production-activation=False; next-step=if green, switch authoritative desktop production selector/scenario to exact @4 in the final activation candidate, then run scheduled-long/reference-plant/I.3/cumulative Phase-I closure;",
        };
        File.WriteAllLines(Path.Combine(directory, "01-i5-repaired-exact-v4-activation-readiness.summary.txt"), summary, Utf8WithoutBom);
    }

    private static string F(double value)
        => value.ToString("G17", CultureInfo.InvariantCulture);

    private static string ReportDirectory()
        => Path.Combine(FindRepositoryRoot(), "artifacts", "i5-repaired-exact-v4-activation-readiness");

    private static void ResetDirectory(string directory)
    {
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }
        Directory.CreateDirectory(directory);
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
        throw new InvalidOperationException("Could not locate repository root from test output directory.");
    }

    private sealed class Counters
    {
        public int SuccessfulSteps { get; set; }
        public int Triggers { get; set; }
        public int Commits { get; set; }
        public int Rollbacks { get; set; }
        public int FallbackCommitViolations { get; set; }
        public int UnsafeCommits { get; set; }
        public int UntargetedDisagreements { get; set; }
        public int TripSteps { get; set; }
    }

    private sealed record Checkpoint(
        long LogicalStep,
        string Segment,
        int SegmentStep,
        string ExhaustPhase,
        double ExhaustSpecificVolume,
        double ExhaustSpecificInternalEnergy,
        double ExhaustPressureKilopascals,
        double ExhaustTemperatureCelsius,
        double RequestMegawatts,
        double GrossMegawatts,
        double ShaftMegawatts,
        double RotorRpm,
        bool TriggerObserved,
        bool CorrectedCandidateCommitted,
        bool RollbackRequired);
}
