using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.Json;
using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.ControlRoom.Automation;
using NuclearReactorSimulator.Application.Scenarios.Training;
using NuclearReactorSimulator.Domain.Physics.Control.Supervisory;
using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Simulation.Physics.Control.Integration;
using NuclearReactorSimulator.Simulation.Physics.Fluids;
using NuclearReactorSimulator.Simulation.Tests.Physics.Fluids.Reference;
using Xunit;

namespace NuclearReactorSimulator.Simulation.Tests.Physics.Fluids;

/// <summary>
/// VR2 Replanning / Materiality Diagnostic 1. Observation-only replay of the validated P1B exact-v9
/// 5 MWe background and 5->6 MWe path. It never substitutes IF97 pressures into runtime state. Instead,
/// it reinterprets the same committed node inventories with the independent test-only IF97 helper and
/// evaluates pressure-only counterfactuals through the already-existing quadratic hydraulic laws.
/// </summary>
public sealed class M10FinalPhysicalReferenceVr2MaterialityDiagnosticTests
{
    private const string OptInEnvironmentVariable = "NRS_M10_FINAL_PHYSICAL_REFERENCE_VR2_MATERIALITY_DIAGNOSTIC";
    private const string ArtifactDirectoryRelative = "artifacts/m10-final-physical-reference-vr2-materiality-diagnostic1";
    private const string P1BContractFileName = "m10-final-replacement-long-closure-plan1-p1b-contract.json";
    private const int SampleSeconds = 60;
    private const int BackgroundSeconds = 600;
    private const int MaximumHoldSeconds = 3600;
    private const int LateAnalysisSeconds = 1200;
    private const int LateWindowSeconds = 300;
    private const double FlowComparisonFloorKilogramsPerSecond = 0.01d;
    private const double ConfirmedImpactRatio = 1d;
    private const double NotExcludedImpactRatio = 0.1d;
    private const int RequiredPersistentWindows = 3;
    private const double HydraulicFlowReproductionToleranceKilogramsPerSecond = 1e-9d;
    private const double ReferenceInverseSelfCheckMaximumRelativeError = 1e-7d;

    private static readonly string[] RequiredNodeIds = ["suction", "pressure", "outlet", "drum", "feedwater-inventory"];
    private static readonly string[] RequiredPathIds = ["MCP", "CHANNEL", "RETURN", "FEEDWATER-PUMP"];
    private static readonly UTF8Encoding Utf8WithoutBom = new(encoderShouldEmitUTF8Identifier: false);

    [Fact(Explicit = true)]
    [Trait("Category", "M10FinalPhysicalReferenceVR2MaterialityDiagnostic1")]
    public void ExactV9_CompressedLiquidPressureDiscrepancy_IsMappedToCanonicalHydraulicMateriality()
    {
        RequireOptIn();
        var artifactDirectory = ResetArtifactDirectory();
        var contract = LoadP1BContract();
        ValidateP1BContract(contract);

        var inverseSelfCheck = ValidateInverseReferenceHelper();
        WriteContractAndProvenance(artifactDirectory, inverseSelfCheck);
        Assert.True(
            inverseSelfCheck <= ReferenceInverseSelfCheckMaximumRelativeError,
            $"VR2 materiality IF97 inverse self-check exceeded {ReferenceInverseSelfCheckMaximumRelativeError:R}: {inverseSelfCheck:R}.");

        var evidence = new EvidenceStore();
        var background = RunBackground(contract, evidence);
        var load = RunLoadProbe(contract, evidence);
        var windows = BuildLateWindowMateriality(evidence.PathRows);
        var repeatedWindows = BuildLateWindowMateriality(evidence.PathRows);
        var deterministicAnalysisRepeat = windows.SequenceEqual(repeatedWindows);
        var classification = Classify(evidence, windows);
        var maximumFlowReproductionError = evidence.PathRows.Count == 0
            ? double.NaN
            : evidence.PathRows.Max(static row => Math.Abs(row.CanonicalFlowKilogramsPerSecond - row.ProductionFormulaFlowKilogramsPerSecond));

        WriteCheckpointReproduction(artifactDirectory, load.Checkpoints);
        WriteNodeInverseMap(artifactDirectory, evidence.NodeRows);
        WriteHydraulicCounterfactual(artifactDirectory, evidence.PathRows);
        WriteLateWindowMateriality(artifactDirectory, windows);
        WriteSentinels(artifactDirectory, background.Sentinels, load.Sentinels);
        WriteSummary(
            artifactDirectory,
            background,
            load,
            evidence,
            windows,
            classification,
            maximumFlowReproductionError,
            deterministicAnalysisRepeat,
            inverseSelfCheck);

        Assert.True(background.ExecutionPass, background.FailureMessage);
        Assert.True(load.ExecutionPass, load.FailureMessage);
        Assert.Equal(contract.RequiredCheckpoints.Length, load.Checkpoints.Count(static checkpoint => checkpoint.Matches));
        Assert.True(
            maximumFlowReproductionError <= HydraulicFlowReproductionToleranceKilogramsPerSecond,
            $"Pressure-only counterfactual mapping did not reproduce the canonical hydraulic law: max flow error={maximumFlowReproductionError:R} kg/s.");
        Assert.True(deterministicAnalysisRepeat, "VR2 materiality analysis repeat was not deterministic.");
        Assert.Contains(
            classification,
            new[]
            {
                "HYDRAULIC-MATERIALITY-CONFIRMED",
                "HYDRAULIC-MATERIALITY-NOT-EXCLUDED",
                "HYDRAULIC-MATERIALITY-NOT-DEMONSTRATED",
                "REFERENCE-INVERSION-GAP",
            });
    }

    private static RunResult RunBackground(P1BContract contract, EvidenceStore evidence)
    {
        var engine = CreateEngine(loadIncrementMegawatts: 5d);
        engine.RequestPlantControlAuthority(PlantControlAuthorityMode.SupervisoryAutomatic);
        engine.RequestSupervisoryObjective(SupervisoryObjectiveRequest.HoldCurrentOperatingPoint());
        var sentinels = new SentinelSummary();

        try
        {
            CaptureObservation("background-reference", 0d, engine, evidence);
            var totalSteps = checked(BackgroundSeconds * contract.StepsPerSecond);
            var sampleSteps = checked(SampleSeconds * contract.StepsPerSecond);
            for (var step = 1; step <= totalSteps; step++)
            {
                engine.Step(ControlRoomRunState.Running);
                AuditSentinels(engine, sentinels);
                if (sentinels.AnyFailure)
                {
                    return new RunResult(false, "Background reference encountered a protection or numerical sentinel failure.", null, [], sentinels);
                }

                if (step % sampleSteps == 0)
                {
                    CaptureObservation("background-reference", step / (double)contract.StepsPerSecond, engine, evidence);
                }
            }

            return new RunResult(true, string.Empty, null, [], sentinels);
        }
        catch (Exception exception)
        {
            return new RunResult(false, $"{exception.GetType().FullName}: {Flatten(exception.Message)}", null, [], sentinels);
        }
    }

    private static RunResult RunLoadProbe(P1BContract contract, EvidenceStore evidence)
    {
        var engine = CreateEngine(contract.LoadIncrementMegawatts);
        engine.RequestPlantControlAuthority(PlantControlAuthorityMode.SupervisoryAutomatic);
        var initial = engine.CreatePresentationSnapshot(ControlRoomRunState.Paused);
        var generatorId = Assert.Single(initial.Electrical.Generators).GeneratorId;
        var initialRequestedMegawatts = Assert.Single(initial.Electrical.Generators).RequestedElectricalPower.NumericValue
            ?? throw new InvalidOperationException("Initial requested generator load is unavailable.");
        var initialThermalMegawatts = initial.ReactorCore.ReactorThermalPower.NumericValue
            ?? throw new InvalidOperationException("Initial reactor thermal power is unavailable.");
        var targetThermalMegawatts = initialThermalMegawatts / initialRequestedMegawatts * contract.TargetLoadMegawatts;
        var preparationSteps = checked(contract.PreparationTimeoutSeconds * contract.StepsPerSecond);
        var sampleSteps = checked(SampleSeconds * contract.StepsPerSecond);
        var sentinels = new SentinelSummary();
        var checkpoints = new List<CheckpointResult>();
        long? loadCommandStep = null;

        try
        {
            engine.RequestSupervisoryObjective(SupervisoryObjectiveRequest.HoldReactorPower(targetThermalMegawatts * 1_000_000d));
            for (var preparationIndex = 0; preparationIndex < preparationSteps; preparationIndex++)
            {
                var sample = CaptureOperationalSummary(engine, elapsedSeconds: 0d);
                if (IsThermallyReady(sample, targetThermalMegawatts, contract))
                {
                    loadCommandStep = engine.LogicalStep + 1;
                    if (loadCommandStep.Value != contract.ExpectedLoadCommandLogicalStep)
                    {
                        return new RunResult(
                            false,
                            $"Load-command logical step {loadCommandStep.Value} != frozen P1B step {contract.ExpectedLoadCommandLogicalStep}.",
                            loadCommandStep,
                            checkpoints,
                            sentinels);
                    }

                    engine.QueueOperatorCommand(new ControlRoomCommand(
                        ControlRoomCommandKind.GeneratorLoadRaise,
                        generatorId,
                        ControlRoomCommandTargetKind.Generator));
                    break;
                }

                engine.Step(ControlRoomRunState.Running);
                AuditSentinels(engine, sentinels);
                if (sentinels.AnyFailure)
                {
                    return new RunResult(false, "Load probe failed a protection or numerical sentinel during preparation.", null, checkpoints, sentinels);
                }
            }

            if (!loadCommandStep.HasValue)
            {
                return new RunResult(false, "Load probe thermal preparation timed out before the frozen P1B load command.", null, checkpoints, sentinels);
            }

            CaptureObservation("exact-v9-5-to-6mwe", 0d, engine, evidence);
            var maximumHoldSteps = checked(MaximumHoldSeconds * contract.StepsPerSecond);
            for (var holdStep = 1; holdStep <= maximumHoldSteps; holdStep++)
            {
                engine.Step(ControlRoomRunState.Running);
                var elapsed = holdStep / (double)contract.StepsPerSecond;
                AuditSentinels(engine, sentinels);
                if (sentinels.AnyFailure)
                {
                    return new RunResult(false, "Load probe failed a protection or numerical sentinel during hold.", loadCommandStep, checkpoints, sentinels);
                }

                if (holdStep % sampleSteps == 0)
                {
                    CaptureObservation("exact-v9-5-to-6mwe", elapsed, engine, evidence);
                }

                foreach (var checkpoint in contract.RequiredCheckpoints)
                {
                    if (holdStep != checked(checkpoint.HoldSeconds * contract.StepsPerSecond))
                    {
                        continue;
                    }

                    var summary = CaptureOperationalSummary(engine, elapsed);
                    checkpoints.Add(new CheckpointResult(
                        checkpoint.HoldSeconds,
                        summary.LogicalStep,
                        CheckpointMatches(summary, checkpoint, contract.CheckpointTolerances),
                        summary));
                }
            }

            if (checkpoints.Count != contract.RequiredCheckpoints.Length || checkpoints.Any(static checkpoint => !checkpoint.Matches))
            {
                return new RunResult(false, "One or more frozen P1B 900/1800/3600 s checkpoints were not reproduced.", loadCommandStep, checkpoints, sentinels);
            }

            return new RunResult(true, string.Empty, loadCommandStep, checkpoints, sentinels);
        }
        catch (Exception exception)
        {
            return new RunResult(false, $"{exception.GetType().FullName}: {Flatten(exception.Message)}", loadCommandStep, checkpoints, sentinels);
        }
    }

    private static void CaptureObservation(string probeId, double elapsedSeconds, IntegratedAutomaticOperationRuntimeEngine engine, EvidenceStore evidence)
    {
        var protectedControl = ReadLatestCanonicalSnapshot(engine).Control.ProtectedControl;
        var primary = protectedControl.FullPlant.IntegratedCycle.PrimaryCircuit;
        var plant = protectedControl.FullPlant.CandidatePlant;
        var nodeReferences = new Dictionary<string, NodeReference>(StringComparer.Ordinal);

        foreach (var nodeId in RequiredNodeIds)
        {
            var node = plant.GetFluidNode(nodeId);
            var reference = ResolveIndependentReference(node);
            nodeReferences.Add(nodeId, reference);
            evidence.NodeRows.Add(new NodeRow(
                probeId,
                engine.LogicalStep,
                elapsedSeconds,
                node.Id,
                node.Phase.ToString(),
                node.VaporQuality?.Fraction,
                node.Density.KilogramsPerCubicMetre,
                node.SpecificInternalEnergy.JoulesPerKilogram,
                node.Temperature.DegreesCelsius,
                node.Pressure.Megapascals,
                reference.Resolved,
                reference.Region,
                reference.Phase,
                reference.TemperatureCelsius,
                reference.PressureMegapascals,
                reference.VaporQuality,
                reference.Resolved ? reference.PressureMegapascals - node.Pressure.Megapascals : double.NaN));
        }

        var loop = Assert.Single(primary.MainCirculation.Loops);
        var branch = Assert.Single(loop.Branches);
        var pumpSnapshot = loop.GetPump("pump");
        var mainPumpDefinition = plant.Definition.GetPump("pump");
        AddPumpPath(
            evidence.PathRows,
            probeId,
            engine.LogicalStep,
            elapsedSeconds,
            "MCP",
            "suction",
            "pressure",
            plant,
            nodeReferences,
            mainPumpDefinition,
            pumpSnapshot.MassFlowRate.KilogramsPerSecond);

        AddPipePath(
            evidence.PathRows,
            probeId,
            engine.LogicalStep,
            elapsedSeconds,
            "CHANNEL",
            "pressure",
            "outlet",
            plant,
            nodeReferences,
            plant.Definition.GetPipe(branch.ChannelPipeId),
            branch.ChannelMassFlowRate.KilogramsPerSecond);

        AddPipePath(
            evidence.PathRows,
            probeId,
            engine.LogicalStep,
            elapsedSeconds,
            "RETURN",
            "outlet",
            "drum",
            plant,
            nodeReferences,
            plant.Definition.GetPipe(branch.ReturnPipeId),
            branch.ReturnMassFlowRate.KilogramsPerSecond);

        var feedwaterPump = plant.Definition.GetPump("feedwater-pump");
        var feedwaterState = plant.GetPump("feedwater-pump");
        var feedwaterFrom = plant.GetFluidNode("feedwater-inventory");
        var feedwaterTo = plant.GetFluidNode("drum");
        var feedwaterCanonical = new PumpFlowSolver()
            .Solve(feedwaterPump, feedwaterState, feedwaterFrom, feedwaterTo)
            .MassFlowRate
            .KilogramsPerSecond;
        AddPumpPath(
            evidence.PathRows,
            probeId,
            engine.LogicalStep,
            elapsedSeconds,
            "FEEDWATER-PUMP",
            "feedwater-inventory",
            "drum",
            plant,
            nodeReferences,
            feedwaterPump,
            feedwaterCanonical);
    }

    private static NodeReference ResolveIndependentReference(FluidNodeState node)
    {
        var specificVolume = node.Volume.CubicMetres / node.Mass.Kilograms;
        var specificInternalEnergy = node.SpecificInternalEnergy.JoulesPerKilogram;
        var preferMixture = node.Phase == FluidPhase.SaturatedMixture;

        if (preferMixture
            && IapwsIf97Reference.TryResolveSaturatedMixtureFromSpecificVolumeAndInternalEnergy(
                specificVolume,
                specificInternalEnergy,
                out var mixture))
        {
            return ToNodeReference(mixture, "SaturatedMixture");
        }

        if (IapwsIf97Reference.TryResolveRegion1FromSpecificVolumeAndInternalEnergy(
                specificVolume,
                specificInternalEnergy,
                out var liquid))
        {
            return ToNodeReference(liquid, "SubcooledLiquid");
        }

        if (!preferMixture
            && IapwsIf97Reference.TryResolveSaturatedMixtureFromSpecificVolumeAndInternalEnergy(
                specificVolume,
                specificInternalEnergy,
                out mixture))
        {
            return ToNodeReference(mixture, "SaturatedMixture");
        }

        return new NodeReference(false, "UNRESOLVED", "Unspecified", double.NaN, double.NaN, null);
    }

    private static NodeReference ToNodeReference(IapwsInverseReferenceState state, string phase)
        => new(
            true,
            state.Region,
            phase,
            state.TemperatureKelvins - 273.15d,
            state.PressureMegapascals,
            state.VaporQuality);

    private static void AddPipePath(
        ICollection<PathRow> rows,
        string probeId,
        long logicalStep,
        double elapsedSeconds,
        string pathId,
        string fromNodeId,
        string toNodeId,
        NuclearReactorSimulator.Simulation.Plant.PlantSnapshot plant,
        IReadOnlyDictionary<string, NodeReference> references,
        PipeDefinition pipe,
        double canonicalFlow)
    {
        var from = plant.GetFluidNode(fromNodeId);
        var to = plant.GetFluidNode(toNodeId);
        var resistance = pipe.Resistance.PascalSecondsSquaredPerKilogramSquared;
        AddPathRow(rows, probeId, logicalStep, elapsedSeconds, pathId, from, to, references, resistance, 0d, false, canonicalFlow);
    }

    private static void AddPumpPath(
        ICollection<PathRow> rows,
        string probeId,
        long logicalStep,
        double elapsedSeconds,
        string pathId,
        string fromNodeId,
        string toNodeId,
        NuclearReactorSimulator.Simulation.Plant.PlantSnapshot plant,
        IReadOnlyDictionary<string, NodeReference> references,
        PumpDefinition pump,
        double canonicalFlow)
    {
        var from = plant.GetFluidNode(fromNodeId);
        var to = plant.GetFluidNode(toNodeId);
        var pumpState = plant.GetPump(pump.Id);
        var effectiveSpeed = pumpState.IsRunning ? pumpState.Speed.Fraction : 0d;
        var activeBoostPascals = pump.RatedPressureBoost.Pascals * effectiveSpeed * effectiveSpeed;
        var resistance = pump.Pipe.Resistance.PascalSecondsSquaredPerKilogramSquared
            + pump.InternalResistance.PascalSecondsSquaredPerKilogramSquared;
        AddPathRow(
            rows,
            probeId,
            logicalStep,
            elapsedSeconds,
            pathId,
            from,
            to,
            references,
            resistance,
            activeBoostPascals,
            pump.HasDischargeCheckValve,
            canonicalFlow);
    }

    private static void AddPathRow(
        ICollection<PathRow> rows,
        string probeId,
        long logicalStep,
        double elapsedSeconds,
        string pathId,
        FluidNodeState from,
        FluidNodeState to,
        IReadOnlyDictionary<string, NodeReference> references,
        double resistance,
        double activeBoostPascals,
        bool hasCheckValve,
        double canonicalFlow)
    {
        var productionDrivingPascals = from.Pressure.Pascals - to.Pressure.Pascals + activeBoostPascals;
        var productionFormulaFlow = SolveQuadraticFlow(productionDrivingPascals, resistance, hasCheckValve);
        var fromReference = references[from.Id];
        var toReference = references[to.Id];
        var resolved = fromReference.Resolved && toReference.Resolved;
        var referenceDrivingPascals = resolved
            ? ((fromReference.PressureMegapascals - toReference.PressureMegapascals) * 1_000_000d) + activeBoostPascals
            : double.NaN;
        var counterfactualFlow = resolved
            ? SolveQuadraticFlow(referenceDrivingPascals, resistance, hasCheckValve)
            : double.NaN;
        var signChanged = resolved
            && Math.Sign(productionDrivingPascals) != Math.Sign(referenceDrivingPascals)
            && Math.Abs(productionDrivingPascals) > 1e-9d
            && Math.Abs(referenceDrivingPascals) > 1e-9d;

        rows.Add(new PathRow(
            probeId,
            logicalStep,
            elapsedSeconds,
            pathId,
            from.Id,
            to.Id,
            resistance,
            activeBoostPascals,
            productionDrivingPascals,
            referenceDrivingPascals,
            canonicalFlow,
            productionFormulaFlow,
            counterfactualFlow,
            resolved,
            signChanged));
    }

    private static double SolveQuadraticFlow(double drivingPressurePascals, double resistance, bool hasCheckValve)
    {
        if (!double.IsFinite(drivingPressurePascals) || !double.IsFinite(resistance) || resistance <= 0d)
        {
            return double.NaN;
        }

        if (Math.Abs(drivingPressurePascals) <= 1e-15d)
        {
            return 0d;
        }

        var magnitude = Math.Sqrt(Math.Abs(drivingPressurePascals) / resistance);
        var signed = drivingPressurePascals > 0d ? magnitude : -magnitude;
        return hasCheckValve && signed < 0d ? 0d : signed;
    }

    private static List<WindowRow> BuildLateWindowMateriality(IReadOnlyList<PathRow> pathRows)
    {
        var output = new List<WindowRow>();
        foreach (var pathId in RequiredPathIds)
        {
            var background = pathRows
                .Where(row => row.PathId == pathId
                    && row.ProbeId == "background-reference"
                    && row.ElapsedSeconds > BackgroundSeconds - LateWindowSeconds
                    && row.ElapsedSeconds <= BackgroundSeconds)
                .ToArray();
            var backgroundMean = background.Length == 0
                ? double.NaN
                : background.Average(static row => row.CanonicalFlowKilogramsPerSecond);

            for (var index = 0; index < 4; index++)
            {
                var start = MaximumHoldSeconds - LateAnalysisSeconds + (index * LateWindowSeconds);
                var end = start + LateWindowSeconds;
                var rows = pathRows
                    .Where(row => row.PathId == pathId
                        && row.ProbeId == "exact-v9-5-to-6mwe"
                        && row.ElapsedSeconds > start
                        && row.ElapsedSeconds <= end)
                    .OrderBy(static row => row.ElapsedSeconds)
                    .ToArray();
                var resolvedRows = rows.Where(static row => row.ReferenceResolved && double.IsFinite(row.CounterfactualFlowKilogramsPerSecond)).ToArray();
                var productionMean = rows.Length == 0 ? double.NaN : rows.Average(static row => row.CanonicalFlowKilogramsPerSecond);
                var counterfactualMean = resolvedRows.Length == 0 ? double.NaN : resolvedRows.Average(static row => row.CounterfactualFlowKilogramsPerSecond);
                var meanAbsoluteCounterfactualShift = resolvedRows.Length == 0
                    ? double.NaN
                    : resolvedRows.Average(static row => Math.Abs(row.CounterfactualFlowKilogramsPerSecond - row.CanonicalFlowKilogramsPerSecond));
                var loadSpecificShift = double.IsFinite(backgroundMean) && double.IsFinite(productionMean)
                    ? Math.Abs(productionMean - backgroundMean)
                    : double.NaN;
                var withinWindowDrift = rows.Length < 2
                    ? double.NaN
                    : Math.Abs(rows[^1].CanonicalFlowKilogramsPerSecond - rows[0].CanonicalFlowKilogramsPerSecond);
                var phenomenonScale = double.IsFinite(loadSpecificShift) && double.IsFinite(withinWindowDrift)
                    ? Math.Max(FlowComparisonFloorKilogramsPerSecond, Math.Max(loadSpecificShift, withinWindowDrift))
                    : double.NaN;
                var impactRatio = double.IsFinite(meanAbsoluteCounterfactualShift) && double.IsFinite(phenomenonScale)
                    ? meanAbsoluteCounterfactualShift / phenomenonScale
                    : double.NaN;
                var signChange = rows.Any(static row => row.DrivingPressureSignChanged);
                var band = !double.IsFinite(impactRatio)
                    ? "REFERENCE-INVERSION-GAP"
                    : signChange || impactRatio >= ConfirmedImpactRatio
                        ? "CONFIRMED"
                        : impactRatio >= NotExcludedImpactRatio
                            ? "NOT-EXCLUDED"
                            : "NOT-DEMONSTRATED";

                output.Add(new WindowRow(
                    pathId,
                    index + 1,
                    start,
                    end,
                    rows.Length,
                    resolvedRows.Length,
                    backgroundMean,
                    productionMean,
                    counterfactualMean,
                    meanAbsoluteCounterfactualShift,
                    loadSpecificShift,
                    withinWindowDrift,
                    phenomenonScale,
                    impactRatio,
                    signChange,
                    band));
            }
        }

        return output;
    }

    private static string Classify(EvidenceStore evidence, IReadOnlyList<WindowRow> windows)
    {
        var requiredPathRows = evidence.PathRows.Where(row => RequiredPathIds.Contains(row.PathId, StringComparer.Ordinal)).ToArray();
        if (requiredPathRows.Any(static row => !row.ReferenceResolved))
        {
            return "REFERENCE-INVERSION-GAP";
        }

        foreach (var pathId in RequiredPathIds)
        {
            var pathWindows = windows.Where(row => row.PathId == pathId).ToArray();
            if (pathWindows.Count(row => row.Band == "CONFIRMED") >= RequiredPersistentWindows)
            {
                return "HYDRAULIC-MATERIALITY-CONFIRMED";
            }
        }

        foreach (var pathId in RequiredPathIds)
        {
            var pathWindows = windows.Where(row => row.PathId == pathId).ToArray();
            if (pathWindows.Count(row => row.Band is "CONFIRMED" or "NOT-EXCLUDED") >= RequiredPersistentWindows)
            {
                return "HYDRAULIC-MATERIALITY-NOT-EXCLUDED";
            }
        }

        return "HYDRAULIC-MATERIALITY-NOT-DEMONSTRATED";
    }

    private static double ValidateInverseReferenceHelper()
    {
        var errors = new List<double>();
        foreach (var point in new[] { (473.15d, 2d), (523.15d, 7d), (553.15d, 10d) })
        {
            var forward = IapwsIf97Reference.Region1(point.Item1, point.Item2);
            Assert.True(
                IapwsIf97Reference.TryResolveRegion1FromSpecificVolumeAndInternalEnergy(
                    forward.SpecificVolumeCubicMetresPerKilogram,
                    forward.SpecificInternalEnergyJoulesPerKilogram,
                    out var inverse),
                $"IF97 Region-1 inverse self-check did not resolve T={point.Item1:R} K, p={point.Item2:R} MPa.");
            errors.Add(RelativeError(point.Item1, inverse.TemperatureKelvins));
            errors.Add(RelativeError(point.Item2, inverse.PressureMegapascals));
        }

        foreach (var point in new[] { (473.15d, 0.25d), (553.15d, 0.50d) })
        {
            var pressure = IapwsIf97Reference.SaturationPressureMegapascals(point.Item1);
            var liquid = IapwsIf97Reference.Region1(point.Item1, pressure);
            var vapor = IapwsIf97Reference.Region2(point.Item1, pressure);
            var volume = liquid.SpecificVolumeCubicMetresPerKilogram
                + point.Item2 * (vapor.SpecificVolumeCubicMetresPerKilogram - liquid.SpecificVolumeCubicMetresPerKilogram);
            var energy = liquid.SpecificInternalEnergyJoulesPerKilogram
                + point.Item2 * (vapor.SpecificInternalEnergyJoulesPerKilogram - liquid.SpecificInternalEnergyJoulesPerKilogram);
            Assert.True(
                IapwsIf97Reference.TryResolveSaturatedMixtureFromSpecificVolumeAndInternalEnergy(volume, energy, out var inverse),
                $"IF97 Region-4 mixture inverse self-check did not resolve T={point.Item1:R} K, quality={point.Item2:R}.");
            errors.Add(RelativeError(point.Item1, inverse.TemperatureKelvins));
            errors.Add(RelativeError(pressure, inverse.PressureMegapascals));
            errors.Add(RelativeError(point.Item2, inverse.VaporQuality ?? double.NaN));
        }

        return errors.Max();
    }

    private static OperationalSummary CaptureOperationalSummary(IntegratedAutomaticOperationRuntimeEngine engine, double elapsedSeconds)
    {
        var protectedControl = ReadLatestCanonicalSnapshot(engine).Control.ProtectedControl;
        var fullPlant = protectedControl.FullPlant;
        var primary = fullPlant.IntegratedCycle.PrimaryCircuit;
        var stage = Assert.Single(fullPlant.IntegratedCycle.TurbineExpansion.StageGroups);
        var rotor = Assert.Single(fullPlant.IntegratedCycle.TurbineExpansion.Rotors);
        var generator = Assert.Single(fullPlant.IntegratedCycle.Generators);
        var generatorDefinition = fullPlant.IntegratedCycle.Definition.GeneratorGridSystem.GetGenerator(generator.GeneratorId);
        var requestedMechanical = generator.RequestedElectricalPower.Megawatts / generatorDefinition.Efficiency.Fraction;
        var dispatchAdequacy = rotor.ShaftPower.Megawatts - rotor.PassiveMechanicalLossPower.Megawatts - requestedMechanical;
        return new OperationalSummary(
            engine.LogicalStep,
            elapsedSeconds,
            generator.ElectricalOutputPower.Megawatts,
            fullPlant.ReactorThermalPower.Megawatts,
            rotor.ShaftPower.Megawatts,
            generator.FinalElectricalFrequency.Hertz,
            dispatchAdequacy,
            stage.EffectiveMassFlowRate.KilogramsPerSecond,
            stage.InletPressure.Megapascals,
            generator.BreakerFinallyClosed,
            protectedControl.Protection.ReactorScramActive,
            protectedControl.Protection.TurbineTripActive,
            protectedControl.Protection.GeneratorTripActive,
            primary.HydraulicNumerics.Converged);
    }

    private static bool IsThermallyReady(OperationalSummary sample, double targetThermalMegawatts, P1BContract contract)
        => !sample.AnyTrip
            && sample.BreakerClosed
            && sample.HydraulicConverged
            && sample.ReactorThermalMegawatts >= targetThermalMegawatts - contract.ThermalReadinessToleranceMegawatts
            && Math.Abs(sample.GeneratorFrequencyHertz - 50d) <= 0.1d;

    private static bool CheckpointMatches(OperationalSummary sample, P1BCheckpoint checkpoint, CheckpointTolerances tolerances)
        => sample.LogicalStep == checkpoint.ExpectedLogicalStep
            && Math.Abs(sample.ElectricalOutputMegawatts - checkpoint.OutputMegawatts) <= tolerances.PowerMegawatts
            && Math.Abs(sample.ReactorThermalMegawatts - checkpoint.ThermalMegawatts) <= tolerances.ThermalMegawatts
            && Math.Abs(sample.TurbineShaftMegawatts - checkpoint.ShaftMegawatts) <= tolerances.PowerMegawatts
            && Math.Abs(sample.GeneratorFrequencyHertz - checkpoint.FrequencyHertz) <= tolerances.FrequencyHertz
            && Math.Abs(sample.DispatchMechanicalAdequacyMegawatts - checkpoint.DispatchAdequacyMegawatts) <= tolerances.PowerMegawatts
            && Math.Abs(sample.TurbineSteamFlowKilogramsPerSecond - checkpoint.FlowKilogramsPerSecond) <= tolerances.FlowKilogramsPerSecond
            && Math.Abs(sample.TurbineInletPressureMegapascals - checkpoint.InletPressureMegapascals) <= tolerances.PressureMegapascals;

    private static IntegratedAutomaticOperationRuntimeEngine CreateEngine(double loadIncrementMegawatts)
    {
        var baseline = Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(
            new DesktopSustainedGenerationPostMoistureEquilibriumCandidateInitialConditionFactory().CreateRuntimeEngine());
        if (Math.Abs(loadIncrementMegawatts - 5d) <= 1e-12d)
        {
            return baseline;
        }

        var solverField = typeof(IntegratedAutomaticOperationRuntimeEngine).GetField("_solver", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("VR2 materiality diagnostic could not locate the private integrated solver field.");
        var solver = solverField.GetValue(baseline) as IntegratedAutomaticOperationSolver
            ?? throw new InvalidOperationException("VR2 materiality diagnostic could not read the integrated solver.");
        var commandPolicy = new ControlRoomRuntimeCommandPolicy(
            ControlRoomRuntimeCommandPolicy.Default.TurbineSpeedSetpointIncrementRpm,
            loadIncrementMegawatts * 1_000_000d);
        return new IntegratedAutomaticOperationRuntimeEngine(
            solver,
            baseline.CurrentState,
            baseline.PersistentInputs,
            ReadLatestCanonicalSnapshot(baseline),
            ReadFixedDeltaTime(baseline),
            baseline.LogicalStep,
            commandPolicy);
    }

    private static IntegratedAutomaticOperationSnapshot ReadLatestCanonicalSnapshot(IntegratedAutomaticOperationRuntimeEngine engine)
    {
        var property = typeof(IntegratedAutomaticOperationRuntimeEngine).GetProperty(
            "LatestCanonicalSnapshot",
            BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("VR2 materiality diagnostic could not locate the internal canonical snapshot evidence seam.");
        return property.GetValue(engine) as IntegratedAutomaticOperationSnapshot
            ?? throw new InvalidOperationException("VR2 materiality diagnostic could not read the internal canonical snapshot evidence seam.");
    }

    private static TimeSpan ReadFixedDeltaTime(IntegratedAutomaticOperationRuntimeEngine engine)
    {
        var property = typeof(IntegratedAutomaticOperationRuntimeEngine).GetProperty(
            "FixedDeltaTime",
            BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("VR2 materiality diagnostic could not locate the internal fixed-delta-time evidence seam.");
        return property.GetValue(engine) is TimeSpan deltaTime
            ? deltaTime
            : throw new InvalidOperationException("VR2 materiality diagnostic could not read the internal fixed-delta-time evidence seam.");
    }

    private static void AuditSentinels(IntegratedAutomaticOperationRuntimeEngine engine, SentinelSummary sentinels)
    {
        var protectedControl = ReadLatestCanonicalSnapshot(engine).Control.ProtectedControl;
        var numerical = protectedControl.FullPlant.IntegratedCycle.PrimaryCircuit.HydraulicNumerics;
        sentinels.TotalSteps++;
        if (!numerical.Converged) sentinels.NonConvergedSteps++;
        if (!double.IsFinite(numerical.MaximumRelativePressureResidual)
            || !double.IsFinite(numerical.MaximumAbsoluteFlowResidualKilogramsPerSecond))
        {
            sentinels.NonFiniteNumericalSteps++;
        }

        var telemetry = numerical.FourNodeBranchContinuity;
        if (telemetry is not null && telemetry.RollbackRequired) sentinels.RollbackSteps++;
        var protection = protectedControl.Protection;
        if (protection.ReactorScramActive || protection.TurbineTripActive || protection.GeneratorTripActive)
        {
            sentinels.TripSteps++;
        }
    }

    private static P1BContract LoadP1BContract()
    {
        var path = Path.Combine(FindRepositoryRoot(), "eng", P1BContractFileName);
        return JsonSerializer.Deserialize<P1BContract>(File.ReadAllText(path, Encoding.UTF8), new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        }) ?? throw new InvalidOperationException($"Could not deserialize {P1BContractFileName}.");
    }

    private static void ValidateP1BContract(P1BContract contract)
    {
        Assert.Equal(100, contract.StepsPerSecond);
        Assert.Equal(600, contract.BackgroundReferenceSeconds);
        Assert.Equal(3600, contract.MaximumHoldSecondsAfterLoad);
        Assert.Equal(6d, contract.TargetLoadMegawatts);
        Assert.Equal(1d, contract.LoadIncrementMegawatts);
        Assert.Equal(2785L, contract.ExpectedLoadCommandLogicalStep);
        Assert.Equal(3, contract.RequiredCheckpoints.Length);
    }

    private static void WriteContractAndProvenance(string directory, double inverseSelfCheck)
    {
        File.WriteAllLines(Path.Combine(directory, "01-contract-and-provenance.txt"),
        [
            "gate=VR2-REPLANNING-MATERIALITY-DIAGNOSTIC1",
            "prerequisite=VR2-MODEL-DISCREPANCY-BLOCKING returned artifact review",
            "question=Does the VR2 compressed-liquid absolute-pressure discrepancy materially change canonical hydraulic driving heads/flows on the actual exact-v9 P1B path?",
            "runtime-substitution=NONE",
            "reference=IAPWS-R7-97(2012)-test-only-Regions-1-2-4",
            $"inverse-reference-selfcheck-max-relative-error={F(inverseSelfCheck)}",
            $"inverse-reference-selfcheck-ceiling={F(ReferenceInverseSelfCheckMaximumRelativeError)}",
            $"sample-seconds={SampleSeconds}",
            $"background-seconds={BackgroundSeconds}",
            $"maximum-hold-seconds={MaximumHoldSeconds}",
            $"late-analysis-seconds={LateAnalysisSeconds}",
            $"late-window-seconds={LateWindowSeconds}",
            $"materiality-flow-floor-kg-s={F(FlowComparisonFloorKilogramsPerSecond)}",
            $"confirmed-impact-ratio={F(ConfirmedImpactRatio)}",
            $"not-excluded-impact-ratio={F(NotExcludedImpactRatio)}",
            $"required-persistent-windows={RequiredPersistentWindows}",
            "production-src-change=NONE",
            "thermodynamic-tolerance-change=NONE",
            "exact-v9-change=NONE",
            "vr3-authorized=False",
            "p3-r1-authorized=False",
            "second-replacement-long-authorized=False",
        ], Utf8WithoutBom);
    }

    private static void WriteCheckpointReproduction(string directory, IEnumerable<CheckpointResult> checkpoints)
    {
        var builder = new StringBuilder();
        builder.AppendLine("hold_seconds,logical_step,matches,output_mwe,thermal_mw,shaft_mw,frequency_hz,dispatch_mw,flow_kg_s,inlet_mpa");
        foreach (var checkpoint in checkpoints.OrderBy(static row => row.HoldSeconds))
        {
            var row = checkpoint.Sample;
            builder.AppendLine(FormattableString.Invariant(
                $"{checkpoint.HoldSeconds},{row.LogicalStep},{checkpoint.Matches.ToString().ToLowerInvariant()},{row.ElectricalOutputMegawatts:R},{row.ReactorThermalMegawatts:R},{row.TurbineShaftMegawatts:R},{row.GeneratorFrequencyHertz:R},{row.DispatchMechanicalAdequacyMegawatts:R},{row.TurbineSteamFlowKilogramsPerSecond:R},{row.TurbineInletPressureMegapascals:R}"));
        }
        File.WriteAllText(Path.Combine(directory, "02-p1b-checkpoint-reproduction.csv"), builder.ToString(), Utf8WithoutBom);
    }

    private static void WriteNodeInverseMap(string directory, IEnumerable<NodeRow> rows)
    {
        var builder = new StringBuilder();
        builder.AppendLine("probe_id,logical_step,elapsed_s,node_id,production_phase,production_quality,production_density_kg_m3,production_u_j_kg,production_temperature_c,production_pressure_mpa,reference_resolved,reference_region,reference_phase,reference_temperature_c,reference_pressure_mpa,reference_quality,reference_minus_production_pressure_mpa");
        foreach (var row in rows)
        {
            builder.AppendLine(FormattableString.Invariant(
                $"{row.ProbeId},{row.LogicalStep},{row.ElapsedSeconds:R},{row.NodeId},{row.ProductionPhase},{NullableF(row.ProductionQuality)},{row.ProductionDensityKilogramsPerCubicMetre:R},{row.ProductionSpecificInternalEnergyJoulesPerKilogram:R},{row.ProductionTemperatureCelsius:R},{row.ProductionPressureMegapascals:R},{row.ReferenceResolved.ToString().ToLowerInvariant()},{row.ReferenceRegion},{row.ReferencePhase},{row.ReferenceTemperatureCelsius:R},{row.ReferencePressureMegapascals:R},{NullableF(row.ReferenceQuality)},{row.ReferenceMinusProductionPressureMegapascals:R}"));
        }
        File.WriteAllText(Path.Combine(directory, "03-node-if97-inverse-map.csv"), builder.ToString(), Utf8WithoutBom);
    }

    private static void WriteHydraulicCounterfactual(string directory, IEnumerable<PathRow> rows)
    {
        var builder = new StringBuilder();
        builder.AppendLine("probe_id,logical_step,elapsed_s,path_id,from_node,to_node,resistance_pa_s2_kg2,active_boost_pa,production_driving_pa,if97_driving_pa,canonical_flow_kg_s,production_formula_flow_kg_s,if97_pressure_only_counterfactual_flow_kg_s,reference_resolved,driving_pressure_sign_changed,abs_counterfactual_flow_shift_kg_s");
        foreach (var row in rows)
        {
            builder.AppendLine(FormattableString.Invariant(
                $"{row.ProbeId},{row.LogicalStep},{row.ElapsedSeconds:R},{row.PathId},{row.FromNodeId},{row.ToNodeId},{row.ResistancePascalSecondsSquaredPerKilogramSquared:R},{row.ActiveBoostPascals:R},{row.ProductionDrivingPressurePascals:R},{row.ReferenceDrivingPressurePascals:R},{row.CanonicalFlowKilogramsPerSecond:R},{row.ProductionFormulaFlowKilogramsPerSecond:R},{row.CounterfactualFlowKilogramsPerSecond:R},{row.ReferenceResolved.ToString().ToLowerInvariant()},{row.DrivingPressureSignChanged.ToString().ToLowerInvariant()},{Math.Abs(row.CounterfactualFlowKilogramsPerSecond - row.CanonicalFlowKilogramsPerSecond):R}"));
        }
        File.WriteAllText(Path.Combine(directory, "04-hydraulic-path-counterfactual.csv"), builder.ToString(), Utf8WithoutBom);
    }

    private static void WriteLateWindowMateriality(string directory, IEnumerable<WindowRow> rows)
    {
        var builder = new StringBuilder();
        builder.AppendLine("path_id,window_index,start_s,end_s,row_count,resolved_row_count,background_mean_flow_kg_s,production_mean_flow_kg_s,if97_counterfactual_mean_flow_kg_s,mean_abs_counterfactual_shift_kg_s,load_specific_shift_kg_s,within_window_drift_kg_s,phenomenon_scale_kg_s,impact_ratio,driving_pressure_sign_change,band");
        foreach (var row in rows)
        {
            builder.AppendLine(FormattableString.Invariant(
                $"{row.PathId},{row.WindowIndex},{row.StartSeconds},{row.EndSeconds},{row.RowCount},{row.ResolvedRowCount},{row.BackgroundMeanFlowKilogramsPerSecond:R},{row.ProductionMeanFlowKilogramsPerSecond:R},{row.CounterfactualMeanFlowKilogramsPerSecond:R},{row.MeanAbsoluteCounterfactualShiftKilogramsPerSecond:R},{row.LoadSpecificShiftKilogramsPerSecond:R},{row.WithinWindowDriftKilogramsPerSecond:R},{row.PhenomenonScaleKilogramsPerSecond:R},{row.ImpactRatio:R},{row.DrivingPressureSignChange.ToString().ToLowerInvariant()},{row.Band}"));
        }
        File.WriteAllText(Path.Combine(directory, "05-late-window-materiality.csv"), builder.ToString(), Utf8WithoutBom);
    }

    private static void WriteSentinels(string directory, SentinelSummary background, SentinelSummary load)
    {
        File.WriteAllLines(Path.Combine(directory, "07-sentinels.txt"),
        [
            $"background-total-steps={background.TotalSteps}",
            $"background-trip-steps={background.TripSteps}",
            $"background-nonconverged-steps={background.NonConvergedSteps}",
            $"background-nonfinite-numerical-steps={background.NonFiniteNumericalSteps}",
            $"background-rollback-steps={background.RollbackSteps}",
            $"load-total-steps={load.TotalSteps}",
            $"load-trip-steps={load.TripSteps}",
            $"load-nonconverged-steps={load.NonConvergedSteps}",
            $"load-nonfinite-numerical-steps={load.NonFiniteNumericalSteps}",
            $"load-rollback-steps={load.RollbackSteps}",
        ], Utf8WithoutBom);
    }

    private static void WriteSummary(
        string directory,
        RunResult background,
        RunResult load,
        EvidenceStore evidence,
        IReadOnlyList<WindowRow> windows,
        string classification,
        double maximumFlowReproductionError,
        bool deterministicAnalysisRepeat,
        double inverseSelfCheck)
    {
        var unresolvedNodes = evidence.NodeRows.Count(static row => !row.ReferenceResolved);
        var unresolvedPaths = evidence.PathRows.Count(static row => !row.ReferenceResolved);
        var confirmedWindows = windows.Count(static row => row.Band == "CONFIRMED");
        var notExcludedWindows = windows.Count(static row => row.Band == "NOT-EXCLUDED");
        var maxImpactRatio = windows.Where(static row => double.IsFinite(row.ImpactRatio)).Select(static row => row.ImpactRatio).DefaultIfEmpty(double.NaN).Max();
        File.WriteAllLines(Path.Combine(directory, "06-materiality-summary.txt"),
        [
            $"execution-pass={(background.ExecutionPass && load.ExecutionPass).ToString()}",
            $"classification={classification}",
            $"reference-inverse-selfcheck-max-relative-error={F(inverseSelfCheck)}",
            $"background-execution-pass={background.ExecutionPass}",
            $"load-execution-pass={load.ExecutionPass}",
            $"load-command-logical-step={load.LoadCommandStep?.ToString(CultureInfo.InvariantCulture) ?? "none"}",
            $"p1b-checkpoints-matched={load.Checkpoints.Count(static row => row.Matches)}/{load.Checkpoints.Count}",
            $"node-row-count={evidence.NodeRows.Count}",
            $"unresolved-node-row-count={unresolvedNodes}",
            $"path-row-count={evidence.PathRows.Count}",
            $"unresolved-path-row-count={unresolvedPaths}",
            $"confirmed-window-count={confirmedWindows}",
            $"not-excluded-window-count={notExcludedWindows}",
            $"max-impact-ratio={F(maxImpactRatio)}",
            $"max-hydraulic-flow-reproduction-error-kg-s={F(maximumFlowReproductionError)}",
            $"deterministic-analysis-repeat={deterministicAnalysisRepeat}",
            "interpretation=IF97 pressure-only counterfactual; no IF97 state is committed to runtime and no production evolution is recomputed.",
            "vr3-authorized=False",
            "production-repair-authorized=False",
            "thermodynamic-tolerance-change-authorized=False",
            "exact-v9-change-authorized=False",
            "p3-r1-authorized=False",
            "second-replacement-long-authorized=False",
            "next-action=Return the complete materiality diagnostic artifact folder for engineering review and a separate decision before any repair or VR3.",
        ], Utf8WithoutBom);
    }

    private static string ResetArtifactDirectory()
    {
        var path = Path.Combine(FindRepositoryRoot(), ArtifactDirectoryRelative.Replace('/', Path.DirectorySeparatorChar));
        if (Directory.Exists(path)) Directory.Delete(path, recursive: true);
        Directory.CreateDirectory(path);
        return path;
    }

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "NuclearReactorSimulator.sln"))) return current.FullName;
            current = current.Parent;
        }
        throw new DirectoryNotFoundException("Could not locate NuclearReactorSimulator.sln from test output directory.");
    }

    private static void RequireOptIn()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable(OptInEnvironmentVariable), "1", StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"VR2 materiality diagnostic is explicit and fail-closed. Set {OptInEnvironmentVariable}=1 through the authorized runner.");
        }
    }

    private static double RelativeError(double expected, double actual)
        => Math.Abs(actual - expected) / Math.Max(Math.Abs(expected), 1e-300d);

    private static string F(double value) => value.ToString("R", CultureInfo.InvariantCulture);
    private static string NullableF(double? value) => value.HasValue ? F(value.Value) : string.Empty;
    private static string Flatten(string value) => value.Replace('\r', ' ').Replace('\n', ' ');

    private sealed class SentinelSummary
    {
        public long TotalSteps { get; set; }
        public long TripSteps { get; set; }
        public long NonConvergedSteps { get; set; }
        public long NonFiniteNumericalSteps { get; set; }
        public long RollbackSteps { get; set; }
        public bool AnyFailure => TripSteps > 0 || NonConvergedSteps > 0 || NonFiniteNumericalSteps > 0 || RollbackSteps > 0;
    }

    private sealed class EvidenceStore
    {
        public List<NodeRow> NodeRows { get; } = [];
        public List<PathRow> PathRows { get; } = [];
    }

    private sealed record P1BContract(
        string ContractId,
        string Baseline,
        string Question,
        int StepsPerSecond,
        int BackgroundReferenceSeconds,
        int PreparationTimeoutSeconds,
        int MaximumHoldSecondsAfterLoad,
        int TrajectorySampleSeconds,
        int LateAnalysisSeconds,
        int LateSubwindowSeconds,
        double ThermalReadinessToleranceMegawatts,
        double TargetLoadMegawatts,
        double LoadIncrementMegawatts,
        long ExpectedLoadCommandLogicalStep,
        double TrendReferenceMultiplier,
        double TrendRelativeMagnitudeGuardPerSecond,
        int TrendMinimumConsistentLateWindows,
        CheckpointTolerances CheckpointTolerances,
        P1BCheckpoint[] RequiredCheckpoints,
        string[] RequiredReactorPrimaryControllerIds,
        string[] RequiredTurbineSecondaryControllerIds,
        string[] AllowedOwnerDomains,
        bool OneSecondSamplingIsSlowStateDownsample,
        bool PerStepProtectionAndNumericalSentinelsRequired,
        bool NormalizedCrossDomainOwnerScoreEnabled,
        bool NewConstitutivePhysicsAuthorized,
        bool BlindHoldExtensionAuthorized,
        bool DirectP3SelectionAuthorized,
        bool SecondReplacementLongAuthorized);

    private sealed record CheckpointTolerances(
        double PowerMegawatts,
        double ThermalMegawatts,
        double FrequencyHertz,
        double FlowKilogramsPerSecond,
        double PressureMegapascals);

    private sealed record P1BCheckpoint(
        int HoldSeconds,
        long ExpectedLogicalStep,
        double OutputMegawatts,
        double ThermalMegawatts,
        double ShaftMegawatts,
        double FrequencyHertz,
        double DispatchAdequacyMegawatts,
        double FlowKilogramsPerSecond,
        double InletPressureMegapascals);

    private sealed record RunResult(
        bool ExecutionPass,
        string FailureMessage,
        long? LoadCommandStep,
        IReadOnlyList<CheckpointResult> Checkpoints,
        SentinelSummary Sentinels);

    private readonly record struct CheckpointResult(int HoldSeconds, long ActualLogicalStep, bool Matches, OperationalSummary Sample);

    private readonly record struct OperationalSummary(
        long LogicalStep,
        double ElapsedSeconds,
        double ElectricalOutputMegawatts,
        double ReactorThermalMegawatts,
        double TurbineShaftMegawatts,
        double GeneratorFrequencyHertz,
        double DispatchMechanicalAdequacyMegawatts,
        double TurbineSteamFlowKilogramsPerSecond,
        double TurbineInletPressureMegapascals,
        bool BreakerClosed,
        bool ReactorScram,
        bool TurbineTrip,
        bool GeneratorTrip,
        bool HydraulicConverged)
    {
        public bool AnyTrip => ReactorScram || TurbineTrip || GeneratorTrip;
    }

    private readonly record struct NodeReference(
        bool Resolved,
        string Region,
        string Phase,
        double TemperatureCelsius,
        double PressureMegapascals,
        double? VaporQuality);

    private readonly record struct NodeRow(
        string ProbeId,
        long LogicalStep,
        double ElapsedSeconds,
        string NodeId,
        string ProductionPhase,
        double? ProductionQuality,
        double ProductionDensityKilogramsPerCubicMetre,
        double ProductionSpecificInternalEnergyJoulesPerKilogram,
        double ProductionTemperatureCelsius,
        double ProductionPressureMegapascals,
        bool ReferenceResolved,
        string ReferenceRegion,
        string ReferencePhase,
        double ReferenceTemperatureCelsius,
        double ReferencePressureMegapascals,
        double? ReferenceQuality,
        double ReferenceMinusProductionPressureMegapascals);

    private readonly record struct PathRow(
        string ProbeId,
        long LogicalStep,
        double ElapsedSeconds,
        string PathId,
        string FromNodeId,
        string ToNodeId,
        double ResistancePascalSecondsSquaredPerKilogramSquared,
        double ActiveBoostPascals,
        double ProductionDrivingPressurePascals,
        double ReferenceDrivingPressurePascals,
        double CanonicalFlowKilogramsPerSecond,
        double ProductionFormulaFlowKilogramsPerSecond,
        double CounterfactualFlowKilogramsPerSecond,
        bool ReferenceResolved,
        bool DrivingPressureSignChanged);

    private readonly record struct WindowRow(
        string PathId,
        int WindowIndex,
        int StartSeconds,
        int EndSeconds,
        int RowCount,
        int ResolvedRowCount,
        double BackgroundMeanFlowKilogramsPerSecond,
        double ProductionMeanFlowKilogramsPerSecond,
        double CounterfactualMeanFlowKilogramsPerSecond,
        double MeanAbsoluteCounterfactualShiftKilogramsPerSecond,
        double LoadSpecificShiftKilogramsPerSecond,
        double WithinWindowDriftKilogramsPerSecond,
        double PhenomenonScaleKilogramsPerSecond,
        double ImpactRatio,
        bool DrivingPressureSignChange,
        string Band);
}
