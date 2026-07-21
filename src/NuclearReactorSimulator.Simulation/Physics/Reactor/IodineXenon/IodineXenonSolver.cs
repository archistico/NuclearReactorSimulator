using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.Reactor.IodineXenon;

namespace NuclearReactorSimulator.Simulation.Physics.Reactor.IodineXenon;

/// <summary>
/// Deterministic reduced I-135 / Xe-135 inventory model.
/// For constant fission power and neutron population over one caller timestep:
/// dI/dt = S_I - lambda_I I
/// dXe/dt = S_Xe + lambda_I I - (lambda_Xe + k_burn*n) Xe
/// The coupled linear system is integrated analytically over the finite timestep.
/// </summary>
public sealed class IodineXenonSolver
{
    private const double NearlyEqualRateTolerance = 1e-12d;
    private const double NegativeRoundoffTolerance = 1e-12d;

    private readonly IodineXenonDefinition _definition;

    public IodineXenonSolver(IodineXenonDefinition definition)
    {
        _definition = definition ?? throw new ArgumentNullException(nameof(definition));
    }

    public IodineXenonDefinition Definition => _definition;

    public IodineXenonStepResult Step(
        IodineXenonState state,
        Power fissionThermalPower,
        NeutronPopulation neutronPopulation,
        TimeSpan elapsed)
    {
        ArgumentNullException.ThrowIfNull(state);

        if (fissionThermalPower < Power.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(fissionThermalPower),
                fissionThermalPower,
                "Fission thermal power cannot be negative.");
        }

        if (elapsed <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(elapsed), elapsed, "Iodine/xenon integration interval must be greater than zero.");
        }

        var seconds = elapsed.TotalSeconds;
        var powerRatio = fissionThermalPower.Watts / _definition.ReferenceFissionPower.Watts;
        var iodineSource = _definition.IodineProductionAtReferencePower.RelativePerSecond * powerRatio;
        var directXenonSource = _definition.DirectXenonProductionAtReferencePower.RelativePerSecond * powerRatio;
        var iodineDecay = _definition.IodineDecayConstant.PerSecond;
        var xenonRemoval = _definition.XenonDecayConstant.PerSecond
            + (_definition.XenonBurnupCoefficient.PerSecondPerRelativeNeutronPopulation * neutronPopulation.Relative);

        var oldIodine = state.Iodine.Relative;
        var oldXenon = state.Xenon.Relative;
        var iodineEquilibrium = iodineSource / iodineDecay;
        var iodineDecayFactor = Math.Exp(-iodineDecay * seconds);
        var xenonDecayFactor = Math.Exp(-xenonRemoval * seconds);
        var newIodine = iodineEquilibrium + ((oldIodine - iodineEquilibrium) * iodineDecayFactor);

        var constantXenonSource = directXenonSource + iodineSource;
        var newXenon = (oldXenon * xenonDecayFactor)
            + (constantXenonSource * (1d - xenonDecayFactor) / xenonRemoval)
            + CoupledIodineContribution(
                iodineDecay,
                xenonRemoval,
                oldIodine - iodineEquilibrium,
                iodineDecayFactor,
                xenonDecayFactor,
                seconds);

        newIodine = NormalizeNonNegative("iodine", newIodine);
        newXenon = NormalizeNonNegative("xenon", newXenon);

        var next = new IodineXenonState(
            IodineInventory.FromRelative(newIodine),
            XenonInventory.FromRelative(newXenon));

        return new IodineXenonStepResult(
            next,
            CreateSnapshot(next, fissionThermalPower, neutronPopulation));
    }

    public IodineXenonSnapshot CreateSnapshot(
        IodineXenonState state,
        Power fissionThermalPower,
        NeutronPopulation neutronPopulation)
    {
        ArgumentNullException.ThrowIfNull(state);

        if (fissionThermalPower < Power.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(fissionThermalPower),
                fissionThermalPower,
                "Fission thermal power cannot be negative.");
        }

        var powerRatio = fissionThermalPower.Watts / _definition.ReferenceFissionPower.Watts;
        var iodineProduction = _definition.IodineProductionAtReferencePower.RelativePerSecond * powerRatio;
        var directXenonProduction = _definition.DirectXenonProductionAtReferencePower.RelativePerSecond * powerRatio;
        var iodineDecayRate = _definition.IodineDecayConstant.PerSecond * state.Iodine.Relative;
        var xenonNaturalDecayRate = _definition.XenonDecayConstant.PerSecond * state.Xenon.Relative;
        var xenonBurnupRate = _definition.XenonBurnupCoefficient.PerSecondPerRelativeNeutronPopulation
            * neutronPopulation.Relative
            * state.Xenon.Relative;
        var xenonReactivity = _definition.XenonReactivityCoefficient * state.Xenon;

        ValidateFinite(
            iodineProduction,
            directXenonProduction,
            iodineDecayRate,
            xenonNaturalDecayRate,
            xenonBurnupRate,
            xenonReactivity.DeltaKOverK);

        return new IodineXenonSnapshot(
            _definition.Id,
            state,
            iodineProduction,
            iodineDecayRate,
            directXenonProduction,
            iodineDecayRate,
            xenonNaturalDecayRate,
            xenonBurnupRate,
            xenonReactivity);
    }

    private static double CoupledIodineContribution(
        double iodineDecay,
        double xenonRemoval,
        double iodineDeviation,
        double iodineDecayFactor,
        double xenonDecayFactor,
        double seconds)
    {
        var difference = xenonRemoval - iodineDecay;

        if (Math.Abs(difference) <= NearlyEqualRateTolerance * Math.Max(1d, Math.Max(iodineDecay, xenonRemoval)))
        {
            return iodineDecay * iodineDeviation * seconds * xenonDecayFactor;
        }

        return iodineDecay * iodineDeviation * (iodineDecayFactor - xenonDecayFactor) / difference;
    }

    private static double NormalizeNonNegative(string inventoryName, double value)
    {
        if (!double.IsFinite(value))
        {
            throw new IodineXenonNumericalException(
                $"{inventoryName} inventory exceeded the finite numerical envelope.");
        }

        if (value >= 0d)
        {
            return value == 0d ? 0d : value;
        }

        if (value >= -NegativeRoundoffTolerance)
        {
            return 0d;
        }

        throw new IodineXenonNumericalException(
            $"{inventoryName} inventory became materially negative ({value:R}).");
    }

    private static void ValidateFinite(params double[] values)
    {
        if (values.Any(static value => !double.IsFinite(value)))
        {
            throw new IodineXenonNumericalException("Iodine/xenon diagnostics exceeded the finite numerical envelope.");
        }
    }
}
