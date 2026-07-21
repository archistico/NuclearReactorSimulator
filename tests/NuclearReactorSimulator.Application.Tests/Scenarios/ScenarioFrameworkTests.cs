using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios;
using NuclearReactorSimulator.Simulation.Runtime.Replay;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.Scenarios;

public sealed class ScenarioFrameworkTests
{
    [Fact]
    public void Registry_ResolvesExactInitialConditionVersionOnly()
    {
        var version1 = new FakeInitialConditionFactory("cold", 1);
        var version2 = new FakeInitialConditionFactory("cold", 2);
        var registry = new VersionedInitialConditionRegistry(new IVersionedInitialConditionFactory[] { version1, version2 });

        Assert.Same(version2, registry.Resolve(new InitialConditionReference("cold", 2)));
        Assert.Throws<KeyNotFoundException>(() => registry.Resolve(new InitialConditionReference("cold", 3)));
    }

    [Fact]
    public void Session_LoadsPausedAndRejectsOperatorActionNotAllowedByScenario()
    {
        var factory = new FakeInitialConditionFactory("reference", 1);
        var sessionFactory = new ScenarioSessionFactory(new VersionedInitialConditionRegistry(new[] { factory }));
        var scenario = new ScenarioDefinition(
            "training",
            "Training",
            "Training scenario",
            factory.Descriptor.Reference,
            allowedOperatorActions: new[] { ControlRoomCommandKind.ReactorScram });

        var session = sessionFactory.Load(scenario);

        Assert.Equal(ControlRoomRunState.Paused, session.Coordinator.RunState);
        Assert.Equal(0, session.Coordinator.Current.LogicalStep);
        session.CommandDispatcher.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.ReactorScram));
        session.CommandDispatcher.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.SingleStep));
        Assert.True(session.Coordinator.Current.ReactorScramActive);
        Assert.Throws<InvalidOperationException>(() =>
            session.CommandDispatcher.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.TurbineTrip)));
    }

    [Fact]
    public void Replay_LoadsFreshInitialConditionAndReproducesLogicalCommandTrace()
    {
        var factory = new FakeInitialConditionFactory("reference", 1);
        var sessionFactory = new ScenarioSessionFactory(new VersionedInitialConditionRegistry(new[] { factory }));
        var scenario = new ScenarioDefinition(
            "training",
            "Training",
            "Training scenario",
            factory.Descriptor.Reference,
            allowedOperatorActions: new[] { ControlRoomCommandKind.ReactorScram });
        var trace = new SimulationCommandTrace<ControlRoomCommand>(new[]
        {
            new SimulationCommandTraceEntry<ControlRoomCommand>(2, new ControlRoomCommand(ControlRoomCommandKind.ReactorScram)),
        });
        var runner = new ScenarioReplayRunner(sessionFactory);

        var left = runner.ReplayToStep(scenario, trace, 4);
        var right = runner.ReplayToStep(scenario, trace, 4);

        Assert.Equal(4, left.Coordinator.Current.LogicalStep);
        Assert.Equal(left.Coordinator.Current, right.Coordinator.Current);
        Assert.True(left.Coordinator.Current.ReactorScramActive);
        Assert.Equal(2, factory.CreatedRuntimeCount);
    }

    [Fact]
    public void ScenarioDefinition_RejectsRuntimeHostCommandsAsAllowedOperatorActions()
    {
        Assert.Throws<ArgumentException>(() => new ScenarioDefinition(
            "invalid",
            "Invalid",
            "Invalid scenario",
            new InitialConditionReference("reference", 1),
            allowedOperatorActions: new[] { ControlRoomCommandKind.Run }));
    }

    private sealed class FakeInitialConditionFactory : IVersionedInitialConditionFactory
    {
        public FakeInitialConditionFactory(string id, int version)
        {
            Descriptor = new InitialConditionDescriptor(
                new InitialConditionReference(id, version),
                $"{id} v{version}",
                "Deterministic test initial condition");
        }

        public InitialConditionDescriptor Descriptor { get; }

        public int CreatedRuntimeCount { get; private set; }

        public IControlRoomRuntimeEngine CreateRuntimeEngine()
        {
            CreatedRuntimeCount++;
            return new FakeRuntimeEngine();
        }
    }

    private sealed class FakeRuntimeEngine : IControlRoomRuntimeEngine
    {
        private bool _pendingScram;
        private bool _scramActive;

        public long LogicalStep { get; private set; }

        public ControlRoomSnapshot CreatePresentationSnapshot(ControlRoomRunState runState)
            => CreateSnapshot(runState);

        public ControlRoomSnapshot Step(ControlRoomRunState runState)
        {
            LogicalStep++;
            if (_pendingScram)
            {
                _scramActive = true;
                _pendingScram = false;
            }

            return CreateSnapshot(runState);
        }

        public void QueueOperatorCommand(ControlRoomCommand command)
        {
            ArgumentNullException.ThrowIfNull(command);
            if (command.Kind == ControlRoomCommandKind.ReactorScram)
            {
                _pendingScram = true;
            }
        }

        private ControlRoomSnapshot CreateSnapshot(ControlRoomRunState runState)
            => new(
                LogicalStep,
                runState,
                0,
                0,
                0,
                0,
                _scramActive,
                false,
                false);
    }
}
