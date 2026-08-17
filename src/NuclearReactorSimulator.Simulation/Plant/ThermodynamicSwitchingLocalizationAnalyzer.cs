using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Plant;
using NuclearReactorSimulator.Simulation.Physics.Fluids;

namespace NuclearReactorSimulator.Simulation.Plant;

/// <summary>
/// M10.9.4.1-H.11 shadow-only localization of the thermodynamic boundaries exposed by H.10.
/// It probes only nodes that H.10 already marked as phase/envelope-switching and does not solve,
/// correct, clamp or commit any plant state.
/// </summary>
public sealed class ThermodynamicSwitchingLocalizationAnalyzer
{
    private readonly IFluidThermodynamicModel _thermodynamicModel;
    private readonly IWaterSteamSaturationPropertyProvider _saturationProvider;

    public ThermodynamicSwitchingLocalizationAnalyzer(
        IFluidThermodynamicModel thermodynamicModel,
        IWaterSteamSaturationPropertyProvider saturationProvider)
    {
        _thermodynamicModel = thermodynamicModel ?? throw new ArgumentNullException(nameof(thermodynamicModel));
        _saturationProvider = saturationProvider ?? throw new ArgumentNullException(nameof(saturationProvider));
    }

    public ThermodynamicSwitchingLocalizationReport Analyze(
        PlantState state,
        HydraulicMapSmoothnessReport h10Report,
        ThermodynamicSwitchingLocalizationOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(h10Report);
        options ??= ThermodynamicSwitchingLocalizationOptions.H11AuditDefault;

        var switchingNodeIds = h10Report.ThermodynamicNodes
            .Where(static item => item.PhaseOrEnvelopeSwitchObserved)
            .Select(static item => item.NodeId)
            .ToHashSet(StringComparer.Ordinal);

        var nodes = state.FluidNodes
            .Where(node => switchingNodeIds.Contains(node.Id))
            .OrderBy(static node => node.Id, StringComparer.Ordinal)
            .Select(node => AnalyzeNode(node, options))
            .ToArray();

        return new ThermodynamicSwitchingLocalizationReport(nodes);
    }

    private ThermodynamicSwitchingNodeLocalization AnalyzeNode(
        FluidNodeState node,
        ThermodynamicSwitchingLocalizationOptions options)
    {
        var coarseEnergyProbe = Math.Max(Math.Abs(node.InternalEnergy.Joules) * options.RelativeInventoryProbe, 1d);
        var coarseMassProbe = Math.Min(
            Math.Max(Math.Abs(node.Mass.Kilograms) * options.RelativeInventoryProbe, 1e-9d),
            node.Mass.Kilograms * 0.25d);
        var fineEnergyProbe = coarseEnergyProbe * options.FineProbeFactor;
        var fineMassProbe = coarseMassProbe * options.FineProbeFactor;
        var nominal = FromResolvedState("nominal", node, node.Mass.Kilograms, node.InternalEnergy.Joules);

        var coarseEnergy = ResolveAxis(node, nominal, "energy", coarseEnergyProbe);
        var fineEnergy = ResolveAxis(node, nominal, "energy", fineEnergyProbe);
        var selectedEnergy = fineEnergy.SwitchObserved ? fineEnergy : coarseEnergy;
        var coarseMass = ResolveAxis(node, nominal, "mass", coarseMassProbe);
        var fineMass = ResolveAxis(node, nominal, "mass", fineMassProbe);
        var selectedMass = fineMass.SwitchObserved ? fineMass : coarseMass;

        var energyPhaseBoundary = selectedEnergy.PhaseBoundaryObserved;
        var massPhaseBoundary = selectedMass.PhaseBoundaryObserved;
        var energyEnvelopeBoundary = selectedEnergy.EnvelopeBoundaryObserved;
        var massEnvelopeBoundary = selectedMass.EnvelopeBoundaryObserved;
        var phaseBoundary = energyPhaseBoundary || massPhaseBoundary;
        var envelopeBoundary = energyEnvelopeBoundary || massEnvelopeBoundary;
        var crossingAxis = ClassifyAxis(selectedEnergy.SwitchObserved, selectedMass.SwitchObserved);
        var boundaryClassification = ClassifyBoundary(phaseBoundary, envelopeBoundary);

        return new ThermodynamicSwitchingNodeLocalization(
            node.Id,
            crossingAxis,
            boundaryClassification,
            phaseBoundary,
            envelopeBoundary,
            $"hold-{node.Phase}",
            selectedEnergy.ProbeSize,
            selectedMass.ProbeSize,
            nominal,
            selectedEnergy.Minus,
            selectedEnergy.Plus,
            selectedMass.Minus,
            selectedMass.Plus);
    }

    private AxisProbe ResolveAxis(
        FluidNodeState node,
        ThermodynamicSwitchingProbePoint nominal,
        string axis,
        double probeSize)
    {
        ThermodynamicSwitchingProbePoint minus;
        ThermodynamicSwitchingProbePoint plus;
        if (string.Equals(axis, "energy", StringComparison.Ordinal))
        {
            minus = ResolveProbe(node, "energy-minus", node.Mass.Kilograms, node.InternalEnergy.Joules - probeSize);
            plus = ResolveProbe(node, "energy-plus", node.Mass.Kilograms, node.InternalEnergy.Joules + probeSize);
        }
        else
        {
            minus = ResolveProbe(node, "mass-minus", node.Mass.Kilograms - probeSize, node.InternalEnergy.Joules);
            plus = ResolveProbe(node, "mass-plus", node.Mass.Kilograms + probeSize, node.InternalEnergy.Joules);
        }

        var phaseBoundary = HasPhaseDifference(nominal, minus) || HasPhaseDifference(nominal, plus);
        var envelopeBoundary = !minus.Resolved || !plus.Resolved;
        return new AxisProbe(probeSize, minus, plus, phaseBoundary, envelopeBoundary);
    }

    private ThermodynamicSwitchingProbePoint ResolveProbe(
        FluidNodeState node,
        string label,
        double massKilograms,
        double internalEnergyJoules)
    {
        var specificVolume = node.Volume.CubicMetres / massKilograms;
        var specificEnergy = internalEnergyJoules / massKilograms;
        if (!double.IsFinite(massKilograms)
            || massKilograms <= 0d
            || !double.IsFinite(internalEnergyJoules)
            || !double.IsFinite(specificVolume)
            || specificVolume <= 0d
            || !double.IsFinite(specificEnergy))
        {
            return ThermodynamicSwitchingProbePoint.Unresolved(label, massKilograms, internalEnergyJoules, specificVolume, specificEnergy);
        }

        try
        {
            var inventory = new FluidNodeInventory(
                Mass.FromKilograms(massKilograms),
                Energy.FromJoules(internalEnergyJoules));
            var resolved = _thermodynamicModel.Resolve(node.Definition, inventory, node.Thermodynamics);
            var resolvedNode = new FluidNodeState(node.Definition, inventory, resolved);
            return FromResolvedState(label, resolvedNode, massKilograms, internalEnergyJoules);
        }
        catch (Exception exception) when (
            exception is WaterSteamStateOutOfRangeException
            or ArgumentOutOfRangeException
            or ArithmeticException)
        {
            return ThermodynamicSwitchingProbePoint.Unresolved(label, massKilograms, internalEnergyJoules, specificVolume, specificEnergy);
        }
    }

    private ThermodynamicSwitchingProbePoint FromResolvedState(
        string label,
        FluidNodeState state,
        double massKilograms,
        double internalEnergyJoules)
    {
        var specificVolume = state.Volume.CubicMetres / massKilograms;
        var specificEnergy = internalEnergyJoules / massKilograms;
        var saturationAvailable = TryGetSaturationReference(state.Temperature, out var saturation);
        var relativePressureDistance = saturationAvailable
            ? (state.Pressure.Pascals - saturation!.Pressure.Pascals) / saturation.Pressure.Pascals
            : double.NaN;
        var liquidEnergy = saturationAvailable
            ? saturation!.SaturatedLiquidInternalEnergy.JoulesPerKilogram
            : double.NaN;
        var vaporEnergy = saturationAvailable
            ? saturation!.SaturatedVaporInternalEnergy.JoulesPerKilogram
            : double.NaN;

        return new ThermodynamicSwitchingProbePoint(
            label,
            true,
            state.Phase.ToString(),
            massKilograms,
            internalEnergyJoules,
            specificVolume,
            specificEnergy,
            state.Pressure.Pascals,
            state.Temperature.Kelvins,
            state.VaporQuality?.Fraction,
            saturationAvailable,
            saturationAvailable ? saturation!.Pressure.Pascals : 0d,
            relativePressureDistance,
            liquidEnergy,
            vaporEnergy,
            saturationAvailable ? specificEnergy - liquidEnergy : double.NaN,
            saturationAvailable ? vaporEnergy - specificEnergy : double.NaN);
    }

    private bool TryGetSaturationReference(Temperature temperature, out WaterSteamSaturationProperties? saturation)
    {
        if (temperature < SimplifiedWaterSteamThermodynamicModel.MinimumTemperature
            || temperature > SimplifiedWaterSteamThermodynamicModel.MaximumSaturationTemperature)
        {
            saturation = null;
            return false;
        }

        try
        {
            saturation = _saturationProvider.GetSaturationProperties(temperature);
            return true;
        }
        catch (ArgumentOutOfRangeException)
        {
            saturation = null;
            return false;
        }
    }

    private static bool HasPhaseDifference(
        ThermodynamicSwitchingProbePoint nominal,
        ThermodynamicSwitchingProbePoint probe)
        => probe.Resolved && !string.Equals(nominal.Phase, probe.Phase, StringComparison.Ordinal);

    private static string ClassifyAxis(bool energy, bool mass)
        => (energy, mass) switch
        {
            (true, true) => "energy+mass",
            (true, false) => "energy",
            (false, true) => "mass",
            _ => "none",
        };

    private static string ClassifyBoundary(bool phaseBoundary, bool envelopeBoundary)
        => (phaseBoundary, envelopeBoundary) switch
        {
            (true, true) => "phase+envelope",
            (true, false) => "phase-boundary",
            (false, true) => "envelope-edge",
            _ => "unclassified",
        };

    private sealed record AxisProbe(
        double ProbeSize,
        ThermodynamicSwitchingProbePoint Minus,
        ThermodynamicSwitchingProbePoint Plus,
        bool PhaseBoundaryObserved,
        bool EnvelopeBoundaryObserved)
    {
        public bool SwitchObserved => PhaseBoundaryObserved || EnvelopeBoundaryObserved;
    }
}
