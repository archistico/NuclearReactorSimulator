using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios;
using NuclearReactorSimulator.Application.Scenarios.Faults;
using NuclearReactorSimulator.Application.Scenarios.Operations;

namespace NuclearReactorSimulator.Application.Scenarios.Faults.LossOfCoolant;

/// <summary>
/// Educational M8.5 leak/LOCA-class scenarios. Break flow is bounded, pressure-driven and conservative, but deliberately
/// does not claim critical-flow, flashing-jet, containment, ECCS or licensing-grade accident-analysis fidelity.
/// </summary>
public static class LossOfCoolantScenarioPack
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

    public static ScenarioDefinition SmallPrimaryLeak { get; } = new(
        "m85-small-primary-leak",
        "Small Primary-Coolant Leak",
        "Bounded pressure-driven leak from the canonical primary pressure node to atmospheric reference pressure.",
        PowerManoeuvringNormalShutdownProgram.InitialCondition,
        new[]
        {
            new ScenarioObjectiveDefinition(
                "observe-small-leak",
                "Observe small leak response",
                "Track inventory, pressure, protection and operator response while the break flow follows committed pressure."),
        },
        SafeActions,
        new[]
        {
            Break("m85-small-primary-leak-event", "pressure", 20, 420, referenceMassFlowKgPerSecond: 0.5d),
        });

    public static ScenarioDefinition LargeBreakClass { get; } = new(
        "m85-large-break-class",
        "Large Break-Class Loss of Coolant",
        "Educational bounded large-break-class inventory-loss transient from the canonical primary pressure node.",
        PowerManoeuvringNormalShutdownProgram.InitialCondition,
        new[]
        {
            new ScenarioObjectiveDefinition(
                "observe-large-break-class",
                "Observe rapid inventory-loss challenge",
                "Compare faster pressure-dependent inventory loss and protection response without treating the model as a licensing-grade LOCA calculation."),
        },
        SafeActions,
        new[]
        {
            Break(
                "m85-large-break-event",
                "pressure",
                20,
                220,
                referenceMassFlowKgPerSecond: 5d,
                maximumInventoryFractionPerStep: 0.001d),
        });

    public static ScenarioDefinition SteamSpaceLeak { get; } = new(
        "m85-steam-space-leak",
        "Steam-Space Leak / Depressurization",
        "Bounded pressure-driven leak from the canonical steam-space node, emphasizing inventory and depressurization consequences.",
        PowerManoeuvringNormalShutdownProgram.InitialCondition,
        new[]
        {
            new ScenarioObjectiveDefinition(
                "observe-steam-leak",
                "Observe steam-space depressurization",
                "Track steam inventory and pressure response while mass and carried internal energy leave through the canonical source-term boundary."),
        },
        SafeActions,
        new[]
        {
            Break(
                "m85-steam-space-leak-event",
                "steam",
                20,
                320,
                referenceMassFlowKgPerSecond: 0.2d,
                referencePressureDifferenceMegapascals: 0.2d,
                maximumInventoryFractionPerStep: 0.00025d),
        });

    public static IReadOnlyList<ScenarioDefinition> All { get; } = new[]
    {
        SmallPrimaryLeak,
        LargeBreakClass,
        SteamSpaceLeak,
    };

    private static ScenarioFaultDefinition Break(
        string faultId,
        string targetId,
        long activationStep,
        long? deactivationStep,
        double referenceMassFlowKgPerSecond,
        double referencePressureDifferenceMegapascals = 1d,
        double maximumInventoryFractionPerStep = 0.0005d)
        => new(
            faultId,
            LossOfCoolantFaultTypeIds.PressureDrivenBreak,
            targetId,
            ScenarioFaultTriggerDefinition.AtLogicalStep(activationStep),
            deactivationStep.HasValue ? ScenarioFaultTriggerDefinition.AtLogicalStep(deactivationStep.Value) : null,
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["referenceMassFlowKgPerSecond"] = referenceMassFlowKgPerSecond.ToString("R", System.Globalization.CultureInfo.InvariantCulture),
                ["referencePressureDifferenceMegapascals"] = referencePressureDifferenceMegapascals.ToString("R", System.Globalization.CultureInfo.InvariantCulture),
                ["ambientPressureKilopascals"] = "101.325",
                ["maximumInventoryFractionPerStep"] = maximumInventoryFractionPerStep.ToString("R", System.Globalization.CultureInfo.InvariantCulture),
            });
}
