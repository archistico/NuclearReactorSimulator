using System.Collections.ObjectModel;
using NuclearReactorSimulator.Domain.Physics.Control;
using NuclearReactorSimulator.Domain.Physics.TurbineIsland.Feedwater;
using NuclearReactorSimulator.Domain.Physics.TurbineIsland.Integration;
using NuclearReactorSimulator.Domain.Physics.TurbineIsland.MainSteam;

namespace NuclearReactorSimulator.Domain.Physics.Control.TurbineSecondary;

/// <summary>
/// Canonical M5.4 semantic composition of measured-signal controllers with existing turbine admission valves and
/// condensate/feedwater pumps. Physical state remains owned by the existing plant/turbine/electrical domains.
/// </summary>
public sealed class TurbineSecondaryControlSystemDefinition
{
    public TurbineSecondaryControlSystemDefinition(
        string id,
        IntegratedSecondaryCycleDefinition plantDefinition,
        ActuatorSystemDefinition actuatorSystem,
        IEnumerable<TurbineSecondaryControlLoopDefinition> loops)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Turbine/secondary control-system id cannot be empty or whitespace.", nameof(id));
        }

        PlantDefinition = plantDefinition ?? throw new ArgumentNullException(nameof(plantDefinition));
        ActuatorSystem = actuatorSystem ?? throw new ArgumentNullException(nameof(actuatorSystem));
        ArgumentNullException.ThrowIfNull(loops);

        var canonical = loops
            .Select(item => item ?? throw new ArgumentException("Turbine/secondary control-loop definitions cannot contain null entries.", nameof(loops)))
            .OrderBy(static item => item.Id, StringComparer.Ordinal)
            .ToArray();
        if (canonical.Length == 0)
        {
            throw new ArgumentException("A turbine/secondary control system must contain at least one loop.", nameof(loops));
        }

        EnsureUnique(canonical.Select(static item => item.Id), "loop id", nameof(loops));
        EnsureUnique(canonical.Select(static item => item.ControllerId), "controller assignment", nameof(loops));
        EnsureUnique(canonical.Select(static item => item.ActuatorId), "actuator assignment", nameof(loops));

        var expectedControllerIds = actuatorSystem.ControlSystem.Controllers.Select(static item => item.Id).OrderBy(static idValue => idValue, StringComparer.Ordinal).ToArray();
        var loopControllerIds = canonical.Select(static item => item.ControllerId).OrderBy(static idValue => idValue, StringComparer.Ordinal).ToArray();
        var expectedActuatorIds = actuatorSystem.Actuators.Select(static item => item.Id).OrderBy(static idValue => idValue, StringComparer.Ordinal).ToArray();
        var loopActuatorIds = canonical.Select(static item => item.ActuatorId).OrderBy(static idValue => idValue, StringComparer.Ordinal).ToArray();
        if (!expectedControllerIds.SequenceEqual(loopControllerIds, StringComparer.Ordinal)
            || !expectedActuatorIds.SequenceEqual(loopActuatorIds, StringComparer.Ordinal))
        {
            throw new ArgumentException("M5.4 loop definitions must cover every controller and actuator exactly once.", nameof(loops));
        }

        foreach (var loop in canonical)
        {
            var controller = actuatorSystem.ControlSystem.GetController(loop.ControllerId);
            var actuator = actuatorSystem.GetActuator(loop.ActuatorId);
            if (!string.Equals(actuator.ControllerId, controller.Id, StringComparison.Ordinal))
            {
                throw new ArgumentException($"Loop '{loop.Id}' controller '{controller.Id}' does not own actuator '{actuator.Id}'.", nameof(loops));
            }

            ValidateLoop(loop, controller, actuator);
        }

        Id = id.Trim();
        Loops = new ReadOnlyCollection<TurbineSecondaryControlLoopDefinition>(canonical);
    }

    public string Id { get; }
    public IntegratedSecondaryCycleDefinition PlantDefinition { get; }
    public ActuatorSystemDefinition ActuatorSystem { get; }
    public IReadOnlyList<TurbineSecondaryControlLoopDefinition> Loops { get; }

    public TurbineSecondaryControlLoopDefinition GetLoop(string id)
        => Loops.FirstOrDefault(item => string.Equals(item.Id, id, StringComparison.Ordinal))
            ?? throw new KeyNotFoundException($"Unknown turbine/secondary control loop '{id}'.");

    private void ValidateLoop(
        TurbineSecondaryControlLoopDefinition loop,
        PidControllerDefinition controller,
        ActuatorDefinition actuator)
    {
        var channel = ActuatorSystem.ControlSystem.Instrumentation.GetChannel(controller.MeasurementChannelId);
        switch (loop.Kind)
        {
            case TurbineSecondaryControlLoopKind.TurbineSpeedAdmission:
                var speedTrain = GetAdmissionTrainForNormalValve(actuator, loop);
                var speedRotorId = GetRotorIdForAdmissionTrain(speedTrain.Id);
                RequireSource(loop, channel.SourceId, $"turbine-rotor/{speedRotorId}/speed");
                break;
            case TurbineSecondaryControlLoopKind.TurbineLoadAdmission:
                var loadTrain = GetAdmissionTrainForNormalValve(actuator, loop);
                var loadRotorId = GetRotorIdForAdmissionTrain(loadTrain.Id);
                var generator = PlantDefinition.GeneratorGridSystem.GetGeneratorForRotor(loadRotorId);
                RequireSource(loop, channel.SourceId, $"generator/{generator.Id}/electrical-output");
                break;
            case TurbineSecondaryControlLoopKind.SteamPressureAdmission:
                var pressureTrain = GetAdmissionTrainForNormalValve(actuator, loop);
                var expectedPressureSources = PlantDefinition.TurbineExpansionSystem.MainSteamNetwork.SteamLines
                    .Where(item => string.Equals(item.HeaderNodeId, pressureTrain.HeaderNodeId, StringComparison.Ordinal))
                    .Select(item => PlantDefinition.PrimaryCircuit.BoundarySystem.GetSteamExportBoundary(item.SteamExportBoundaryId))
                    .Select(static export => $"steam-drum/{export.SteamDrumId}/pressure")
                    .ToHashSet(StringComparer.Ordinal);
                if (!expectedPressureSources.Contains(channel.SourceId))
                {
                    throw new ArgumentException(
                        $"Loop '{loop.Id}' must consume pressure from a steam drum feeding header '{pressureTrain.HeaderNodeId}'.",
                        nameof(loop));
                }
                break;
            case TurbineSecondaryControlLoopKind.SteamDrumLevelFeedwater:
                if (actuator.TargetKind != ActuatorTargetKind.Pump)
                {
                    throw new ArgumentException($"Drum-level loop '{loop.Id}' must target a feedwater pump.", nameof(loop));
                }
                var feedwaterTrain = FindTrainByFeedwaterPump(actuator.TargetId, loop);
                var feedwaterBoundary = PlantDefinition.PrimaryCircuit.BoundarySystem.GetFeedwaterBoundary(feedwaterTrain.FeedwaterBoundaryId);
                RequireSource(loop, channel.SourceId, $"steam-drum/{feedwaterBoundary.SteamDrumId}/level");
                break;
            case TurbineSecondaryControlLoopKind.HotwellInventoryCondensate:
                if (actuator.TargetKind != ActuatorTargetKind.Pump)
                {
                    throw new ArgumentException($"Hotwell-inventory loop '{loop.Id}' must target a condensate pump.", nameof(loop));
                }
                var condensateTrain = FindTrainByCondensatePump(actuator.TargetId, loop);
                RequireSource(loop, channel.SourceId, $"condenser/{condensateTrain.CondenserId}/hotwell-mass");
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(loop), loop.Kind, "Unknown turbine/secondary control-loop kind.");
        }
    }

    private TurbineAdmissionTrainDefinition GetAdmissionTrainForNormalValve(ActuatorDefinition actuator, TurbineSecondaryControlLoopDefinition loop)
    {
        if (actuator.TargetKind != ActuatorTargetKind.Valve)
        {
            throw new ArgumentException($"Admission loop '{loop.Id}' must target a valve.", nameof(loop));
        }

        var train = PlantDefinition.TurbineExpansionSystem.MainSteamNetwork.AdmissionTrains.SingleOrDefault(item =>
            string.Equals(item.ControlValveId, actuator.TargetId, StringComparison.Ordinal)
            || string.Equals(item.AdmissionValveId, actuator.TargetId, StringComparison.Ordinal));
        return train ?? throw new ArgumentException(
            $"Admission-loop actuator '{actuator.Id}' must target a canonical control/admission valve; stop valves remain reserved for isolation/trip logic.",
            nameof(loop));
    }

    private string GetRotorIdForAdmissionTrain(string trainId)
    {
        var boundary = PlantDefinition.TurbineExpansionSystem.MainSteamNetwork.TurbineAdmissionBoundaries
            .Single(item => string.Equals(item.AdmissionTrainId, trainId, StringComparison.Ordinal));
        return PlantDefinition.TurbineExpansionSystem.StageGroups
            .Single(item => string.Equals(item.AdmissionBoundaryId, boundary.Id, StringComparison.Ordinal))
            .RotorId;
    }

    private CondensateFeedwaterTrainDefinition FindTrainByFeedwaterPump(string pumpId, TurbineSecondaryControlLoopDefinition loop)
        => PlantDefinition.CondensateFeedwaterSystem.Trains.SingleOrDefault(item => string.Equals(item.FeedwaterPumpId, pumpId, StringComparison.Ordinal))
            ?? throw new ArgumentException($"Loop '{loop.Id}' target '{pumpId}' is not a canonical M4.4 feedwater pump.", nameof(loop));

    private CondensateFeedwaterTrainDefinition FindTrainByCondensatePump(string pumpId, TurbineSecondaryControlLoopDefinition loop)
        => PlantDefinition.CondensateFeedwaterSystem.Trains.SingleOrDefault(item => string.Equals(item.CondensatePumpId, pumpId, StringComparison.Ordinal))
            ?? throw new ArgumentException($"Loop '{loop.Id}' target '{pumpId}' is not a canonical M4.4 condensate pump.", nameof(loop));

    private static void RequireSource(TurbineSecondaryControlLoopDefinition loop, string actualSourceId, string expectedSourceId)
    {
        if (!string.Equals(actualSourceId, expectedSourceId, StringComparison.Ordinal))
        {
            throw new ArgumentException($"Loop '{loop.Id}' must consume source '{expectedSourceId}', not '{actualSourceId}'.", nameof(loop));
        }
    }

    private static void EnsureUnique(IEnumerable<string> values, string label, string parameterName)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var value in values)
        {
            if (!seen.Add(value))
            {
                throw new ArgumentException($"Duplicate turbine/secondary {label} '{value}'.", parameterName);
            }
        }
    }
}
