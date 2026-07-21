using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.Reactor.Neutronics;

namespace NuclearReactorSimulator.Simulation.Physics.Reactor.Neutronics;

/// <summary>
/// Deterministic point-reactor kinetics with delayed-neutron groups.
/// The solver uses fixed-count RK4 substepping derived only from the physical coefficients and caller timestep.
/// </summary>
public sealed class PointKineticsSolver
{
    private const double MaxDimensionlessInternalStep = 0.05d;
    private const int MaxInternalSubsteps = 1_000_000;
    private const double NearZeroLogRatePerSecond = 1e-12d;
    private const double NegativeRoundoffTolerance = 1e-12d;

    private readonly PointKineticsParameters _parameters;

    public PointKineticsSolver(PointKineticsParameters parameters)
    {
        _parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));
    }

    public PointKineticsParameters Parameters => _parameters;

    public PointKineticsState Step(
        PointKineticsState state,
        Reactivity reactivity,
        TimeSpan elapsed)
    {
        ArgumentNullException.ThrowIfNull(state);
        ValidateStateCoverage(state);

        if (elapsed <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(elapsed), elapsed, "Point-kinetics integration interval must be greater than zero.");
        }

        var values = ToVector(state);
        var substeps = CalculateInternalSubstepCount(reactivity, elapsed);
        var stepSeconds = elapsed.TotalSeconds / substeps;

        var k1 = new double[values.Length];
        var k2 = new double[values.Length];
        var k3 = new double[values.Length];
        var k4 = new double[values.Length];
        var scratch = new double[values.Length];

        for (var substep = 0; substep < substeps; substep++)
        {
            EvaluateDerivative(values, reactivity, k1);
            Combine(values, k1, stepSeconds * 0.5d, scratch);

            EvaluateDerivative(scratch, reactivity, k2);
            Combine(values, k2, stepSeconds * 0.5d, scratch);

            EvaluateDerivative(scratch, reactivity, k3);
            Combine(values, k3, stepSeconds, scratch);

            EvaluateDerivative(scratch, reactivity, k4);

            for (var index = 0; index < values.Length; index++)
            {
                var increment = stepSeconds / 6d
                    * (k1[index] + (2d * k2[index]) + (2d * k3[index]) + k4[index]);
                values[index] = NormalizePopulation(values[index] + increment, index);
            }
        }

        return FromVector(values);
    }

    public PointKineticsSnapshot CreateSnapshot(
        PointKineticsState state,
        Reactivity reactivity)
    {
        ArgumentNullException.ThrowIfNull(state);
        ValidateStateCoverage(state);

        var values = ToVector(state);
        var derivative = new double[values.Length];
        EvaluateDerivative(values, reactivity, derivative);

        double? logRate = null;
        double? reactorPeriodSeconds = null;

        if (state.NeutronPopulation.Relative > 0d)
        {
            var candidateLogRate = derivative[0] / state.NeutronPopulation.Relative;
            if (!double.IsFinite(candidateLogRate))
            {
                throw new NeutronKineticsNumericalException("Point-kinetics logarithmic neutron-population rate is not finite.");
            }

            logRate = candidateLogRate;
            if (Math.Abs(candidateLogRate) > NearZeroLogRatePerSecond)
            {
                reactorPeriodSeconds = 1d / candidateLogRate;
            }
        }

        return new PointKineticsSnapshot(
            state,
            reactivity,
            _parameters.EffectiveDelayedNeutronFraction,
            logRate,
            reactorPeriodSeconds);
    }

    private int CalculateInternalSubstepCount(Reactivity reactivity, TimeSpan elapsed)
    {
        var promptLifetime = _parameters.PromptNeutronGenerationTimeSeconds;
        var beta = _parameters.EffectiveDelayedNeutronFraction.Fraction;
        var neutronRowNorm = Math.Abs((reactivity.DeltaKOverK - beta) / promptLifetime);

        foreach (var group in _parameters.DelayedNeutronGroups)
        {
            neutronRowNorm += group.DecayConstant.PerSecond;
        }

        var maximumRowNorm = neutronRowNorm;
        foreach (var group in _parameters.DelayedNeutronGroups)
        {
            var precursorRowNorm = group.Fraction.Fraction / promptLifetime
                + group.DecayConstant.PerSecond;
            maximumRowNorm = Math.Max(maximumRowNorm, precursorRowNorm);
        }

        var raw = elapsed.TotalSeconds * maximumRowNorm / MaxDimensionlessInternalStep;
        if (!double.IsFinite(raw))
        {
            throw new NeutronKineticsNumericalException("Point-kinetics stiffness estimate is not finite.");
        }

        var required = Math.Max(1d, Math.Ceiling(raw));
        if (required > MaxInternalSubsteps)
        {
            throw new NeutronKineticsNumericalException(
                $"Point-kinetics integration requires {required:0} internal substeps, exceeding the supported limit of {MaxInternalSubsteps}.");
        }

        return checked((int)required);
    }

    private void EvaluateDerivative(
        IReadOnlyList<double> values,
        Reactivity reactivity,
        double[] derivative)
    {
        var promptLifetime = _parameters.PromptNeutronGenerationTimeSeconds;
        var beta = _parameters.EffectiveDelayedNeutronFraction.Fraction;
        var neutronPopulation = values[0];

        var neutronDerivative = (reactivity.DeltaKOverK - beta) / promptLifetime * neutronPopulation;
        var compensation = 0d;

        for (var index = 0; index < _parameters.DelayedNeutronGroups.Count; index++)
        {
            var definition = _parameters.DelayedNeutronGroups[index];
            var precursorPopulation = values[index + 1];
            var delayedSource = definition.DecayConstant.PerSecond * precursorPopulation;
            var corrected = delayedSource - compensation;
            var next = neutronDerivative + corrected;
            compensation = (next - neutronDerivative) - corrected;
            neutronDerivative = next;

            derivative[index + 1] = definition.Fraction.Fraction / promptLifetime * neutronPopulation
                - definition.DecayConstant.PerSecond * precursorPopulation;
        }

        derivative[0] = neutronDerivative;
    }

    private static void Combine(
        IReadOnlyList<double> baseline,
        IReadOnlyList<double> derivative,
        double scale,
        double[] destination)
    {
        for (var index = 0; index < destination.Length; index++)
        {
            destination[index] = baseline[index] + derivative[index] * scale;
        }
    }

    private PointKineticsState FromVector(IReadOnlyList<double> values)
    {
        var groups = new DelayedNeutronGroupState[_parameters.DelayedNeutronGroups.Count];
        for (var index = 0; index < groups.Length; index++)
        {
            groups[index] = new DelayedNeutronGroupState(
                _parameters.DelayedNeutronGroups[index].Id,
                DelayedNeutronPrecursorPopulation.FromRelative(values[index + 1]));
        }

        return new PointKineticsState(
            NeutronPopulation.FromRelative(values[0]),
            groups);
    }

    private static double NormalizePopulation(double value, int componentIndex)
    {
        if (!double.IsFinite(value))
        {
            throw new NeutronKineticsNumericalException(
                $"Point-kinetics component {componentIndex} became non-finite.");
        }

        if (value >= 0d)
        {
            return value == 0d ? 0d : value;
        }

        if (value >= -NegativeRoundoffTolerance)
        {
            return 0d;
        }

        throw new NeutronKineticsNumericalException(
            $"Point-kinetics component {componentIndex} became negative ({value:R}).");
    }

    private double[] ToVector(PointKineticsState state)
    {
        var values = new double[_parameters.DelayedNeutronGroups.Count + 1];
        values[0] = state.NeutronPopulation.Relative;

        for (var index = 0; index < _parameters.DelayedNeutronGroups.Count; index++)
        {
            values[index + 1] = state.DelayedNeutronGroups[index].PrecursorPopulation.Relative;
        }

        return values;
    }

    private void ValidateStateCoverage(PointKineticsState state)
    {
        if (state.DelayedNeutronGroups.Count != _parameters.DelayedNeutronGroups.Count)
        {
            throw new ArgumentException("Point-kinetics state does not cover the complete delayed-neutron parameter set.", nameof(state));
        }

        for (var index = 0; index < _parameters.DelayedNeutronGroups.Count; index++)
        {
            if (!string.Equals(
                    _parameters.DelayedNeutronGroups[index].Id,
                    state.DelayedNeutronGroups[index].GroupId,
                    StringComparison.Ordinal))
            {
                throw new ArgumentException("Point-kinetics state group ids do not match the canonical parameter set.", nameof(state));
            }
        }
    }
}
