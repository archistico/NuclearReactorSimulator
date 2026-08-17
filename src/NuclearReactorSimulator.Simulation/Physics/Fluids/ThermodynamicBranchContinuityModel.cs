using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Physics.Quantities;

namespace NuclearReactorSimulator.Simulation.Physics.Fluids;

/// <summary>
/// M10.9.4.1-H.13 shadow-only wrapper that tests deterministic branch continuity policies over the
/// unchanged simplified water/steam inverse map. It never changes the wrapped production resolver.
/// </summary>
public sealed class ThermodynamicBranchContinuityModel : IFluidThermodynamicModel
{
    private readonly IFluidThermodynamicModel _productionModel;
    private readonly IWaterSteamInverseBranchDiagnosticProvider _diagnosticProvider;
    private readonly List<ThermodynamicBranchContinuityDecision> _decisions = new();
    private readonly HashSet<string>? _targetNodeIds;

    public ThermodynamicBranchContinuityModel(
        IFluidThermodynamicModel productionModel,
        IWaterSteamInverseBranchDiagnosticProvider diagnosticProvider,
        ThermodynamicBranchContinuityOptions options,
        IEnumerable<string>? targetNodeIds = null)
    {
        _productionModel = productionModel ?? throw new ArgumentNullException(nameof(productionModel));
        _diagnosticProvider = diagnosticProvider ?? throw new ArgumentNullException(nameof(diagnosticProvider));
        Options = options ?? throw new ArgumentNullException(nameof(options));
        _targetNodeIds = targetNodeIds is null
            ? null
            : new HashSet<string>(targetNodeIds, StringComparer.Ordinal);
    }

    public ThermodynamicBranchContinuityOptions Options { get; }

    public IReadOnlyList<ThermodynamicBranchContinuityDecision> Decisions => _decisions;

    public FluidThermodynamicState Resolve(
        FluidNodeDefinition definition,
        FluidNodeInventory inventory,
        FluidThermodynamicState previousState)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(inventory);
        ArgumentNullException.ThrowIfNull(previousState);

        var production = _productionModel.Resolve(definition, inventory, previousState);
        if (_targetNodeIds is not null && !_targetNodeIds.Contains(definition.Id))
        {
            return production;
        }

        if (Options.Policy == ThermodynamicBranchContinuityPolicy.ProductionControl)
        {
            RecordDecision(
                definition.Id,
                previousState,
                production,
                production,
                "production-control",
                multipleRoots: false,
                previousPhaseRootFound: false,
                previousPhasePressureDrift: double.NaN,
                previousPhaseTemperatureDriftKelvins: double.NaN);
            return production;
        }

        var diagnostic = _diagnosticProvider.DiagnoseInverseBranchSelection(definition, inventory, previousState);
        if (!diagnostic.MultiplePhaseRootsAvailable
            || previousState.Phase is not (FluidPhase.SaturatedMixture or FluidPhase.SuperheatedVapor))
        {
            RecordDecision(
                definition.Id,
                previousState,
                production,
                production,
                "production-no-overlap",
                diagnostic.MultiplePhaseRootsAvailable,
                previousPhaseRootFound: false,
                previousPhasePressureDrift: double.NaN,
                previousPhaseTemperatureDriftKelvins: double.NaN);
            return production;
        }

        var previousPhaseCandidate = diagnostic.Candidates
            .Where(static candidate => candidate.RootFound)
            .Where(candidate => string.Equals(candidate.Phase, previousState.Phase.ToString(), StringComparison.Ordinal))
            .OrderBy(static candidate => candidate.AttemptOrder)
            .FirstOrDefault();
        if (previousPhaseCandidate is null)
        {
            RecordDecision(
                definition.Id,
                previousState,
                production,
                production,
                "production-previous-phase-root-missing",
                multipleRoots: true,
                previousPhaseRootFound: false,
                previousPhasePressureDrift: double.NaN,
                previousPhaseTemperatureDriftKelvins: double.NaN);
            return production;
        }

        var pressureScale = Math.Max(Math.Abs(previousState.Pressure.Pascals), 1_000d);
        var relativePressureDrift = Math.Abs(previousPhaseCandidate.PressurePascals - previousState.Pressure.Pascals) / pressureScale;
        var temperatureDrift = Math.Abs(previousPhaseCandidate.TemperatureKelvins - previousState.Temperature.Kelvins);
        var selectPreviousPhase = Options.Policy switch
        {
            ThermodynamicBranchContinuityPolicy.PreviousPhaseContinuity => true,
            ThermodynamicBranchContinuityPolicy.BoundedPreviousPhaseHysteresis =>
                relativePressureDrift <= Options.MaximumRelativePressureDrift
                && temperatureDrift <= Options.MaximumTemperatureDriftKelvins,
            _ => false,
        };

        if (!selectPreviousPhase)
        {
            RecordDecision(
                definition.Id,
                previousState,
                production,
                production,
                "production-hysteresis-release",
                multipleRoots: true,
                previousPhaseRootFound: true,
                previousPhasePressureDrift: relativePressureDrift,
                previousPhaseTemperatureDriftKelvins: temperatureDrift);
            return production;
        }

        var selected = ToState(previousPhaseCandidate);
        RecordDecision(
            definition.Id,
            previousState,
            production,
            selected,
            Options.Policy == ThermodynamicBranchContinuityPolicy.PreviousPhaseContinuity
                ? "hold-previous-phase-continuity"
                : "hold-previous-phase-hysteresis",
            multipleRoots: true,
            previousPhaseRootFound: true,
            previousPhasePressureDrift: relativePressureDrift,
            previousPhaseTemperatureDriftKelvins: temperatureDrift);
        return selected;
    }

    public void ClearDecisions() => _decisions.Clear();

    private void RecordDecision(
        string nodeId,
        FluidThermodynamicState previous,
        FluidThermodynamicState production,
        FluidThermodynamicState selected,
        string decisionKind,
        bool multipleRoots,
        bool previousPhaseRootFound,
        double previousPhasePressureDrift,
        double previousPhaseTemperatureDriftKelvins)
    {
        _decisions.Add(new ThermodynamicBranchContinuityDecision(
            _decisions.Count + 1,
            nodeId,
            Options.Policy.ToString(),
            previous.Phase.ToString(),
            production.Phase.ToString(),
            selected.Phase.ToString(),
            decisionKind,
            multipleRoots,
            previousPhaseRootFound,
            previousPhasePressureDrift,
            previousPhaseTemperatureDriftKelvins,
            Math.Abs(selected.Pressure.Pascals - production.Pressure.Pascals),
            Math.Abs(selected.Temperature.Kelvins - production.Temperature.Kelvins)));
    }

    private static FluidThermodynamicState ToState(WaterSteamInverseBranchCandidate candidate)
    {
        var phase = Enum.Parse<FluidPhase>(candidate.Phase, ignoreCase: false);
        VaporQuality? quality = phase == FluidPhase.SaturatedMixture
            ? VaporQuality.FromFraction(candidate.VaporQuality
                ?? throw new InvalidOperationException("A saturated branch candidate must expose vapor quality."))
            : null;
        return new FluidThermodynamicState(
            Pressure.FromPascals(candidate.PressurePascals),
            Temperature.FromKelvins(candidate.TemperatureKelvins),
            phase,
            quality);
    }
}

public enum ThermodynamicBranchContinuityPolicy
{
    ProductionControl = 0,
    PreviousPhaseContinuity = 1,
    BoundedPreviousPhaseHysteresis = 2,
}

public sealed record ThermodynamicBranchContinuityOptions(
    ThermodynamicBranchContinuityPolicy Policy,
    double MaximumRelativePressureDrift,
    double MaximumTemperatureDriftKelvins)
{
    public static ThermodynamicBranchContinuityOptions ProductionControl { get; } = new(
        ThermodynamicBranchContinuityPolicy.ProductionControl,
        MaximumRelativePressureDrift: 0d,
        MaximumTemperatureDriftKelvins: 0d);

    public static ThermodynamicBranchContinuityOptions H13PreviousPhaseContinuity { get; } = new(
        ThermodynamicBranchContinuityPolicy.PreviousPhaseContinuity,
        MaximumRelativePressureDrift: double.PositiveInfinity,
        MaximumTemperatureDriftKelvins: double.PositiveInfinity);

    public static ThermodynamicBranchContinuityOptions H13BoundedHysteresis { get; } = new(
        ThermodynamicBranchContinuityPolicy.BoundedPreviousPhaseHysteresis,
        MaximumRelativePressureDrift: 0.02d,
        MaximumTemperatureDriftKelvins: 5d);
}

public sealed record ThermodynamicBranchContinuityDecision(
    int Sequence,
    string NodeId,
    string Policy,
    string PreviousPhase,
    string ProductionPhase,
    string SelectedPhase,
    string DecisionKind,
    bool MultiplePhaseRootsAvailable,
    bool PreviousPhaseRootFound,
    double PreviousPhaseRelativePressureDrift,
    double PreviousPhaseTemperatureDriftKelvins,
    double AvoidedProductionPressureDifferencePascals,
    double AvoidedProductionTemperatureDifferenceKelvins)
{
    public bool SelectionDiffersFromProduction => !string.Equals(ProductionPhase, SelectedPhase, StringComparison.Ordinal);

    public bool SelectedPreviousPhase => string.Equals(PreviousPhase, SelectedPhase, StringComparison.Ordinal);
}
