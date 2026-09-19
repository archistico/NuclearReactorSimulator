using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.ControlRoom.MissionPerformance;
using NuclearReactorSimulator.Application.Scenarios;
using NuclearReactorSimulator.Application.Scenarios.Challenges.Packs;
using NuclearReactorSimulator.Application.Scenarios.Recording;
using NuclearReactorSimulator.Application.Scenarios.Training;
using NuclearReactorSimulator.Simulation.Plant;
using NuclearReactorSimulator.Simulation.Physics.Control.Integration;
using NuclearReactorSimulator.Simulation.Physics.TurbineIsland.MainSteam;
using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Physics.TurbineIsland.Turbine;
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
    private const int StageCausalStep = 126;
    private const string FrozenDeterminismFingerprint = "7880AD580179B936C584EB0055BE663E0A1CFA65C5191B0DB8A7F3C514DB5418";
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

        var selectorTrace = DeterminismTrace(useSelector: true);
        var directTrace = DeterminismTrace(useSelector: false);
        WriteExactV9CrossHostTransitiveDiagnostic(selectorTrace, directTrace);
        var selectorFingerprint = selectorTrace.AggregateFingerprint;
        var directFingerprint = directTrace.AggregateFingerprint;
        Assert.Equal(directFingerprint, selectorFingerprint);
        Assert.Equal(FrozenDeterminismFingerprint, selectorFingerprint);

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

    private static DeterminismTraceCapture DeterminismTrace(bool useSelector)
    {
        var engine = useSelector
            ? Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(
                DesktopHydraulicProductionPolicySelector.CreateFactory(
                    DesktopHydraulicProductionPolicySelector.Resolve(
                        DesktopHydraulicProductionPolicySelector.AuthoritativeDefaultPolicy)).CreateRuntimeEngine())
            : Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(
                new DesktopSustainedGenerationPostMoistureEquilibriumCandidateInitialConditionFactory().CreateRuntimeEngine());

        var builder = new StringBuilder();
        var entries = new List<DeterminismTraceEntry>(DeterminismSteps);
        IntegratedAutomaticOperationSnapshot? stageCausalSnapshot = null;
        long stageCausalLogicalStep = -1;

        for (var step = 1; step <= DeterminismSteps; step++)
        {
            var snapshot = engine.Step(ControlRoomRunState.Running);
            var payload = ControlRoomSnapshotFingerprint.SerializeCanonicalPayload(snapshot);
            var fingerprint = Convert.ToHexString(SHA256.HashData(payload)).ToLowerInvariant();
            builder.Append(FormattableString.Invariant($"{step}:{fingerprint}||"));
            entries.Add(new DeterminismTraceEntry(step, snapshot.LogicalStep, fingerprint, payload));

            // Capture only the immutable canonical-snapshot reference after step 126 has already been computed and hashed.
            // All resolver traversal and arithmetic remain post-loop, preserving the Diagnostic 2 loop workload.
            if (step == StageCausalStep)
            {
                stageCausalSnapshot = engine.LatestCanonicalSnapshot;
                stageCausalLogicalStep = snapshot.LogicalStep;
            }
        }

        if (stageCausalSnapshot is null || stageCausalLogicalStep < 0)
        {
            throw new InvalidOperationException("Exact-V9 resolver causal snapshot was not captured at the frozen divergent step.");
        }

        var stageDiagnostic = BuildExactV9StageCausalDiagnostic(stageCausalSnapshot);
        var resolverDiagnostic = BuildExactV9StageMassFlowResolverDiagnostic(
            stageCausalSnapshot,
            engine.FixedDeltaTime);
        return new DeterminismTraceCapture(
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString()))),
            entries,
            new ExactV9StageCausalTraceEntry(StageCausalStep, stageCausalLogicalStep, stageDiagnostic),
            new ExactV9StageMassFlowResolverTraceEntry(StageCausalStep, stageCausalLogicalStep, resolverDiagnostic));
    }

    private static ExactV9StageCausalDiagnostic BuildExactV9StageCausalDiagnostic(
        IntegratedAutomaticOperationSnapshot canonicalSnapshot)
    {
        var fullPlant = canonicalSnapshot.Control.ProtectedControl.FullPlant;
        var turbine = fullPlant.IntegratedCycle.TurbineExpansion;
        var stage = Assert.Single(turbine.StageGroups);
        var rotor = Assert.Single(turbine.Rotors);
        var stageDefinition = turbine.Definition.GetStageGroup(stage.StageGroupId);
        var admissionBoundary = turbine.MainSteamNetwork.GetTurbineAdmissionBoundary(stage.AdmissionBoundaryId);
        var admissionTrain = turbine.MainSteamNetwork.GetAdmissionTrain(admissionBoundary.AdmissionTrainId);

        var rawThermodynamicVaporMassFraction = ResolveDiagnosticVaporMassFraction(
            stage.InletPhase,
            stage.InletVaporQuality?.Fraction);
        var resolvedAdmissionVaporMassFraction = stageDefinition.AdmissionPhasePolicy == TurbineAdmissionPhasePolicy.LegacyUnrestricted
            ? 1d
            : Math.Clamp(rawThermodynamicVaporMassFraction ?? 0d, 0d, 1d);
        var phaseLimitedFlow = stage.CommandedMassFlowRate.KilogramsPerSecond * resolvedAdmissionVaporMassFraction;
        var expectedEffectiveMassFlow = stage.TripBlocked ? 0d : phaseLimitedFlow;

        return new ExactV9StageCausalDiagnostic(
            stageDefinition.AdmissionPhasePolicy.ToString(),
            stage.TripBlocked,
            admissionTrain.TurbineInletPhase.ToString(),
            admissionTrain.TurbineInletVaporQuality?.Fraction,
            stage.InletPhase.ToString(),
            stage.InletVaporQuality?.Fraction,
            rawThermodynamicVaporMassFraction,
            resolvedAdmissionVaporMassFraction,
            stage.CommandedMassFlowRate.KilogramsPerSecond,
            stage.EffectiveMassFlowRate.KilogramsPerSecond,
            admissionBoundary.MassFlowRate.KilogramsPerSecond,
            phaseLimitedFlow,
            expectedEffectiveMassFlow,
            stage.EffectiveMassFlowRate.KilogramsPerSecond - expectedEffectiveMassFlow,
            stage.InletPressure.Pascals,
            stage.InletTemperature.Kelvins,
            stage.InletSpecificInternalEnergy.JoulesPerKilogram,
            stage.ExhaustPressure.Pascals,
            stage.ExhaustTemperature.Kelvins,
            stage.EffectiveIdealSpecificWork.JoulesPerKilogram,
            stage.ShaftTorque.NewtonMetres,
            stage.ShaftPower.Watts,
            stage.MoistureDrainMassFlowRate.KilogramsPerSecond,
            rotor.InitialAngularSpeed.RadiansPerSecond,
            rotor.FinalAngularSpeed.RadiansPerSecond,
            rotor.AverageAngularSpeed.RadiansPerSecond,
            rotor.TurbineTorque.NewtonMetres,
            rotor.CommandedExternalLoadTorque.NewtonMetres,
            rotor.EffectiveExternalLoadTorque.NewtonMetres,
            rotor.PassiveMechanicalLossTorque.NewtonMetres,
            rotor.NetTorque.NewtonMetres,
            rotor.ShaftPower.Watts);
    }

    private static ExactV9StageMassFlowResolverDiagnostic BuildExactV9StageMassFlowResolverDiagnostic(
        IntegratedAutomaticOperationSnapshot causalSnapshot,
        TimeSpan deltaTime)
    {
        var fullPlant = causalSnapshot.Control.ProtectedControl.FullPlant;
        var turbine = fullPlant.IntegratedCycle.TurbineExpansion;
        var stage = Assert.Single(turbine.StageGroups);
        var stageDefinition = turbine.Definition.GetStageGroup(stage.StageGroupId);
        var resistance = stageDefinition.ExpansionResistance
            ?? throw new InvalidOperationException("Frozen Exact-V9 stage must retain its pressure-driven expansion resistance.");
        var boundaryDefinition = turbine.MainSteamNetwork.Definition.GetTurbineAdmissionBoundary(stage.AdmissionBoundaryId);
        var trainDefinition = turbine.MainSteamNetwork.Definition.GetAdmissionTrain(boundaryDefinition.AdmissionTrainId);
        var trainSnapshot = turbine.MainSteamNetwork.GetAdmissionTrain(trainDefinition.Id);
        var postStepInlet = fullPlant.CandidatePlant.GetFluidNode(boundaryDefinition.SourceNodeId);

        var drivingPressurePa = stage.InletPressure.Pascals - stage.ExhaustPressure.Pascals;
        var expansionResistance = resistance.PascalSecondsSquaredPerKilogramSquared;
        var hydraulicCandidate = drivingPressurePa <= 0d
            ? 0d
            : Math.Sqrt(drivingPressurePa / expansionResistance);

        var stop = BuildExactV9ValveFlowDiagnostic(
            "STOP",
            turbine.Definition.PlantDefinition.GetValve(trainDefinition.StopValveId),
            trainSnapshot.StopValve);
        var control = BuildExactV9ValveFlowDiagnostic(
            "CONTROL",
            turbine.Definition.PlantDefinition.GetValve(trainDefinition.ControlValveId),
            trainSnapshot.ControlValve);
        var admission = BuildExactV9ValveFlowDiagnostic(
            "ADMISSION",
            turbine.Definition.PlantDefinition.GetValve(trainDefinition.AdmissionValveId),
            trainSnapshot.AdmissionValve);

        var admissionTrainCandidate = Math.Min(
            stop.PositiveSnapshotMassFlowKgPerS,
            Math.Min(control.PositiveSnapshotMassFlowKgPerS, admission.PositiveSnapshotMassFlowKgPerS));
        var visibleCandidate = Math.Min(hydraulicCandidate, admissionTrainCandidate);
        var observedCommanded = stage.CommandedMassFlowRate.KilogramsPerSecond;
        var drainableTiePreStepMassKg = 2d * visibleCandidate * deltaTime.TotalSeconds;
        var selectedVisibleLimiter = hydraulicCandidate <= admissionTrainCandidate
            ? "HYDRAULIC_CANDIDATE"
            : "ADMISSION_TRAIN";
        var selectedValveLimiter = stop.PositiveSnapshotMassFlowKgPerS
            <= Math.Min(control.PositiveSnapshotMassFlowKgPerS, admission.PositiveSnapshotMassFlowKgPerS)
                ? "STOP"
                : control.PositiveSnapshotMassFlowKgPerS <= admission.PositiveSnapshotMassFlowKgPerS
                    ? "CONTROL"
                    : "ADMISSION";

        return new ExactV9StageMassFlowResolverDiagnostic(
            deltaTime.TotalSeconds,
            postStepInlet.Mass.Kilograms,
            drivingPressurePa,
            expansionResistance,
            hydraulicCandidate,
            admissionTrainCandidate,
            visibleCandidate,
            observedCommanded,
            observedCommanded - visibleCandidate,
            observedCommanded - admissionTrainCandidate,
            observedCommanded - hydraulicCandidate,
            drainableTiePreStepMassKg,
            selectedVisibleLimiter,
            selectedValveLimiter,
            stop,
            control,
            admission);
    }

    private static ExactV9ValveFlowDiagnostic BuildExactV9ValveFlowDiagnostic(
        string role,
        ValveDefinition definition,
        MainSteamValveSnapshot snapshot)
    {
        var coefficientSquared = snapshot.FlowCoefficient.Fraction * snapshot.FlowCoefficient.Fraction;
        var baseResistance = definition.Pipe.Resistance.PascalSecondsSquaredPerKilogramSquared;
        var effectiveResistance = snapshot.FlowCoefficient.IsClosed
            ? double.PositiveInfinity
            : baseResistance / coefficientSquared;
        var squaredMassFlow = snapshot.FlowCoefficient.IsClosed
            ? 0d
            : Math.Abs(snapshot.PressureDifference.Pascals) / effectiveResistance;
        var massFlowMagnitude = Math.Sqrt(squaredMassFlow);
        var reconstructedSignedFlow = snapshot.PressureDifference.Pascals > 0d
            ? massFlowMagnitude
            : snapshot.PressureDifference.Pascals < 0d
                ? -massFlowMagnitude
                : 0d;
        var snapshotMassFlow = snapshot.MassFlowRate.KilogramsPerSecond;

        return new ExactV9ValveFlowDiagnostic(
            role,
            snapshot.ValveId,
            definition.Characteristic.Kind.ToString(),
            definition.Characteristic.Rangeability,
            snapshot.EffectivePosition.Fraction,
            snapshot.FlowCoefficient.Fraction,
            snapshot.PressureDifference.Pascals,
            baseResistance,
            coefficientSquared,
            effectiveResistance,
            squaredMassFlow,
            reconstructedSignedFlow,
            snapshotMassFlow,
            Math.Max(0d, snapshotMassFlow),
            snapshotMassFlow - reconstructedSignedFlow);
    }

    private static double? ResolveDiagnosticVaporMassFraction(FluidPhase phase, double? vaporQualityFraction)
        => phase switch
        {
            FluidPhase.SubcooledLiquid => 0d,
            FluidPhase.SaturatedMixture => vaporQualityFraction,
            FluidPhase.SuperheatedVapor => 1d,
            _ => null,
        };

    // NRS-MARKER:M10974-EXACT-V9-CROSS-HOST-TRANSITIVE-DIAGNOSTIC1
    private static void WriteExactV9CrossHostTransitiveDiagnostic(
        DeterminismTraceCapture selector,
        DeterminismTraceCapture direct)
    {
        var diagnosticsRoot = Environment.GetEnvironmentVariable("NRS_FINGERPRINT_V1_DIAGNOSTICS_DIR");
        if (string.IsNullOrWhiteSpace(diagnosticsRoot))
        {
            return;
        }

        var outputDirectory = Path.Combine(diagnosticsRoot, "exact-v9-transitive");
        var captureDirectory = Path.Combine(outputDirectory, "capture");
        if (Directory.Exists(captureDirectory))
        {
            Directory.Delete(captureDirectory, recursive: true);
        }
        Directory.CreateDirectory(captureDirectory);

        var selectorDirectory = Path.Combine(captureDirectory, "payloads-selector");
        var directDirectory = Path.Combine(captureDirectory, "payloads-direct");
        Directory.CreateDirectory(selectorDirectory);
        Directory.CreateDirectory(directDirectory);

        var selectorLines = new List<string> { "step\tlogicalStep\tfingerprint\tpayloadBytes" };
        var directLines = new List<string> { "step\tlogicalStep\tfingerprint\tpayloadBytes" };
        var perStepEqual = selector.Entries.Count == direct.Entries.Count;

        for (var index = 0; index < selector.Entries.Count; index++)
        {
            var selectorEntry = selector.Entries[index];
            selectorLines.Add(FormattableString.Invariant(
                $"{selectorEntry.Step}\t{selectorEntry.LogicalStep}\t{selectorEntry.Fingerprint}\t{selectorEntry.Payload.Length}"));
            File.WriteAllBytes(
                Path.Combine(selectorDirectory, $"step-{selectorEntry.Step:D3}.json"),
                selectorEntry.Payload);

            if (index >= direct.Entries.Count)
            {
                perStepEqual = false;
                continue;
            }

            var directEntry = direct.Entries[index];
            directLines.Add(FormattableString.Invariant(
                $"{directEntry.Step}\t{directEntry.LogicalStep}\t{directEntry.Fingerprint}\t{directEntry.Payload.Length}"));
            File.WriteAllBytes(
                Path.Combine(directDirectory, $"step-{directEntry.Step:D3}.json"),
                directEntry.Payload);

            if (selectorEntry.Step != directEntry.Step
                || selectorEntry.LogicalStep != directEntry.LogicalStep
                || !string.Equals(selectorEntry.Fingerprint, directEntry.Fingerprint, StringComparison.Ordinal)
                || !selectorEntry.Payload.AsSpan().SequenceEqual(directEntry.Payload))
            {
                perStepEqual = false;
            }
        }

        File.WriteAllLines(Path.Combine(captureDirectory, "sequence-selector.tsv"), selectorLines, Utf8WithoutBom);
        File.WriteAllLines(Path.Combine(captureDirectory, "sequence-direct.tsv"), directLines, Utf8WithoutBom);
        WriteExactV9StageCausalDiagnostic(captureDirectory, selector, direct);
        WriteExactV9StageMassFlowResolverDiagnostic(captureDirectory, selector, direct);

        var summaryLines = new[]
        {
            "schema=m10974-exact-v9-cross-host-transitive-determinism-diagnostic1",
            $"expected-frozen-aggregate={FrozenDeterminismFingerprint}",
            $"selector-aggregate={selector.AggregateFingerprint}",
            $"direct-aggregate={direct.AggregateFingerprint}",
            $"selector-direct-per-step-equal={perStepEqual}",
            $"determinism-steps={DeterminismSteps}",
            $"framework={RuntimeInformation.FrameworkDescription}",
            $"os={RuntimeInformation.OSDescription}",
            $"process-architecture={RuntimeInformation.ProcessArchitecture}",
            $"os-architecture={RuntimeInformation.OSArchitecture}",
            $"processor-count={Environment.ProcessorCount}",
            $"current-culture={System.Globalization.CultureInfo.CurrentCulture.Name}",
            $"current-ui-culture={System.Globalization.CultureInfo.CurrentUICulture.Name}",
            "production-change=False",
            "golden-change=False",
            "vr2-r3-change=False",
        };
        File.WriteAllLines(Path.Combine(captureDirectory, "summary.txt"), summaryLines, Utf8WithoutBom);

        Directory.CreateDirectory(outputDirectory);
        var summaryPath = Path.Combine(outputDirectory, "exact-v9-transitive-summary.txt");
        File.WriteAllLines(summaryPath, summaryLines, Utf8WithoutBom);

        var zipPath = Path.Combine(outputDirectory, "exact-v9-transitive-diagnostic.zip");
        if (File.Exists(zipPath))
        {
            File.Delete(zipPath);
        }
        ZipFile.CreateFromDirectory(captureDirectory, zipPath, CompressionLevel.SmallestSize, includeBaseDirectory: false);
    }

    // NRS-MARKER:M10974-EXACT-V9-CROSS-HOST-STAGE-CAUSAL-SEAM-DIAGNOSTIC2
    // NRS-MARKER:M10974-EXACT-V9-CROSS-HOST-STAGE-CAUSAL-SEAM-DIAGNOSTIC2-REV1
    private static void WriteExactV9StageCausalDiagnostic(
        string captureDirectory,
        DeterminismTraceCapture selector,
        DeterminismTraceCapture direct)
    {
        const string header = "step\tlogicalStep\tadmissionPhasePolicy\ttripBlocked\ttrainInletPhase\ttrainInletVaporQuality\ttrainInletVaporQualityBits\tstageInletPhase\tstageInletVaporQuality\tstageInletVaporQualityBits\trawThermodynamicVaporMassFraction\trawThermodynamicVaporMassFractionBits\tresolvedAdmissionVaporMassFraction\tresolvedAdmissionVaporMassFractionBits\tcommandedMassFlowKgPerS\tcommandedMassFlowBits\teffectiveMassFlowKgPerS\teffectiveMassFlowBits\tadmissionBoundaryMassFlowKgPerS\tadmissionBoundaryMassFlowBits\tphaseLimitedMassFlowKgPerS\tphaseLimitedMassFlowBits\texpectedEffectiveMassFlowKgPerS\texpectedEffectiveMassFlowBits\teffectiveFlowResidualKgPerS\teffectiveFlowResidualBits\tinletPressurePa\tinletPressureBits\tinletTemperatureK\tinletTemperatureBits\tinletSpecificInternalEnergyJPerKg\tinletSpecificInternalEnergyBits\texhaustPressurePa\texhaustPressureBits\texhaustTemperatureK\texhaustTemperatureBits\teffectiveIdealSpecificWorkJPerKg\teffectiveIdealSpecificWorkBits\tstageShaftTorqueNm\tstageShaftTorqueBits\tstageShaftPowerW\tstageShaftPowerBits\tmoistureDrainMassFlowKgPerS\tmoistureDrainMassFlowBits\trotorInitialRadPerS\trotorInitialRadPerSBits\trotorFinalRadPerS\trotorFinalRadPerSBits\trotorAverageRadPerS\trotorAverageRadPerSBits\trotorTurbineTorqueNm\trotorTurbineTorqueBits\trotorCommandedExternalLoadTorqueNm\trotorCommandedExternalLoadTorqueBits\trotorEffectiveExternalLoadTorqueNm\trotorEffectiveExternalLoadTorqueBits\trotorPassiveMechanicalLossTorqueNm\trotorPassiveMechanicalLossTorqueBits\trotorNetTorqueNm\trotorNetTorqueBits\trotorShaftPowerW\trotorShaftPowerBits";
        var selectorLines = new[] { header, StageCausalDiagnosticLine(selector.StageCausalTrace) };
        var directLines = new[] { header, StageCausalDiagnosticLine(direct.StageCausalTrace) };

        File.WriteAllLines(Path.Combine(captureDirectory, "stage-causal-selector.tsv"), selectorLines, Utf8WithoutBom);
        File.WriteAllLines(Path.Combine(captureDirectory, "stage-causal-direct.tsv"), directLines, Utf8WithoutBom);
    }

    private static string StageCausalDiagnosticLine(ExactV9StageCausalTraceEntry entry)
    {
        var d = entry.Diagnostic;
        return string.Join("\t", new[]
        {
            entry.Step.ToString(System.Globalization.CultureInfo.InvariantCulture),
            entry.LogicalStep.ToString(System.Globalization.CultureInfo.InvariantCulture),
            d.AdmissionPhasePolicy,
            d.TripBlocked ? "True" : "False",
            d.TrainInletPhase,
            DiagnosticNullableDouble(d.TrainInletVaporQuality), DiagnosticNullableBits(d.TrainInletVaporQuality),
            d.StageInletPhase,
            DiagnosticNullableDouble(d.StageInletVaporQuality), DiagnosticNullableBits(d.StageInletVaporQuality),
            DiagnosticNullableDouble(d.RawThermodynamicVaporMassFraction), DiagnosticNullableBits(d.RawThermodynamicVaporMassFraction),
            DiagnosticDouble(d.ResolvedAdmissionVaporMassFraction), DiagnosticBits(d.ResolvedAdmissionVaporMassFraction),
            DiagnosticDouble(d.CommandedMassFlowKgPerS), DiagnosticBits(d.CommandedMassFlowKgPerS),
            DiagnosticDouble(d.EffectiveMassFlowKgPerS), DiagnosticBits(d.EffectiveMassFlowKgPerS),
            DiagnosticDouble(d.AdmissionBoundaryMassFlowKgPerS), DiagnosticBits(d.AdmissionBoundaryMassFlowKgPerS),
            DiagnosticDouble(d.PhaseLimitedMassFlowKgPerS), DiagnosticBits(d.PhaseLimitedMassFlowKgPerS),
            DiagnosticDouble(d.ExpectedEffectiveMassFlowKgPerS), DiagnosticBits(d.ExpectedEffectiveMassFlowKgPerS),
            DiagnosticDouble(d.EffectiveFlowResidualKgPerS), DiagnosticBits(d.EffectiveFlowResidualKgPerS),
            DiagnosticDouble(d.InletPressurePa), DiagnosticBits(d.InletPressurePa),
            DiagnosticDouble(d.InletTemperatureK), DiagnosticBits(d.InletTemperatureK),
            DiagnosticDouble(d.InletSpecificInternalEnergyJPerKg), DiagnosticBits(d.InletSpecificInternalEnergyJPerKg),
            DiagnosticDouble(d.ExhaustPressurePa), DiagnosticBits(d.ExhaustPressurePa),
            DiagnosticDouble(d.ExhaustTemperatureK), DiagnosticBits(d.ExhaustTemperatureK),
            DiagnosticDouble(d.EffectiveIdealSpecificWorkJPerKg), DiagnosticBits(d.EffectiveIdealSpecificWorkJPerKg),
            DiagnosticDouble(d.StageShaftTorqueNm), DiagnosticBits(d.StageShaftTorqueNm),
            DiagnosticDouble(d.StageShaftPowerW), DiagnosticBits(d.StageShaftPowerW),
            DiagnosticDouble(d.MoistureDrainMassFlowKgPerS), DiagnosticBits(d.MoistureDrainMassFlowKgPerS),
            DiagnosticDouble(d.RotorInitialRadPerS), DiagnosticBits(d.RotorInitialRadPerS),
            DiagnosticDouble(d.RotorFinalRadPerS), DiagnosticBits(d.RotorFinalRadPerS),
            DiagnosticDouble(d.RotorAverageRadPerS), DiagnosticBits(d.RotorAverageRadPerS),
            DiagnosticDouble(d.RotorTurbineTorqueNm), DiagnosticBits(d.RotorTurbineTorqueNm),
            DiagnosticDouble(d.RotorCommandedExternalLoadTorqueNm), DiagnosticBits(d.RotorCommandedExternalLoadTorqueNm),
            DiagnosticDouble(d.RotorEffectiveExternalLoadTorqueNm), DiagnosticBits(d.RotorEffectiveExternalLoadTorqueNm),
            DiagnosticDouble(d.RotorPassiveMechanicalLossTorqueNm), DiagnosticBits(d.RotorPassiveMechanicalLossTorqueNm),
            DiagnosticDouble(d.RotorNetTorqueNm), DiagnosticBits(d.RotorNetTorqueNm),
            DiagnosticDouble(d.RotorShaftPowerW), DiagnosticBits(d.RotorShaftPowerW),
        });
    }

    // NRS-MARKER:M10974-EXACT-V9-CROSS-HOST-STAGE-MASS-FLOW-RESOLVER-CAUSAL-SEAM-DIAGNOSTIC3
    private static void WriteExactV9StageMassFlowResolverDiagnostic(
        string captureDirectory,
        DeterminismTraceCapture selector,
        DeterminismTraceCapture direct)
    {
        const string resolverHeader = "step\tlogicalStep\tdeltaTimeSeconds\tdeltaTimeSecondsBits\tpostStepInletMassKg\tpostStepInletMassBits\tdrivingPressurePa\tdrivingPressureBits\texpansionResistance\texpansionResistanceBits\thydraulicCandidateKgPerS\thydraulicCandidateBits\tadmissionTrainCandidateKgPerS\tadmissionTrainCandidateBits\tvisibleCandidateKgPerS\tvisibleCandidateBits\tobservedCommandedKgPerS\tobservedCommandedBits\tobservedMinusVisibleKgPerS\tobservedMinusVisibleBits\tobservedMinusAdmissionTrainKgPerS\tobservedMinusAdmissionTrainBits\tobservedMinusHydraulicKgPerS\tobservedMinusHydraulicBits\tdrainableTiePreStepMassKg\tdrainableTiePreStepMassBits\tselectedVisibleLimiter\tselectedValveLimiter";
        const string valveHeader = "step\tlogicalStep\trole\tvalveId\tcharacteristicKind\trangeability\trangeabilityBits\teffectivePosition\teffectivePositionBits\tflowCoefficient\tflowCoefficientBits\tpressureDifferencePa\tpressureDifferenceBits\tbaseResistance\tbaseResistanceBits\tcoefficientSquared\tcoefficientSquaredBits\teffectiveResistance\teffectiveResistanceBits\tsquaredMassFlow\tsquaredMassFlowBits\treconstructedSignedMassFlowKgPerS\treconstructedSignedMassFlowBits\tsnapshotMassFlowKgPerS\tsnapshotMassFlowBits\tpositiveSnapshotMassFlowKgPerS\tpositiveSnapshotMassFlowBits\treconstructionResidualKgPerS\treconstructionResidualBits";

        File.WriteAllLines(
            Path.Combine(captureDirectory, "stage-resolver-selector.tsv"),
            new[] { resolverHeader, StageResolverDiagnosticLine(selector.StageMassFlowResolverTrace) },
            Utf8WithoutBom);
        File.WriteAllLines(
            Path.Combine(captureDirectory, "stage-resolver-direct.tsv"),
            new[] { resolverHeader, StageResolverDiagnosticLine(direct.StageMassFlowResolverTrace) },
            Utf8WithoutBom);
        File.WriteAllLines(
            Path.Combine(captureDirectory, "stage-resolver-valves-selector.tsv"),
            StageResolverValveDiagnosticLines(valveHeader, selector.StageMassFlowResolverTrace),
            Utf8WithoutBom);
        File.WriteAllLines(
            Path.Combine(captureDirectory, "stage-resolver-valves-direct.tsv"),
            StageResolverValveDiagnosticLines(valveHeader, direct.StageMassFlowResolverTrace),
            Utf8WithoutBom);
    }

    private static string StageResolverDiagnosticLine(ExactV9StageMassFlowResolverTraceEntry entry)
    {
        var d = entry.Diagnostic;
        return string.Join("\t", new[]
        {
            entry.Step.ToString(System.Globalization.CultureInfo.InvariantCulture),
            entry.LogicalStep.ToString(System.Globalization.CultureInfo.InvariantCulture),
            DiagnosticDouble(d.DeltaTimeSeconds), DiagnosticBits(d.DeltaTimeSeconds),
            DiagnosticDouble(d.PostStepInletMassKg), DiagnosticBits(d.PostStepInletMassKg),
            DiagnosticDouble(d.DrivingPressurePa), DiagnosticBits(d.DrivingPressurePa),
            DiagnosticDouble(d.ExpansionResistance), DiagnosticBits(d.ExpansionResistance),
            DiagnosticDouble(d.HydraulicCandidateKgPerS), DiagnosticBits(d.HydraulicCandidateKgPerS),
            DiagnosticDouble(d.AdmissionTrainCandidateKgPerS), DiagnosticBits(d.AdmissionTrainCandidateKgPerS),
            DiagnosticDouble(d.VisibleCandidateKgPerS), DiagnosticBits(d.VisibleCandidateKgPerS),
            DiagnosticDouble(d.ObservedCommandedKgPerS), DiagnosticBits(d.ObservedCommandedKgPerS),
            DiagnosticDouble(d.ObservedMinusVisibleKgPerS), DiagnosticBits(d.ObservedMinusVisibleKgPerS),
            DiagnosticDouble(d.ObservedMinusAdmissionTrainKgPerS), DiagnosticBits(d.ObservedMinusAdmissionTrainKgPerS),
            DiagnosticDouble(d.ObservedMinusHydraulicKgPerS), DiagnosticBits(d.ObservedMinusHydraulicKgPerS),
            DiagnosticDouble(d.DrainableTiePreStepMassKg), DiagnosticBits(d.DrainableTiePreStepMassKg),
            d.SelectedVisibleLimiter,
            d.SelectedValveLimiter,
        });
    }

    private static string[] StageResolverValveDiagnosticLines(
        string header,
        ExactV9StageMassFlowResolverTraceEntry entry)
    {
        var d = entry.Diagnostic;
        return new[]
        {
            header,
            StageResolverValveDiagnosticLine(entry, d.StopValve),
            StageResolverValveDiagnosticLine(entry, d.ControlValve),
            StageResolverValveDiagnosticLine(entry, d.AdmissionValve),
        };
    }

    private static string StageResolverValveDiagnosticLine(
        ExactV9StageMassFlowResolverTraceEntry entry,
        ExactV9ValveFlowDiagnostic valve)
        => string.Join("\t", new[]
        {
            entry.Step.ToString(System.Globalization.CultureInfo.InvariantCulture),
            entry.LogicalStep.ToString(System.Globalization.CultureInfo.InvariantCulture),
            valve.Role,
            valve.ValveId,
            valve.CharacteristicKind,
            DiagnosticDouble(valve.Rangeability), DiagnosticBits(valve.Rangeability),
            DiagnosticDouble(valve.EffectivePosition), DiagnosticBits(valve.EffectivePosition),
            DiagnosticDouble(valve.FlowCoefficient), DiagnosticBits(valve.FlowCoefficient),
            DiagnosticDouble(valve.PressureDifferencePa), DiagnosticBits(valve.PressureDifferencePa),
            DiagnosticDouble(valve.BaseResistance), DiagnosticBits(valve.BaseResistance),
            DiagnosticDouble(valve.CoefficientSquared), DiagnosticBits(valve.CoefficientSquared),
            DiagnosticDouble(valve.EffectiveResistance), DiagnosticBits(valve.EffectiveResistance),
            DiagnosticDouble(valve.SquaredMassFlow), DiagnosticBits(valve.SquaredMassFlow),
            DiagnosticDouble(valve.ReconstructedSignedMassFlowKgPerS), DiagnosticBits(valve.ReconstructedSignedMassFlowKgPerS),
            DiagnosticDouble(valve.SnapshotMassFlowKgPerS), DiagnosticBits(valve.SnapshotMassFlowKgPerS),
            DiagnosticDouble(valve.PositiveSnapshotMassFlowKgPerS), DiagnosticBits(valve.PositiveSnapshotMassFlowKgPerS),
            DiagnosticDouble(valve.ReconstructionResidualKgPerS), DiagnosticBits(valve.ReconstructionResidualKgPerS),
        });

    private static string DiagnosticDouble(double value)
        => value.ToString("G17", System.Globalization.CultureInfo.InvariantCulture);

    private static string DiagnosticBits(double value)
        => unchecked((ulong)BitConverter.DoubleToInt64Bits(value)).ToString("X16", System.Globalization.CultureInfo.InvariantCulture);

    private static string DiagnosticNullableDouble(double? value)
        => value.HasValue ? DiagnosticDouble(value.Value) : string.Empty;

    private static string DiagnosticNullableBits(double? value)
        => value.HasValue ? DiagnosticBits(value.Value) : string.Empty;

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


    private sealed record DeterminismTraceEntry(
        int Step,
        long LogicalStep,
        string Fingerprint,
        byte[] Payload);

    private sealed record ExactV9StageCausalTraceEntry(
        int Step,
        long LogicalStep,
        ExactV9StageCausalDiagnostic Diagnostic);

    private sealed record ExactV9StageCausalDiagnostic(
        string AdmissionPhasePolicy,
        bool TripBlocked,
        string TrainInletPhase,
        double? TrainInletVaporQuality,
        string StageInletPhase,
        double? StageInletVaporQuality,
        double? RawThermodynamicVaporMassFraction,
        double ResolvedAdmissionVaporMassFraction,
        double CommandedMassFlowKgPerS,
        double EffectiveMassFlowKgPerS,
        double AdmissionBoundaryMassFlowKgPerS,
        double PhaseLimitedMassFlowKgPerS,
        double ExpectedEffectiveMassFlowKgPerS,
        double EffectiveFlowResidualKgPerS,
        double InletPressurePa,
        double InletTemperatureK,
        double InletSpecificInternalEnergyJPerKg,
        double ExhaustPressurePa,
        double ExhaustTemperatureK,
        double EffectiveIdealSpecificWorkJPerKg,
        double StageShaftTorqueNm,
        double StageShaftPowerW,
        double MoistureDrainMassFlowKgPerS,
        double RotorInitialRadPerS,
        double RotorFinalRadPerS,
        double RotorAverageRadPerS,
        double RotorTurbineTorqueNm,
        double RotorCommandedExternalLoadTorqueNm,
        double RotorEffectiveExternalLoadTorqueNm,
        double RotorPassiveMechanicalLossTorqueNm,
        double RotorNetTorqueNm,
        double RotorShaftPowerW);

    private sealed record ExactV9StageMassFlowResolverTraceEntry(
        int Step,
        long LogicalStep,
        ExactV9StageMassFlowResolverDiagnostic Diagnostic);

    private sealed record ExactV9StageMassFlowResolverDiagnostic(
        double DeltaTimeSeconds,
        double PostStepInletMassKg,
        double DrivingPressurePa,
        double ExpansionResistance,
        double HydraulicCandidateKgPerS,
        double AdmissionTrainCandidateKgPerS,
        double VisibleCandidateKgPerS,
        double ObservedCommandedKgPerS,
        double ObservedMinusVisibleKgPerS,
        double ObservedMinusAdmissionTrainKgPerS,
        double ObservedMinusHydraulicKgPerS,
        double DrainableTiePreStepMassKg,
        string SelectedVisibleLimiter,
        string SelectedValveLimiter,
        ExactV9ValveFlowDiagnostic StopValve,
        ExactV9ValveFlowDiagnostic ControlValve,
        ExactV9ValveFlowDiagnostic AdmissionValve);

    private sealed record ExactV9ValveFlowDiagnostic(
        string Role,
        string ValveId,
        string CharacteristicKind,
        double Rangeability,
        double EffectivePosition,
        double FlowCoefficient,
        double PressureDifferencePa,
        double BaseResistance,
        double CoefficientSquared,
        double EffectiveResistance,
        double SquaredMassFlow,
        double ReconstructedSignedMassFlowKgPerS,
        double SnapshotMassFlowKgPerS,
        double PositiveSnapshotMassFlowKgPerS,
        double ReconstructionResidualKgPerS);

    private sealed record DeterminismTraceCapture(
        string AggregateFingerprint,
        IReadOnlyList<DeterminismTraceEntry> Entries,
        ExactV9StageCausalTraceEntry StageCausalTrace,
        ExactV9StageMassFlowResolverTraceEntry StageMassFlowResolverTrace);

    private sealed record MissionResult(
        string PackExactId,
        string ScenarioId,
        int TripSteps,
        int BreakerOpenSteps,
        decimal? FinalScore);
}
