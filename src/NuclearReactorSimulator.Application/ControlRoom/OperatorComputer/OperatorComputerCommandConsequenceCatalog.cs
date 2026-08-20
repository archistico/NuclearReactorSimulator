using System.Collections.ObjectModel;
using System.Globalization;
using NuclearReactorSimulator.Application.ControlRoom;

namespace NuclearReactorSimulator.Application.ControlRoom.OperatorComputer;

public enum OperatorComputerCommandConsequenceMappingStatus
{
    Authored = 0,
    ExplicitlyUnmapped = 1,
}

public enum OperatorComputerCommandConsequenceRelation
{
    IncreasesExpectedDemandOn = 0,
    DecreasesExpectedDemandOn = 1,
    EnablesPath = 2,
    DisablesPath = 3,
    Affects = 4,
    MayAffect = 5,
    ProtectionMayOverride = 6,
}

public enum OperatorComputerCommandConsequenceReferenceKind
{
    PlantMimicElement = 0,
    PublishedState = 1,
    CommandTarget = 2,
}

public sealed record OperatorComputerCommandConsequenceReference(
    OperatorComputerCommandConsequenceReferenceKind Kind,
    string Id,
    string Label);

public sealed record OperatorComputerCommandExpectedInfluence(
    OperatorComputerCommandConsequenceRelation Relation,
    OperatorComputerCommandConsequenceReference Target,
    string Explanation);

public sealed record OperatorComputerCommandMonitorTarget(
    OperatorComputerCommandConsequenceReference Target,
    OperatorComputerInformationProvenance Provenance,
    string Reason);

public sealed record OperatorComputerCommandConsequenceDefinition(
    ControlRoomCommandKind CommandKind,
    IReadOnlyList<ControlRoomCommandTargetKind> SupportedTargetKinds,
    string DirectIntent,
    IReadOnlyList<OperatorComputerCommandExpectedInfluence> ExpectedInfluences,
    IReadOnlyList<OperatorComputerCommandConsequenceReference> PermissiveReferences,
    IReadOnlyList<OperatorComputerCommandMonitorTarget> MonitorTargets);

public sealed record OperatorComputerCommandConsequenceProjection(
    ControlRoomCommand Command,
    OperatorComputerCommandConsequenceMappingStatus MappingStatus,
    string DirectIntentText,
    OperatorComputerCommandConsequenceReference? CanonicalCommandTarget,
    IReadOnlyList<OperatorComputerCommandExpectedInfluence> ExpectedInfluences,
    IReadOnlyList<OperatorComputerCommandConsequenceReference> PermissiveReferences,
    IReadOnlyList<OperatorComputerCommandMonitorTarget> MonitorTargets,
    string MappingNote)
{
    public bool HasAuthoredMap => MappingStatus == OperatorComputerCommandConsequenceMappingStatus.Authored;
}

/// <summary>
/// M10.9.5.1 authored command-consequence semantics. This catalog is presentation metadata only: it never dispatches a
/// command, never writes plant state and never predicts a future numeric plant value.
/// </summary>
public static class OperatorComputerCommandConsequenceCatalog
{
    private const string NoAuthoredMap = "NO AUTHORED CONSEQUENCE MAP";

    private static readonly IReadOnlyList<OperatorComputerCommandConsequenceDefinition> Catalog =
        new ReadOnlyCollection<OperatorComputerCommandConsequenceDefinition>(new[]
        {
            Global(ControlRoomCommandKind.Run, "REQUEST CONTINUOUS RUNTIME ADVANCE",
                Influences(Affects(Published(nameof(ControlRoomSnapshot.RunState), "RUNTIME STATE"), "Changes the host runtime mode when the coordinator accepts the request.")),
                Permissives(Published(nameof(ControlRoomSnapshot.RunState), "RUNTIME STATE")),
                Monitors(State(nameof(ControlRoomSnapshot.RunState), "RUNTIME STATE", "Confirm RUNNING/PAUSED state."), State(nameof(ControlRoomSnapshot.LogicalStep), "LOGICAL STEP", "Confirm logical simulation time advances."))),

            Global(ControlRoomCommandKind.Pause, "REQUEST RUNTIME PAUSE",
                Influences(Affects(Published(nameof(ControlRoomSnapshot.RunState), "RUNTIME STATE"), "Changes the host runtime mode when the coordinator accepts the request.")),
                Permissives(Published(nameof(ControlRoomSnapshot.RunState), "RUNTIME STATE")),
                Monitors(State(nameof(ControlRoomSnapshot.RunState), "RUNTIME STATE", "Confirm the runtime is paused."), State(nameof(ControlRoomSnapshot.LogicalStep), "LOGICAL STEP", "Confirm continuous logical-step advance stops."))),

            Global(ControlRoomCommandKind.SingleStep, "REQUEST ONE DETERMINISTIC RUNTIME STEP",
                Influences(Affects(Published(nameof(ControlRoomSnapshot.LogicalStep), "LOGICAL STEP"), "Advances the paused runtime by one canonical fixed step when accepted.")),
                Permissives(Published(nameof(ControlRoomSnapshot.RunState), "RUNTIME STATE")),
                Monitors(State(nameof(ControlRoomSnapshot.RunState), "RUNTIME STATE", "Single-step remains a paused-runtime operation."), State(nameof(ControlRoomSnapshot.LogicalStep), "LOGICAL STEP", "Confirm exactly one logical step is advanced."))),

            Global(ControlRoomCommandKind.ReactorScram, "LATCH MANUAL REACTOR SCRAM REQUEST",
                Influences(Affects(Mimic("reactor-core", "REACTOR CORE"), "Requests the canonical reactor protection action; protection remains authoritative."), ProtectionOverride(Mimic("reactor-core", "REACTOR CORE"), "Canonical protection/interlock state remains authoritative over reactor control.")),
                Permissives(Published(nameof(ControlRoomSnapshot.ReactorScramActive), "REACTOR SCRAM STATE")),
                Monitors(State(nameof(ControlRoomSnapshot.ReactorScramActive), "REACTOR SCRAM", "Confirm the canonical SCRAM latch."), Measured("ReactorCore.ReactorThermalPower", "REACTOR THERMAL POWER", "Observe the measured reactor response after the protection action."))),

            Global(ControlRoomCommandKind.ProtectionReset, "REQUEST CANONICAL PROTECTION RESET",
                Influences(Affects(Published(nameof(ControlRoomSnapshot.ProtectionReset), "PROTECTION RESET READINESS"), "Requests reset only; the canonical protection owner revalidates reset conditions.")),
                Permissives(Published(nameof(ControlRoomSnapshot.ProtectionReset), "PROTECTION RESET READINESS"), Published(nameof(ControlRoomSnapshot.AnyTripActive), "ANY TRIP ACTIVE")),
                Monitors(State(nameof(ControlRoomSnapshot.ReactorScramActive), "REACTOR SCRAM", "Confirm whether the reactor trip remains latched."), State(nameof(ControlRoomSnapshot.TurbineTripActive), "TURBINE TRIP", "Confirm whether the turbine trip remains latched."), State(nameof(ControlRoomSnapshot.GeneratorTripActive), "GENERATOR TRIP", "Confirm whether the generator trip remains latched."))),

            Targeted(ControlRoomCommandKind.ControlRodInsert, "REQUEST CONTROL-ROD INSERTION", Targets(ControlRoomCommandTargetKind.ControlRod, ControlRoomCommandTargetKind.ControlRodGroup),
                Influences(Affects(Mimic("reactor-core", "REACTOR CORE"), "Changes the requested manual rod motion for the canonical rod or rod group."), ProtectionOverride(Mimic("reactor-core", "REACTOR CORE"), "Protection/interlock logic remains authoritative over permitted rod motion.")),
                Permissives(),
                Monitors(Model("ReactorCore.AverageRodWithdrawal", "AVERAGE ROD WITHDRAWAL", "Observe actual rod-position response."), Measured("ReactorCore.ReactorThermalPower", "REACTOR THERMAL POWER", "Observe measured reactor response without treating it as a guaranteed command result."))),

            Targeted(ControlRoomCommandKind.ControlRodHold, "REQUEST CONTROL-ROD HOLD", Targets(ControlRoomCommandTargetKind.ControlRod, ControlRoomCommandTargetKind.ControlRodGroup),
                Influences(Affects(Mimic("reactor-core", "REACTOR CORE"), "Requests HOLD for the canonical rod or rod group."), ProtectionOverride(Mimic("reactor-core", "REACTOR CORE"), "Protection/interlock logic remains authoritative.")),
                Permissives(),
                Monitors(Model("ReactorCore.AverageRodWithdrawal", "AVERAGE ROD WITHDRAWAL", "Confirm rod-position response is consistent with HOLD."), Measured("ReactorCore.ReactorThermalPower", "REACTOR THERMAL POWER", "Continue monitoring measured reactor response."))),

            Targeted(ControlRoomCommandKind.ControlRodWithdraw, "REQUEST CONTROL-ROD WITHDRAWAL", Targets(ControlRoomCommandTargetKind.ControlRod, ControlRoomCommandTargetKind.ControlRodGroup),
                Influences(Affects(Mimic("reactor-core", "REACTOR CORE"), "Changes the requested manual rod motion for the canonical rod or rod group."), ProtectionOverride(Mimic("reactor-core", "REACTOR CORE"), "Withdrawal may be inhibited by the canonical protection/interlock state.")),
                Permissives(Published("ReactorCore.RodWithdrawalInhibited", "ROD WITHDRAWAL INTERLOCK")),
                Monitors(Model("ReactorCore.AverageRodWithdrawal", "AVERAGE ROD WITHDRAWAL", "Observe actual rod-position response."), Measured("ReactorCore.ReactorThermalPower", "REACTOR THERMAL POWER", "Observe measured reactor response without treating it as a guaranteed command result."))),

            Targeted(ControlRoomCommandKind.MainCirculationPumpStart, "REQUEST MAIN-CIRCULATION PUMP START", Targets(ControlRoomCommandTargetKind.Pump),
                Influences(Enables(Mimic("main-circulation", "MAIN CIRCULATION"), "Requests the selected canonical MCP to run."), MayAffect(Mimic("reactor-core", "REACTOR CORE"), "Primary-coolant circulation may change core cooling/flow conditions."), MayAffect(Mimic("steam-drums", "STEAM DRUMS"), "Primary-circuit circulation may change drum return/recirculation conditions.")),
                Permissives(Published("PrimaryCircuit.Pumps.IsRunning", "MCP RUN/STOP STATE")),
                Monitors(State("PrimaryCircuit.Pumps.IsRunning", "MCP RUN/STOP STATE", "Confirm the selected pump state and resulting circulation."), Model("PrimaryCircuit.TotalPrimaryMass", "PRIMARY INVENTORY", "Continue monitoring primary inventory while circulation changes."))),

            Targeted(ControlRoomCommandKind.MainCirculationPumpStop, "REQUEST MAIN-CIRCULATION PUMP STOP", Targets(ControlRoomCommandTargetKind.Pump),
                Influences(Disables(Mimic("main-circulation", "MAIN CIRCULATION"), "Requests the selected canonical MCP to stop."), MayAffect(Mimic("reactor-core", "REACTOR CORE"), "Reduced circulation may change core cooling/flow conditions."), MayAffect(Mimic("steam-drums", "STEAM DRUMS"), "Primary-circuit circulation may change drum return/recirculation conditions.")),
                Permissives(Published("PrimaryCircuit.Pumps.IsRunning", "MCP RUN/STOP STATE")),
                Monitors(State("PrimaryCircuit.Pumps.IsRunning", "MCP RUN/STOP STATE", "Confirm the selected pump state and resulting circulation."), Model("PrimaryCircuit.TotalPrimaryMass", "PRIMARY INVENTORY", "Continue monitoring primary inventory while circulation changes."))),

            Global(ControlRoomCommandKind.TurbineTrip, "LATCH MANUAL TURBINE TRIP REQUEST",
                Influences(Disables(Mimic("turbine", "STEAM TURBINE"), "Requests the canonical turbine trip action."), MayAffect(Mimic("generator", "GENERATOR"), "Loss of turbine mechanical support may drive generator/protection response."), MayAffect(Mimic("condenser", "CONDENSER / HOTWELL"), "Turbine steam/exhaust conditions may change after the trip.")),
                Permissives(Published(nameof(ControlRoomSnapshot.TurbineTripActive), "TURBINE TRIP STATE")),
                Monitors(State(nameof(ControlRoomSnapshot.TurbineTripActive), "TURBINE TRIP", "Confirm the canonical turbine-trip latch."), Measured("TurbineSecondary.TotalTurbineShaftPower", "TURBINE SHAFT POWER", "Observe mechanical response."), Measured("Electrical.GrossElectricalOutput", "GROSS ELECTRICAL OUTPUT", "Observe electrical response."))),

            Global(ControlRoomCommandKind.GeneratorTrip, "LATCH MANUAL GENERATOR TRIP REQUEST",
                Influences(Disables(Mimic("generator", "GENERATOR"), "Requests the canonical generator trip action."), Disables(Mimic("grid", "EXTERNAL GRID"), "Generator-grid exchange is expected to be interrupted when the breaker/protection action completes.")),
                Permissives(Published(nameof(ControlRoomSnapshot.GeneratorTripActive), "GENERATOR TRIP STATE")),
                Monitors(State(nameof(ControlRoomSnapshot.GeneratorTripActive), "GENERATOR TRIP", "Confirm the canonical generator-trip latch."), Measured("Electrical.GrossElectricalOutput", "GROSS ELECTRICAL OUTPUT", "Observe electrical export/import response."))),

            Targeted(ControlRoomCommandKind.GeneratorBreakerClose, "REQUEST GENERATOR BREAKER CLOSE", Targets(ControlRoomCommandTargetKind.Breaker),
                Influences(Enables(Mimic("generator", "GENERATOR"), "Requests connection of the canonical generator breaker."), Enables(Mimic("grid", "EXTERNAL GRID"), "A successful close enables generator-grid electrical exchange."), ProtectionOverride(Mimic("generator", "GENERATOR"), "Synchronization permissive and generator protection remain authoritative.")),
                Permissives(Published("Electrical.Generators.BreakerClosed", "GENERATOR BREAKER STATE"), Published("Electrical.Generators.SynchronizationConditionsSatisfied", "SYNCHRONIZATION PERMISSIVE"), Published(nameof(ControlRoomSnapshot.GeneratorTripActive), "GENERATOR TRIP STATE")),
                Monitors(State("Electrical.Generators.BreakerClosed", "GENERATOR BREAKER STATE", "Confirm breaker and synchronization state."), Measured("Electrical.GrossElectricalOutput", "GROSS ELECTRICAL OUTPUT", "Observe actual grid exchange after closing."))),

            Targeted(ControlRoomCommandKind.GeneratorBreakerOpen, "REQUEST GENERATOR BREAKER OPEN", Targets(ControlRoomCommandTargetKind.Breaker),
                Influences(Disables(Mimic("grid", "EXTERNAL GRID"), "Requests disconnection of the canonical generator breaker from grid exchange."), MayAffect(Mimic("generator", "GENERATOR"), "Generator electrical loading changes when disconnected.")),
                Permissives(Published("Electrical.Generators.BreakerClosed", "GENERATOR BREAKER STATE")),
                Monitors(State("Electrical.Generators.BreakerClosed", "GENERATOR BREAKER STATE", "Confirm breaker state."), Measured("Electrical.GrossElectricalOutput", "GROSS ELECTRICAL OUTPUT", "Observe actual grid exchange after opening."))),

            Targeted(ControlRoomCommandKind.TurbineSpeedRaise, "INCREASE TURBINE SPEED SETPOINT REQUEST", Targets(ControlRoomCommandTargetKind.TurbineRotor),
                Influences(IncreaseDemand(Mimic("turbine", "STEAM TURBINE"), "Raises the canonical turbine-speed controller setpoint."), MayAffect(Mimic("generator", "GENERATOR"), "Rotor-speed changes may alter synchronization/electrical conditions."), ProtectionOverride(Mimic("turbine", "STEAM TURBINE"), "Turbine/generator protection remains authoritative.")),
                Permissives(Published(nameof(ControlRoomSnapshot.TurbineTripActive), "TURBINE TRIP STATE"), Published(nameof(ControlRoomSnapshot.GeneratorTripActive), "GENERATOR TRIP STATE")),
                Monitors(Measured("TurbineSecondary.Rotors.Speed", "TURBINE ROTOR SPEED", "Observe actual rotor-speed response."), Model("TurbineSecondary.EffectiveTurbineSteamFlow", "EFFECTIVE TURBINE STEAM FLOW", "Observe the steam-flow path response."))),

            Targeted(ControlRoomCommandKind.TurbineSpeedLower, "DECREASE TURBINE SPEED SETPOINT REQUEST", Targets(ControlRoomCommandTargetKind.TurbineRotor),
                Influences(DecreaseDemand(Mimic("turbine", "STEAM TURBINE"), "Lowers the canonical turbine-speed controller setpoint."), MayAffect(Mimic("generator", "GENERATOR"), "Rotor-speed changes may alter synchronization/electrical conditions."), ProtectionOverride(Mimic("turbine", "STEAM TURBINE"), "Turbine/generator protection remains authoritative.")),
                Permissives(Published(nameof(ControlRoomSnapshot.TurbineTripActive), "TURBINE TRIP STATE"), Published(nameof(ControlRoomSnapshot.GeneratorTripActive), "GENERATOR TRIP STATE")),
                Monitors(Measured("TurbineSecondary.Rotors.Speed", "TURBINE ROTOR SPEED", "Observe actual rotor-speed response."), Model("TurbineSecondary.EffectiveTurbineSteamFlow", "EFFECTIVE TURBINE STEAM FLOW", "Observe the steam-flow path response."))),

            Targeted(ControlRoomCommandKind.GeneratorLoadRaise, "INCREASE GENERATOR LOAD REQUEST", Targets(ControlRoomCommandTargetKind.Generator),
                Influences(IncreaseDemand(Mimic("generator", "GENERATOR"), "Raises the canonical requested electrical-power setpoint."), MayAffect(Mimic("turbine", "STEAM TURBINE"), "Electrical load demand may require a turbine/governor response."), ProtectionOverride(Mimic("generator", "GENERATOR"), "Generator/turbine protection remains authoritative.")),
                Permissives(Published("Electrical.Generators.BreakerClosed", "GENERATOR BREAKER STATE"), Published(nameof(ControlRoomSnapshot.TurbineTripActive), "TURBINE TRIP STATE"), Published(nameof(ControlRoomSnapshot.GeneratorTripActive), "GENERATOR TRIP STATE")),
                Monitors(Measured("Electrical.GrossElectricalOutput", "GROSS ELECTRICAL OUTPUT", "Observe actual electrical output; do not equate request with achieved output."), Measured("TurbineSecondary.TotalTurbineShaftPower", "TURBINE SHAFT POWER", "Observe mechanical support for the requested electrical load."))),

            Targeted(ControlRoomCommandKind.GeneratorLoadLower, "DECREASE GENERATOR LOAD REQUEST", Targets(ControlRoomCommandTargetKind.Generator),
                Influences(DecreaseDemand(Mimic("generator", "GENERATOR"), "Lowers the canonical requested electrical-power setpoint."), MayAffect(Mimic("turbine", "STEAM TURBINE"), "Electrical load demand may require a turbine/governor response."), ProtectionOverride(Mimic("generator", "GENERATOR"), "Generator/turbine protection remains authoritative.")),
                Permissives(Published("Electrical.Generators.BreakerClosed", "GENERATOR BREAKER STATE"), Published(nameof(ControlRoomSnapshot.TurbineTripActive), "TURBINE TRIP STATE"), Published(nameof(ControlRoomSnapshot.GeneratorTripActive), "GENERATOR TRIP STATE")),
                Monitors(Measured("Electrical.GrossElectricalOutput", "GROSS ELECTRICAL OUTPUT", "Observe actual electrical output; do not equate request with achieved output."), Measured("TurbineSecondary.TotalTurbineShaftPower", "TURBINE SHAFT POWER", "Observe mechanical response."))),

            Targeted(ControlRoomCommandKind.AlarmAcknowledge, "REQUEST ALARM ACKNOWLEDGEMENT", Targets(ControlRoomCommandTargetKind.Alarm),
                Influences(Affects(Published(nameof(ControlRoomSnapshot.AlarmEvents), "ALARM EVENT STATE"), "Changes annunciator acknowledgement state only when the canonical alarm owner accepts it.")),
                Permissives(Published("AlarmEvents.Alarms.CanAcknowledge", "ALARM ACKNOWLEDGE ELIGIBILITY")),
                Monitors(State("AlarmEvents.Alarms.IsAcknowledged", "ALARM ACKNOWLEDGEMENT STATE", "Confirm acknowledgement state; acknowledgement is not a plant-process action."))),

            Targeted(ControlRoomCommandKind.AlarmReset, "REQUEST ALARM RESET", Targets(ControlRoomCommandTargetKind.Alarm),
                Influences(Affects(Published(nameof(ControlRoomSnapshot.AlarmEvents), "ALARM EVENT STATE"), "Requests reset of the selected alarm; canonical reset conditions remain authoritative.")),
                Permissives(Published("AlarmEvents.Alarms.CanReset", "ALARM RESET ELIGIBILITY")),
                Monitors(State("AlarmEvents.Alarms.IsAnnunciated", "ALARM ANNUNCIATOR STATE", "Confirm reset state; reset is not proof that the initiating plant condition changed because of this command."))),

            Global(ControlRoomCommandKind.AlarmAcknowledgeAll, "REQUEST ACKNOWLEDGEMENT OF ALL ELIGIBLE ALARMS",
                Influences(Affects(Published(nameof(ControlRoomSnapshot.AlarmEvents), "ALARM EVENT STATE"), "Changes acknowledgement state only for currently eligible alarms.")),
                Permissives(Published(nameof(ControlRoomSnapshot.AlarmEvents), "ALARM EVENT STATE")),
                Monitors(State(nameof(ControlRoomSnapshot.UnacknowledgedAlarmCount), "UNACKNOWLEDGED ALARM COUNT", "Confirm annunciator acknowledgement response."))),

            Global(ControlRoomCommandKind.AlarmResetAll, "REQUEST RESET OF ALL ELIGIBLE ALARMS",
                Influences(Affects(Published(nameof(ControlRoomSnapshot.AlarmEvents), "ALARM EVENT STATE"), "Requests reset only for alarms satisfying canonical reset conditions.")),
                Permissives(Published(nameof(ControlRoomSnapshot.AlarmEvents), "ALARM EVENT STATE")),
                Monitors(State("AlarmEvents.Alarms", "ALARM STATE", "Confirm which alarms actually reset."))),

            Targeted(ControlRoomCommandKind.TurbineValveOpen, "REQUEST TURBINE STOP/ADMISSION VALVE OPEN", Targets(ControlRoomCommandTargetKind.Valve),
                Influences(Enables(Mimic("turbine", "STEAM TURBINE"), "Requests opening of a canonical turbine STOP or ADMISSION steam-path valve."), MayAffect(Mimic("steam-drums", "STEAM DRUMS"), "Opening the turbine steam path may alter steam export conditions."), MayAffect(Mimic("condenser", "CONDENSER / HOTWELL"), "Turbine steam admission/exhaust may alter condenser conditions."), ProtectionOverride(Mimic("turbine", "STEAM TURBINE"), "Protection and actuator authority remain canonical.")),
                Permissives(Published(nameof(ControlRoomSnapshot.TurbineSecondary), "TURBINE / SECONDARY STATE"), Published(nameof(ControlRoomSnapshot.AnyTripActive), "PROTECTION STATE")),
                Monitors(Model("TurbineSecondary.AdmissionTrains.ControlValvePosition", "TURBINE ADMISSION TRAIN VALVE POSITION", "Confirm actual valve position/state."), Model("TurbineSecondary.EffectiveTurbineSteamFlow", "EFFECTIVE TURBINE STEAM FLOW", "Observe steam-path response."))),

            Targeted(ControlRoomCommandKind.TurbineValveClose, "REQUEST TURBINE STOP/ADMISSION VALVE CLOSE", Targets(ControlRoomCommandTargetKind.Valve),
                Influences(Disables(Mimic("turbine", "STEAM TURBINE"), "Requests closing of a canonical turbine STOP or ADMISSION steam-path valve."), MayAffect(Mimic("steam-drums", "STEAM DRUMS"), "Closing the turbine steam path may alter steam export conditions."), MayAffect(Mimic("condenser", "CONDENSER / HOTWELL"), "Turbine steam admission/exhaust may alter condenser conditions."), ProtectionOverride(Mimic("turbine", "STEAM TURBINE"), "Protection and actuator authority remain canonical.")),
                Permissives(Published(nameof(ControlRoomSnapshot.TurbineSecondary), "TURBINE / SECONDARY STATE"), Published(nameof(ControlRoomSnapshot.AnyTripActive), "PROTECTION STATE")),
                Monitors(Model("TurbineSecondary.AdmissionTrains.ControlValvePosition", "TURBINE ADMISSION TRAIN VALVE POSITION", "Confirm actual valve position/state."), Model("TurbineSecondary.EffectiveTurbineSteamFlow", "EFFECTIVE TURBINE STEAM FLOW", "Observe steam-path response."))),

            Targeted(ControlRoomCommandKind.TurbineControlValveManualMode, "REQUEST TURBINE CONTROL VALVE MANUAL MODE", Targets(ControlRoomCommandTargetKind.Valve),
                Influences(Affects(Mimic("turbine", "STEAM TURBINE"), "Transfers the canonical control-valve controller to MANUAL while preserving the current effective position as the initial manual output."), ProtectionOverride(Mimic("turbine", "STEAM TURBINE"), "Protection remains able to override normal admission authority.")),
                Permissives(Published(nameof(ControlRoomSnapshot.TurbineSecondary), "TURBINE / SECONDARY STATE"), Published(nameof(ControlRoomSnapshot.AnyTripActive), "PROTECTION STATE")),
                Monitors(State("TurbineSecondary.AdmissionTrains.ControlValveManualMode", "CONTROL-VALVE MODE", "Confirm MANUAL/AUTO state."), Model("TurbineSecondary.EffectiveTurbineSteamFlow", "EFFECTIVE TURBINE STEAM FLOW", "Observe resulting steam-path response."))),

            Targeted(ControlRoomCommandKind.TurbineControlValveAutomaticMode, "REQUEST TURBINE CONTROL VALVE AUTOMATIC MODE", Targets(ControlRoomCommandTargetKind.Valve),
                Influences(Affects(Mimic("turbine", "STEAM TURBINE"), "Returns the canonical control-valve controller to AUTOMATIC governor ownership."), ProtectionOverride(Mimic("turbine", "STEAM TURBINE"), "Protection remains able to override normal admission authority.")),
                Permissives(Published(nameof(ControlRoomSnapshot.TurbineSecondary), "TURBINE / SECONDARY STATE"), Published(nameof(ControlRoomSnapshot.AnyTripActive), "PROTECTION STATE")),
                Monitors(State("TurbineSecondary.AdmissionTrains.ControlValveManualMode", "CONTROL-VALVE MODE", "Confirm MANUAL/AUTO state."), Model("TurbineSecondary.EffectiveTurbineSteamFlow", "EFFECTIVE TURBINE STEAM FLOW", "Observe resulting steam-path response."))),

            Targeted(ControlRoomCommandKind.TurbineControlValveManualDemandSet, "REQUEST TURBINE CONTROL VALVE MANUAL DEMAND", Targets(ControlRoomCommandTargetKind.Valve),
                Influences(Affects(Mimic("turbine", "STEAM TURBINE"), "Changes the requested manual control-valve output; sign of plant response depends on current state and is not predicted here."), MayAffect(Mimic("generator", "GENERATOR"), "Steam-admission changes may alter shaft/electrical response."), ProtectionOverride(Mimic("turbine", "STEAM TURBINE"), "MANUAL mode and protection authority remain canonical prerequisites.")),
                Permissives(Published(nameof(ControlRoomSnapshot.TurbineSecondary), "TURBINE / SECONDARY STATE"), Published(nameof(ControlRoomSnapshot.AnyTripActive), "PROTECTION STATE")),
                Monitors(State("TurbineSecondary.AdmissionTrains.ControlValveManualMode", "CONTROL-VALVE MODE", "Confirm MANUAL mode remains active."), Model("TurbineSecondary.AdmissionTrains.ControlValveManualDemand", "CONTROL-VALVE MANUAL DEMAND", "Confirm requested manual demand separately from effective valve position."), Model("TurbineSecondary.EffectiveTurbineSteamFlow", "EFFECTIVE TURBINE STEAM FLOW", "Observe actual steam-flow response."), Measured("TurbineSecondary.TotalTurbineShaftPower", "TURBINE SHAFT POWER", "Observe actual mechanical response."))),
        });

    private static readonly IReadOnlyDictionary<ControlRoomCommandKind, OperatorComputerCommandConsequenceDefinition> ByKind =
        new ReadOnlyDictionary<ControlRoomCommandKind, OperatorComputerCommandConsequenceDefinition>(
            Catalog.ToDictionary(static item => item.CommandKind));

    public static IReadOnlyList<OperatorComputerCommandConsequenceDefinition> Definitions => Catalog;

    public static OperatorComputerCommandConsequenceProjection Project(ControlRoomCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (!ByKind.TryGetValue(command.Kind, out var definition))
        {
            return Unmapped(command, $"{NoAuthoredMap} FOR COMMAND KIND {(int)command.Kind}.");
        }

        if (!TargetShapeMatches(command, definition.SupportedTargetKinds))
        {
            return Unmapped(command, $"{NoAuthoredMap} FOR {command.Kind} / TARGET SHAPE.");
        }

        var target = command.TargetId is null
            ? null
            : new OperatorComputerCommandConsequenceReference(
                OperatorComputerCommandConsequenceReferenceKind.CommandTarget,
                command.TargetId,
                $"{command.TargetKind!.Value.ToString().ToUpperInvariant()}:{command.TargetId}");

        var directIntent = definition.DirectIntent;
        if (command.Kind == ControlRoomCommandKind.TurbineControlValveManualDemandSet && command.NumericValue is { } demand)
        {
            directIntent = string.Concat(
                directIntent,
                " · REQUEST ",
                demand.ToString("0.###", CultureInfo.InvariantCulture),
                "%");
        }

        return new OperatorComputerCommandConsequenceProjection(
            command,
            OperatorComputerCommandConsequenceMappingStatus.Authored,
            directIntent,
            target,
            definition.ExpectedInfluences,
            definition.PermissiveReferences,
            definition.MonitorTargets,
            "AUTHORED QUALITATIVE MAP · NOT A NUMERICAL PREDICTION");
    }

    private static bool TargetShapeMatches(
        ControlRoomCommand command,
        IReadOnlyList<ControlRoomCommandTargetKind> supportedTargetKinds)
    {
        if (supportedTargetKinds.Count == 0)
        {
            return command.TargetId is null && command.TargetKind is null;
        }

        return !string.IsNullOrWhiteSpace(command.TargetId)
            && command.TargetKind is { } targetKind
            && supportedTargetKinds.Contains(targetKind);
    }

    private static OperatorComputerCommandConsequenceProjection Unmapped(ControlRoomCommand command, string note)
        => new(
            command,
            OperatorComputerCommandConsequenceMappingStatus.ExplicitlyUnmapped,
            NoAuthoredMap,
            null,
            Array.Empty<OperatorComputerCommandExpectedInfluence>(),
            Array.Empty<OperatorComputerCommandConsequenceReference>(),
            Array.Empty<OperatorComputerCommandMonitorTarget>(),
            note);

    private static OperatorComputerCommandConsequenceDefinition Global(
        ControlRoomCommandKind kind,
        string directIntent,
        IReadOnlyList<OperatorComputerCommandExpectedInfluence> influences,
        IReadOnlyList<OperatorComputerCommandConsequenceReference> permissives,
        IReadOnlyList<OperatorComputerCommandMonitorTarget> monitors)
        => Targeted(kind, directIntent, Array.Empty<ControlRoomCommandTargetKind>(), influences, permissives, monitors);

    private static OperatorComputerCommandConsequenceDefinition Targeted(
        ControlRoomCommandKind kind,
        string directIntent,
        IReadOnlyList<ControlRoomCommandTargetKind> targetKinds,
        IReadOnlyList<OperatorComputerCommandExpectedInfluence> influences,
        IReadOnlyList<OperatorComputerCommandConsequenceReference> permissives,
        IReadOnlyList<OperatorComputerCommandMonitorTarget> monitors)
        => new(kind, targetKinds, directIntent, influences, permissives, monitors);

    private static IReadOnlyList<ControlRoomCommandTargetKind> Targets(params ControlRoomCommandTargetKind[] values)
        => Array.AsReadOnly(values);

    private static IReadOnlyList<OperatorComputerCommandExpectedInfluence> Influences(params OperatorComputerCommandExpectedInfluence[] values)
        => Array.AsReadOnly(values);

    private static IReadOnlyList<OperatorComputerCommandConsequenceReference> Permissives(params OperatorComputerCommandConsequenceReference[] values)
        => Array.AsReadOnly(values);

    private static IReadOnlyList<OperatorComputerCommandMonitorTarget> Monitors(params OperatorComputerCommandMonitorTarget[] values)
        => Array.AsReadOnly(values);

    private static OperatorComputerCommandConsequenceReference Mimic(string id, string label)
        => new(OperatorComputerCommandConsequenceReferenceKind.PlantMimicElement, id, label);

    private static OperatorComputerCommandConsequenceReference Published(string id, string label)
        => new(OperatorComputerCommandConsequenceReferenceKind.PublishedState, id, label);

    private static OperatorComputerCommandExpectedInfluence IncreaseDemand(OperatorComputerCommandConsequenceReference target, string explanation)
        => new(OperatorComputerCommandConsequenceRelation.IncreasesExpectedDemandOn, target, explanation);

    private static OperatorComputerCommandExpectedInfluence DecreaseDemand(OperatorComputerCommandConsequenceReference target, string explanation)
        => new(OperatorComputerCommandConsequenceRelation.DecreasesExpectedDemandOn, target, explanation);

    private static OperatorComputerCommandExpectedInfluence Enables(OperatorComputerCommandConsequenceReference target, string explanation)
        => new(OperatorComputerCommandConsequenceRelation.EnablesPath, target, explanation);

    private static OperatorComputerCommandExpectedInfluence Disables(OperatorComputerCommandConsequenceReference target, string explanation)
        => new(OperatorComputerCommandConsequenceRelation.DisablesPath, target, explanation);

    private static OperatorComputerCommandExpectedInfluence Affects(OperatorComputerCommandConsequenceReference target, string explanation)
        => new(OperatorComputerCommandConsequenceRelation.Affects, target, explanation);

    private static OperatorComputerCommandExpectedInfluence MayAffect(OperatorComputerCommandConsequenceReference target, string explanation)
        => new(OperatorComputerCommandConsequenceRelation.MayAffect, target, explanation);

    private static OperatorComputerCommandExpectedInfluence ProtectionOverride(OperatorComputerCommandConsequenceReference target, string explanation)
        => new(OperatorComputerCommandConsequenceRelation.ProtectionMayOverride, target, explanation);

    private static OperatorComputerCommandMonitorTarget Measured(string id, string label, string reason)
        => new(Published(id, label), OperatorComputerInformationProvenance.Measured, reason);

    private static OperatorComputerCommandMonitorTarget Model(string id, string label, string reason)
        => new(Published(id, label), OperatorComputerInformationProvenance.ModelDiagnostic, reason);

    private static OperatorComputerCommandMonitorTarget State(string id, string label, string reason)
        => new(Published(id, label), OperatorComputerInformationProvenance.CanonicalState, reason);
}
