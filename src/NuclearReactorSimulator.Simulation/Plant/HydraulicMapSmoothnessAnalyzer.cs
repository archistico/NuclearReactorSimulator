using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Plant;
using NuclearReactorSimulator.Simulation.Physics.Fluids;

namespace NuclearReactorSimulator.Simulation.Plant;

/// <summary>
/// Phase H.10 shadow-only diagnostic for local switching and non-smoothness in the already validated
/// hydraulic map. It does not solve or commit a candidate. Law-local pressure probes isolate pipe/valve/pump
/// branch structure, while conserved inventory probes detect thermodynamic phase/envelope switching.
/// </summary>
public sealed class HydraulicMapSmoothnessAnalyzer
{
    private readonly IFluidThermodynamicModel _thermodynamicModel;
    private readonly PipeFlowSolver _pipeFlowSolver = new();
    private readonly ValveFlowSolver _valveFlowSolver = new();
    private readonly PumpFlowSolver _pumpFlowSolver = new();

    public HydraulicMapSmoothnessAnalyzer(IFluidThermodynamicModel thermodynamicModel)
    {
        _thermodynamicModel = thermodynamicModel ?? throw new ArgumentNullException(nameof(thermodynamicModel));
    }

    public HydraulicMapSmoothnessReport Analyze(
        PlantState state,
        HydraulicMapSmoothnessProbeOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(state);
        options ??= HydraulicMapSmoothnessProbeOptions.H10AuditDefault;

        var nodes = state.FluidNodes.ToDictionary(static item => item.Id, StringComparer.Ordinal);
        var valveStates = state.Valves.ToDictionary(static item => item.ValveId, StringComparer.Ordinal);
        var pumpStates = state.Pumps.ToDictionary(static item => item.PumpId, StringComparer.Ordinal);
        var paths = new List<HydraulicPathSmoothnessProbe>();

        foreach (var pipe in state.Definition.Pipes.OrderBy(static item => item.Id, StringComparer.Ordinal))
        {
            var from = nodes[pipe.FromNodeId];
            var to = nodes[pipe.ToNodeId];
            paths.Add(AnalyzePath(
                "pipe",
                pipe.Id,
                pipe.FromNodeId,
                pipe.ToNodeId,
                from,
                to,
                static (self, definition, _, left, right) => self._pipeFlowSolver.Solve((PipeDefinition)definition, left, right).MassFlowRate.KilogramsPerSecond,
                pipe,
                null,
                (self, definition, _, left, right) => ClassifyPassiveBranch(
                    left.Pressure.Pascals - right.Pressure.Pascals,
                    self._pipeFlowSolver.Solve((PipeDefinition)definition, left, right).MassFlowRate.KilogramsPerSecond),
                from.Pressure.Pascals - to.Pressure.Pascals,
                options));
        }

        foreach (var valve in state.Definition.Valves.OrderBy(static item => item.Id, StringComparer.Ordinal))
        {
            var valveState = valveStates[valve.Id];
            var from = nodes[valve.Pipe.FromNodeId];
            var to = nodes[valve.Pipe.ToNodeId];
            var baseResult = _valveFlowSolver.Solve(valve, valveState, from, to);
            paths.Add(AnalyzePath(
                "valve",
                valve.Id,
                valve.Pipe.FromNodeId,
                valve.Pipe.ToNodeId,
                from,
                to,
                static (self, definition, componentState, left, right) => self._valveFlowSolver.Solve(
                    (ValveDefinition)definition,
                    (ValveState)componentState!,
                    left,
                    right).MassFlowRate.KilogramsPerSecond,
                valve,
                valveState,
                (self, definition, componentState, left, right) =>
                {
                    var result = self._valveFlowSolver.Solve((ValveDefinition)definition, (ValveState)componentState!, left, right);
                    if (result.FlowCoefficient.IsClosed)
                    {
                        return "closed";
                    }

                    return ClassifyPassiveBranch(
                        left.Pressure.Pascals - right.Pressure.Pascals,
                        result.MassFlowRate.KilogramsPerSecond);
                },
                baseResult.PressureDifference.Pascals,
                options));
        }

        foreach (var pump in state.Definition.Pumps.OrderBy(static item => item.Id, StringComparer.Ordinal))
        {
            var pumpState = pumpStates[pump.Id];
            var from = nodes[pump.Pipe.FromNodeId];
            var to = nodes[pump.Pipe.ToNodeId];
            var baseResult = _pumpFlowSolver.Solve(pump, pumpState, from, to);
            paths.Add(AnalyzePath(
                "pump",
                pump.Id,
                pump.Pipe.FromNodeId,
                pump.Pipe.ToNodeId,
                from,
                to,
                static (self, definition, componentState, left, right) => self._pumpFlowSolver.Solve(
                    (PumpDefinition)definition,
                    (PumpState)componentState!,
                    left,
                    right).MassFlowRate.KilogramsPerSecond,
                pump,
                pumpState,
                (self, definition, componentState, left, right) =>
                {
                    var typedDefinition = (PumpDefinition)definition;
                    var result = self._pumpFlowSolver.Solve(typedDefinition, (PumpState)componentState!, left, right);
                    var driving = (left.Pressure.Pascals - right.Pressure.Pascals) + result.ActivePressureBoost.Pascals;
                    if (typedDefinition.HasDischargeCheckValve && driving < 0d && result.MassFlowRate == MassFlowRate.Zero)
                    {
                        return "check-blocked";
                    }

                    return ClassifyPassiveBranch(driving, result.MassFlowRate.KilogramsPerSecond);
                },
                baseResult.NodePressureDifference.Pascals + baseResult.ActivePressureBoost.Pascals,
                options));
        }

        var thermodynamicNodes = state.FluidNodes
            .OrderBy(static item => item.Id, StringComparer.Ordinal)
            .Select(node => AnalyzeThermodynamicNode(node, options))
            .ToArray();

        return new HydraulicMapSmoothnessReport(paths.ToArray(), thermodynamicNodes);
    }

    private HydraulicPathSmoothnessProbe AnalyzePath(
        string componentKind,
        string componentId,
        string fromNodeId,
        string toNodeId,
        FluidNodeState from,
        FluidNodeState to,
        Func<HydraulicMapSmoothnessAnalyzer, object, object?, FluidNodeState, FluidNodeState, double> solveFlow,
        object definition,
        object? componentState,
        Func<HydraulicMapSmoothnessAnalyzer, object, object?, FluidNodeState, FluidNodeState, string> classifyBranch,
        double baseDrivingPressurePascals,
        HydraulicMapSmoothnessProbeOptions options)
    {
        var coarseStep = PressureProbeStep(from.Pressure.Pascals, options.RelativePressureProbe);
        var fineStep = coarseStep * options.FineProbeFactor;
        var coarseMinus = WithPressure(from, from.Pressure.Pascals - coarseStep);
        var coarsePlus = WithPressure(from, from.Pressure.Pascals + coarseStep);
        var fineMinus = WithPressure(from, from.Pressure.Pascals - fineStep);
        var finePlus = WithPressure(from, from.Pressure.Pascals + fineStep);

        var baseFlow = solveFlow(this, definition, componentState, from, to);
        var coarseMinusFlow = solveFlow(this, definition, componentState, coarseMinus, to);
        var coarsePlusFlow = solveFlow(this, definition, componentState, coarsePlus, to);
        var fineMinusFlow = solveFlow(this, definition, componentState, fineMinus, to);
        var finePlusFlow = solveFlow(this, definition, componentState, finePlus, to);

        var coarseCentralSlope = (coarsePlusFlow - coarseMinusFlow) / (2d * coarseStep);
        var fineCentralSlope = (finePlusFlow - fineMinusFlow) / (2d * fineStep);
        var derivativeScaleGrowth = ScaleGrowth(coarseCentralSlope, fineCentralSlope);
        var coarseLeftSlope = (baseFlow - coarseMinusFlow) / coarseStep;
        var coarseRightSlope = (coarsePlusFlow - baseFlow) / coarseStep;
        var oneSidedSlopeAsymmetry = RelativeDifference(coarseLeftSlope, coarseRightSlope);

        var baseBranch = classifyBranch(this, definition, componentState, from, to);
        var coarseMinusBranch = classifyBranch(this, definition, componentState, coarseMinus, to);
        var coarsePlusBranch = classifyBranch(this, definition, componentState, coarsePlus, to);
        var fineMinusBranch = classifyBranch(this, definition, componentState, fineMinus, to);
        var finePlusBranch = classifyBranch(this, definition, componentState, finePlus, to);
        var branchSwitch = !string.Equals(baseBranch, coarseMinusBranch, StringComparison.Ordinal)
            || !string.Equals(baseBranch, coarsePlusBranch, StringComparison.Ordinal)
            || !string.Equals(baseBranch, fineMinusBranch, StringComparison.Ordinal)
            || !string.Equals(baseBranch, finePlusBranch, StringComparison.Ordinal);
        var nonSmooth = branchSwitch
            || derivativeScaleGrowth >= options.DerivativeScaleGrowthThreshold
            || oneSidedSlopeAsymmetry >= options.OneSidedSlopeAsymmetryThreshold;

        return new HydraulicPathSmoothnessProbe(
            componentKind,
            componentId,
            fromNodeId,
            toNodeId,
            baseBranch,
            coarseMinusBranch,
            coarsePlusBranch,
            fineMinusBranch,
            finePlusBranch,
            baseDrivingPressurePascals,
            coarseStep,
            baseFlow,
            coarseMinusFlow,
            coarsePlusFlow,
            fineMinusFlow,
            finePlusFlow,
            coarseCentralSlope,
            fineCentralSlope,
            derivativeScaleGrowth,
            oneSidedSlopeAsymmetry,
            branchSwitch,
            nonSmooth);
    }

    private ThermodynamicNodeSmoothnessProbe AnalyzeThermodynamicNode(
        FluidNodeState node,
        HydraulicMapSmoothnessProbeOptions options)
    {
        var energyStep = Math.Max(Math.Abs(node.InternalEnergy.Joules) * options.RelativeInventoryProbe, 1d);
        var massStep = Math.Min(
            Math.Max(Math.Abs(node.Mass.Kilograms) * options.RelativeInventoryProbe, 1e-9d),
            node.Mass.Kilograms * 0.25d);
        var fineEnergyStep = energyStep * options.FineProbeFactor;
        var fineMassStep = massStep * options.FineProbeFactor;

        var energyMinus = TryResolve(node, node.Mass.Kilograms, node.InternalEnergy.Joules - energyStep);
        var energyPlus = TryResolve(node, node.Mass.Kilograms, node.InternalEnergy.Joules + energyStep);
        var energyFineMinus = TryResolve(node, node.Mass.Kilograms, node.InternalEnergy.Joules - fineEnergyStep);
        var energyFinePlus = TryResolve(node, node.Mass.Kilograms, node.InternalEnergy.Joules + fineEnergyStep);
        var massMinus = TryResolve(node, node.Mass.Kilograms - massStep, node.InternalEnergy.Joules);
        var massPlus = TryResolve(node, node.Mass.Kilograms + massStep, node.InternalEnergy.Joules);
        var massFineMinus = TryResolve(node, node.Mass.Kilograms - fineMassStep, node.InternalEnergy.Joules);
        var massFinePlus = TryResolve(node, node.Mass.Kilograms + fineMassStep, node.InternalEnergy.Joules);

        var energyGrowth = DerivativeScaleGrowth(
            energyMinus,
            energyPlus,
            energyStep,
            energyFineMinus,
            energyFinePlus,
            fineEnergyStep);
        var massGrowth = DerivativeScaleGrowth(
            massMinus,
            massPlus,
            massStep,
            massFineMinus,
            massFinePlus,
            fineMassStep);
        var basePhase = node.Phase.ToString();
        var phaseOrEnvelopeSwitch = IsPhaseOrEnvelopeSwitch(basePhase, energyMinus, energyPlus, energyFineMinus, energyFinePlus)
            || IsPhaseOrEnvelopeSwitch(basePhase, massMinus, massPlus, massFineMinus, massFinePlus);
        var nonSmooth = phaseOrEnvelopeSwitch
            || energyGrowth >= options.DerivativeScaleGrowthThreshold
            || massGrowth >= options.DerivativeScaleGrowthThreshold;

        return new ThermodynamicNodeSmoothnessProbe(
            node.Id,
            basePhase,
            PhaseLabel(energyMinus),
            PhaseLabel(energyPlus),
            PhaseLabel(massMinus),
            PhaseLabel(massPlus),
            energyMinus.Resolved,
            energyPlus.Resolved,
            massMinus.Resolved,
            massPlus.Resolved,
            node.Pressure.Pascals,
            energyGrowth,
            massGrowth,
            phaseOrEnvelopeSwitch,
            nonSmooth);
    }

    private ThermodynamicProbe TryResolve(FluidNodeState node, double massKilograms, double internalEnergyJoules)
    {
        if (!double.IsFinite(massKilograms) || massKilograms <= 0d || !double.IsFinite(internalEnergyJoules))
        {
            return ThermodynamicProbe.Unresolved;
        }

        try
        {
            var inventory = new FluidNodeInventory(
                Mass.FromKilograms(massKilograms),
                Energy.FromJoules(internalEnergyJoules));
            var resolved = _thermodynamicModel.Resolve(node.Definition, inventory, node.Thermodynamics);
            return resolved is null
                ? ThermodynamicProbe.Unresolved
                : new ThermodynamicProbe(true, resolved.Phase.ToString(), resolved.Pressure.Pascals);
        }
        catch (WaterSteamStateOutOfRangeException)
        {
            return ThermodynamicProbe.Unresolved;
        }
        catch (ArgumentOutOfRangeException)
        {
            return ThermodynamicProbe.Unresolved;
        }
        catch (ArithmeticException)
        {
            return ThermodynamicProbe.Unresolved;
        }
    }

    private static double DerivativeScaleGrowth(
        ThermodynamicProbe coarseMinus,
        ThermodynamicProbe coarsePlus,
        double coarseStep,
        ThermodynamicProbe fineMinus,
        ThermodynamicProbe finePlus,
        double fineStep)
    {
        if (!coarseMinus.Resolved || !coarsePlus.Resolved || !fineMinus.Resolved || !finePlus.Resolved)
        {
            return 0d;
        }

        var coarseSlope = (coarsePlus.PressurePascals - coarseMinus.PressurePascals) / (2d * coarseStep);
        var fineSlope = (finePlus.PressurePascals - fineMinus.PressurePascals) / (2d * fineStep);
        return ScaleGrowth(coarseSlope, fineSlope);
    }

    private static bool IsPhaseOrEnvelopeSwitch(string basePhase, params ThermodynamicProbe[] probes)
        => probes.Any(item => !item.Resolved || !string.Equals(basePhase, item.Phase, StringComparison.Ordinal));

    private static string PhaseLabel(ThermodynamicProbe probe) => probe.Resolved ? probe.Phase : "out-of-range";

    private static FluidNodeState WithPressure(FluidNodeState node, double pressurePascals)
    {
        if (!double.IsFinite(pressurePascals) || pressurePascals <= 0d)
        {
            throw new ArgumentOutOfRangeException(nameof(pressurePascals), pressurePascals, "Diagnostic pressure probe must remain positive and finite.");
        }

        var thermodynamics = new FluidThermodynamicState(
            Pressure.FromPascals(pressurePascals),
            node.Temperature,
            node.Phase,
            node.VaporQuality);
        return new FluidNodeState(node.Definition, node.Inventory, thermodynamics);
    }

    private static double PressureProbeStep(double pressurePascals, double relativeProbe)
    {
        var step = Math.Max(Math.Abs(pressurePascals) * relativeProbe, 1d);
        return Math.Min(step, pressurePascals * 0.25d);
    }

    private static string ClassifyPassiveBranch(double drivingPressurePascals, double massFlowKilogramsPerSecond)
    {
        if (drivingPressurePascals == 0d || massFlowKilogramsPerSecond == 0d)
        {
            return "zero";
        }

        return massFlowKilogramsPerSecond > 0d ? "forward" : "reverse";
    }

    private static double ScaleGrowth(double coarseSlope, double fineSlope)
    {
        var coarseMagnitude = Math.Abs(coarseSlope);
        var fineMagnitude = Math.Abs(fineSlope);
        var scale = Math.Max(coarseMagnitude, 1e-30d);
        return fineMagnitude / scale;
    }

    private static double RelativeDifference(double left, double right)
    {
        var scale = Math.Max(Math.Max(Math.Abs(left), Math.Abs(right)), 1e-30d);
        return Math.Abs(right - left) / scale;
    }

    private sealed record ThermodynamicProbe(bool Resolved, string Phase, double PressurePascals)
    {
        public static ThermodynamicProbe Unresolved { get; } = new(false, "out-of-range", 0d);
    }
}
