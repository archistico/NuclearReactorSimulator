using System.Globalization;
using NuclearReactorSimulator.Domain.Physics.Control.Alarms;
using NuclearReactorSimulator.Domain.Physics.Instrumentation;
using NuclearReactorSimulator.Simulation.Physics.Control.Alarms;
using NuclearReactorSimulator.Simulation.Physics.Control.Integration;
using NuclearReactorSimulator.Simulation.Physics.Instrumentation;

namespace NuclearReactorSimulator.Application.ControlRoom;

/// <summary>
/// Projects validated M5.7 immutable boundaries into the intentionally smaller M6 presentation contract.
/// Measured instruments are sourced from the candidate instrumentation frame; explicitly labelled model diagnostics are
/// projected here rather than exposing Simulation state to Avalonia.
/// </summary>
public static class ControlRoomSnapshotProjector
{
    private const string ReactorThermalPowerSourceId = "plant/reactor/thermal-power";

    public static ControlRoomSnapshot Project(
        long logicalStep,
        ControlRoomRunState runState,
        IntegratedAutomaticOperationSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        if (logicalStep < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(logicalStep), "Logical step cannot be negative.");
        }

        var measuredSignals = snapshot.NextMeasuredSignals.Signals;
        var alarms = snapshot.Control.Alarms;
        var protection = snapshot.Control.ProtectedControl.Protection;

        return new ControlRoomSnapshot(
            logicalStep,
            runState,
            measuredSignals.Count,
            measuredSignals.Count(static signal => signal.Validity == SignalValidity.Invalid),
            alarms.AnnunciatedCount,
            alarms.UnacknowledgedCount,
            protection.ReactorScramActive,
            protection.TurbineTripActive,
            protection.GeneratorTripActive,
            ProjectReactorCore(snapshot),
            ProjectPrimaryCircuit(snapshot),
            ProjectTurbineSecondary(snapshot),
            ProjectElectrical(snapshot),
            ProjectAlarmEvents(logicalStep, alarms));
    }

    private static ReactorCorePanelSnapshot ProjectReactorCore(IntegratedAutomaticOperationSnapshot snapshot)
    {
        var measuredFrame = snapshot.NextMeasuredSignals;
        var protectedControl = snapshot.Control.ProtectedControl;
        var reactorControl = protectedControl.ReactorPrimary;
        var protection = protectedControl.Protection;
        var primary = protectedControl.FullPlant.IntegratedCycle.PrimaryCircuit;

        var power = ProjectMeasuredSource(
            measuredFrame,
            ReactorThermalPowerSourceId,
            "MWth",
            1d / 1_000_000d,
            "0.0");

        var periodSeconds = reactorControl.PointKinetics.ReactorPeriodSeconds;
        var period = periodSeconds.HasValue && double.IsFinite(periodSeconds.Value)
            ? Value(periodSeconds.Value, "s", "0.00")
            : ControlRoomValueSnapshot.Unavailable("s");

        var totalReactivity = Value(reactorControl.PointKinetics.ReactivityCents, "¢", "0.0");
        var rodReactivity = Value(reactorControl.CommittedRodReactivity.Total.Pcm, "pcm", "0.0");
        var nonRodReactivity = Value(reactorControl.NonRodReactivity.Pcm, "pcm", "0.0");

        var rods = reactorControl.CandidateRodState.Rods
            .Select(rod => new ReactorRodPresentationSnapshot(
                rod.RodId,
                rod.Position.PercentWithdrawn,
                rod.Motion.ToString().ToUpperInvariant(),
                protection.ReactorScramActive ? ControlRoomVisualState.Trip : ControlRoomVisualState.Normal))
            .OrderBy(static rod => rod.RodId, StringComparer.Ordinal)
            .ToArray();

        var averageRodWithdrawal = rods.Length == 0
            ? ControlRoomValueSnapshot.Unavailable("%")
            : Value(rods.Average(static rod => rod.PercentWithdrawn), "% withdrawn", "0.0");

        var rodTargets = reactorControl.Definition.ActuatorSystem.Actuators
            .Where(static actuator => actuator.TargetKind == NuclearReactorSimulator.Domain.Physics.Control.ActuatorTargetKind.ControlRod)
            .Select(static actuator => new ReactorRodTargetPresentationSnapshot(
                actuator.TargetId,
                actuator.RodTargetKind == NuclearReactorSimulator.Domain.Physics.Reactor.ControlRods.ControlRodCommandTargetKind.Rod
                    ? ControlRoomCommandTargetKind.ControlRod
                    : ControlRoomCommandTargetKind.ControlRodGroup))
            .Distinct()
            .OrderBy(static target => target.TargetKind)
            .ThenBy(static target => target.TargetId, StringComparer.Ordinal)
            .ToArray();

        var zones = primary.Core.Zones
            .OrderBy(static zone => zone.Coordinate.Row)
            .ThenBy(static zone => zone.Coordinate.Column)
            .ThenBy(static zone => zone.ZoneId, StringComparer.Ordinal)
            .Select(zone => new ReactorCoreZonePresentationSnapshot(
                zone.ZoneId,
                zone.Coordinate.Row,
                zone.Coordinate.Column,
                zone.FissionThermalPower.Megawatts,
                zone.PowerFraction.Percent,
                zone.FuelTemperature.DegreesCelsius,
                zone.CoolantTemperature.DegreesCelsius,
                zone.VoidFraction?.Percent,
                ControlRoomVisualState.Normal))
            .ToArray();

        // M2.8 xenon physics is validated, but its state is not yet part of the M5.7 automatic-operation envelope.
        // M6 must not invent or privately reconstruct that missing operational boundary.
        var xenon = ControlRoomValueSnapshot.Unavailable("pcm");

        return new ReactorCorePanelSnapshot(
            power,
            period,
            totalReactivity,
            rodReactivity,
            nonRodReactivity,
            averageRodWithdrawal,
            xenon,
            zones,
            rods,
            rodTargets,
            protection.ReactorScramActive,
            protection.RodWithdrawalInhibited);
    }


    private static PrimaryCircuitPanelSnapshot ProjectPrimaryCircuit(IntegratedAutomaticOperationSnapshot snapshot)
    {
        var measuredFrame = snapshot.NextMeasuredSignals;
        var protectedControl = snapshot.Control.ProtectedControl;
        var primary = protectedControl.FullPlant.IntegratedCycle.PrimaryCircuit;
        var commandablePumpIds = protectedControl.ReactorPrimary.Definition.ActuatorSystem.Actuators
            .Where(static actuator => actuator.TargetKind == NuclearReactorSimulator.Domain.Physics.Control.ActuatorTargetKind.Pump)
            .Select(static actuator => actuator.TargetId)
            .ToHashSet(StringComparer.Ordinal);

        var loops = primary.MainCirculation.Loops
            .OrderBy(static loop => loop.LoopId, StringComparer.Ordinal)
            .Select(loop =>
            {
                var measuredFlow = ProjectMeasuredSource(
                    measuredFrame,
                    $"main-circulation-loop/{loop.LoopId}/total-pump-flow",
                    "kg/s",
                    1d,
                    "0.0");
                var measuredPressureRise = ProjectMeasuredSource(
                    measuredFrame,
                    $"main-circulation-loop/{loop.LoopId}/header-pressure-rise",
                    "MPa",
                    1d / 1_000_000d,
                    "0.000");

                var pumps = loop.Pumps
                    .OrderBy(static pump => pump.PumpId, StringComparer.Ordinal)
                    .Select(pump => new PrimaryCircuitPumpPresentationSnapshot(
                        pump.PumpId,
                        loop.LoopId,
                        pump.IsRunning,
                        Value(pump.EffectiveSpeed.Percent, "% rated", "0.0"),
                        Value(pump.MassFlowRate.KilogramsPerSecond, "kg/s", "0.0"),
                        Value(pump.ActivePressureBoost.Megapascals, "MPa", "0.000"),
                        commandablePumpIds.Contains(pump.PumpId)))
                    .ToArray();

                var branches = loop.Branches
                    .OrderBy(static branch => branch.FuelChannelGroupId, StringComparer.Ordinal)
                    .Select(branch => new PrimaryCircuitBranchPresentationSnapshot(
                        branch.FuelChannelGroupId,
                        branch.RepresentedChannelCount,
                        Value(branch.ChannelMassFlowRate.KilogramsPerSecond, "kg/s", "0.0"),
                        Value(branch.ReturnMassFlowRate.KilogramsPerSecond, "kg/s", "0.0"),
                        Value(branch.PerChannelMassFlowRate.KilogramsPerSecond, "kg/s/ch", "0.000"),
                        Value(branch.ChannelPressureDifference.Megapascals, "MPa", "0.000"),
                        branch.OutletPhase.ToString().ToUpperInvariant(),
                        FlowDirection(branch.ChannelMassFlowRate.KilogramsPerSecond),
                        branch.OutletVoidFraction.HasValue
                            ? $"Void {branch.OutletVoidFraction.Value.Percent:0.0}%"
                            : "Void —"))
                    .ToArray();

                return new PrimaryCircuitLoopPresentationSnapshot(
                    loop.LoopId,
                    measuredFlow,
                    measuredPressureRise,
                    Value(loop.SuctionHeaderPressure.Megapascals, "MPa", "0.000"),
                    Value(loop.PressureHeaderPressure.Megapascals, "MPa", "0.000"),
                    measuredFlow.NumericValue.HasValue ? FlowDirection(measuredFlow.NumericValue.Value) : "UNAVAILABLE",
                    pumps,
                    branches);
            })
            .ToArray();

        var drums = primary.SteamDrums.Drums
            .OrderBy(static drum => drum.DrumId, StringComparer.Ordinal)
            .Select(drum => new PrimaryCircuitSteamDrumPresentationSnapshot(
                drum.DrumId,
                drum.MainCirculationLoopId,
                ProjectMeasuredSource(
                    measuredFrame,
                    $"steam-drum/{drum.DrumId}/pressure",
                    "MPa",
                    1d / 1_000_000d,
                    "0.000"),
                ProjectMeasuredSource(
                    measuredFrame,
                    $"steam-drum/{drum.DrumId}/level",
                    "%",
                    100d,
                    "0.0"),
                Value(drum.Temperature.DegreesCelsius, "°C", "0.0"),
                Value(drum.IncomingReturnMassFlowRate.KilogramsPerSecond, "kg/s", "0.0"),
                Value(drum.SeparatedSteamMassFlowRate.KilogramsPerSecond, "kg/s", "0.0"),
                Value(drum.RecirculatedLiquidMassFlowRate.KilogramsPerSecond, "kg/s", "0.0"),
                drum.Phase.ToString().ToUpperInvariant()))
            .ToArray();

        var primaryNodeIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var loop in primary.Definition.MainCirculationSystem.Loops)
        {
            primaryNodeIds.Add(loop.SuctionHeaderNodeId);
            primaryNodeIds.Add(loop.PressureHeaderNodeId);
            primaryNodeIds.Add(loop.ReturnCollectorNodeId);
            foreach (var branch in loop.Branches)
            {
                var group = primary.Definition.ChannelGroups.GetGroup(branch.FuelChannelGroupId);
                primaryNodeIds.Add(group.InletCoolantNodeId);
                primaryNodeIds.Add(group.OutletCoolantNodeId);
            }
        }

        foreach (var drum in primary.SteamDrums.Drums)
        {
            primaryNodeIds.Add(drum.InventoryNodeId);
            primaryNodeIds.Add(drum.SteamOutletNodeId);
            primaryNodeIds.Add(drum.LiquidRecirculationNodeId);
        }

        var valves = primary.CandidatePlant.Definition.Valves
            .Where(valve => primaryNodeIds.Contains(valve.Pipe.FromNodeId) || primaryNodeIds.Contains(valve.Pipe.ToNodeId))
            .OrderBy(static valve => valve.Id, StringComparer.Ordinal)
            .Select(valve =>
            {
                var state = primary.CandidatePlant.GetValve(valve.Id);
                return new PrimaryCircuitValvePresentationSnapshot(
                    valve.Id,
                    valve.Pipe.FromNodeId,
                    valve.Pipe.ToNodeId,
                    Value(state.Position.Percent, "% open", "0.0"),
                    state.IsFailSafeActive);
            })
            .ToArray();

        return new PrimaryCircuitPanelSnapshot(
            loops,
            drums,
            valves,
            Value(primary.TotalPlantMass.Kilograms, "kg", "0"),
            Value(primary.TotalFeedwaterMassFlowRate.KilogramsPerSecond, "kg/s", "0.0"),
            Value(primary.TotalSteamExportMassFlowRate.KilogramsPerSecond, "kg/s", "0.0"));
    }

    private static TurbineSecondaryPanelSnapshot ProjectTurbineSecondary(IntegratedAutomaticOperationSnapshot snapshot)
    {
        var measuredFrame = snapshot.NextMeasuredSignals;
        var protectedControl = snapshot.Control.ProtectedControl;
        var integrated = protectedControl.FullPlant.IntegratedCycle;
        var turbine = integrated.TurbineExpansion;
        var mainSteam = turbine.MainSteamNetwork;
        var condenserSystem = integrated.Condenser;
        var feedwaterSystem = integrated.CondensateFeedwater;

        var steamLines = mainSteam.SteamLines
            .OrderBy(static line => line.LineId, StringComparer.Ordinal)
            .Select(line => new MainSteamLinePresentationSnapshot(
                line.LineId,
                line.SourceNodeId,
                line.HeaderNodeId,
                Value(line.MassFlowRate.KilogramsPerSecond, "kg/s", "0.0"),
                Value(line.PressureDifference.Megapascals, "MPa", "0.000"),
                FlowDirection(line.MassFlowRate.KilogramsPerSecond)))
            .ToArray();

        var admissionTrains = mainSteam.AdmissionTrains
            .OrderBy(static train => train.TrainId, StringComparer.Ordinal)
            .Select(train => new TurbineAdmissionTrainPresentationSnapshot(
                train.TrainId,
                train.HeaderNodeId,
                train.TurbineInletNodeId,
                train.StopValve.ValveId,
                Value(train.StopValve.EffectivePosition.Percent, "% open", "0.0"),
                train.ControlValve.ValveId,
                Value(train.ControlValve.EffectivePosition.Percent, "% open", "0.0"),
                train.AdmissionValve.ValveId,
                Value(train.AdmissionValve.EffectivePosition.Percent, "% open", "0.0"),
                Value(train.AdmissionValve.MassFlowRate.KilogramsPerSecond, "kg/s", "0.0"),
                Value(train.TurbineInletPressure.Megapascals, "MPa", "0.000"),
                Value(train.TurbineInletTemperature.DegreesCelsius, "°C", "0.0"),
                train.TurbineInletPhase.ToString().ToUpperInvariant()))
            .ToArray();

        var rotors = turbine.Rotors
            .OrderBy(static rotor => rotor.RotorId, StringComparer.Ordinal)
            .Select(rotor => new TurbineRotorPresentationSnapshot(
                rotor.RotorId,
                ProjectMeasuredSource(
                    measuredFrame,
                    $"turbine-rotor/{rotor.RotorId}/speed",
                    "rpm",
                    1d,
                    "0.0"),
                Value(rotor.ShaftPower.Megawatts, "MW", "0.0"),
                Value(rotor.NetTorque.NewtonMetres, "N·m", "0"),
                rotor.TripCommandActive,
                rotor.OverspeedDetectedAtStart || rotor.OverspeedDetectedAtEnd))
            .ToArray();

        var stageGroups = turbine.StageGroups
            .OrderBy(static stage => stage.StageGroupId, StringComparer.Ordinal)
            .Select(stage => new TurbineStageGroupPresentationSnapshot(
                stage.StageGroupId,
                stage.RotorId,
                stage.InletNodeId,
                stage.ExhaustNodeId,
                Value(stage.EffectiveMassFlowRate.KilogramsPerSecond, "kg/s", "0.0"),
                Value(stage.ShaftPower.Megawatts, "MW", "0.0"),
                Value(stage.InletPressure.Megapascals, "MPa", "0.000"),
                Value(stage.InletTemperature.DegreesCelsius, "°C", "0.0"),
                stage.InletPhase.ToString().ToUpperInvariant(),
                stage.TripBlocked))
            .ToArray();

        var condensers = condenserSystem.Condensers
            .OrderBy(static condenser => condenser.CondenserId, StringComparer.Ordinal)
            .Select(condenser => new CondenserPresentationSnapshot(
                condenser.CondenserId,
                condenser.TurbineStageGroupId,
                ProjectMeasuredSource(
                    measuredFrame,
                    $"condenser/{condenser.CondenserId}/pressure",
                    "kPa abs",
                    1d / 1_000d,
                    "0.00"),
                ProjectMeasuredSource(
                    measuredFrame,
                    $"condenser/{condenser.CondenserId}/vacuum",
                    "kPa",
                    1d / 1_000d,
                    "0.00"),
                ProjectMeasuredSource(
                    measuredFrame,
                    $"condenser/{condenser.CondenserId}/hotwell-mass",
                    "kg",
                    1d,
                    "0"),
                Value(condenser.ActualCondensationMassFlowRate.KilogramsPerSecond, "kg/s", "0.0"),
                Value(condenser.HeatRejectionPower.Megawatts, "MW", "0.0"),
                Value(condenser.FinalSteamSpaceTemperature.DegreesCelsius, "°C", "0.0"),
                Value(condenser.FinalHotwellTemperature.DegreesCelsius, "°C", "0.0"),
                condenser.FinalSteamSpacePhase.ToString().ToUpperInvariant()))
            .ToArray();

        var feedwaterTrains = feedwaterSystem.Trains
            .OrderBy(static train => train.TrainId, StringComparer.Ordinal)
            .Select(train => new FeedwaterTrainPresentationSnapshot(
                train.TrainId,
                train.CondenserId,
                train.FeedwaterTargetNodeId,
                ProjectSecondaryPump(train.CondensatePump),
                ProjectSecondaryPump(train.FeedwaterPump),
                Value(train.FinalHotwellMass.Kilograms, "kg", "0"),
                Value(train.FinalFeedwaterInventoryMass.Kilograms, "kg", "0"),
                Value(train.FinalFeedwaterInventoryTemperature.DegreesCelsius, "°C", "0.0"),
                Value(train.ThermalConditioningPower.Megawatts, "MW", "0.000")))
            .ToArray();

        return new TurbineSecondaryPanelSnapshot(
            steamLines,
            admissionTrains,
            rotors,
            stageGroups,
            condensers,
            feedwaterTrains,
            Value(mainSteam.TotalTurbineAdmissionMassFlowRate.KilogramsPerSecond, "kg/s", "0.0"),
            ProjectMeasuredSource(
                measuredFrame,
                "plant/turbine/total-shaft-power",
                "MW",
                1d / 1_000_000d,
                "0.0"),
            ProjectMeasuredSource(
                measuredFrame,
                "plant/condenser/total-heat-rejection",
                "MW",
                1d / 1_000_000d,
                "0.0"),
            protectedControl.Protection.TurbineTripActive);
    }

    private static ElectricalPanelSnapshot ProjectElectrical(IntegratedAutomaticOperationSnapshot snapshot)
    {
        var measuredFrame = snapshot.NextMeasuredSignals;
        var protectedControl = snapshot.Control.ProtectedControl;
        var generatorGrid = protectedControl.FullPlant.IntegratedCycle.GeneratorGrid;
        var grid = generatorGrid.Grid;

        var generators = generatorGrid.Generators
            .OrderBy(static generator => generator.GeneratorId, StringComparer.Ordinal)
            .Select(generator => new GeneratorPresentationSnapshot(
                generator.GeneratorId,
                generator.RotorId,
                generator.BreakerId,
                ProjectMeasuredSource(
                    measuredFrame,
                    $"generator/{generator.GeneratorId}/frequency",
                    "Hz",
                    1d,
                    "0.000"),
                ProjectMeasuredSource(
                    measuredFrame,
                    $"generator/{generator.GeneratorId}/electrical-output",
                    "MWe",
                    1d / 1_000_000d,
                    "0.0"),
                Value(generator.TerminalLineVoltage.Kilovolts, "kV", "0.0"),
                Value(generator.GridLineVoltage.Kilovolts, "kV", "0.0"),
                Value(generator.FinalPhaseDifference.Degrees, "°", "0.00"),
                Value(generator.MechanicalInputPower.Megawatts, "MW", "0.0"),
                Value(generator.ConversionLossPower.Megawatts, "MW", "0.000"),
                generator.SynchronizationConditionsSatisfied,
                generator.BreakerFinallyClosed,
                generator.CloseCommandAccepted,
                generator.CloseCommandRejected))
            .ToArray();

        return new ElectricalPanelSnapshot(
            new ElectricalGridPresentationSnapshot(
                grid.GridId,
                Value(grid.Frequency.Hertz, "Hz", "0.000"),
                Value(grid.LineVoltage.Kilovolts, "kV", "0.0"),
                Value(grid.FinalPhaseAngle.Degrees, "°", "0.00")),
            generators,
            ProjectMeasuredSource(
                measuredFrame,
                "plant/generator/gross-electrical-output",
                "MWe",
                1d / 1_000_000d,
                "0.0"),
            protectedControl.Protection.GeneratorTripActive);
    }

    private static AlarmEventsPanelSnapshot ProjectAlarmEvents(long logicalStep, AlarmSystemSnapshot alarms)
    {
        var rows = alarms.Alarms
            .OrderBy(static alarm => alarm.AlarmId, StringComparer.Ordinal)
            .Select(alarm =>
            {
                var definition = alarms.Definition.GetAlarm(alarm.AlarmId);
                return new ControlRoomAlarmPresentationSnapshot(
                    alarm.AlarmId,
                    alarm.Title,
                    MapSeverity(alarm.Severity),
                    MapAnnunciatorState(alarm.AnnunciatorState),
                    alarm.FirstOutGroupId,
                    alarm.ConditionActive,
                    alarm.IsLatched,
                    alarm.IsAcknowledged,
                    alarm.IsAnnunciated,
                    alarm.IsFirstOut,
                    alarm.ActivationSequence,
                    definition.LatchingMode == AlarmLatchingMode.LatchedUntilReset);
            })
            .ToArray();

        var firstOutGroups = alarms.FirstOutGroups
            .Select(group => new ControlRoomFirstOutGroupPresentationSnapshot(
                group.GroupId,
                group.FirstOutAlarmId,
                group.AnnunciatedAlarmIds.ToArray()))
            .ToArray();

        var titles = rows.ToDictionary(static alarm => alarm.AlarmId, static alarm => alarm.Title, StringComparer.Ordinal);
        var events = alarms.Events
            .OrderBy(static alarmEvent => alarmEvent.Sequence)
            .Select(alarmEvent => new ControlRoomAlarmEventPresentationSnapshot(
                alarmEvent.Sequence,
                logicalStep,
                alarmEvent.AlarmId,
                titles.TryGetValue(alarmEvent.AlarmId, out var title) ? title : alarmEvent.AlarmId,
                MapEventKind(alarmEvent.Kind)))
            .ToArray();

        return new AlarmEventsPanelSnapshot(rows, firstOutGroups, events);
    }

    private static ControlRoomAlarmSeverity MapSeverity(AlarmSeverity severity)
        => severity switch
        {
            AlarmSeverity.Advisory => ControlRoomAlarmSeverity.Advisory,
            AlarmSeverity.Warning => ControlRoomAlarmSeverity.Warning,
            AlarmSeverity.Trip => ControlRoomAlarmSeverity.Trip,
            _ => throw new ArgumentOutOfRangeException(nameof(severity), severity, "Unknown alarm severity."),
        };

    private static ControlRoomAlarmAnnunciatorState MapAnnunciatorState(AlarmAnnunciatorState state)
        => state switch
        {
            AlarmAnnunciatorState.Normal => ControlRoomAlarmAnnunciatorState.Normal,
            AlarmAnnunciatorState.ActiveUnacknowledged => ControlRoomAlarmAnnunciatorState.ActiveUnacknowledged,
            AlarmAnnunciatorState.ActiveAcknowledged => ControlRoomAlarmAnnunciatorState.ActiveAcknowledged,
            AlarmAnnunciatorState.ReturnedUnacknowledged => ControlRoomAlarmAnnunciatorState.ReturnedUnacknowledged,
            AlarmAnnunciatorState.ReturnedAcknowledged => ControlRoomAlarmAnnunciatorState.ReturnedAcknowledged,
            _ => throw new ArgumentOutOfRangeException(nameof(state), state, "Unknown alarm annunciator state."),
        };

    private static ControlRoomAlarmEventKind MapEventKind(AlarmEventKind kind)
        => kind switch
        {
            AlarmEventKind.Activated => ControlRoomAlarmEventKind.Activated,
            AlarmEventKind.Cleared => ControlRoomAlarmEventKind.Cleared,
            AlarmEventKind.Acknowledged => ControlRoomAlarmEventKind.Acknowledged,
            AlarmEventKind.Reset => ControlRoomAlarmEventKind.Reset,
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown alarm event kind."),
        };

    private static SecondaryPumpPresentationSnapshot ProjectSecondaryPump(
        NuclearReactorSimulator.Simulation.Physics.TurbineIsland.Feedwater.FeedwaterPumpSnapshot pump)
        => new(
            pump.PumpId,
            pump.IsRunning,
            Value(pump.EffectiveSpeed.Percent, "% rated", "0.0"),
            Value(pump.MassFlowRate.KilogramsPerSecond, "kg/s", "0.0"),
            Value(pump.ActivePressureBoost.Megapascals, "MPa", "0.000"),
            Value(pump.ShaftPowerDemand.Megawatts, "MW", "0.000"));

    private static string FlowDirection(double kilogramsPerSecond)
    {
        if (!double.IsFinite(kilogramsPerSecond))
        {
            return "UNAVAILABLE";
        }

        if (kilogramsPerSecond > 0d)
        {
            return "FORWARD →";
        }

        if (kilogramsPerSecond < 0d)
        {
            return "← REVERSE";
        }

        return "NO FLOW";
    }

    private static ControlRoomValueSnapshot ProjectMeasuredSource(
        MeasuredSignalFrame frame,
        string sourceId,
        string displayUnit,
        double engineeringScale,
        string format)
    {
        var channel = frame.Definition.Channels
            .Where(channel => string.Equals(channel.SourceId, sourceId, StringComparison.Ordinal))
            .OrderBy(static channel => channel.Id, StringComparer.Ordinal)
            .FirstOrDefault();
        if (channel is null)
        {
            return ControlRoomValueSnapshot.Unavailable(displayUnit);
        }

        var signal = frame.GetSignal(channel.Id);
        if (signal.Validity != SignalValidity.Valid
            || !signal.EngineeringValue.HasValue
            || !double.IsFinite(signal.EngineeringValue.Value)
            || signal.Quality is SignalQuality.Bad or SignalQuality.Unavailable)
        {
            return ControlRoomValueSnapshot.Unavailable(displayUnit);
        }

        var scaled = signal.EngineeringValue.Value * engineeringScale;
        var state = signal.Quality == SignalQuality.Suspect || signal.OutOfMeasurementRange
            ? ControlRoomVisualState.Warning
            : ControlRoomVisualState.Normal;

        return new ControlRoomValueSnapshot(
            scaled.ToString(format, CultureInfo.InvariantCulture),
            displayUnit,
            scaled,
            state);
    }

    private static ControlRoomValueSnapshot Value(double value, string unit, string format)
    {
        if (!double.IsFinite(value))
        {
            return ControlRoomValueSnapshot.Unavailable(unit);
        }

        return new ControlRoomValueSnapshot(
            value.ToString(format, CultureInfo.InvariantCulture),
            unit,
            value,
            ControlRoomVisualState.Normal);
    }
}
