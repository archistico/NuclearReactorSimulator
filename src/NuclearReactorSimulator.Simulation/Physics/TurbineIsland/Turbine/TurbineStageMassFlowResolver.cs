using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.TurbineIsland.Turbine;
using NuclearReactorSimulator.Domain.Plant;
using NuclearReactorSimulator.Simulation.Physics.Fluids;

namespace NuclearReactorSimulator.Simulation.Physics.TurbineIsland.Turbine;

/// <summary>
/// Canonical M4/M5 turbine-stage flow resolver. Current stage definitions may opt into a pressure-driven
/// turbine-inlet-to-exhaust hydraulic law; definitions without an expansion resistance retain the historical upstream
/// valve-minimum law only for explicit legacy compatibility.
/// </summary>
internal sealed class TurbineStageMassFlowResolver
{
    private readonly TurbineExpansionSystemDefinition _definition;
    private readonly ValveFlowSolver _valveFlowSolver = new();

    public TurbineStageMassFlowResolver(TurbineExpansionSystemDefinition definition)
    {
        _definition = definition ?? throw new ArgumentNullException(nameof(definition));
    }

    public MassFlowRate Resolve(PlantState state, TurbineStageGroupDefinition stage, TimeSpan deltaTime)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(stage);
        if (deltaTime <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(deltaTime));
        }

        return stage.ExpansionResistance is { } resistance
            ? ResolvePressureDrivenExpansionFlow(state, stage, resistance, deltaTime)
            : ResolveLegacyUpstreamMinimum(state, stage.AdmissionBoundaryId);
    }

    private MassFlowRate ResolvePressureDrivenExpansionFlow(
        PlantState state,
        TurbineStageGroupDefinition stage,
        QuadraticHydraulicResistance resistance,
        TimeSpan deltaTime)
    {
        var boundary = _definition.MainSteamNetwork.GetTurbineAdmissionBoundary(stage.AdmissionBoundaryId);
        var inlet = state.GetFluidNode(boundary.SourceNodeId);
        var exhaust = state.GetFluidNode(stage.ExhaustNodeId);
        var drivingPascals = inlet.Pressure.Pascals - exhaust.Pressure.Pascals;
        if (drivingPascals <= 0d)
        {
            return MassFlowRate.Zero;
        }

        var hydraulicKilogramsPerSecond = Math.Sqrt(
            drivingPascals / resistance.PascalSecondsSquaredPerKilogramSquared);

        // Keep the source-term drain bounded even under extreme transient pressure differentials. This guard does not set
        // the steady-state flow; it only prevents one fixed step from removing more than half of the committed inlet mass.
        var drainableKilogramsPerSecond = 0.5d * inlet.Mass.Kilograms / deltaTime.TotalSeconds;
        return MassFlowRate.FromKilogramsPerSecond(
            Math.Min(hydraulicKilogramsPerSecond, drainableKilogramsPerSecond));
    }

    private MassFlowRate ResolveLegacyUpstreamMinimum(PlantState state, string admissionBoundaryId)
    {
        var mainSteam = _definition.MainSteamNetwork;
        var boundary = mainSteam.GetTurbineAdmissionBoundary(admissionBoundaryId);
        var train = mainSteam.GetAdmissionTrain(boundary.AdmissionTrainId);
        var stopFlow = SolvePositiveValveFlow(state, train.StopValveId);
        var controlFlow = SolvePositiveValveFlow(state, train.ControlValveId);
        var admissionFlow = SolvePositiveValveFlow(state, train.AdmissionValveId);
        return MassFlowRate.FromKilogramsPerSecond(Math.Min(stopFlow, Math.Min(controlFlow, admissionFlow)));
    }

    private double SolvePositiveValveFlow(PlantState state, string valveId)
    {
        var definition = state.Definition.GetValve(valveId);
        var flow = _valveFlowSolver.Solve(
            definition,
            state.GetValve(valveId),
            state.GetFluidNode(definition.Pipe.FromNodeId),
            state.GetFluidNode(definition.Pipe.ToNodeId));
        return Math.Max(0d, flow.MassFlowRate.KilogramsPerSecond);
    }
}
