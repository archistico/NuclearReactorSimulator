using NuclearReactorSimulator.Application.Scenarios;
using NuclearReactorSimulator.Application.Scenarios.Faults;
using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios.Faults.Hydraulics;
using NuclearReactorSimulator.Application.Scenarios.Faults.InstrumentationControl;
using NuclearReactorSimulator.Application.Scenarios.Faults.SecondaryTransients;
using NuclearReactorSimulator.Application.Scenarios.Operations;

namespace NuclearReactorSimulator.Application.Scenarios.Faults.ElectricalLoss;

/// <summary>
/// M8.6 educational electrical-loss scenarios. The reference plant does not yet model AC/DC buses, diesel generators or
/// emergency switchgear; therefore station-blackout-class consequences are composed explicitly from the modeled external-grid,
/// pump and actuator-command seams rather than inferred from an invented electrical distribution network.
/// </summary>
public static class ElectricalLossScenarioPack
{
    private static readonly ControlRoomCommandKind[] SafeActions =
    {
        ControlRoomCommandKind.ReactorScram,
        ControlRoomCommandKind.TurbineTrip,
        ControlRoomCommandKind.GeneratorTrip,
        ControlRoomCommandKind.GeneratorBreakerOpen,
        ControlRoomCommandKind.GeneratorBreakerClose,
        ControlRoomCommandKind.GeneratorLoadLower,
        ControlRoomCommandKind.TurbineSpeedRaise,
        ControlRoomCommandKind.TurbineSpeedLower,
        ControlRoomCommandKind.ControlRodInsert,
        ControlRoomCommandKind.ControlRodHold,
        ControlRoomCommandKind.MainCirculationPumpStop,
        ControlRoomCommandKind.AlarmAcknowledge,
        ControlRoomCommandKind.AlarmAcknowledgeAll,
    };

    public static ScenarioDefinition ExternalSupplyLoss { get; } = new(
        "m86-external-supply-loss",
        "Loss of External Electrical Supply",
        "Educational loss of the modeled external-grid connection: canonical generator breakers are forced open while the supply fault is active, with no synthetic bus-voltage model.",
        PowerManoeuvringNormalShutdownProgram.InitialCondition,
        new[]
        {
            new ScenarioObjectiveDefinition(
                "recognize-grid-loss",
                "Recognize loss of external supply",
                "Observe canonical grid disconnection, zero generator export after breaker opening, and the need for deliberate recovery/re-synchronization after supply restoration."),
        },
        SafeActions,
        new[]
        {
            Fault("m86-external-grid-loss", ElectricalLossFaultTypeIds.ExternalSupplyLoss, "grid", 20, 180),
        });

    public static ScenarioDefinition StationBlackoutClass { get; } = new(
        "m86-station-blackout-class",
        "Station Blackout-Class Electrical Loss",
        "Educational station-blackout-class challenge composed from explicit loss of external supply plus modeled pump/drive unavailability. It does not invent unmodeled buses, diesels, batteries or ECCS.",
        PowerManoeuvringNormalShutdownProgram.InitialCondition,
        new[]
        {
            new ScenarioObjectiveDefinition(
                "recognize-total-ac-loss",
                "Recognize station-blackout-class conditions",
                "Identify external-grid isolation together with loss of the modeled electrically driven circulation/feedwater/condensate functions."),
            new ScenarioObjectiveDefinition(
                "protect-and-remove-heat",
                "Protect the reactor and preserve heat-removal options",
                "Use the protection and plant systems actually modeled; do not infer unavailable emergency-power or ECCS capability."),
        },
        SafeActions,
        new[]
        {
            Fault("m86-sbo-grid-loss", ElectricalLossFaultTypeIds.ExternalSupplyLoss, "grid", 20),
            Fault("m86-sbo-main-circulation-trip", HydraulicFaultTypeIds.PumpTrip, "pump", 20),
            Fault("m86-sbo-feedwater-trip", HydraulicFaultTypeIds.PumpTrip, "feedwater-pump", 20),
            Fault("m86-sbo-condensate-trip", HydraulicFaultTypeIds.PumpTrip, "condensate-pump", 20),
            Fault("m86-sbo-mcp-command-loss", InstrumentationControlFaultTypeIds.ActuatorCommandFailLow, "mcp-actuator", 20),
            Fault("m86-sbo-feedwater-command-loss", InstrumentationControlFaultTypeIds.ActuatorCommandFailLow, "feedwater-actuator", 20),
            Fault("m86-sbo-condensate-command-loss", InstrumentationControlFaultTypeIds.ActuatorCommandFailLow, "condensate-actuator", 20),
            Fault("m86-sbo-turbine-trip", SecondaryTransientFaultTypeIds.TurbineTrip, "rotor", 20),
            Fault("m86-sbo-generator-trip", SecondaryTransientFaultTypeIds.GeneratorTrip, "generator", 20),
        });

    public static IReadOnlyList<ScenarioDefinition> All { get; } = new[]
    {
        ExternalSupplyLoss,
        StationBlackoutClass,
    };

    private static ScenarioFaultDefinition Fault(
        string faultId,
        string faultTypeId,
        string targetId,
        long activationStep,
        long? deactivationStep = null)
        => new(
            faultId,
            faultTypeId,
            targetId,
            ScenarioFaultTriggerDefinition.AtLogicalStep(activationStep),
            deactivationStep.HasValue ? ScenarioFaultTriggerDefinition.AtLogicalStep(deactivationStep.Value) : null,
            new Dictionary<string, string>(StringComparer.Ordinal));
}
