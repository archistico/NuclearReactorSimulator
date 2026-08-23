using System.Diagnostics;
using System.Globalization;
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
using NuclearReactorSimulator.Application.Scenarios.Recording;
using NuclearReactorSimulator.Application.Scenarios.Training;
using NuclearReactorSimulator.Domain.Physics.Control.Supervisory;
using NuclearReactorSimulator.Simulation.Physics.Fluids;
using NuclearReactorSimulator.Simulation.Plant;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay;

/// <summary>
/// Authorized M10 Final replacement long over the frozen exact-v9 production baseline. The source tree and every
/// pre-existing test file are frozen by eng/m10-final-replacement-long-v9-baseline-*.sha256. This is the one additional
/// test file authorized by the baseline-freeze contract. It executes the five frozen legs inside one wall-clock envelope
/// so the 60 minute campaign cap includes all authored steps plus replay/checkpoint work.
/// </summary>
public sealed class M10FinalReplacementLongValidationTests
{
    private const string OptInEnvironmentVariable = "NRS_M10_FINAL_REPLACEMENT_LONG";
    private const int StepsPerSecond = 100;
    private const int HealthySteps = 90_000;
    private const int MissionSteps = 48_000;
    private const int DegradedSteps = 30_000;
    private const int ProtectionSteps = 18_000;
    private const int ReplaySteps = 6_000;
    private const int TotalAuthoredSteps = 192_000;
    private const int TotalAuthoredSeconds = 1_920;
    private const double MaximumMassClosureResidualKilograms = 1e-6d;
    private const double MaximumEnergyClosureResidualJoules = 1e-2d;
    private const double MaximumBalanceMassRateResidualKilogramsPerSecond = 1e-8d;
    private const double MaximumBalancePowerResidualWatts = 1e-3d;
    private const double MaximumNodeMassSlopeKilogramsPerSecond = 1e-5d;
    private const double MaximumLateNetExternalPowerMegawatts = 1e-4d;
    private const double MaximumFullToHalfArchiveSizeRatio = 2.25d;
    private const double MaximumMissionLateToEarlyWallRatio = 2d;
    private const double HardCampaignCapMinutes = 60d;
    private static readonly UTF8Encoding Utf8WithoutBom = new(encoderShouldEmitUTF8Identifier: false);
    private static readonly string[] HealthyNodeIds =
    {
        "suction", "pressure", "outlet", "drum", "steam", "header", "stop-out", "control-out",
        "turbine-inlet", "exhaust", "hotwell", "feedwater-inventory",
    };

    [Fact(Explicit = true)]
    [Trait("Category", "M10FinalReplacementLongValidation")]
    public void AuthorizedExactV9ReplacementLong_ExecutesFrozenFiveLegCampaignWithinWallBudget()
    {
        RequireOptIn();
        Directory.CreateDirectory(ReportDirectory());
        AssertFrozenProductionIdentity();

        var campaign = Stopwatch.StartNew();
        var failures = new List<string>();
        var executedAuthoredSteps = 0;

        if (ExecuteLeg("RL-H1", HealthySteps, campaign, failures, () => RunHealthyLeg(campaign))) executedAuthoredSteps += HealthySteps;
        if (ExecuteLeg("RL-M1", MissionSteps, campaign, failures, () => RunMissionLeg(campaign))) executedAuthoredSteps += MissionSteps;
        if (ExecuteLeg("RL-D1", DegradedSteps, campaign, failures, () => RunDegradedLeg(campaign))) executedAuthoredSteps += DegradedSteps;
        if (ExecuteLeg("RL-P1", ProtectionSteps, campaign, failures, () => RunProtectionLeg(campaign))) executedAuthoredSteps += ProtectionSteps;
        if (ExecuteLeg("RL-R1", ReplaySteps, campaign, failures, () => RunReplayLeg(campaign))) executedAuthoredSteps += ReplaySteps;

        campaign.Stop();
        var wallPass = campaign.Elapsed.TotalMinutes <= HardCampaignCapMinutes;
        WriteWallBudget(campaign.Elapsed, wallPass);
        AppendProgress(FormattableString.Invariant(
            $"campaign-finished completed-authored-steps={executedAuthoredSteps}; completed-authored-seconds={executedAuthoredSteps / StepsPerSecond}; planned-authored-steps={TotalAuthoredSteps}; planned-authored-seconds={TotalAuthoredSeconds}; wall-seconds={campaign.Elapsed.TotalSeconds:G17}; failures={failures.Count}; hard-cap-pass={wallPass}"));

        Assert.True(
            failures.Count == 0 && wallPass,
            $"M10 Final replacement long failed. failures={string.Join(" | ", failures)}; wall-minutes={campaign.Elapsed.TotalMinutes:G17}. Inspect m10-final-replacement-long-validation artifacts.");
    }

    private static void AssertFrozenProductionIdentity()
    {
        var decision = DesktopHydraulicProductionPolicySelector.Resolve(
            DesktopHydraulicProductionPolicySelector.AuthoritativeDefaultPolicy);
        Assert.Equal(DesktopHydraulicProductionPolicy.M10FinalExactV9QualifiedCandidate, decision.EffectivePolicy);
        Assert.Equal(new InitialConditionReference("integrated-operations-desktop-stable", 9), decision.InitialCondition);
        Assert.Equal("integrated-normal-operations-training-m10-final-v9-production", DesktopIntegratedOperationsProductionProgram.Scenario.ScenarioId);
        Assert.Equal(new InitialConditionReference("integrated-operations-desktop-stable", 9), DesktopIntegratedOperationsProductionProgram.Scenario.InitialCondition);
        Assert.Equal("bounded-demand-following-5-10-5@3", ProductionOperationalChallengePack.BoundedDemandFollowing.ExactId);
        Assert.Equal(DesktopIntegratedOperationsProductionProgram.M10FinalExactV9ProductionScenario, ProductionOperationalChallengePack.BoundedDemandFollowing.Scenario);
        Assert.Equal("bounded-demand-following-5-10-5@2", ProductionOperationalChallengePack.BoundedDemandFollowingV2.ExactId);
    }

    private static void RunHealthyLeg(Stopwatch campaign)
    {
        var leg = Stopwatch.StartNew();
        var decision = DesktopHydraulicProductionPolicySelector.Resolve(
            DesktopHydraulicProductionPolicySelector.AuthoritativeDefaultPolicy);
        var engine = Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(
            DesktopHydraulicProductionPolicySelector.CreateFactory(decision).CreateRuntimeEngine());
        Assert.Equal(TimeSpan.FromMilliseconds(10d), engine.FixedDeltaTime);

        var telemetryProbe = new DesktopHydraulicProductionTelemetryProbe();
        var samples = new List<HealthySample>(901);
        var nodeSamples = new List<NodeMassSample>(901 * HealthyNodeIds.Length);
        CaptureHealthySample(engine, 0, samples, nodeSamples);

        var unexpectedTrips = 0;
        var unexpectedFaultActivations = 0;
        var nonFiniteObservations = 0;
        var envelopeExcursions = 0;
        var maxMassClosure = 0d;
        var maxEnergyClosure = 0d;
        var maxBalanceMassRate = 0d;
        var maxBalancePower = 0d;
        var minimumMoistureDrain = double.PositiveInfinity;
        var maximumTransferMismatch = 0d;
        var maximumStageOwnershipResidual = 0d;

        for (var step = 1; step <= HealthySteps; step++)
        {
            CheckDeadline(campaign);
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
            var fullPlant = engine.LatestCanonicalSnapshot.Control.ProtectedControl.FullPlant;
            var cycle = fullPlant.IntegratedCycle;
            var heat = cycle.HeatBalance;
            var stage = Assert.Single(cycle.TurbineExpansion.StageGroups);
            var generator = Assert.Single(presentation.Electrical.Generators);
            var drum = Assert.Single(cycle.PrimaryCircuit.SteamDrums.Drums);
            var speed = engine.LatestCanonicalSnapshot.Control.ProtectedControl.TurbineSecondary.ControlAndActuator.Controllers.GetDiagnostic("speed-control");

            var values = new[]
            {
                generator.ElectricalOutput.NumericValue ?? double.NaN,
                cycle.PrimaryCircuit.MainCirculation.TotalPumpMassFlowRate.KilogramsPerSecond,
                drum.LiquidLevelFraction.Fraction,
                speed.Output,
                stage.MoistureDrainMassFlowRate.KilogramsPerSecond,
                stage.TotalTransferredMassFlowRate.KilogramsPerSecond,
                stage.CommandedMassFlowRate.KilogramsPerSecond,
                stage.TurbineEnergyOwnershipResidual.Watts,
                fullPlant.HeatBalance.MassClosureResidualKilograms,
                heat.FullEnergyPathClosureResidualJoules,
                cycle.ThermofluidAudit.BalanceMassRateResidualKilogramsPerSecond,
                cycle.ThermofluidAudit.BalancePowerResidualWatts,
                heat.NetReactorToGridExternalPower.Megawatts,
            };
            if (values.Any(static value => !double.IsFinite(value)))
            {
                nonFiniteObservations++;
            }
            if (presentation.AnyTripActive)
            {
                unexpectedTrips++;
            }
            unexpectedFaultActivations = Math.Max(unexpectedFaultActivations, presentation.Faults.ActiveCount);

            maxMassClosure = Math.Max(maxMassClosure, Math.Abs(fullPlant.HeatBalance.MassClosureResidualKilograms));
            maxEnergyClosure = Math.Max(maxEnergyClosure, Math.Abs(heat.FullEnergyPathClosureResidualJoules));
            maxBalanceMassRate = Math.Max(maxBalanceMassRate, Math.Abs(cycle.ThermofluidAudit.BalanceMassRateResidualKilogramsPerSecond));
            maxBalancePower = Math.Max(maxBalancePower, Math.Abs(cycle.ThermofluidAudit.BalancePowerResidualWatts));
            minimumMoistureDrain = Math.Min(minimumMoistureDrain, stage.MoistureDrainMassFlowRate.KilogramsPerSecond);
            maximumTransferMismatch = Math.Max(maximumTransferMismatch,
                Math.Abs(stage.TotalTransferredMassFlowRate.KilogramsPerSecond - stage.CommandedMassFlowRate.KilogramsPerSecond));
            maximumStageOwnershipResidual = Math.Max(maximumStageOwnershipResidual, Math.Abs(stage.TurbineEnergyOwnershipResidual.Watts));

            if (step % StepsPerSecond == 0)
            {
                CaptureHealthySample(engine, step, samples, nodeSamples);
            }
            if (step % 10_000 == 0)
            {
                AppendProgress($"RL-H1 logical-step={step}/{HealthySteps}; simulated-seconds={step / StepsPerSecond}");
            }
        }

        leg.Stop();
        var sentinelRows = new List<HealthyWindowResult>();
        foreach (var windowEnd in new[] { 300, 600, 900 })
        {
            var window = samples.Where(item => item.SimulatedSeconds >= windowEnd - 60d && item.SimulatedSeconds <= windowEnd).ToArray();
            Assert.Equal(61, window.Length);
            var nodeWindow = nodeSamples.Where(item => item.SimulatedSeconds >= windowEnd - 60d && item.SimulatedSeconds <= windowEnd).ToArray();
            var maxAbsNodeSlope = HealthyNodeIds.Max(nodeId => Math.Abs(Slope(
                nodeWindow.Where(item => item.NodeId == nodeId).ToArray(), static item => item.MassKilograms)));
            var row = new HealthyWindowResult(
                windowEnd,
                window.Min(static item => item.ElectricalExportMegawatts),
                window.Max(static item => item.ElectricalExportMegawatts),
                window.Min(static item => item.PrimaryPumpFlowKilogramsPerSecond),
                window.Max(static item => item.PrimaryPumpFlowKilogramsPerSecond),
                window.Min(static item => item.DrumLevelFraction),
                window.Max(static item => item.DrumLevelFraction),
                window.Min(static item => item.GovernorOutputPercent),
                window.Max(static item => item.GovernorOutputPercent),
                window.Min(static item => item.MoistureDrainKilogramsPerSecond),
                window.Max(static item => item.TransferMismatchKilogramsPerSecond),
                window.Max(static item => item.StageEnergyOwnershipResidualWatts),
                maxAbsNodeSlope,
                window.Max(static item => Math.Abs(item.NetExternalPowerMegawatts)));
            sentinelRows.Add(row);
        }

        var sentinelsPass = sentinelRows.All(static row => row.Passes);
        var conservationPass = maxMassClosure <= MaximumMassClosureResidualKilograms
            && maxEnergyClosure <= MaximumEnergyClosureResidualJoules
            && maxBalanceMassRate <= MaximumBalanceMassRateResidualKilogramsPerSecond
            && maxBalancePower <= MaximumBalancePowerResidualWatts;
        var telemetry = telemetryProbe.Snapshot();
        var telemetryPass = telemetry.ObservedSteps == HealthySteps
            && telemetry.FourNodeTelemetrySteps == HealthySteps
            && telemetry.RollbackSteps == 0
            && telemetry.ExplicitFallbackSteps == 0
            && telemetry.FallbackCommitViolations == 0
            && telemetry.UnsafeCommitViolations == 0
            && telemetry.UntargetedBranchDisagreementSteps == 0;
        var pass = unexpectedTrips == 0
            && unexpectedFaultActivations == 0
            && nonFiniteObservations == 0
            && envelopeExcursions == 0
            && minimumMoistureDrain >= 0.3d
            && maximumTransferMismatch <= 1e-8d
            && maximumStageOwnershipResidual <= 1e-3d
            && conservationPass
            && telemetryPass
            && sentinelsPass;

        WriteConservation("RL-H1", maxMassClosure, maxEnergyClosure, maxBalanceMassRate, maxBalancePower, conservationPass);
        WriteHealthySentinels(sentinelRows);
        WriteTelemetry(telemetry);
        WriteTripFaultProtection("RL-H1", 0, 0, 0, unexpectedTrips, unexpectedFaultActivations, nonFiniteObservations, envelopeExcursions,
            $"exact-v9 healthy; minimum-moisture-drain={minimumMoistureDrain:G17}; max-transfer-mismatch={maximumTransferMismatch:G17}; max-stage-ownership-residual={maximumStageOwnershipResidual:G17}");
        WritePerformance("RL-H1", HealthySteps, 900, leg.Elapsed, string.Empty, pass);
        WriteLegSummary("RL-H1", 900, HealthySteps, pass,
            $"trips={unexpectedTrips}; unexpected-faults={unexpectedFaultActivations}; nonfinite={nonFiniteObservations}; envelope={envelopeExcursions}; sentinels={sentinelsPass}; conservation={conservationPass}; telemetry={telemetryPass}");

        if (!pass)
        {
            throw new InvalidOperationException("RL-H1 exact-v9 healthy replacement soak failed.");
        }
    }

    private static void RunMissionLeg(Stopwatch campaign)
    {
        var leg = Stopwatch.StartNew();
        var pack = ProductionOperationalChallengePack.BoundedDemandFollowing;
        Assert.Equal("bounded-demand-following-5-10-5@3", pack.ExactId);
        Assert.Equal(new InitialConditionReference("integrated-operations-desktop-stable", 9), pack.Scenario.InitialCondition);
        var session = CreateExactV9Factory().Load(pack.Scenario);
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
        var duplicateTimelineRows = 0;
        var maxLifecycleSpine = 0;
        var maxRecentOperationalEvidence = 0;
        ChallengeLifecycleState finalLifecycle = source.Current.LifecycleState;
        long? terminalStep = null;
        var windowTicks = new long[8];
        var windowCalls = new int[8];
        var evidenceRows = new List<string>
        {
            "logical_step,lifecycle,external_demand_mwe,requested_load_mwe,actual_output_mwe,demand_output_error_mwe,next_change_step,next_demand_mwe,timeline_count,lifecycle_spine_count,recent_operational_count,score,grade"
        };

        for (var index = 0; index < MissionSteps; index++)
        {
            CheckDeadline(campaign);
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
                && missionBefore.LogicalStep >= 3_000
                && !lowerDispatched)
            {
                session.CommandDispatcher.Dispatch(new ControlRoomCommand(
                    ControlRoomCommandKind.GeneratorLoadLower,
                    generatorId,
                    ControlRoomCommandTargetKind.Generator));
                lowerDispatched = true;
                observedReturnFiveDemand = true;
            }

            Step(session);
            var snapshot = session.Coordinator.Current;
            var timingStart = Stopwatch.GetTimestamp();
            var mission = source.Current;
            var timingEnd = Stopwatch.GetTimestamp();
            var windowIndex = Math.Min(7, checked((int)((snapshot.LogicalStep - 1) / 6_000)));
            windowTicks[windowIndex] += timingEnd - timingStart;
            windowCalls[windowIndex]++;

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
            if (snapshot.LogicalStep % 6_000 == 0)
            {
                AppendProgress($"RL-M1 logical-step={snapshot.LogicalStep}/{MissionSteps}; simulated-seconds={snapshot.LogicalStep / StepsPerSecond}");
            }
        }

        leg.Stop();
        WriteAllLines("08-mission-demand-score-evidence.csv", evidenceRows);
        var acceptedKinds = session.OperatorActions.Actions.Select(static item => item.Command.Kind).ToArray();
        var windowWallSeconds = windowTicks.Select(static ticks => ticks / (double)Stopwatch.Frequency).ToArray();
        var lateToEarlyRatio = windowWallSeconds[0] > 0d ? windowWallSeconds[^1] / windowWallSeconds[0] : double.PositiveInfinity;
        var performancePass = windowCalls.All(static calls => calls == 6_000)
            && windowWallSeconds.All(static seconds => double.IsFinite(seconds) && seconds > 0d)
            && lateToEarlyRatio <= MaximumMissionLateToEarlyWallRatio;
        for (var window = 0; window < 8; window++)
        {
            WritePerformance(
                $"RL-M1-W{window + 1}",
                windowCalls[window],
                60,
                TimeSpan.FromSeconds(windowWallSeconds[window]),
                FormattableString.Invariant($"projection-window={window + 1}; late-to-early={lateToEarlyRatio:G17}"),
                performancePass);
        }

        var postTerminalSteps = terminalStep.HasValue ? MissionSteps - terminalStep.Value : -1L;
        var pass = raiseDispatched
            && lowerDispatched
            && observedTenDemand
            && observedReturnFiveDemand
            && acceptedKinds.Contains(ControlRoomCommandKind.GeneratorLoadRaise)
            && acceptedKinds.Contains(ControlRoomCommandKind.GeneratorLoadLower)
            && unexpectedTrips == 0
            && unexpectedFaultActivations == 0
            && nonFiniteObservations == 0
            && duplicateTimelineRows == 0
            && maxLifecycleSpine <= 32
            && maxRecentOperationalEvidence <= 100
            && finalLifecycle != ChallengeLifecycleState.Failed
            && terminalStep.HasValue
            && terminalStep.Value <= 8_000
            && postTerminalSteps >= 40_000
            && performancePass;

        WriteTripFaultProtection("RL-M1", 0, 0, 0, unexpectedTrips, unexpectedFaultActivations, nonFiniteObservations, 0,
            $"final-lifecycle={finalLifecycle}; terminal-step={terminalStep?.ToString(CultureInfo.InvariantCulture) ?? "none"}; post-terminal-steps={postTerminalSteps}");
        WriteEvidenceGrowth("RL-M1", 0, 0, session.OperatorActions.Actions.Count, session.AutomationIntents.Intents.Count, 0, 0, 0, maxLifecycleSpine, maxRecentOperationalEvidence, duplicateTimelineRows, pass);
        WritePerformance("RL-M1", MissionSteps, 480, leg.Elapsed, FormattableString.Invariant($"projection-late-to-early={lateToEarlyRatio:G17}"), pass);
        WriteLegSummary("RL-M1", 480, MissionSteps, pass,
            $"final-lifecycle={finalLifecycle}; terminal-step={terminalStep}; post-terminal={postTerminalSteps}; raise={raiseDispatched}; lower={lowerDispatched}; trips={unexpectedTrips}; unexpected-faults={unexpectedFaultActivations}; duplicate-timeline={duplicateTimelineRows}; lifecycle-cap={maxLifecycleSpine}; recent-cap={maxRecentOperationalEvidence}; projection-late-to-early={lateToEarlyRatio:G17}");

        if (!pass)
        {
            throw new InvalidOperationException("RL-M1 mission @3 continuation/scalability replacement leg failed.");
        }
    }

    private static void RunDegradedLeg(Stopwatch campaign)
    {
        const int activationStep = 9_000;
        const int clearStep = 15_000;
        var leg = Stopwatch.StartNew();
        var production = DesktopIntegratedOperationsProductionProgram.M10FinalExactV9ProductionScenario;
        var scenario = new ScenarioDefinition(
            "m10-final-replacement-long-degraded-measurement-v9",
            "M10 final exact-v9 replacement-long degraded measurement",
            "Validation-only exact-v9 composition reusing the canonical unavailable power measurement fault seam.",
            production.InitialCondition,
            production.Objectives,
            production.AllowedOperatorActions,
            new[]
            {
                new ScenarioFaultDefinition(
                    "m10-final-replacement-long-power-unavailable-v9",
                    InstrumentationControlFaultTypeIds.SensorUnavailable,
                    "power",
                    ScenarioFaultTriggerDefinition.AtLogicalStep(activationStep),
                    ScenarioFaultTriggerDefinition.AtLogicalStep(clearStep)),
            });

        var session = CreateExactV9Factory().Load(scenario);
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

        for (var index = 0; index < DegradedSteps; index++)
        {
            CheckDeadline(campaign);
            Step(session);
            var snapshot = session.Coordinator.Current;
            var automation = session.PlantControlAuthority.CurrentAutomation;
            var fault = Assert.Single(snapshot.Faults.Faults);
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
            if (snapshot.LogicalStep % 5_000 == 0)
            {
                AppendProgress($"RL-D1 logical-step={snapshot.LogicalStep}/{DegradedSteps}; simulated-seconds={snapshot.LogicalStep / StepsPerSecond}");
            }
        }

        leg.Stop();
        var finalAutomation = session.PlantControlAuthority.CurrentAutomation;
        var finalFault = Assert.Single(session.Coordinator.Current.Faults.Faults);
        var pass = firstFaultActive == activationStep
            && firstFaultCleared == clearStep
            && firstDegradedAuthority.HasValue
            && firstDegradedAuthority.Value >= activationStep
            && firstDegradedAuthority.Value <= activationStep + 1
            && firstRecoveredAuthority.HasValue
            && firstRecoveredAuthority.Value >= clearStep
            && firstRecoveredAuthority.Value <= clearStep + 1
            && degradedSteps > 0
            && normalPostRecoverySteps >= DegradedSteps - clearStep - 1
            && session.Coordinator.Current.InvalidMeasuredSignalCount == baselineInvalid
            && finalFault.Lifecycle == ScenarioFaultLifecycleState.Cleared
            && finalAutomation.RequestedAuthority == PlantControlAuthorityMode.SupervisoryAutomatic
            && finalAutomation.EffectiveAuthority == PlantControlAuthorityMode.SupervisoryAutomatic
            && finalAutomation.Health == PlantControlAuthorityHealth.Normal
            && unexpectedTrips == 0
            && unexpectedFaults == 0
            && nonFiniteObservations == 0;

        WriteTripFaultProtection("RL-D1", 1, 1, 0, unexpectedTrips, unexpectedFaults, nonFiniteObservations, 0,
            $"fault-active={firstFaultActive}; fault-clear={firstFaultCleared}; degraded-authority={firstDegradedAuthority}; recovered-authority={firstRecoveredAuthority}; degraded-steps={degradedSteps}; post-recovery-steps={normalPostRecoverySteps}");
        WritePerformance("RL-D1", DegradedSteps, 300, leg.Elapsed, string.Empty, pass);
        WriteLegSummary("RL-D1", 300, DegradedSteps, pass,
            $"fault-active={firstFaultActive}; fault-clear={firstFaultCleared}; degraded-authority={firstDegradedAuthority}; recovered-authority={firstRecoveredAuthority}; trips={unexpectedTrips}; unexpected-faults={unexpectedFaults}; nonfinite={nonFiniteObservations}");

        if (!pass)
        {
            throw new InvalidOperationException("RL-D1 exact-v9 degraded/recovery replacement leg failed.");
        }
    }

    private static void RunProtectionLeg(Stopwatch campaign)
    {
        const int scramStep = 6_000;
        const int authorityObservationStep = 6_001;
        const int blockedCommandStep = 7_500;
        const int manualTakeoverStep = 12_000;
        var leg = Stopwatch.StartNew();
        var session = CreateExactV9Factory().Load(DesktopIntegratedOperationsProductionProgram.M10FinalExactV9ProductionScenario);
        ConfigureSupervisory(session);
        var unexpectedProtectionEvents = 0;
        var scramObserved = false;
        var suspendedObserved = false;
        var blockedCommandPreservedScram = false;
        var manualTakeoverObserved = false;
        var unexpectedFaultActivations = 0;
        var nonFiniteObservations = 0;

        for (var nextStep = 1; nextStep <= ProtectionSteps; nextStep++)
        {
            CheckDeadline(campaign);
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
            var generator = Assert.Single(snapshot.Electrical.Generators);
            if (generator.ElectricalOutput.NumericValue.HasValue && !double.IsFinite(generator.ElectricalOutput.NumericValue.Value))
            {
                nonFiniteObservations++;
            }
            if (snapshot.LogicalStep % 3_000 == 0)
            {
                AppendProgress($"RL-P1 logical-step={snapshot.LogicalStep}/{ProtectionSteps}; simulated-seconds={snapshot.LogicalStep / StepsPerSecond}");
            }
        }

        leg.Stop();
        var finalSnapshot = session.Coordinator.Current;
        var pass = unexpectedProtectionEvents == 0
            && scramObserved
            && suspendedObserved
            && blockedCommandPreservedScram
            && manualTakeoverObserved
            && finalSnapshot.ReactorScramActive
            && unexpectedFaultActivations == 0
            && nonFiniteObservations == 0;

        WriteTripFaultProtection("RL-P1", 0, 0, 1, unexpectedProtectionEvents, unexpectedFaultActivations, nonFiniteObservations, 0,
            $"scram-step={scramStep}; authority-suspended-step={authorityObservationStep}; blocked-command-step={blockedCommandStep}; manual-takeover-step={manualTakeoverStep}; scram-final={finalSnapshot.ReactorScramActive}");
        WritePerformance("RL-P1", ProtectionSteps, 180, leg.Elapsed, string.Empty, pass);
        WriteLegSummary("RL-P1", 180, ProtectionSteps, pass,
            $"scram={scramObserved}; suspended={suspendedObserved}; blocked-command-preserved-scram={blockedCommandPreservedScram}; manual-takeover={manualTakeoverObserved}; unexpected-protection={unexpectedProtectionEvents}; unexpected-faults={unexpectedFaultActivations}; nonfinite={nonFiniteObservations}");

        if (!pass)
        {
            throw new InvalidOperationException("RL-P1 exact-v9 protection/takeover replacement leg failed.");
        }
    }

    private static void RunReplayLeg(Stopwatch campaign)
    {
        const int loadRaiseStep = 500;
        const int checkpointStep = 3_000;
        const int loadLowerStep = 3_000;
        const int rodHoldStep = 4_000;
        var leg = Stopwatch.StartNew();
        var pack = ProductionOperationalChallengePack.BoundedDemandFollowing;
        Assert.Equal("bounded-demand-following-5-10-5@3", pack.ExactId);
        var factory = CreateExactV9Factory();
        var session = factory.Load(pack.Scenario);
        using var recorder = new ScenarioRecorder(session);
        ConfigureSupervisory(session);
        var generatorId = session.Coordinator.Current.Electrical.Generators.Single().GeneratorId;
        ScenarioCheckpoint? checkpoint = null;
        var unexpectedTrips = 0;
        var unexpectedFaultActivations = 0;
        var nonFiniteObservations = 0;

        for (var nextStep = 1; nextStep <= ReplaySteps; nextStep++)
        {
            CheckDeadline(campaign);
            if (nextStep == loadRaiseStep)
            {
                session.CommandDispatcher.Dispatch(new ControlRoomCommand(
                    ControlRoomCommandKind.GeneratorLoadRaise,
                    generatorId,
                    ControlRoomCommandTargetKind.Generator));
            }
            if (nextStep == loadLowerStep)
            {
                session.CommandDispatcher.Dispatch(new ControlRoomCommand(
                    ControlRoomCommandKind.GeneratorLoadLower,
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
            Step(session);
            var current = session.Coordinator.Current;
            if (current.AnyTripActive)
            {
                unexpectedTrips++;
            }
            unexpectedFaultActivations = Math.Max(unexpectedFaultActivations, current.Faults.ActiveCount);
            var currentGenerator = Assert.Single(current.Electrical.Generators);
            var finiteValues = new[]
            {
                current.ReactorCore.ReactorThermalPower.NumericValue,
                currentGenerator.RequestedElectricalPower.NumericValue,
                currentGenerator.ElectricalOutput.NumericValue,
            };
            nonFiniteObservations += finiteValues.Count(static value => value.HasValue && !double.IsFinite(value.Value));
            if (current.LogicalStep == checkpointStep)
            {
                checkpoint = recorder.CreateCheckpoint("m10-final-replacement-long-r1-prefix");
            }
            if (current.LogicalStep % 1_000 == 0)
            {
                AppendProgress($"RL-R1 authored logical-step={current.LogicalStep}/{ReplaySteps}; simulated-seconds={current.LogicalStep / StepsPerSecond}");
            }
        }

        var recording = recorder.Complete();
        Assert.NotNull(checkpoint);
        var archive = ScenarioSessionArchive.FromRecording("m10-final-replacement-long-r1", pack.Scenario, recording);
        var finalFingerprint = ControlRoomSnapshotFingerprint.Compute(session.Coordinator.Current);
        var projection = OperationalChallengeRecordingProjector.Project(pack, recording);
        var runner = new ScenarioFullReplayRunner(factory);

        CheckDeadline(campaign);
        var replay = runner.ReplayAndVerify(archive);
        CheckDeadline(campaign);
        var replayFingerprint = ControlRoomSnapshotFingerprint.Compute(replay.Session.Coordinator.Current);
        var replayProjection = OperationalChallengeRecordingProjector.Project(pack, replay.ReplayedRecording);

        var restored = runner.SeekAndVerify(archive, checkpoint!.CheckpointId);
        using var continuationRecorder = new ScenarioRecorder(restored.Session, restored.ReplayedRecording);
        for (var nextStep = checkpointStep + 1; nextStep <= ReplaySteps; nextStep++)
        {
            CheckDeadline(campaign);
            if (nextStep == rodHoldStep)
            {
                restored.Session.CommandDispatcher.Dispatch(new ControlRoomCommand(
                    ControlRoomCommandKind.ControlRodHold,
                    "regulating",
                    ControlRoomCommandTargetKind.ControlRodGroup));
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
        var frameGrowthPass = recording.Frames.Count == ReplaySteps + 1
            && archive.Frames.Count == ReplaySteps + 1
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

        leg.Stop();
        WriteReplaySentinel(finalFingerprint, replayFingerprint, continuationFingerprint,
            projection.DeterministicFingerprint, replayProjection.DeterministicFingerprint, continuationProjection.DeterministicFingerprint,
            checkpointStep, archive.SchemaVersion, exactRecordingPass, pass);
        WriteEvidenceGrowth("RL-R1", recording.Frames.Count, recording.Events.Count, recording.OperatorActions.Count,
            recording.AutomationIntents.Count, recording.Checkpoints.Count, halfBytes, fullBytes, 0, 0, 0, frameGrowthPass && sizeGrowthPass);
        WriteTripFaultProtection("RL-R1", 0, 0, 0, unexpectedTrips, unexpectedFaultActivations, nonFiniteObservations, 0, "exact-v9 mission @3 replay/checkpoint sentinel");
        WritePerformance("RL-R1", ReplaySteps, 60, leg.Elapsed, FormattableString.Invariant($"archive-size-ratio={sizeRatio:G17}; replay-extra-physical-work-included-in-wall=true"), pass);
        WriteLegSummary("RL-R1", 60, ReplaySteps, pass,
            $"frames={recording.Frames.Count}; events={recording.Events.Count}; actions={recording.OperatorActions.Count}; intents={recording.AutomationIntents.Count}; checkpoints={recording.Checkpoints.Count}; half-bytes={halfBytes}; full-bytes={fullBytes}; size-ratio={sizeRatio:G17}; exact-recording={exactRecordingPass}; trips={unexpectedTrips}; unexpected-faults={unexpectedFaultActivations}; nonfinite={nonFiniteObservations}");

        if (!pass)
        {
            throw new InvalidOperationException("RL-R1 exact-v9 replay/checkpoint replacement sentinel failed.");
        }
    }

    private static void CaptureHealthySample(
        IntegratedAutomaticOperationRuntimeEngine engine,
        int logicalStep,
        List<HealthySample> samples,
        List<NodeMassSample> nodeSamples)
    {
        var protectedControl = engine.LatestCanonicalSnapshot.Control.ProtectedControl;
        var fullPlant = protectedControl.FullPlant;
        var cycle = fullPlant.IntegratedCycle;
        var primary = cycle.PrimaryCircuit;
        var plant = fullPlant.CandidatePlant;
        var drum = Assert.Single(primary.SteamDrums.Drums);
        var stage = Assert.Single(cycle.TurbineExpansion.StageGroups);
        var speed = protectedControl.TurbineSecondary.ControlAndActuator.Controllers.GetDiagnostic("speed-control");
        var presentation = engine.CreatePresentationSnapshot(ControlRoomRunState.Paused);
        var generator = Assert.Single(presentation.Electrical.Generators);
        var transferMismatch = Math.Abs(stage.TotalTransferredMassFlowRate.KilogramsPerSecond - stage.CommandedMassFlowRate.KilogramsPerSecond);

        samples.Add(new HealthySample(
            logicalStep / (double)StepsPerSecond,
            generator.ElectricalOutput.NumericValue ?? double.NaN,
            primary.MainCirculation.TotalPumpMassFlowRate.KilogramsPerSecond,
            drum.LiquidLevelFraction.Fraction,
            speed.Output,
            stage.MoistureDrainMassFlowRate.KilogramsPerSecond,
            transferMismatch,
            Math.Abs(stage.TurbineEnergyOwnershipResidual.Watts),
            cycle.HeatBalance.NetReactorToGridExternalPower.Megawatts));

        foreach (var nodeId in HealthyNodeIds)
        {
            nodeSamples.Add(new NodeMassSample(
                logicalStep / (double)StepsPerSecond,
                nodeId,
                plant.GetFluidNode(nodeId).Mass.Kilograms));
        }
    }

    private static double Slope<T>(IReadOnlyList<T> rows, Func<T, double> selector) where T : ITimeSample
    {
        if (rows.Count < 2)
        {
            return double.NaN;
        }
        var meanTime = rows.Average(static item => item.SimulatedSeconds);
        var meanValue = rows.Average(selector);
        var numerator = 0d;
        var denominator = 0d;
        foreach (var row in rows)
        {
            var dx = row.SimulatedSeconds - meanTime;
            numerator += dx * (selector(row) - meanValue);
            denominator += dx * dx;
        }
        return denominator > 0d ? numerator / denominator : double.NaN;
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

    private static ScenarioSessionFactory CreateExactV9Factory()
        => new(new VersionedInitialConditionRegistry(new IVersionedInitialConditionFactory[]
        {
            new DesktopSustainedGenerationPostMoistureEquilibriumCandidateInitialConditionFactory(),
        }));

    private static void ConfigureSupervisory(ScenarioSession session)
    {
        session.PlantControlAuthority.RequestSupervisoryObjective(SupervisoryObjectiveRequest.HoldCurrentOperatingPoint());
        session.PlantControlAuthority.RequestAuthority(PlantControlAuthorityMode.SupervisoryAutomatic);
    }

    private static void Step(ScenarioSession session)
        => session.CommandDispatcher.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.SingleStep));

    private static bool ExecuteLeg(string legId, int plannedSteps, Stopwatch campaign, ICollection<string> failures, Action action)
    {
        try
        {
            CheckDeadline(campaign);
            action();
            return true;
        }
        catch (Exception exception)
        {
            EnsureFailureLegSummary(legId, plannedSteps, exception);
            if (!HasClassificationRow(legId))
            {
                WriteFailureClassification(legId, exception);
            }
            failures.Add($"{legId}:{exception.GetType().Name}:{exception.Message}");
            return false;
        }
    }

    private static void CheckDeadline(Stopwatch campaign)
    {
        if (campaign.Elapsed.TotalMinutes > HardCampaignCapMinutes)
        {
            throw new TimeoutException(FormattableString.Invariant(
                $"Replacement long exceeded the frozen {HardCampaignCapMinutes:G17} minute wall-clock campaign cap."));
        }
    }

    private static void RequireOptIn()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable(OptInEnvironmentVariable), "1", StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"{OptInEnvironmentVariable}=1 is required for the explicit M10 Final replacement long.");
        }
    }

    private static void AppendProgress(string text)
    {
        Directory.CreateDirectory(ReportDirectory());
        File.AppendAllText(Path.Combine(ReportDirectory(), "00-progress.txt"), text + Environment.NewLine, Utf8WithoutBom);
    }

    private static void WriteLegSummary(string legId, int simulatedSeconds, int logicalSteps, bool pass, string details)
    {
        var path = Path.Combine(ReportDirectory(), "03-leg-summary.csv");
        EnsureHeader(path, "leg_id,simulated_seconds,logical_steps,passes,details");
        File.AppendAllText(path, $"{legId},{simulatedSeconds},{logicalSteps},{pass},{Csv(details)}{Environment.NewLine}", Utf8WithoutBom);
    }

    private static void EnsureFailureLegSummary(string legId, int plannedSteps, Exception exception)
    {
        var path = Path.Combine(ReportDirectory(), "03-leg-summary.csv");
        if (File.Exists(path) && File.ReadLines(path).Skip(1).Any(line => line.StartsWith(legId + ",", StringComparison.Ordinal)))
        {
            return;
        }
        WriteLegSummary(legId, 0, 0, false,
            $"planned-seconds={plannedSteps / StepsPerSecond}; planned-steps={plannedSteps}; failure={exception.GetType().FullName}: {exception.Message}");
    }

    private static bool HasClassificationRow(string legId)
    {
        var path = Path.Combine(ReportDirectory(), "07-trip-fault-protection-classification.csv");
        return File.Exists(path)
            && File.ReadLines(path).Skip(1).Any(line => line.StartsWith(legId + ",", StringComparison.Ordinal));
    }

    private static void WriteFailureClassification(string legId, Exception exception)
    {
        var path = Path.Combine(ReportDirectory(), "07-trip-fault-protection-classification.csv");
        EnsureHeader(path, "leg_id,fault_activations,fault_clears,expected_protection_events,unexpected_trip_or_protection_count,unexpected_fault_activations,nonfinite_observations,envelope_excursions,classification");
        var prefix = exception.GetType().Namespace?.StartsWith("Xunit", StringComparison.Ordinal) == true
            ? "ASSERTION "
            : "UNHANDLED ";
        File.AppendAllText(path,
            $"{legId},0,0,0,0,0,0,{(exception is WaterSteamStateOutOfRangeException ? 1 : 0)},{Csv(prefix + exception.GetType().FullName + ": " + exception.Message)}{Environment.NewLine}",
            Utf8WithoutBom);
    }

    private static void WriteConservation(string legId, double mass, double energy, double massRate, double power, bool pass)
    {
        var path = Path.Combine(ReportDirectory(), "04-conservation-maxima.csv");
        EnsureHeader(path, "leg_id,max_mass_closure_kg,max_energy_closure_j,max_balance_mass_rate_kg_s,max_balance_power_w,passes");
        File.AppendAllText(path, FormattableString.Invariant(
            $"{legId},{mass:G17},{energy:G17},{massRate:G17},{power:G17},{pass}{Environment.NewLine}"), Utf8WithoutBom);
    }

    private static void WriteHealthySentinels(IEnumerable<HealthyWindowResult> rows)
    {
        var lines = new List<string>
        {
            "window_end_seconds,min_electrical_mwe,max_electrical_mwe,min_primary_pump_kg_s,max_primary_pump_kg_s,min_drum_level,max_drum_level,min_governor_output_percent,max_governor_output_percent,min_moisture_drain_kg_s,max_transfer_mismatch_kg_s,max_stage_energy_ownership_residual_w,max_abs_node_mass_slope_kg_s,max_abs_net_external_power_mw,passes"
        };
        lines.AddRange(rows.Select(static row => FormattableString.Invariant(
            $"{row.WindowEndSeconds},{row.MinElectricalMegawatts:G17},{row.MaxElectricalMegawatts:G17},{row.MinPrimaryPumpKilogramsPerSecond:G17},{row.MaxPrimaryPumpKilogramsPerSecond:G17},{row.MinDrumLevel:G17},{row.MaxDrumLevel:G17},{row.MinGovernorOutputPercent:G17},{row.MaxGovernorOutputPercent:G17},{row.MinMoistureDrainKilogramsPerSecond:G17},{row.MaxTransferMismatchKilogramsPerSecond:G17},{row.MaxStageEnergyOwnershipResidualWatts:G17},{row.MaxAbsoluteNodeMassSlopeKilogramsPerSecond:G17},{row.MaxAbsoluteNetExternalPowerMegawatts:G17},{row.Passes}")));
        WriteAllLines("05-healthy-window-v9-operating-point-sentinels.csv", lines);
    }

    private static void WriteTelemetry(FourNodeProductionActivationTelemetrySnapshot telemetry)
    {
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
        File.AppendAllText(path,
            $"{legId},{faultActivations},{faultClears},{expectedProtection},{unexpected},{unexpectedFaultActivations},{nonFiniteObservations},{envelope},{Csv(classification)}{Environment.NewLine}", Utf8WithoutBom);
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
        File.AppendAllText(path, FormattableString.Invariant(
            $"{legId},{frames},{events},{actions},{intents},{checkpoints},{halfBytes},{fullBytes},{ratio:G17},{lifecycleSpine},{recentOperational},{duplicateTimeline},{pass}{Environment.NewLine}"), Utf8WithoutBom);
    }

    private static void WritePerformance(string scope, int steps, int simulatedSeconds, TimeSpan elapsed, string details, bool pass)
    {
        var path = Path.Combine(ReportDirectory(), "11-performance-diagnostics.csv");
        EnsureHeader(path, "scope,logical_steps,simulated_seconds,wall_seconds,steps_per_wall_second,details,passes");
        var rate = elapsed.TotalSeconds > 0d ? steps / elapsed.TotalSeconds : double.PositiveInfinity;
        File.AppendAllText(path, FormattableString.Invariant(
            $"{scope},{steps},{simulatedSeconds},{elapsed.TotalSeconds:G17},{rate:G17},{Csv(details)},{pass}{Environment.NewLine}"), Utf8WithoutBom);
    }

    private static void WriteWallBudget(TimeSpan elapsed, bool pass)
    {
        WriteAllLines("12-wall-budget-summary.txt", new[]
        {
            "=== M10 Final exact-v9 replacement-long wall budget ===",
            FormattableString.Invariant($"authored-simulated-seconds={TotalAuthoredSeconds}; authored-logical-steps={TotalAuthoredSteps};"),
            FormattableString.Invariant($"campaign-wall-seconds={elapsed.TotalSeconds:G17}; campaign-wall-minutes={elapsed.TotalMinutes:G17};"),
            "target-workstation-minutes=35-45;",
            FormattableString.Invariant($"hard-campaign-cap-minutes={HardCampaignCapMinutes:G17}; hard-cap-is-validation-job-policy-not-physics-tolerance=True;"),
            $"wall-deadline-exceeded={!pass}; wall-budget-pass={pass};",
        });
        WritePerformance("CAMPAIGN", TotalAuthoredSteps, TotalAuthoredSeconds, elapsed, "includes replay/checkpoint extra physical work", pass);
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
        => Path.Combine(FindRepositoryRoot(), "artifacts", "m10-final-replacement-long-validation");

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null && !File.Exists(Path.Combine(current.FullName, "NuclearReactorSimulator.sln")))
        {
            current = current.Parent;
        }
        return current?.FullName ?? throw new DirectoryNotFoundException("Repository root could not be resolved for M10 Final replacement long validation.");
    }

    private interface ITimeSample
    {
        double SimulatedSeconds { get; }
    }

    private sealed record HealthySample(
        double SimulatedSeconds,
        double ElectricalExportMegawatts,
        double PrimaryPumpFlowKilogramsPerSecond,
        double DrumLevelFraction,
        double GovernorOutputPercent,
        double MoistureDrainKilogramsPerSecond,
        double TransferMismatchKilogramsPerSecond,
        double StageEnergyOwnershipResidualWatts,
        double NetExternalPowerMegawatts) : ITimeSample;

    private sealed record NodeMassSample(
        double SimulatedSeconds,
        string NodeId,
        double MassKilograms) : ITimeSample;

    private sealed record HealthyWindowResult(
        int WindowEndSeconds,
        double MinElectricalMegawatts,
        double MaxElectricalMegawatts,
        double MinPrimaryPumpKilogramsPerSecond,
        double MaxPrimaryPumpKilogramsPerSecond,
        double MinDrumLevel,
        double MaxDrumLevel,
        double MinGovernorOutputPercent,
        double MaxGovernorOutputPercent,
        double MinMoistureDrainKilogramsPerSecond,
        double MaxTransferMismatchKilogramsPerSecond,
        double MaxStageEnergyOwnershipResidualWatts,
        double MaxAbsoluteNodeMassSlopeKilogramsPerSecond,
        double MaxAbsoluteNetExternalPowerMegawatts)
    {
        public bool Passes
            => MinElectricalMegawatts >= 4.99d
                && MaxElectricalMegawatts <= 5.01d
                && MinPrimaryPumpKilogramsPerSecond >= 99.9d
                && MaxPrimaryPumpKilogramsPerSecond <= 100.1d
                && MinDrumLevel >= 0.49d
                && MaxDrumLevel <= 0.51d
                && MinGovernorOutputPercent >= 29.27d
                && MaxGovernorOutputPercent <= 29.3d
                && MinMoistureDrainKilogramsPerSecond >= 0.3d
                && MaxTransferMismatchKilogramsPerSecond <= 1e-8d
                && MaxStageEnergyOwnershipResidualWatts <= 1e-3d
                && MaxAbsoluteNodeMassSlopeKilogramsPerSecond <= MaximumNodeMassSlopeKilogramsPerSecond
                && MaxAbsoluteNetExternalPowerMegawatts <= MaximumLateNetExternalPowerMegawatts;
    }
}
