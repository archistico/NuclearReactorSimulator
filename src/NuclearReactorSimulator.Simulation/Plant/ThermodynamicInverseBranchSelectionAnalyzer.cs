using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Plant;
using NuclearReactorSimulator.Simulation.Physics.Fluids;

namespace NuclearReactorSimulator.Simulation.Plant;

/// <summary>
/// M10.9.4.1-H.12 shadow-only analysis of inverse-map branch selection around the thermodynamic
/// boundaries localized by H.11. The analyzer observes existing roots and selection order only;
/// it never changes the production resolver or commits an alternative thermodynamic state.
/// </summary>
public sealed class ThermodynamicInverseBranchSelectionAnalyzer
{
    private readonly IFluidThermodynamicModel _thermodynamicModel;
    private readonly IWaterSteamInverseBranchDiagnosticProvider _diagnosticProvider;

    public ThermodynamicInverseBranchSelectionAnalyzer(
        IFluidThermodynamicModel thermodynamicModel,
        IWaterSteamInverseBranchDiagnosticProvider diagnosticProvider)
    {
        _thermodynamicModel = thermodynamicModel ?? throw new ArgumentNullException(nameof(thermodynamicModel));
        _diagnosticProvider = diagnosticProvider ?? throw new ArgumentNullException(nameof(diagnosticProvider));
    }

    public ThermodynamicInverseBranchSelectionReport Analyze(
        PlantState state,
        ThermodynamicSwitchingLocalizationReport h11Report)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(h11Report);

        var localizations = h11Report.Nodes.ToDictionary(static item => item.NodeId, StringComparer.Ordinal);
        var nodes = state.FluidNodes
            .Where(node => localizations.ContainsKey(node.Id))
            .OrderBy(static node => node.Id, StringComparer.Ordinal)
            .Select(node => AnalyzeNode(node, localizations[node.Id]))
            .ToArray();

        return new ThermodynamicInverseBranchSelectionReport(nodes);
    }

    private ThermodynamicInverseBranchNodeDiagnosis AnalyzeNode(
        FluidNodeState node,
        ThermodynamicSwitchingNodeLocalization localization)
    {
        var probes = new[]
        {
            DiagnoseProbe(node, localization.Nominal),
            DiagnoseProbe(node, localization.EnergyMinus),
            DiagnoseProbe(node, localization.EnergyPlus),
            DiagnoseProbe(node, localization.MassMinus),
            DiagnoseProbe(node, localization.MassPlus),
        };
        var allOverlap = probes.All(static item => item.MultiplePhaseRootsAvailable);
        var coarseToggle = probes.Any(static item => item.CoarseSaturatedRootFound)
            && probes.Any(static item => !item.CoarseSaturatedRootFound);
        var lateShadowCount = probes.Count(static item => item.LateBoundarySaturatedShadowedByEarlierSuperheated);
        var previousStateTieBreak = probes.Any(static item => item.PreviousStateSelectionSensitive);
        var mechanism = ClassifyMechanism(allOverlap, coarseToggle, lateShadowCount, previousStateTieBreak);
        var policy = mechanism == "overlapping-roots+coarse-saturated-detection+fixed-priority-no-hysteresis"
            ? $"shadow-continuity-hysteresis-hold-{localization.Nominal.Phase}"
            : "diagnose-before-policy";

        return new ThermodynamicInverseBranchNodeDiagnosis(
            node.Id,
            localization.Nominal.Phase,
            allOverlap,
            coarseToggle,
            lateShadowCount,
            previousStateTieBreak,
            mechanism,
            policy,
            probes);
    }

    private ThermodynamicInverseBranchProbeDiagnosis DiagnoseProbe(
        FluidNodeState node,
        ThermodynamicSwitchingProbePoint probe)
    {
        var inventory = new FluidNodeInventory(
            Mass.FromKilograms(probe.MassKilograms),
            Energy.FromJoules(probe.InternalEnergyJoules));
        var diagnostic = _diagnosticProvider.DiagnoseInverseBranchSelection(
            node.Definition,
            inventory,
            node.Thermodynamics);
        var saturatedPrevious = new FluidThermodynamicState(
            node.Pressure,
            node.Temperature,
            FluidPhase.SaturatedMixture,
            VaporQuality.FromFraction(0.5d));
        var superheatedPrevious = new FluidThermodynamicState(
            node.Pressure,
            node.Temperature,
            FluidPhase.SuperheatedVapor,
            null);
        var resolvedFromSaturated = _thermodynamicModel.Resolve(node.Definition, inventory, saturatedPrevious);
        var resolvedFromSuperheated = _thermodynamicModel.Resolve(node.Definition, inventory, superheatedPrevious);
        var previousSensitive = resolvedFromSaturated.Phase != resolvedFromSuperheated.Phase
            || Math.Abs(resolvedFromSaturated.Pressure.Pascals - resolvedFromSuperheated.Pressure.Pascals) > 1e-9d
            || Math.Abs(resolvedFromSaturated.Temperature.Kelvins - resolvedFromSuperheated.Temperature.Kelvins) > 1e-12d;

        return new ThermodynamicInverseBranchProbeDiagnosis(
            node.Id,
            probe.Label,
            probe.MassKilograms,
            probe.InternalEnergyJoules,
            probe.Phase,
            diagnostic.ProductionSelectedBranch,
            diagnostic.ProductionSelectedPhase,
            diagnostic.SaturatedRootAvailable,
            diagnostic.SuperheatedRootAvailable,
            diagnostic.MultiplePhaseRootsAvailable,
            diagnostic.CoarseSaturatedRootFound,
            diagnostic.BoundaryAwareSaturatedRootFound,
            diagnostic.CoarseSuperheatedRootFound,
            diagnostic.BoundaryAwareSuperheatedRootFound,
            diagnostic.LateBoundarySaturatedShadowedByEarlierSuperheated,
            previousSensitive,
            diagnostic.Candidates);
    }

    private static string ClassifyMechanism(
        bool allOverlap,
        bool coarseToggle,
        int lateShadowCount,
        bool previousStateTieBreak)
    {
        if (allOverlap && coarseToggle && lateShadowCount > 0 && !previousStateTieBreak)
        {
            return "overlapping-roots+coarse-saturated-detection+fixed-priority-no-hysteresis";
        }

        if (allOverlap && !previousStateTieBreak)
        {
            return "overlapping-roots+fixed-priority-no-hysteresis";
        }

        if (coarseToggle && lateShadowCount > 0)
        {
            return "coarse-saturated-detection+late-boundary-shadow";
        }

        return "unclassified";
    }
}
