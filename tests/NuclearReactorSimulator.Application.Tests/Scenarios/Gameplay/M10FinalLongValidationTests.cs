using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.ControlRoom.Automation;
using NuclearReactorSimulator.Application.ControlRoom.MissionPerformance;
using NuclearReactorSimulator.Application.Scenarios;
using NuclearReactorSimulator.Application.Scenarios.Challenges;
using NuclearReactorSimulator.Application.Scenarios.Challenges.Packs;
using NuclearReactorSimulator.Application.Scenarios.Challenges.Replay;
using NuclearReactorSimulator.Application.Scenarios.Faults;
using NuclearReactorSimulator.Application.Scenarios.Faults.InstrumentationControl;
using NuclearReactorSimulator.Application.Scenarios.Operations;
using NuclearReactorSimulator.Application.Scenarios.Recording;
using NuclearReactorSimulator.Application.Scenarios.Training;
using NuclearReactorSimulator.Domain.Physics.Control.Supervisory;
using NuclearReactorSimulator.Domain.Plant;
using NuclearReactorSimulator.Simulation.Physics.Fluids;
using NuclearReactorSimulator.Simulation.Plant;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay;

/// <summary>
/// Final M10 scheduled-long integral qualification. The workload and acceptance limits are frozen in
/// eng/m10-final-long-validation-contract.json before the first acceptance run. These tests add evidence only:
/// production runtime, physics, archive schema, fingerprint algorithms and exact historical identities are unchanged.
/// </summary>
public sealed class M10FinalLongValidationTests
{
    private const string OptInEnvironmentVariable = "NRS_M10_FINAL_LONG_VALIDATION";
    private const int StepsPerSecond = 100;
    private const double MaximumMassClosureResidualKilograms = 1e-6d;
    private const double MaximumEnergyClosureResidualJoules = 1e-2d;
    private const double MaximumBalanceMassRateResidualKilogramsPerSecond = 1e-8d;
    private const double MaximumBalancePowerResidualWatts = 1e-3d;
    private const double MaximumFullToHalfArchiveSizeRatio = 2.25d;
    private static readonly UTF8Encoding Utf8WithoutBom = new(encoderShouldEmitUTF8Identifier: false);

    [Fact(Explicit = true)]
    [Trait("Category", "M10FinalLongValidation")]
    public void LR_H1_HealthyExactV4_LongSoakPreservesConservationBudgetsAndNumericalSafety()
    {
        RequireOptIn();
        ExecuteWithFailureArtifact("LR-H1", 720_000, RunHealthyLeg);
    }

    [Fact(Explicit = true)]
    [Trait("Category", "M10FinalLongValidation")]
    public void LR_M1_ProductionMissionV2_LongContinuationPreservesDemandEvidenceAndPlantHealth()
    {
        RequireOptIn();
        ExecuteWithFailureArtifact("LR-M1", 440_000, RunMissionLeg);
    }

    [Fact(Explicit = true)]
    [Trait("Category", "M10FinalLongValidation")]
    public void LR_D1_DegradedMeasurement_LongRecoveryRemainsFailClosedAndDeterministic()
    {
        RequireOptIn();
        ExecuteWithFailureArtifact("LR-D1", 180_000, RunDegradedLeg);
    }

    [Fact(Explicit = true)]
    [Trait("Category", "M10FinalLongValidation")]
    public void LR_P1_ProtectionAndTakeover_LongObservationPreservesProtectionPrecedence()
    {
        RequireOptIn();
        ExecuteWithFailureArtifact("LR-P1", 90_000, RunProtectionLeg);
    }

    [Fact(Explicit = true)]
    [Trait("Category", "M10FinalLongValidation")]
    public void LR_R1_ReplayCheckpoint_LongSentinelRemainsExactlyEquivalent()
    {
        RequireOptIn();
        ExecuteWithFailureArtifact("LR-R1", 10_000, RunReplayLeg);
    }

    private static void RunHealthyLeg()
    {
        const int totalSteps = 720_000;
        var stopwatch = Stopwatch.StartNew();
        var decision = DesktopHydraulicProductionPolicySelector.Resolve(
            DesktopHydraulicProductionPolicySelector.AuthoritativeDefaultPolicy);
        Assert.Equal(DesktopHydraulicProductionPolicy.I5RepairedFourNodeCorrectedCommit, decision.EffectivePolicy);
        Assert.Equal("integrated-operations-desktop-stable", decision.InitialCondition.InitialConditionId);
        Assert.Equal(4, decision.InitialCondition.Version);

        var engine = Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(
            DesktopHydraulicProductionPolicySelector.CreateFactory(decision).CreateRuntimeEngine());
        Assert.Equal(TimeSpan.FromMilliseconds(10d), engine.FixedDeltaTime);
        Assert.Equal(
            HydraulicNumericalCouplingMode.FourNodeBranchContinuityCorrectedCommitOptIn,
            CurrentHydraulics(engine).Mode);

        var telemetryProbe = new DesktopHydraulicProductionTelemetryProbe();
        var samples = new List<ReferenceSample>(7201) { CaptureReferenceSample(engine, engine.CreatePresentationSnapshot(ControlRoomRunState.Paused)) };
        var maxMassClosure = 0d;
        var maxEnergyClosure = 0d;
        var maxBalanceMassRate = 0d;
        var maxBalancePower = 0d;
        var healthViolations = 0;
        var reverseFlowViolations = 0;
        var unexpectedTrips = 0;
        var unexpectedFaultActivations = 0;
        var nonFiniteObservations = 0;
        var envelopeExcursions = 0;

        for (var step = 1; step <= totalSteps; step++)
        {
            ControlRoomSnapshot presentation;
            try
            {
                presentation = engine.Step(ControlRoomRunState.Running);
            }
            catch (WaterSteamStateOutOfRangeException)
            {
                envelopeExcursions++;
                throw;
            }

            telemetryProbe.Observe(engine);
            var observation = CaptureStepObservation(engine, presentation);
            if (!IsFinite(observation))
            {
                nonFiniteObservations++;
            }
            if (!IsHealthy(observation))
            {
                healthViolations++;
            }
            if (observation.AnyTrip)
            {
                unexpectedTrips++;
            }
            unexpectedFaultActivations = Math.Max(unexpectedFaultActivations, presentation.Faults.ActiveCount);
            if (HasTargetedReverseFlow(observation))
            {
                reverseFlowViolations++;
            }

            maxMassClosure = Math.Max(maxMassClosure, Math.Abs(observation.MassClosureResidualKilograms));
            maxEnergyClosure = Math.Max(maxEnergyClosure, Math.Abs(observation.EnergyClosureResidualJoules));
            maxBalanceMassRate = Math.Max(maxBalanceMassRate, Math.Abs(observation.BalanceMassRateResidualKilogramsPerSecond));
            maxBalancePower = Math.Max(maxBalancePower, Math.Abs(observation.BalancePowerResidualWatts));

            if (step % StepsPerSecond == 0)
            {
                samples.Add(CaptureReferenceSample(engine, presentation));
            }
            if (step % 30000 == 0)
            {
                AppendProgress("LR-H1", step, totalSteps);
            }
        }

        stopwatch.Stop();
        Assert.Equal(7201, samples.Count);
        var budgets = ReadFrozenBudgets();
        Assert.Equal(19, budgets.Count);
        var windowEnds = Enumerable.Range(1, 24).Select(static index => index * 300).ToArray();
        var budgetComparisons = new List<WindowBudgetComparison>();
        foreach (var windowEnd in windowEnds)
        {
            var window = samples.Where(sample => sample.SimulatedSeconds >= windowEnd - 60 && sample.SimulatedSeconds <= windowEnd).ToArray();
            Assert.Equal(61, window.Length);
            var slopes = BuildInventorySlopes(window);
            budgetComparisons.AddRange(CompareFrozenBudgets(windowEnd, window, slopes, budgets));
        }

        var telemetry = telemetryProbe.Snapshot();
        var conservationPass = maxMassClosure <= MaximumMassClosureResidualKilograms
            && maxEnergyClosure <= MaximumEnergyClosureResidualJoules
            && maxBalanceMassRate <= MaximumBalanceMassRateResidualKilogramsPerSecond
            && maxBalancePower <= MaximumBalancePowerResidualWatts;
        var budgetPass = budgetComparisons.All(static item => item.Passes);
        var telemetryPass = telemetry.ObservedSteps == totalSteps
            && telemetry.FourNodeTelemetrySteps == totalSteps
            && telemetry.TriggeredSteps > 0
            && telemetry.CandidateEligibleSteps == telemetry.TriggeredSteps
            && telemetry.CommitAuthorizedSteps == telemetry.TriggeredSteps
            && telemetry.CorrectedCommittedSteps == telemetry.TriggeredSteps
            && telemetry.RollbackSteps == 0
            && telemetry.ExplicitFallbackSteps == 0
            && telemetry.FallbackCommitViolations == 0
            && telemetry.UnsafeCommitViolations == 0
            && telemetry.UntargetedBranchDisagreementSteps == 0;
        var pass = healthViolations == 0
            && reverseFlowViolations == 0
            && unexpectedTrips == 0
            && unexpectedFaultActivations == 0
            && nonFiniteObservations == 0
            && envelopeExcursions == 0
            && conservationPass
            && budgetPass
            && telemetryPass;

        WriteConservation("LR-H1", maxMassClosure, maxEnergyClosure, maxBalanceMassRate, maxBalancePower, conservationPass);
        WriteBudgetComparisons(budgetComparisons);
        WriteTelemetry(telemetry);
        WriteTripFaultProtection("LR-H1", 0, 0, 0, unexpectedTrips, unexpectedFaultActivations, nonFiniteObservations, envelopeExcursions, $"healthy-exact-v4; health-violations={healthViolations}; reverse-flow={reverseFlowViolations}");
        WritePerformance("LR-H1", totalSteps, stopwatch.Elapsed);
        WriteLegSummary("LR-H1", 7200, totalSteps, pass,
            $"health-violations={healthViolations}; reverse-flow={reverseFlowViolations}; trips={unexpectedTrips}; unexpected-faults={unexpectedFaultActivations}; nonfinite={nonFiniteObservations}; envelope={envelopeExcursions}; budget-violations={budgetComparisons.Count(static item => !item.Passes)}; commits={telemetry.CorrectedCommittedSteps}; rollback={telemetry.RollbackSteps}; fallback={telemetry.ExplicitFallbackSteps}; unsafe={telemetry.UnsafeCommitViolations}; untargeted={telemetry.UntargetedBranchDisagreementSteps}");

        Assert.True(pass, "LR-H1 long healthy exact-v4 qualification failed; inspect m10-final-long-validation artifacts.");
    }

    private static void RunMissionLeg()
    {
        const int totalSteps = 440_000;
        var stopwatch = Stopwatch.StartNew();
        var pack = ProductionOperationalChallengePack.BoundedDemandFollowing;
        Assert.Equal("bounded-demand-following-5-10-5@2", pack.ExactId);
        Assert.Equal(new InitialConditionReference("integrated-operations-desktop-stable", 4), pack.Scenario.InitialCondition);
        var session = CreateExactV4Factory().Load(pack.Scenario);
        using var source = new MissionPerformanceLiveSnapshotSource(session, pack, TrainingGuidanceMode.Guided);
        ConfigureSupervisory(session);

        var generatorId = session.Coordinator.Current.Electrical.Generators.Single().GeneratorId;
        var raiseDispatched = false;
        var lowerDispatched = false;
        var observedTenDemand = false;
        var observedReturnFiveDemand = false;
        var unexpectedTrips = 0;
        var unexpectedFaultActivations = 0;
        var nonFiniteObservations = 0;
        var envelopeExcursions = 0;
        var duplicateTimelineRows = 0;
        var maxLifecycleSpine = 0;
        var maxRecentOperationalEvidence = 0;
        ChallengeLifecycleState finalLifecycle = source.Current.LifecycleState;
        long? terminalStep = null;
        var evidenceRows = new List<string>
        {
            "logical_step,lifecycle,external_demand_mwe,requested_load_mwe,actual_output_mwe,demand_output_error_mwe,next_change_step,next_demand_mwe,timeline_count,lifecycle_spine_count,recent_operational_count,score,grade"
        };

        for (var index = 0; index < totalSteps; index++)
        {
            var missionBefore = source.Current;
            if (missionBefore.Demand.ExternalDemandMegawatts is 10d && !raiseDispatched)
            {
                session.CommandDispatcher.Dispatch(new ControlRoomCommand(
                    ControlRoomCommandKind.GeneratorLoadRaise,
                    generatorId,
                    ControlRoomCommandTargetKind.Generator));
                raiseDispatched = true;
                observedTenDemand = true;
            }
            else if (raiseDispatched
                && missionBefore.Demand.ExternalDemandMegawatts is 5d
                && missionBefore.LogicalStep > 1000
                && !lowerDispatched)
            {
                session.CommandDispatcher.Dispatch(new ControlRoomCommand(
                    ControlRoomCommandKind.GeneratorLoadLower,
                    generatorId,
                    ControlRoomCommandTargetKind.Generator));
                lowerDispatched = true;
                observedReturnFiveDemand = true;
            }

            try
            {
                Step(session);
            }
            catch (WaterSteamStateOutOfRangeException)
            {
                envelopeExcursions++;
                throw;
            }

            var snapshot = session.Coordinator.Current;
            var mission = source.Current;
            finalLifecycle = mission.LifecycleState;
            terminalStep ??= mission.TerminalLogicalStep;
            if (snapshot.AnyTripActive)
            {
                unexpectedTrips++;
            }
            unexpectedFaultActivations = Math.Max(unexpectedFaultActivations, snapshot.Faults.ActiveCount);

            var demand = mission.Demand;
            foreach (var value in new[] { demand.ExternalDemandMegawatts, demand.RequestedGeneratorLoadMegawatts, demand.ActualElectricalOutputMegawatts })
            {
                if (value.HasValue && !double.IsFinite(value.Value))
                {
                    nonFiniteObservations++;
                }
            }

            maxLifecycleSpine = Math.Max(maxLifecycleSpine, mission.LifecycleSpine.Count);
            maxRecentOperationalEvidence = Math.Max(maxRecentOperationalEvidence, mission.RecentOperationalEvidence.Count);
            var keys = mission.Timeline.Select(static item => (item.LogicalStep, item.Kind, item.SourceId, item.SourceSequence)).ToArray();
            duplicateTimelineRows += keys.Length - keys.Distinct().Count();

            if (snapshot.LogicalStep % StepsPerSecond == 0)
            {
                evidenceRows.Add(string.Join(',', new[]
                {
                    snapshot.LogicalStep.ToString(CultureInfo.InvariantCulture),
                    mission.LifecycleState.ToString(),
                    FormatNullable(demand.ExternalDemandMegawatts),
                    FormatNullable(demand.RequestedGeneratorLoadMegawatts),
                    FormatNullable(demand.ActualElectricalOutputMegawatts),
                    FormatNullable(demand.DemandOutputErrorMegawatts),
                    demand.NextScheduledDemandChangeLogicalStep?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
                    FormatNullable(demand.NextScheduledDemandMegawatts),
                    mission.Timeline.Count.ToString(CultureInfo.InvariantCulture),
                    mission.LifecycleSpine.Count.ToString(CultureInfo.InvariantCulture),
                    mission.RecentOperationalEvidence.Count.ToString(CultureInfo.InvariantCulture),
                    mission.Score.FinalScore?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
                    mission.Score.Grade?.ToString() ?? string.Empty,
                }));
            }
            if (snapshot.LogicalStep % 30000 == 0)
            {
                AppendProgress("LR-M1", checked((int)snapshot.LogicalStep), totalSteps);
            }
        }

        stopwatch.Stop();
        WriteAllLines("08-mission-demand-score-evidence.csv", evidenceRows);
        var acceptedKinds = session.OperatorActions.Actions.Select(static item => item.Command.Kind).ToArray();
        var pass = raiseDispatched
            && lowerDispatched
            && observedTenDemand
            && observedReturnFiveDemand
            && acceptedKinds.Contains(ControlRoomCommandKind.GeneratorLoadRaise)
            && acceptedKinds.Contains(ControlRoomCommandKind.GeneratorLoadLower)
            && unexpectedTrips == 0
            && unexpectedFaultActivations == 0
            && nonFiniteObservations == 0
            && envelopeExcursions == 0
            && duplicateTimelineRows == 0
            && maxLifecycleSpine <= 32
            && maxRecentOperationalEvidence <= 100
            && finalLifecycle != ChallengeLifecycleState.Failed;

        WriteTripFaultProtection("LR-M1", 0, 0, 0, unexpectedTrips, unexpectedFaultActivations, nonFiniteObservations, envelopeExcursions, $"final-lifecycle={finalLifecycle}; terminal-step={terminalStep?.ToString(CultureInfo.InvariantCulture) ?? "none"}");
        WriteEvidenceGrowth("LR-M1", totalSteps + 1L, 0, 0, 0, 0, 0, 0, maxLifecycleSpine, maxRecentOperationalEvidence, duplicateTimelineRows, true);
        WritePerformance("LR-M1", totalSteps, stopwatch.Elapsed);
        WriteLegSummary("LR-M1", 4400, totalSteps, pass,
            $"final-lifecycle={finalLifecycle}; raise={raiseDispatched}; lower={lowerDispatched}; trips={unexpectedTrips}; unexpected-faults={unexpectedFaultActivations}; nonfinite={nonFiniteObservations}; envelope={envelopeExcursions}; duplicate-timeline={duplicateTimelineRows}; lifecycle-cap={maxLifecycleSpine}; recent-cap={maxRecentOperationalEvidence}");

        Assert.True(pass, "LR-M1 production mission long continuation failed; inspect m10-final-long-validation artifacts.");
    }

    private static void RunDegradedLeg()
    {
        const int totalSteps = 180_000;
        const int activationStep = 54_000;
        const int clearStep = 90_000;
        var stopwatch = Stopwatch.StartNew();
        var production = DesktopIntegratedOperationsProductionProgram.RepairedProductionScenario;
        var scenario = new ScenarioDefinition(
            "m10-final-long-degraded-measurement-v1",
            "M10 final long degraded measurement validation",
            "Validation-only exact-v4 composition reusing the existing unavailable power measurement fault seam.",
            production.InitialCondition,
            production.Objectives,
            production.AllowedOperatorActions,
            new[]
            {
                new ScenarioFaultDefinition(
                    "m10-final-long-power-unavailable",
                    InstrumentationControlFaultTypeIds.SensorUnavailable,
                    "power",
                    ScenarioFaultTriggerDefinition.AtLogicalStep(activationStep),
                    ScenarioFaultTriggerDefinition.AtLogicalStep(clearStep)),
            });

        var session = CreateExactV4Factory().Load(scenario);
        ConfigureSupervisory(session);
        var baselineInvalid = session.Coordinator.Current.InvalidMeasuredSignalCount;
        long? firstFaultActive = null;
        long? firstFaultCleared = null;
        long? firstDegradedAuthority = null;
        long? firstRecoveredAuthority = null;
        var unexpectedTrips = 0;
        var unexpectedFaults = 0;
        var nonFiniteObservations = 0;
        var degradedSteps = 0;
        var normalPostRecoverySteps = 0;

        for (var index = 0; index < totalSteps; index++)
        {
            Step(session);
            var snapshot = session.Coordinator.Current;
            var automation = session.PlantControlAuthority.CurrentAutomation;
            var fault = snapshot.Faults.Faults.Single();
            if (fault.Lifecycle == ScenarioFaultLifecycleState.Active)
            {
                firstFaultActive ??= snapshot.LogicalStep;
            }
            if (fault.Lifecycle == ScenarioFaultLifecycleState.Cleared)
            {
                firstFaultCleared ??= snapshot.LogicalStep;
            }
            if (automation.Health == PlantControlAuthorityHealth.Degraded)
            {
                firstDegradedAuthority ??= snapshot.LogicalStep;
                degradedSteps++;
                if (automation.RequestedAuthority != PlantControlAuthorityMode.SupervisoryAutomatic
                    || automation.EffectiveAuthority != PlantControlAuthorityMode.Assisted)
                {
                    unexpectedFaults++;
                }
            }
            if (snapshot.LogicalStep > clearStep
                && automation.Health == PlantControlAuthorityHealth.Normal
                && automation.EffectiveAuthority == PlantControlAuthorityMode.SupervisoryAutomatic)
            {
                firstRecoveredAuthority ??= snapshot.LogicalStep;
                normalPostRecoverySteps++;
            }
            if (snapshot.AnyTripActive)
            {
                unexpectedTrips++;
            }
            var power = snapshot.ReactorCore.ReactorThermalPower.NumericValue;
            if (power.HasValue && !double.IsFinite(power.Value))
            {
                nonFiniteObservations++;
            }
            if (snapshot.Faults.ActiveCount > 1 || snapshot.Faults.ClearedCount > 1)
            {
                unexpectedFaults++;
            }
            if (snapshot.LogicalStep % 30000 == 0)
            {
                AppendProgress("LR-D1", checked((int)snapshot.LogicalStep), totalSteps);
            }
        }

        stopwatch.Stop();
        var finalAutomation = session.PlantControlAuthority.CurrentAutomation;
        var finalFault = session.Coordinator.Current.Faults.Faults.Single();
        var pass = firstFaultActive == activationStep
            && firstFaultCleared == clearStep
            && firstDegradedAuthority.HasValue
            && firstDegradedAuthority.Value >= activationStep
            && firstDegradedAuthority.Value <= activationStep + 1
            && firstRecoveredAuthority.HasValue
            && firstRecoveredAuthority.Value >= clearStep
            && firstRecoveredAuthority.Value <= clearStep + 1
            && degradedSteps > 0
            && normalPostRecoverySteps > 0
            && session.Coordinator.Current.InvalidMeasuredSignalCount == baselineInvalid
            && finalFault.Lifecycle == ScenarioFaultLifecycleState.Cleared
            && finalAutomation.RequestedAuthority == PlantControlAuthorityMode.SupervisoryAutomatic
            && finalAutomation.EffectiveAuthority == PlantControlAuthorityMode.SupervisoryAutomatic
            && finalAutomation.Health == PlantControlAuthorityHealth.Normal
            && unexpectedTrips == 0
            && unexpectedFaults == 0
            && nonFiniteObservations == 0;

        WriteTripFaultProtection("LR-D1", 1, 1, 0, unexpectedTrips, unexpectedFaults, nonFiniteObservations, 0,
            $"fault-active={firstFaultActive}; fault-clear={firstFaultCleared}; degraded-authority={firstDegradedAuthority}; recovered-authority={firstRecoveredAuthority}; degraded-steps={degradedSteps}");
        WritePerformance("LR-D1", totalSteps, stopwatch.Elapsed);
        WriteLegSummary("LR-D1", 1800, totalSteps, pass,
            $"fault-active={firstFaultActive}; fault-clear={firstFaultCleared}; degraded-authority={firstDegradedAuthority}; recovered-authority={firstRecoveredAuthority}; trips={unexpectedTrips}; unexpected-faults={unexpectedFaults}; nonfinite={nonFiniteObservations}");

        Assert.True(pass, "LR-D1 degraded/recovery long qualification failed; inspect m10-final-long-validation artifacts.");
    }

    private static void RunProtectionLeg()
    {
        const int totalSteps = 90_000;
        const int scramStep = 54_000;
        const int authorityObservationStep = 54_001;
        const int blockedCommandStep = 60_000;
        const int manualTakeoverStep = 72_000;
        var stopwatch = Stopwatch.StartNew();
        var session = CreateExactV4Factory().Load(DesktopIntegratedOperationsProductionProgram.RepairedProductionScenario);
        ConfigureSupervisory(session);
        var unexpectedProtectionEvents = 0;
        var scramObserved = false;
        var suspendedObserved = false;
        var blockedCommandPreservedScram = false;
        var manualTakeoverObserved = false;
        var unexpectedFaultActivations = 0;
        var nonFiniteObservations = 0;

        for (var nextStep = 1; nextStep <= totalSteps; nextStep++)
        {
            if (nextStep == scramStep)
            {
                session.CommandDispatcher.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.ReactorScram));
            }
            if (nextStep == blockedCommandStep)
            {
                session.CommandDispatcher.Dispatch(new ControlRoomCommand(
                    ControlRoomCommandKind.ControlRodWithdraw,
                    "regulating",
                    ControlRoomCommandTargetKind.ControlRodGroup));
            }
            if (nextStep == manualTakeoverStep)
            {
                session.PlantControlAuthority.RequestAuthority(PlantControlAuthorityMode.Manual);
            }

            Step(session);
            var snapshot = session.Coordinator.Current;
            var automation = session.PlantControlAuthority.CurrentAutomation;
            if (snapshot.LogicalStep < scramStep && snapshot.AnyTripActive)
            {
                unexpectedProtectionEvents++;
            }
            if (snapshot.LogicalStep >= scramStep)
            {
                scramObserved |= snapshot.ReactorScramActive;
            }
            if (snapshot.LogicalStep == authorityObservationStep)
            {
                suspendedObserved = snapshot.ReactorScramActive
                    && automation.RequestedAuthority == PlantControlAuthorityMode.SupervisoryAutomatic
                    && automation.EffectiveAuthority == PlantControlAuthorityMode.Assisted
                    && automation.Health == PlantControlAuthorityHealth.SuspendedByProtection;
            }
            if (snapshot.LogicalStep == blockedCommandStep)
            {
                blockedCommandPreservedScram = snapshot.ReactorScramActive;
            }
            if (snapshot.LogicalStep >= manualTakeoverStep)
            {
                manualTakeoverObserved |= automation.RequestedAuthority == PlantControlAuthorityMode.Manual
                    && automation.EffectiveAuthority == PlantControlAuthorityMode.Manual
                    && automation.Health == PlantControlAuthorityHealth.Normal
                    && snapshot.ReactorScramActive;
            }
            unexpectedFaultActivations = Math.Max(unexpectedFaultActivations, snapshot.Faults.ActiveCount);
            var generator = snapshot.Electrical.Generators.Single();
            if (generator.ElectricalOutput.NumericValue.HasValue && !double.IsFinite(generator.ElectricalOutput.NumericValue.Value))
            {
                nonFiniteObservations++;
            }
            if (snapshot.LogicalStep % 30000 == 0)
            {
                AppendProgress("LR-P1", checked((int)snapshot.LogicalStep), totalSteps);
            }
        }

        stopwatch.Stop();
        var finalSnapshot = session.Coordinator.Current;
        var pass = unexpectedProtectionEvents == 0
            && scramObserved
            && suspendedObserved
            && blockedCommandPreservedScram
            && manualTakeoverObserved
            && finalSnapshot.ReactorScramActive
            && unexpectedFaultActivations == 0
            && nonFiniteObservations == 0;

        WriteTripFaultProtection("LR-P1", 0, 0, 1, unexpectedProtectionEvents, unexpectedFaultActivations, nonFiniteObservations, 0,
            $"scram-step={scramStep}; authority-suspended-step={authorityObservationStep}; blocked-command-step={blockedCommandStep}; manual-takeover-step={manualTakeoverStep}; scram-final={finalSnapshot.ReactorScramActive}");
        WritePerformance("LR-P1", totalSteps, stopwatch.Elapsed);
        WriteLegSummary("LR-P1", 900, totalSteps, pass,
            $"scram={scramObserved}; suspended={suspendedObserved}; blocked-command-preserved-scram={blockedCommandPreservedScram}; manual-takeover={manualTakeoverObserved}; unexpected-protection={unexpectedProtectionEvents}; unexpected-faults={unexpectedFaultActivations}; nonfinite={nonFiniteObservations}");

        Assert.True(pass, "LR-P1 protection/takeover long qualification failed; inspect m10-final-long-validation artifacts.");
    }

    private static void RunReplayLeg()
    {
        const int totalSteps = 10_000;
        const int loadRaiseStep = 500;
        const int checkpointStep = 5_000;
        const int rodHoldStep = 6_000;
        const int loadLowerStep = 3_000;
        var stopwatch = Stopwatch.StartNew();
        var pack = ProductionOperationalChallengePack.BoundedDemandFollowing;
        var factory = CreateExactV4Factory();
        var session = factory.Load(pack.Scenario);
        using var recorder = new ScenarioRecorder(session);
        ConfigureSupervisory(session);
        var generatorId = session.Coordinator.Current.Electrical.Generators.Single().GeneratorId;
        ScenarioCheckpoint? checkpoint = null;
        var unexpectedTrips = 0;
        var unexpectedFaultActivations = 0;
        var nonFiniteObservations = 0;

        for (var nextStep = 1; nextStep <= totalSteps; nextStep++)
        {
            if (nextStep == loadRaiseStep)
            {
                session.CommandDispatcher.Dispatch(new ControlRoomCommand(
                    ControlRoomCommandKind.GeneratorLoadRaise,
                    generatorId,
                    ControlRoomCommandTargetKind.Generator));
            }
            if (nextStep == rodHoldStep)
            {
                session.CommandDispatcher.Dispatch(new ControlRoomCommand(
                    ControlRoomCommandKind.ControlRodHold,
                    "regulating",
                    ControlRoomCommandTargetKind.ControlRodGroup));
            }
            if (nextStep == loadLowerStep)
            {
                session.CommandDispatcher.Dispatch(new ControlRoomCommand(
                    ControlRoomCommandKind.GeneratorLoadLower,
                    generatorId,
                    ControlRoomCommandTargetKind.Generator));
            }
            Step(session);
            var current = session.Coordinator.Current;
            if (current.AnyTripActive)
            {
                unexpectedTrips++;
            }
            unexpectedFaultActivations = Math.Max(unexpectedFaultActivations, current.Faults.ActiveCount);
            var currentGenerator = current.Electrical.Generators.Single();
            var finiteValues = new[]
            {
                current.ReactorCore.ReactorThermalPower.NumericValue,
                currentGenerator.RequestedElectricalPower.NumericValue,
                currentGenerator.ElectricalOutput.NumericValue,
            };
            nonFiniteObservations += finiteValues.Count(static value => value.HasValue && !double.IsFinite(value.Value));
            if (current.LogicalStep == checkpointStep)
            {
                checkpoint = recorder.CreateCheckpoint("m10-final-long-r1-prefix");
            }
            if (session.Coordinator.Current.LogicalStep % 3000 == 0)
            {
                AppendProgress("LR-R1", checked((int)session.Coordinator.Current.LogicalStep), totalSteps);
            }
        }

        var recording = recorder.Complete();
        Assert.NotNull(checkpoint);
        var archive = ScenarioSessionArchive.FromRecording("m10-final-long-r1", pack.Scenario, recording);
        var finalFingerprint = ControlRoomSnapshotFingerprint.Compute(session.Coordinator.Current);
        var projection = OperationalChallengeRecordingProjector.Project(pack, recording);
        var runner = new ScenarioFullReplayRunner(factory);

        var replay = runner.ReplayAndVerify(archive);
        var replayFingerprint = ControlRoomSnapshotFingerprint.Compute(replay.Session.Coordinator.Current);
        var replayProjection = OperationalChallengeRecordingProjector.Project(pack, replay.ReplayedRecording);

        var restored = runner.SeekAndVerify(archive, checkpoint!.CheckpointId);
        using var continuationRecorder = new ScenarioRecorder(restored.Session, restored.ReplayedRecording);
        var restoredGeneratorId = restored.Session.Coordinator.Current.Electrical.Generators.Single().GeneratorId;
        for (var nextStep = checkpointStep + 1; nextStep <= totalSteps; nextStep++)
        {
            if (nextStep == rodHoldStep)
            {
                restored.Session.CommandDispatcher.Dispatch(new ControlRoomCommand(
                    ControlRoomCommandKind.ControlRodHold,
                    "regulating",
                    ControlRoomCommandTargetKind.ControlRodGroup));
            }
            if (nextStep == loadLowerStep)
            {
                restored.Session.CommandDispatcher.Dispatch(new ControlRoomCommand(
                    ControlRoomCommandKind.GeneratorLoadLower,
                    restoredGeneratorId,
                    ControlRoomCommandTargetKind.Generator));
            }
            Step(restored.Session);
        }
        var continuation = continuationRecorder.Complete();
        var continuationFingerprint = ControlRoomSnapshotFingerprint.Compute(restored.Session.Coordinator.Current);
        var continuationProjection = OperationalChallengeRecordingProjector.Project(pack, continuation);

        var halfArchive = archive.ThroughLogicalStep(checkpointStep);
        var halfBytes = JsonSerializer.SerializeToUtf8Bytes(halfArchive).LongLength;
        var fullBytes = JsonSerializer.SerializeToUtf8Bytes(archive).LongLength;
        var sizeRatio = halfBytes > 0 ? fullBytes / (double)halfBytes : double.PositiveInfinity;
        var frameGrowthPass = recording.Frames.Count == totalSteps + 1
            && archive.Frames.Count == totalSteps + 1
            && halfArchive.Frames.Count == checkpointStep + 1;
        var sizeGrowthPass = sizeRatio <= MaximumFullToHalfArchiveSizeRatio;
        var exactRecordingPass = RecordingEquivalent(recording, replay.ReplayedRecording)
            && RecordingEquivalent(recording, continuation);
        var pass = unexpectedTrips == 0
            && unexpectedFaultActivations == 0
            && nonFiniteObservations == 0
            && finalFingerprint == replayFingerprint
            && finalFingerprint == continuationFingerprint
            && projection.DeterministicFingerprint == replayProjection.DeterministicFingerprint
            && projection.DeterministicFingerprint == continuationProjection.DeterministicFingerprint
            && archive.SchemaVersion == ScenarioSessionArchive.CurrentSchemaVersion
            && exactRecordingPass
            && frameGrowthPass
            && sizeGrowthPass;

        stopwatch.Stop();
        WriteReplaySentinel(finalFingerprint, replayFingerprint, continuationFingerprint,
            projection.DeterministicFingerprint, replayProjection.DeterministicFingerprint, continuationProjection.DeterministicFingerprint,
            checkpointStep, archive.SchemaVersion, exactRecordingPass, pass);
        WriteEvidenceGrowth("LR-R1", recording.Frames.Count, recording.Events.Count, recording.OperatorActions.Count,
            recording.AutomationIntents.Count, recording.Checkpoints.Count, halfBytes, fullBytes, 0, 0, 0, frameGrowthPass && sizeGrowthPass);
        WriteTripFaultProtection("LR-R1", 0, 0, 0, unexpectedTrips, unexpectedFaultActivations, nonFiniteObservations, 0, "replay-checkpoint-sentinel");
        WritePerformance("LR-R1", totalSteps, stopwatch.Elapsed);
        WriteLegSummary("LR-R1", 100, totalSteps, pass,
            $"frames={recording.Frames.Count}; events={recording.Events.Count}; actions={recording.OperatorActions.Count}; intents={recording.AutomationIntents.Count}; checkpoints={recording.Checkpoints.Count}; half-bytes={halfBytes}; full-bytes={fullBytes}; size-ratio={sizeRatio:G17}; exact-recording={exactRecordingPass}; trips={unexpectedTrips}; unexpected-faults={unexpectedFaultActivations}; nonfinite={nonFiniteObservations}");

        Assert.True(pass, "LR-R1 replay/checkpoint long sentinel failed; inspect m10-final-long-validation artifacts.");
    }

    private static bool RecordingEquivalent(ScenarioRecording expected, ScenarioRecording actual)
        => expected.ScenarioId == actual.ScenarioId
            && expected.InitialCondition == actual.InitialCondition
            && expected.Frames.Select(static frame => (frame.LogicalStep, frame.SnapshotFingerprint, frame.FirstEventSequence, frame.LastEventSequence))
                .SequenceEqual(actual.Frames.Select(static frame => (frame.LogicalStep, frame.SnapshotFingerprint, frame.FirstEventSequence, frame.LastEventSequence)))
            && expected.OperatorActions.SequenceEqual(actual.OperatorActions)
            && expected.AutomationIntents.SequenceEqual(actual.AutomationIntents)
            && expected.Events.SequenceEqual(actual.Events)
            && expected.Checkpoints.SequenceEqual(actual.Checkpoints);

    private static void ExecuteWithFailureArtifact(string legId, int plannedSteps, Action action)
    {
        try
        {
            action();
        }
        catch (Exception exception) when (exception.GetType().Namespace?.StartsWith("Xunit", StringComparison.Ordinal) == true)
        {
            EnsureFailureLegSummary(legId, plannedSteps, exception);
            throw;
        }
        catch (Exception exception)
        {
            WriteUnhandledException(legId, exception);
            EnsureFailureLegSummary(legId, plannedSteps, exception);
            throw;
        }
    }

    private static void RequireOptIn()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable(OptInEnvironmentVariable), "1", StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"{OptInEnvironmentVariable}=1 is required for the explicit final M10 long validation.");
        }
    }

    private static ScenarioSessionFactory CreateExactV4Factory()
        => new(new VersionedInitialConditionRegistry(new IVersionedInitialConditionFactory[]
        {
            new DesktopSustainedGenerationI5RepairedActivationCandidateInitialConditionFactory(),
        }));

    private static void ConfigureSupervisory(ScenarioSession session)
    {
        session.PlantControlAuthority.RequestSupervisoryObjective(SupervisoryObjectiveRequest.HoldCurrentOperatingPoint());
        session.PlantControlAuthority.RequestAuthority(PlantControlAuthorityMode.SupervisoryAutomatic);
    }

    private static void Step(ScenarioSession session)
        => session.CommandDispatcher.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.SingleStep));

    private static StepObservation CaptureStepObservation(IntegratedAutomaticOperationRuntimeEngine engine, ControlRoomSnapshot presentation)
    {
        var fullPlant = engine.LatestCanonicalSnapshot.Control.ProtectedControl.FullPlant;
        var turbine = fullPlant.IntegratedCycle.TurbineExpansion;
        var train = Assert.Single(turbine.MainSteamNetwork.AdmissionTrains);
        var stage = Assert.Single(turbine.StageGroups);
        var generator = Assert.Single(presentation.Electrical.Generators);
        var rotor = Assert.Single(presentation.TurbineSecondary.Rotors);
        return new StepObservation(
            presentation.LogicalStep,
            presentation.LogicalStep / (double)StepsPerSecond,
            presentation.AnyTripActive,
            generator.BreakerClosed,
            generator.RequestedElectricalPower.NumericValue ?? double.NaN,
            generator.ElectricalOutput.NumericValue ?? double.NaN,
            rotor.ShaftPower.NumericValue ?? double.NaN,
            turbine.TotalShaftPower.Megawatts,
            stage.EffectiveMassFlowRate.KilogramsPerSecond,
            train.StopValve.MassFlowRate.KilogramsPerSecond,
            train.ControlValve.MassFlowRate.KilogramsPerSecond,
            train.AdmissionValve.MassFlowRate.KilogramsPerSecond,
            fullPlant.HeatBalance.MassClosureResidualKilograms,
            fullPlant.HeatBalance.FullEnergyPathClosureResidualJoules,
            fullPlant.IntegratedCycle.ThermofluidAudit.BalanceMassRateResidualKilogramsPerSecond,
            fullPlant.IntegratedCycle.ThermofluidAudit.BalancePowerResidualWatts);
    }

    private static ReferenceSample CaptureReferenceSample(IntegratedAutomaticOperationRuntimeEngine engine, ControlRoomSnapshot presentation)
    {
        var fullPlant = engine.LatestCanonicalSnapshot.Control.ProtectedControl.FullPlant;
        var plant = fullPlant.CandidatePlant;
        var turbine = fullPlant.IntegratedCycle.TurbineExpansion;
        var admissionTrain = Assert.Single(turbine.MainSteamNetwork.AdmissionTrains);
        var condenser = Assert.Single(fullPlant.IntegratedCycle.Condenser.Condensers);
        var condensateTrain = Assert.Single(fullPlant.IntegratedCycle.CondensateFeedwater.Trains);
        var drum = Assert.Single(fullPlant.IntegratedCycle.PrimaryCircuit.SteamDrums.Drums);
        var steamLine = Assert.Single(turbine.MainSteamNetwork.SteamLines);
        var generator = Assert.Single(presentation.Electrical.Generators);
        var rotor = Assert.Single(presentation.TurbineSecondary.Rotors);
        var exhaust = plant.GetFluidNode(condenser.SteamSpaceNodeId);
        var hotwell = plant.GetFluidNode(condenser.HotwellNodeId);
        var feedwater = plant.GetFluidNode(condensateTrain.FeedwaterInventoryNodeId);
        var drumInventory = plant.GetFluidNode(drum.InventoryNodeId);
        var header = plant.GetFluidNode(steamLine.HeaderNodeId);
        return new ReferenceSample(
            presentation.LogicalStep,
            presentation.LogicalStep / (double)StepsPerSecond,
            presentation.AnyTripActive,
            generator.BreakerClosed,
            generator.RequestedElectricalPower.NumericValue ?? double.NaN,
            generator.ElectricalOutput.NumericValue ?? double.NaN,
            rotor.ShaftPower.NumericValue ?? double.NaN,
            rotor.Speed.NumericValue ?? double.NaN,
            condenser.FinalSteamSpacePressure.Kilopascals,
            drum.LiquidLevelFraction.Fraction,
            plant.FluidNodes.Sum(static node => node.Mass.Kilograms),
            plant.FluidNodes.Sum(static node => node.InternalEnergy.Joules),
            exhaust.Mass.Kilograms,
            hotwell.Mass.Kilograms,
            feedwater.Mass.Kilograms,
            drumInventory.Mass.Kilograms,
            header.Mass.Kilograms,
            admissionTrain.StopValve.MassFlowRate.KilogramsPerSecond,
            admissionTrain.ControlValve.MassFlowRate.KilogramsPerSecond,
            admissionTrain.AdmissionValve.MassFlowRate.KilogramsPerSecond);
    }

    private static bool IsFinite(StepObservation observation)
        => new[]
        {
            observation.SimulatedSeconds,
            observation.RequestedElectricalPowerMegawatts,
            observation.GrossElectricalPowerMegawatts,
            observation.RotorShaftPowerMegawatts,
            observation.CanonicalShaftPowerMegawatts,
            observation.StageFlowKilogramsPerSecond,
            observation.StopFlowKilogramsPerSecond,
            observation.ControlFlowKilogramsPerSecond,
            observation.AdmissionFlowKilogramsPerSecond,
            observation.MassClosureResidualKilograms,
            observation.EnergyClosureResidualJoules,
            observation.BalanceMassRateResidualKilogramsPerSecond,
            observation.BalancePowerResidualWatts,
        }.All(double.IsFinite);

    private static bool IsHealthy(StepObservation observation)
        => !observation.AnyTrip
            && observation.GeneratorBreakerClosed
            && observation.RequestedElectricalPowerMegawatts > 4.5d
            && observation.GrossElectricalPowerMegawatts > 4.0d
            && observation.RotorShaftPowerMegawatts > 4.5d
            && observation.CanonicalShaftPowerMegawatts > 4.5d;

    private static bool HasTargetedReverseFlow(StepObservation observation)
        => observation.StopFlowKilogramsPerSecond < 0d
            || observation.ControlFlowKilogramsPerSecond < 0d
            || observation.AdmissionFlowKilogramsPerSecond < 0d;

    private static IReadOnlyList<ToleranceBudget> ReadFrozenBudgets()
    {
        var lines = File.ReadAllLines(Path.Combine(FindRepositoryRoot(), "eng", "frozen-evidence", "ordinary", "I3_ValidatedAuthoritativeToleranceBudgets.csv"));
        Assert.Equal(20, lines.Length);
        return lines.Skip(1).Where(static line => !string.IsNullOrWhiteSpace(line)).Select(static line =>
        {
            var fields = line.Split(',', 5, StringSplitOptions.None);
            return new ToleranceBudget(
                fields[0],
                fields[1],
                double.Parse(fields[2], NumberStyles.Float, CultureInfo.InvariantCulture),
                double.Parse(fields[3], NumberStyles.Float, CultureInfo.InvariantCulture));
        }).ToArray();
    }

    private static IReadOnlyList<InventorySlope> BuildInventorySlopes(IReadOnlyList<ReferenceSample> window)
        => new[]
        {
            BuildSlope("total-fluid-mass", window, static sample => sample.TotalFluidMassKilograms),
            BuildSlope("total-fluid-internal-energy", window, static sample => sample.TotalFluidInternalEnergyJoules),
            BuildSlope("exhaust-mass", window, static sample => sample.ExhaustMassKilograms),
            BuildSlope("hotwell-mass", window, static sample => sample.HotwellMassKilograms),
            BuildSlope("feedwater-inventory-mass", window, static sample => sample.FeedwaterInventoryMassKilograms),
            BuildSlope("drum-inventory-mass", window, static sample => sample.DrumInventoryMassKilograms),
            BuildSlope("main-steam-header-mass", window, static sample => sample.MainSteamHeaderMassKilograms),
        };

    private static InventorySlope BuildSlope(string metricId, IReadOnlyList<ReferenceSample> window, Func<ReferenceSample, double> selector)
    {
        var meanTime = window.Average(static sample => sample.SimulatedSeconds);
        var meanValue = window.Average(selector);
        var numerator = 0d;
        var denominator = 0d;
        foreach (var sample in window)
        {
            var dx = sample.SimulatedSeconds - meanTime;
            numerator += dx * (selector(sample) - meanValue);
            denominator += dx * dx;
        }
        return new InventorySlope(metricId, denominator > 0d ? numerator / denominator : double.NaN);
    }

    private static IReadOnlyList<WindowBudgetComparison> CompareFrozenBudgets(
        int windowEndSeconds,
        IReadOnlyList<ReferenceSample> window,
        IReadOnlyList<InventorySlope> slopes,
        IReadOnlyList<ToleranceBudget> budgets)
    {
        var observed = new Dictionary<string, double>(StringComparer.Ordinal)
        {
            ["gross-electrical-power"] = window.Average(static sample => sample.GrossElectricalPowerMegawatts),
            ["shaft-power"] = window.Average(static sample => sample.RotorShaftPowerMegawatts),
            ["rotor-speed"] = window.Average(static sample => sample.RotorSpeedRpm),
            ["condenser-pressure"] = window.Average(static sample => sample.CondenserPressureKilopascals),
            ["drum-level-fraction"] = window.Average(static sample => sample.DrumLevelFraction),
            ["total-fluid-mass"] = window.Average(static sample => sample.TotalFluidMassKilograms),
            ["total-fluid-internal-energy"] = window.Average(static sample => sample.TotalFluidInternalEnergyJoules),
            ["exhaust-mass"] = window.Average(static sample => sample.ExhaustMassKilograms),
            ["hotwell-mass"] = window.Average(static sample => sample.HotwellMassKilograms),
            ["feedwater-inventory-mass"] = window.Average(static sample => sample.FeedwaterInventoryMassKilograms),
            ["drum-inventory-mass"] = window.Average(static sample => sample.DrumInventoryMassKilograms),
            ["main-steam-header-mass"] = window.Average(static sample => sample.MainSteamHeaderMassKilograms),
        };
        foreach (var slope in slopes)
        {
            observed[$"slope.{slope.MetricId}"] = slope.SlopePerSecond;
        }

        return budgets.Select(budget =>
        {
            if (!observed.TryGetValue(budget.MetricId, out var value))
            {
                throw new InvalidDataException($"No LR-H1 observation maps to frozen budget '{budget.MetricId}'.");
            }
            var deviation = Math.Abs(value - budget.Target);
            return new WindowBudgetComparison(windowEndSeconds, budget.MetricId, budget.Unit, budget.Target, budget.AbsoluteTolerance, value, deviation, deviation <= budget.AbsoluteTolerance);
        }).ToArray();
    }

    private static PlantNetworkHydraulicNumericalSnapshot CurrentHydraulics(IntegratedAutomaticOperationRuntimeEngine engine)
        => engine.LatestCanonicalSnapshot.Control.ProtectedControl.FullPlant.IntegratedCycle.PrimaryCircuit.HydraulicNumerics;

    private static void AppendProgress(string legId, int step, int totalSteps)
    {
        Directory.CreateDirectory(ReportDirectory());
        File.AppendAllText(
            Path.Combine(ReportDirectory(), "00-progress.txt"),
            $"{DateTimeOffset.UtcNow:O} leg={legId}; logical-step={step}; planned-steps={totalSteps}; simulated-seconds={step / StepsPerSecond}{Environment.NewLine}",
            Utf8WithoutBom);
    }

    private static void WriteLegSummary(string legId, int seconds, int steps, bool pass, string details)
    {
        var path = Path.Combine(ReportDirectory(), "03-leg-summary.csv");
        EnsureHeader(path, "leg_id,simulated_seconds,logical_steps,passes,details");
        File.AppendAllText(path, $"{legId},{seconds},{steps},{pass},{Csv(details)}{Environment.NewLine}", Utf8WithoutBom);
    }

    private static void EnsureFailureLegSummary(string legId, int plannedSteps, Exception exception)
    {
        var path = Path.Combine(ReportDirectory(), "03-leg-summary.csv");
        if (File.Exists(path) && File.ReadLines(path).Skip(1).Any(line => line.StartsWith(legId + ",", StringComparison.Ordinal)))
        {
            return;
        }
        WriteLegSummary(legId, plannedSteps / StepsPerSecond, plannedSteps, false, $"unhandled={exception.GetType().FullName}: {exception.Message}");
    }

    private static void WriteUnhandledException(string legId, Exception exception)
    {
        var path = Path.Combine(ReportDirectory(), "07-trip-fault-protection-classification.csv");
        EnsureHeader(path, "leg_id,fault_activations,fault_clears,expected_protection_events,unexpected_trip_or_protection_count,unexpected_fault_activations,nonfinite_observations,envelope_excursions,classification");
        File.AppendAllText(path,
            $"{legId},0,0,0,0,0,0,{(exception is WaterSteamStateOutOfRangeException ? 1 : 0)},{Csv("UNHANDLED " + exception.GetType().FullName + ": " + exception.Message)}{Environment.NewLine}",
            Utf8WithoutBom);
    }

    private static void WriteConservation(string legId, double mass, double energy, double massRate, double power, bool pass)
    {
        var path = Path.Combine(ReportDirectory(), "04-conservation-maxima.csv");
        EnsureHeader(path, "leg_id,max_mass_closure_kg,max_energy_closure_j,max_balance_mass_rate_kg_s,max_balance_power_w,passes");
        File.AppendAllText(path, FormattableString.Invariant($"{legId},{mass:G17},{energy:G17},{massRate:G17},{power:G17},{pass}{Environment.NewLine}"), Utf8WithoutBom);
    }

    private static void WriteBudgetComparisons(IEnumerable<WindowBudgetComparison> comparisons)
    {
        var path = Path.Combine(ReportDirectory(), "05-healthy-window-i3-budget-comparison.csv");
        EnsureHeader(path, "window_end_seconds,metric_id,unit,target,absolute_tolerance,observed,absolute_deviation,passes");
        foreach (var item in comparisons)
        {
            File.AppendAllText(path, FormattableString.Invariant($"{item.WindowEndSeconds},{item.MetricId},{item.Unit},{item.Target:G17},{item.AbsoluteTolerance:G17},{item.Observed:G17},{item.AbsoluteDeviation:G17},{item.Passes}{Environment.NewLine}"), Utf8WithoutBom);
        }
    }

    private static void WriteTelemetry(FourNodeProductionActivationTelemetrySnapshot telemetry)
    {
        var path = Path.Combine(ReportDirectory(), "06-numerical-coupling-telemetry.csv");
        WriteAllLines("06-numerical-coupling-telemetry.csv", new[]
        {
            "observed_steps,four_node_steps,triggered,eligible,authorized,committed,rollbacks,explicit_fallbacks,fallback_commit_violations,unsafe_commits,untargeted_branch_disagreements",
            FormattableString.Invariant($"{telemetry.ObservedSteps},{telemetry.FourNodeTelemetrySteps},{telemetry.TriggeredSteps},{telemetry.CandidateEligibleSteps},{telemetry.CommitAuthorizedSteps},{telemetry.CorrectedCommittedSteps},{telemetry.RollbackSteps},{telemetry.ExplicitFallbackSteps},{telemetry.FallbackCommitViolations},{telemetry.UnsafeCommitViolations},{telemetry.UntargetedBranchDisagreementSteps}"),
        });
    }

    private static void WriteTripFaultProtection(
        string legId,
        int faultActivations,
        int faultClears,
        int expectedProtection,
        int unexpected,
        int unexpectedFaultActivations,
        int nonFiniteObservations,
        int envelope,
        string classification)
    {
        var path = Path.Combine(ReportDirectory(), "07-trip-fault-protection-classification.csv");
        EnsureHeader(path, "leg_id,fault_activations,fault_clears,expected_protection_events,unexpected_trip_or_protection_count,unexpected_fault_activations,nonfinite_observations,envelope_excursions,classification");
        File.AppendAllText(path, $"{legId},{faultActivations},{faultClears},{expectedProtection},{unexpected},{unexpectedFaultActivations},{nonFiniteObservations},{envelope},{Csv(classification)}{Environment.NewLine}", Utf8WithoutBom);
    }

    private static void WriteReplaySentinel(
        string finalFingerprint,
        string replayFingerprint,
        string continuationFingerprint,
        string challengeFingerprint,
        string replayChallengeFingerprint,
        string continuationChallengeFingerprint,
        int checkpointStep,
        int archiveSchema,
        bool recordingEquivalent,
        bool pass)
    {
        WriteAllLines("09-replay-checkpoint-fingerprint-sentinels.csv", new[]
        {
            "checkpoint_step,archive_schema,final_fingerprint,full_replay_fingerprint,checkpoint_continuation_fingerprint,challenge_fingerprint,full_replay_challenge_fingerprint,checkpoint_continuation_challenge_fingerprint,recording_equivalent,passes",
            $"{checkpointStep},{archiveSchema},{finalFingerprint},{replayFingerprint},{continuationFingerprint},{challengeFingerprint},{replayChallengeFingerprint},{continuationChallengeFingerprint},{recordingEquivalent},{pass}",
        });
    }

    private static void WriteEvidenceGrowth(
        string legId,
        long frames,
        long events,
        long actions,
        long intents,
        long checkpoints,
        long halfBytes,
        long fullBytes,
        int lifecycleSpine,
        int recentOperational,
        int duplicateTimeline,
        bool pass)
    {
        var path = Path.Combine(ReportDirectory(), "10-evidence-growth.csv");
        EnsureHeader(path, "leg_id,frames,events,operator_actions,automation_intents,checkpoints,half_archive_bytes,full_archive_bytes,full_to_half_ratio,lifecycle_spine_max,recent_operational_max,duplicate_timeline_rows,passes");
        var ratio = halfBytes > 0 ? fullBytes / (double)halfBytes : 0d;
        File.AppendAllText(path, FormattableString.Invariant($"{legId},{frames},{events},{actions},{intents},{checkpoints},{halfBytes},{fullBytes},{ratio:G17},{lifecycleSpine},{recentOperational},{duplicateTimeline},{pass}{Environment.NewLine}"), Utf8WithoutBom);
    }

    private static void WritePerformance(string legId, int steps, TimeSpan elapsed)
    {
        var path = Path.Combine(ReportDirectory(), "11-performance-diagnostics.csv");
        EnsureHeader(path, "leg_id,logical_steps,wall_seconds,steps_per_second");
        var stepsPerSecond = elapsed.TotalSeconds > 0d ? steps / elapsed.TotalSeconds : double.PositiveInfinity;
        File.AppendAllText(path, FormattableString.Invariant($"{legId},{steps},{elapsed.TotalSeconds:G17},{stepsPerSecond:G17}{Environment.NewLine}"), Utf8WithoutBom);
    }

    private static void WriteAllLines(string fileName, IEnumerable<string> lines)
    {
        Directory.CreateDirectory(ReportDirectory());
        File.WriteAllLines(Path.Combine(ReportDirectory(), fileName), lines, Utf8WithoutBom);
    }

    private static void EnsureHeader(string path, string header)
    {
        Directory.CreateDirectory(ReportDirectory());
        if (!File.Exists(path))
        {
            File.WriteAllText(path, header + Environment.NewLine, Utf8WithoutBom);
        }
    }

    private static string Csv(string value)
        => '"' + value.Replace("\"", "\"\"", StringComparison.Ordinal) + '"';

    private static string FormatNullable(double? value)
        => value.HasValue ? value.Value.ToString("G17", CultureInfo.InvariantCulture) : string.Empty;

    private static string ReportDirectory()
        => Path.Combine(FindRepositoryRoot(), "artifacts", "m10-final-long-validation");

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null && !File.Exists(Path.Combine(current.FullName, "NuclearReactorSimulator.sln")))
        {
            current = current.Parent;
        }
        return current?.FullName ?? throw new DirectoryNotFoundException("Repository root could not be resolved for M10 final long validation.");
    }

    private sealed record StepObservation(
        long LogicalStep,
        double SimulatedSeconds,
        bool AnyTrip,
        bool GeneratorBreakerClosed,
        double RequestedElectricalPowerMegawatts,
        double GrossElectricalPowerMegawatts,
        double RotorShaftPowerMegawatts,
        double CanonicalShaftPowerMegawatts,
        double StageFlowKilogramsPerSecond,
        double StopFlowKilogramsPerSecond,
        double ControlFlowKilogramsPerSecond,
        double AdmissionFlowKilogramsPerSecond,
        double MassClosureResidualKilograms,
        double EnergyClosureResidualJoules,
        double BalanceMassRateResidualKilogramsPerSecond,
        double BalancePowerResidualWatts);

    private sealed record ReferenceSample(
        long LogicalStep,
        double SimulatedSeconds,
        bool AnyTrip,
        bool GeneratorBreakerClosed,
        double RequestedElectricalPowerMegawatts,
        double GrossElectricalPowerMegawatts,
        double RotorShaftPowerMegawatts,
        double RotorSpeedRpm,
        double CondenserPressureKilopascals,
        double DrumLevelFraction,
        double TotalFluidMassKilograms,
        double TotalFluidInternalEnergyJoules,
        double ExhaustMassKilograms,
        double HotwellMassKilograms,
        double FeedwaterInventoryMassKilograms,
        double DrumInventoryMassKilograms,
        double MainSteamHeaderMassKilograms,
        double StopFlowKilogramsPerSecond,
        double ControlFlowKilogramsPerSecond,
        double AdmissionFlowKilogramsPerSecond);

    private sealed record ToleranceBudget(string MetricId, string Unit, double Target, double AbsoluteTolerance);
    private sealed record InventorySlope(string MetricId, double SlopePerSecond);
    private sealed record WindowBudgetComparison(int WindowEndSeconds, string MetricId, string Unit, double Target, double AbsoluteTolerance, double Observed, double AbsoluteDeviation, bool Passes);
}
