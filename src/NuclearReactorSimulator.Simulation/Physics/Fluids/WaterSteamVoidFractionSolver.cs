using NuclearReactorSimulator.Domain.Physics.Fluids;

namespace NuclearReactorSimulator.Simulation.Physics.Fluids;

/// <summary>
/// Resolves vapor volumetric fraction from the committed coarse water/steam thermodynamic state.
/// Saturated-mixture vapor quality is a mass fraction and is converted to volume fraction using
/// the same simplified saturation-property model that produced the thermodynamic closure.
/// </summary>
public sealed class WaterSteamVoidFractionSolver
{
    private readonly SimplifiedWaterSteamThermodynamicModel _thermodynamicModel;

    public WaterSteamVoidFractionSolver(SimplifiedWaterSteamThermodynamicModel thermodynamicModel)
    {
        ArgumentNullException.ThrowIfNull(thermodynamicModel);
        _thermodynamicModel = thermodynamicModel;
    }

    public VoidFraction Resolve(FluidThermodynamicState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        return state.Phase switch
        {
            FluidPhase.SubcooledLiquid => VoidFraction.NoVoid,
            FluidPhase.SuperheatedVapor => VoidFraction.AllVapor,
            FluidPhase.SaturatedMixture => ResolveSaturatedMixture(state),
            _ => throw new InvalidOperationException(
                "Void fraction cannot be resolved from an unspecified fluid phase."),
        };
    }

    private VoidFraction ResolveSaturatedMixture(FluidThermodynamicState state)
    {
        var quality = state.VaporQuality
            ?? throw new InvalidOperationException("A saturated mixture must expose vapor quality.");

        if (quality == VaporQuality.SaturatedLiquid)
        {
            return VoidFraction.NoVoid;
        }

        if (quality == VaporQuality.DrySaturatedVapor)
        {
            return VoidFraction.AllVapor;
        }

        var saturation = _thermodynamicModel.GetSaturationProperties(state.Temperature);
        var vaporSpecificVolume = saturation.SaturatedVaporSpecificVolumeCubicMetresPerKilogram;
        var liquidSpecificVolume = saturation.SaturatedLiquidSpecificVolumeCubicMetresPerKilogram;
        var vaporVolumePerUnitMixtureMass = quality.Fraction * vaporSpecificVolume;
        var liquidVolumePerUnitMixtureMass = (1d - quality.Fraction) * liquidSpecificVolume;
        var totalVolumePerUnitMixtureMass = vaporVolumePerUnitMixtureMass + liquidVolumePerUnitMixtureMass;

        if (!double.IsFinite(totalVolumePerUnitMixtureMass) || totalVolumePerUnitMixtureMass <= 0d)
        {
            throw new InvalidOperationException("The saturated-mixture specific volume is not physically valid.");
        }

        var voidFraction = vaporVolumePerUnitMixtureMass / totalVolumePerUnitMixtureMass;

        if (!double.IsFinite(voidFraction))
        {
            throw new InvalidOperationException("The calculated void fraction is not finite.");
        }

        return VoidFraction.FromFraction(Math.Clamp(voidFraction, 0d, 1d));
    }
}
