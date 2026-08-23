using System.Globalization;
using System.Text;
using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios;
using NuclearReactorSimulator.Application.Scenarios.Training;
using NuclearReactorSimulator.Domain.Plant;
using NuclearReactorSimulator.Simulation.Plant;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay;

/// <summary>
/// M10 final LR-H1 Diagnostic 3. Exact-v5 is a distinct reference operating-point candidate only; exact-v4
/// remains immutable and production selection remains unchanged until this candidate is qualified.
/// </summary>
public sealed class M10FinalLongFailureDiagnostic3Tests
{
    private const string OptInEnvironmentVariable = "NRS_M10_FINAL_LONG_DIAGNOSTIC3";
    private const int StepsPerSecond = 100;
    private static readonly UTF8Encoding Utf8WithoutBom = new(encoderShouldEmitUTF8Identifier: false);

    [Fact]
    public void ExactV5Candidate_IsDistinctHydraulicallyCoherentAndDoesNotSwitchProductionDefault()
    {
        var v4 = new DesktopSustainedGenerationI5RepairedActivationCandidateInitialConditionFactory();
        var v5 = new DesktopSustainedGenerationReferenceOperatingPointCandidateInitialConditionFactory();
        var registry = new VersionedInitialConditionRegistry(new IVersionedInitialConditionFactory[] { v4, v5 });

        Assert.Same(v4, registry.Resolve(new InitialConditionReference("integrated-operations-desktop-stable", 4)));
        Assert.Same(v5, registry.Resolve(new InitialConditionReference("integrated-operations-desktop-stable", 5)));
        Assert.NotEqual(v4.Descriptor.Reference, v5.Descriptor.Reference);

        var production = DesktopHydraulicProductionPolicySelector.Resolve(
            DesktopHydraulicProductionPolicySelector.I5RepairedProductionPolicy);
        Assert.Equal(DesktopSustainedGenerationI5RepairedActivationCandidateInitialConditionFactory.Reference, production.InitialCondition);

        var engine = Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(v5.CreateRuntimeEngine());
        Assert.Equal(TimeSpan.FromMilliseconds(10d), engine.FixedDeltaTime);

        var primary = engine.LatestCanonicalSnapshot.Control.ProtectedControl.FullPlant.IntegratedCycle.PrimaryCircuit;
        Assert.Equal(
            HydraulicNumericalCouplingMode.FourNodeBranchContinuityCorrectedCommitOptIn,
            primary.HydraulicNumerics.Mode);

        Assert.InRange(primary.MainCirculation.TotalPumpMassFlowRate.KilogramsPerSecond, 250d, 270d);
        Assert.InRange(primary.MainCirculation.TotalChannelMassFlowRate.KilogramsPerSecond, 250d, 270d);
        Assert.InRange(primary.MainCirculation.TotalReturnMassFlowRate.KilogramsPerSecond, 250d, 270d);
        Assert.InRange(
            Math.Abs(primary.MainCirculation.TotalChannelMassFlowRate.KilogramsPerSecond
                - primary.MainCirculation.TotalReturnMassFlowRate.KilogramsPerSecond),
            0d,
            2d);
    }

    [Fact(Explicit = true)]
    [Trait("Category", "M10FinalLongDiagnostic3")]
    public void LR_H1_ExactV5_ReferenceOperatingPointSixHundredSecondCensus()
    {
        RequireOptIn();
        ResetDirectory(ReportDirectory());
        const int totalSteps = 60_000;

        var factory = new DesktopSustainedGenerationReferenceOperatingPointCandidateInitialConditionFactory();
        var engine = Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(factory.CreateRuntimeEngine());
        var rows = new List<Sample>();
        var tripSteps = 0;
        var correctedCommits = 0;
        var rollbacks = 0;

        Capture(engine, 0, rows);
        for (var step = 1; step <= totalSteps; step++)
        {
            var presentation = engine.Step(ControlRoomRunState.Running);
            if (presentation.AnyTripActive)
            {
                tripSteps++;
            }

            var telemetry = engine.LatestCanonicalSnapshot.Control.ProtectedControl.FullPlant
                .IntegratedCycle.PrimaryCircuit.HydraulicNumerics.FourNodeBranchContinuity;
            if (telemetry?.CorrectedCandidateCommitted == true)
            {
                correctedCommits++;
            }
            if (telemetry?.RollbackRequired == true)
            {
                rollbacks++;
            }

            if (step % StepsPerSecond == 0)
            {
                Capture(engine, step, rows);
            }
            if (step % 3000 == 0)
            {
                AppendProgress($"LR-H1 diagnostic3 exact-v5 simulated-seconds={step / StepsPerSecond}; logical-step={step}");
            }
        }

        WriteTrajectory(rows);
        var finalWindow = rows.Where(static item => item.SimulatedSeconds >= 540d).ToArray();
        var outletMassSlope = Slope(finalWindow, static item => item.OutletMassKilograms);
        var continuityResidualMean = finalWindow.Average(static item => item.ChannelReturnResidualKilogramsPerSecond);
        var continuityResidualSlope = Slope(finalWindow, static item => item.ChannelReturnResidualKilogramsPerSecond);
        var drumLevelSlope = Slope(finalWindow, static item => item.DrumLevelFraction);
        var outletSpecificVolumeSlope = Slope(finalWindow, static item => item.OutletSpecificVolumeCubicMetresPerKilogram);
        var outletSpecificEnergySlope = Slope(finalWindow, static item => item.OutletSpecificInternalEnergyJoulesPerKilogram);
        var fuelTemperatureSlope = Slope(finalWindow, static item => item.FuelTemperatureCelsius);
        var structureTemperatureSlope = Slope(finalWindow, static item => item.StructureTemperatureCelsius);

        File.WriteAllLines(Path.Combine(ReportDirectory(), "41-v5-final60-summary.txt"), new[]
        {
            "scope=exact-v5 600 s reference operating-point candidate; exact-v4 immutable; production selector unchanged; no long replacement authorization implied;",
            "reference-target-primary-flow-kg-s=260; channel-resistance-pa-s2-kg2=25; return-resistance-pa-s2-kg2=25; rated-pump-head-pa=1000000;",
            "reference-pressure-grade-mpa=suction:12.176459281680371|pressure:9.796459281680372|outlet:8.106459281680372|drum:6.416459281680372;",
            "reference-outlet=saturated-mixture|quality:0.035881742881444335|temperature-c:295.93357730105606;",
            "reference-solids-c=fuel:316.93357730105606|structure:301.93357730105606; unchanged initial fission power=30 MW;",
            FormattableString.Invariant($"outlet-mass-slope-kg-s={outletMassSlope:G17};"),
            FormattableString.Invariant($"channel-return-residual-mean-kg-s={continuityResidualMean:G17};"),
            FormattableString.Invariant($"channel-return-residual-slope-kg-s2={continuityResidualSlope:G17};"),
            FormattableString.Invariant($"drum-level-slope-fraction-per-s={drumLevelSlope:G17};"),
            FormattableString.Invariant($"outlet-specific-volume-slope-m3-kg-s={outletSpecificVolumeSlope:G17};"),
            FormattableString.Invariant($"outlet-specific-u-slope-j-kg-s={outletSpecificEnergySlope:G17};"),
            FormattableString.Invariant($"fuel-temperature-slope-c-per-s={fuelTemperatureSlope:G17};"),
            FormattableString.Invariant($"structure-temperature-slope-c-per-s={structureTemperatureSlope:G17};"),
            FormattableString.Invariant($"trip-steps={tripSteps}; corrected-commits={correctedCommits}; rollbacks={rollbacks};"),
            "decision-rule=qualify only after returned artifacts show bounded/no material monotonic outlet inventory drift, bounded drum-level behavior and no new thermal-body drift; thresholds are not frozen by this diagnostic candidate;",
        }, Utf8WithoutBom);

        var initial = rows[0];
        File.WriteAllLines(Path.Combine(ReportDirectory(), "42-v5-initial-reference-point.txt"), new[]
        {
            FormattableString.Invariant($"pump-flow-kg-s={initial.PumpFlowKilogramsPerSecond:G17};"),
            FormattableString.Invariant($"channel-flow-kg-s={initial.ChannelFlowKilogramsPerSecond:G17};"),
            FormattableString.Invariant($"return-flow-kg-s={initial.ReturnFlowKilogramsPerSecond:G17};"),
            FormattableString.Invariant($"channel-return-residual-kg-s={initial.ChannelReturnResidualKilogramsPerSecond:G17};"),
            FormattableString.Invariant($"suction-pressure-pa={initial.SuctionPressurePascals:G17};"),
            FormattableString.Invariant($"pressure-header-pa={initial.PressureHeaderPascals:G17};"),
            FormattableString.Invariant($"outlet-pressure-pa={initial.OutletPressurePascals:G17};"),
            FormattableString.Invariant($"drum-pressure-pa={initial.DrumPressurePascals:G17};"),
            FormattableString.Invariant($"outlet-v-m3-kg={initial.OutletSpecificVolumeCubicMetresPerKilogram:G17}; outlet-u-j-kg={initial.OutletSpecificInternalEnergyJoulesPerKilogram:G17}; outlet-phase={initial.OutletPhase}; outlet-quality={initial.OutletVaporQuality?.ToString("G17", CultureInfo.InvariantCulture) ?? string.Empty};"),
            FormattableString.Invariant($"fuel-c={initial.FuelTemperatureCelsius:G17}; structure-c={initial.StructureTemperatureCelsius:G17}; drum-level={initial.DrumLevelFraction:G17};"),
        }, Utf8WithoutBom);

        Assert.Equal(totalSteps, engine.LogicalStep);
        Assert.Equal(0, tripSteps);
        Assert.Equal(0, rollbacks);
        Assert.All(rows, static item => Assert.True(item.AllFinite));
    }

    private static void Capture(IntegratedAutomaticOperationRuntimeEngine engine, int logicalStep, List<Sample> destination)
    {
        var protectedControl = engine.LatestCanonicalSnapshot.Control.ProtectedControl;
        var primary = protectedControl.FullPlant.IntegratedCycle.PrimaryCircuit;
        var plant = protectedControl.FullPlant.CandidatePlant;
        var drum = Assert.Single(primary.SteamDrums.Drums);
        var outlet = plant.GetFluidNode("outlet");
        var levelController = protectedControl.TurbineSecondary.ControlAndActuator.Controllers.GetDiagnostic("level-control");

        destination.Add(new Sample(
            logicalStep,
            logicalStep / (double)StepsPerSecond,
            outlet.Mass.Kilograms,
            outlet.Volume.CubicMetres / outlet.Mass.Kilograms,
            outlet.SpecificInternalEnergy.JoulesPerKilogram,
            outlet.Pressure.Pascals,
            outlet.Temperature.DegreesCelsius,
            outlet.Phase.ToString(),
            outlet.VaporQuality?.Fraction,
            plant.GetFluidNode("suction").Pressure.Pascals,
            plant.GetFluidNode("pressure").Pressure.Pascals,
            plant.GetFluidNode("drum").Pressure.Pascals,
            primary.MainCirculation.TotalPumpMassFlowRate.KilogramsPerSecond,
            primary.MainCirculation.TotalChannelMassFlowRate.KilogramsPerSecond,
            primary.MainCirculation.TotalReturnMassFlowRate.KilogramsPerSecond,
            primary.MainCirculation.TotalChannelMassFlowRate.KilogramsPerSecond - primary.MainCirculation.TotalReturnMassFlowRate.KilogramsPerSecond,
            drum.LiquidLevelFraction.Fraction,
            plant.GetFluidNode("drum").Mass.Kilograms,
            plant.GetThermalBody("fuel").Temperature.DegreesCelsius,
            plant.GetThermalBody("structure").Temperature.DegreesCelsius,
            levelController.Error,
            levelController.IntegralTerm,
            levelController.Output));
    }

    private static void WriteTrajectory(IEnumerable<Sample> rows)
    {
        var lines = new List<string>
        {
            "logical_step,simulated_seconds,outlet_mass_kg,outlet_v_m3_kg,outlet_u_j_kg,outlet_pressure_pa,outlet_temperature_c,outlet_phase,outlet_quality,suction_pressure_pa,pressure_header_pa,drum_pressure_pa,pump_flow_kg_s,channel_flow_kg_s,return_flow_kg_s,channel_return_residual_kg_s,drum_level_fraction,drum_mass_kg,fuel_temperature_c,structure_temperature_c,level_controller_error,level_controller_integral,level_controller_output"
        };
        lines.AddRange(rows.Select(static item => string.Join(",",
            item.LogicalStep,
            F(item.SimulatedSeconds),
            F(item.OutletMassKilograms),
            F(item.OutletSpecificVolumeCubicMetresPerKilogram),
            F(item.OutletSpecificInternalEnergyJoulesPerKilogram),
            F(item.OutletPressurePascals),
            F(item.OutletTemperatureCelsius),
            item.OutletPhase,
            item.OutletVaporQuality.HasValue ? F(item.OutletVaporQuality.Value) : string.Empty,
            F(item.SuctionPressurePascals),
            F(item.PressureHeaderPascals),
            F(item.DrumPressurePascals),
            F(item.PumpFlowKilogramsPerSecond),
            F(item.ChannelFlowKilogramsPerSecond),
            F(item.ReturnFlowKilogramsPerSecond),
            F(item.ChannelReturnResidualKilogramsPerSecond),
            F(item.DrumLevelFraction),
            F(item.DrumMassKilograms),
            F(item.FuelTemperatureCelsius),
            F(item.StructureTemperatureCelsius),
            F(item.LevelControllerError),
            F(item.LevelControllerIntegral),
            F(item.LevelControllerOutput))));
        File.WriteAllLines(Path.Combine(ReportDirectory(), "40-v5-reference-trajectory.csv"), lines, Utf8WithoutBom);
    }

    private static double Slope(IReadOnlyList<Sample> rows, Func<Sample, double> selector)
    {
        if (rows.Count < 2)
        {
            return 0d;
        }

        var meanX = rows.Average(static item => item.SimulatedSeconds);
        var meanY = rows.Average(selector);
        var numerator = 0d;
        var denominator = 0d;
        foreach (var row in rows)
        {
            var dx = row.SimulatedSeconds - meanX;
            numerator += dx * (selector(row) - meanY);
            denominator += dx * dx;
        }
        return denominator == 0d ? 0d : numerator / denominator;
    }

    private static string F(double value) => value.ToString("G17", CultureInfo.InvariantCulture);

    private static void RequireOptIn()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable(OptInEnvironmentVariable), "1", StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Set {OptInEnvironmentVariable}=1 to run M10 final long Diagnostic 3.");
        }
    }

    private static string ReportDirectory()
        => Path.Combine(FindRepositoryRoot(), "artifacts", "m10-final-long-diagnostic3");

    private static void AppendProgress(string message)
    {
        Directory.CreateDirectory(ReportDirectory());
        File.AppendAllText(
            Path.Combine(ReportDirectory(), "00-progress.txt"),
            $"{DateTimeOffset.UtcNow:O} {message}{Environment.NewLine}",
            Utf8WithoutBom);
    }

    private static void ResetDirectory(string directory)
    {
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }
        Directory.CreateDirectory(directory);
        File.WriteAllText(
            Path.Combine(directory, "00-progress.txt"),
            $"M10 FINAL LONG FAILURE DIAGNOSTIC 3 STARTED{Environment.NewLine}",
            Utf8WithoutBom);
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

    private sealed record Sample(
        int LogicalStep,
        double SimulatedSeconds,
        double OutletMassKilograms,
        double OutletSpecificVolumeCubicMetresPerKilogram,
        double OutletSpecificInternalEnergyJoulesPerKilogram,
        double OutletPressurePascals,
        double OutletTemperatureCelsius,
        string OutletPhase,
        double? OutletVaporQuality,
        double SuctionPressurePascals,
        double PressureHeaderPascals,
        double DrumPressurePascals,
        double PumpFlowKilogramsPerSecond,
        double ChannelFlowKilogramsPerSecond,
        double ReturnFlowKilogramsPerSecond,
        double ChannelReturnResidualKilogramsPerSecond,
        double DrumLevelFraction,
        double DrumMassKilograms,
        double FuelTemperatureCelsius,
        double StructureTemperatureCelsius,
        double LevelControllerError,
        double LevelControllerIntegral,
        double LevelControllerOutput)
    {
        public bool AllFinite => new[]
        {
            OutletMassKilograms,
            OutletSpecificVolumeCubicMetresPerKilogram,
            OutletSpecificInternalEnergyJoulesPerKilogram,
            OutletPressurePascals,
            OutletTemperatureCelsius,
            SuctionPressurePascals,
            PressureHeaderPascals,
            DrumPressurePascals,
            PumpFlowKilogramsPerSecond,
            ChannelFlowKilogramsPerSecond,
            ReturnFlowKilogramsPerSecond,
            ChannelReturnResidualKilogramsPerSecond,
            DrumLevelFraction,
            DrumMassKilograms,
            FuelTemperatureCelsius,
            StructureTemperatureCelsius,
            LevelControllerError,
            LevelControllerIntegral,
            LevelControllerOutput,
        }.All(double.IsFinite)
        && (!OutletVaporQuality.HasValue || double.IsFinite(OutletVaporQuality.Value));
    }
}
