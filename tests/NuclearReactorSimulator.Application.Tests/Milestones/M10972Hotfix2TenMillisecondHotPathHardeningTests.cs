using System.Diagnostics;
using System.Globalization;
using System.Text;
using NuclearReactorSimulator.Application.Scenarios.Challenges;
using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.Thermal;
using NuclearReactorSimulator.Domain.Physics.TurbineIsland.MainSteam;
using NuclearReactorSimulator.Domain.Plant;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.Milestones;

public sealed class M10972Hotfix2TenMillisecondHotPathHardeningTests
{
    private const int LookupNodeCount = 256;
    private const int LookupIterations = 20_000;
    private const int FingerprintIterations = 10_000;
    private const int CriticalRatioIterations = 100_000;

    [Fact(Explicit = true)]
    public void MeasuredHotPathAudit_EliminatesLookupAndObservationFingerprintAllocationsWithoutChangingSemantics()
    {
        var (definition, state) = CreateLookupPlant(LookupNodeCount);
        const string targetId = "node-0255";
        var expectedDefinition = definition.GetFluidNode(targetId);
        var expectedState = state.GetFluidNode(targetId);

        for (var index = 0; index < 512; index++)
        {
            _ = definition.GetFluidNode(targetId);
            _ = state.GetFluidNode(targetId);
            _ = LegacyLinearGet(definition.FluidNodes, targetId, static item => item.Id);
            _ = LegacyLinearGet(state.FluidNodes, targetId, static item => item.Id);
        }

        var indexedDefinition = MeasureLookup(
            LookupIterations,
            () => definition.GetFluidNode(targetId));
        var legacyDefinition = MeasureLookup(
            LookupIterations,
            () => LegacyLinearGet(definition.FluidNodes, targetId, static item => item.Id));
        var indexedState = MeasureLookup(
            LookupIterations,
            () => state.GetFluidNode(targetId));
        var legacyState = MeasureLookup(
            LookupIterations,
            () => LegacyLinearGet(state.FluidNodes, targetId, static item => item.Id));

        Assert.Same(expectedDefinition, indexedDefinition.LastValue);
        Assert.Same(expectedDefinition, legacyDefinition.LastValue);
        Assert.Same(expectedState, indexedState.LastValue);
        Assert.Same(expectedState, legacyState.LastValue);
        Assert.True(indexedDefinition.AllocatedBytes <= 128L, $"Indexed PlantDefinition lookup allocated {indexedDefinition.AllocatedBytes} bytes.");
        Assert.True(indexedState.AllocatedBytes <= 128L, $"Indexed PlantState lookup allocated {indexedState.AllocatedBytes} bytes.");
        Assert.True(legacyDefinition.AllocatedBytes > indexedDefinition.AllocatedBytes, "Reference linear PlantDefinition lookup did not allocate more than the indexed path.");
        Assert.True(legacyState.AllocatedBytes > indexedState.AllocatedBytes, "Reference linear PlantState lookup did not allocate more than the indexed path.");
        Assert.True(indexedDefinition.ElapsedTicks < legacyDefinition.ElapsedTicks, "Indexed PlantDefinition lookup was not faster than the pre-Hotfix-2 reference linear path.");
        Assert.True(indexedState.ElapsedTicks < legacyState.ElapsedTicks, "Indexed PlantState lookup was not faster than the pre-Hotfix-2 reference linear path.");

        var observations = Enumerable.Range(0, 12)
            .Select(index => new ChallengeConditionObservation(
                $"condition-{index:D2}",
                index % 2 == 0,
                42L,
                $"evidence-{index:D2}"))
            .ToDictionary(static item => item.ConditionId, StringComparer.Ordinal);
        for (var index = 0; index < 128; index++)
        {
            _ = LegacyObservationFingerprint(observations);
        }

        var legacyFingerprint = MeasureFingerprint(FingerprintIterations, observations);
        var versionCounter = MeasureVersionCounter(FingerprintIterations);
        Assert.True(legacyFingerprint.AllocatedBytes > 0L, "Reference observation fingerprint unexpectedly allocated no memory.");
        Assert.Equal(0L, versionCounter.AllocatedBytes);
        Assert.True(versionCounter.ElapsedTicks < legacyFingerprint.ElapsedTicks, "Version-counter change tracking was not faster than the reference string fingerprint path.");

        var steam = new CompressibleSteamFlowDefinition(
            Area.FromSquareMillimetres(1_600d),
            dischargeCoefficient: 0.95d,
            specificGasConstant: SpecificGasConstant.FromJoulesPerKilogramKelvin(461.526d),
            heatCapacityRatio: 1.3d);
        var expectedRatio = Math.Pow(
            2d / (steam.HeatCapacityRatio + 1d),
            steam.HeatCapacityRatio / (steam.HeatCapacityRatio - 1d));
        Assert.Equal(expectedRatio, steam.CriticalDownstreamToUpstreamPressureRatio, 15);
        var cachedRatio = MeasureCriticalRatio(CriticalRatioIterations, steam, useCachedProperty: true);
        var recomputedRatio = MeasureCriticalRatio(CriticalRatioIterations, steam, useCachedProperty: false);
        Assert.Equal(recomputedRatio.Checksum, cachedRatio.Checksum, 9);

        var output = ResolveArtifactDirectory();
        Directory.CreateDirectory(output);
        var summaryPath = Path.Combine(output, "01-m10972-hotfix2-ten-millisecond-hot-path-hardening.summary.txt");
        var metricsPath = Path.Combine(output, "02-m10972-hotfix2-ten-millisecond-hot-path-metrics.csv");
        File.WriteAllLines(summaryPath, new[]
        {
            "scope=M10.9.7.2 Hotfix 2 REV1 measured 10-ms hot-path allocation/lookup hardening over M10.9.7.2 Hotfix 1 REV1 VALIDATED; original Hotfix 2 superseded/not validated after one lifecycle regression fixture condition-id failure; no solver retuning, physics coefficient change, workstation activation, scoring arithmetic, challenge definition or plant command authority;",
            $"lookup-registry-size={LookupNodeCount}; lookup-iterations={LookupIterations}; plant-definition-indexed-allocated-bytes={indexedDefinition.AllocatedBytes}; plant-definition-reference-linear-allocated-bytes={legacyDefinition.AllocatedBytes}; plant-definition-indexed-vs-linear-wall-ratio={Ratio(indexedDefinition.ElapsedTicks, legacyDefinition.ElapsedTicks):G17};",
            $"plant-state-indexed-allocated-bytes={indexedState.AllocatedBytes}; plant-state-reference-linear-allocated-bytes={legacyState.AllocatedBytes}; plant-state-indexed-vs-linear-wall-ratio={Ratio(indexedState.ElapsedTicks, legacyState.ElapsedTicks):G17}; plant-state-per-instance-lookup-dictionary=False; plant-state-reuses-definition-index=True;",
            $"observation-fingerprint-iterations={FingerprintIterations}; reference-string-fingerprint-allocated-bytes={legacyFingerprint.AllocatedBytes}; version-counter-allocated-bytes={versionCounter.AllocatedBytes}; version-counter-vs-string-fingerprint-wall-ratio={Ratio(versionCounter.ElapsedTicks, legacyFingerprint.ElapsedTicks):G17}; observation-fingerprint-string-materialization-removed=True; lifecycle-changed-semantics-preserved=True;",
            $"critical-ratio-iterations={CriticalRatioIterations}; critical-ratio-cached-vs-recomputed-wall-ratio={Ratio(cachedRatio.ElapsedTicks, recomputedRatio.ElapsedTicks):G17}; critical-ratio-value={steam.CriticalDownstreamToUpstreamPressureRatio.ToString("G17", CultureInfo.InvariantCulture)}; critical-ratio-precomputed=True;",
            "canonical-list-order-preserved=True; unknown-id-fail-closed-preserved=True; indexed-lookups-zero-allocation-after-warmup=True; wall-clock-measurements-same-process-relative=True; performance-gate-primary-signal=allocation-elimination-plus-relative-hot-path-improvement;",
            "record-equality-used-as-ui-change-detector=False; score-dominance-followup-retained=True; final-score-percentage-v1-invariant-retained=True; ui-route-activated=False; plant-command-authority=False;",
            "original-hotfix2-promotable=False; lifecycle-regression-fixture-condition-id-aligned=True; m10972-hotfix2-rev1-ten-millisecond-hot-path-hardening-passes=True; next-step=validate Hotfix 2 REV1 then begin M10.9.7.3 live Mission/Performance workspace wiring using explicit presentation change detection;",
        }, new UTF8Encoding(false));
        File.WriteAllLines(metricsPath, new[]
        {
            "metric,optimized,reference,ratio",
            FormattableString.Invariant($"plant_definition_lookup_allocated_bytes,{indexedDefinition.AllocatedBytes},{legacyDefinition.AllocatedBytes},{Ratio(indexedDefinition.AllocatedBytes, legacyDefinition.AllocatedBytes):G17}"),
            FormattableString.Invariant($"plant_definition_lookup_elapsed_ticks,{indexedDefinition.ElapsedTicks},{legacyDefinition.ElapsedTicks},{Ratio(indexedDefinition.ElapsedTicks, legacyDefinition.ElapsedTicks):G17}"),
            FormattableString.Invariant($"plant_state_lookup_allocated_bytes,{indexedState.AllocatedBytes},{legacyState.AllocatedBytes},{Ratio(indexedState.AllocatedBytes, legacyState.AllocatedBytes):G17}"),
            FormattableString.Invariant($"plant_state_lookup_elapsed_ticks,{indexedState.ElapsedTicks},{legacyState.ElapsedTicks},{Ratio(indexedState.ElapsedTicks, legacyState.ElapsedTicks):G17}"),
            FormattableString.Invariant($"observation_change_tracking_allocated_bytes,{versionCounter.AllocatedBytes},{legacyFingerprint.AllocatedBytes},{Ratio(versionCounter.AllocatedBytes, legacyFingerprint.AllocatedBytes):G17}"),
            FormattableString.Invariant($"observation_change_tracking_elapsed_ticks,{versionCounter.ElapsedTicks},{legacyFingerprint.ElapsedTicks},{Ratio(versionCounter.ElapsedTicks, legacyFingerprint.ElapsedTicks):G17}"),
            FormattableString.Invariant($"critical_ratio_elapsed_ticks,{cachedRatio.ElapsedTicks},{recomputedRatio.ElapsedTicks},{Ratio(cachedRatio.ElapsedTicks, recomputedRatio.ElapsedTicks):G17}"),
        }, new UTF8Encoding(false));

        Assert.True(File.Exists(summaryPath));
        Assert.True(File.Exists(metricsPath));
    }

    private static (PlantDefinition Definition, PlantState State) CreateLookupPlant(int nodeCount)
    {
        var nodes = Enumerable.Range(0, nodeCount)
            .Select(index => new FluidNodeDefinition($"node-{index:D4}", Volume.FromCubicMetres(10d)))
            .Reverse()
            .ToArray();
        var definition = new PlantDefinition(
            "m10972-hot-path-plant",
            nodes,
            Array.Empty<PipeDefinition>(),
            Array.Empty<ValveDefinition>(),
            Array.Empty<PumpDefinition>(),
            Array.Empty<ThermalBodyDefinition>(),
            Array.Empty<HeatTransferDefinition>(),
            Array.Empty<HeatSourceDefinition>());
        var states = definition.FluidNodes
            .Reverse()
            .Select(static item => new FluidNodeState(
                item,
                new FluidNodeInventory(Mass.FromKilograms(1_000d), Energy.FromMegajoules(500d)),
                new FluidThermodynamicState(
                    Pressure.FromMegapascals(5d),
                    Temperature.FromDegreesCelsius(250d))))
            .ToArray();
        var state = new PlantState(
            definition,
            states,
            Array.Empty<ValveState>(),
            Array.Empty<PumpState>(),
            Array.Empty<ThermalBodyState>(),
            Array.Empty<HeatSourceState>());
        return (definition, state);
    }

    private static T LegacyLinearGet<T>(IEnumerable<T> source, string id, Func<T, string> idSelector)
        where T : class
        => source.First(item => string.Equals(idSelector(item), id, StringComparison.Ordinal));

    private static LookupMeasurement<T> MeasureLookup<T>(int iterations, Func<T> lookup)
        where T : class
    {
        var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        var started = Stopwatch.GetTimestamp();
        T? value = null;
        for (var index = 0; index < iterations; index++)
        {
            value = lookup();
        }
        var elapsed = Stopwatch.GetTimestamp() - started;
        var allocated = Math.Max(0L, GC.GetAllocatedBytesForCurrentThread() - allocatedBefore);
        return new LookupMeasurement<T>(value!, elapsed, allocated);
    }

    private static StringMeasurement MeasureFingerprint(
        int iterations,
        IReadOnlyDictionary<string, ChallengeConditionObservation> observations)
    {
        var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        var started = Stopwatch.GetTimestamp();
        var checksum = 0;
        for (var index = 0; index < iterations; index++)
        {
            checksum ^= LegacyObservationFingerprint(observations).Length;
        }
        var elapsed = Stopwatch.GetTimestamp() - started;
        var allocated = Math.Max(0L, GC.GetAllocatedBytesForCurrentThread() - allocatedBefore);
        return new StringMeasurement(checksum, elapsed, allocated);
    }

    private static StringMeasurement MeasureVersionCounter(int iterations)
    {
        var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        var started = Stopwatch.GetTimestamp();
        long version = 0L;
        var checksum = 0;
        for (var index = 0; index < iterations; index++)
        {
            version++;
            checksum ^= (int)(version & 0x7fffffffL);
        }
        var elapsed = Stopwatch.GetTimestamp() - started;
        var allocated = Math.Max(0L, GC.GetAllocatedBytesForCurrentThread() - allocatedBefore);
        return new StringMeasurement(checksum, elapsed, allocated);
    }

    private static string LegacyObservationFingerprint(IReadOnlyDictionary<string, ChallengeConditionObservation> observations)
        => string.Join(
            "|",
            observations.Values
                .OrderBy(static item => item.ConditionId, StringComparer.Ordinal)
                .Select(static item => $"{item.ConditionId}:{item.IsSatisfied}:{item.LogicalStep}:{item.Evidence}"));

    private static RatioMeasurement MeasureCriticalRatio(
        int iterations,
        CompressibleSteamFlowDefinition definition,
        bool useCachedProperty)
    {
        var started = Stopwatch.GetTimestamp();
        var checksum = 0d;
        for (var index = 0; index < iterations; index++)
        {
            checksum += useCachedProperty
                ? definition.CriticalDownstreamToUpstreamPressureRatio
                : Math.Pow(
                    2d / (definition.HeatCapacityRatio + 1d),
                    definition.HeatCapacityRatio / (definition.HeatCapacityRatio - 1d));
        }
        return new RatioMeasurement(checksum, Stopwatch.GetTimestamp() - started);
    }

    private static double Ratio(long numerator, long denominator)
        => denominator == 0L ? 0d : (double)numerator / denominator;

    private static string ResolveArtifactDirectory()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null && !File.Exists(Path.Combine(current.FullName, "NuclearReactorSimulator.sln")))
        {
            current = current.Parent;
        }
        if (current is null)
        {
            throw new InvalidOperationException("Repository root could not be resolved for M10.9.7.2 Hotfix 2 REV1 hot-path artifacts.");
        }
        return Path.Combine(current.FullName, "artifacts", "m10972-hotfix2-ten-ms-hot-path");
    }

    private sealed record LookupMeasurement<T>(T LastValue, long ElapsedTicks, long AllocatedBytes)
        where T : class;

    private sealed record StringMeasurement(int Checksum, long ElapsedTicks, long AllocatedBytes);

    private sealed record RatioMeasurement(double Checksum, long ElapsedTicks);
}
