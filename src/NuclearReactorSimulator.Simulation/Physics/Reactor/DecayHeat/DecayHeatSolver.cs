using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.Reactor.DecayHeat;

namespace NuclearReactorSimulator.Simulation.Physics.Reactor.DecayHeat;

/// <summary>
/// Deterministic equivalent-group decay-heat model.
/// Each group stores latent decay energy E and obeys dE/dt = f*P_fission - lambda*E.
/// The finite-step solution is analytic for constant fission power over the caller timestep.
/// </summary>
public sealed class DecayHeatSolver
{
    private const double NegativeRoundoffToleranceJoules = 1e-9d;

    private readonly DecayHeatDefinition _definition;

    public DecayHeatSolver(DecayHeatDefinition definition)
    {
        _definition = definition ?? throw new ArgumentNullException(nameof(definition));
    }

    public DecayHeatDefinition Definition => _definition;

    public DecayHeatStepResult Step(
        DecayHeatState state,
        Power fissionThermalPower,
        TimeSpan elapsed)
    {
        ArgumentNullException.ThrowIfNull(state);
        ValidateStateCoverage(state);

        if (fissionThermalPower < Power.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(fissionThermalPower),
                fissionThermalPower,
                "Fission thermal power cannot be negative.");
        }

        if (elapsed <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(elapsed), elapsed, "Decay-heat integration interval must be greater than zero.");
        }

        var seconds = elapsed.TotalSeconds;
        var newGroups = new DecayHeatGroupState[_definition.Groups.Count];
        var producedJoules = new double[_definition.Groups.Count];
        var emittedJoules = new double[_definition.Groups.Count];
        var productionWatts = new double[_definition.Groups.Count];

        for (var index = 0; index < _definition.Groups.Count; index++)
        {
            var groupDefinition = _definition.Groups[index];
            var groupState = state.Groups[index];
            var lambda = groupDefinition.DecayConstant.PerSecond;
            var sourceWatts = fissionThermalPower.Watts * groupDefinition.GenerationFraction.Fraction;
            var exponent = -lambda * seconds;
            var decayFactor = Math.Exp(exponent);
            var equilibriumJoules = sourceWatts / lambda;
            var oldJoules = groupState.StoredDecayEnergy.Joules;
            var newJoules = (oldJoules * decayFactor) + (equilibriumJoules * (1d - decayFactor));
            var produced = sourceWatts * seconds;
            var emitted = oldJoules + produced - newJoules;

            ValidateFinite(groupDefinition.Id, sourceWatts, equilibriumJoules, newJoules, produced, emitted);

            if (newJoules < 0d)
            {
                if (newJoules >= -NegativeRoundoffToleranceJoules)
                {
                    newJoules = 0d;
                }
                else
                {
                    throw new DecayHeatNumericalException(
                        $"Decay-heat group '{groupDefinition.Id}' produced negative stored energy {newJoules:R} J.");
                }
            }

            if (emitted < 0d)
            {
                if (emitted >= -NegativeRoundoffToleranceJoules)
                {
                    emitted = 0d;
                }
                else
                {
                    throw new DecayHeatNumericalException(
                        $"Decay-heat group '{groupDefinition.Id}' produced negative emitted energy {emitted:R} J.");
                }
            }

            productionWatts[index] = sourceWatts;
            producedJoules[index] = produced;
            emittedJoules[index] = emitted;
            newGroups[index] = new DecayHeatGroupState(groupDefinition.Id, Energy.FromJoules(newJoules));
        }

        var nextState = new DecayHeatState(newGroups);
        var totalProductionPower = Power.FromWatts(CompensatedSum(productionWatts));
        var totalProducedEnergy = Energy.FromJoules(CompensatedSum(producedJoules));
        var totalEmittedEnergy = Energy.FromJoules(CompensatedSum(emittedJoules));
        var averageDecayHeatPower = totalEmittedEnergy.Per(elapsed);
        var averageDepositions = Allocate(averageDecayHeatPower);
        var snapshot = CreateSnapshot(nextState);

        return new DecayHeatStepResult(
            nextState,
            totalProductionPower,
            totalProducedEnergy,
            totalEmittedEnergy,
            averageDecayHeatPower,
            snapshot,
            averageDepositions);
    }

    public DecayHeatSnapshot CreateSnapshot(DecayHeatState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        ValidateStateCoverage(state);

        var groups = new DecayHeatGroupSnapshot[_definition.Groups.Count];
        var powers = new double[_definition.Groups.Count];

        for (var index = 0; index < _definition.Groups.Count; index++)
        {
            var groupDefinition = _definition.Groups[index];
            var groupState = state.Groups[index];
            var watts = groupState.StoredDecayEnergy.Joules * groupDefinition.DecayConstant.PerSecond;

            if (!double.IsFinite(watts) || watts < 0d)
            {
                throw new DecayHeatNumericalException(
                    $"Decay-heat group '{groupDefinition.Id}' produced invalid instantaneous power {watts:R} W.");
            }

            powers[index] = watts;
            groups[index] = new DecayHeatGroupSnapshot(
                groupDefinition.Id,
                groupState.StoredDecayEnergy,
                Power.FromWatts(watts));
        }

        var totalPower = Power.FromWatts(CompensatedSum(powers));
        return new DecayHeatSnapshot(
            _definition.Id,
            state,
            totalPower,
            groups,
            Allocate(totalPower));
    }

    private IReadOnlyList<DecayHeatDeposition> Allocate(Power totalPower)
    {
        var destinations = _definition.HeatDestinations;
        var result = new DecayHeatDeposition[destinations.Count];
        var allocatedWatts = 0d;

        for (var index = 0; index < destinations.Count; index++)
        {
            var destination = destinations[index];
            double watts;

            if (index == destinations.Count - 1)
            {
                watts = totalPower.Watts - allocatedWatts;
            }
            else
            {
                var normalizedFraction = destination.Fraction.Fraction / _definition.HeatDestinationFractionSum;
                watts = totalPower.Watts * normalizedFraction;
                allocatedWatts += watts;
            }

            if (!double.IsFinite(watts) || watts < 0d)
            {
                throw new DecayHeatNumericalException(
                    $"Decay-heat allocation for target '{destination.TargetDomainId}' produced invalid power {watts:R} W.");
            }

            result[index] = new DecayHeatDeposition(destination.TargetDomainId, Power.FromWatts(watts));
        }

        return result;
    }

    private void ValidateStateCoverage(DecayHeatState state)
    {
        if (state.Groups.Count != _definition.Groups.Count)
        {
            throw new ArgumentException("Decay-heat state does not cover the complete configured group set.", nameof(state));
        }

        for (var index = 0; index < _definition.Groups.Count; index++)
        {
            if (!string.Equals(_definition.Groups[index].Id, state.Groups[index].GroupId, StringComparison.Ordinal))
            {
                throw new ArgumentException("Decay-heat state group ids do not match the canonical definition.", nameof(state));
            }
        }
    }

    private static void ValidateFinite(string groupId, params double[] values)
    {
        if (values.Any(static value => !double.IsFinite(value)))
        {
            throw new DecayHeatNumericalException(
                $"Decay-heat group '{groupId}' exceeded the finite numerical envelope.");
        }
    }

    private static double CompensatedSum(IEnumerable<double> values)
    {
        var sum = 0d;
        var compensation = 0d;

        foreach (var value in values)
        {
            var corrected = value - compensation;
            var next = sum + corrected;
            compensation = (next - sum) - corrected;
            sum = next;
        }

        return sum;
    }
}
