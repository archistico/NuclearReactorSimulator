using NuclearReactorSimulator.Application.Scenarios;
using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios.Faults.Hydraulics;

namespace NuclearReactorSimulator.Application.Scenarios.Faults.SecondaryTransients;

/// <summary>
/// Deterministic M8.4 transient scenarios. Each scenario perturbs an existing M4/M5 owner and observes the resulting
/// physical response; none scripts rotor speed, breaker state, feedwater inventory or condenser pressure directly.
/// </summary>
public static class SecondaryTransientScenarioPack
{
    private static readonly ControlRoomCommandKind[] SafeActions =
    {
        ControlRoomCommandKind.ReactorScram,
        ControlRoomCommandKind.TurbineTrip,
        ControlRoomCommandKind.GeneratorTrip,
        ControlRoomCommandKind.GeneratorBreakerOpen,
        ControlRoomCommandKind.GeneratorLoadLower,
        ControlRoomCommandKind.ControlRodInsert,
        ControlRoomCommandKind.ControlRodHold,
        ControlRoomCommandKind.MainCirculationPumpStart,
        ControlRoomCommandKind.MainCirculationPumpStop,
        ControlRoomCommandKind.AlarmAcknowledge,
        ControlRoomCommandKind.AlarmAcknowledgeAll,
    };

    public static ScenarioDefinition TurbineTrip { get; } = new(
        "m84-turbine-trip",
        "Turbine Trip Transient",
        "Automatic deterministic turbine-trip transient using the existing M5.5 turbine-trip latch and M4 steam/rotor ownership.",
        SecondaryTransientInitialConditionFactory.InitialCondition,
        new[]
        {
            new ScenarioObjectiveDefinition("observe-turbine-trip", "Observe turbine trip", "Confirm canonical stop/isolation action and rotor response without scripted speed changes."),
        },
        SafeActions,
        new[]
        {
            new ScenarioFaultDefinition(
                "m84-turbine-trip-event",
                SecondaryTransientFaultTypeIds.TurbineTrip,
                "rotor",
                ScenarioFaultTriggerDefinition.AtLogicalStep(20),
                ScenarioFaultTriggerDefinition.AtLogicalStep(21)),
        });

    public static ScenarioDefinition GeneratorTripLoadRejection { get; } = new(
        "m84-generator-trip-load-rejection",
        "Generator Trip / Load Rejection",
        "Deterministic generator trip causing breaker opening and electrical load rejection through the existing M5.5/M4.5 path.",
        SecondaryTransientInitialConditionFactory.InitialCondition,
        new[]
        {
            new ScenarioObjectiveDefinition("observe-load-rejection", "Observe load rejection", "Confirm generator trip, breaker opening and removal of electromagnetic loading through canonical electrical ownership."),
        },
        SafeActions,
        new[]
        {
            new ScenarioFaultDefinition(
                "m84-generator-trip-event",
                SecondaryTransientFaultTypeIds.GeneratorTrip,
                "generator",
                ScenarioFaultTriggerDefinition.AtLogicalStep(20),
                ScenarioFaultTriggerDefinition.AtLogicalStep(21)),
        });

    public static ScenarioDefinition FeedwaterLossDegradation { get; } = new(
        "m84-feedwater-loss-degradation",
        "Feedwater Loss / Degradation",
        "Deterministic degradation followed by trip of the canonical feedwater pump using the validated M8.2 hydraulic fault seam.",
        SecondaryTransientInitialConditionFactory.InitialCondition,
        new[]
        {
            new ScenarioObjectiveDefinition("observe-feedwater-degradation", "Observe feedwater degradation", "Compare commanded operation with reduced and then lost canonical feedwater-pump capability."),
        },
        SafeActions,
        new[]
        {
            Fault("m84-feedwater-degraded", HydraulicFaultTypeIds.PumpDegradation, "feedwater-pump", 20, 60, ("capacityFraction", "0.35")),
            Fault("m84-feedwater-lost", HydraulicFaultTypeIds.PumpTrip, "feedwater-pump", 80, 120),
        });

    public static ScenarioDefinition CondenserVacuumDegradationLoss { get; } = new(
        "m84-condenser-vacuum-degradation-loss",
        "Condenser Vacuum Degradation / Loss",
        "Deterministic reduction and loss of the existing M4.3 cooling-boundary heat-rejection capacity; condenser pressure/vacuum remain consequences of conserved exhaust inventory.",
        SecondaryTransientInitialConditionFactory.InitialCondition,
        new[]
        {
            new ScenarioObjectiveDefinition("observe-condenser-degradation", "Observe condenser degradation", "Track cooling capacity, condensation and vacuum response without directly setting pressure."),
        },
        SafeActions,
        new[]
        {
            Fault("m84-condenser-degraded", SecondaryTransientFaultTypeIds.CondenserCoolingDegradation, "cooling", 20, 60, ("capacityFraction", "0.25")),
            Fault("m84-condenser-cooling-loss", SecondaryTransientFaultTypeIds.CondenserCoolingLoss, "cooling", 80, 120),
        });

    public static IReadOnlyList<ScenarioDefinition> All { get; } = new[]
    {
        TurbineTrip,
        GeneratorTripLoadRejection,
        FeedwaterLossDegradation,
        CondenserVacuumDegradationLoss,
    };

    private static ScenarioFaultDefinition Fault(
        string faultId,
        string faultTypeId,
        string targetId,
        long activationStep,
        long? deactivationStep = null,
        params (string Key, string Value)[] parameters)
        => new(
            faultId,
            faultTypeId,
            targetId,
            ScenarioFaultTriggerDefinition.AtLogicalStep(activationStep),
            deactivationStep.HasValue ? ScenarioFaultTriggerDefinition.AtLogicalStep(deactivationStep.Value) : null,
            parameters.ToDictionary(static pair => pair.Key, static pair => pair.Value, StringComparer.Ordinal));
}
