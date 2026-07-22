using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.Reactor.Core;
using NuclearReactorSimulator.Domain.Physics.Reactor.Core.Spatial;
using NuclearReactorSimulator.Domain.Plant;
using NuclearReactorSimulator.Simulation.Physics.Fluids;
using NuclearReactorSimulator.Simulation.Physics.Reactor.Feedback;

namespace NuclearReactorSimulator.Simulation.Physics.Reactor.Core.Spatial;

/// <summary>
/// M9.4 deterministic quasi-spatial refinement. It evaluates validated linear M2 temperature/void feedback formulas on
/// committed M3.3 zone domains, reduces them to one power-weighted global feedback contribution, and evolves only normalized
/// zone power shares. Global point kinetics remains authoritative and no local neutron population is introduced.
/// </summary>
public sealed class QuasiSpatialCoreFeedbackSolver
{
    private const double FractionSumTolerance = 1e-12d;
    private const double MaximumExponentMagnitude = 50d;

    private readonly QuasiSpatialCoreFeedbackDefinition _definition;
    private readonly TemperatureFeedbackSolver _temperatureSolver = new();
    private readonly VoidFeedbackSolver _voidSolver = new();
    private readonly WaterSteamVoidFractionSolver _voidFractionSolver = new(new SimplifiedWaterSteamThermodynamicModel());

    public QuasiSpatialCoreFeedbackSolver(QuasiSpatialCoreFeedbackDefinition definition)
    {
        _definition = definition ?? throw new ArgumentNullException(nameof(definition));
    }

    public QuasiSpatialCoreFeedbackDefinition Definition => _definition;

    public QuasiSpatialCoreFeedbackStepResult Step(
        AggregatedCoreState committedCoreState,
        PlantState committedPlantState,
        TimeSpan deltaTime)
    {
        ArgumentNullException.ThrowIfNull(committedCoreState);
        ArgumentNullException.ThrowIfNull(committedPlantState);

        if (deltaTime <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(deltaTime), deltaTime, "Quasi-spatial core timestep must be positive.");
        }

        if (!ReferenceEquals(committedCoreState.Definition, _definition.CoreDefinition))
        {
            throw new ArgumentException("Committed core state does not use this solver's canonical aggregated-core definition.", nameof(committedCoreState));
        }

        if (!ReferenceEquals(committedPlantState.Definition, _definition.CoreDefinition.PlantDefinition))
        {
            throw new ArgumentException("Committed plant state does not use this solver's canonical plant definition.", nameof(committedPlantState));
        }

        var local = new Dictionary<string, LocalZoneEvaluation>(StringComparer.Ordinal);
        var weightedFeedbackDeltaKOverK = 0d;
        foreach (var zone in _definition.CoreDefinition.Zones)
        {
            var zoneState = committedCoreState.GetZone(zone.Id);
            var fuel = committedPlantState.GetThermalBody(zone.FuelThermalBodyId);
            var coolant = committedPlantState.GetFluidNode(zone.CoolantFluidNodeId);
            if (coolant.Phase == FluidPhase.Unspecified)
            {
                throw new InvalidOperationException(
                    $"Quasi-spatial core zone '{zone.Id}' requires a specified coolant phase to evaluate canonical void feedback.");
            }

            var voidFraction = _voidFractionSolver.Resolve(coolant.Thermodynamics);
            var fuelFeedback = _temperatureSolver.Evaluate(new TemperatureFeedbackInput(_definition.FuelTemperatureFeedback, fuel.Temperature));
            var coolantFeedback = _temperatureSolver.Evaluate(new TemperatureFeedbackInput(_definition.CoolantTemperatureFeedback, coolant.Temperature));
            var voidFeedback = _voidSolver.Evaluate(new VoidFeedbackInput(_definition.VoidFeedback, voidFraction));
            var localFeedback = fuelFeedback.Reactivity + coolantFeedback.Reactivity + voidFeedback.Reactivity;

            local.Add(zone.Id, new LocalZoneEvaluation(
                zoneState.PowerFraction,
                fuel.Temperature,
                coolant.Temperature,
                voidFraction,
                fuelFeedback.Reactivity,
                coolantFeedback.Reactivity,
                voidFeedback.Reactivity,
                localFeedback));
            weightedFeedbackDeltaKOverK += zoneState.PowerFraction.Fraction * localFeedback.DeltaKOverK;
        }

        var globalFeedback = Reactivity.FromDeltaKOverK(weightedFeedbackDeltaKOverK);
        var coupled = ResolveCoupledShapeSignals(local);
        var targetFractions = ResolveTargetFractions(committedCoreState, coupled, globalFeedback);
        var candidateState = RelaxTowardTarget(committedCoreState, targetFractions, deltaTime);

        var snapshots = _definition.CoreDefinition.Zones.Select(zone =>
        {
            var evaluation = local[zone.Id];
            return new QuasiSpatialCoreZoneSnapshot(
                zone.Id,
                evaluation.CommittedPowerFraction,
                candidateState.GetZone(zone.Id).PowerFraction,
                evaluation.FuelTemperature,
                evaluation.CoolantTemperature,
                evaluation.VoidFraction,
                evaluation.FuelTemperatureReactivity,
                evaluation.CoolantTemperatureReactivity,
                evaluation.VoidReactivity,
                evaluation.LocalFeedbackReactivity,
                coupled[zone.Id]);
        }).ToArray();

        var snapshot = new QuasiSpatialCoreFeedbackSnapshot(
            _definition,
            committedCoreState,
            candidateState,
            globalFeedback,
            snapshots);
        return new QuasiSpatialCoreFeedbackStepResult(candidateState, snapshot);
    }

    private Dictionary<string, Reactivity> ResolveCoupledShapeSignals(
        IReadOnlyDictionary<string, LocalZoneEvaluation> local)
    {
        var result = new Dictionary<string, Reactivity>(StringComparer.Ordinal);
        foreach (var zone in _definition.CoreDefinition.Zones)
        {
            var own = local[zone.Id].LocalFeedbackReactivity;
            var incident = _definition.Couplings.Where(item => item.Connects(zone.Id)).ToArray();
            var coupledFraction = incident.Sum(static item => item.CouplingFraction.Fraction);
            var value = own * (1d - coupledFraction);
            foreach (var coupling in incident)
            {
                value += local[coupling.GetOtherZoneId(zone.Id)].LocalFeedbackReactivity * coupling.CouplingFraction.Fraction;
            }

            result.Add(zone.Id, value);
        }

        return result;
    }

    private Dictionary<string, double> ResolveTargetFractions(
        AggregatedCoreState committedCoreState,
        IReadOnlyDictionary<string, Reactivity> coupled,
        Reactivity globalFeedback)
    {
        var rawWeights = new Dictionary<string, double>(StringComparer.Ordinal);
        var rawSum = 0d;
        foreach (var zone in _definition.CoreDefinition.Zones)
        {
            var committedFraction = committedCoreState.GetZone(zone.Id).PowerFraction.Fraction;
            var deviationPcm = coupled[zone.Id].Pcm - globalFeedback.Pcm;
            var exponent = Math.Clamp(
                _definition.PowerShapeSensitivity.PerPcm * deviationPcm,
                -MaximumExponentMagnitude,
                MaximumExponentMagnitude);
            var rawWeight = committedFraction * Math.Exp(exponent);
            if (!double.IsFinite(rawWeight) || rawWeight < 0d)
            {
                throw new InvalidOperationException($"Quasi-spatial power-shape target for zone '{zone.Id}' is invalid.");
            }

            rawWeights.Add(zone.Id, rawWeight);
            rawSum += rawWeight;
        }

        if (!double.IsFinite(rawSum) || rawSum <= 0d)
        {
            throw new InvalidOperationException("Quasi-spatial power-shape target normalization produced a non-positive total weight.");
        }

        return rawWeights.ToDictionary(
            static item => item.Key,
            item => item.Value / rawSum,
            StringComparer.Ordinal);
    }

    private AggregatedCoreState RelaxTowardTarget(
        AggregatedCoreState committedCoreState,
        IReadOnlyDictionary<string, double> targetFractions,
        TimeSpan deltaTime)
    {
        var alpha = 1d - Math.Exp(-deltaTime.TotalSeconds / _definition.PowerShapeRelaxationTime.TotalSeconds);
        if (!double.IsFinite(alpha) || alpha <= 0d || alpha > 1d)
        {
            throw new InvalidOperationException($"Quasi-spatial power-shape relaxation produced invalid alpha {alpha:R}.");
        }

        var provisional = new double[_definition.CoreDefinition.Zones.Count];
        var sum = 0d;
        for (var index = 0; index < _definition.CoreDefinition.Zones.Count; index++)
        {
            var zone = _definition.CoreDefinition.Zones[index];
            var committed = committedCoreState.GetZone(zone.Id).PowerFraction.Fraction;
            var value = committed + (targetFractions[zone.Id] - committed) * alpha;
            provisional[index] = value;
            sum += value;
        }

        if (!double.IsFinite(sum) || sum <= 0d || Math.Abs(sum - 1d) > FractionSumTolerance * 100d)
        {
            throw new InvalidOperationException($"Quasi-spatial relaxed power fractions produced invalid sum {sum:R}.");
        }

        var states = new CoreZoneState[_definition.CoreDefinition.Zones.Count];
        var allocated = 0d;
        for (var index = 0; index < _definition.CoreDefinition.Zones.Count; index++)
        {
            var zone = _definition.CoreDefinition.Zones[index];
            var normalized = index == _definition.CoreDefinition.Zones.Count - 1
                ? 1d - allocated
                : provisional[index] / sum;
            if (index != _definition.CoreDefinition.Zones.Count - 1)
            {
                allocated += normalized;
            }

            states[index] = new CoreZoneState(zone.Id, CoreZonePowerFraction.FromFraction(normalized));
        }

        return new AggregatedCoreState(_definition.CoreDefinition, states);
    }

    private sealed record LocalZoneEvaluation(
        CoreZonePowerFraction CommittedPowerFraction,
        Temperature FuelTemperature,
        Temperature CoolantTemperature,
        VoidFraction VoidFraction,
        Reactivity FuelTemperatureReactivity,
        Reactivity CoolantTemperatureReactivity,
        Reactivity VoidReactivity,
        Reactivity LocalFeedbackReactivity);
}
