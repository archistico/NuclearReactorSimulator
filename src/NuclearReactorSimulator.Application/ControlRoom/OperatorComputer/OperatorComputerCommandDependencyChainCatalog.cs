using System.Collections.ObjectModel;
using NuclearReactorSimulator.Application.ControlRoom;

namespace NuclearReactorSimulator.Application.ControlRoom.OperatorComputer;

public enum OperatorComputerCommandDependencyStepKind
{
    CommandIntent = 0,
    ControlOrActuatorState = 1,
    PhysicalProcessPath = 2,
    MeasurementOrModelObservation = 3,
    ProtectionOrAlarmRelation = 4,
}

public sealed record OperatorComputerCommandDependencyStep(
    int Sequence,
    OperatorComputerCommandDependencyStepKind Kind,
    OperatorComputerCommandConsequenceReference? Reference,
    OperatorComputerInformationProvenance? Provenance,
    string Explanation);

public sealed record OperatorComputerCommandDependencyChainProjection(
    ControlRoomCommand Command,
    OperatorComputerCommandConsequenceMappingStatus MappingStatus,
    IReadOnlyList<OperatorComputerCommandDependencyStep> Steps,
    string MappingNote)
{
    public bool HasAuthoredChain => MappingStatus == OperatorComputerCommandConsequenceMappingStatus.Authored;
}

/// <summary>
/// M10.9.5.2 authored dependency-chain projection. This is a bounded presentation map over existing command, mimic,
/// published-state and monitor contracts. It never traverses the plant graph, never dispatches a command and never infers
/// a numerical future state.
/// </summary>
public static class OperatorComputerCommandDependencyChainCatalog
{
    private const string NoAuthoredChain = "NO AUTHORED DEPENDENCY CHAIN";

    public static OperatorComputerCommandDependencyChainProjection Project(ControlRoomCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        var consequence = OperatorComputerCommandConsequenceCatalog.Project(command);
        if (!consequence.HasAuthoredMap)
        {
            return Unmapped(command, $"{NoAuthoredChain} · {consequence.MappingNote}");
        }

        var authored = AuthoredCore(command, consequence.CanonicalCommandTarget);
        if (authored.Count == 0)
        {
            return Unmapped(command, $"{NoAuthoredChain} FOR {command.Kind}.");
        }

        var steps = new List<OperatorComputerCommandDependencyStep>(
            1 + authored.Count + consequence.MonitorTargets.Count);

        Add(steps,
            OperatorComputerCommandDependencyStepKind.CommandIntent,
            consequence.CanonicalCommandTarget,
            null,
            consequence.DirectIntentText);

        foreach (var step in authored)
        {
            Add(steps, step.Kind, step.Reference, step.Provenance, step.Explanation);
        }

        foreach (var monitor in consequence.MonitorTargets)
        {
            Add(steps,
                OperatorComputerCommandDependencyStepKind.MeasurementOrModelObservation,
                monitor.Target,
                monitor.Provenance,
                monitor.Reason);
        }

        return new OperatorComputerCommandDependencyChainProjection(
            command,
            OperatorComputerCommandConsequenceMappingStatus.Authored,
            new ReadOnlyCollection<OperatorComputerCommandDependencyStep>(steps),
            "AUTHORED BOUNDED DEPENDENCY CHAIN · PRESENTATION ONLY · NO AUTOMATIC GRAPH TRAVERSAL");
    }

    private static IReadOnlyList<CoreStep> AuthoredCore(
        ControlRoomCommand command,
        OperatorComputerCommandConsequenceReference? commandTarget)
        => command.Kind switch
        {
            ControlRoomCommandKind.Run => Core(
                Control(Published(nameof(ControlRoomSnapshot.RunState), "RUNTIME STATE"), "The runtime coordinator owns the accepted RUN state.")),

            ControlRoomCommandKind.Pause => Core(
                Control(Published(nameof(ControlRoomSnapshot.RunState), "RUNTIME STATE"), "The runtime coordinator owns the accepted PAUSE state.")),

            ControlRoomCommandKind.SingleStep => Core(
                Control(Published(nameof(ControlRoomSnapshot.RunState), "RUNTIME STATE"), "Single-step is admitted only through the paused runtime coordinator."),
                Process(Published(nameof(ControlRoomSnapshot.LogicalStep), "LOGICAL STEP"), "One canonical fixed logical step is the bounded runtime path.")),

            ControlRoomCommandKind.ReactorScram => Core(
                Control(Mimic("reactor-core", "REACTOR CORE"), "The manual request enters canonical reactor protection ownership."),
                Protection(Published(nameof(ControlRoomSnapshot.ReactorScramActive), "REACTOR SCRAM STATE"), "Protection latching remains authoritative over the requested action.")),

            ControlRoomCommandKind.ProtectionReset => Core(
                Control(Published(nameof(ControlRoomSnapshot.ProtectionReset), "PROTECTION RESET READINESS"), "Reset is evaluated by the canonical protection owner."),
                Protection(Published(nameof(ControlRoomSnapshot.AnyTripActive), "ANY TRIP ACTIVE"), "Existing trip latches and reset conditions remain authoritative.")),

            ControlRoomCommandKind.ControlRodInsert => Core(
                Control(Target(commandTarget), "The selected canonical rod or rod-group motion request is the actuator/control target."),
                Process(Mimic("reactor-core", "REACTOR CORE"), "Rod motion acts only through the existing reactor-core control path.")),

            ControlRoomCommandKind.ControlRodHold => Core(
                Control(Target(commandTarget), "The selected canonical rod or rod-group HOLD request is the actuator/control target."),
                Process(Mimic("reactor-core", "REACTOR CORE"), "Rod-position state remains part of the existing reactor-core control path.")),

            ControlRoomCommandKind.ControlRodWithdraw => Core(
                Control(Target(commandTarget), "The selected canonical rod or rod-group motion request is the actuator/control target."),
                Process(Mimic("reactor-core", "REACTOR CORE"), "Rod motion acts only through the existing reactor-core control path."),
                Protection(Published("ReactorCore.RodWithdrawalInhibited", "ROD WITHDRAWAL INTERLOCK"), "Canonical withdrawal inhibit remains authoritative.")),

            ControlRoomCommandKind.MainCirculationPumpStart => Core(
                Control(Target(commandTarget), "The selected canonical main-circulation pump is the actuator target."),
                Process(Mimic("main-circulation", "MAIN CIRCULATION"), "Pump state changes the existing main-circulation subsystem path."),
                Process(Connection("mcp-core", "MCP → REACTOR PRIMARY COOLANT"), "The whole-plant mimic publishes the primary-coolant path from main circulation toward the reactor core.")),

            ControlRoomCommandKind.MainCirculationPumpStop => Core(
                Control(Target(commandTarget), "The selected canonical main-circulation pump is the actuator target."),
                Process(Mimic("main-circulation", "MAIN CIRCULATION"), "Pump state changes the existing main-circulation subsystem path."),
                Process(Connection("mcp-core", "MCP → REACTOR PRIMARY COOLANT"), "The whole-plant mimic publishes the primary-coolant path from main circulation toward the reactor core.")),

            ControlRoomCommandKind.TurbineTrip => Core(
                Control(Mimic("turbine", "STEAM TURBINE"), "The manual trip request enters canonical turbine protection ownership."),
                Process(Connection("turbine-generator", "TURBINE → GENERATOR SHAFT"), "Loss of turbine mechanical support is represented by the existing shaft path."),
                Protection(Published(nameof(ControlRoomSnapshot.TurbineTripActive), "TURBINE TRIP STATE"), "The turbine-trip latch remains authoritative.")),

            ControlRoomCommandKind.GeneratorTrip => Core(
                Control(Mimic("generator", "GENERATOR"), "The manual trip request enters canonical generator protection ownership."),
                Process(Connection("generator-grid", "GENERATOR → GRID EXCHANGE"), "Generator/grid exchange uses the existing electrical connection path."),
                Protection(Published(nameof(ControlRoomSnapshot.GeneratorTripActive), "GENERATOR TRIP STATE"), "The generator-trip latch remains authoritative.")),

            ControlRoomCommandKind.GeneratorBreakerClose => Core(
                Control(Target(commandTarget), "The canonical generator breaker is the actuator target."),
                Process(Connection("generator-grid", "GENERATOR → GRID EXCHANGE"), "A permitted close enables the existing generator/grid electrical path."),
                Protection(Published("Electrical.Generators.SynchronizationConditionsSatisfied", "SYNCHRONIZATION PERMISSIVE"), "Synchronization permissive and generator protection remain authoritative.")),

            ControlRoomCommandKind.GeneratorBreakerOpen => Core(
                Control(Target(commandTarget), "The canonical generator breaker is the actuator target."),
                Process(Connection("generator-grid", "GENERATOR → GRID EXCHANGE"), "Opening acts on the existing generator/grid electrical path.")),

            ControlRoomCommandKind.TurbineSpeedRaise => TurbineSpeedChain(commandTarget),
            ControlRoomCommandKind.TurbineSpeedLower => TurbineSpeedChain(commandTarget),
            ControlRoomCommandKind.GeneratorLoadRaise => GeneratorLoadChain(commandTarget),
            ControlRoomCommandKind.GeneratorLoadLower => GeneratorLoadChain(commandTarget),

            ControlRoomCommandKind.AlarmAcknowledge => Core(
                Control(Target(commandTarget), "The selected canonical alarm annunciator entry is the command target."),
                Protection(Published(nameof(ControlRoomSnapshot.AlarmEvents), "ALARM EVENT STATE"), "Acknowledgement changes annunciator state only; the initiating plant condition remains independently owned.")),

            ControlRoomCommandKind.AlarmReset => Core(
                Control(Target(commandTarget), "The selected canonical alarm entry is the reset target."),
                Protection(Published("AlarmEvents.Alarms.CanReset", "ALARM RESET ELIGIBILITY"), "Canonical reset eligibility remains authoritative.")),

            ControlRoomCommandKind.AlarmAcknowledgeAll => Core(
                Control(Published(nameof(ControlRoomSnapshot.AlarmEvents), "ALARM EVENT STATE"), "The alarm owner evaluates acknowledgement for all currently eligible alarms."),
                Protection(Published(nameof(ControlRoomSnapshot.UnacknowledgedAlarmCount), "UNACKNOWLEDGED ALARM COUNT"), "Annunciator state is separate from the plant conditions that caused alarms.")),

            ControlRoomCommandKind.AlarmResetAll => Core(
                Control(Published(nameof(ControlRoomSnapshot.AlarmEvents), "ALARM EVENT STATE"), "The alarm owner evaluates reset for all currently eligible alarms."),
                Protection(Published(nameof(ControlRoomSnapshot.AlarmEvents), "ALARM EVENT STATE"), "Canonical reset rules remain authoritative for every alarm.")),

            ControlRoomCommandKind.TurbineValveOpen => TurbineValveChain(commandTarget),
            ControlRoomCommandKind.TurbineValveClose => TurbineValveChain(commandTarget),

            ControlRoomCommandKind.TurbineControlValveManualMode => TurbineControlValveModeChain(commandTarget),
            ControlRoomCommandKind.TurbineControlValveAutomaticMode => TurbineControlValveModeChain(commandTarget),

            ControlRoomCommandKind.TurbineControlValveManualDemandSet => Core(
                Control(Target(commandTarget), "The canonical control-valve controller/manual demand is the actuator-control target."),
                Process(Connection("drums-turbine", "STEAM DRUMS → TURBINE MAIN STEAM"), "Manual valve demand can influence the existing main-steam admission path without guaranteeing a numeric response."),
                Process(Connection("turbine-generator", "TURBINE → GENERATOR SHAFT"), "Any resulting mechanical response propagates only through the existing shaft path."),
                Protection(Published(nameof(ControlRoomSnapshot.AnyTripActive), "PROTECTION STATE"), "Manual mode and protection authority remain canonical prerequisites.")),

            _ => Array.Empty<CoreStep>(),
        };

    private static IReadOnlyList<CoreStep> TurbineSpeedChain(OperatorComputerCommandConsequenceReference? commandTarget)
        => Core(
            Control(Target(commandTarget), "The selected canonical turbine-rotor speed controller is the control target."),
            Process(Mimic("turbine", "STEAM TURBINE"), "The request acts through the existing turbine/governor path."),
            Process(Connection("turbine-generator", "TURBINE → GENERATOR SHAFT"), "Rotor response reaches the electrical island only through the existing shaft path."),
            Protection(Published(nameof(ControlRoomSnapshot.AnyTripActive), "PROTECTION STATE"), "Turbine/generator protection remains authoritative."));

    private static IReadOnlyList<CoreStep> GeneratorLoadChain(OperatorComputerCommandConsequenceReference? commandTarget)
        => Core(
            Control(Target(commandTarget), "The canonical generator requested-load controller is the control target."),
            Process(Mimic("turbine", "STEAM TURBINE"), "Requested electrical load is supported through the existing turbine/governor mechanical path; achieved output is not guaranteed."),
            Process(Connection("turbine-generator", "TURBINE → GENERATOR SHAFT"), "Mechanical support reaches the generator through the existing shaft path."),
            Process(Connection("generator-grid", "GENERATOR → GRID EXCHANGE"), "Electrical output reaches the external grid only through the existing breaker/grid path."),
            Protection(Published(nameof(ControlRoomSnapshot.AnyTripActive), "PROTECTION STATE"), "Generator/turbine protection remains authoritative."));

    private static IReadOnlyList<CoreStep> TurbineValveChain(OperatorComputerCommandConsequenceReference? commandTarget)
        => Core(
            Control(Target(commandTarget), "The selected canonical STOP/ADMISSION valve is the actuator target."),
            Process(Connection("drums-turbine", "STEAM DRUMS → TURBINE MAIN STEAM"), "Valve motion acts on the existing main-steam admission path."),
            Process(Connection("turbine-condenser", "TURBINE → CONDENSER EXHAUST"), "Downstream exhaust conditions remain on the existing turbine/condenser path."),
            Protection(Published(nameof(ControlRoomSnapshot.AnyTripActive), "PROTECTION STATE"), "Protection and actuator authority remain canonical."));

    private static IReadOnlyList<CoreStep> TurbineControlValveModeChain(OperatorComputerCommandConsequenceReference? commandTarget)
        => Core(
            Control(Target(commandTarget), "The canonical turbine control-valve controller mode is the control target."),
            Process(Mimic("turbine", "STEAM TURBINE"), "Controller ownership changes only the existing turbine admission-control path."),
            Process(Connection("drums-turbine", "STEAM DRUMS → TURBINE MAIN STEAM"), "Steam admission remains the existing main-steam physical path."),
            Protection(Published(nameof(ControlRoomSnapshot.AnyTripActive), "PROTECTION STATE"), "Protection remains able to override normal admission authority."));

    private static void Add(
        ICollection<OperatorComputerCommandDependencyStep> steps,
        OperatorComputerCommandDependencyStepKind kind,
        OperatorComputerCommandConsequenceReference? reference,
        OperatorComputerInformationProvenance? provenance,
        string explanation)
        => steps.Add(new OperatorComputerCommandDependencyStep(
            steps.Count + 1,
            kind,
            reference,
            provenance,
            explanation));

    private static OperatorComputerCommandDependencyChainProjection Unmapped(ControlRoomCommand command, string note)
        => new(
            command,
            OperatorComputerCommandConsequenceMappingStatus.ExplicitlyUnmapped,
            Array.Empty<OperatorComputerCommandDependencyStep>(),
            note);

    private static IReadOnlyList<CoreStep> Core(params CoreStep[] values)
        => Array.AsReadOnly(values);

    private static CoreStep Control(OperatorComputerCommandConsequenceReference reference, string explanation)
        => new(OperatorComputerCommandDependencyStepKind.ControlOrActuatorState, reference, null, explanation);

    private static CoreStep Process(OperatorComputerCommandConsequenceReference reference, string explanation)
        => new(OperatorComputerCommandDependencyStepKind.PhysicalProcessPath, reference, null, explanation);

    private static CoreStep Protection(OperatorComputerCommandConsequenceReference reference, string explanation)
        => new(OperatorComputerCommandDependencyStepKind.ProtectionOrAlarmRelation, reference, OperatorComputerInformationProvenance.CanonicalState, explanation);

    private static OperatorComputerCommandConsequenceReference Target(OperatorComputerCommandConsequenceReference? target)
        => target ?? throw new InvalidOperationException("Authored targeted dependency chain requires a canonical command target.");

    private static OperatorComputerCommandConsequenceReference Mimic(string id, string label)
        => new(OperatorComputerCommandConsequenceReferenceKind.PlantMimicElement, id, label);

    private static OperatorComputerCommandConsequenceReference Connection(string id, string label)
        => new(OperatorComputerCommandConsequenceReferenceKind.PlantMimicConnection, id, label);

    private static OperatorComputerCommandConsequenceReference Published(string id, string label)
        => new(OperatorComputerCommandConsequenceReferenceKind.PublishedState, id, label);

    private sealed record CoreStep(
        OperatorComputerCommandDependencyStepKind Kind,
        OperatorComputerCommandConsequenceReference Reference,
        OperatorComputerInformationProvenance? Provenance,
        string Explanation);
}
