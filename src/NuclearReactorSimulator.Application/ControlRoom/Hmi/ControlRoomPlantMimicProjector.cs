using NuclearReactorSimulator.Application.ControlRoom;

namespace NuclearReactorSimulator.Application.ControlRoom.Hmi;

/// <summary>
/// Builds the M10.9.3 whole-plant mimic from the already projected immutable control-room snapshot.
/// This is presentation composition only: it does not infer hidden plant state or introduce new topology ownership.
/// </summary>
public static class ControlRoomPlantMimicProjector
{
    public static ControlRoomPlantMimicSnapshot Project(ControlRoomSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var reactor = snapshot.ReactorCore;
        var primary = snapshot.PrimaryCircuit;
        var turbine = snapshot.TurbineSecondary;
        var electrical = snapshot.Electrical;
        var shellOnly = snapshot.RunState == ControlRoomRunState.ShellOnly;

        var pumpCount = primary.Pumps.Count;
        var runningPumpCount = primary.Pumps.Count(static pump => pump.IsRunning);
        var feedwaterPumpCount = turbine.FeedwaterTrains.Count;
        var runningFeedwaterPumpCount = turbine.FeedwaterTrains.Count(static train => train.FeedwaterPump.IsRunning);

        var elements = new[]
        {
            new ControlRoomPlantMimicElementSnapshot(
                "reactor-core",
                "REACTOR CORE",
                ControlRoomPlantMimicElementKind.Reactor,
                0.22d, 0.30d, 0.15d, 0.28d,
                reactor.ReactorScramActive ? ControlRoomVisualState.Trip : Worst(reactor.ReactorThermalPower, reactor.AverageRodWithdrawal),
                shellOnly ? "REACTOR DATA UNAVAILABLE" : reactor.ReactorScramActive ? "SCRAM ACTIVE" : reactor.RodWithdrawalInhibited ? "WITHDRAWAL INHIBITED" : "REACTOR AVAILABLE",
                Display("POWER", reactor.ReactorThermalPower),
                Display("RODS", reactor.AverageRodWithdrawal),
                "IN · PRESSURIZED PRIMARY COOLANT",
                "OUT · HEATED COOLANT / CHANNEL RETURN",
                $"{reactor.ZoneCount} CORE ZONES · {reactor.RodCount} RODS · {reactor.ProtectionText}",
                ControlRoomWorkspaceId.Reactor),

            new ControlRoomPlantMimicElementSnapshot(
                "main-circulation",
                "MAIN CIRCULATION",
                ControlRoomPlantMimicElementKind.MainCirculation,
                0.045d, 0.57d, 0.145d, 0.22d,
                Worst(primary.Loops.SelectMany(static loop => new[] { loop.TotalPumpFlow, loop.HeaderPressureRise })),
                shellOnly || pumpCount == 0 ? "MCP DATA UNAVAILABLE" : $"{runningPumpCount}/{pumpCount} MCP RUNNING",
                Display("FLOW", Sum(primary.Loops.Select(static loop => loop.TotalPumpFlow), "kg/s")),
                Display("ΔP", Average(primary.Loops.Select(static loop => loop.HeaderPressureRise), "MPa")),
                "IN · DRUM LIQUID RETURN",
                "OUT · PRESSURIZED COOLANT TO CORE",
                $"{primary.Loops.Count} MAIN CIRCULATION LOOPS · operator-commandable pumps remain controlled through canonical commands",
                ControlRoomWorkspaceId.PrimaryCircuit),

            new ControlRoomPlantMimicElementSnapshot(
                "steam-drums",
                "STEAM DRUMS",
                ControlRoomPlantMimicElementKind.SteamDrums,
                0.42d, 0.18d, 0.145d, 0.23d,
                Worst(primary.SteamDrums.SelectMany(static drum => new[] { drum.Pressure, drum.Level })),
                shellOnly || primary.SteamDrums.Count == 0 ? "DRUM DATA UNAVAILABLE" : $"{primary.SteamDrums.Count} DRUMS IN SERVICE",
                Range("P", primary.SteamDrums.Select(static drum => drum.Pressure)),
                Range("LEVEL", primary.SteamDrums.Select(static drum => drum.Level)),
                "IN · CORE RETURN + FEEDWATER",
                "OUT · STEAM TO TURBINE + LIQUID RECIRCULATION",
                $"STEAM EXPORT {DisplayValue(primary.TotalSteamExportFlow)} · FEEDWATER {DisplayValue(primary.TotalFeedwaterFlow)}",
                ControlRoomWorkspaceId.PrimaryCircuit),

            new ControlRoomPlantMimicElementSnapshot(
                "turbine",
                "STEAM TURBINE",
                ControlRoomPlantMimicElementKind.Turbine,
                0.61d, 0.18d, 0.14d, 0.23d,
                turbine.TurbineTripActive ? ControlRoomVisualState.Trip : Worst(turbine.EffectiveTurbineSteamFlow, turbine.TotalTurbineShaftPower),
                shellOnly ? "TURBINE DATA UNAVAILABLE" : turbine.TurbineTripActive ? "TURBINE TRIP ACTIVE" : turbine.Rotors.Count == 0 ? "ROTOR DATA UNAVAILABLE" : turbine.Rotors[0].ProtectionText,
                Display("STEAM", turbine.EffectiveTurbineSteamFlow),
                Display("SHAFT", turbine.TotalTurbineShaftPower),
                "IN · MAIN STEAM",
                "OUT · SHAFT POWER + EXHAUST STEAM",
                turbine.Rotors.Count == 0 ? "No canonical rotor is projected." : $"{turbine.Rotors.Count} ROTOR(S) · {Display("SPEED", turbine.Rotors[0].Speed)}",
                ControlRoomWorkspaceId.TurbineSecondary),

            new ControlRoomPlantMimicElementSnapshot(
                "generator",
                "GENERATOR",
                ControlRoomPlantMimicElementKind.Generator,
                0.785d, 0.18d, 0.12d, 0.23d,
                electrical.GeneratorTripActive ? ControlRoomVisualState.Trip : Worst(electrical.GrossElectricalOutput),
                shellOnly ? "GENERATOR DATA UNAVAILABLE" : electrical.GeneratorTripActive ? "GENERATOR TRIP ACTIVE" : electrical.Generators.Count == 0 ? "GENERATOR DATA UNAVAILABLE" : electrical.Generators[0].BreakerText,
                Display("OUTPUT", electrical.GrossElectricalOutput),
                electrical.Generators.Count == 0 ? "FREQUENCY —" : Display("FREQUENCY", electrical.Generators[0].Frequency),
                "IN · TURBINE SHAFT",
                "OUT · ELECTRICAL POWER",
                electrical.Generators.Count == 0 ? "No canonical generator is projected." : electrical.Generators[0].DisplaySynchronizationText,
                ControlRoomWorkspaceId.Electrical),

            new ControlRoomPlantMimicElementSnapshot(
                "grid",
                "EXTERNAL GRID",
                ControlRoomPlantMimicElementKind.Grid,
                0.925d, 0.205d, 0.07d, 0.18d,
                Worst(electrical.Grid.Frequency, electrical.Grid.LineVoltage),
                shellOnly ? "GRID DATA UNAVAILABLE" : "GRID REFERENCE",
                Display("f", electrical.Grid.Frequency),
                Display("V", electrical.Grid.LineVoltage),
                "IN · GENERATOR EXPORT",
                "OUT · EXTERNAL ELECTRICAL SYSTEM",
                $"GRID {electrical.Grid.GridId} · phase {DisplayValue(electrical.Grid.PhaseAngle)}",
                ControlRoomWorkspaceId.Electrical),

            new ControlRoomPlantMimicElementSnapshot(
                "condenser",
                "CONDENSER / HOTWELL",
                ControlRoomPlantMimicElementKind.Condenser,
                0.61d, 0.63d, 0.15d, 0.23d,
                Worst(turbine.Condensers.SelectMany(static condenser => new[] { condenser.Pressure, condenser.Vacuum, condenser.HotwellMass })),
                shellOnly || turbine.Condensers.Count == 0 ? "CONDENSER DATA UNAVAILABLE" : $"{turbine.Condensers.Count} CONDENSER(S)",
                turbine.Condensers.Count == 0 ? "VACUUM —" : Range("VAC", turbine.Condensers.Select(static condenser => condenser.Vacuum)),
                Display("HEAT", turbine.TotalCondenserHeatRejection),
                "IN · TURBINE EXHAUST STEAM",
                "OUT · CONDENSATE / HOTWELL",
                turbine.Condensers.Count == 0 ? "No canonical condenser is projected." : Range("HOTWELL", turbine.Condensers.Select(static condenser => condenser.HotwellMass)),
                ControlRoomWorkspaceId.TurbineSecondary),

            new ControlRoomPlantMimicElementSnapshot(
                "feedwater",
                "CONDENSATE / FEEDWATER",
                ControlRoomPlantMimicElementKind.Feedwater,
                0.39d, 0.66d, 0.17d, 0.21d,
                Worst(turbine.FeedwaterTrains.SelectMany(static train => new[] { train.FeedwaterPump.MassFlow, train.FeedwaterTemperature })),
                shellOnly || feedwaterPumpCount == 0 ? "FEEDWATER DATA UNAVAILABLE" : $"{runningFeedwaterPumpCount}/{feedwaterPumpCount} FW PUMPS RUNNING",
                Display("FLOW", primary.TotalFeedwaterFlow),
                turbine.FeedwaterTrains.Count == 0 ? "TEMP —" : Range("TEMP", turbine.FeedwaterTrains.Select(static train => train.FeedwaterTemperature)),
                "IN · CONDENSATE FROM HOTWELL",
                "OUT · CONDITIONED FEEDWATER TO DRUMS",
                turbine.FeedwaterTrains.Count == 0 ? "No canonical feedwater train is projected." : $"{turbine.FeedwaterTrains.Count} TRAIN(S) · inventories and pump states remain canonical secondary-cycle data",
                ControlRoomWorkspaceId.TurbineSecondary),
        };

        var drumPressure = primary.SteamDrums.Count == 0 ? "P —" : Range("P", primary.SteamDrums.Select(static drum => drum.Pressure));
        var drumTemperature = primary.SteamDrums.Count == 0 ? "T —" : Range("T", primary.SteamDrums.Select(static drum => drum.Temperature));
        var condenserPressure = turbine.Condensers.Count == 0 ? "P —" : Range("P", turbine.Condensers.Select(static condenser => condenser.Pressure));
        var feedwaterTemperature = turbine.FeedwaterTrains.Count == 0 ? "T —" : Range("T", turbine.FeedwaterTrains.Select(static train => train.FeedwaterTemperature));

        var connections = new[]
        {
            Connection("mcp-core", "main-circulation", "reactor-core", ControlRoomPlantMimicMedium.PrimaryCoolant, "PRIMARY COOLANT", DisplayValue(Sum(primary.Loops.Select(static loop => loop.TotalPumpFlow), "kg/s")), Display("PRESSURE", Average(primary.Loops.Select(static loop => loop.PressureHeaderPressure), "MPa")), Worst(primary.Loops.Select(static loop => loop.TotalPumpFlow)), 0.095d, 0.47d,
                (0.19d,0.68d),(0.205d,0.68d),(0.205d,0.44d),(0.22d,0.44d)),
            Connection("core-drums", "reactor-core", "steam-drums", ControlRoomPlantMimicMedium.PrimaryCoolant, "CHANNEL RETURN", DisplayValue(Sum(primary.Loops.Select(static loop => loop.TotalPumpFlow), "kg/s")), $"{drumPressure} · {drumTemperature}", Worst(primary.SteamDrums.Select(static drum => drum.Pressure)), 0.355d, 0.235d,
                (0.37d,0.39d),(0.395d,0.39d),(0.395d,0.295d),(0.42d,0.295d)),
            Connection("drums-mcp", "steam-drums", "main-circulation", ControlRoomPlantMimicMedium.PrimaryCoolant, "LIQUID RECIRCULATION", DisplayValue(Sum(primary.SteamDrums.Select(static drum => drum.RecirculationFlow), "kg/s")), drumTemperature, Worst(primary.SteamDrums.Select(static drum => drum.RecirculationFlow)), 0.29d, 0.67d,
                (0.44d,0.41d),(0.40d,0.41d),(0.40d,0.60d),(0.205d,0.60d),(0.205d,0.68d),(0.19d,0.68d)),
            Connection("drums-turbine", "steam-drums", "turbine", ControlRoomPlantMimicMedium.Steam, "MAIN STEAM", DisplayValue(primary.TotalSteamExportFlow), $"{drumPressure} · {drumTemperature}", Worst(primary.TotalSteamExportFlow), 0.565d, 0.115d,
                (0.565d,0.255d),(0.61d,0.255d)),
            Connection("turbine-generator", "turbine", "generator", ControlRoomPlantMimicMedium.Mechanical, "SHAFT", DisplayValue(turbine.TotalTurbineShaftPower), turbine.Rotors.Count == 0 ? "SPEED —" : DisplayValue(turbine.Rotors[0].Speed), Worst(turbine.TotalTurbineShaftPower), 0.745d, 0.105d,
                (0.75d,0.255d),(0.785d,0.255d)),
            Connection("generator-grid", "generator", "grid", ControlRoomPlantMimicMedium.Electrical, "ELECTRICAL EXPORT", DisplayValue(electrical.GrossElectricalOutput), $"{DisplayValue(electrical.Grid.Frequency)} · {DisplayValue(electrical.Grid.LineVoltage)}", Worst(electrical.GrossElectricalOutput), 0.91d, 0.48d,
                (0.905d,0.255d),(0.925d,0.255d)),
            Connection("turbine-condenser", "turbine", "condenser", ControlRoomPlantMimicMedium.Steam, "EXHAUST STEAM", DisplayValue(turbine.EffectiveTurbineSteamFlow), condenserPressure, Worst(turbine.EffectiveTurbineSteamFlow), 0.69d, 0.50d,
                (0.68d,0.41d),(0.68d,0.63d)),
            Connection("condenser-feedwater", "condenser", "feedwater", ControlRoomPlantMimicMedium.Condensate, "CONDENSATE", turbine.Condensers.Count == 0 ? "FLOW —" : DisplayValue(Sum(turbine.Condensers.Select(static condenser => condenser.CondensationFlow), "kg/s")), condenserPressure, Worst(turbine.Condensers.Select(static condenser => condenser.CondensationFlow)), 0.58d, 0.93d,
                (0.685d,0.86d),(0.685d,0.92d),(0.475d,0.92d),(0.475d,0.87d)),
            Connection("feedwater-drums", "feedwater", "steam-drums", ControlRoomPlantMimicMedium.Feedwater, "FEEDWATER", DisplayValue(primary.TotalFeedwaterFlow), feedwaterTemperature, Worst(primary.TotalFeedwaterFlow), 0.47d, 0.52d,
                (0.475d,0.66d),(0.475d,0.41d)),
        };

        return new ControlRoomPlantMimicSnapshot(
            elements,
            connections,
            "MAIN CIRCULATION → REACTOR → STEAM DRUMS → TURBINE → GENERATOR → GRID   ·   TURBINE → CONDENSER → FEEDWATER → STEAM DRUMS");
    }

    private static ControlRoomPlantMimicConnectionSnapshot Connection(
        string id,
        string from,
        string to,
        ControlRoomPlantMimicMedium medium,
        string mediumText,
        string primaryText,
        string secondaryText,
        ControlRoomVisualState state,
        double labelX,
        double labelY,
        params (double X, double Y)[] route)
        => new(
            id,
            from,
            to,
            medium,
            mediumText,
            primaryText,
            secondaryText,
            state,
            route.Select(static point => new ControlRoomPlantMimicPointSnapshot(point.X, point.Y)).ToArray(),
            labelX,
            labelY);

    private static string Display(string label, ControlRoomValueSnapshot value)
        => $"{label} {DisplayValue(value)}";

    private static string DisplayValue(ControlRoomValueSnapshot value)
        => value.State == ControlRoomVisualState.Unavailable
            ? "—"
            : $"{value.ValueText} {value.Unit}".TrimEnd();

    private static string Range(string label, IEnumerable<ControlRoomValueSnapshot> values)
    {
        var available = values.Where(static value => value.NumericValue.HasValue && value.State != ControlRoomVisualState.Unavailable).ToArray();
        if (available.Length == 0)
        {
            return $"{label} —";
        }

        var minimum = available.Min(static value => value.NumericValue!.Value);
        var maximum = available.Max(static value => value.NumericValue!.Value);
        var unit = available[0].Unit;
        return Math.Abs(maximum - minimum) < 1e-9d
            ? $"{label} {minimum:0.###} {unit}".TrimEnd()
            : $"{label} {minimum:0.###}–{maximum:0.###} {unit}".TrimEnd();
    }

    private static ControlRoomValueSnapshot Sum(IEnumerable<ControlRoomValueSnapshot> values, string unit)
    {
        var items = values.ToArray();
        if (items.Length == 0 || items.Any(static value => !value.NumericValue.HasValue || value.State == ControlRoomVisualState.Unavailable))
        {
            return ControlRoomValueSnapshot.Unavailable(unit);
        }

        var sum = items.Sum(static value => value.NumericValue!.Value);
        return new ControlRoomValueSnapshot(sum.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture), unit, sum, Worst(items));
    }

    private static ControlRoomValueSnapshot Average(IEnumerable<ControlRoomValueSnapshot> values, string unit)
    {
        var items = values.ToArray();
        if (items.Length == 0 || items.Any(static value => !value.NumericValue.HasValue || value.State == ControlRoomVisualState.Unavailable))
        {
            return ControlRoomValueSnapshot.Unavailable(unit);
        }

        var average = items.Average(static value => value.NumericValue!.Value);
        return new ControlRoomValueSnapshot(average.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture), unit, average, Worst(items));
    }

    private static ControlRoomVisualState Worst(params ControlRoomValueSnapshot[] values) => Worst((IEnumerable<ControlRoomValueSnapshot>)values);

    private static ControlRoomVisualState Worst(IEnumerable<ControlRoomValueSnapshot> values)
    {
        var states = values.Select(static value => value.State).ToArray();
        if (states.Length == 0 || states.All(static state => state == ControlRoomVisualState.Unavailable))
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

        return ControlRoomVisualState.Normal;
    }
}
