namespace NuclearReactorSimulator.Simulation.Plant;

/// <summary>
/// M10.9.4.1-H.11 localization evidence for thermodynamic phase/envelope boundaries already detected by H.10.
/// </summary>
public sealed record ThermodynamicSwitchingLocalizationReport(
    IReadOnlyList<ThermodynamicSwitchingNodeLocalization> Nodes)
{
    public int LocalizedNodeCount => Nodes.Count;

    public int PhaseBoundaryNodeCount => Nodes.Count(static item => item.PhaseBoundaryObserved);

    public int EnvelopeBoundaryNodeCount => Nodes.Count(static item => item.EnvelopeBoundaryObserved);
}

public sealed record ThermodynamicSwitchingNodeLocalization(
    string NodeId,
    string CrossingAxis,
    string BoundaryClassification,
    bool PhaseBoundaryObserved,
    bool EnvelopeBoundaryObserved,
    string SuggestedActiveSet,
    double EnergyProbeJoules,
    double MassProbeKilograms,
    ThermodynamicSwitchingProbePoint Nominal,
    ThermodynamicSwitchingProbePoint EnergyMinus,
    ThermodynamicSwitchingProbePoint EnergyPlus,
    ThermodynamicSwitchingProbePoint MassMinus,
    ThermodynamicSwitchingProbePoint MassPlus);

public sealed record ThermodynamicSwitchingProbePoint(
    string Label,
    bool Resolved,
    string Phase,
    double MassKilograms,
    double InternalEnergyJoules,
    double SpecificVolumeCubicMetresPerKilogram,
    double SpecificInternalEnergyJoulesPerKilogram,
    double PressurePascals,
    double TemperatureKelvins,
    double? VaporQualityFraction,
    bool SaturationReferenceAvailable,
    double SaturationPressurePascals,
    double RelativePressureDistanceFromSaturation,
    double SaturatedLiquidInternalEnergyJoulesPerKilogram,
    double SaturatedVaporInternalEnergyJoulesPerKilogram,
    double DistanceAboveSaturatedLiquidEnergyJoulesPerKilogram,
    double DistanceBelowSaturatedVaporEnergyJoulesPerKilogram)
{
    public static ThermodynamicSwitchingProbePoint Unresolved(
        string label,
        double massKilograms,
        double internalEnergyJoules,
        double specificVolumeCubicMetresPerKilogram,
        double specificInternalEnergyJoulesPerKilogram)
        => new(
            label,
            false,
            "out-of-range",
            massKilograms,
            internalEnergyJoules,
            specificVolumeCubicMetresPerKilogram,
            specificInternalEnergyJoulesPerKilogram,
            0d,
            0d,
            null,
            false,
            0d,
            double.NaN,
            double.NaN,
            double.NaN,
            double.NaN,
            double.NaN);
}
