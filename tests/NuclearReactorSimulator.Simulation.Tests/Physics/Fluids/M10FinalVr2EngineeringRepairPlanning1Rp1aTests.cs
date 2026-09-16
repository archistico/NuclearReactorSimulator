using System.Diagnostics;
using System.Globalization;
using System.Text;
using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.ControlRoom.Automation;
using NuclearReactorSimulator.Application.Scenarios.Training;
using NuclearReactorSimulator.Domain.Physics.Control.Supervisory;
using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Simulation.Physics.Fluids;
using NuclearReactorSimulator.Simulation.Tests.Physics.Fluids.Reference;
using Xunit;

namespace NuclearReactorSimulator.Simulation.Tests.Physics.Fluids;

/// <summary>
/// VR2 Engineering Repair Planning 1 / RP1A. Freezes the reference-domain corpus, current exact-v9
/// phase/seam ownership and machine-local performance baseline before any RP1B candidate is implemented.
/// No production physics is changed by this test.
/// </summary>
public sealed class M10FinalVr2EngineeringRepairPlanning1Rp1aTests
{
    private const string OptInEnvironmentVariable = "NRS_M10_FINAL_VR2_REPAIR_PLANNING1_RP1A";
    private const string ArtifactDirectoryRelative = "artifacts/m10-final-physical-reference-vr2-engineering-repair-planning1-rp1a";
    private const string FrozenAttempt5Relative = "eng/frozen-evidence/ordinary/M10FinalPhysicalReferenceVR2MaterialityDiagnostic1_Attempt5_Artifacts";
    private const double BoundaryPressureRelativeOffset = 1e-5d;
    private const double BoundaryQualityOffset = 1e-6d;
    private const int ResolveWarmupPasses = 8;
    private const int ResolveMeasuredPasses = 64;
    private const int WholeStepWarmupSteps = 128;
    private const int WholeStepMeasuredSteps = 512;
    private const int ExpectedVr2ReferenceCorpusRows = 40;
    private const int ExpectedSeamBoundaryCount = 320;
    private const int ExpectedSeamProbeRows = 1_280;
    private const double HistoricalMedianWallRatioLimit = 8d;
    private const double HistoricalP95WallRatioLimit = 12d;
    private const double HistoricalMedianAllocationRatioLimit = 16d;
    private const string FrozenNodeCsvHeader = "probe_id,logical_step,elapsed_s,node_id,production_phase,production_quality,production_density_kg_m3,production_u_j_kg,production_temperature_c,production_pressure_mpa,reference_resolved,reference_region,reference_phase,reference_temperature_c,reference_pressure_mpa,reference_quality,reference_minus_production_pressure_mpa";
    private const string FrozenHydraulicCsvHeader = "probe_id,logical_step,elapsed_s,path_id,from_node,to_node,resistance_pa_s2_kg2,active_boost_pa,production_driving_pa,if97_driving_pa,canonical_flow_kg_s,production_formula_flow_kg_s,if97_pressure_only_counterfactual_flow_kg_s,reference_resolved,driving_pressure_sign_changed,abs_counterfactual_flow_shift_kg_s";
    private static readonly UTF8Encoding Utf8WithoutBom = new(encoderShouldEmitUTF8Identifier: false);

    private static readonly double[] SaturationFullPropertyTemperaturesCelsius =
        [100d, 125d, 150d, 175d, 200d, 225d, 250d, 280d, 300d, 340d];

    private static readonly double[] SaturationPressureOnlyTemperaturesCelsius = [360d];

    private static readonly StatePoint[] CompressedLiquidPoints =
    [
        new("VR2-L100-P0.5", 100d, 0.5d, "REGION-1", FluidPhase.SubcooledLiquid),
        new("VR2-L150-P1", 150d, 1d, "REGION-1", FluidPhase.SubcooledLiquid),
        new("VR2-L200-P2", 200d, 2d, "REGION-1", FluidPhase.SubcooledLiquid),
        new("VR2-L250-P7", 250d, 7d, "REGION-1", FluidPhase.SubcooledLiquid),
        new("VR2-L280-P10", 280d, 10d, "REGION-1", FluidPhase.SubcooledLiquid),
    ];

    private static readonly StatePoint[] SuperheatedVaporPoints =
    [
        new("VR2-V200-P0.2", 200d, 0.2d, "REGION-2", FluidPhase.SuperheatedVapor),
        new("VR2-V250-P0.5", 250d, 0.5d, "REGION-2", FluidPhase.SuperheatedVapor),
        new("VR2-V300-P1", 300d, 1d, "REGION-2", FluidPhase.SuperheatedVapor),
        new("VR2-V400-P5", 400d, 5d, "REGION-2", FluidPhase.SuperheatedVapor),
    ];

    [Fact(Explicit = true)]
    [Trait("Category", "M10FinalVr2EngineeringRepairPlanning1Rp1a")]
    public void Rp1a_FreezesReferenceCorpusSeamMapAndPreCandidatePerformanceCeilings()
    {
        RequireOptIn();
        var root = FindRepositoryRoot();
        var artifactDirectory = ResetArtifactDirectory(root);
        var frozenAttempt5Directory = Path.Combine(root, FrozenAttempt5Relative.Replace('/', Path.DirectorySeparatorChar));
        var frozenNodePath = Path.Combine(frozenAttempt5Directory, "03-node-if97-inverse-map.csv");
        var frozenHydraulicPath = Path.Combine(frozenAttempt5Directory, "04-hydraulic-path-counterfactual.csv");

        Assert.True(File.Exists(frozenNodePath), $"Frozen Attempt-5 node corpus missing: {frozenNodePath}");
        Assert.True(File.Exists(frozenHydraulicPath), $"Frozen Attempt-5 hydraulic context missing: {frozenHydraulicPath}");

        var nodeRows = LoadFrozenNodeRows(frozenNodePath);
        var hydraulicRowCount = CountDataRows(frozenHydraulicPath, FrozenHydraulicCsvHeader);
        Assert.Equal(360, nodeRows.Count);
        Assert.Equal(288, hydraulicRowCount);
        Assert.All(nodeRows, static row => Assert.True(row.ReferenceResolved));

        WriteContractAndProvenance(artifactDirectory, nodeRows.Count, hydraulicRowCount);

        var model = new SimplifiedWaterSteamThermodynamicModel(WaterSteamThermodynamicClosureMode.CorrelationConsistentInverseDomain);
        var vr2Corpus = BuildVr2ReferenceCorpus(model);
        WriteVr2ReferenceCorpus(artifactDirectory, vr2Corpus);

        File.Copy(frozenNodePath, Path.Combine(artifactDirectory, "03-exact-v9-node-corpus.csv"), overwrite: true);
        File.Copy(frozenHydraulicPath, Path.Combine(artifactDirectory, "04-hydraulic-context.csv"), overwrite: true);

        var seamRows = BuildSeamProbeMap(model, nodeRows);
        var repeatedSeamRows = BuildSeamProbeMap(
            new SimplifiedWaterSteamThermodynamicModel(WaterSteamThermodynamicClosureMode.CorrelationConsistentInverseDomain),
            nodeRows);
        var deterministicSeamRepeat = seamRows.SequenceEqual(repeatedSeamRows);
        WriteSeamProbeMap(artifactDirectory, seamRows);

        var performance = MeasurePerformanceBaseline(model, nodeRows);
        WritePerformanceBaseline(artifactDirectory, performance);

        var unresolvedSeamCount = seamRows.Count(static row => !row.ProductionResolved);
        var referenceClassifiableNodeCount = nodeRows.Count(static row => row.ReferenceResolved);
        var region4NodeCount = nodeRows.Count(static row => string.Equals(row.ReferenceRegion, "REGION-4-MIXTURE", StringComparison.Ordinal));
        var region1NodeCount = nodeRows.Count(static row => string.Equals(row.ReferenceRegion, "REGION-1", StringComparison.Ordinal));
        var seamBoundaryCount = seamRows.Select(static row => row.BoundaryIndex).Distinct().Count();
        var r1SideCount = seamRows.Count(static row => string.Equals(row.ProbeSide, "R1-SIDE", StringComparison.Ordinal));
        var r4LiquidSideCount = seamRows.Count(static row => string.Equals(row.ProbeSide, "R4-LIQUID-SIDE", StringComparison.Ordinal));
        var r4VaporSideCount = seamRows.Count(static row => string.Equals(row.ProbeSide, "R4-VAPOR-SIDE", StringComparison.Ordinal));
        var r2SideCount = seamRows.Count(static row => string.Equals(row.ProbeSide, "R2-SIDE", StringComparison.Ordinal));
        var seamTemperaturesInsideRegion12Boundary = seamRows.All(static row => row.BoundaryTemperatureCelsius + 273.15d <= 623.15d);
        var performanceFinite = IsFinitePositive(performance.ResolveMedianMicroseconds)
            && IsFinitePositive(performance.ResolveP95Microseconds)
            && IsFinitePositive(performance.ResolveMaximumMicroseconds)
            && IsFinitePositive(performance.WholeStepMedianMicroseconds)
            && IsFinitePositive(performance.WholeStepP95Microseconds)
            && IsFinitePositive(performance.WholeStepMaximumMicroseconds)
            && IsFinitePositive(performance.CandidateResolveMedianCeilingMicroseconds)
            && IsFinitePositive(performance.CandidateResolveP95CeilingMicroseconds)
            && IsFinitePositive(performance.CandidateResolveMaximumCeilingMicroseconds)
            && double.IsFinite(performance.ResolveMedianAllocatedBytes)
            && performance.ResolveMedianAllocatedBytes >= 0d
            && double.IsFinite(performance.CandidateResolveMedianAllocatedBytesCeiling)
            && performance.CandidateResolveMedianAllocatedBytesCeiling >= 0d;
        var pass = deterministicSeamRepeat
            && vr2Corpus.Count == ExpectedVr2ReferenceCorpusRows
            && referenceClassifiableNodeCount == 360
            && region4NodeCount == 348
            && region1NodeCount == 12
            && seamBoundaryCount == ExpectedSeamBoundaryCount
            && seamRows.Count == ExpectedSeamProbeRows
            && r1SideCount == ExpectedSeamBoundaryCount
            && r4LiquidSideCount == ExpectedSeamBoundaryCount
            && r4VaporSideCount == ExpectedSeamBoundaryCount
            && r2SideCount == ExpectedSeamBoundaryCount
            && seamTemperaturesInsideRegion12Boundary
            && performanceFinite
            && performance.ResolveMeasuredCallCount == nodeRows.Count * ResolveMeasuredPasses
            && performance.WholeStepMeasuredCount == WholeStepMeasuredSteps
            && performance.WholeStepTripCount == 0;

        WriteSummary(
            artifactDirectory,
            pass,
            vr2Corpus,
            nodeRows,
            hydraulicRowCount,
            seamRows,
            unresolvedSeamCount,
            deterministicSeamRepeat,
            performance);

        Assert.True(deterministicSeamRepeat, "RP1A seam-map repeat was not deterministic.");
        Assert.Equal(ExpectedVr2ReferenceCorpusRows, vr2Corpus.Count);
        Assert.Equal(ExpectedSeamBoundaryCount, seamBoundaryCount);
        Assert.Equal(ExpectedSeamProbeRows, seamRows.Count);
        Assert.Equal(ExpectedSeamBoundaryCount, r1SideCount);
        Assert.Equal(ExpectedSeamBoundaryCount, r4LiquidSideCount);
        Assert.Equal(ExpectedSeamBoundaryCount, r4VaporSideCount);
        Assert.Equal(ExpectedSeamBoundaryCount, r2SideCount);
        Assert.True(seamTemperaturesInsideRegion12Boundary, "RP1A generated a Region-1/2 seam probe above the frozen 623.15 K boundary ceiling.");
        Assert.True(performanceFinite, "RP1A performance baseline contained non-finite or non-positive timing evidence.");
        Assert.Equal(360, referenceClassifiableNodeCount);
        Assert.Equal(348, region4NodeCount);
        Assert.Equal(12, region1NodeCount);
        Assert.Equal(0, performance.WholeStepTripCount);
        Assert.True(pass, "RP1A corpus/seam/performance freeze did not meet its authored observation-only contract.");
    }

    private static List<Vr2CorpusRow> BuildVr2ReferenceCorpus(SimplifiedWaterSteamThermodynamicModel model)
    {
        var rows = new List<Vr2CorpusRow>();

        foreach (var temperatureCelsius in SaturationFullPropertyTemperaturesCelsius)
        {
            var temperatureKelvins = temperatureCelsius + 273.15d;
            var pressureMegapascals = IapwsIf97Reference.SaturationPressureMegapascals(temperatureKelvins);
            var liquid = IapwsIf97Reference.Region1(temperatureKelvins, pressureMegapascals);
            var vapor = IapwsIf97Reference.Region2(temperatureKelvins, pressureMegapascals);
            AddReferenceState(rows, model, $"VR2-SAT-{temperatureCelsius:R}C-LIQ", "SATURATION-ENDPOINT", "REGION-1", FluidPhase.SaturatedMixture,
                temperatureKelvins, pressureMegapascals, liquid.SpecificVolumeCubicMetresPerKilogram, liquid.SpecificInternalEnergyJoulesPerKilogram, 0d);
            AddMixtureState(rows, model, $"VR2-SAT-{temperatureCelsius:R}C-Q0.5", temperatureKelvins, pressureMegapascals, liquid, vapor, 0.5d);
            AddReferenceState(rows, model, $"VR2-SAT-{temperatureCelsius:R}C-VAP", "SATURATION-ENDPOINT", "REGION-2", FluidPhase.SaturatedMixture,
                temperatureKelvins, pressureMegapascals, vapor.SpecificVolumeCubicMetresPerKilogram, vapor.SpecificInternalEnergyJoulesPerKilogram, 1d);
        }

        foreach (var temperatureCelsius in SaturationPressureOnlyTemperaturesCelsius)
        {
            var temperatureKelvins = temperatureCelsius + 273.15d;
            var pressureMegapascals = IapwsIf97Reference.SaturationPressureMegapascals(temperatureKelvins);
            rows.Add(new Vr2CorpusRow(
                $"VR2-SAT-{temperatureCelsius:R}C-PONLY",
                "SATURATION-PRESSURE-ONLY",
                "REGION-4",
                "BOUNDARY-ONLY",
                temperatureCelsius,
                pressureMegapascals,
                double.NaN,
                double.NaN,
                double.NaN,
                false,
                string.Empty,
                double.NaN,
                double.NaN));
        }

        foreach (var point in CompressedLiquidPoints)
        {
            var reference = IapwsIf97Reference.Region1(point.TemperatureCelsius + 273.15d, point.PressureMegapascals);
            AddReferenceState(rows, model, point.Id, "COMPRESSED-LIQUID", point.Region, point.ExpectedPhase,
                point.TemperatureCelsius + 273.15d, point.PressureMegapascals,
                reference.SpecificVolumeCubicMetresPerKilogram, reference.SpecificInternalEnergyJoulesPerKilogram, double.NaN);
        }

        foreach (var point in SuperheatedVaporPoints)
        {
            var reference = IapwsIf97Reference.Region2(point.TemperatureCelsius + 273.15d, point.PressureMegapascals);
            AddReferenceState(rows, model, point.Id, "SUPERHEATED-VAPOR", point.Region, point.ExpectedPhase,
                point.TemperatureCelsius + 273.15d, point.PressureMegapascals,
                reference.SpecificVolumeCubicMetresPerKilogram, reference.SpecificInternalEnergyJoulesPerKilogram, double.NaN);
        }

        return rows;
    }

    private static void AddMixtureState(
        ICollection<Vr2CorpusRow> rows,
        SimplifiedWaterSteamThermodynamicModel model,
        string id,
        double temperatureKelvins,
        double pressureMegapascals,
        IapwsReferenceState liquid,
        IapwsReferenceState vapor,
        double quality)
    {
        var specificVolume = liquid.SpecificVolumeCubicMetresPerKilogram
            + quality * (vapor.SpecificVolumeCubicMetresPerKilogram - liquid.SpecificVolumeCubicMetresPerKilogram);
        var specificEnergy = liquid.SpecificInternalEnergyJoulesPerKilogram
            + quality * (vapor.SpecificInternalEnergyJoulesPerKilogram - liquid.SpecificInternalEnergyJoulesPerKilogram);
        AddReferenceState(rows, model, id, "SATURATION-MIXTURE", "REGION-4", FluidPhase.SaturatedMixture,
            temperatureKelvins, pressureMegapascals, specificVolume, specificEnergy, quality);
    }

    private static void AddReferenceState(
        ICollection<Vr2CorpusRow> rows,
        SimplifiedWaterSteamThermodynamicModel model,
        string id,
        string sourceFamily,
        string referenceRegion,
        FluidPhase expectedPhase,
        double temperatureKelvins,
        double pressureMegapascals,
        double specificVolume,
        double specificEnergy,
        double quality)
    {
        var resolved = TryResolveProduction(model, id, specificVolume, specificEnergy, out var production);
        rows.Add(new Vr2CorpusRow(
            id,
            sourceFamily,
            referenceRegion,
            expectedPhase.ToString(),
            temperatureKelvins - 273.15d,
            pressureMegapascals,
            specificVolume,
            specificEnergy,
            quality,
            resolved,
            resolved ? production.Phase.ToString() : string.Empty,
            resolved ? production.Temperature.DegreesCelsius : double.NaN,
            resolved ? production.Pressure.Megapascals : double.NaN));
    }

    private static List<SeamProbeRow> BuildSeamProbeMap(
        SimplifiedWaterSteamThermodynamicModel model,
        IReadOnlyCollection<FrozenNodeRow> nodeRows)
    {
        var temperaturesKelvins = new SortedSet<double>();
        foreach (var temperatureCelsius in SaturationFullPropertyTemperaturesCelsius)
        {
            temperaturesKelvins.Add(temperatureCelsius + 273.15d);
        }

        foreach (var row in nodeRows)
        {
            if (row.ReferenceResolved
                && string.Equals(row.ReferenceRegion, "REGION-4-MIXTURE", StringComparison.Ordinal)
                && row.ReferenceTemperatureCelsius + 273.15d <= 623.15d)
            {
                temperaturesKelvins.Add(row.ReferenceTemperatureCelsius + 273.15d);
            }
        }

        var rows = new List<SeamProbeRow>();
        var index = 0;
        foreach (var temperatureKelvins in temperaturesKelvins)
        {
            var saturationPressure = IapwsIf97Reference.SaturationPressureMegapascals(temperatureKelvins);
            var liquid = IapwsIf97Reference.Region1(temperatureKelvins, saturationPressure);
            var vapor = IapwsIf97Reference.Region2(temperatureKelvins, saturationPressure);
            var liquidSidePressure = saturationPressure * (1d + BoundaryPressureRelativeOffset);
            var vaporSidePressure = saturationPressure * (1d - BoundaryPressureRelativeOffset);
            var liquidSide = IapwsIf97Reference.Region1(temperatureKelvins, liquidSidePressure);
            var vaporSide = IapwsIf97Reference.Region2(temperatureKelvins, vaporSidePressure);

            AddSeamProbe(rows, model, index, temperatureKelvins, "R1-SIDE", "REGION-1", FluidPhase.SubcooledLiquid,
                liquidSidePressure, liquidSide.SpecificVolumeCubicMetresPerKilogram, liquidSide.SpecificInternalEnergyJoulesPerKilogram, double.NaN);
            AddMixtureSeamProbe(rows, model, index, temperatureKelvins, saturationPressure, liquid, vapor, BoundaryQualityOffset, "R4-LIQUID-SIDE");
            AddMixtureSeamProbe(rows, model, index, temperatureKelvins, saturationPressure, liquid, vapor, 1d - BoundaryQualityOffset, "R4-VAPOR-SIDE");
            AddSeamProbe(rows, model, index, temperatureKelvins, "R2-SIDE", "REGION-2", FluidPhase.SuperheatedVapor,
                vaporSidePressure, vaporSide.SpecificVolumeCubicMetresPerKilogram, vaporSide.SpecificInternalEnergyJoulesPerKilogram, double.NaN);
            index++;
        }

        return rows;
    }

    private static void AddMixtureSeamProbe(
        ICollection<SeamProbeRow> rows,
        SimplifiedWaterSteamThermodynamicModel model,
        int boundaryIndex,
        double temperatureKelvins,
        double pressureMegapascals,
        IapwsReferenceState liquid,
        IapwsReferenceState vapor,
        double quality,
        string side)
    {
        var specificVolume = liquid.SpecificVolumeCubicMetresPerKilogram
            + quality * (vapor.SpecificVolumeCubicMetresPerKilogram - liquid.SpecificVolumeCubicMetresPerKilogram);
        var specificEnergy = liquid.SpecificInternalEnergyJoulesPerKilogram
            + quality * (vapor.SpecificInternalEnergyJoulesPerKilogram - liquid.SpecificInternalEnergyJoulesPerKilogram);
        AddSeamProbe(rows, model, boundaryIndex, temperatureKelvins, side, "REGION-4", FluidPhase.SaturatedMixture,
            pressureMegapascals, specificVolume, specificEnergy, quality);
    }

    private static void AddSeamProbe(
        ICollection<SeamProbeRow> rows,
        SimplifiedWaterSteamThermodynamicModel model,
        int boundaryIndex,
        double temperatureKelvins,
        string side,
        string referenceRegion,
        FluidPhase referencePhase,
        double referencePressureMegapascals,
        double specificVolume,
        double specificEnergy,
        double quality)
    {
        var id = $"RP1A-SEAM-{boundaryIndex:D4}-{side}";
        var resolved = TryResolveProduction(model, id, specificVolume, specificEnergy, out var production);
        rows.Add(new SeamProbeRow(
            boundaryIndex,
            temperatureKelvins - 273.15d,
            side,
            referenceRegion,
            referencePhase.ToString(),
            referencePressureMegapascals,
            specificVolume,
            specificEnergy,
            quality,
            resolved,
            resolved ? production.Phase.ToString() : string.Empty,
            resolved ? production.Temperature.DegreesCelsius : double.NaN,
            resolved ? production.Pressure.Megapascals : double.NaN,
            resolved && production.Phase == referencePhase,
            resolved ? production.Temperature.DegreesCelsius - (temperatureKelvins - 273.15d) : double.NaN,
            resolved ? production.Pressure.Megapascals - referencePressureMegapascals : double.NaN));
    }

    private static PerformanceBaseline MeasurePerformanceBaseline(
        SimplifiedWaterSteamThermodynamicModel model,
        IReadOnlyList<FrozenNodeRow> nodeRows)
    {
        var states = nodeRows.Select(static row => new BenchmarkState(
            row.NodeId,
            1d / row.ProductionDensityKilogramsPerCubicMetre,
            row.ProductionSpecificInternalEnergyJoulesPerKilogram)).ToArray();

        for (var pass = 0; pass < ResolveWarmupPasses; pass++)
        {
            foreach (var state in states)
            {
                ResolveBenchmarkState(model, state);
            }
        }

        var resolveRows = new List<TimedRow>(ResolveMeasuredPasses);
        for (var pass = 0; pass < ResolveMeasuredPasses; pass++)
        {
            var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
            var started = Stopwatch.GetTimestamp();
            foreach (var state in states)
            {
                ResolveBenchmarkState(model, state);
            }
            var elapsedTicks = Stopwatch.GetTimestamp() - started;
            var allocated = Math.Max(0L, GC.GetAllocatedBytesForCurrentThread() - allocatedBefore);
            resolveRows.Add(new TimedRow(
                TicksToMicroseconds(elapsedTicks) / states.Length,
                allocated / (double)states.Length));
        }

        Assert.Equal("integrated-operations-desktop-stable", DesktopSustainedGenerationPostMoistureEquilibriumCandidateInitialConditionFactory.Reference.InitialConditionId);
        Assert.Equal(9, DesktopSustainedGenerationPostMoistureEquilibriumCandidateInitialConditionFactory.Reference.Version);
        var engine = Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(
            new DesktopSustainedGenerationPostMoistureEquilibriumCandidateInitialConditionFactory().CreateRuntimeEngine());
        engine.RequestPlantControlAuthority(PlantControlAuthorityMode.SupervisoryAutomatic);
        engine.RequestSupervisoryObjective(SupervisoryObjectiveRequest.HoldCurrentOperatingPoint());

        for (var step = 0; step < WholeStepWarmupSteps; step++)
        {
            var warmup = engine.Step(ControlRoomRunState.Running);
            Assert.False(warmup.AnyTripActive);
        }

        var stepRows = new List<TimedRow>(WholeStepMeasuredSteps);
        var tripCount = 0;
        for (var step = 0; step < WholeStepMeasuredSteps; step++)
        {
            var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
            var started = Stopwatch.GetTimestamp();
            var presentation = engine.Step(ControlRoomRunState.Running);
            var elapsedTicks = Stopwatch.GetTimestamp() - started;
            var allocated = Math.Max(0L, GC.GetAllocatedBytesForCurrentThread() - allocatedBefore);
            if (presentation.AnyTripActive) tripCount++;
            stepRows.Add(new TimedRow(TicksToMicroseconds(elapsedTicks), allocated));
        }

        var resolveMedianUs = Median(resolveRows.Select(static row => row.Microseconds).ToArray());
        var resolveP95Us = Percentile(resolveRows.Select(static row => row.Microseconds).ToArray(), 0.95d);
        var resolveMaxUs = resolveRows.Max(static row => row.Microseconds);
        var resolveMedianAlloc = Median(resolveRows.Select(static row => row.AllocatedBytes).ToArray());
        var stepMedianUs = Median(stepRows.Select(static row => row.Microseconds).ToArray());
        var stepP95Us = Percentile(stepRows.Select(static row => row.Microseconds).ToArray(), 0.95d);
        var stepMaxUs = stepRows.Max(static row => row.Microseconds);
        var stepMedianAlloc = Median(stepRows.Select(static row => row.AllocatedBytes).ToArray());

        return new PerformanceBaseline(
            states.Length * ResolveMeasuredPasses,
            WholeStepMeasuredSteps,
            tripCount,
            resolveMedianUs,
            resolveP95Us,
            resolveMaxUs,
            resolveMedianAlloc,
            stepMedianUs,
            stepP95Us,
            stepMaxUs,
            stepMedianAlloc,
            10_000d - stepP95Us,
            resolveMedianUs * HistoricalMedianWallRatioLimit,
            resolveP95Us * HistoricalP95WallRatioLimit,
            resolveMaxUs * HistoricalP95WallRatioLimit,
            resolveMedianAlloc * HistoricalMedianAllocationRatioLimit);
    }

    private static void ResolveBenchmarkState(SimplifiedWaterSteamThermodynamicModel model, BenchmarkState state)
    {
        var definition = new FluidNodeDefinition(state.Id, Volume.FromCubicMetres(state.SpecificVolume));
        var inventory = new FluidNodeInventory(Mass.FromKilograms(1d), Energy.FromJoules(state.SpecificEnergy));
        _ = model.Resolve(definition, inventory, new FluidThermodynamicState(Pressure.StandardAtmosphere, Temperature.FromDegreesCelsius(20d)));
    }

    private static bool TryResolveProduction(
        SimplifiedWaterSteamThermodynamicModel model,
        string id,
        double specificVolume,
        double specificEnergy,
        out FluidThermodynamicState state)
    {
        try
        {
            var definition = new FluidNodeDefinition(id, Volume.FromCubicMetres(specificVolume));
            var inventory = new FluidNodeInventory(Mass.FromKilograms(1d), Energy.FromJoules(specificEnergy));
            state = model.Resolve(definition, inventory, new FluidThermodynamicState(Pressure.StandardAtmosphere, Temperature.FromDegreesCelsius(20d)));
            return true;
        }
        catch (WaterSteamStateOutOfRangeException)
        {
            state = null!;
            return false;
        }
    }

    private static List<FrozenNodeRow> LoadFrozenNodeRows(string path)
    {
        var lines = File.ReadLines(path, Encoding.UTF8).ToArray();
        Assert.True(lines.Length > 0, "Frozen CSV evidence must contain a header row.");
        Assert.Equal(FrozenNodeCsvHeader, lines[0].TrimStart('\uFEFF'));

        var rows = new List<FrozenNodeRow>(lines.Length - 1);
        foreach (var line in lines.Skip(1))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var parts = line.Split(',');
            Assert.Equal(17, parts.Length);
            rows.Add(new FrozenNodeRow(
                parts[0],
                long.Parse(parts[1], CultureInfo.InvariantCulture),
                double.Parse(parts[2], CultureInfo.InvariantCulture),
                parts[3],
                parts[4],
                double.Parse(parts[6], CultureInfo.InvariantCulture),
                double.Parse(parts[7], CultureInfo.InvariantCulture),
                bool.Parse(parts[10]),
                parts[11],
                double.Parse(parts[13], CultureInfo.InvariantCulture),
                double.Parse(parts[14], CultureInfo.InvariantCulture)));
        }

        return rows;
    }

    private static int CountDataRows(string path, string expectedHeader)
    {
        var lines = File.ReadLines(path, Encoding.UTF8).ToArray();
        Assert.True(lines.Length > 0, "Frozen CSV evidence must contain a header row.");
        Assert.Equal(expectedHeader, lines[0].TrimStart('\uFEFF'));
        return lines.Skip(1).Count(static line => !string.IsNullOrWhiteSpace(line));
    }

    private static void WriteContractAndProvenance(string directory, int nodeCount, int hydraulicCount)
    {
        File.WriteAllLines(Path.Combine(directory, "01-contract-and-provenance.txt"),
        [
            "gate=VR2-ENGINEERING-REPAIR-PLANNING1-RP1A",
            "purpose=reference-domain-corpus-and-seam-map-freeze",
            "production-src-change=False",
            "thermodynamic-repair-authorized=False",
            "exact-v9-change-authorized=False",
            "vr3-authorized=False",
            "source-vr2-matrix=tests/NuclearReactorSimulator.Simulation.Tests/Physics/Fluids/M10FinalPhysicalReferenceVr2WaterSteamBenchmarkTests.cs",
            "source-exact-v9-node-corpus=eng/frozen-evidence/ordinary/M10FinalPhysicalReferenceVR2MaterialityDiagnostic1_Attempt5_Artifacts/03-node-if97-inverse-map.csv",
            "source-hydraulic-context=eng/frozen-evidence/ordinary/M10FinalPhysicalReferenceVR2MaterialityDiagnostic1_Attempt5_Artifacts/04-hydraulic-path-counterfactual.csv",
            $"frozen-node-rows={nodeCount}",
            $"frozen-hydraulic-rows={hydraulicCount}",
            FormattableString.Invariant($"seam-pressure-relative-offset={BoundaryPressureRelativeOffset:R}"),
            FormattableString.Invariant($"seam-quality-offset={BoundaryQualityOffset:R}"),
            $"performance-resolve-warmup-passes={ResolveWarmupPasses}",
            $"performance-resolve-measured-passes={ResolveMeasuredPasses}",
            $"performance-whole-step-warmup-steps={WholeStepWarmupSteps}",
            $"performance-whole-step-measured-steps={WholeStepMeasuredSteps}",
            FormattableString.Invariant($"inherited-h28-median-wall-ratio-limit={HistoricalMedianWallRatioLimit:R}"),
            FormattableString.Invariant($"inherited-h28-p95-wall-ratio-limit={HistoricalP95WallRatioLimit:R}"),
            FormattableString.Invariant($"inherited-h28-median-allocation-ratio-limit={HistoricalMedianAllocationRatioLimit:R}"),
        ], Utf8WithoutBom);
    }

    private static void WriteVr2ReferenceCorpus(string directory, IEnumerable<Vr2CorpusRow> rows)
    {
        var lines = new List<string>
        {
            "point_id,source_family,reference_region,reference_phase,reference_temperature_c,reference_pressure_mpa,specific_volume_m3_kg,specific_u_j_kg,reference_quality,production_resolved,production_phase,production_temperature_c,production_pressure_mpa"
        };
        lines.AddRange(rows.Select(static row => string.Join(",", new[]
        {
            row.PointId,
            row.SourceFamily,
            row.ReferenceRegion,
            row.ReferencePhase,
            F(row.ReferenceTemperatureCelsius),
            F(row.ReferencePressureMegapascals),
            F(row.SpecificVolume),
            F(row.SpecificEnergy),
            F(row.ReferenceQuality),
            row.ProductionResolved.ToString(CultureInfo.InvariantCulture),
            row.ProductionPhase,
            F(row.ProductionTemperatureCelsius),
            F(row.ProductionPressureMegapascals),
        })));
        File.WriteAllLines(Path.Combine(directory, "02-vr2-reference-point-corpus.csv"), lines, Utf8WithoutBom);
    }

    private static void WriteSeamProbeMap(string directory, IEnumerable<SeamProbeRow> rows)
    {
        var lines = new List<string>
        {
            "boundary_index,boundary_temperature_c,probe_side,reference_region,reference_phase,reference_pressure_mpa,specific_volume_m3_kg,specific_u_j_kg,reference_quality,production_resolved,production_phase,production_temperature_c,production_pressure_mpa,phase_matches,production_minus_reference_temperature_c,production_minus_reference_pressure_mpa"
        };
        lines.AddRange(rows.Select(static row => string.Join(",", new[]
        {
            row.BoundaryIndex.ToString(CultureInfo.InvariantCulture),
            F(row.BoundaryTemperatureCelsius),
            row.ProbeSide,
            row.ReferenceRegion,
            row.ReferencePhase,
            F(row.ReferencePressureMegapascals),
            F(row.SpecificVolume),
            F(row.SpecificEnergy),
            F(row.ReferenceQuality),
            row.ProductionResolved.ToString(CultureInfo.InvariantCulture),
            row.ProductionPhase,
            F(row.ProductionTemperatureCelsius),
            F(row.ProductionPressureMegapascals),
            row.PhaseMatches.ToString(CultureInfo.InvariantCulture),
            F(row.ProductionMinusReferenceTemperatureCelsius),
            F(row.ProductionMinusReferencePressureMegapascals),
        })));
        File.WriteAllLines(Path.Combine(directory, "05-seam-probe-map.csv"), lines, Utf8WithoutBom);
    }

    private static void WritePerformanceBaseline(string directory, PerformanceBaseline performance)
    {
        File.WriteAllLines(Path.Combine(directory, "06-performance-baseline.csv"),
        [
            "metric,value",
            $"resolve_measured_call_count,{performance.ResolveMeasuredCallCount}",
            $"whole_step_measured_count,{performance.WholeStepMeasuredCount}",
            $"whole_step_trip_count,{performance.WholeStepTripCount}",
            $"baseline_resolve_median_us,{F(performance.ResolveMedianMicroseconds)}",
            $"baseline_resolve_p95_us,{F(performance.ResolveP95Microseconds)}",
            $"baseline_resolve_max_us,{F(performance.ResolveMaximumMicroseconds)}",
            $"baseline_resolve_median_allocated_bytes,{F(performance.ResolveMedianAllocatedBytes)}",
            $"baseline_exact_v9_step_median_us,{F(performance.WholeStepMedianMicroseconds)}",
            $"baseline_exact_v9_step_p95_us,{F(performance.WholeStepP95Microseconds)}",
            $"baseline_exact_v9_step_max_us,{F(performance.WholeStepMaximumMicroseconds)}",
            $"baseline_exact_v9_step_median_allocated_bytes,{F(performance.WholeStepMedianAllocatedBytes)}",
            $"baseline_exact_v9_step_p95_margin_to_10ms_us,{F(performance.WholeStepP95MarginToTenMillisecondsMicroseconds)}",
            $"rp1b_candidate_resolve_median_ceiling_us,{F(performance.CandidateResolveMedianCeilingMicroseconds)}",
            $"rp1b_candidate_resolve_p95_ceiling_us,{F(performance.CandidateResolveP95CeilingMicroseconds)}",
            $"rp1b_candidate_resolve_max_ceiling_us,{F(performance.CandidateResolveMaximumCeilingMicroseconds)}",
            $"rp1b_candidate_resolve_median_allocated_bytes_ceiling,{F(performance.CandidateResolveMedianAllocatedBytesCeiling)}",
            "ceiling_basis=validated-H28-relative-cost-ratios-applied-to-RP1A-machine-local-current-closure-baseline",
            "whole_step_baseline_role=observation-only-context-RP1B-does-not-claim-integrated-runtime-qualification",
        ], Utf8WithoutBom);
    }

    private static void WriteSummary(
        string directory,
        bool pass,
        IReadOnlyCollection<Vr2CorpusRow> vr2Rows,
        IReadOnlyCollection<FrozenNodeRow> nodeRows,
        int hydraulicRowCount,
        IReadOnlyCollection<SeamProbeRow> seamRows,
        int unresolvedSeamCount,
        bool deterministicSeamRepeat,
        PerformanceBaseline performance)
    {
        File.WriteAllLines(Path.Combine(directory, "07-rp1a-summary.txt"),
        [
            "gate=VR2-ENGINEERING-REPAIR-PLANNING1-RP1A",
            $"status={(pass ? "PASS" : "RED")}",
            $"vr2-reference-corpus-rows={vr2Rows.Count}",
            $"exact-v9-node-corpus-rows={nodeRows.Count}",
            $"hydraulic-context-rows={hydraulicRowCount}",
            $"reference-region4-node-rows={nodeRows.Count(static row => string.Equals(row.ReferenceRegion, "REGION-4-MIXTURE", StringComparison.Ordinal))}",
            $"reference-region1-node-rows={nodeRows.Count(static row => string.Equals(row.ReferenceRegion, "REGION-1", StringComparison.Ordinal))}",
            $"seam-probe-rows={seamRows.Count}",
            $"seam-boundary-count={seamRows.Select(static row => row.BoundaryIndex).Distinct().Count()}",
            $"seam-production-unresolved-count={unresolvedSeamCount}",
            $"seam-phase-mismatch-count={seamRows.Count(static row => row.ProductionResolved && !row.PhaseMatches)}",
            $"deterministic-seam-repeat={deterministicSeamRepeat}",
            FormattableString.Invariant($"baseline-resolve-median-us={performance.ResolveMedianMicroseconds:R}"),
            FormattableString.Invariant($"baseline-resolve-p95-us={performance.ResolveP95Microseconds:R}"),
            FormattableString.Invariant($"baseline-exact-v9-step-p95-us={performance.WholeStepP95Microseconds:R}"),
            FormattableString.Invariant($"baseline-exact-v9-step-p95-margin-to-10ms-us={performance.WholeStepP95MarginToTenMillisecondsMicroseconds:R}"),
            FormattableString.Invariant($"rp1b-candidate-resolve-median-ceiling-us={performance.CandidateResolveMedianCeilingMicroseconds:R}"),
            FormattableString.Invariant($"rp1b-candidate-resolve-p95-ceiling-us={performance.CandidateResolveP95CeilingMicroseconds:R}"),
            FormattableString.Invariant($"rp1b-candidate-resolve-max-ceiling-us={performance.CandidateResolveMaximumCeilingMicroseconds:R}"),
            FormattableString.Invariant($"rp1b-candidate-resolve-median-allocated-bytes-ceiling={performance.CandidateResolveMedianAllocatedBytesCeiling:R}"),
            "candidate-timing-inspected=False",
            "rp1b-authorized=False",
            "production-repair-authorized=False",
            "thermodynamic-tolerance-change-authorized=False",
            "exact-v9-change-authorized=False",
            "vr3-authorized=False",
            "next-action=Return complete RP1A artifact folder for corpus/seam/performance review before RP1B implementation.",
        ], Utf8WithoutBom);
    }

    private static string ResetArtifactDirectory(string root)
    {
        var path = Path.Combine(root, ArtifactDirectoryRelative.Replace('/', Path.DirectorySeparatorChar));
        if (Directory.Exists(path)) Directory.Delete(path, recursive: true);
        Directory.CreateDirectory(path);
        return path;
    }

    private static void RequireOptIn()
        => Assert.Equal("1", Environment.GetEnvironmentVariable(OptInEnvironmentVariable));

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "NuclearReactorSimulator.sln"))) return current.FullName;
            current = current.Parent;
        }
        throw new InvalidOperationException("Could not locate NuclearReactorSimulator.sln from test base directory.");
    }

    private static double TicksToMicroseconds(long ticks) => ticks * 1_000_000d / Stopwatch.Frequency;

    private static bool IsFinitePositive(double value) => double.IsFinite(value) && value > 0d;

    private static double Median(double[] values)
    {
        var sorted = values.OrderBy(static value => value).ToArray();
        if (sorted.Length == 0) return double.NaN;
        var middle = sorted.Length / 2;
        return sorted.Length % 2 == 0 ? 0.5d * (sorted[middle - 1] + sorted[middle]) : sorted[middle];
    }

    private static double Percentile(double[] values, double percentile)
    {
        var sorted = values.OrderBy(static value => value).ToArray();
        if (sorted.Length == 0) return double.NaN;
        var index = (int)Math.Ceiling(percentile * sorted.Length) - 1;
        return sorted[Math.Clamp(index, 0, sorted.Length - 1)];
    }

    private static string F(double value) => double.IsNaN(value) ? string.Empty : value.ToString("R", CultureInfo.InvariantCulture);

    private sealed record StatePoint(string Id, double TemperatureCelsius, double PressureMegapascals, string Region, FluidPhase ExpectedPhase);
    private sealed record BenchmarkState(string Id, double SpecificVolume, double SpecificEnergy);
    private sealed record TimedRow(double Microseconds, double AllocatedBytes);

    private sealed record FrozenNodeRow(
        string ProbeId,
        long LogicalStep,
        double ElapsedSeconds,
        string NodeId,
        string ProductionPhase,
        double ProductionDensityKilogramsPerCubicMetre,
        double ProductionSpecificInternalEnergyJoulesPerKilogram,
        bool ReferenceResolved,
        string ReferenceRegion,
        double ReferenceTemperatureCelsius,
        double ReferencePressureMegapascals);

    private sealed record Vr2CorpusRow(
        string PointId,
        string SourceFamily,
        string ReferenceRegion,
        string ReferencePhase,
        double ReferenceTemperatureCelsius,
        double ReferencePressureMegapascals,
        double SpecificVolume,
        double SpecificEnergy,
        double ReferenceQuality,
        bool ProductionResolved,
        string ProductionPhase,
        double ProductionTemperatureCelsius,
        double ProductionPressureMegapascals);

    private sealed record SeamProbeRow(
        int BoundaryIndex,
        double BoundaryTemperatureCelsius,
        string ProbeSide,
        string ReferenceRegion,
        string ReferencePhase,
        double ReferencePressureMegapascals,
        double SpecificVolume,
        double SpecificEnergy,
        double ReferenceQuality,
        bool ProductionResolved,
        string ProductionPhase,
        double ProductionTemperatureCelsius,
        double ProductionPressureMegapascals,
        bool PhaseMatches,
        double ProductionMinusReferenceTemperatureCelsius,
        double ProductionMinusReferencePressureMegapascals);

    private sealed record PerformanceBaseline(
        int ResolveMeasuredCallCount,
        int WholeStepMeasuredCount,
        int WholeStepTripCount,
        double ResolveMedianMicroseconds,
        double ResolveP95Microseconds,
        double ResolveMaximumMicroseconds,
        double ResolveMedianAllocatedBytes,
        double WholeStepMedianMicroseconds,
        double WholeStepP95Microseconds,
        double WholeStepMaximumMicroseconds,
        double WholeStepMedianAllocatedBytes,
        double WholeStepP95MarginToTenMillisecondsMicroseconds,
        double CandidateResolveMedianCeilingMicroseconds,
        double CandidateResolveP95CeilingMicroseconds,
        double CandidateResolveMaximumCeilingMicroseconds,
        double CandidateResolveMedianAllocatedBytesCeiling);
}
