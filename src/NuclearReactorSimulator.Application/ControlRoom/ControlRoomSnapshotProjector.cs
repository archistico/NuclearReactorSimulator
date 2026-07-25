using System.Globalization;
using NuclearReactorSimulator.Application.ControlRoom.Hmi;
using NuclearReactorSimulator.Domain.Physics.Control.Alarms;
using NuclearReactorSimulator.Domain.Physics.Instrumentation;
using NuclearReactorSimulator.Simulation.Physics.Control.Alarms;
using NuclearReactorSimulator.Simulation.Physics.Control.Integration;
using NuclearReactorSimulator.Simulation.Physics.Control.TurbineSecondary;
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
        IntegratedAutomaticOperationSnapshot snapshot,
        TurbineSecondaryControlInputs? requestedSecondaryInputs = null)
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
            ProjectTurbineSecondary(snapshot, requestedSecondaryInputs),
            ProjectElectrical(snapshot),
            ProjectAlarmEvents(logicalStep, alarms),
            protectionReset: ProjectProtectionReset(protection));
    }

    private static ProtectionResetPresentationSnapshot ProjectProtectionReset(
        NuclearReactorSimulator.Simulation.Physics.Control.Protection.ProtectionSystemSnapshot protection)
    {
        var blockers = new List<string>();
        foreach (var function in protection.Functions)
        {
            if (function.TriggerActive)
            {
                blockers.Add($"{function.FunctionId}: trip condition still active");
            }
            else if (!function.ResetConditionSafe)
            {
                blockers.Add($"{function.FunctionId}: reset condition not safe");
            }
        }

        foreach (var permissive in protection.ResetPermissives.Where(static permissive => !permissive.IsSatisfied))
        {
            blockers.Add($"{permissive.PermissiveId}: reset permissive not satisfied");
        }

        var resetConditionsSatisfied = protection.Functions.All(static function => !function.TriggerActive && function.ResetConditionSafe)
            && protection.ResetPermissives.All(static permissive => permissive.IsSatisfied);
        return new ProtectionResetPresentationSnapshot(
            protection.ReactorScramActive || protection.TurbineTripActive || protection.GeneratorTripActive,
            resetConditionsSatisfied,
            protection.ResetRequested,
            protection.ResetAccepted,
            blockers);
    }

    private static ReactorCorePanelSnapshot ProjectReactorCore(IntegratedAutomaticOperationSnapshot snapshot)
    {
        var measuredFrame = snapshot.NextMeasuredSignals;
        var protectedControl = snapshot.Control.ProtectedControl;
        var reactorControl = protectedControl.ReactorPrimary;
        var protection = protectedControl.Protection;
        var primary = protectedControl.FullPlant.IntegratedCycle.PrimaryCircuit;

        var powerSetpointMegawatts = reactorControl.Loops
            .FirstOrDefault(static loop => loop.Kind == NuclearReactorSimulator.Domain.Physics.Control.ReactorPrimary.ReactorPrimaryControlLoopKind.ReactorPowerRodRegulation)
            ?.Setpoint / 1_000_000d;
        var referenceThermalPowerMegawatts = reactorControl.Definition.FissionPowerDefinition.Calibration.ReferenceThermalPower.Megawatts;
        var reactorPowerScaleMaximum = Math.Max(referenceThermalPowerMegawatts * 1.2d, 1d);
        var power = ProjectMeasuredSource(
            measuredFrame,
            ReactorThermalPowerSourceId,
            "MWth",
            1d / 1_000_000d,
            "0.0") with
        {
            InstrumentScale = new ControlRoomInstrumentScaleSnapshot(
                0d,
                reactorPowerScaleMaximum,
                setpoint: WithinScale(powerSetpointMegawatts, 0d, reactorPowerScaleMaximum)),
        };

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

        var averageRodWithdrawal = (rods.Length == 0
            ? ControlRoomValueSnapshot.Unavailable("%", ControlRoomInstrumentProvenance.Model)
            : Value(rods.Average(static rod => rod.PercentWithdrawn), "% withdrawn", "0.0")) with
        {
            InstrumentScale = new ControlRoomInstrumentScaleSnapshot(0d, 100d),
        };

        var rodTargets = reactorControl.Definition.ActuatorSystem.Actuators
            .Where(static actuator => actuator.TargetKind == NuclearReactorSimulator.Domain.Physics.Control.ActuatorTargetKind.ControlRod)
            .Select(actuator =>
            {
                var targetKind = actuator.RodTargetKind == NuclearReactorSimulator.Domain.Physics.Reactor.ControlRods.ControlRodCommandTargetKind.Rod
                    ? ControlRoomCommandTargetKind.ControlRod
                    : ControlRoomCommandTargetKind.ControlRodGroup;
                var effectiveMotion = ResolveRodTargetMotion(
                    reactorControl.Definition.ControlRods,
                    rods,
                    actuator.TargetId,
                    targetKind);

                return new ReactorRodTargetPresentationSnapshot(actuator.TargetId, targetKind)
                {
                    EffectiveMotion = effectiveMotion,
                };
            })
            .GroupBy(static target => (target.TargetKind, target.TargetId))
            .Select(static group => group.First())
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

        // M9.3 promotes the canonical M2.8 poison snapshot through the existing M5/M6 presentation boundary.
        // Application remains observational: no iodine/xenon integration or reactivity reconstruction occurs here.
        var xenon = reactorControl.CommittedIodineXenon is { } iodineXenon
            ? Value(iodineXenon.XenonReactivity.Pcm, "pcm", "0.0")
            : ControlRoomValueSnapshot.Unavailable("pcm");

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


    private static string ResolveRodTargetMotion(
        NuclearReactorSimulator.Domain.Physics.Reactor.ControlRods.ControlRodSystemDefinition definition,
        IReadOnlyList<ReactorRodPresentationSnapshot> rods,
        string targetId,
        ControlRoomCommandTargetKind targetKind)
    {
        IEnumerable<string> rodIds = targetKind == ControlRoomCommandTargetKind.ControlRod
            ? new[] { targetId }
            : definition.GetGroup(targetId).RodIds;

        var motions = rodIds
            .Select(rodId => rods.FirstOrDefault(rod => string.Equals(rod.RodId, rodId, StringComparison.Ordinal))?.Motion)
            .Where(static motion => !string.IsNullOrWhiteSpace(motion))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        return motions.Length switch
        {
            0 => "UNAVAILABLE",
            1 => motions[0]!,
            _ => "MIXED",
        };
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
                var measuredFlow = ProjectMeasuredChannelOrSource(
                    measuredFrame,
                    $"primary-display-loop-{loop.LoopId}-flow",
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
                        ProjectMeasuredChannelOrModel(
                            measuredFrame,
                            $"primary-display-pump-{pump.PumpId}-flow",
                            pump.MassFlowRate.KilogramsPerSecond,
                            "kg/s",
                            "0.0"),
                        Value(pump.ActivePressureBoost.Megapascals, "MPa", "0.000"),
                        commandablePumpIds.Contains(pump.PumpId)))
                    .ToArray();

                var branches = loop.Branches
                    .OrderBy(static branch => branch.FuelChannelGroupId, StringComparer.Ordinal)
                    .Select(branch =>
                    {
                        var channelFlow = ProjectMeasuredChannelOrModel(
                            measuredFrame,
                            $"primary-display-branch-{loop.LoopId}-{branch.FuelChannelGroupId}-channel-flow",
                            branch.ChannelMassFlowRate.KilogramsPerSecond,
                            "kg/s",
                            "0.0");
                        var returnFlow = ProjectMeasuredChannelOrModel(
                            measuredFrame,
                            $"primary-display-branch-{loop.LoopId}-{branch.FuelChannelGroupId}-return-flow",
                            branch.ReturnMassFlowRate.KilogramsPerSecond,
                            "kg/s",
                            "0.0");
                        var perChannel = channelFlow.NumericValue.HasValue
                            ? Value(channelFlow.NumericValue.Value / branch.RepresentedChannelCount, "kg/s/ch", "0.000")
                            : ControlRoomValueSnapshot.Unavailable("kg/s/ch", ControlRoomInstrumentProvenance.Measured);
                        var directionValue = channelFlow.NumericValue ?? branch.ChannelMassFlowRate.KilogramsPerSecond;

                        return new PrimaryCircuitBranchPresentationSnapshot(
                            branch.FuelChannelGroupId,
                            branch.RepresentedChannelCount,
                            channelFlow,
                            returnFlow,
                            perChannel,
                            Value(branch.ChannelPressureDifference.Megapascals, "MPa", "0.000"),
                            branch.OutletPhase.ToString().ToUpperInvariant(),
                            FlowDirection(directionValue),
                            branch.OutletVoidFraction.HasValue
                                ? $"Void {branch.OutletVoidFraction.Value.Percent:0.0}%"
                                : "Void —");
                    })
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
                WithScale(
                    ProjectMeasuredSource(
                        measuredFrame,
                        $"steam-drum/{drum.DrumId}/pressure",
                        "MPa",
                        1d / 1_000_000d,
                        "0.000"),
                    BuildSteamDrumPressureScale(snapshot, drum.DrumId)),
                WithScale(
                    ProjectMeasuredSource(
                        measuredFrame,
                        $"steam-drum/{drum.DrumId}/level",
                        "%",
                        100d,
                        "0.0"),
                    BuildSteamDrumLevelScale(snapshot, drum.DrumId)),
                Value(drum.Temperature.DegreesCelsius, "°C", "0.0"),
                ProjectMeasuredChannelOrModel(
                    measuredFrame,
                    $"primary-display-drum-{drum.DrumId}-inlet-flow",
                    drum.IncomingReturnMassFlowRate.KilogramsPerSecond,
                    "kg/s",
                    "0.0"),
                Value(drum.SeparatedSteamMassFlowRate.KilogramsPerSecond, "kg/s", "0.0"),
                ProjectMeasuredChannelOrModel(
                    measuredFrame,
                    $"primary-display-drum-{drum.DrumId}-recirculation-flow",
                    drum.RecirculatedLiquidMassFlowRate.KilogramsPerSecond,
                    "kg/s",
                    "0.0"),
                Value(drum.SeparableLiquidInventoryMass.Kilograms, "kg", "0.0"),
                Value(drum.SeparableLiquidInventoryMassFraction * 100d, "% mass", "0.0"),
                drum.WaterSteamSeparationUnavailable
                    ? "SEPARATION UNAVAILABLE · NO COMMITTED LIQUID"
                    : drum.LiquidRecirculationInventoryLimited
                        ? "LIQUID INVENTORY LIMITED"
                        : "LIQUID INVENTORY AVAILABLE",
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

    private static TurbineSecondaryPanelSnapshot ProjectTurbineSecondary(
        IntegratedAutomaticOperationSnapshot snapshot,
        TurbineSecondaryControlInputs? requestedSecondaryInputs)
    {
        var measuredFrame = snapshot.NextMeasuredSignals;
        var protectedControl = snapshot.Control.ProtectedControl;
        var integrated = protectedControl.FullPlant.IntegratedCycle;
        var turbine = integrated.TurbineExpansion;
        var mainSteam = turbine.MainSteamNetwork;
        var condenserSystem = integrated.Condenser;
        var feedwaterSystem = integrated.CondensateFeedwater;
        var secondaryControl = protectedControl.TurbineSecondary;
        var protection = protectedControl.Protection;
        var arbitration = protectedControl.Arbitration;

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
            .Select(train =>
            {
                var controlActuator = secondaryControl.Definition.ActuatorSystem.Actuators.Single(item =>
                    item.TargetKind == NuclearReactorSimulator.Domain.Physics.Control.ActuatorTargetKind.Valve
                    && string.Equals(item.TargetId, train.ControlValve.ValveId, StringComparison.Ordinal));
                var controlInput = requestedSecondaryInputs?.Controllers.Controllers.FirstOrDefault(item =>
                    string.Equals(item.ControllerId, controlActuator.ControllerId, StringComparison.Ordinal));
                var controlCommand = secondaryControl.ControlAndActuator.ActuatorCommands.ValveCommands.FirstOrDefault(item =>
                    string.Equals(item.ValveId, train.ControlValve.ValveId, StringComparison.Ordinal));
                var admissionCommand = secondaryControl.ControlAndActuator.ActuatorCommands.ValveCommands.FirstOrDefault(item =>
                    string.Equals(item.ValveId, train.AdmissionValve.ValveId, StringComparison.Ordinal));
                var stopCommand = requestedSecondaryInputs?.IsolationValveCommands.FirstOrDefault(item =>
                    string.Equals(item.ValveId, train.StopValve.ValveId, StringComparison.Ordinal));
                var manualDemandPercent = controlInput is null
                    ? train.ControlValve.EffectivePosition.Percent
                    : ((controlInput.ManualOutput - controlActuator.InputRange.Minimum)
                        / controlActuator.InputRange.Span) * 100d;

                return new TurbineAdmissionTrainPresentationSnapshot(
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
                    train.TurbineInletPhase.ToString().ToUpperInvariant())
                {
                    StopValveRequestedPosition = Value(
                        (stopCommand?.RequestedPosition ?? train.StopValve.EffectivePosition).Percent,
                        "% open",
                        "0.0"),
                    ControlValveRequestedPosition = Value(
                        (controlCommand?.RequestedPosition ?? train.ControlValve.EffectivePosition).Percent,
                        "% open",
                        "0.0"),
                    ControlValveManualDemand = Value(
                        Math.Clamp(manualDemandPercent, 0d, 100d),
                        "% open",
                        "0.0"),
                    AdmissionValveRequestedPosition = Value(
                        (admissionCommand?.RequestedPosition ?? train.AdmissionValve.EffectivePosition).Percent,
                        "% open",
                        "0.0"),
                    ControlValveManualMode = controlInput?.Mode
                        == NuclearReactorSimulator.Domain.Physics.Control.ControllerMode.Manual,
                    TurbineAdmissionOpeningInhibited = protection.TurbineAdmissionOpeningInhibited,
                    StopValveForcedClosed = arbitration.StopValvesForcedClosed.Contains(
                        train.StopValve.ValveId,
                        StringComparer.Ordinal),
                };
            })
            .ToArray();

        var rotors = turbine.Rotors
            .OrderBy(static rotor => rotor.RotorId, StringComparer.Ordinal)
            .Select(rotor => new TurbineRotorPresentationSnapshot(
                rotor.RotorId,
                WithScale(
                    ProjectMeasuredSource(
                        measuredFrame,
                        $"turbine-rotor/{rotor.RotorId}/speed",
                        "rpm",
                        1d,
                        "0.0"),
                    BuildTurbineSpeedScale(snapshot, rotor.RotorId)),
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
                Value(stage.EffectiveIdealSpecificWork.KilojoulesPerKilogram, "kJ/kg", "0.0"),
                Value(stage.ExtractedSpecificWork.KilojoulesPerKilogram, "kJ/kg", "0.0"),
                stage.ThermodynamicWorkModelActive,
                stage.ThermodynamicWorkLimited,
                stage.TripBlocked))
            .ToArray();

        var condensers = condenserSystem.Condensers
            .OrderBy(static condenser => condenser.CondenserId, StringComparer.Ordinal)
            .Select(condenser =>
            {
                var coolingBoundary = condenserSystem.GetCoolingBoundary(condenser.CoolingBoundaryId);
                return new CondenserPresentationSnapshot(
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
                condenser.FinalSteamSpacePhase.ToString().ToUpperInvariant(),
                Value(condenser.CondensateSpecificInternalEnergy.KilojoulesPerKilogram, "kJ/kg", "0.0"),
                Value(condenser.SpecificCondensationEnergyDrop.KilojoulesPerKilogram, "kJ/kg", "0.0"),
                condenser.ActiveCondensationLimits,
                Value(coolingBoundary.InstalledHeatRejectionCapacity.Megawatts, "MW", "0.0"),
                Value(coolingBoundary.AvailableHeatRejectionPower.Megawatts, "MW", "0.0"),
                Value(coolingBoundary.SurfaceHeatTransferLimitedPower.Megawatts, "MW", "0.0"),
                coolingBoundary.ActiveHeatRejectionLimits);
            })
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

        var legacyBoundarySteamFlow = Value(
            mainSteam.TotalTurbineAdmissionMassFlowRate.KilogramsPerSecond,
            "kg/s",
            "0.0");
        var effectiveTurbineSteamFlow = Value(
            turbine.StageGroups.Sum(static stage => stage.EffectiveMassFlowRate.KilogramsPerSecond),
            "kg/s",
            "0.0");

        return new TurbineSecondaryPanelSnapshot(
            steamLines,
            admissionTrains,
            rotors,
            stageGroups,
            condensers,
            feedwaterTrains,
            legacyBoundarySteamFlow,
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
            protectedControl.Protection.TurbineTripActive)
        {
            EffectiveTurbineSteamFlow = effectiveTurbineSteamFlow,
        };
    }

    private static ElectricalPanelSnapshot ProjectElectrical(IntegratedAutomaticOperationSnapshot snapshot)
    {
        var measuredFrame = snapshot.NextMeasuredSignals;
        var protectedControl = snapshot.Control.ProtectedControl;
        var generatorGrid = protectedControl.FullPlant.IntegratedCycle.GeneratorGrid;
        var grid = generatorGrid.Grid;

        var generators = generatorGrid.Generators
            .OrderBy(static generator => generator.GeneratorId, StringComparer.Ordinal)
            .Select(generator =>
            {
                var definition = generatorGrid.Definition.GetGenerator(generator.GeneratorId);
                return new GeneratorPresentationSnapshot(
                    generator.GeneratorId,
                    generator.RotorId,
                    generator.BreakerId,
                    WithScale(
                        ProjectMeasuredSource(
                            measuredFrame,
                            $"generator/{generator.GeneratorId}/frequency",
                            "Hz",
                            1d,
                            "0.000"),
                        BuildGeneratorFrequencyScale(grid.Frequency.Hertz, definition.MaximumSynchronizationFrequencyDifference.Hertz)),
                    WithScale(
                        ProjectMeasuredSource(
                            measuredFrame,
                            $"generator/{generator.GeneratorId}/electrical-output",
                            "MWe",
                            1d / 1_000_000d,
                            "0.0"),
                        new ControlRoomInstrumentScaleSnapshot(0d, definition.MaximumElectricalPower.Megawatts)),
                    WithScale(
                        Value(generator.TerminalLineVoltage.Kilovolts, "kV", "0.0"),
                        BuildGeneratorVoltageScale(generator.GridLineVoltage.Kilovolts, definition.MaximumSynchronizationVoltageDifference.Kilovolts)),
                    Value(generator.GridLineVoltage.Kilovolts, "kV", "0.0"),
                    WithScale(
                        Value(generator.FinalPhaseDifference.Degrees, "°", "0.00"),
                        BuildGeneratorPhaseScale(definition.MaximumSynchronizationPhaseDifference.Degrees)),
                    Value(generator.MechanicalInputPower.Megawatts, "MW", "0.0"),
                    Value(generator.ConversionLossPower.Megawatts, "MW", "0.000"),
                    generator.SynchronizationConditionsSatisfied,
                    generator.BreakerFinallyClosed,
                    generator.CloseCommandAccepted,
                    generator.CloseCommandRejected,
                    generator.FrequencyDifferenceAtCloseCheck.Hertz,
                    definition.MaximumSynchronizationFrequencyDifference.Hertz,
                    generator.InitialPhaseDifference.Degrees,
                    definition.MaximumSynchronizationPhaseDifference.Degrees,
                    generator.VoltageDifferenceAtCloseCheck.Kilovolts,
                    definition.MaximumSynchronizationVoltageDifference.Kilovolts)
                {
                    RequestedElectricalPower = Value(generator.RequestedElectricalPower.Megawatts, "MWe", "0.0"),
                };
            })
            .ToArray();

        return new ElectricalPanelSnapshot(
            new ElectricalGridPresentationSnapshot(
                grid.GridId,
                Value(grid.Frequency.Hertz, "Hz", "0.000"),
                Value(grid.LineVoltage.Kilovolts, "kV", "0.0"),
                Value(grid.FinalPhaseAngle.Degrees, "°", "0.00")),
            generators,
            WithScale(
                ProjectMeasuredSource(
                    measuredFrame,
                    "plant/generator/gross-electrical-output",
                    "MWe",
                    1d / 1_000_000d,
                    "0.0"),
                new ControlRoomInstrumentScaleSnapshot(
                    0d,
                    Math.Max(1d, generatorGrid.Definition.Generators.Sum(static definition => definition.MaximumElectricalPower.Megawatts)))),
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

    private static ControlRoomValueSnapshot ProjectMeasuredChannelOrSource(
        MeasuredSignalFrame frame,
        string preferredChannelId,
        string fallbackSourceId,
        string displayUnit,
        double engineeringScale,
        string format)
    {
        var preferred = ProjectMeasuredChannel(frame, preferredChannelId, displayUnit, engineeringScale, format);
        return preferred.NumericValue.HasValue
            ? preferred
            : ProjectMeasuredSource(frame, fallbackSourceId, displayUnit, engineeringScale, format);
    }

    private static ControlRoomValueSnapshot ProjectMeasuredChannelOrModel(
        MeasuredSignalFrame frame,
        string channelId,
        double modelValue,
        string displayUnit,
        string format)
    {
        var measured = ProjectMeasuredChannel(frame, channelId, displayUnit, 1d, format);
        return measured.NumericValue.HasValue ? measured : Value(modelValue, displayUnit, format);
    }

    private static ControlRoomValueSnapshot ProjectMeasuredChannel(
        MeasuredSignalFrame frame,
        string channelId,
        string displayUnit,
        double engineeringScale,
        string format)
    {
        var channel = frame.Definition.Channels.FirstOrDefault(item => string.Equals(item.Id, channelId, StringComparison.Ordinal));
        if (channel is null)
        {
            return ControlRoomValueSnapshot.Unavailable(displayUnit, ControlRoomInstrumentProvenance.Measured);
        }

        var signal = frame.GetSignal(channel.Id);
        if (signal.Validity != SignalValidity.Valid
            || !signal.EngineeringValue.HasValue
            || !double.IsFinite(signal.EngineeringValue.Value)
            || signal.Quality is SignalQuality.Bad or SignalQuality.Unavailable)
        {
            return ControlRoomValueSnapshot.Unavailable(displayUnit, ControlRoomInstrumentProvenance.Measured);
        }

        var scaled = signal.EngineeringValue.Value * engineeringScale;
        var state = signal.Quality == SignalQuality.Suspect || signal.OutOfMeasurementRange
            ? ControlRoomVisualState.Warning
            : ControlRoomVisualState.Normal;

        return new ControlRoomValueSnapshot(
            scaled.ToString(format, CultureInfo.InvariantCulture),
            displayUnit,
            scaled,
            state)
        {
            Provenance = ControlRoomInstrumentProvenance.Measured,
            Quality = signal.Quality == SignalQuality.Suspect || signal.OutOfMeasurementRange
                ? ControlRoomInstrumentQuality.Suspect
                : ControlRoomInstrumentQuality.Good,
        };
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
            return ControlRoomValueSnapshot.Unavailable(displayUnit, ControlRoomInstrumentProvenance.Measured);
        }

        var signal = frame.GetSignal(channel.Id);
        if (signal.Validity != SignalValidity.Valid
            || !signal.EngineeringValue.HasValue
            || !double.IsFinite(signal.EngineeringValue.Value)
            || signal.Quality is SignalQuality.Bad or SignalQuality.Unavailable)
        {
            return ControlRoomValueSnapshot.Unavailable(displayUnit, ControlRoomInstrumentProvenance.Measured);
        }

        var scaled = signal.EngineeringValue.Value * engineeringScale;
        var state = signal.Quality == SignalQuality.Suspect || signal.OutOfMeasurementRange
            ? ControlRoomVisualState.Warning
            : ControlRoomVisualState.Normal;

        return new ControlRoomValueSnapshot(
            scaled.ToString(format, CultureInfo.InvariantCulture),
            displayUnit,
            scaled,
            state)
        {
            Provenance = ControlRoomInstrumentProvenance.Measured,
            Quality = signal.Quality == SignalQuality.Suspect || signal.OutOfMeasurementRange
                ? ControlRoomInstrumentQuality.Suspect
                : ControlRoomInstrumentQuality.Good,
        };
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
            ControlRoomVisualState.Normal)
        {
            Provenance = ControlRoomInstrumentProvenance.Model,
            Quality = ControlRoomInstrumentQuality.Good,
        };
    }

    private static ControlRoomValueSnapshot WithScale(
        ControlRoomValueSnapshot value,
        ControlRoomInstrumentScaleSnapshot scale)
        => value with { InstrumentScale = scale };

    private static double? WithinScale(double? value, double minimum, double maximum)
        => value.HasValue && double.IsFinite(value.Value) && value.Value >= minimum && value.Value <= maximum
            ? value.Value
            : null;

    private static ControlRoomInstrumentScaleSnapshot BuildSteamDrumLevelScale(
        IntegratedAutomaticOperationSnapshot snapshot,
        string drumId)
    {
        var protectedControl = snapshot.Control.ProtectedControl;
        var instrumentation = protectedControl.Protection.Definition.Instrumentation;
        var sourceId = $"steam-drum/{drumId}/level";

        var alarmThresholds = snapshot.Control.Alarms.Definition.Alarms
            .Select(static alarm => (Alarm: alarm, Condition: alarm.Condition as MeasuredAlarmConditionDefinition))
            .Where(static pair => pair.Condition is not null)
            .Where(pair => string.Equals(
                instrumentation.GetChannel(pair.Condition!.MeasurementChannelId).SourceId,
                sourceId,
                StringComparison.Ordinal))
            .Select(pair => (
                Threshold: pair.Condition!.Threshold * 100d,
                Comparison: pair.Condition.Comparison,
                Severity: pair.Alarm.Severity,
                Title: pair.Alarm.Title))
            .ToArray();

        var protectionThresholds = protectedControl.Protection.Definition.TripFunctions
            .Where(function => string.Equals(
                instrumentation.GetChannel(function.MeasurementChannelId).SourceId,
                sourceId,
                StringComparison.Ordinal))
            .Select(function => (
                Threshold: function.TripThreshold * 100d,
                Comparison: function.Comparison,
                Label: function.Id))
            .ToArray();

        var bands = new List<ControlRoomInstrumentBandSnapshot>();
        foreach (var alarm in alarmThresholds)
        {
            if (alarm.Comparison == AlarmComparison.Low && alarm.Threshold > 0d)
            {
                var nextLimit = protectionThresholds
                    .Where(item => item.Comparison == NuclearReactorSimulator.Domain.Physics.Control.Protection.ProtectionComparison.Low
                        && item.Threshold < alarm.Threshold)
                    .Select(static item => item.Threshold)
                    .DefaultIfEmpty(0d)
                    .Max();
                if (alarm.Threshold > nextLimit)
                {
                    bands.Add(new ControlRoomInstrumentBandSnapshot(
                        nextLimit,
                        Math.Min(100d, alarm.Threshold),
                        alarm.Severity == AlarmSeverity.Trip
                            ? ControlRoomInstrumentBandKind.Alarm
                            : ControlRoomInstrumentBandKind.Warning,
                        alarm.Title.ToUpperInvariant()));
                }
            }
            else if (alarm.Comparison == AlarmComparison.High && alarm.Threshold < 100d)
            {
                var nextLimit = protectionThresholds
                    .Where(item => item.Comparison == NuclearReactorSimulator.Domain.Physics.Control.Protection.ProtectionComparison.High
                        && item.Threshold > alarm.Threshold)
                    .Select(static item => item.Threshold)
                    .DefaultIfEmpty(100d)
                    .Min();
                if (nextLimit > alarm.Threshold)
                {
                    bands.Add(new ControlRoomInstrumentBandSnapshot(
                        Math.Max(0d, alarm.Threshold),
                        Math.Min(100d, nextLimit),
                        alarm.Severity == AlarmSeverity.Trip
                            ? ControlRoomInstrumentBandKind.Alarm
                            : ControlRoomInstrumentBandKind.Warning,
                        alarm.Title.ToUpperInvariant()));
                }
            }
        }

        var limits = protectionThresholds.Select(item => new ControlRoomProtectionLimitSnapshot(
            item.Threshold,
            item.Comparison == NuclearReactorSimulator.Domain.Physics.Control.Protection.ProtectionComparison.High
                ? ControlRoomLimitDirection.High
                : ControlRoomLimitDirection.Low,
            item.Label.ToUpperInvariant())).ToArray();

        var levelLoop = protectedControl.TurbineSecondary.Loops
            .FirstOrDefault(static loop => loop.Kind == NuclearReactorSimulator.Domain.Physics.Control.TurbineSecondary.TurbineSecondaryControlLoopKind.SteamDrumLevelFeedwater);
        double? setpointPercent = levelLoop is null ? null : levelLoop.Setpoint * 100d;
        return new ControlRoomInstrumentScaleSnapshot(
            0d,
            100d,
            bands,
            setpoint: WithinScale(setpointPercent, 0d, 100d),
            protectionLimits: limits);
    }

    private static ControlRoomInstrumentScaleSnapshot BuildTurbineSpeedScale(
        IntegratedAutomaticOperationSnapshot snapshot,
        string rotorId)
    {
        var turbine = snapshot.Control.ProtectedControl.FullPlant.IntegratedCycle.TurbineExpansion;
        var definition = turbine.Definition.GetRotor(rotorId);
        var overspeedRpm = definition.OverspeedThreshold.RevolutionsPerMinute;
        var maximum = Math.Max(overspeedRpm * 1.05d, 1d);
        var speedLoop = snapshot.Control.ProtectedControl.TurbineSecondary.Loops
            .FirstOrDefault(static loop => loop.Kind == NuclearReactorSimulator.Domain.Physics.Control.TurbineSecondary.TurbineSecondaryControlLoopKind.TurbineSpeedAdmission);
        var setpoint = WithinScale(speedLoop?.Setpoint, 0d, maximum);

        return new ControlRoomInstrumentScaleSnapshot(
            0d,
            maximum,
            setpoint: setpoint,
            protectionLimits: new[]
            {
                new ControlRoomProtectionLimitSnapshot(
                    overspeedRpm,
                    ControlRoomLimitDirection.High,
                    "OVERSPEED TRIP"),
            });
    }

    private static ControlRoomInstrumentScaleSnapshot BuildGeneratorFrequencyScale(
        double gridFrequencyHertz,
        double synchronizationToleranceHertz)
    {
        var maximum = Math.Max(gridFrequencyHertz * 1.1d, gridFrequencyHertz + synchronizationToleranceHertz + 1d);
        return new ControlRoomInstrumentScaleSnapshot(
            0d,
            maximum,
            targetBand: new ControlRoomTargetBandSnapshot(
                Math.Max(0d, gridFrequencyHertz - synchronizationToleranceHertz),
                Math.Min(maximum, gridFrequencyHertz + synchronizationToleranceHertz),
                "SYNC WINDOW"),
            setpoint: WithinScale(gridFrequencyHertz, 0d, maximum));
    }

    private static ControlRoomInstrumentScaleSnapshot BuildGeneratorPhaseScale(double synchronizationToleranceDegrees)
    {
        var extent = Math.Max(180d, Math.Abs(synchronizationToleranceDegrees) * 1.1d);
        var tolerance = Math.Min(extent, Math.Abs(synchronizationToleranceDegrees));
        return new ControlRoomInstrumentScaleSnapshot(
            -extent,
            extent,
            targetBand: new ControlRoomTargetBandSnapshot(-tolerance, tolerance, "SYNC WINDOW"),
            setpoint: 0d);
    }

    private static ControlRoomInstrumentScaleSnapshot BuildGeneratorVoltageScale(
        double gridVoltageKilovolts,
        double synchronizationToleranceKilovolts)
    {
        var maximum = Math.Max(gridVoltageKilovolts * 1.2d, gridVoltageKilovolts + synchronizationToleranceKilovolts + 1d);
        return new ControlRoomInstrumentScaleSnapshot(
            0d,
            maximum,
            targetBand: new ControlRoomTargetBandSnapshot(
                Math.Max(0d, gridVoltageKilovolts - synchronizationToleranceKilovolts),
                Math.Min(maximum, gridVoltageKilovolts + synchronizationToleranceKilovolts),
                "SYNC WINDOW"),
            setpoint: WithinScale(gridVoltageKilovolts, 0d, maximum));
    }

    private static ControlRoomInstrumentScaleSnapshot BuildSteamDrumPressureScale(
        IntegratedAutomaticOperationSnapshot snapshot,
        string drumId)
    {
        var protectedControl = snapshot.Control.ProtectedControl;
        var instrumentation = protectedControl.Protection.Definition.Instrumentation;
        var sourceId = $"steam-drum/{drumId}/pressure";

        var alarmThresholds = snapshot.Control.Alarms.Definition.Alarms
            .Select(static alarm => (Alarm: alarm, Condition: alarm.Condition as MeasuredAlarmConditionDefinition))
            .Where(static pair => pair.Condition is not null)
            .Where(pair => string.Equals(
                instrumentation.GetChannel(pair.Condition!.MeasurementChannelId).SourceId,
                sourceId,
                StringComparison.Ordinal))
            .Select(pair => (
                Threshold: pair.Condition!.Threshold / 1_000_000d,
                Comparison: pair.Condition.Comparison,
                Severity: pair.Alarm.Severity,
                Title: pair.Alarm.Title))
            .ToArray();

        var protectionThresholds = protectedControl.Protection.Definition.TripFunctions
            .Where(function => string.Equals(
                instrumentation.GetChannel(function.MeasurementChannelId).SourceId,
                sourceId,
                StringComparison.Ordinal))
            .Select(function => (
                Threshold: function.TripThreshold / 1_000_000d,
                Comparison: function.Comparison,
                Label: function.Id))
            .ToArray();

        var highestThreshold = alarmThresholds.Select(static item => item.Threshold)
            .Concat(protectionThresholds.Select(static item => item.Threshold))
            .DefaultIfEmpty(10d)
            .Max();
        var maximum = Math.Max(1d, highestThreshold * 1.1d);

        var bands = new List<ControlRoomInstrumentBandSnapshot>();
        foreach (var alarm in alarmThresholds)
        {
            if (alarm.Comparison == AlarmComparison.High)
            {
                var nextLimit = protectionThresholds
                    .Where(item => item.Comparison == NuclearReactorSimulator.Domain.Physics.Control.Protection.ProtectionComparison.High && item.Threshold > alarm.Threshold)
                    .Select(static item => item.Threshold)
                    .DefaultIfEmpty(maximum)
                    .Min();
                if (nextLimit > alarm.Threshold)
                {
                    bands.Add(new ControlRoomInstrumentBandSnapshot(
                        alarm.Threshold,
                        nextLimit,
                        alarm.Severity == AlarmSeverity.Trip ? ControlRoomInstrumentBandKind.Alarm : ControlRoomInstrumentBandKind.Warning,
                        alarm.Title.ToUpperInvariant()));
                }
            }
            else if (alarm.Threshold > 0d)
            {
                bands.Add(new ControlRoomInstrumentBandSnapshot(
                    0d,
                    Math.Min(maximum, alarm.Threshold),
                    alarm.Severity == AlarmSeverity.Trip ? ControlRoomInstrumentBandKind.Alarm : ControlRoomInstrumentBandKind.Warning,
                    alarm.Title.ToUpperInvariant()));
            }
        }

        var limits = protectionThresholds.Select(item => new ControlRoomProtectionLimitSnapshot(
            item.Threshold,
            item.Comparison == NuclearReactorSimulator.Domain.Physics.Control.Protection.ProtectionComparison.High
                ? ControlRoomLimitDirection.High
                : ControlRoomLimitDirection.Low,
            item.Label.ToUpperInvariant())).ToArray();

        var pressureLoop = protectedControl.TurbineSecondary.Loops
            .FirstOrDefault(static loop => loop.Kind == NuclearReactorSimulator.Domain.Physics.Control.TurbineSecondary.TurbineSecondaryControlLoopKind.SteamPressureAdmission);
        double? setpointMegapascals = pressureLoop is null ? null : pressureLoop.Setpoint / 1_000_000d;

        return new ControlRoomInstrumentScaleSnapshot(
            0d,
            maximum,
            bands,
            setpoint: WithinScale(setpointMegapascals, 0d, maximum),
            protectionLimits: limits);
    }
}
