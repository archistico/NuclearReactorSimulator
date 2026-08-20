using System.Globalization;
using System.Text;
using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios.Training;
using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Simulation.Physics.Control.Integration;
using NuclearReactorSimulator.Simulation.Physics.Fluids;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay;

/// <summary>
/// I.5 REV1 Hotfix 10 opt-in repair-candidate audit. Production/default composition remains on the historical
/// water/steam closure. This audit proves that the correlation-consistent candidate removes the two internal
/// inverse-domain defect families before any registered initial-condition version or production policy is changed.
/// </summary>
public sealed class PhaseIThermodynamicInverseDomainRepairCandidateAuditTests
{
    private static readonly TimeSpan Step = TimeSpan.FromMilliseconds(10d);
    private const int StepsPerSecond = 100;
    private const int WarmupSteps = 10 * StepsPerSecond;
    private const int LoadSegmentSteps = 30 * StepsPerSecond;
    private static readonly UTF8Encoding Utf8WithoutBom = new(encoderShouldEmitUTF8Identifier: false);

    [Fact(Explicit = true)]
    [Trait("Category", "PhaseIThermodynamicInverseDomainRepairCandidate")]
    public void CorrelationConsistentCandidate_ClosesKnownVaporAndLowTemperatureInverseDomainDefects()
    {
        ResetDirectory(TopologyReportDirectory());
        var repaired = new SimplifiedWaterSteamThermodynamicModel(
            WaterSteamThermodynamicClosureMode.CorrelationConsistentInverseDomain);

        var observed = new[]
        {
            Probe("desktop-v3-exhaust", 39.711673348724119d, 2_443_634.7334251222d),
            Probe("desktop-v2-exhaust", 32.501022453035212d, 2_447_440.7115321835d),
            Probe("m9.7-negative-regression", 65.477888248812704d, 2_434_355d),
        };
        var observedResolved = observed.Count(item => Resolves(repaired, item.SpecificVolume, item.SpecificInternalEnergy));

        var seamTemperaturesCelsius = new[] { 20d, 80d, 140d, 180d, 220d, 280d, 330d };
        var seamRows = new List<SeamRow>();
        foreach (var temperatureCelsius in seamTemperaturesCelsius)
        {
            var saturation = repaired.GetSaturationProperties(Temperature.FromDegreesCelsius(temperatureCelsius));
            var specificVolume = saturation.SaturatedVaporSpecificVolumeCubicMetresPerKilogram;
            var boundaryEnergy = saturation.SaturatedVaporInternalEnergy.JoulesPerKilogram;
            var below = Diagnose(repaired, $"seam-below-{temperatureCelsius:G17}", specificVolume, boundaryEnergy - 10d);
            var above = Diagnose(repaired, $"seam-above-{temperatureCelsius:G17}", specificVolume, boundaryEnergy + 10d);

            seamRows.Add(new SeamRow(
                temperatureCelsius,
                specificVolume,
                boundaryEnergy,
                below.SaturatedRootAvailable,
                below.SuperheatedRootAvailable,
                below.MultiplePhaseRootsAvailable,
                above.SaturatedRootAvailable,
                above.SuperheatedRootAvailable,
                above.MultiplePhaseRootsAvailable));
        }

        var lowTemperatureSamples = 0;
        var lowTemperatureResolved = 0;
        var lowTemperatureOutOfRange = 0;
        for (var offsetKelvins = 0.5d; offsetKelvins <= 12d + 1e-12d; offsetKelvins += 0.05d)
        {
            lowTemperatureSamples++;
            var boundary = repaired.GetSaturationProperties(Temperature.FromKelvins(273.16d + offsetKelvins));
            var specificVolume = boundary.SaturatedLiquidSpecificVolumeCubicMetresPerKilogram;
            var targetEnergy = boundary.SaturatedLiquidInternalEnergy.JoulesPerKilogram - 10d;
            if (Resolves(repaired, specificVolume, targetEnergy))
            {
                lowTemperatureResolved++;
            }
            else
            {
                lowTemperatureOutOfRange++;
            }
        }

        WriteTopologyArtifacts(observed, observedResolved, seamRows, lowTemperatureSamples, lowTemperatureResolved, lowTemperatureOutOfRange);

        Assert.Equal(observed.Length, observedResolved);
        Assert.All(seamRows, static row =>
        {
            Assert.True(row.BelowSaturatedRoot);
            Assert.False(row.BelowSuperheatedRoot);
            Assert.False(row.BelowMultipleRoots);
            Assert.False(row.AboveSaturatedRoot);
            Assert.True(row.AboveSuperheatedRoot);
            Assert.False(row.AboveMultipleRoots);
        });
        Assert.Equal(231, lowTemperatureSamples);
        Assert.Equal(lowTemperatureSamples, lowTemperatureResolved);
        Assert.Equal(0, lowTemperatureOutOfRange);
    }

    [Fact(Explicit = true)]
    [Trait("Category", "PhaseIThermodynamicInverseDomainRepairCandidate")]
    public void CorrelationConsistentCandidate_CompletesFrozenLoadRaiseLowerJourneyUnderExplicitAndCorrectedHydraulics()
    {
        ResetDirectory(OperationalReportDirectory());

        var explicitResult = RunJourney("repair-v2-explicit", useFourNodeCorrectedCommit: false);
        var correctedResult = RunJourney("repair-v3-corrected", useFourNodeCorrectedCommit: true);
        WriteJourneyArtifacts(explicitResult, correctedResult);

        Assert.Null(explicitResult.Failure);
        Assert.Null(correctedResult.Failure);
        Assert.Equal(WarmupSteps + (2 * LoadSegmentSteps), explicitResult.SuccessfulSteps);
        Assert.Equal(WarmupSteps + (2 * LoadSegmentSteps), correctedResult.SuccessfulSteps);
    }

    private static WaterSteamInverseBranchSelectionDiagnostic Diagnose(
        SimplifiedWaterSteamThermodynamicModel model,
        string id,
        double specificVolume,
        double specificInternalEnergy)
    {
        var definition = new FluidNodeDefinition(id, Volume.FromCubicMetres(specificVolume));
        var inventory = new FluidNodeInventory(Mass.FromKilograms(1d), Energy.FromJoules(specificInternalEnergy));
        return model.DiagnoseInverseBranchSelection(definition, inventory, PreviousState());
    }

    private static bool Resolves(
        SimplifiedWaterSteamThermodynamicModel model,
        double specificVolume,
        double specificInternalEnergy)
    {
        var definition = new FluidNodeDefinition("repair-probe", Volume.FromCubicMetres(specificVolume));
        var inventory = new FluidNodeInventory(Mass.FromKilograms(1d), Energy.FromJoules(specificInternalEnergy));
        try
        {
            _ = model.Resolve(definition, inventory, PreviousState());
            return true;
        }
        catch (WaterSteamStateOutOfRangeException)
        {
            return false;
        }
    }

    private static JourneyResult RunJourney(string label, bool useFourNodeCorrectedCommit)
    {
        var checkpoints = new List<JourneyCheckpoint>();
        var successfulSteps = 0;

        try
        {
            var engine = Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(
                DesktopSustainedGenerationInitialConditionFactory.CreateThermodynamicInverseDomainRepairEvidenceRuntimeEngine(
                    Step,
                    useFourNodeCorrectedCommit));
            var generator = Assert.Single(engine.CreatePresentationSnapshot(ControlRoomRunState.Paused).Electrical.Generators);
            Advance(engine, "steady", WarmupSteps, checkpoints, ref successfulSteps);
            engine.QueueOperatorCommand(new ControlRoomCommand(
                ControlRoomCommandKind.GeneratorLoadRaise,
                generator.GeneratorId,
                ControlRoomCommandTargetKind.Generator));
            Advance(engine, "load-raise-hold", LoadSegmentSteps, checkpoints, ref successfulSteps);
            engine.QueueOperatorCommand(new ControlRoomCommand(
                ControlRoomCommandKind.GeneratorLoadLower,
                generator.GeneratorId,
                ControlRoomCommandTargetKind.Generator));
            Advance(engine, "load-lower-hold", LoadSegmentSteps, checkpoints, ref successfulSteps);
            return new JourneyResult(label, useFourNodeCorrectedCommit, successfulSteps, checkpoints, null);
        }
        catch (Exception exception)
        {
            return new JourneyResult(label, useFourNodeCorrectedCommit, successfulSteps, checkpoints, exception);
        }
    }

    private static void Advance(
        IntegratedAutomaticOperationRuntimeEngine engine,
        string segment,
        int count,
        List<JourneyCheckpoint> checkpoints,
        ref int successfulSteps)
    {
        for (var step = 1; step <= count; step++)
        {
            var snapshot = engine.Step(ControlRoomRunState.Running);
            successfulSteps++;
            if (step % StepsPerSecond != 0 && step != count)
            {
                continue;
            }

            var exhaust = engine.CurrentState.PlantState.PlantState.GetFluidNode("exhaust");
            var generator = Assert.Single(snapshot.Electrical.Generators);
            var rotor = Assert.Single(snapshot.TurbineSecondary.Rotors);
            checkpoints.Add(new JourneyCheckpoint(
                engine.LogicalStep,
                segment,
                step,
                exhaust.Phase.ToString(),
                exhaust.Volume.CubicMetres / exhaust.Mass.Kilograms,
                exhaust.SpecificInternalEnergy.JoulesPerKilogram,
                exhaust.Pressure.Kilopascals,
                exhaust.Temperature.DegreesCelsius,
                generator.RequestedElectricalPower.NumericValue ?? double.NaN,
                generator.ElectricalOutput.NumericValue ?? double.NaN,
                rotor.ShaftPower.NumericValue ?? double.NaN,
                rotor.Speed.NumericValue ?? double.NaN));
        }
    }

    private static void WriteTopologyArtifacts(
        IReadOnlyList<ProbePoint> observed,
        int observedResolved,
        IReadOnlyList<SeamRow> seamRows,
        int lowTemperatureSamples,
        int lowTemperatureResolved,
        int lowTemperatureOutOfRange)
    {
        var directory = TopologyReportDirectory();
        var seamCsv = new List<string>
        {
            "temperature_c,v_m3_kg,boundary_u_j_kg,below_saturated,below_superheated,below_multiple,above_saturated,above_superheated,above_multiple",
        };
        seamCsv.AddRange(seamRows.Select(row => string.Join(",",
            F(row.TemperatureCelsius), F(row.SpecificVolume), F(row.BoundaryEnergy),
            row.BelowSaturatedRoot, row.BelowSuperheatedRoot, row.BelowMultipleRoots,
            row.AboveSaturatedRoot, row.AboveSuperheatedRoot, row.AboveMultipleRoots)));
        File.WriteAllLines(Path.Combine(directory, "02-repaired-vapor-seam.csv"), seamCsv, Utf8WithoutBom);

        var observedCsv = new List<string> { "label,v_m3_kg,u_j_kg" };
        observedCsv.AddRange(observed.Select(row => $"{row.Label},{F(row.SpecificVolume)},{F(row.SpecificInternalEnergy)}"));
        File.WriteAllLines(Path.Combine(directory, "03-repaired-observed-gap-probes.csv"), observedCsv, Utf8WithoutBom);

        var summary = new[]
        {
            "=== 01-i5-thermodynamic-inverse-domain-repair-topology ===",
            "scope=opt-in CorrelationConsistentInverseDomain candidate only; registered/default runtimes remain HistoricalCorrelationTopology; no acceptance floor or fail-closed rule weakened;",
            $"observed-historical-no-root-probes-resolved={observedResolved}/{observed.Count};",
            $"vapor-seam-two-sided-probes={seamRows.Count}; below-only-saturated={seamRows.Count(static row => row.BelowSaturatedRoot && !row.BelowSuperheatedRoot && !row.BelowMultipleRoots)}/{seamRows.Count}; above-only-superheated={seamRows.Count(static row => !row.AboveSaturatedRoot && row.AboveSuperheatedRoot && !row.AboveMultipleRoots)}/{seamRows.Count};",
            $"low-temperature-census={lowTemperatureSamples}; resolved={lowTemperatureResolved}; out-of-range={lowTemperatureOutOfRange};",
            "candidate-status=TOPOLOGY-REPAIR-PASSES-IF-TEST-GREEN; production-activation=False; next-gate=frozen operational load raise/lower journey under explicit and corrected hydraulics, then H.12-H.30 requalification before activation;",
        };
        File.WriteAllLines(Path.Combine(directory, "01-i5-thermodynamic-inverse-domain-repair-topology.summary.txt"), summary, Utf8WithoutBom);
    }

    private static void WriteJourneyArtifacts(params JourneyResult[] results)
    {
        var directory = OperationalReportDirectory();
        var matrix = new List<string> { "label,corrected_hydraulics,completed,successful_steps,failure_type,failure_message" };
        matrix.AddRange(results.Select(result => string.Join(",",
            result.Label,
            result.UseFourNodeCorrectedCommit,
            result.Failure is null,
            result.SuccessfulSteps,
            result.Failure?.GetType().Name ?? string.Empty,
            Csv(result.Failure?.Message ?? string.Empty))));
        File.WriteAllLines(Path.Combine(directory, "04-repaired-operational-journey-matrix.csv"), matrix, Utf8WithoutBom);

        var trace = new List<string>
        {
            "label,logical_step,segment,segment_step,exhaust_phase,exhaust_v_m3_kg,exhaust_u_j_kg,exhaust_kpa,exhaust_c,request_mwe,gross_mwe,shaft_mw,rotor_rpm",
        };
        foreach (var result in results)
        {
            trace.AddRange(result.Checkpoints.Select(row => string.Join(",",
                result.Label, row.LogicalStep, row.Segment, row.SegmentStep, row.ExhaustPhase,
                F(row.ExhaustSpecificVolume), F(row.ExhaustSpecificInternalEnergy), F(row.ExhaustPressureKilopascals), F(row.ExhaustTemperatureCelsius),
                F(row.RequestMegawatts), F(row.GrossMegawatts), F(row.ShaftMegawatts), F(row.RotorRpm))));
        }
        File.WriteAllLines(Path.Combine(directory, "05-repaired-operational-journey-checkpoints.csv"), trace, Utf8WithoutBom);

        var summary = new List<string>
        {
            "=== 06-i5-thermodynamic-inverse-domain-repair-operational ===",
            "scope=frozen 10 s steady -> 30 s load raise -> 30 s load lower desktop physical seed using opt-in CorrelationConsistentInverseDomain; explicit and corrected hydraulic paths compared independently;",
        };
        summary.AddRange(results.Select(result =>
            $"{result.Label}=corrected-hydraulics:{result.UseFourNodeCorrectedCommit}; completed:{result.Failure is null}; successful-steps:{result.SuccessfulSteps}; failure:{result.Failure?.GetType().Name ?? "NONE"};"));
        summary.Add("candidate-status=OPERATIONAL-REPAIR-PASSES-IF-BOTH-COMPLETE; production-activation=False; phase-i-status=BLOCKED-PENDING-H12-H30-AND-LONG-REQUALIFICATION;");
        File.WriteAllLines(Path.Combine(directory, "06-i5-thermodynamic-inverse-domain-repair-operational.summary.txt"), summary, Utf8WithoutBom);
    }

    private static FluidThermodynamicState PreviousState()
        => new(Pressure.FromKilopascals(101.325d), Temperature.FromDegreesCelsius(20d));

    private static ProbePoint Probe(string label, double specificVolume, double specificInternalEnergy)
        => new(label, specificVolume, specificInternalEnergy);

    private static string F(double value)
        => value.ToString("G17", CultureInfo.InvariantCulture);

    private static string Csv(string value)
        => "\"" + value.Replace("\"", "\"\"") + "\"";

    private static string ReportDirectory()
        => Path.Combine(FindRepositoryRoot(), "artifacts", "i5-thermodynamic-inverse-domain-repair-candidate");

    private static string TopologyReportDirectory()
        => Path.Combine(ReportDirectory(), "topology");

    private static string OperationalReportDirectory()
        => Path.Combine(ReportDirectory(), "operational");

    private static void ResetDirectory(string directory)
    {
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }
        Directory.CreateDirectory(directory);
    }

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "NuclearReactorSimulator.sln")))
            {
                return current.FullName;
            }
            current = current.Parent;
        }
        throw new InvalidOperationException("Could not locate repository root from test output directory.");
    }

    private sealed record ProbePoint(string Label, double SpecificVolume, double SpecificInternalEnergy);

    private sealed record SeamRow(
        double TemperatureCelsius,
        double SpecificVolume,
        double BoundaryEnergy,
        bool BelowSaturatedRoot,
        bool BelowSuperheatedRoot,
        bool BelowMultipleRoots,
        bool AboveSaturatedRoot,
        bool AboveSuperheatedRoot,
        bool AboveMultipleRoots);

    private sealed record JourneyCheckpoint(
        long LogicalStep,
        string Segment,
        int SegmentStep,
        string ExhaustPhase,
        double ExhaustSpecificVolume,
        double ExhaustSpecificInternalEnergy,
        double ExhaustPressureKilopascals,
        double ExhaustTemperatureCelsius,
        double RequestMegawatts,
        double GrossMegawatts,
        double ShaftMegawatts,
        double RotorRpm);

    private sealed record JourneyResult(
        string Label,
        bool UseFourNodeCorrectedCommit,
        int SuccessfulSteps,
        IReadOnlyList<JourneyCheckpoint> Checkpoints,
        Exception? Failure);
}
