using System.Reflection;
using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios;
using NuclearReactorSimulator.Application.Scenarios.Challenges;
using NuclearReactorSimulator.Application.Scenarios.Challenges.Demand;
using NuclearReactorSimulator.Application.Scenarios.Challenges.Packs;
using NuclearReactorSimulator.Application.Scenarios.Challenges.Scoring;
using NuclearReactorSimulator.Application.Scenarios.Faults.SecondaryTransients;
using NuclearReactorSimulator.Application.Scenarios.Operations;
using NuclearReactorSimulator.Application.Scenarios.PreStartup;
using NuclearReactorSimulator.Application.Scenarios.Synchronization;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.Scenarios.Challenges.Packs;

public sealed class M10964InitialOperationalChallengePackTests
{
    [Fact]
    public void Pack_ContainsSixVersionedChallengesBoundToExistingScenarioObjectivesAndExactScoringPolicies()
    {
        var packs = InitialOperationalChallengePack.All;

        Assert.Collection(
            packs,
            static _ => { },
            static _ => { },
            static _ => { },
            static _ => { },
            static _ => { },
            static _ => { });
        Assert.DoesNotContain(
            packs.GroupBy(static item => item.ExactId, StringComparer.Ordinal),
            static group => group.Skip(1).Any());
        Assert.DoesNotContain(
            packs.GroupBy(static item => item.Challenge.ExactId, StringComparer.Ordinal),
            static group => group.Skip(1).Any());

        foreach (var pack in packs)
        {
            Assert.Equal(1, pack.Version);
            Assert.Equal(pack.Scenario.ScenarioId, pack.Challenge.ScenarioId);
            Assert.Contains(pack.Scenario.Objectives, objective => string.Equals(objective.ObjectiveId, pack.Challenge.ObjectiveId, StringComparison.Ordinal));
            Assert.Equal(pack.ScoringPolicy.ExactId, pack.Challenge.AssistancePolicy.ScoringPolicyId);
            Assert.Equal(
                pack.ScoringPolicy.Dimensions.Select(static item => item.Kind).OrderBy(static item => item),
                pack.ScoreEvidenceBindings.Select(static item => item.Kind).OrderBy(static item => item));
            Assert.DoesNotContain(
                pack.ScoreEvidenceBindings.GroupBy(static item => item.EvidenceSourceId, StringComparer.Ordinal),
                static group => group.Skip(1).Any());
        }
    }

    [Fact]
    public void DemandOwnershipAndScheduleVisibility_AreAuthoredPerChallengeWithoutGeneratorCommandAuthority()
    {
        Assert.Null(InitialOperationalChallengePack.PreStartupPreparation.Challenge.ExternalDemandProfile);
        Assert.Null(InitialOperationalChallengePack.ControlledNormalShutdown.Challenge.ExternalDemandProfile);
        Assert.Null(InitialOperationalChallengePack.GeneratorTripLoadRejectionRecovery.Challenge.ExternalDemandProfile);

        Assert.Null(InitialOperationalChallengePack.SynchronizationInitialLoading.Challenge.ExternalDemandProfile);

        var demand = Assert.IsType<ExternalEnergyDemandProfileDefinition>(
            InitialOperationalChallengePack.BoundedDemandFollowing.Challenge.ExternalDemandProfile);
        Assert.True(demand.ExposeNextScheduledChange);
        Assert.Equal(5d, demand.Evaluate(0).DemandMegawatts, 8);
        Assert.Equal(5d, demand.Evaluate(499).DemandMegawatts, 8);
        Assert.Equal(10d, demand.Evaluate(500).DemandMegawatts, 8);
        Assert.Equal(10d, demand.Evaluate(2_999).DemandMegawatts, 8);
        Assert.Equal(5d, demand.Evaluate(3_000).DemandMegawatts, 8);

        var stabilization = Assert.IsType<ExternalEnergyDemandProfileDefinition>(
            InitialOperationalChallengePack.PostLoadChangeStabilization.Challenge.ExternalDemandProfile);
        Assert.Equal(10d, stabilization.Evaluate(0).DemandMegawatts, 8);
        Assert.False(stabilization.ExposeNextScheduledChange);

        Assert.All(
            InitialOperationalChallengePack.All.Where(static item => item.Challenge.ExternalDemandProfile is not null),
            static pack => Assert.Contains(pack.ScoringPolicy.Dimensions, dimension => dimension.Kind == ChallengeScoreDimensionKind.DemandTracking));
    }

    [Fact]
    public void ConditionEvaluator_ReusesValidatedScenarioEvidenceAndSupportsEveryAuthoredConditionFailClosed()
    {
        foreach (var pack in InitialOperationalChallengePack.All)
        {
            var snapshot = InitialSnapshot(pack.Scenario.ScenarioId);
            var allConditions = new[] { pack.Challenge.ActivationCondition }
                .Concat(pack.Challenge.RequiredObservations)
                .Concat(pack.Challenge.CompletionConditions)
                .Concat(pack.Challenge.FailureConditions)
                .ToArray();

            foreach (var condition in allConditions)
            {
                var observation = pack.ConditionEvaluator.Evaluate(
                    condition,
                    snapshot,
                    Array.Empty<ScenarioOperatorActionRecord>());
                Assert.Equal(condition.ConditionId, observation.ConditionId);
                Assert.Equal(snapshot.LogicalStep, observation.LogicalStep);
                Assert.False(string.IsNullOrWhiteSpace(observation.Evidence));
            }
        }

        var evaluator = new StandardOperationalChallengeConditionEvaluator();
        var preStart = InitialSnapshot(ColdShutdownPreStartupProgram.Scenario.ScenarioId);
        Assert.Throws<KeyNotFoundException>(() => evaluator.Evaluate(
            new ChallengeConditionDefinition("unknown:m10964-condition", "Unknown", "Fail-closed unknown condition."),
            preStart,
            Array.Empty<ScenarioOperatorActionRecord>()));
    }

    [Fact]
    public void NormalOperationAndFaultResponse_PreserveChallengeSpecificFailureSemantics()
    {
        Assert.Contains(
            InitialOperationalChallengePack.PreStartupPreparation.Challenge.FailureConditions,
            static item => item.ConditionId == "prestart:unexpected-trip");
        Assert.Contains(
            InitialOperationalChallengePack.SynchronizationInitialLoading.Challenge.FailureConditions,
            static item => item.ConditionId == "sync:unexpected-trip");
        Assert.Contains(
            InitialOperationalChallengePack.BoundedDemandFollowing.Challenge.FailureConditions,
            static item => item.ConditionId == "demand:unexpected-trip");
        Assert.Contains(
            InitialOperationalChallengePack.PostLoadChangeStabilization.Challenge.FailureConditions,
            static item => item.ConditionId == "stabilize:unexpected-trip");
        Assert.Contains(
            InitialOperationalChallengePack.ControlledNormalShutdown.Challenge.FailureConditions,
            static item => item.ConditionId == "shutdown:emergency-action-used");

        Assert.Empty(InitialOperationalChallengePack.GeneratorTripLoadRejectionRecovery.Challenge.FailureConditions);
        Assert.Contains(
            InitialOperationalChallengePack.GeneratorTripLoadRejectionRecovery.Challenge.RequiredObservations,
            static item => item.ConditionId == "load-rejection:generator-trip-observed");
    }

    [Fact]
    public void PackLayer_HasNoDispatcherRuntimeEngineControllerOrWallClockAuthoritySurface()
    {
        var packTypes = new[]
        {
            typeof(OperationalChallengePackDefinition),
            typeof(OperationalChallengeScoreEvidenceBinding),
            typeof(StandardOperationalChallengeConditionEvaluator),
            typeof(InitialOperationalChallengePack),
        };

        foreach (var type in packTypes)
        {
            foreach (var memberType in PublicMemberTypes(type).Select(Unwrap))
            {
                var name = memberType.FullName ?? memberType.Name;
                Assert.DoesNotContain("CommandDispatcher", name, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("RuntimeEngine", name, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("Controller", name, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("Simulation", name, StringComparison.OrdinalIgnoreCase);
                Assert.NotEqual(typeof(DateTime), memberType);
                Assert.NotEqual(typeof(DateTimeOffset), memberType);
                Assert.NotEqual(typeof(TimeSpan), memberType);
            }
        }
    }

    [Fact]
    public void ArtifactSummary_WritesInitialChallengePackContractEvidence()
    {
        var directory = ResolveArtifactDirectory();
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "01-m1096-initial-operational-challenge-pack.summary.txt");
        File.WriteAllLines(path, new[]
        {
            "scope=M10.9.6.4 six initial Application-layer operational challenge packs composed only from existing validated scenario/check/fault owners; no new physics, fault, command authority, UI or score arithmetic;",
            "challenge-pack-count=6; packs=pre-start-circulation-preparation@1|synchronization-initial-loading@1|bounded-demand-following-5-10-5@1|post-load-change-stabilization@1|controlled-normal-shutdown@1|generator-trip-load-rejection-recovery@1;",
            "future-demand-schedule-exposed=bounded-demand-following-5-10-5@1 only; post-load-change stabilization exposes current demand only; synchronization owns no external-demand profile;",
            "demand-profile-commands-generator=False; trip-global-failure=False; generator-trip-load-rejection-treats-trip-as-required-evidence=True; normal-shutdown-emergency-action-is-procedural-failure=True;",
            "score-evidence-bindings-policy-owned=True; challenge-evaluator-reuses-m72-m75-m76-m84-evidence=True; hard-deadline-authored=False; wall-clock-dependence=False; plant-command-authority=False;",
            "m10964-initial-challenge-pack-contract-passes=True; next-step=if green, move to M10.9.6.5 replay/checkpoint/determinism and closure without adding new challenge physics;",
        });

        Assert.True(File.Exists(path));
        Assert.Contains("m10964-initial-challenge-pack-contract-passes=True", File.ReadAllText(path), StringComparison.Ordinal);
    }

    private static ControlRoomSnapshot InitialSnapshot(string scenarioId)
        => scenarioId switch
        {
            "cold-shutdown-pre-start" => new ColdShutdownInitialConditionFactory().CreateRuntimeEngine().CreatePresentationSnapshot(ControlRoomRunState.Paused),
            "grid-synchronization-initial-loading" => new GridSynchronizationInitialConditionFactory().CreateRuntimeEngine().CreatePresentationSnapshot(ControlRoomRunState.Paused),
            "power-manoeuvring-normal-shutdown" => new PowerManoeuvringInitialConditionFactory().CreateRuntimeEngine().CreatePresentationSnapshot(ControlRoomRunState.Paused),
            "m84-generator-trip-load-rejection" => new SecondaryTransientInitialConditionFactory().CreateRuntimeEngine().CreatePresentationSnapshot(ControlRoomRunState.Paused),
            _ => throw new KeyNotFoundException($"No M10.9.6.4 initial snapshot factory is authored for scenario '{scenarioId}'."),
        };

    private static IEnumerable<Type> PublicMemberTypes(Type type)
    {
        foreach (var constructor in type.GetConstructors(BindingFlags.Instance | BindingFlags.Public))
        {
            foreach (var parameter in constructor.GetParameters())
            {
                yield return parameter.ParameterType;
            }
        }
        foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly))
        {
            yield return method.ReturnType;
            foreach (var parameter in method.GetParameters())
            {
                yield return parameter.ParameterType;
            }
        }
        foreach (var property in type.GetProperties(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly))
        {
            yield return property.PropertyType;
        }
    }

    private static Type Unwrap(Type type)
    {
        if (type.IsArray)
        {
            return type.GetElementType() ?? type;
        }
        if (type.IsGenericType)
        {
            return type.GetGenericArguments()[0];
        }
        return type;
    }

    private static string ResolveArtifactDirectory()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null && !File.Exists(Path.Combine(current.FullName, "NuclearReactorSimulator.sln")))
        {
            current = current.Parent;
        }
        if (current is null)
        {
            throw new InvalidOperationException("Repository root could not be resolved for M10.9.6.4 audit artifacts.");
        }
        return Path.Combine(current.FullName, "artifacts", "m1096-initial-challenge-packs");
    }
}
