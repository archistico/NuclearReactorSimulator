using NuclearReactorSimulator.Application.ControlRoom;

namespace NuclearReactorSimulator.Application.ControlRoom.Hmi;

/// <summary>
/// M10.9.4 presentation-only subsystem schematic composition. It projects already-published control-room contracts into
/// normalized engineering-diagram topology. No simulation topology, control law, protection rule or hidden true state is owned here.
/// </summary>
public static class ControlRoomSubsystemSchematicProjector
{
    public static ControlRoomSubsystemSchematicsSnapshot Project(ControlRoomSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        return new ControlRoomSubsystemSchematicsSnapshot(
            ProjectReactorCore(snapshot),
            ProjectPrimary(snapshot),
            ProjectTurbineSecondary(snapshot),
            ProjectGeneratorGrid(snapshot),
            ProjectInstrumentationProtection(snapshot));
    }

    private static ControlRoomSubsystemSchematicSnapshot ProjectReactorCore(ControlRoomSnapshot snapshot)
    {
        var reactor = snapshot.ReactorCore;
        var shellOnly = snapshot.RunState == ControlRoomRunState.ShellOnly;
        var feedback = reactor.Zones.Count == 0
            ? "TEMP / VOID —"
            : FormattableString.Invariant($"TEMP {reactor.Zones.Average(static z => z.CoolantTemperatureCelsius):0.0} °C · VOID {AverageNullable(reactor.Zones.Select(static z => z.VoidPercent)):0.###}%");

        var nodes = new[]
        {
            Node("rods", "CONTROL RODS", ControlRoomSubsystemSchematicNodeKind.ControlRods, .04, .08, .15, .18,
                reactor.RodWithdrawalInterlockState,
                shellOnly ? "UNAVAILABLE" : reactor.RodWithdrawalInterlockText,
                Display("WITHDRAWAL", reactor.AverageRodWithdrawal),
                $"{reactor.RodCount} ROD(S)",
                "IN · OPERATOR / CONTROL ACTUATION", "OUT · ROD REACTIVITY"),
            Node("reactivity", "TOTAL REACTIVITY", ControlRoomSubsystemSchematicNodeKind.Reactivity, .29, .08, .16, .18,
                reactor.TotalReactivity.State,
                shellOnly ? "UNAVAILABLE" : "COMBINED REACTIVITY STATE",
                Display("TOTAL", reactor.TotalReactivity),
                $"ROD {DisplayValue(reactor.RodReactivity)} · OTHER {DisplayValue(reactor.NonRodReactivity)}",
                "IN · RODS + FEEDBACKS", "OUT · NEUTRONIC DRIVE"),
            Node("neutronics", "NEUTRON / POWER RESPONSE", ControlRoomSubsystemSchematicNodeKind.Neutronics, .54, .08, .18, .18,
                reactor.ReactorThermalPower.State,
                shellOnly ? "UNAVAILABLE" : "KINETICS / PERIOD RESPONSE",
                Display("POWER", reactor.ReactorThermalPower),
                Display("PERIOD", reactor.ReactorPeriod),
                "IN · TOTAL REACTIVITY", "OUT · FISSION HEAT"),
            Node("protection", "REACTOR PROTECTION", ControlRoomSubsystemSchematicNodeKind.Protection, .79, .08, .17, .18,
                reactor.ProtectionState,
                reactor.ProtectionText,
                reactor.ReactorScramActive ? "SCRAM ACTIVE" : "READY",
                reactor.RodWithdrawalInhibited ? "WITHDRAWAL BLOCKED" : "NO ROD BLOCK",
                "IN · MEASURED / PROTECTION CONDITIONS", "OUT · SCRAM / INTERLOCK OVERRIDE"),
            Node("coolant-in", "PRIMARY COOLANT IN", ControlRoomSubsystemSchematicNodeKind.Boundary, .03, .57, .13, .15,
                Worst(snapshot.PrimaryCircuit.Loops.Select(static l => l.TotalPumpFlow)),
                shellOnly ? "UNAVAILABLE" : "PRESSURIZED INLET",
                DisplayValue(Sum(snapshot.PrimaryCircuit.Loops.Select(static l => l.TotalPumpFlow), "kg/s")),
                snapshot.PrimaryCircuit.Loops.Count == 0 ? "P —" : Display("P", Average(snapshot.PrimaryCircuit.Loops.Select(static l => l.PressureHeaderPressure), "MPa")),
                "IN · MCP DISCHARGE", "OUT · CORE CHANNELS"),
            Node("core", "REACTOR CORE / CHANNELS", ControlRoomSubsystemSchematicNodeKind.ReactorCore, .23, .43, .22, .29,
                reactor.ReactorScramActive ? ControlRoomVisualState.Trip : Worst(reactor.ReactorThermalPower, reactor.AverageRodWithdrawal),
                shellOnly ? "UNAVAILABLE" : reactor.ReactorScramActive ? "SCRAM ACTIVE" : "HEAT GENERATION / TRANSFER",
                Display("THERMAL", reactor.ReactorThermalPower),
                $"{reactor.ZoneCount} ZONE(S) · {reactor.RodCount} ROD(S)",
                "IN · PRESSURIZED COOLANT + FISSION HEAT", "OUT · HEATED CHANNEL RETURN"),
            Node("thermal", "FUEL / COOLANT THERMAL STATE", ControlRoomSubsystemSchematicNodeKind.Thermal, .53, .43, .19, .29,
                reactor.ReactorThermalPower.State,
                shellOnly ? "UNAVAILABLE" : "THERMAL FEEDBACK SOURCE",
                feedback,
                reactor.XenonReactivity.State == ControlRoomVisualState.Unavailable ? "XENON · UNAVAILABLE" : Display("XENON", reactor.XenonReactivity),
                "IN · FISSION HEAT + COOLANT", "OUT · TEMPERATURE / VOID FEEDBACK"),
            Node("return-out", "CHANNEL RETURN", ControlRoomSubsystemSchematicNodeKind.Boundary, .81, .51, .15, .17,
                Worst(snapshot.PrimaryCircuit.TotalSteamExportFlow, snapshot.PrimaryCircuit.TotalFeedwaterFlow),
                shellOnly ? "UNAVAILABLE" : "TO STEAM DRUM SEPARATION",
                snapshot.PrimaryCircuit.SteamDrums.Count == 0 ? "P —" : Range("P", snapshot.PrimaryCircuit.SteamDrums.Select(static d => d.Pressure)),
                snapshot.PrimaryCircuit.SteamDrums.Count == 0 ? "T —" : Range("T", snapshot.PrimaryCircuit.SteamDrums.Select(static d => d.Temperature)),
                "IN · HEATED CORE RETURN", "OUT · DRUM INLET"),
            Node("feedback", "REACTIVITY FEEDBACKS", ControlRoomSubsystemSchematicNodeKind.Feedback, .55, .80, .21, .15,
                Worst(reactor.NonRodReactivity, reactor.XenonReactivity),
                shellOnly ? "UNAVAILABLE" : "TEMPERATURE / VOID / XENON PATH",
                Display("NON-ROD", reactor.NonRodReactivity),
                reactor.XenonReactivity.State == ControlRoomVisualState.Unavailable ? "XENON —" : Display("XENON", reactor.XenonReactivity),
                "IN · THERMAL / VOID / POISON STATE", "OUT · REACTIVITY CONTRIBUTION"),
        };

        var connections = new[]
        {
            Link("rods-reactivity", "rods", "reactivity", ControlRoomSubsystemSchematicConnectionKind.ControlSignal, "ROD WORTH", DisplayValue(reactor.RodReactivity), "CONTROL CONTRIBUTION", reactor.RodReactivity.State, .24, .13, P(.19,.17), P(.29,.17)),
            Link("reactivity-neutronics", "reactivity", "neutronics", ControlRoomSubsystemSchematicConnectionKind.FeedbackSignal, "REACTIVITY → KINETICS", DisplayValue(reactor.TotalReactivity), Display("PERIOD", reactor.ReactorPeriod), reactor.TotalReactivity.State, .495, .13, P(.45,.17), P(.54,.17)),
            Link("neutronics-core", "neutronics", "core", ControlRoomSubsystemSchematicConnectionKind.ThermalInfluence, "FISSION HEAT", DisplayValue(reactor.ReactorThermalPower), "POWER DEPOSITION", reactor.ReactorThermalPower.State, .50, .34, P(.63,.26), P(.63,.34), P(.40,.34), P(.40,.43)),
            Link("coolant-core", "coolant-in", "core", ControlRoomSubsystemSchematicConnectionKind.PrimaryCoolant, "PRIMARY COOLANT", DisplayValue(Sum(snapshot.PrimaryCircuit.Loops.Select(static l => l.TotalPumpFlow), "kg/s")), "FLOW →", Worst(snapshot.PrimaryCircuit.Loops.Select(static l => l.TotalPumpFlow)), .19, .63, P(.16,.64), P(.23,.64)),
            Link("core-thermal", "core", "thermal", ControlRoomSubsystemSchematicConnectionKind.PrimaryCoolant, "HEAT TRANSFER", DisplayValue(reactor.ReactorThermalPower), "FUEL → COOLANT", reactor.ReactorThermalPower.State, .49, .61, P(.45,.60), P(.53,.60)),
            Link("thermal-return", "thermal", "return-out", ControlRoomSubsystemSchematicConnectionKind.PrimaryCoolant, "HEATED RETURN", snapshot.PrimaryCircuit.SteamDrums.Count == 0 ? "FLOW —" : DisplayValue(Sum(snapshot.PrimaryCircuit.SteamDrums.Select(static d => d.IncomingReturnFlow), "kg/s")), "TO DRUMS →", Worst(snapshot.PrimaryCircuit.SteamDrums.Select(static d => d.IncomingReturnFlow)), .77, .58, P(.72,.59), P(.81,.59)),
            Link("thermal-feedback", "thermal", "feedback", ControlRoomSubsystemSchematicConnectionKind.FeedbackSignal, "THERMAL / VOID FEEDBACK", feedback, "STATE → REACTIVITY", Worst(reactor.NonRodReactivity), .65, .76, P(.63,.72), P(.63,.80)),
            Link("feedback-reactivity", "feedback", "reactivity", ControlRoomSubsystemSchematicConnectionKind.FeedbackSignal, "REACTIVITY FEEDBACK", DisplayValue(reactor.NonRodReactivity), "CLOSES FEEDBACK LOOP", reactor.NonRodReactivity.State, .37, .82, P(.55,.87), P(.34,.87), P(.34,.26)),
            Link("protection-rods", "protection", "rods", ControlRoomSubsystemSchematicConnectionKind.ProtectionOverride, "PROTECTION OVERRIDE", reactor.ReactorScramActive ? "SCRAM ACTIVE" : "PRIORITY PATH ARMED", "OVERRIDES NORMAL ROD CONTROL", reactor.ProtectionState, .50, .03, P(.79,.10), P(.70,.03), P(.12,.03), P(.12,.08)),
        };

        return new ControlRoomSubsystemSchematicSnapshot(
            ControlRoomSubsystemSchematicKind.ReactorCore,
            "REACTOR / CORE ENGINEERING SCHEMATIC",
            "Reactivity → kinetics → heat → coolant/feedback chain with explicit protection override",
            nodes,
            connections,
            "Read left-to-right for the direct power chain; follow the lower feedback path back to TOTAL REACTIVITY. Protection is a separate higher-priority override path, never a normal-control continuation.");
    }

    private static ControlRoomSubsystemSchematicSnapshot ProjectPrimary(ControlRoomSnapshot snapshot)
    {
        var primary = snapshot.PrimaryCircuit;
        var shellOnly = snapshot.RunState == ControlRoomRunState.ShellOnly;
        var drum = primary.SteamDrums.FirstOrDefault();
        var pump = primary.Pumps.FirstOrDefault(static p => p.IsOperatorCommandable) ?? primary.Pumps.FirstOrDefault();
        var loop = primary.Loops.FirstOrDefault();

        var nodes = new[]
        {
            Node("drum", "STEAM DRUM", ControlRoomSubsystemSchematicNodeKind.SteamDrum, .36, .08, .24, .22,
                drum is null ? ControlRoomVisualState.Unavailable : Worst(drum.Pressure, drum.Level),
                shellOnly || drum is null ? "UNAVAILABLE" : $"{drum.DrumId} · {drum.Phase}",
                drum is null ? "P — · LEVEL —" : $"P {DisplayValue(drum.Pressure)} · LEVEL {DisplayValue(drum.Level)}",
                drum is null ? "T —" : Display("T", drum.Temperature),
                "IN · CORE RETURN + FEEDWATER", "OUT · STEAM + LIQUID RECIRCULATION"),
            Node("steam-out", "MAIN STEAM OUT", ControlRoomSubsystemSchematicNodeKind.Boundary, .75, .08, .18, .16,
                primary.TotalSteamExportFlow.State,
                shellOnly ? "UNAVAILABLE" : "TO TURBINE ISLAND",
                Display("FLOW", primary.TotalSteamExportFlow),
                drum is null ? "P / T —" : $"{DisplayValue(drum.Pressure)} · {DisplayValue(drum.Temperature)}",
                "IN · SEPARATED STEAM", "OUT · MAIN STEAM SYSTEM"),
            Node("suction", "SUCTION HEADER", ControlRoomSubsystemSchematicNodeKind.Header, .07, .43, .17, .18,
                loop?.SuctionHeaderPressure.State ?? ControlRoomVisualState.Unavailable,
                shellOnly ? "UNAVAILABLE" : "LOW-PRESSURE PRIMARY HEADER",
                loop is null ? "P —" : Display("P", loop.SuctionHeaderPressure),
                primary.TotalPrimaryMass.State == ControlRoomVisualState.Unavailable ? "MASS —" : Display("PRIMARY MASS", primary.TotalPrimaryMass),
                "IN · DRUM LIQUID RETURN", "OUT · MCP SUCTION"),
            Node("mcp", "MAIN CIRCULATION PUMP", ControlRoomSubsystemSchematicNodeKind.Pump, .30, .43, .18, .18,
                pump is null ? ControlRoomVisualState.Unavailable : pump.MassFlow.State,
                shellOnly || pump is null ? "UNAVAILABLE" : $"{pump.PumpId} · {pump.OperatingText}",
                pump is null ? "FLOW —" : Display("FLOW", pump.MassFlow),
                pump is null ? "BOOST —" : Display("BOOST", pump.PressureBoost),
                "IN · SUCTION HEADER", "OUT · PRESSURE HEADER"),
            Node("pressure", "PRESSURE HEADER", ControlRoomSubsystemSchematicNodeKind.Header, .54, .43, .17, .18,
                loop?.PressureHeaderPressure.State ?? ControlRoomVisualState.Unavailable,
                shellOnly ? "UNAVAILABLE" : "MCP DISCHARGE HEADER",
                loop is null ? "P —" : Display("P", loop.PressureHeaderPressure),
                loop is null ? "ΔP —" : Display("ΔP", loop.HeaderPressureRise),
                "IN · MCP DISCHARGE", "OUT · CORE CHANNELS"),
            Node("channels", "CORE CHANNEL GROUPS", ControlRoomSubsystemSchematicNodeKind.ReactorCore, .77, .40, .18, .24,
                loop?.TotalPumpFlow.State ?? ControlRoomVisualState.Unavailable,
                shellOnly ? "UNAVAILABLE" : "HEATED PRIMARY FLOW PATH",
                loop is null ? "FLOW —" : Display("FLOW", loop.TotalPumpFlow),
                $"{primary.Loops.Count} LOOP(S)",
                "IN · PRESSURIZED COOLANT", "OUT · HEATED RETURN"),
            Node("return", "RETURN COLLECTOR", ControlRoomSubsystemSchematicNodeKind.Header, .63, .73, .19, .16,
                drum?.IncomingReturnFlow.State ?? ControlRoomVisualState.Unavailable,
                shellOnly ? "UNAVAILABLE" : "CORE RETURN TO DRUM",
                drum is null ? "FLOW —" : Display("FLOW", drum.IncomingReturnFlow),
                drum is null ? "PHASE —" : drum.PhaseText,
                "IN · CHANNEL RETURN", "OUT · STEAM DRUM"),
            Node("feedwater", "FEEDWATER IN", ControlRoomSubsystemSchematicNodeKind.Boundary, .12, .76, .18, .15,
                primary.TotalFeedwaterFlow.State,
                shellOnly ? "UNAVAILABLE" : "SECONDARY → DRUM MAKEUP",
                Display("FLOW", primary.TotalFeedwaterFlow),
                "BOUNDARY FLOW",
                "IN · FEEDWATER SYSTEM", "OUT · STEAM DRUM"),
        };

        var connections = new[]
        {
            Link("drum-steam", "drum", "steam-out", ControlRoomSubsystemSchematicConnectionKind.Steam, "SEPARATED STEAM", DisplayValue(primary.TotalSteamExportFlow), drum is null ? "P / T —" : $"{DisplayValue(drum.Pressure)} · {DisplayValue(drum.Temperature)}", primary.TotalSteamExportFlow.State, .68, .14, P(.60,.16), P(.75,.16)),
            Link("drum-suction", "drum", "suction", ControlRoomSubsystemSchematicConnectionKind.PrimaryCoolant, "LIQUID RECIRCULATION", drum is null ? "FLOW —" : DisplayValue(drum.RecirculationFlow), "DOWNCOMER / SUCTION", drum?.RecirculationFlow.State ?? ControlRoomVisualState.Unavailable, .20, .29, P(.40,.30), P(.40,.34), P(.15,.34), P(.15,.43)),
            Link("suction-mcp", "suction", "mcp", ControlRoomSubsystemSchematicConnectionKind.PrimaryCoolant, "MCP SUCTION", loop is null ? "FLOW —" : DisplayValue(loop.TotalPumpFlow), loop is null ? "P —" : DisplayValue(loop.SuctionHeaderPressure), loop?.TotalPumpFlow.State ?? ControlRoomVisualState.Unavailable, .27, .51, P(.24,.52), P(.30,.52)),
            Link("mcp-pressure", "mcp", "pressure", ControlRoomSubsystemSchematicConnectionKind.PrimaryCoolant, "PRESSURIZED COOLANT", pump is null ? "FLOW —" : DisplayValue(pump.MassFlow), pump is null ? "BOOST —" : DisplayValue(pump.PressureBoost), pump?.MassFlow.State ?? ControlRoomVisualState.Unavailable, .51, .50, P(.48,.52), P(.54,.52)),
            Link("pressure-channels", "pressure", "channels", ControlRoomSubsystemSchematicConnectionKind.PrimaryCoolant, "CORE INLET", loop is null ? "FLOW —" : DisplayValue(loop.TotalPumpFlow), loop is null ? "P —" : DisplayValue(loop.PressureHeaderPressure), loop?.TotalPumpFlow.State ?? ControlRoomVisualState.Unavailable, .74, .49, P(.71,.52), P(.77,.52)),
            Link("channels-return", "channels", "return", ControlRoomSubsystemSchematicConnectionKind.PrimaryCoolant, "HEATED RETURN", drum is null ? "FLOW —" : DisplayValue(drum.IncomingReturnFlow), "TO DRUM SEPARATION", drum?.IncomingReturnFlow.State ?? ControlRoomVisualState.Unavailable, .82, .69, P(.86,.64), P(.86,.70), P(.72,.70), P(.72,.73)),
            Link("return-drum", "return", "drum", ControlRoomSubsystemSchematicConnectionKind.PrimaryCoolant, "DRUM INLET", drum is null ? "FLOW —" : DisplayValue(drum.IncomingReturnFlow), drum is null ? "T —" : DisplayValue(drum.Temperature), drum?.IncomingReturnFlow.State ?? ControlRoomVisualState.Unavailable, .55, .63, P(.63,.80), P(.51,.80), P(.51,.30)),
            Link("feedwater-drum", "feedwater", "drum", ControlRoomSubsystemSchematicConnectionKind.Feedwater, "FEEDWATER", DisplayValue(primary.TotalFeedwaterFlow), "LEVEL / INVENTORY MAKEUP", primary.TotalFeedwaterFlow.State, .34, .78, P(.30,.83), P(.43,.83), P(.43,.30)),
        };

        return new ControlRoomSubsystemSchematicSnapshot(
            ControlRoomSubsystemSchematicKind.PrimarySteamDrum,
            "PRIMARY CIRCUIT / STEAM-DRUM ENGINEERING SCHEMATIC",
            "Closed primary recirculation loop with explicit steam-export and feedwater boundaries",
            nodes,
            connections,
            "Follow the closed cyan loop DRUM → SUCTION → MCP → PRESSURE HEADER → CORE CHANNELS → RETURN → DRUM. Steam export and feedwater are separate boundary paths; every arrow has an explicit source and destination.");
    }

    private static ControlRoomSubsystemSchematicSnapshot ProjectTurbineSecondary(ControlRoomSnapshot snapshot)
    {
        var turbine = snapshot.TurbineSecondary;
        var shellOnly = snapshot.RunState == ControlRoomRunState.ShellOnly;
        var line = turbine.SteamLines.FirstOrDefault();
        var train = turbine.AdmissionTrains.FirstOrDefault();
        var stage = turbine.StageGroups.FirstOrDefault();
        var rotor = turbine.Rotors.FirstOrDefault();
        var condenser = turbine.Condensers.FirstOrDefault();
        var feedwater = turbine.FeedwaterTrains.FirstOrDefault();

        var nodes = new[]
        {
            Node("steam-in", "MAIN STEAM HEADER", ControlRoomSubsystemSchematicNodeKind.Header, .02, .12, .14, .17,
                line?.MassFlow.State ?? ControlRoomVisualState.Unavailable,
                shellOnly ? "UNAVAILABLE" : "STEAM SOURCE",
                line is null ? "FLOW —" : Display("FLOW", line.MassFlow),
                line is null ? "ΔP —" : Display("ΔP", line.PressureDifference),
                "IN · STEAM DRUM EXPORT", "OUT · STOP VALVE"),
            Node("stop", "STOP VALVE", ControlRoomSubsystemSchematicNodeKind.Valve, .20, .12, .12, .17,
                train?.StopValvePosition.State ?? ControlRoomVisualState.Unavailable,
                shellOnly || train is null ? "UNAVAILABLE" : train.StopValveText,
                train is null ? "POSITION —" : Display("OPEN", train.StopValvePosition),
                "ISOLATION",
                "IN · MAIN STEAM", "OUT · CONTROL VALVE"),
            Node("control", "CONTROL VALVE", ControlRoomSubsystemSchematicNodeKind.Valve, .36, .12, .12, .17,
                train?.ControlValvePosition.State ?? ControlRoomVisualState.Unavailable,
                shellOnly || train is null ? "UNAVAILABLE" : train.ControlValveText,
                train is null ? "POSITION —" : Display("OPEN", train.ControlValvePosition),
                "SPEED / LOAD GOVERNING PATH",
                "IN · STOP-VALVE OUTLET", "OUT · ADMISSION VALVE"),
            Node("admission", "ADMISSION VALVE", ControlRoomSubsystemSchematicNodeKind.Valve, .52, .12, .12, .17,
                train?.AdmissionValvePosition.State ?? ControlRoomVisualState.Unavailable,
                shellOnly || train is null ? "UNAVAILABLE" : train.AdmissionValveText,
                train is null ? "POSITION —" : Display("OPEN", train.AdmissionValvePosition),
                train is null ? "FLOW —" : Display("FLOW", train.AdmissionFlow),
                "IN · CONTROLLED STEAM", "OUT · TURBINE INLET"),
            Node("stage", "TURBINE STAGE GROUP", ControlRoomSubsystemSchematicNodeKind.TurbineStage, .68, .08, .16, .25,
                stage is null ? ControlRoomVisualState.Unavailable : stage.ShaftPower.State,
                shellOnly || stage is null ? "UNAVAILABLE" : stage.TripBlocked ? "TRIP-BLOCKED FLOW" : "STEAM EXPANSION ACTIVE",
                stage is null ? "STEAM —" : Display("STEAM", stage.SteamFlow),
                stage is null ? "SHAFT —" : Display("SHAFT", stage.ShaftPower),
                "IN · ADMITTED STEAM", "OUT · SHAFT WORK + EXHAUST"),
            Node("rotor", "TURBINE ROTOR / SHAFT", ControlRoomSubsystemSchematicNodeKind.Rotor, .86, .08, .12, .25,
                rotor?.State ?? ControlRoomVisualState.Unavailable,
                shellOnly || rotor is null ? "UNAVAILABLE" : rotor.ProtectionText,
                rotor is null ? "SPEED —" : Display("SPEED", rotor.Speed),
                rotor is null ? "SHAFT —" : Display("POWER", rotor.ShaftPower),
                "IN · TURBINE TORQUE", "OUT · GENERATOR SHAFT"),
            Node("condenser", "CONDENSER / HOTWELL", ControlRoomSubsystemSchematicNodeKind.Condenser, .64, .54, .19, .24,
                condenser is null ? ControlRoomVisualState.Unavailable : Worst(condenser.Pressure, condenser.Vacuum, condenser.HotwellMass),
                shellOnly || condenser is null ? "UNAVAILABLE" : $"{condenser.CondenserId} · {condenser.SteamSpacePhase}",
                condenser is null ? "VACUUM —" : Display("VAC", condenser.Vacuum),
                condenser is null ? "HOTWELL —" : Display("HOTWELL", condenser.HotwellMass),
                "IN · TURBINE EXHAUST STEAM", "OUT · CONDENSATE"),
            Node("cond-pump", "CONDENSATE PUMP", ControlRoomSubsystemSchematicNodeKind.Pump, .38, .58, .14, .18,
                feedwater?.CondensatePump.MassFlow.State ?? ControlRoomVisualState.Unavailable,
                shellOnly || feedwater is null ? "UNAVAILABLE" : feedwater.CondensatePump.OperatingText,
                feedwater is null ? "FLOW —" : Display("FLOW", feedwater.CondensatePump.MassFlow),
                feedwater is null ? "BOOST —" : Display("BOOST", feedwater.CondensatePump.PressureBoost),
                "IN · HOTWELL", "OUT · FEEDWATER INVENTORY"),
            Node("fw-pump", "FEEDWATER PUMP", ControlRoomSubsystemSchematicNodeKind.Pump, .17, .58, .14, .18,
                feedwater?.FeedwaterPump.MassFlow.State ?? ControlRoomVisualState.Unavailable,
                shellOnly || feedwater is null ? "UNAVAILABLE" : feedwater.FeedwaterPump.OperatingText,
                feedwater is null ? "FLOW —" : Display("FLOW", feedwater.FeedwaterPump.MassFlow),
                feedwater is null ? "T —" : Display("T", feedwater.FeedwaterTemperature),
                "IN · CONDITIONED CONDENSATE", "OUT · STEAM DRUM FEEDWATER"),
            Node("fw-out", "FEEDWATER TO DRUMS", ControlRoomSubsystemSchematicNodeKind.Boundary, .02, .58, .11, .18,
                snapshot.PrimaryCircuit.TotalFeedwaterFlow.State,
                shellOnly ? "UNAVAILABLE" : "RETURN TO PRIMARY BOUNDARY",
                Display("FLOW", snapshot.PrimaryCircuit.TotalFeedwaterFlow),
                feedwater is null ? "T —" : Display("T", feedwater.FeedwaterTemperature),
                "IN · FEEDWATER PUMP", "OUT · STEAM DRUM"),
        };

        var connections = new[]
        {
            Link("steam-stop", "steam-in", "stop", ControlRoomSubsystemSchematicConnectionKind.Steam, "MAIN STEAM", line is null ? "FLOW —" : DisplayValue(line.MassFlow), "→", line?.MassFlow.State ?? ControlRoomVisualState.Unavailable, .18, .18, P(.16,.20), P(.20,.20)),
            Link("stop-control", "stop", "control", ControlRoomSubsystemSchematicConnectionKind.Steam, "STEAM", train is null ? "FLOW —" : DisplayValue(train.AdmissionFlow), "→", train?.AdmissionFlow.State ?? ControlRoomVisualState.Unavailable, .34, .18, P(.32,.20), P(.36,.20)),
            Link("control-admission", "control", "admission", ControlRoomSubsystemSchematicConnectionKind.Steam, "GOVERNED STEAM", train is null ? "FLOW —" : DisplayValue(train.AdmissionFlow), "→", train?.AdmissionFlow.State ?? ControlRoomVisualState.Unavailable, .50, .18, P(.48,.20), P(.52,.20)),
            Link("admission-stage", "admission", "stage", ControlRoomSubsystemSchematicConnectionKind.Steam, "ADMISSION", train is null ? "FLOW —" : DisplayValue(train.AdmissionFlow), train is null ? "P / T —" : $"{DisplayValue(train.TurbineInletPressure)} · {DisplayValue(train.TurbineInletTemperature)}", train?.AdmissionFlow.State ?? ControlRoomVisualState.Unavailable, .66, .17, P(.64,.20), P(.68,.20)),
            Link("stage-rotor", "stage", "rotor", ControlRoomSubsystemSchematicConnectionKind.Mechanical, "SHAFT TORQUE / POWER", stage is null ? "POWER —" : DisplayValue(stage.ShaftPower), rotor is null ? "SPEED —" : DisplayValue(rotor.Speed), stage?.ShaftPower.State ?? ControlRoomVisualState.Unavailable, .85, .16, P(.84,.20), P(.86,.20)),
            Link("stage-condenser", "stage", "condenser", ControlRoomSubsystemSchematicConnectionKind.Steam, "EXHAUST STEAM", stage is null ? "FLOW —" : DisplayValue(stage.SteamFlow), condenser is null ? "P —" : DisplayValue(condenser.Pressure), stage?.SteamFlow.State ?? ControlRoomVisualState.Unavailable, .75, .43, P(.76,.33), P(.76,.54)),
            Link("condenser-cond", "condenser", "cond-pump", ControlRoomSubsystemSchematicConnectionKind.Condensate, "CONDENSATE", condenser is null ? "FLOW —" : DisplayValue(condenser.CondensationFlow), condenser is null ? "HOTWELL —" : DisplayValue(condenser.HotwellMass), condenser?.CondensationFlow.State ?? ControlRoomVisualState.Unavailable, .57, .67, P(.64,.67), P(.52,.67)),
            Link("cond-fw", "cond-pump", "fw-pump", ControlRoomSubsystemSchematicConnectionKind.Condensate, "CONDENSATE / INVENTORY", feedwater is null ? "FLOW —" : DisplayValue(feedwater.CondensatePump.MassFlow), feedwater is null ? "MASS —" : DisplayValue(feedwater.FeedwaterInventoryMass), feedwater?.CondensatePump.MassFlow.State ?? ControlRoomVisualState.Unavailable, .34, .67, P(.38,.67), P(.31,.67)),
            Link("fw-out", "fw-pump", "fw-out", ControlRoomSubsystemSchematicConnectionKind.Feedwater, "FEEDWATER", DisplayValue(snapshot.PrimaryCircuit.TotalFeedwaterFlow), feedwater is null ? "T —" : DisplayValue(feedwater.FeedwaterTemperature), snapshot.PrimaryCircuit.TotalFeedwaterFlow.State, .145, .67, P(.17,.67), P(.13,.67)),
        };

        var context = turbine.TurbineTripActive
            ? "TURBINE TRIP is active: steam admission is protection-subordinate. Diagnose/reset protection before expecting sustained shaft power."
            : "Steam must pass MAIN STEAM → STOP → CONTROL → ADMISSION → STAGE before shaft power can exist. The amber SHAFT path denotes mechanical energy transfer; amber is not a warning state.";

        return new ControlRoomSubsystemSchematicSnapshot(
            ControlRoomSubsystemSchematicKind.TurbineSecondary,
            "TURBINE / SECONDARY ENGINEERING SCHEMATIC",
            "Steam-admission train, expansion/shaft path, condenser and condensate/feedwater return",
            nodes,
            connections,
            context);
    }

    private static ControlRoomSubsystemSchematicSnapshot ProjectGeneratorGrid(ControlRoomSnapshot snapshot)
    {
        var electrical = snapshot.Electrical;
        var turbine = snapshot.TurbineSecondary;
        var generator = electrical.Generators.FirstOrDefault();
        var rotor = turbine.Rotors.FirstOrDefault();
        var shellOnly = snapshot.RunState == ControlRoomRunState.ShellOnly;
        var requested = generator?.RequestedElectricalPower;

        var nodes = new[]
        {
            Node("shaft", "TURBINE SHAFT", ControlRoomSubsystemSchematicNodeKind.Rotor, .03, .30, .16, .24,
                rotor?.State ?? ControlRoomVisualState.Unavailable,
                shellOnly || rotor is null ? "UNAVAILABLE" : rotor.ProtectionText,
                rotor is null ? "SPEED —" : Display("SPEED", rotor.Speed),
                Display("SHAFT", turbine.TotalTurbineShaftPower),
                "IN · TURBINE TORQUE", "OUT · GENERATOR MECHANICAL INPUT"),
            Node("generator", "SYNCHRONOUS GENERATOR", ControlRoomSubsystemSchematicNodeKind.Generator, .28, .27, .22, .30,
                generator is null ? ControlRoomVisualState.Unavailable : generator.ElectricalOutput.State,
                shellOnly || generator is null ? "UNAVAILABLE" : generator.BreakerText,
                generator is null ? "OUTPUT —" : Display("OUTPUT", generator.ElectricalOutput),
                generator is null ? "REQUEST —" : $"REQUEST {DisplayValue(requested)} · INPUT {DisplayValue(generator.MechanicalInputPower)}",
                "IN · SHAFT MECHANICAL POWER", "OUT · 3-PHASE ELECTRICAL POWER"),
            Node("breaker", "GENERATOR BREAKER", ControlRoomSubsystemSchematicNodeKind.Breaker, .59, .30, .15, .24,
                generator is null ? ControlRoomVisualState.Unavailable : generator.BreakerState,
                shellOnly || generator is null ? "UNAVAILABLE" : generator.BreakerText,
                generator is null ? "STATE —" : generator.BreakerText,
                generator is null ? "SYNC —" : generator.SynchronizationLabel,
                "IN · GENERATOR TERMINALS", "OUT · GRID CONNECTION"),
            Node("grid", "EXTERNAL GRID", ControlRoomSubsystemSchematicNodeKind.Grid, .83, .29, .14, .26,
                Worst(electrical.Grid.Frequency, electrical.Grid.LineVoltage),
                shellOnly ? "UNAVAILABLE" : "INFINITE-BUS REFERENCE",
                Display("f", electrical.Grid.Frequency),
                Display("V", electrical.Grid.LineVoltage),
                "IN · GENERATOR EXPORT", "OUT · EXTERNAL ELECTRICAL SYSTEM"),
            Node("sync", "SYNCHRONIZATION CHECK", ControlRoomSubsystemSchematicNodeKind.Instrumentation, .35, .68, .23, .20,
                generator?.DisplaySynchronizationState ?? ControlRoomVisualState.Unavailable,
                shellOnly || generator is null ? "UNAVAILABLE" : generator.DisplaySynchronizationText,
                generator is null ? "Δf / Δφ / ΔV —" : generator.SynchronizationDetailText,
                "CLOSE PERMISSIVE ONLY",
                "IN · GENERATOR + GRID MEASUREMENTS", "OUT · BREAKER CLOSE PERMISSIVE"),
            Node("protection", "GENERATOR / TURBINE PROTECTION", ControlRoomSubsystemSchematicNodeKind.Protection, .66, .68, .27, .20,
                snapshot.AnyTripActive ? ControlRoomVisualState.Trip : ControlRoomVisualState.Normal,
                snapshot.AnyTripActive ? "ONE OR MORE TRIPS ACTIVE" : "PROTECTION CLEAR",
                turbine.ProtectionText,
                electrical.ProtectionText,
                "IN · PROTECTION FUNCTIONS", "OUT · TRIP / BREAKER OVERRIDE"),
        };

        var connections = new[]
        {
            Link("shaft-generator", "shaft", "generator", ControlRoomSubsystemSchematicConnectionKind.Mechanical, "SHAFT", DisplayValue(turbine.TotalTurbineShaftPower), rotor is null ? "SPEED —" : DisplayValue(rotor.Speed), turbine.TotalTurbineShaftPower.State, .235, .38, P(.19,.42), P(.28,.42)),
            Link("generator-breaker", "generator", "breaker", ControlRoomSubsystemSchematicConnectionKind.Electrical, "GENERATOR OUTPUT", DisplayValue(electrical.GrossElectricalOutput), generator is null ? "V —" : DisplayValue(generator.TerminalVoltage), electrical.GrossElectricalOutput.State, .545, .40, P(.50,.42), P(.59,.42)),
            Link("breaker-grid", "breaker", "grid", ControlRoomSubsystemSchematicConnectionKind.Electrical, "GRID EXPORT", DisplayValue(electrical.GrossElectricalOutput), generator is null ? "BREAKER —" : generator.BreakerText, electrical.GrossElectricalOutput.State, .785, .40, P(.74,.42), P(.83,.42)),
            Link("gen-sync", "generator", "sync", ControlRoomSubsystemSchematicConnectionKind.MeasurementSignal, "GENERATOR MEASUREMENTS", generator is null ? "f / V / φ —" : $"{DisplayValue(generator.Frequency)} · {DisplayValue(generator.TerminalVoltage)} · {DisplayValue(generator.PhaseDifference)}", "SIGNAL", generator?.Frequency.State ?? ControlRoomVisualState.Unavailable, .39, .63, P(.40,.57), P(.40,.68)),
            Link("grid-sync", "grid", "sync", ControlRoomSubsystemSchematicConnectionKind.MeasurementSignal, "GRID REFERENCE", $"{DisplayValue(electrical.Grid.Frequency)} · {DisplayValue(electrical.Grid.LineVoltage)}", "SIGNAL", electrical.Grid.Frequency.State, .66, .61, P(.89,.55), P(.89,.62), P(.56,.62), P(.56,.68)),
            Link("sync-breaker", "sync", "breaker", ControlRoomSubsystemSchematicConnectionKind.ControlSignal, "CLOSE PERMISSIVE", generator?.SynchronizationLabel ?? "SYNC —", "DOES NOT CLOSE BY ITSELF", generator?.DisplaySynchronizationState ?? ControlRoomVisualState.Unavailable, .62, .61, P(.58,.75), P(.62,.75), P(.62,.54)),
            Link("protection-breaker", "protection", "breaker", ControlRoomSubsystemSchematicConnectionKind.ProtectionOverride, "PROTECTION OVERRIDE", snapshot.AnyTripActive ? "TRIP ACTIVE" : "PRIORITY PATH ARMED", "OVERRIDES NORMAL CLOSE/LOAD CONTROL", snapshot.AnyTripActive ? ControlRoomVisualState.Trip : ControlRoomVisualState.Normal, .75, .62, P(.75,.68), P(.69,.62), P(.69,.54)),
        };

        return new ControlRoomSubsystemSchematicSnapshot(
            ControlRoomSubsystemSchematicKind.GeneratorGrid,
            "GENERATOR / GRID ENGINEERING SCHEMATIC",
            "Mechanical shaft → generator → breaker → grid, with synchronization and protection shown as separate signal paths",
            nodes,
            connections,
            BuildGeneratorPowerPathDiagnostic(snapshot));
    }

    private static ControlRoomSubsystemSchematicSnapshot ProjectInstrumentationProtection(ControlRoomSnapshot snapshot)
    {
        var shellOnly = snapshot.RunState == ControlRoomRunState.ShellOnly;
        var signalState = shellOnly || snapshot.TotalMeasuredSignalCount == 0
            ? ControlRoomVisualState.Unavailable
            : snapshot.InvalidMeasuredSignalCount > 0 ? ControlRoomVisualState.Warning : ControlRoomVisualState.Normal;
        var protectionState = snapshot.AnyTripActive ? ControlRoomVisualState.Trip : ControlRoomVisualState.Normal;
        var alarmState = snapshot.UnacknowledgedAlarmCount > 0 ? ControlRoomVisualState.Warning : ControlRoomVisualState.Normal;

        var nodes = new[]
        {
            Node("plant", "PHYSICAL PLANT", ControlRoomSubsystemSchematicNodeKind.Boundary, .03, .35, .16, .24,
                shellOnly ? ControlRoomVisualState.Unavailable : ControlRoomVisualState.Normal,
                shellOnly ? "UNAVAILABLE" : "M2 / M3 / M4 CANONICAL OWNERS",
                Display("MWth", snapshot.ReactorCore.ReactorThermalPower),
                Display("MWe", snapshot.Electrical.GrossElectricalOutput),
                "IN · ACTUATOR COMMANDS / BOUNDARIES", "OUT · PHYSICAL STATE"),
            Node("instrumentation", "INSTRUMENTATION", ControlRoomSubsystemSchematicNodeKind.Instrumentation, .27, .16, .18, .22,
                signalState,
                shellOnly ? "UNAVAILABLE" : $"{snapshot.ValidMeasuredSignalCount}/{snapshot.TotalMeasuredSignalCount} VALID",
                $"VALID {snapshot.ValidMeasuredSignalCount}",
                $"INVALID {snapshot.InvalidMeasuredSignalCount}",
                "IN · PHYSICAL SOURCES", "OUT · MEASURED SIGNAL FRAME"),
            Node("controllers", "NORMAL / SUPERVISORY CONTROL", ControlRoomSubsystemSchematicNodeKind.Controller, .52, .16, .20, .22,
                shellOnly ? ControlRoomVisualState.Unavailable : ControlRoomVisualState.Normal,
                shellOnly ? "UNAVAILABLE" : "MEASURED-SIGNAL CONSUMERS",
                "PID / SUPERVISORY INTENTS",
                "PROTECTION-SUBORDINATE",
                "IN · MEASURED SIGNALS + SETPOINTS", "OUT · NORMAL CONTROL REQUESTS"),
            Node("arbitration", "COMMAND ARBITRATION / ACTUATORS", ControlRoomSubsystemSchematicNodeKind.Arbitration, .78, .32, .19, .25,
                protectionState,
                snapshot.AnyTripActive ? "PROTECTION OVERRIDE ACTIVE" : "NORMAL AUTHORITY AVAILABLE",
                "TYPED COMMANDS ONLY",
                "FAIL-CLOSED",
                "IN · NORMAL CONTROL + PROTECTION", "OUT · PLANT ACTUATION"),
            Node("protection", "PROTECTION / INTERLOCKS", ControlRoomSubsystemSchematicNodeKind.Protection, .48, .63, .23, .22,
                protectionState,
                snapshot.AnyTripActive ? "TRIP / SCRAM ACTIVE" : "PROTECTION CLEAR",
                snapshot.AnyTripActive ? "OVERRIDE ACTIVE" : "PRIORITY PATH ARMED",
                snapshot.ProtectionReset.StatusText,
                "IN · MEASURED SIGNALS", "OUT · TRIPS / SCRAM / INTERLOCKS"),
            Node("alarms", "ALARMS / FIRST-OUT", ControlRoomSubsystemSchematicNodeKind.Alarm, .22, .65, .18, .20,
                alarmState,
                shellOnly ? "UNAVAILABLE" : $"{snapshot.AnnunciatedAlarmCount} ANNUNCIATED",
                $"UNACK {snapshot.UnacknowledgedAlarmCount}",
                $"FIRST-OUT {snapshot.AlarmEvents.FirstOutCount}",
                "IN · MEASURED / PROTECTION CONDITIONS", "OUT · OPERATOR ANNUNCIATION"),
        };

        var connections = new[]
        {
            Link("plant-inst", "plant", "instrumentation", ControlRoomSubsystemSchematicConnectionKind.MeasurementSignal, "MEASURED SIGNALS", $"{snapshot.ValidMeasuredSignalCount}/{snapshot.TotalMeasuredSignalCount} VALID", "NO TRUE-STATE FALLBACK", signalState, .23, .33, P(.19,.42), P(.23,.42), P(.23,.27), P(.27,.27)),
            Link("inst-control", "instrumentation", "controllers", ControlRoomSubsystemSchematicConnectionKind.MeasurementSignal, "MEASUREMENTS", "VALIDITY / QUALITY PRESERVED", "→ CONTROL", signalState, .485, .23, P(.45,.27), P(.52,.27)),
            Link("control-arb", "controllers", "arbitration", ControlRoomSubsystemSchematicConnectionKind.ControlSignal, "NORMAL CONTROL REQUESTS", "ACTUATOR DEMANDS", "PROTECTION-SUBORDINATE", ControlRoomVisualState.Normal, .76, .25, P(.72,.27), P(.78,.39)),
            Link("arb-plant", "arbitration", "plant", ControlRoomSubsystemSchematicConnectionKind.ControlSignal, "ACTUATION", "RODS / VALVES / PUMPS / BREAKER", "TYPED CANONICAL INTENTS", protectionState, .51, .51, P(.78,.49), P(.70,.51), P(.19,.51)),
            Link("inst-protection", "instrumentation", "protection", ControlRoomSubsystemSchematicConnectionKind.MeasurementSignal, "PROTECTION INPUTS", "MEASURED ONLY", "FAIL-CLOSED", signalState, .42, .55, P(.36,.38), P(.36,.54), P(.53,.54), P(.53,.63)),
            Link("protection-arb", "protection", "arbitration", ControlRoomSubsystemSchematicConnectionKind.ProtectionOverride, "PROTECTION OVERRIDE", snapshot.AnyTripActive ? "ACTIVE" : "HIGHEST PRIORITY PATH", "OVERRIDES NORMAL / SUPERVISORY CONTROL", protectionState, .75, .67, P(.71,.73), P(.82,.73), P(.82,.57)),
            Link("inst-alarm", "instrumentation", "alarms", ControlRoomSubsystemSchematicConnectionKind.AlarmSignal, "ALARM CONDITIONS", $"{snapshot.AnnunciatedAlarmCount} ANNUNCIATED", "OPERATOR ANNUNCIATION", alarmState, .29, .56, P(.31,.38), P(.31,.65)),
            Link("protection-alarm", "protection", "alarms", ControlRoomSubsystemSchematicConnectionKind.AlarmSignal, "TRIP / FIRST-OUT", $"{snapshot.AlarmEvents.FirstOutCount} FIRST-OUT", "EVIDENCE, NOT CONTROL", alarmState, .44, .77, P(.48,.77), P(.40,.77)),
        };

        return new ControlRoomSubsystemSchematicSnapshot(
            ControlRoomSubsystemSchematicKind.InstrumentationProtection,
            "INSTRUMENTATION / CONTROL / PROTECTION SIGNAL-FLOW SCHEMATIC",
            "Signal paths are intentionally distinct from process piping; protection priority is explicit",
            nodes,
            connections,
            "Solid process piping is not used here. Dashed/dotted signal paths show information and control flow. The PROTECTION OVERRIDE path has priority over normal and supervisory control; alarms report conditions but do not own physical protection reset.");
    }

    public static string BuildGeneratorPowerPathDiagnostic(ControlRoomSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        if (snapshot.RunState == ControlRoomRunState.ShellOnly)
        {
            return "POWER PATH UNAVAILABLE · no integrated runtime is attached.";
        }

        var generator = snapshot.Electrical.Generators.FirstOrDefault();
        var rotor = snapshot.TurbineSecondary.Rotors.FirstOrDefault();
        if (generator is null || rotor is null)
        {
            return "POWER PATH UNAVAILABLE · generator or turbine rotor presentation data is missing.";
        }

        if (snapshot.TurbineSecondary.TurbineTripActive)
        {
            return "0 MWe DIAGNOSTIC · TURBINE TRIP ACTIVE. Steam admission/shaft production is protection-subordinate; diagnose and safely reset protection before expecting generation.";
        }

        if (snapshot.Electrical.GeneratorTripActive)
        {
            return "0 MWe DIAGNOSTIC · GENERATOR TRIP ACTIVE. Generator connection/load commands remain protection-subordinate until canonical reset conditions are satisfied.";
        }

        if (!generator.BreakerClosed)
        {
            return generator.SynchronizationConditionsSatisfied
                ? "0 MWe IS EXPECTED · breaker OPEN. SYNC READY: issue CLOSE BREAKER, verify PARALLELED, then use LOAD RAISE to request electrical export."
                : "0 MWe IS EXPECTED · breaker OPEN and SYNC NOT READY. Bring turbine frequency/phase/voltage into the synchronization window before CLOSE BREAKER; only then can load be requested.";
        }

        var requested = generator.RequestedElectricalPower.NumericValue;
        if (requested.HasValue && requested.Value <= 0.001d)
        {
            return "0 MWe IS EXPECTED · breaker is CLOSED/PARALLELED but requested electrical load is 0 MWe. Use GENERATOR LOAD RAISE in bounded increments, then monitor shaft power, rotor speed and actual MWe.";
        }

        var shaft = snapshot.TurbineSecondary.TotalTurbineShaftPower.NumericValue;
        var output = snapshot.Electrical.GrossElectricalOutput.NumericValue;
        var steam = snapshot.TurbineSecondary.EffectiveTurbineSteamFlow.NumericValue;
        var modelRotorShaft = SumAvailable(snapshot.TurbineSecondary.Rotors.Select(static rotor => rotor.ShaftPower));
        if (requested.HasValue && requested.Value > 0.001d && !shaft.HasValue)
        {
            return $"LOAD REQUEST {requested.Value:0.###} MWe · TURBINE SHAFT POWER MEASUREMENT UNAVAILABLE · MODEL rotor shaft {FormatNullable(modelRotorShaft, "MW")}. Cannot confirm measured mechanical support; inspect MAIN STEAM flow/admission valves, turbine speed and turbine protection. Current steam {FormatNullable(steam, "kg/s")}; amber SHAFT color denotes mechanical energy, not a warning.";
        }

        if (requested.HasValue && requested.Value > 0.001d && shaft.HasValue && shaft.Value <= 0.05d)
        {
            return $"LOAD REQUEST {requested.Value:0.###} MWe BUT MEASURED SHAFT POWER IS NEAR ZERO · MODEL rotor shaft {FormatNullable(modelRotorShaft, "MW")} · inspect MAIN STEAM flow/admission valves, turbine speed and turbine protection. Current steam {FormatNullable(steam, "kg/s")}; amber SHAFT color denotes mechanical energy, not a warning.";
        }

        if (requested.HasValue && requested.Value > 0.001d && output.HasValue && output.Value <= 0.05d)
        {
            return $"LOAD REQUEST {requested.Value:0.###} MWe WITH BREAKER CLOSED, BUT ACTUAL OUTPUT IS NEAR ZERO · shaft {FormatNullable(shaft, "MW")}. Inspect mechanical input, rotor speed and protection before increasing load further.";
        }

        return $"POWER PATH ACTIVE · breaker CLOSED/PARALLELED · request {FormatNullable(requested, "MWe")} · shaft {FormatNullable(shaft, "MW")} · actual {FormatNullable(output, "MWe")}. Amber SHAFT is the normal mechanical-energy color.";
    }

    private static double? SumAvailable(IEnumerable<ControlRoomValueSnapshot> values)
    {
        var canonical = values.ToArray();
        if (canonical.Length == 0 || canonical.Any(static value => !value.NumericValue.HasValue))
        {
            return null;
        }

        return canonical.Sum(static value => value.NumericValue!.Value);
    }

    private static ControlRoomSubsystemSchematicNodeSnapshot Node(
        string id,
        string name,
        ControlRoomSubsystemSchematicNodeKind kind,
        double x,
        double y,
        double width,
        double height,
        ControlRoomVisualState state,
        string status,
        string primary,
        string secondary,
        string input,
        string output)
        => new(id, name, kind, x, y, width, height, state, status, primary, secondary, input, output);

    private static ControlRoomSubsystemSchematicConnectionSnapshot Link(
        string id,
        string from,
        string to,
        ControlRoomSubsystemSchematicConnectionKind kind,
        string label,
        string primary,
        string secondary,
        ControlRoomVisualState state,
        double labelX,
        double labelY,
        params ControlRoomSubsystemSchematicPointSnapshot[] route)
        => new(id, from, to, kind, label, primary, secondary, state, labelX, labelY, route);

    private static ControlRoomSubsystemSchematicPointSnapshot P(double x, double y) => new(x, y);

    private static string Display(string label, ControlRoomValueSnapshot value)
        => $"{label} {DisplayValue(value)}";

    private static string DisplayValue(ControlRoomValueSnapshot? value)
        => value is null || value.State == ControlRoomVisualState.Unavailable
            ? "—"
            : $"{value.ValueText} {value.Unit}".TrimEnd();

    private static ControlRoomValueSnapshot Sum(IEnumerable<ControlRoomValueSnapshot> values, string unit)
    {
        var list = values.ToArray();
        if (list.Length == 0 || list.Any(static value => !value.NumericValue.HasValue))
        {
            return ControlRoomValueSnapshot.Unavailable(unit);
        }

        return new ControlRoomValueSnapshot(
            list.Sum(static value => value.NumericValue!.Value).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture),
            unit,
            list.Sum(static value => value.NumericValue!.Value),
            Worst(list));
    }

    private static ControlRoomValueSnapshot Average(IEnumerable<ControlRoomValueSnapshot> values, string unit)
    {
        var list = values.ToArray();
        if (list.Length == 0 || list.Any(static value => !value.NumericValue.HasValue))
        {
            return ControlRoomValueSnapshot.Unavailable(unit);
        }

        var average = list.Average(static value => value.NumericValue!.Value);
        return new ControlRoomValueSnapshot(
            average.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture),
            unit,
            average,
            Worst(list));
    }

    private static string Range(string label, IEnumerable<ControlRoomValueSnapshot> values)
    {
        var list = values.Where(static value => value.NumericValue.HasValue).ToArray();
        if (list.Length == 0)
        {
            return $"{label} —";
        }

        var min = list.Min(static value => value.NumericValue!.Value);
        var max = list.Max(static value => value.NumericValue!.Value);
        var unit = list[0].Unit;
        return Math.Abs(max - min) < 1e-9d
            ? FormattableString.Invariant($"{label} {min:0.###} {unit}").TrimEnd()
            : FormattableString.Invariant($"{label} {min:0.###}–{max:0.###} {unit}").TrimEnd();
    }

    private static ControlRoomVisualState Worst(params ControlRoomValueSnapshot[] values) => Worst((IEnumerable<ControlRoomValueSnapshot>)values);

    private static ControlRoomVisualState Worst(IEnumerable<ControlRoomValueSnapshot> values)
    {
        var states = values.Select(static value => value.State).ToArray();
        if (states.Length == 0)
        {
            return ControlRoomVisualState.Unavailable;
        }

        if (states.Contains(ControlRoomVisualState.Trip))
        {
            return ControlRoomVisualState.Trip;
        }

        if (states.Contains(ControlRoomVisualState.Warning))
        {
            return ControlRoomVisualState.Warning;
        }

        if (states.All(static state => state == ControlRoomVisualState.Unavailable))
        {
            return ControlRoomVisualState.Unavailable;
        }

        return ControlRoomVisualState.Normal;
    }

    private static double AverageNullable(IEnumerable<double?> values)
    {
        var finite = values.Where(static value => value.HasValue && double.IsFinite(value.Value)).Select(static value => value!.Value).ToArray();
        return finite.Length == 0 ? double.NaN : finite.Average();
    }

    private static string FormatNullable(double? value, string unit)
        => value.HasValue && double.IsFinite(value.Value)
            ? FormattableString.Invariant($"{value.Value:0.###} {unit}")
            : $"— {unit}";
}
