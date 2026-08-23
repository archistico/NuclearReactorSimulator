using System.Globalization;
using System.Text;
using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios.Training;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay;

/// <summary>
/// M10 final LR-H1 Diagnostic 4. Exact-v5 is intentionally reused unchanged after Diagnostic 3 proved that
/// instantaneous hydraulic coherence alone is insufficient: branch continuity becomes nearly balanced, while
/// drum inventory and coupled stored energy continue to drift. This census exposes the canonical mass/energy
/// terms required before another reference operating-point seed is authored.
/// </summary>
public sealed class M10FinalLongFailureDiagnostic4Tests
{
    private const string OptInEnvironmentVariable = "NRS_M10_FINAL_LONG_DIAGNOSTIC4";
    private const int StepsPerSecond = 100;
    private const int TotalSteps = 60_000;
    private const double DeltaTimeSeconds = 0.01d;
    private static readonly UTF8Encoding Utf8WithoutBom = new(encoderShouldEmitUTF8Identifier: false);

    [Fact(Explicit = true)]
    [Trait("Category", "M10FinalLongDiagnostic4")]
    public void LR_H1_ExactV5_SixHundredSecondFullPlantMassEnergyBalanceCensus()
    {
        RequireOptIn();
        ResetDirectory(ReportDirectory());

        var factory = new DesktopSustainedGenerationReferenceOperatingPointCandidateInitialConditionFactory();
        var engine = Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(factory.CreateRuntimeEngine());
        var rows = new List<Sample>();
        var tripSteps = 0;
        var correctedCommits = 0;
        var rollbacks = 0;

        Capture(engine, 0, rows);
        for (var step = 1; step <= TotalSteps; step++)
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
                AppendProgress($"LR-H1 diagnostic4 exact-v5 balance census simulated-seconds={step / StepsPerSecond}; logical-step={step}");
            }
        }

        WriteTrajectory(rows);
        WriteFinalWindowSummary(rows, tripSteps, correctedCommits, rollbacks);

        Assert.Equal(TotalSteps, engine.LogicalStep);
        Assert.Equal(0, tripSteps);
        Assert.Equal(0, rollbacks);
        Assert.All(rows, static item => Assert.True(item.AllFinite));
    }

    private static void Capture(IntegratedAutomaticOperationRuntimeEngine engine, int logicalStep, List<Sample> destination)
    {
        var protectedControl = engine.LatestCanonicalSnapshot.Control.ProtectedControl;
        var fullPlant = protectedControl.FullPlant;
        var cycle = fullPlant.IntegratedCycle;
        var primary = cycle.PrimaryCircuit;
        var drum = Assert.Single(primary.SteamDrums.Drums);
        var feedwaterBoundary = Assert.Single(primary.Boundaries.FeedwaterBoundaries);
        var steamExportBoundary = Assert.Single(primary.Boundaries.SteamExportBoundaries);
        var feedwaterTrain = Assert.Single(cycle.CondensateFeedwater.Trains);
        var condenser = Assert.Single(cycle.Condenser.Condensers);
        var generator = Assert.Single(cycle.Generators);
        var plant = fullPlant.CandidatePlant;
        var outlet = plant.GetFluidNode("outlet");
        var heat = cycle.HeatBalance;
        var levelController = protectedControl.TurbineSecondary.ControlAndActuator.Controllers.GetDiagnostic("level-control");

        var feedwaterFlow = primary.TotalFeedwaterMassFlowRate.KilogramsPerSecond;
        var steamExportFlow = primary.TotalSteamExportMassFlowRate.KilogramsPerSecond;
        var drumNetRate = drum.IncomingReturnMassFlowRate.KilogramsPerSecond
            + feedwaterFlow
            - drum.SeparatedSteamMassFlowRate.KilogramsPerSecond
            - drum.RecirculatedLiquidMassFlowRate.KilogramsPerSecond;
        var coupledStoredPowerMegawatts = heat.CoupledStoredEnergyChange.Joules / DeltaTimeSeconds / 1_000_000d;

        destination.Add(new Sample(
            logicalStep,
            logicalStep / (double)StepsPerSecond,
            outlet.Mass.Kilograms,
            plant.GetFluidNode("drum").Mass.Kilograms,
            drum.LiquidLevelFraction.Fraction,
            primary.MainCirculation.TotalPumpMassFlowRate.KilogramsPerSecond,
            primary.MainCirculation.TotalChannelMassFlowRate.KilogramsPerSecond,
            primary.MainCirculation.TotalReturnMassFlowRate.KilogramsPerSecond,
            primary.MainCirculation.TotalChannelMassFlowRate.KilogramsPerSecond - primary.MainCirculation.TotalReturnMassFlowRate.KilogramsPerSecond,
            drum.IncomingReturnMassFlowRate.KilogramsPerSecond,
            drum.SeparatedSteamMassFlowRate.KilogramsPerSecond,
            drum.RecirculatedLiquidMassFlowRate.KilogramsPerSecond,
            drum.RequestedLiquidRecirculationMassFlowRate.KilogramsPerSecond,
            drum.LiquidRecirculationInventoryLimited,
            feedwaterFlow,
            steamExportFlow,
            feedwaterFlow - steamExportFlow,
            primary.Boundaries.NetExternalMassFlowRate.KilogramsPerSecond,
            drumNetRate,
            feedwaterTrain.FeedwaterPump.MassFlowRate.KilogramsPerSecond,
            feedwaterTrain.CondensatePump.MassFlowRate.KilogramsPerSecond,
            condenser.ActualCondensationMassFlowRate.KilogramsPerSecond,
            primary.TotalPlantMass.Kilograms,
            primary.TotalStoredEnergy.Joules,
            primary.Audit.NetAccumulatedMassRate.KilogramsPerSecond,
            primary.Audit.ExpectedExternalMassFlowRate.KilogramsPerSecond,
            primary.Audit.NetAccumulatedEnergyRate.Megawatts,
            primary.Audit.ExpectedExternalPower.Megawatts,
            primary.TotalFissionThermalPower.Megawatts,
            primary.TotalDecayHeatPower.Megawatts,
            primary.TotalNuclearHeatPower.Megawatts,
            heat.PrimaryBoundaryNetExternalPower.Megawatts,
            heat.PumpHydraulicPowerExchange.Megawatts,
            heat.FeedwaterConditioningPower.Megawatts,
            heat.CondenserHeatRejectionPower.Megawatts,
            heat.TurbineShaftPower.Megawatts,
            heat.ElectricalExportPower.Megawatts,
            heat.GeneratorConversionLossPower.Megawatts,
            heat.PassiveRotorMechanicalLossPower.Megawatts,
            heat.NetReactorToGridExternalPower.Megawatts,
            coupledStoredPowerMegawatts,
            heat.FullEnergyPathClosureResidualJoules,
            cycle.ThermofluidAudit.ExpectedExternalMassFlowRate.KilogramsPerSecond,
            cycle.ThermofluidAudit.NetAccumulatedMassRate.KilogramsPerSecond,
            generator.RequestedElectricalPower.Megawatts,
            generator.ElectricalOutputPower.Megawatts,
            outlet.Pressure.Pascals,
            plant.GetFluidNode("drum").Pressure.Pascals,
            outlet.Temperature.DegreesCelsius,
            plant.GetThermalBody("fuel").Temperature.DegreesCelsius,
            plant.GetThermalBody("structure").Temperature.DegreesCelsius,
            levelController.Output));
    }

    private static void WriteTrajectory(IEnumerable<Sample> rows)
    {
        var lines = new List<string>
        {
            "logical_step,simulated_seconds,outlet_mass_kg,drum_mass_kg,drum_level_fraction,pump_flow_kg_s,channel_flow_kg_s,return_flow_kg_s,channel_return_residual_kg_s,drum_incoming_return_kg_s,drum_separated_steam_kg_s,drum_recirculation_kg_s,drum_requested_recirculation_kg_s,drum_recirculation_inventory_limited,feedwater_boundary_kg_s,steam_export_boundary_kg_s,primary_boundary_mass_residual_kg_s,primary_boundary_net_external_mass_kg_s,drum_algebraic_net_mass_rate_kg_s,feedwater_pump_kg_s,condensate_pump_kg_s,condenser_condensation_kg_s,primary_total_mass_kg,primary_total_stored_energy_j,primary_net_accumulated_mass_rate_kg_s,primary_expected_external_mass_rate_kg_s,primary_net_accumulated_energy_rate_mw,primary_expected_external_power_mw,fission_power_mw,decay_heat_power_mw,nuclear_heat_power_mw,primary_boundary_net_external_power_mw,pump_hydraulic_power_mw,feedwater_conditioning_power_mw,condenser_heat_rejection_mw,turbine_shaft_power_mw,electrical_export_mw,generator_conversion_loss_mw,passive_rotor_loss_mw,net_reactor_to_grid_external_power_mw,coupled_stored_energy_change_rate_mw,full_energy_path_closure_residual_j,full_thermofluid_expected_external_mass_kg_s,full_thermofluid_net_accumulated_mass_kg_s,generator_requested_mw,generator_output_mw,outlet_pressure_pa,drum_pressure_pa,outlet_temperature_c,fuel_temperature_c,structure_temperature_c,level_controller_output"
        };
        lines.AddRange(rows.Select(static item => string.Join(",",
            item.LogicalStep,
            F(item.SimulatedSeconds),
            F(item.OutletMassKilograms),
            F(item.DrumMassKilograms),
            F(item.DrumLevelFraction),
            F(item.PumpFlowKilogramsPerSecond),
            F(item.ChannelFlowKilogramsPerSecond),
            F(item.ReturnFlowKilogramsPerSecond),
            F(item.ChannelReturnResidualKilogramsPerSecond),
            F(item.DrumIncomingReturnKilogramsPerSecond),
            F(item.DrumSeparatedSteamKilogramsPerSecond),
            F(item.DrumRecirculationKilogramsPerSecond),
            F(item.DrumRequestedRecirculationKilogramsPerSecond),
            item.DrumRecirculationInventoryLimited ? "True" : "False",
            F(item.FeedwaterBoundaryKilogramsPerSecond),
            F(item.SteamExportBoundaryKilogramsPerSecond),
            F(item.PrimaryBoundaryMassResidualKilogramsPerSecond),
            F(item.PrimaryBoundaryNetExternalMassKilogramsPerSecond),
            F(item.DrumAlgebraicNetMassRateKilogramsPerSecond),
            F(item.FeedwaterPumpKilogramsPerSecond),
            F(item.CondensatePumpKilogramsPerSecond),
            F(item.CondenserCondensationKilogramsPerSecond),
            F(item.PrimaryTotalMassKilograms),
            F(item.PrimaryTotalStoredEnergyJoules),
            F(item.PrimaryNetAccumulatedMassRateKilogramsPerSecond),
            F(item.PrimaryExpectedExternalMassRateKilogramsPerSecond),
            F(item.PrimaryNetAccumulatedEnergyRateMegawatts),
            F(item.PrimaryExpectedExternalPowerMegawatts),
            F(item.FissionPowerMegawatts),
            F(item.DecayHeatPowerMegawatts),
            F(item.NuclearHeatPowerMegawatts),
            F(item.PrimaryBoundaryNetExternalPowerMegawatts),
            F(item.PumpHydraulicPowerMegawatts),
            F(item.FeedwaterConditioningPowerMegawatts),
            F(item.CondenserHeatRejectionMegawatts),
            F(item.TurbineShaftPowerMegawatts),
            F(item.ElectricalExportMegawatts),
            F(item.GeneratorConversionLossMegawatts),
            F(item.PassiveRotorLossMegawatts),
            F(item.NetReactorToGridExternalPowerMegawatts),
            F(item.CoupledStoredEnergyChangeRateMegawatts),
            F(item.FullEnergyPathClosureResidualJoules),
            F(item.FullThermofluidExpectedExternalMassKilogramsPerSecond),
            F(item.FullThermofluidNetAccumulatedMassKilogramsPerSecond),
            F(item.GeneratorRequestedMegawatts),
            F(item.GeneratorOutputMegawatts),
            F(item.OutletPressurePascals),
            F(item.DrumPressurePascals),
            F(item.OutletTemperatureCelsius),
            F(item.FuelTemperatureCelsius),
            F(item.StructureTemperatureCelsius),
            F(item.LevelControllerOutput))));
        File.WriteAllLines(Path.Combine(ReportDirectory(), "50-v5-full-plant-balance-trajectory.csv"), lines, Utf8WithoutBom);
    }

    private static void WriteFinalWindowSummary(
        IReadOnlyList<Sample> rows,
        int tripSteps,
        int correctedCommits,
        int rollbacks)
    {
        var finalWindow = rows.Where(static item => item.SimulatedSeconds >= 540d).ToArray();
        var final = rows[^1];
        var drumMassSlope = Slope(finalWindow, static item => item.DrumMassKilograms);
        var drumLevelSlope = Slope(finalWindow, static item => item.DrumLevelFraction);
        var outletMassSlope = Slope(finalWindow, static item => item.OutletMassKilograms);
        var outletPressureSlope = Slope(finalWindow, static item => item.OutletPressurePascals);
        var drumPressureSlope = Slope(finalWindow, static item => item.DrumPressurePascals);
        var fuelTemperatureSlope = Slope(finalWindow, static item => item.FuelTemperatureCelsius);
        var structureTemperatureSlope = Slope(finalWindow, static item => item.StructureTemperatureCelsius);

        var meanFeedwater = finalWindow.Average(static item => item.FeedwaterBoundaryKilogramsPerSecond);
        var meanSteamExport = finalWindow.Average(static item => item.SteamExportBoundaryKilogramsPerSecond);
        var meanBoundaryMassResidual = finalWindow.Average(static item => item.PrimaryBoundaryMassResidualKilogramsPerSecond);
        var meanDrumAlgebraicNetMass = finalWindow.Average(static item => item.DrumAlgebraicNetMassRateKilogramsPerSecond);
        var meanChannelReturnResidual = finalWindow.Average(static item => item.ChannelReturnResidualKilogramsPerSecond);
        var meanNuclearHeat = finalWindow.Average(static item => item.NuclearHeatPowerMegawatts);
        var meanCondenserHeatRejection = finalWindow.Average(static item => item.CondenserHeatRejectionMegawatts);
        var meanElectricalExport = finalWindow.Average(static item => item.ElectricalExportMegawatts);
        var meanNetExternalPower = finalWindow.Average(static item => item.NetReactorToGridExternalPowerMegawatts);
        var meanStoredPower = finalWindow.Average(static item => item.CoupledStoredEnergyChangeRateMegawatts);
        var meanFeedwaterPumpFlow = finalWindow.Average(static item => item.FeedwaterPumpKilogramsPerSecond);
        var meanSteamSeparationFlow = finalWindow.Average(static item => item.DrumSeparatedSteamKilogramsPerSecond);
        var meanReturnFlow = finalWindow.Average(static item => item.ReturnFlowKilogramsPerSecond);
        var meanRecirculationFlow = finalWindow.Average(static item => item.DrumRecirculationKilogramsPerSecond);

        File.WriteAllLines(Path.Combine(ReportDirectory(), "51-v5-final60-balance-summary.txt"), new[]
        {
            "scope=Diagnostic 4 exact-v5 unchanged 600 s full-plant mass/energy balance census; Diagnostic 3 execution PASS but exact-v5 engineering decision NOT QUALIFIED; production selector remains exact-v4; replacement long remains unauthorized;",
            FormattableString.Invariant($"final-state=drum-level:{final.DrumLevelFraction:G17}|drum-mass-kg:{final.DrumMassKilograms:G17}|outlet-mass-kg:{final.OutletMassKilograms:G17}|pump-flow-kg-s:{final.PumpFlowKilogramsPerSecond:G17}|channel-flow-kg-s:{final.ChannelFlowKilogramsPerSecond:G17}|return-flow-kg-s:{final.ReturnFlowKilogramsPerSecond:G17};"),
            FormattableString.Invariant($"final60-inventory-slopes=outlet-mass-kg-s:{outletMassSlope:G17}|drum-mass-kg-s:{drumMassSlope:G17}|drum-level-fraction-s:{drumLevelSlope:G17};"),
            FormattableString.Invariant($"final60-primary-boundary=feedwater-mean-kg-s:{meanFeedwater:G17}|steam-export-mean-kg-s:{meanSteamExport:G17}|feedwater-minus-export-mean-kg-s:{meanBoundaryMassResidual:G17};"),
            FormattableString.Invariant($"final60-drum-balance=algebraic-net-mass-rate-mean-kg-s:{meanDrumAlgebraicNetMass:G17}|return-mean-kg-s:{meanReturnFlow:G17}|recirculation-mean-kg-s:{meanRecirculationFlow:G17}|separated-steam-mean-kg-s:{meanSteamSeparationFlow:G17}|feedwater-pump-mean-kg-s:{meanFeedwaterPumpFlow:G17};"),
            FormattableString.Invariant($"final60-branch=channel-return-residual-mean-kg-s:{meanChannelReturnResidual:G17};"),
            FormattableString.Invariant($"final60-pressure-thermal-slopes=outlet-pressure-pa-s:{outletPressureSlope:G17}|drum-pressure-pa-s:{drumPressureSlope:G17}|fuel-c-s:{fuelTemperatureSlope:G17}|structure-c-s:{structureTemperatureSlope:G17};"),
            FormattableString.Invariant($"final60-energy-means-mw=nuclear-heat:{meanNuclearHeat:G17}|condenser-rejection:{meanCondenserHeatRejection:G17}|electrical-export:{meanElectricalExport:G17}|net-reactor-to-grid-external:{meanNetExternalPower:G17}|coupled-stored-change-rate:{meanStoredPower:G17};"),
            FormattableString.Invariant($"trip-steps={tripSteps}; corrected-commits={correctedCommits}; rollbacks={rollbacks};"),
            "decision-use=derive the next candidate only from measured mass and energy residual owners; do not promote exact-v5 and do not choose another primary-flow target before reviewing this balance evidence;",
        }, Utf8WithoutBom);
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
            throw new InvalidOperationException($"Set {OptInEnvironmentVariable}=1 to run M10 final long Diagnostic 4.");
        }
    }

    private static string ReportDirectory()
        => Path.Combine(FindRepositoryRoot(), "artifacts", "m10-final-long-diagnostic4");

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
            $"M10 FINAL LONG FAILURE DIAGNOSTIC 4 STARTED{Environment.NewLine}",
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
        double DrumMassKilograms,
        double DrumLevelFraction,
        double PumpFlowKilogramsPerSecond,
        double ChannelFlowKilogramsPerSecond,
        double ReturnFlowKilogramsPerSecond,
        double ChannelReturnResidualKilogramsPerSecond,
        double DrumIncomingReturnKilogramsPerSecond,
        double DrumSeparatedSteamKilogramsPerSecond,
        double DrumRecirculationKilogramsPerSecond,
        double DrumRequestedRecirculationKilogramsPerSecond,
        bool DrumRecirculationInventoryLimited,
        double FeedwaterBoundaryKilogramsPerSecond,
        double SteamExportBoundaryKilogramsPerSecond,
        double PrimaryBoundaryMassResidualKilogramsPerSecond,
        double PrimaryBoundaryNetExternalMassKilogramsPerSecond,
        double DrumAlgebraicNetMassRateKilogramsPerSecond,
        double FeedwaterPumpKilogramsPerSecond,
        double CondensatePumpKilogramsPerSecond,
        double CondenserCondensationKilogramsPerSecond,
        double PrimaryTotalMassKilograms,
        double PrimaryTotalStoredEnergyJoules,
        double PrimaryNetAccumulatedMassRateKilogramsPerSecond,
        double PrimaryExpectedExternalMassRateKilogramsPerSecond,
        double PrimaryNetAccumulatedEnergyRateMegawatts,
        double PrimaryExpectedExternalPowerMegawatts,
        double FissionPowerMegawatts,
        double DecayHeatPowerMegawatts,
        double NuclearHeatPowerMegawatts,
        double PrimaryBoundaryNetExternalPowerMegawatts,
        double PumpHydraulicPowerMegawatts,
        double FeedwaterConditioningPowerMegawatts,
        double CondenserHeatRejectionMegawatts,
        double TurbineShaftPowerMegawatts,
        double ElectricalExportMegawatts,
        double GeneratorConversionLossMegawatts,
        double PassiveRotorLossMegawatts,
        double NetReactorToGridExternalPowerMegawatts,
        double CoupledStoredEnergyChangeRateMegawatts,
        double FullEnergyPathClosureResidualJoules,
        double FullThermofluidExpectedExternalMassKilogramsPerSecond,
        double FullThermofluidNetAccumulatedMassKilogramsPerSecond,
        double GeneratorRequestedMegawatts,
        double GeneratorOutputMegawatts,
        double OutletPressurePascals,
        double DrumPressurePascals,
        double OutletTemperatureCelsius,
        double FuelTemperatureCelsius,
        double StructureTemperatureCelsius,
        double LevelControllerOutput)
    {
        public bool AllFinite => new[]
        {
            SimulatedSeconds,
            OutletMassKilograms,
            DrumMassKilograms,
            DrumLevelFraction,
            PumpFlowKilogramsPerSecond,
            ChannelFlowKilogramsPerSecond,
            ReturnFlowKilogramsPerSecond,
            ChannelReturnResidualKilogramsPerSecond,
            DrumIncomingReturnKilogramsPerSecond,
            DrumSeparatedSteamKilogramsPerSecond,
            DrumRecirculationKilogramsPerSecond,
            DrumRequestedRecirculationKilogramsPerSecond,
            FeedwaterBoundaryKilogramsPerSecond,
            SteamExportBoundaryKilogramsPerSecond,
            PrimaryBoundaryMassResidualKilogramsPerSecond,
            PrimaryBoundaryNetExternalMassKilogramsPerSecond,
            DrumAlgebraicNetMassRateKilogramsPerSecond,
            FeedwaterPumpKilogramsPerSecond,
            CondensatePumpKilogramsPerSecond,
            CondenserCondensationKilogramsPerSecond,
            PrimaryTotalMassKilograms,
            PrimaryTotalStoredEnergyJoules,
            PrimaryNetAccumulatedMassRateKilogramsPerSecond,
            PrimaryExpectedExternalMassRateKilogramsPerSecond,
            PrimaryNetAccumulatedEnergyRateMegawatts,
            PrimaryExpectedExternalPowerMegawatts,
            FissionPowerMegawatts,
            DecayHeatPowerMegawatts,
            NuclearHeatPowerMegawatts,
            PrimaryBoundaryNetExternalPowerMegawatts,
            PumpHydraulicPowerMegawatts,
            FeedwaterConditioningPowerMegawatts,
            CondenserHeatRejectionMegawatts,
            TurbineShaftPowerMegawatts,
            ElectricalExportMegawatts,
            GeneratorConversionLossMegawatts,
            PassiveRotorLossMegawatts,
            NetReactorToGridExternalPowerMegawatts,
            CoupledStoredEnergyChangeRateMegawatts,
            FullEnergyPathClosureResidualJoules,
            FullThermofluidExpectedExternalMassKilogramsPerSecond,
            FullThermofluidNetAccumulatedMassKilogramsPerSecond,
            GeneratorRequestedMegawatts,
            GeneratorOutputMegawatts,
            OutletPressurePascals,
            DrumPressurePascals,
            OutletTemperatureCelsius,
            FuelTemperatureCelsius,
            StructureTemperatureCelsius,
            LevelControllerOutput,
        }.All(double.IsFinite);
    }
}
