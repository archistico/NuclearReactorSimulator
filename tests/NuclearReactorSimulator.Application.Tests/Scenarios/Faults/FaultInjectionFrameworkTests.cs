using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios;
using NuclearReactorSimulator.Application.Scenarios.Faults;
using NuclearReactorSimulator.Simulation.Runtime.Replay;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.Scenarios.Faults;

public sealed class FaultInjectionFrameworkTests
{
    [Fact]
    public void LogicalStepSchedule_ActivatesAndClearsAtExactDeterministicBoundaries()
    {
        var harness = CreateHarness(new ScenarioFaultDefinition(
            "fault-1",
            FakeFaultApplicatorFactory.TypeId,
            "component-a",
            ScenarioFaultTriggerDefinition.AtLogicalStep(2),
            ScenarioFaultTriggerDefinition.AtLogicalStep(4)));

        var session = harness.SessionFactory.Load(harness.Scenario);

        Assert.Equal(ScenarioFaultLifecycleState.Pending, Assert.Single(session.Coordinator.Current.Faults.Faults).Lifecycle);

        Step(session, 1);
        Assert.Equal(ScenarioFaultLifecycleState.Pending, Assert.Single(session.Coordinator.Current.Faults.Faults).Lifecycle);

        Step(session, 1);
        var active = Assert.Single(session.Coordinator.Current.Faults.Faults);
        Assert.Equal(ScenarioFaultLifecycleState.Active, active.Lifecycle);
        Assert.Equal(2L, active.ActivatedLogicalStep);
        Assert.True(harness.RuntimeFactory.LastCreated!.FaultActive);

        Step(session, 2);
        var cleared = Assert.Single(session.Coordinator.Current.Faults.Faults);
        Assert.Equal(ScenarioFaultLifecycleState.Cleared, cleared.Lifecycle);
        Assert.Equal(4L, cleared.ClearedLogicalStep);
        Assert.False(harness.RuntimeFactory.LastCreated!.FaultActive);
        Assert.Equal(new[] { "activate:fault-1", "deactivate:fault-1" }, harness.RuntimeFactory.LastCreated!.Transitions);
    }

    [Fact]
    public void PlantConditionSchedule_UsesCommittedSnapshotAndActivatesBeforeFollowingStep()
    {
        var fault = new ScenarioFaultDefinition(
            "condition-fault",
            FakeFaultApplicatorFactory.TypeId,
            "component-b",
            ScenarioFaultTriggerDefinition.WhenPlantCondition(ScramActiveConditionEvaluator.Id));
        var harness = CreateHarness(fault, new ScramActiveConditionEvaluator());
        var scenario = new ScenarioDefinition(
            harness.Scenario.ScenarioId,
            harness.Scenario.Title,
            harness.Scenario.Description,
            harness.Scenario.InitialCondition,
            allowedOperatorActions: new[] { ControlRoomCommandKind.ReactorScram },
            faults: harness.Scenario.Faults);

        var session = harness.SessionFactory.Load(scenario);
        session.CommandDispatcher.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.ReactorScram));
        Step(session, 1);

        Assert.True(session.Coordinator.Current.ReactorScramActive);
        Assert.Equal(ScenarioFaultLifecycleState.Pending, Assert.Single(session.Coordinator.Current.Faults.Faults).Lifecycle);

        Step(session, 1);

        var active = Assert.Single(session.Coordinator.Current.Faults.Faults);
        Assert.Equal(ScenarioFaultLifecycleState.Active, active.Lifecycle);
        Assert.Equal(2L, active.ActivatedLogicalStep);
    }

    [Fact]
    public void SessionLoad_FailsClosedWhenFaultTypeOrConditionHandlerIsMissing()
    {
        var runtimeFactory = new FakeInitialConditionFactory();
        var registry = new VersionedInitialConditionRegistry(new[] { runtimeFactory });
        var missingApplicatorScenario = new ScenarioDefinition(
            "missing-applicator",
            "Missing applicator",
            "Must fail closed",
            runtimeFactory.Descriptor.Reference,
            faults: new[]
            {
                new ScenarioFaultDefinition(
                    "fault",
                    "unknown.type",
                    "target",
                    ScenarioFaultTriggerDefinition.AtLogicalStep(1)),
            });

        var noApplicatorFactory = new ScenarioSessionFactory(registry);
        Assert.Throws<KeyNotFoundException>(() => noApplicatorFactory.Load(missingApplicatorScenario));

        var missingConditionScenario = new ScenarioDefinition(
            "missing-condition",
            "Missing condition",
            "Must fail closed",
            runtimeFactory.Descriptor.Reference,
            faults: new[]
            {
                new ScenarioFaultDefinition(
                    "fault",
                    FakeFaultApplicatorFactory.TypeId,
                    "target",
                    ScenarioFaultTriggerDefinition.WhenPlantCondition("unknown-condition")),
            });
        var applicators = new ScenarioFaultApplicatorRegistry(new[] { new FakeFaultApplicatorFactory() });
        var noConditionFactory = new ScenarioSessionFactory(registry, faultApplicators: applicators);

        Assert.Throws<InvalidOperationException>(() => noConditionFactory.Load(missingConditionScenario));
    }

    [Fact]
    public void Replay_ReconstructsSameFaultLifecycleWithoutWallClockOrExternalFaultTrace()
    {
        var harness = CreateHarness(new ScenarioFaultDefinition(
            "replay-fault",
            FakeFaultApplicatorFactory.TypeId,
            "component-c",
            ScenarioFaultTriggerDefinition.AtLogicalStep(2),
            ScenarioFaultTriggerDefinition.AtLogicalStep(5)));
        var replay = new ScenarioReplayRunner(harness.SessionFactory);
        var emptyTrace = new SimulationCommandTrace<ControlRoomCommand>(Array.Empty<SimulationCommandTraceEntry<ControlRoomCommand>>());

        var left = replay.ReplayToStep(harness.Scenario, emptyTrace, 6);
        var right = replay.ReplayToStep(harness.Scenario, emptyTrace, 6);

        var leftFault = Assert.Single(left.Coordinator.Current.Faults.Faults);
        var rightFault = Assert.Single(right.Coordinator.Current.Faults.Faults);
        Assert.Equal(leftFault, rightFault);
        Assert.Equal(ScenarioFaultLifecycleState.Cleared, leftFault.Lifecycle);
        Assert.Equal(2L, leftFault.ActivatedLogicalStep);
        Assert.Equal(5L, leftFault.ClearedLogicalStep);
        Assert.Equal(2L, leftFault.LastTransitionSequence);
    }

    private static Harness CreateHarness(
        ScenarioFaultDefinition fault,
        params IScenarioFaultConditionEvaluator[] conditions)
    {
        var runtimeFactory = new FakeInitialConditionFactory();
        var initialConditions = new VersionedInitialConditionRegistry(new[] { runtimeFactory });
        var applicators = new ScenarioFaultApplicatorRegistry(new[] { new FakeFaultApplicatorFactory() });
        var conditionRegistry = new ScenarioFaultConditionRegistry(conditions);
        var sessionFactory = new ScenarioSessionFactory(
            initialConditions,
            faultApplicators: applicators,
            faultConditions: conditionRegistry);
        var scenario = new ScenarioDefinition(
            "fault-framework",
            "Fault framework",
            "Deterministic fault framework test",
            runtimeFactory.Descriptor.Reference,
            faults: new[] { fault });
        return new Harness(runtimeFactory, sessionFactory, scenario);
    }

    private static void Step(ScenarioSession session, int count)
    {
        for (var index = 0; index < count; index++)
        {
            session.CommandDispatcher.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.SingleStep));
        }
    }

    private sealed record Harness(
        FakeInitialConditionFactory RuntimeFactory,
        ScenarioSessionFactory SessionFactory,
        ScenarioDefinition Scenario);

    private sealed class FakeInitialConditionFactory : IVersionedInitialConditionFactory
    {
        public InitialConditionDescriptor Descriptor { get; } = new(
            new InitialConditionReference("fault-test", 1),
            "Fault test",
            "Deterministic fault test initial condition");

        public FakeRuntimeEngine? LastCreated { get; private set; }

        public IControlRoomRuntimeEngine CreateRuntimeEngine()
        {
            LastCreated = new FakeRuntimeEngine();
            return LastCreated;
        }
    }

    private sealed class FakeRuntimeEngine : IControlRoomRuntimeEngine
    {
        private bool _pendingScram;

        public long LogicalStep { get; private set; }

        public bool ScramActive { get; private set; }

        public bool FaultActive { get; set; }

        public List<string> Transitions { get; } = new();

        public ControlRoomSnapshot CreatePresentationSnapshot(ControlRoomRunState runState)
            => Snapshot(runState);

        public ControlRoomSnapshot Step(ControlRoomRunState runState)
        {
            LogicalStep++;
            if (_pendingScram)
            {
                ScramActive = true;
                _pendingScram = false;
            }

            return Snapshot(runState);
        }

        public void QueueOperatorCommand(ControlRoomCommand command)
        {
            if (command.Kind == ControlRoomCommandKind.ReactorScram)
            {
                _pendingScram = true;
            }
        }

        private ControlRoomSnapshot Snapshot(ControlRoomRunState runState)
            => new(
                LogicalStep,
                runState,
                FaultActive ? 1 : 0,
                0,
                0,
                0,
                ScramActive,
                false,
                false);
    }

    private sealed class FakeFaultApplicatorFactory : IScenarioFaultApplicatorFactory
    {
        public const string TypeId = "test.toggle";

        public string FaultTypeId => TypeId;

        public IScenarioFaultApplicator Create(IControlRoomRuntimeEngine runtimeEngine)
            => new FakeFaultApplicator((FakeRuntimeEngine)runtimeEngine);
    }

    private sealed class FakeFaultApplicator : IScenarioFaultApplicator
    {
        private readonly FakeRuntimeEngine _runtime;

        public FakeFaultApplicator(FakeRuntimeEngine runtime)
        {
            _runtime = runtime;
        }

        public void Activate(ScenarioFaultDefinition fault)
        {
            _runtime.FaultActive = true;
            _runtime.Transitions.Add($"activate:{fault.FaultId}");
        }

        public void Deactivate(ScenarioFaultDefinition fault)
        {
            _runtime.FaultActive = false;
            _runtime.Transitions.Add($"deactivate:{fault.FaultId}");
        }
    }

    private sealed class ScramActiveConditionEvaluator : IScenarioFaultConditionEvaluator
    {
        public const string Id = "reactor-scram-active";

        public string ConditionId => Id;

        public bool IsSatisfied(ControlRoomSnapshot snapshot) => snapshot.ReactorScramActive;
    }
}
