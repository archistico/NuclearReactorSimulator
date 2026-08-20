using System.Globalization;
using System.Text;
using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios;
using NuclearReactorSimulator.Application.Scenarios.Challenges;
using NuclearReactorSimulator.Application.Scenarios.Training;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.Scenarios.Challenges;

public sealed class M10961ChallengeLifecycleContractTests
{
    [Fact]
    public void Lifecycle_UsesLogicalStepsOnlyAndTransitionsDeterministically()
    {
        var left = CreateSessionAndTracker(CreateDefinition());
        var right = CreateSessionAndTracker(CreateDefinition());
        using var leftTracker = left.Tracker;
        using var rightTracker = right.Tracker;

        Assert.Equal(ChallengeLifecycleState.NotStarted, leftTracker.Snapshot.State);
        left.Session.CommandDispatcher.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.Run));
        right.Session.CommandDispatcher.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.Run));
        left.Session.Coordinator.AdvanceRunning(4, publicationStride: 1);
        right.Session.Coordinator.AdvanceRunning(4, publicationStride: 4);

        Assert.Equal(ChallengeLifecycleState.Completed, leftTracker.Snapshot.State);
        Assert.Equal(4L, leftTracker.Snapshot.TerminalLogicalStep);
        Assert.Equal(2L, leftTracker.Snapshot.ActivatedLogicalStep);
        Assert.Equal(3L, leftTracker.Snapshot.TargetWindowStartLogicalStep);
        Assert.Equal(5L, leftTracker.Snapshot.TargetWindowEndLogicalStep);
        Assert.Equal(8L, leftTracker.Snapshot.HardFailureDeadlineLogicalStep);
        Assert.Equal(Fingerprint(leftTracker.Snapshot), Fingerprint(rightTracker.Snapshot));

        var states = leftTracker.Snapshot.Transitions.Select(static item => item.To).ToArray();
        Assert.Equal(
            new[] { ChallengeLifecycleState.Ready, ChallengeLifecycleState.Active, ChallengeLifecycleState.Completed },
            states);
    }

    [Fact]
    public void Lifecycle_FailureConditionsAreDefinitionOwnedAndFailClosedAtSameLogicalStep()
    {
        var definition = CreateDefinition(
            readyAtLogicalStep: 0,
            activationConditionId: "step>=0",
            completionConditionId: "step>=1",
            failureConditionIds: new[] { "failure-step>=1" });
        var pair = CreateSessionAndTracker(definition);
        using var tracker = pair.Tracker;

        Assert.Equal(ChallengeLifecycleState.Active, tracker.Snapshot.State);
        pair.Session.CommandDispatcher.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.Run));
        pair.Session.Coordinator.AdvanceRunning(1);

        Assert.Equal(ChallengeLifecycleState.Failed, tracker.Snapshot.State);
        Assert.Contains("Failure condition", tracker.Snapshot.Transitions[^1].Reason, StringComparison.Ordinal);
    }


    [Fact]
    public void Lifecycle_RequiredObservationsGateCompletionRatherThanBeingDecorative()
    {
        var definition = CreateDefinition(
            readyAtLogicalStep: 0,
            activationConditionId: "step>=0",
            requiredObservationId: "step>=3-observation",
            completionConditionId: "step>=1",
            targetStartOffset: null,
            targetEndOffset: null,
            hardDeadlineOffset: null);
        var pair = CreateSessionAndTracker(definition);
        using var tracker = pair.Tracker;
        pair.Session.CommandDispatcher.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.Run));

        pair.Session.Coordinator.AdvanceRunning(1);
        Assert.Equal(ChallengeLifecycleState.Active, tracker.Snapshot.State);
        pair.Session.Coordinator.AdvanceRunning(2);

        Assert.Equal(ChallengeLifecycleState.Completed, tracker.Snapshot.State);
        Assert.Equal(3L, tracker.Snapshot.TerminalLogicalStep);
    }

    [Fact]
    public void Lifecycle_HardDeadlineIsExplicitAndTargetWindowAloneDoesNotFail()
    {
        var definition = CreateDefinition(
            readyAtLogicalStep: 0,
            activationConditionId: "step>=0",
            completionConditionId: "step>=99",
            targetStartOffset: 1,
            targetEndOffset: 2,
            hardDeadlineOffset: 3);
        var pair = CreateSessionAndTracker(definition);
        using var tracker = pair.Tracker;
        pair.Session.CommandDispatcher.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.Run));

        pair.Session.Coordinator.AdvanceRunning(3);
        Assert.Equal(ChallengeLifecycleState.Active, tracker.Snapshot.State);
        pair.Session.Coordinator.AdvanceRunning(1);

        Assert.Equal(ChallengeLifecycleState.Failed, tracker.Snapshot.State);
        Assert.Equal(4L, tracker.Snapshot.TerminalLogicalStep);
        Assert.Contains("hard logical-step deadline", tracker.Snapshot.Transitions[^1].Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Lifecycle_CancelAndResetRequireExplicitLifecycleCallsAndNeverDispatchPlantCommands()
    {
        var factory = new FakeInitialConditionFactory();
        var scenario = CreateScenario(factory);
        var session = new ScenarioSessionFactory(new VersionedInitialConditionRegistry(new[] { factory })).Load(scenario);
        var definition = CreateDefinition(readyAtLogicalStep: 0, activationConditionId: "step>=0", completionConditionId: "step>=9");
        using var tracker = ScenarioChallengeTracker.Attach(session, definition, new TestConditionEvaluator());

        Assert.Equal(ChallengeLifecycleState.Active, tracker.Snapshot.State);
        Assert.Equal(0, factory.Engine.QueuedOperatorCommandCount);

        tracker.Cancel("Explicit session cancellation.");
        Assert.Equal(ChallengeLifecycleState.Cancelled, tracker.Snapshot.State);
        Assert.Equal(0, factory.Engine.QueuedOperatorCommandCount);

        tracker.Reset();
        Assert.Equal(ChallengeLifecycleState.Active, tracker.Snapshot.State);
        Assert.Equal(0, factory.Engine.QueuedOperatorCommandCount);
        Assert.Contains(tracker.Snapshot.Transitions, static item => item.To == ChallengeLifecycleState.NotStarted);
    }

    [Fact]
    public void Lifecycle_SameSeedAndAcceptedActionTraceReconstructsSameState()
    {
        var left = CreateActionChallenge();
        var right = CreateActionChallenge();
        using var leftTracker = left.Tracker;
        using var rightTracker = right.Tracker;

        left.Session.CommandDispatcher.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.Run));
        right.Session.CommandDispatcher.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.Run));
        left.Session.Coordinator.AdvanceRunning(2, publicationStride: 1);
        right.Session.Coordinator.AdvanceRunning(2, publicationStride: 2);
        left.Session.CommandDispatcher.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.ReactorScram));
        right.Session.CommandDispatcher.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.ReactorScram));

        Assert.Equal(ChallengeLifecycleState.Completed, leftTracker.Snapshot.State);
        Assert.Equal(Fingerprint(leftTracker.Snapshot), Fingerprint(rightTracker.Snapshot));
        Assert.Single(left.Session.OperatorActions.Actions);
        Assert.Single(right.Session.OperatorActions.Actions);
    }

    [Fact]
    public void Definition_OwnsVersionObjectiveTimingAssistanceAndScoringPolicyIdentityWithoutScoringArithmetic()
    {
        var definition = CreateDefinition();

        Assert.Equal("m10961-lifecycle@1", definition.ExactId);
        Assert.Equal("reach-step", definition.ObjectiveId);
        Assert.True(definition.AssistancePolicy.Allows(TrainingGuidanceMode.Hidden));
        Assert.True(definition.AssistancePolicy.Allows(TrainingGuidanceMode.ChecklistOnly));
        Assert.False(definition.AssistancePolicy.Allows(TrainingGuidanceMode.Guided));
        Assert.Equal("unscored-m10961-contract", definition.AssistancePolicy.ScoringPolicyId);
        Assert.Single(definition.RequiredObservations);
        Assert.Single(definition.CompletionConditions);
        Assert.Empty(definition.FailureConditions);

        var publicTypes = new[]
        {
            typeof(ChallengeDefinition),
            typeof(ChallengeLogicalTimeContract),
            typeof(ChallengeLifecycleSnapshot),
            typeof(ChallengeLifecycleTransition),
        };
        Assert.DoesNotContain(
            publicTypes.SelectMany(static type => type.GetProperties()),
            static property => property.PropertyType == typeof(DateTime)
                || property.PropertyType == typeof(DateTimeOffset)
                || property.PropertyType == typeof(TimeSpan));

        Assert.DoesNotContain(
            typeof(IChallengeEvidenceSource).GetMembers(),
            static member => member.Name.Contains("Dispatch", StringComparison.Ordinal)
                || member.Name.Contains("Authority", StringComparison.Ordinal)
                || member.Name.Contains("CommandDispatcher", StringComparison.Ordinal));
    }

    [Fact]
    public void ChallengeDefinition_RejectsInvalidOrAmbiguousContracts()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ChallengeLogicalTimeContract(-1));
        Assert.Throws<ArgumentException>(() => new ChallengeLogicalTimeContract(0, 3, 2));
        Assert.Throws<ArgumentException>(() => new ChallengeLogicalTimeContract(0, 1, null));
        Assert.Throws<ArgumentException>(() => new ChallengeAssistancePolicy(Array.Empty<TrainingGuidanceMode>(), "policy"));

        var activation = Condition("same");
        Assert.Throws<ArgumentException>(() => new ChallengeDefinition(
            "duplicate",
            1,
            "challenge-scenario",
            "reach-step",
            "Duplicate",
            "Duplicate condition IDs",
            activation,
            new[] { Condition("required") },
            new[] { Condition("same") },
            null,
            new ChallengeLogicalTimeContract(),
            new ChallengeAssistancePolicy(new[] { TrainingGuidanceMode.Hidden }, "unscored")));
    }

    [Fact]
    public void M10961FocusedAudit_WritesLifecycleContractEvidence()
    {
        var first = CreateSessionAndTracker(CreateDefinition());
        var repeat = CreateSessionAndTracker(CreateDefinition());
        using var firstTracker = first.Tracker;
        using var repeatTracker = repeat.Tracker;
        first.Session.CommandDispatcher.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.Run));
        repeat.Session.CommandDispatcher.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.Run));
        first.Session.Coordinator.AdvanceRunning(4, publicationStride: 1);
        repeat.Session.Coordinator.AdvanceRunning(4, publicationStride: 4);

        var deterministic = string.Equals(Fingerprint(firstTracker.Snapshot), Fingerprint(repeatTracker.Snapshot), StringComparison.Ordinal);
        var output = ResolveArtifactDirectory();
        Directory.CreateDirectory(output);
        var summary = new[]
        {
            "=== 01-m1096-challenge-lifecycle-logical-time-contract ===",
            "scope=M10.9.6.1 deterministic challenge lifecycle and logical-time contract only; no demand profile, scoring arithmetic, UI or plant-control authority;",
            $"challenge-exact-id={firstTracker.Definition.ExactId}; scenario-id={first.Session.Scenario.ScenarioId}; objective-id={firstTracker.Definition.ObjectiveId};",
            $"terminal-state={firstTracker.Snapshot.State}; activated-step={firstTracker.Snapshot.ActivatedLogicalStep}; terminal-step={firstTracker.Snapshot.TerminalLogicalStep}; transitions={firstTracker.Snapshot.Transitions.Count};",
            $"target-window={firstTracker.Snapshot.TargetWindowStartLogicalStep}..{firstTracker.Snapshot.TargetWindowEndLogicalStep}; hard-deadline={firstTracker.Snapshot.HardFailureDeadlineLogicalStep};",
            $"deterministic-repeat={deterministic}; publication-stride-independent={deterministic}; wall-clock-dependence=False; challenge-command-authority=False;",
            "lifecycle-states=NotStarted|Ready|Active|Completed|Failed|Cancelled; required-observations-gate-completion=True; failure-condition-ownership=challenge-definition; same-step-failure-precedence=True;",
            "assistance-policy-id=unscored-m10961-contract; scoring-arithmetic-introduced=False; demand-profile-introduced=False;",
            "m10961-challenge-lifecycle-contract-passes=True; next-step=M10.9.6.2 deterministic external energy-demand profiles;",
        };
        File.WriteAllLines(Path.Combine(output, "01-m1096-challenge-lifecycle-logical-time-contract.summary.txt"), summary, new UTF8Encoding(false));

        Assert.True(deterministic);
        Assert.Equal(ChallengeLifecycleState.Completed, firstTracker.Snapshot.State);
    }

    private static (ScenarioSession Session, ScenarioChallengeTracker Tracker) CreateSessionAndTracker(ChallengeDefinition definition)
    {
        var factory = new FakeInitialConditionFactory();
        var scenario = CreateScenario(factory);
        var session = new ScenarioSessionFactory(new VersionedInitialConditionRegistry(new[] { factory })).Load(scenario);
        return (session, ScenarioChallengeTracker.Attach(session, definition, new TestConditionEvaluator()));
    }

    private static (ScenarioSession Session, ScenarioChallengeTracker Tracker) CreateActionChallenge()
    {
        var factory = new FakeInitialConditionFactory();
        var scenario = CreateScenario(factory, new[] { ControlRoomCommandKind.ReactorScram });
        var session = new ScenarioSessionFactory(new VersionedInitialConditionRegistry(new[] { factory })).Load(scenario);
        var definition = CreateDefinition(
            readyAtLogicalStep: 0,
            activationConditionId: "step>=0",
            requiredObservationId: "step>=0-observation",
            completionConditionId: "action:ReactorScram");
        return (session, ScenarioChallengeTracker.Attach(session, definition, new TestConditionEvaluator()));
    }

    private static ScenarioDefinition CreateScenario(
        FakeInitialConditionFactory factory,
        IEnumerable<ControlRoomCommandKind>? allowedActions = null)
        => new(
            "challenge-scenario",
            "Challenge scenario",
            "M10.9.6.1 deterministic lifecycle test scenario",
            factory.Descriptor.Reference,
            objectives: new[] { new ScenarioObjectiveDefinition("reach-step", "Reach step", "Reach the authored deterministic completion evidence.") },
            allowedOperatorActions: allowedActions);

    private static ChallengeDefinition CreateDefinition(
        long readyAtLogicalStep = 1,
        string activationConditionId = "step>=2",
        string requiredObservationId = "step>=3-observation",
        string completionConditionId = "step>=4",
        IEnumerable<string>? failureConditionIds = null,
        long? targetStartOffset = 1,
        long? targetEndOffset = 3,
        long? hardDeadlineOffset = 6)
        => new(
            "m10961-lifecycle",
            1,
            "challenge-scenario",
            "reach-step",
            "Deterministic lifecycle",
            "Exercise challenge lifecycle without wall-clock or plant-control authority.",
            Condition(activationConditionId),
            new[] { Condition(requiredObservationId) },
            new[] { Condition(completionConditionId) },
            (failureConditionIds ?? Array.Empty<string>()).Select(Condition),
            new ChallengeLogicalTimeContract(readyAtLogicalStep, targetStartOffset, targetEndOffset, hardDeadlineOffset),
            new ChallengeAssistancePolicy(
                new[] { TrainingGuidanceMode.Hidden, TrainingGuidanceMode.ChecklistOnly },
                "unscored-m10961-contract"));

    private static ChallengeConditionDefinition Condition(string id)
        => new(id, id, $"Authored deterministic condition {id}.");

    private static string Fingerprint(ChallengeLifecycleSnapshot snapshot)
        => string.Join(
            "|",
            new[]
            {
                snapshot.ChallengeExactId,
                snapshot.State.ToString(),
                snapshot.ActivatedLogicalStep?.ToString(CultureInfo.InvariantCulture) ?? "-",
                snapshot.TerminalLogicalStep?.ToString(CultureInfo.InvariantCulture) ?? "-",
                string.Join(",", snapshot.Transitions.Select(static item => $"{item.Sequence}:{item.From}>{item.To}@{item.LogicalStep}")),
                string.Join(",", snapshot.Observations.Select(static item => $"{item.ConditionId}:{item.IsSatisfied}@{item.LogicalStep}")),
            });

    private static string ResolveArtifactDirectory()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null && !File.Exists(Path.Combine(current.FullName, "NuclearReactorSimulator.sln")))
        {
            current = current.Parent;
        }
        if (current is null)
        {
            throw new InvalidOperationException("Repository root could not be resolved for M10.9.6.1 audit artifacts.");
        }
        return Path.Combine(current.FullName, "artifacts", "m1096-challenge-lifecycle");
    }

    private sealed class TestConditionEvaluator : IChallengeConditionEvaluator
    {
        public ChallengeConditionObservation Evaluate(
            ChallengeConditionDefinition condition,
            ControlRoomSnapshot snapshot,
            IReadOnlyList<ScenarioOperatorActionRecord> acceptedOperatorActions)
        {
            var satisfied = condition.ConditionId switch
            {
                "step>=0" => snapshot.LogicalStep >= 0,
                "step>=1" => snapshot.LogicalStep >= 1,
                "step>=2" => snapshot.LogicalStep >= 2,
                "step>=0-observation" => snapshot.LogicalStep >= 0,
                "step>=3-observation" => snapshot.LogicalStep >= 3,
                "step>=4" => snapshot.LogicalStep >= 4,
                "step>=9" => snapshot.LogicalStep >= 9,
                "step>=99" => snapshot.LogicalStep >= 99,
                "failure-step>=1" => snapshot.LogicalStep >= 1,
                "action:ReactorScram" => acceptedOperatorActions.Any(static item => item.Command.Kind == ControlRoomCommandKind.ReactorScram),
                _ => throw new InvalidOperationException($"Unsupported test condition '{condition.ConditionId}'."),
            };
            return new ChallengeConditionObservation(
                condition.ConditionId,
                satisfied,
                snapshot.LogicalStep,
                $"STEP {snapshot.LogicalStep}; actions={acceptedOperatorActions.Count}; satisfied={satisfied}.");
        }
    }

    private sealed class FakeInitialConditionFactory : IVersionedInitialConditionFactory
    {
        public FakeInitialConditionFactory()
        {
            Engine = new FakeRuntimeEngine();
        }

        public InitialConditionDescriptor Descriptor { get; } = new(
            new InitialConditionReference("m10961-challenge-reference", 1),
            "M10.9.6.1 challenge reference",
            "Deterministic logical-time challenge lifecycle reference");

        public FakeRuntimeEngine Engine { get; }

        public IControlRoomRuntimeEngine CreateRuntimeEngine() => Engine;
    }

    private sealed class FakeRuntimeEngine : IControlRoomRuntimeEngine
    {
        public long LogicalStep { get; private set; }
        public int QueuedOperatorCommandCount { get; private set; }

        public ControlRoomSnapshot CreatePresentationSnapshot(ControlRoomRunState runState) => Snapshot(runState);

        public ControlRoomSnapshot Step(ControlRoomRunState runState)
        {
            LogicalStep++;
            return Snapshot(runState);
        }

        public void QueueOperatorCommand(ControlRoomCommand command)
        {
            ArgumentNullException.ThrowIfNull(command);
            QueuedOperatorCommandCount++;
        }

        private ControlRoomSnapshot Snapshot(ControlRoomRunState runState)
            => new(LogicalStep, runState, 0, 0, 0, 0, false, false, false);
    }
}
