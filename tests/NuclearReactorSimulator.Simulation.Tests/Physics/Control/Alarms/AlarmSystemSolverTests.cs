using NuclearReactorSimulator.Domain.Physics.Control.Alarms;
using NuclearReactorSimulator.Domain.Physics.Control.Protection;
using NuclearReactorSimulator.Domain.Physics.Instrumentation;
using NuclearReactorSimulator.Simulation.Physics.Control.Alarms;
using NuclearReactorSimulator.Simulation.Physics.Control.Protection;
using NuclearReactorSimulator.Simulation.Physics.Control.TurbineSecondary;
using NuclearReactorSimulator.Simulation.Physics.Instrumentation;
using NuclearReactorSimulator.Simulation.Tests.Physics.Control.TurbineSecondary;
using Xunit;

namespace NuclearReactorSimulator.Simulation.Tests.Physics.Control.Alarms;

public sealed class AlarmSystemSolverTests
{
    [Fact]
    public void LatchedAlarm_RequiresAcknowledgeAndSafeExplicitReset()
    {
        var fixture = CreateFixture();
        var solver = new AlarmSystemSolver(fixture.Alarms);
        var state = AlarmSystemState.CreateInitial(fixture.Alarms);

        var active = solver.Step(fixture.Signals, fixture.ProtectionSnapshot, state, new AlarmSystemInputs(fixture.Alarms));
        var alarm = active.Snapshot.GetAlarm("pressure-high");
        Assert.True(alarm.ConditionActive);
        Assert.True(alarm.IsLatched);
        Assert.False(alarm.IsAcknowledged);
        Assert.Equal(AlarmAnnunciatorState.ActiveUnacknowledged, alarm.AnnunciatorState);
        Assert.Equal(AlarmEventKind.Activated, Assert.Single(active.Snapshot.Events).Kind);

        var acknowledged = solver.Step(
            fixture.Signals,
            fixture.ProtectionSnapshot,
            active.CandidateState,
            new AlarmSystemInputs(fixture.Alarms, acknowledgeAlarmIds: new[] { "pressure-high" }));
        Assert.True(acknowledged.Snapshot.GetAlarm("pressure-high").IsAcknowledged);
        Assert.Equal(2L, Assert.Single(acknowledged.Snapshot.Events).Sequence);

        var safeSignals = WithValue(fixture.Signals, "pressure", 4_000_000d);
        var returned = solver.Step(
            safeSignals,
            fixture.ProtectionSnapshot,
            acknowledged.CandidateState,
            new AlarmSystemInputs(fixture.Alarms));
        Assert.Equal(AlarmAnnunciatorState.ReturnedAcknowledged, returned.Snapshot.GetAlarm("pressure-high").AnnunciatorState);
        Assert.True(returned.Snapshot.GetAlarm("pressure-high").IsLatched);
        Assert.Equal(3L, Assert.Single(returned.Snapshot.Events).Sequence);

        var reset = solver.Step(
            safeSignals,
            fixture.ProtectionSnapshot,
            returned.CandidateState,
            new AlarmSystemInputs(fixture.Alarms, resetAlarmIds: new[] { "pressure-high" }));
        Assert.Equal(AlarmAnnunciatorState.Normal, reset.Snapshot.GetAlarm("pressure-high").AnnunciatorState);
        Assert.True(reset.Snapshot.GetAlarm("pressure-high").ResetAccepted);
        Assert.Equal(4L, Assert.Single(reset.Snapshot.Events).Sequence);
    }

    [Fact]
    public void AcknowledgeOrAlarmReset_NeverResetsLatchedProtection()
    {
        var fixture = CreateFixture(protectionTripActive: true);
        var solver = new AlarmSystemSolver(fixture.Alarms);
        var active = solver.Step(
            fixture.Signals,
            fixture.ProtectionSnapshot,
            AlarmSystemState.CreateInitial(fixture.Alarms),
            new AlarmSystemInputs(fixture.Alarms, acknowledgeAll: true, resetAll: true));

        Assert.True(fixture.ProtectionSnapshot.ReactorScramActive);
        Assert.True(active.Snapshot.GetAlarm("scram-active").ConditionActive);
        Assert.True(active.Snapshot.GetAlarm("scram-active").IsLatched);
        Assert.False(active.Snapshot.GetAlarm("scram-active").ResetAccepted);
    }

    [Fact]
    public void FirstOut_UsesDeterministicActivationOrderingAndPersistsUntilReset()
    {
        var fixture = CreateFixture(protectionTripActive: true);
        var solver = new AlarmSystemSolver(fixture.Alarms);
        var result = solver.Step(
            fixture.Signals,
            fixture.ProtectionSnapshot,
            AlarmSystemState.CreateInitial(fixture.Alarms),
            new AlarmSystemInputs(fixture.Alarms));

        var group = Assert.Single(result.Snapshot.FirstOutGroups);
        Assert.Equal("reactor-first-out", group.GroupId);
        Assert.Equal("pressure-high", group.FirstOutAlarmId);
        Assert.True(result.Snapshot.GetAlarm("pressure-high").IsFirstOut);
        Assert.False(result.Snapshot.GetAlarm("scram-active").IsFirstOut);
        Assert.Equal(new[] { 1L, 2L }, result.Snapshot.Events.Select(static item => item.Sequence).ToArray());
    }

    [Fact]
    public void NonLatchingAlarm_ClearsAutomaticallyAndDoesNotRetainAnnunciatorMemory()
    {
        var fixture = CreateFixture();
        var solver = new AlarmSystemSolver(fixture.Alarms);
        var activeSignals = WithValue(fixture.Signals, "speed", 3_200d);
        var active = solver.Step(
            activeSignals,
            fixture.ProtectionSnapshot,
            AlarmSystemState.CreateInitial(fixture.Alarms),
            new AlarmSystemInputs(fixture.Alarms));
        Assert.Equal(AlarmAnnunciatorState.ActiveUnacknowledged, active.Snapshot.GetAlarm("speed-high").AnnunciatorState);

        var cleared = solver.Step(
            WithValue(activeSignals, "speed", 2_900d),
            fixture.ProtectionSnapshot,
            active.CandidateState,
            new AlarmSystemInputs(fixture.Alarms));
        var alarm = cleared.Snapshot.GetAlarm("speed-high");
        Assert.False(alarm.IsAnnunciated);
        Assert.False(alarm.IsLatched);
        Assert.Equal(AlarmAnnunciatorState.Normal, alarm.AnnunciatorState);
    }

    [Fact]
    public void InvalidMeasurementPolicy_IsExplicitAndDoesNotFallbackToTruePlantState()
    {
        var fixture = CreateFixture();
        var invalidAlarmDefinition = new AlarmSystemDefinition(
            "invalid-alarm-system",
            fixture.Instrumentation,
            fixture.ProtectionDefinition,
            new[]
            {
                new AlarmDefinition(
                    "pressure-unavailable",
                    "Pressure unavailable",
                    AlarmSeverity.Warning,
                    AlarmLatchingMode.NonLatching,
                    new MeasuredAlarmConditionDefinition("pressure", AlarmComparison.High, 99_000_000d, activeOnInvalidMeasurement: true)),
            });
        var invalidSignal = fixture.Signals.GetSignal("pressure") with
        {
            EngineeringValue = null,
            ScaledValue = null,
            Validity = SignalValidity.Invalid,
            Quality = SignalQuality.Unavailable,
            ActiveFaultMode = SensorFaultMode.Unavailable,
        };
        var frame = new MeasuredSignalFrame(
            fixture.Instrumentation,
            fixture.Signals.Signals.Select(signal => signal.ChannelId == "pressure" ? invalidSignal : signal));

        var result = new AlarmSystemSolver(invalidAlarmDefinition).Step(
            frame,
            fixture.ProtectionSnapshot,
            AlarmSystemState.CreateInitial(invalidAlarmDefinition),
            new AlarmSystemInputs(invalidAlarmDefinition));

        Assert.True(result.Snapshot.GetAlarm("pressure-unavailable").ConditionActive);
    }

    [Fact]
    public void Definition_RejectsUnknownMeasuredOrProtectionSource()
    {
        var fixture = CreateFixture();
        Assert.Throws<KeyNotFoundException>(() => new AlarmSystemDefinition(
            "bad-measurement",
            fixture.Instrumentation,
            fixture.ProtectionDefinition,
            new[]
            {
                new AlarmDefinition("bad", "Bad", AlarmSeverity.Warning, AlarmLatchingMode.NonLatching,
                    new MeasuredAlarmConditionDefinition("missing", AlarmComparison.High, 1d)),
            }));
        Assert.Throws<KeyNotFoundException>(() => new AlarmSystemDefinition(
            "bad-protection",
            fixture.Instrumentation,
            fixture.ProtectionDefinition,
            new[]
            {
                new AlarmDefinition("bad", "Bad", AlarmSeverity.Trip, AlarmLatchingMode.LatchedUntilReset,
                    new ProtectionFunctionAlarmConditionDefinition("missing")),
            }));
    }

    private static Fixture CreateFixture(bool protectionTripActive = false)
    {
        var controlFixture = TurbineSecondaryControlSolverTests.CreateFixture();
        var protectionDefinition = new ProtectionSystemDefinition(
            "protection",
            controlFixture.FullPlantDefinition,
            controlFixture.Instrumentation,
            new[]
            {
                new ProtectionFunctionDefinition(
                    "high-pressure-trip",
                    "pressure",
                    ProtectionComparison.High,
                    5_000_000d,
                    4_500_000d,
                    ProtectionAction.ReactorScram),
            });
        var protectionSignals = protectionTripActive ? controlFixture.Signals : WithValue(controlFixture.Signals, "pressure", 4_000_000d);
        var protectionStep = new ProtectionSystemSolver(protectionDefinition).Step(
            protectionSignals,
            ProtectionSystemState.CreateInitial(protectionDefinition),
            new ProtectionSystemInputs(protectionDefinition));
        var alarms = new AlarmSystemDefinition(
            "alarms",
            controlFixture.Instrumentation,
            protectionDefinition,
            new[]
            {
                new AlarmDefinition(
                    "pressure-high",
                    "Steam drum pressure high",
                    AlarmSeverity.Warning,
                    AlarmLatchingMode.LatchedUntilReset,
                    new MeasuredAlarmConditionDefinition("pressure", AlarmComparison.High, 5_500_000d),
                    "reactor-first-out"),
                new AlarmDefinition(
                    "scram-active",
                    "Reactor SCRAM active",
                    AlarmSeverity.Trip,
                    AlarmLatchingMode.LatchedUntilReset,
                    new ProtectionActionAlarmConditionDefinition(ProtectionAction.ReactorScram),
                    "reactor-first-out"),
                new AlarmDefinition(
                    "speed-high",
                    "Turbine speed high",
                    AlarmSeverity.Warning,
                    AlarmLatchingMode.NonLatching,
                    new MeasuredAlarmConditionDefinition("speed", AlarmComparison.High, 3_100d)),
            });
        return new Fixture(
            controlFixture.Instrumentation,
            controlFixture.Signals,
            protectionDefinition,
            protectionStep.Snapshot,
            alarms);
    }

    private static MeasuredSignalFrame WithValue(MeasuredSignalFrame source, string channelId, double value)
        => new(
            source.Definition,
            source.Signals.Select(signal => signal.ChannelId == channelId
                ? signal with { EngineeringValue = value, ScaledValue = value }
                : signal));

    private sealed record Fixture(
        InstrumentationSystemDefinition Instrumentation,
        MeasuredSignalFrame Signals,
        ProtectionSystemDefinition ProtectionDefinition,
        ProtectionSystemSnapshot ProtectionSnapshot,
        AlarmSystemDefinition Alarms);
}
