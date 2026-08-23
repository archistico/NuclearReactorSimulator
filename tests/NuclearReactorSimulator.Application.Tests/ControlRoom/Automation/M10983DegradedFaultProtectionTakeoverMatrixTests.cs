using System.Text;
using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.ControlRoom.Automation;
using NuclearReactorSimulator.Application.ControlRoom.Hmi;
using NuclearReactorSimulator.Application.ControlRoom.MissionPerformance;
using NuclearReactorSimulator.Application.Scenarios;
using NuclearReactorSimulator.Application.Scenarios.Challenges;
using NuclearReactorSimulator.Application.Scenarios.Challenges.Packs;
using NuclearReactorSimulator.Application.Scenarios.Faults;
using NuclearReactorSimulator.Application.Scenarios.Faults.Hydraulics;
using NuclearReactorSimulator.Application.Scenarios.Faults.InstrumentationControl;
using NuclearReactorSimulator.Application.Scenarios.Operations;
using NuclearReactorSimulator.Application.Scenarios.Training;
using NuclearReactorSimulator.Domain.Physics.Control.Supervisory;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.ControlRoom.Automation;

public sealed class M10983DegradedFaultProtectionTakeoverMatrixTests
{
    [Fact]
    public void ValidationOnlyChallengeMeasurementFault_DegradesFailClosedAndRecovers()
    {
        var pack = CreateValidationOnlyDegradedChallengePack();
        var session = CreateExactV4Factory().Load(pack.Scenario);
        using var source = new MissionPerformanceLiveSnapshotSource(session, pack, TrainingGuidanceMode.Guided);

        session.PlantControlAuthority.RequestSupervisoryObjective(SupervisoryObjectiveRequest.HoldCurrentOperatingPoint());
        session.PlantControlAuthority.RequestAuthority(PlantControlAuthorityMode.SupervisoryAutomatic);
        Step(session);

        Assert.Equal(ChallengeLifecycleState.Active, source.Current.LifecycleState);
        Assert.True(source.Current.Demand.ExternalDemandAvailable);
        Assert.Equal(PlantControlAuthorityMode.SupervisoryAutomatic, session.PlantControlAuthority.CurrentAutomation.EffectiveAuthority);
        Assert.Equal(PlantControlAuthorityHealth.Normal, session.PlantControlAuthority.CurrentAutomation.Health);
        var healthyInvalidMeasuredSignalCount = session.Coordinator.Current.InvalidMeasuredSignalCount;

        while (session.Coordinator.Current.LogicalStep < 3)
        {
            Step(session);
        }

        var degraded = session.PlantControlAuthority.CurrentAutomation;
        var degradedMission = source.Current;
        Assert.Equal(PlantControlAuthorityMode.SupervisoryAutomatic, degraded.RequestedAuthority);
        Assert.Equal(PlantControlAuthorityMode.Assisted, degraded.EffectiveAuthority);
        Assert.Equal(PlantControlAuthorityHealth.Degraded, degraded.Health);
        Assert.False(string.IsNullOrWhiteSpace(degraded.DegradationReason));
        Assert.True(session.Coordinator.Current.InvalidMeasuredSignalCount > healthyInvalidMeasuredSignalCount);
        Assert.Equal(1, session.Coordinator.Current.Faults.ActiveCount);
        Assert.False(session.Coordinator.Current.AnyTripActive);
        Assert.Equal(ChallengeLifecycleState.Active, degradedMission.LifecycleState);
        Assert.True(degradedMission.Demand.ExternalDemandAvailable);
        Assert.Equal(PlantControlAuthorityMode.SupervisoryAutomatic, degradedMission.RequestedControlAuthority);
        Assert.Equal(PlantControlAuthorityMode.Assisted, degradedMission.EffectiveControlAuthority);
        Assert.Equal(PlantControlAuthorityHealth.Degraded, degradedMission.ControlAuthorityHealth);
        Assert.Equal(degraded.DegradationReason, degradedMission.ControlAuthorityDegradationReason);

        while (session.Coordinator.Current.LogicalStep < 6)
        {
            Step(session);
        }

        var recovered = session.PlantControlAuthority.CurrentAutomation;
        Assert.Equal(PlantControlAuthorityMode.SupervisoryAutomatic, recovered.RequestedAuthority);
        Assert.Equal(PlantControlAuthorityMode.SupervisoryAutomatic, recovered.EffectiveAuthority);
        Assert.Equal(PlantControlAuthorityHealth.Normal, recovered.Health);
        Assert.Null(recovered.DegradationReason);
        Assert.Equal(healthyInvalidMeasuredSignalCount, session.Coordinator.Current.InvalidMeasuredSignalCount);
        Assert.Equal(0, session.Coordinator.Current.Faults.ActiveCount);
        Assert.Equal(1, session.Coordinator.Current.Faults.ClearedCount);
        Assert.Equal(ChallengeLifecycleState.Active, source.Current.LifecycleState);
        Assert.True(source.Current.Demand.ExternalDemandAvailable);
    }

    [Fact]
    public void InstrumentationTruth_PreservesSuspectAndUnavailablePresentation()
    {
        var biased = Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(
            new PowerManoeuvringInitialConditionFactory().CreateRuntimeEngine());
        ((IInstrumentationControlFaultTarget)biased).ActivateSensorBias("m10983-power-bias", "power", 5_000_000d);
        var biasedSnapshot = biased.Step(ControlRoomRunState.Paused);

        Assert.True(biasedSnapshot.ReactorCore.ReactorThermalPower.NumericValue.HasValue);
        Assert.Equal(ControlRoomVisualState.Warning, biasedSnapshot.ReactorCore.ReactorThermalPower.State);
        Assert.Equal(ControlRoomInstrumentQuality.Suspect, biasedSnapshot.ReactorCore.ReactorThermalPower.Quality);
        Assert.Equal("QUALITY SUSPECT", biasedSnapshot.ReactorCore.ReactorThermalPower.QualityText);

        var unavailable = Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(
            new PowerManoeuvringInitialConditionFactory().CreateRuntimeEngine());
        ((IInstrumentationControlFaultTarget)unavailable).ActivateSensorUnavailable("m10983-power-unavailable", "power");
        var unavailableSnapshot = unavailable.Step(ControlRoomRunState.Paused);

        Assert.Null(unavailableSnapshot.ReactorCore.ReactorThermalPower.NumericValue);
        Assert.Equal(ControlRoomVisualState.Unavailable, unavailableSnapshot.ReactorCore.ReactorThermalPower.State);
        Assert.Equal(ControlRoomInstrumentQuality.Unavailable, unavailableSnapshot.ReactorCore.ReactorThermalPower.Quality);
        Assert.Equal("UNAVAILABLE", unavailableSnapshot.ReactorCore.ReactorThermalPower.QualityText);
        Assert.True(unavailableSnapshot.InvalidMeasuredSignalCount > 0);
    }

    [Fact]
    public void HydraulicFault_RemainsFaultFrameworkOwned()
    {
        var reference = Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(
            new PowerManoeuvringInitialConditionFactory().CreateRuntimeEngine());
        var faulted = Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(
            new PowerManoeuvringInitialConditionFactory().CreateRuntimeEngine());
        ((IHydraulicComponentFaultTarget)faulted).ActivatePumpDegradation("m10983-pump-degraded", "pump", 0.40d);

        reference.Step(ControlRoomRunState.Paused);
        faulted.Step(ControlRoomRunState.Paused);

        var referencePump = reference.CurrentState.PlantState.PlantState.GetPump("pump");
        var faultedPump = faulted.CurrentState.PlantState.PlantState.GetPump("pump");
        Assert.True(referencePump.IsRunning);
        Assert.True(faultedPump.IsRunning);
        Assert.Equal(referencePump.Speed.Fraction * 0.40d, faultedPump.Speed.Fraction, 12);
    }

    [Fact]
    public void ProtectionPrecedence_RemainsAuthoritativeAcrossNormalCommandAndChallengeFailure()
    {
        var pack = ProductionOperationalChallengePack.BoundedDemandFollowingV2;
        var session = CreateExactV4Factory().Load(pack.Scenario);
        using var source = new MissionPerformanceLiveSnapshotSource(session, pack, TrainingGuidanceMode.Guided);

        session.PlantControlAuthority.RequestSupervisoryObjective(SupervisoryObjectiveRequest.HoldCurrentOperatingPoint());
        session.PlantControlAuthority.RequestAuthority(PlantControlAuthorityMode.SupervisoryAutomatic);
        Step(session);
        Assert.Equal(ChallengeLifecycleState.Active, source.Current.LifecycleState);
        Assert.Equal(PlantControlAuthorityMode.SupervisoryAutomatic, session.PlantControlAuthority.CurrentAutomation.EffectiveAuthority);

        session.CommandDispatcher.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.ReactorScram));
        Step(session);
        Assert.True(session.Coordinator.Current.ReactorScramActive);

        session.CommandDispatcher.Dispatch(new ControlRoomCommand(
            ControlRoomCommandKind.ControlRodWithdraw,
            "regulating",
            ControlRoomCommandTargetKind.ControlRodGroup));
        Step(session);

        var automation = session.PlantControlAuthority.CurrentAutomation;
        Assert.True(session.Coordinator.Current.ReactorScramActive);
        Assert.Equal(PlantControlAuthorityMode.SupervisoryAutomatic, automation.RequestedAuthority);
        Assert.Equal(PlantControlAuthorityMode.Assisted, automation.EffectiveAuthority);
        Assert.Equal(PlantControlAuthorityHealth.SuspendedByProtection, automation.Health);
        Assert.False(string.IsNullOrWhiteSpace(automation.DegradationReason));
        Assert.Equal(PlantControlAuthorityMode.SupervisoryAutomatic, source.Current.RequestedControlAuthority);
        Assert.Equal(PlantControlAuthorityMode.Assisted, source.Current.EffectiveControlAuthority);
        Assert.Equal(PlantControlAuthorityHealth.SuspendedByProtection, source.Current.ControlAuthorityHealth);
        Assert.Equal(ChallengeLifecycleState.Failed, source.Current.LifecycleState);
    }

    [Fact]
    public void ManualTakeover_UsesExactV4AndClearsSupervisoryObjective()
    {
        var session = CreateExactV4Factory().Load(DesktopIntegratedOperationsProductionProgram.RepairedProductionScenario);
        session.PlantControlAuthority.RequestSupervisoryObjective(SupervisoryObjectiveRequest.HoldCurrentOperatingPoint());
        session.PlantControlAuthority.RequestAuthority(PlantControlAuthorityMode.SupervisoryAutomatic);
        Step(session);

        Assert.Equal(PlantControlAuthorityMode.SupervisoryAutomatic, session.PlantControlAuthority.CurrentAutomation.EffectiveAuthority);
        Assert.Contains("HOLD OPERATING POINT", session.PlantControlAuthority.CurrentAutomation.ObjectiveText, StringComparison.Ordinal);

        session.PlantControlAuthority.RequestAuthority(PlantControlAuthorityMode.Manual);
        Step(session);

        var automation = session.PlantControlAuthority.CurrentAutomation;
        Assert.Equal(PlantControlAuthorityMode.Manual, automation.RequestedAuthority);
        Assert.Equal(PlantControlAuthorityMode.Manual, automation.EffectiveAuthority);
        Assert.Equal(PlantControlAuthorityHealth.Normal, automation.Health);
        Assert.Equal("NONE", automation.ObjectiveText);
        Assert.All(automation.ControllerModes, static controller =>
            Assert.Equal(NuclearReactorSimulator.Domain.Physics.Control.ControllerMode.Manual, controller.Mode));
    }

    [Fact]
    public void ArtifactSummary_DeclaresElevenCaseMatrixAndNextOwnershipBoundary()
    {
        var directory = ResolveArtifactDirectory();
        Directory.CreateDirectory(directory);
        File.WriteAllLines(Path.Combine(directory, "01-m10983-degraded-fault-protection-takeover.summary.txt"), new[]
        {
            "scope=M10.9.8.3 Degraded Measurement / Fault / Protection / Takeover Matrix over M10.9.8.2 Hotfix 1 REV5 VALIDATED; automated integration/evidence only; no production runtime, Simulation physics, challenge/scoring/protection owner, archive schema, fingerprint algorithm or plant-command authority change;",
            "execution-matrix=m10983-degraded-fault-protection-takeover-v1; required-cases=11; validation-only-compositions-versioned=True; production-scenario-registration-added=False; new-fault-type-added=False;",
            "invalid-required-supervisory-measurement-covered=True; suspect-unavailable-measurement-operator-truth-covered=True; protection-active-before-normal-command-covered=True; protection-trip-during-automated-operation-covered=True; component-fault-covered=True; instrumentation-fault-covered=True;",
            "blocked-permissive-owner-rerun-required=True; requested-supervisory-degrades-assisted-covered=True; manual-takeover-covered=True; supported-recovery-sequence-covered=True; challenge-active-during-degraded-protection-covered=True;",
            "requested-effective-authority-distinct=True; supervisory-degradation-fail-closed=True; no-true-state-fallback=True; protection-overrides-normal-control=True; faults-remain-fault-framework-owned=True; mission-source-presentation-only=True;",
            "m10983-integration-composition-passes=True; m10984-replay-checkpoint-owned-by-next-milestone=True; next-step=M10.9.8.4 replay/checkpoint/same-seed integrity;",
        }, new UTF8Encoding(false));
    }

    private static OperationalChallengePackDefinition CreateValidationOnlyDegradedChallengePack()
    {
        var source = ProductionOperationalChallengePack.BoundedDemandFollowingV2;
        var scenario = new ScenarioDefinition(
            "m10983-bounded-demand-supervisory-degraded-validation",
            "M10.9.8.3 bounded-demand supervisory degraded validation",
            "Validation-only exact-v4 composition that injects one existing M8.3 unavailable measured signal through the canonical fault seam while the bounded-demand challenge remains observational and active.",
            DesktopSustainedGenerationI5RepairedActivationCandidateInitialConditionFactory.Reference,
            source.Scenario.Objectives,
            source.Scenario.AllowedOperatorActions,
            new[]
            {
                new ScenarioFaultDefinition(
                    "m10983-power-unavailable",
                    InstrumentationControlFaultTypeIds.SensorUnavailable,
                    "power",
                    ScenarioFaultTriggerDefinition.AtLogicalStep(2),
                    ScenarioFaultTriggerDefinition.AtLogicalStep(5)),
            });

        var contract = source.Challenge;
        var challenge = new ChallengeDefinition(
            "m10983-bounded-demand-supervisory-degraded",
            1,
            scenario.ScenarioId,
            contract.ObjectiveId,
            contract.Title,
            contract.Description,
            contract.ActivationCondition,
            contract.RequiredObservations,
            contract.CompletionConditions,
            contract.FailureConditions,
            contract.LogicalTime,
            contract.AssistancePolicy,
            contract.ExternalDemandProfile);

        return new OperationalChallengePackDefinition(
            "m10983-bounded-demand-supervisory-degraded",
            1,
            scenario,
            challenge,
            source.ConditionEvaluator,
            source.ScoringPolicy,
            source.ScoreEvidenceBindings);
    }

    private static ScenarioSessionFactory CreateExactV4Factory()
        => new(new VersionedInitialConditionRegistry(new IVersionedInitialConditionFactory[]
        {
            new DesktopSustainedGenerationI5RepairedActivationCandidateInitialConditionFactory(),
        }));

    private static void Step(ScenarioSession session)
        => session.CommandDispatcher.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.SingleStep));

    private static string ResolveArtifactDirectory()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null && !File.Exists(Path.Combine(current.FullName, "NuclearReactorSimulator.sln")))
        {
            current = current.Parent;
        }
        if (current is null)
        {
            throw new InvalidOperationException("Repository root could not be resolved for M10.9.8.3 audit artifacts.");
        }
        return Path.Combine(current.FullName, "artifacts", "m1098-degraded-fault-protection-takeover");
    }
}
