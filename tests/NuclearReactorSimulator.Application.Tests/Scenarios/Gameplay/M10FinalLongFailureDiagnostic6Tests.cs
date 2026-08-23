using System.Globalization;
using System.Text;
using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios;
using NuclearReactorSimulator.Application.Scenarios.Training;
using NuclearReactorSimulator.Domain.Plant;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay;

/// <summary>
/// M10 final LR-H1 Diagnostic 6. Diagnostic 5 exposed the complete whole-cycle state; source-level
/// balance review then showed that exact-v5 relied on a non-stationary suction/drum pressure separation.
/// Exact-v6 is a new analytical authored-state candidate that closes the unchanged primary, steam-path, turbine-generator,
/// condenser and condensate/feedwater equations simultaneously. Exact-v4 remains production.
/// </summary>
public sealed class M10FinalLongFailureDiagnostic6Tests
{
    private const string OptInEnvironmentVariable = "NRS_M10_FINAL_LONG_DIAGNOSTIC6";
    private const int StepsPerSecond = 100;
    private const int TotalSteps = 60_000;
    private const double DeltaTimeSeconds = 0.01d;
    private static readonly UTF8Encoding Utf8WithoutBom = new(encoderShouldEmitUTF8Identifier: false);
    private static readonly string[] NodeIds =
    {
        "suction", "pressure", "outlet", "drum", "steam", "header", "stop-out", "control-out",
        "turbine-inlet", "exhaust", "hotwell", "feedwater-inventory",
    };

    [Fact]
    public void ExactV6Candidate_IsDistinctAnalyticallyClosedAndDoesNotSwitchProductionDefault()
    {
        var v4 = new DesktopSustainedGenerationI5RepairedActivationCandidateInitialConditionFactory();
        var v5 = new DesktopSustainedGenerationReferenceOperatingPointCandidateInitialConditionFactory();
        var v6 = new DesktopSustainedGenerationWholeCycleEquilibriumCandidateInitialConditionFactory();
        var registry = new VersionedInitialConditionRegistry(new IVersionedInitialConditionFactory[] { v4, v5, v6 });

        Assert.Same(v4, registry.Resolve(new InitialConditionReference("integrated-operations-desktop-stable", 4)));
        Assert.Same(v5, registry.Resolve(new InitialConditionReference("integrated-operations-desktop-stable", 5)));
        Assert.Same(v6, registry.Resolve(new InitialConditionReference("integrated-operations-desktop-stable", 6)));
        Assert.NotEqual(v5.Descriptor.Reference, v6.Descriptor.Reference);

        var production = DesktopHydraulicProductionPolicySelector.Resolve(
            DesktopHydraulicProductionPolicySelector.I5RepairedProductionPolicy);
        Assert.Equal(DesktopSustainedGenerationI5RepairedActivationCandidateInitialConditionFactory.Reference, production.InitialCondition);

        var engine = Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(v6.CreateRuntimeEngine());
        Assert.Equal(TimeSpan.FromMilliseconds(10d), engine.FixedDeltaTime);

        var primary = engine.LatestCanonicalSnapshot.Control.ProtectedControl.FullPlant.IntegratedCycle.PrimaryCircuit;
        Assert.Equal(HydraulicNumericalCouplingMode.FourNodeBranchContinuityCorrectedCommitOptIn, primary.HydraulicNumerics.Mode);
        Assert.InRange(primary.MainCirculation.TotalPumpMassFlowRate.KilogramsPerSecond, 95d, 105d);
        Assert.InRange(primary.MainCirculation.TotalChannelMassFlowRate.KilogramsPerSecond, 95d, 105d);
        Assert.InRange(primary.MainCirculation.TotalReturnMassFlowRate.KilogramsPerSecond, 95d, 105d);

        var fullPlant = engine.LatestCanonicalSnapshot.Control.ProtectedControl.FullPlant;
        var drum = Assert.Single(primary.SteamDrums.Drums);
        var feedwater = Assert.Single(fullPlant.IntegratedCycle.CondensateFeedwater.Trains);
        Assert.InRange(drum.LiquidLevelFraction.Fraction, 0.49d, 0.51d);
        Assert.InRange(drum.SeparatedSteamMassFlowRate.KilogramsPerSecond, 12d, 14d);
        Assert.InRange(feedwater.FeedwaterPump.MassFlowRate.KilogramsPerSecond, 12d, 14d);
        Assert.InRange(fullPlant.HeatBalance.NuclearHeatInputPower.Megawatts, 32d, 33d);
        Assert.InRange(fullPlant.HeatBalance.ElectricalExportPower.Megawatts, 4.5d, 5.5d);
    }

    [Fact(Explicit = true)]
    [Trait("Category", "M10FinalLongDiagnostic6")]
    public void LR_H1_ExactV6_SixHundredSecondWholeCycleEquilibriumCensus()
    {
        RequireOptIn();
        ResetDirectory(ReportDirectory());

        var factory = new DesktopSustainedGenerationWholeCycleEquilibriumCandidateInitialConditionFactory();
        var engine = Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(factory.CreateRuntimeEngine());
        var aggregateRows = new List<AggregateSample>();
        var nodeRows = new List<NodeSample>();
        var tripSteps = 0;
        var rollbacks = 0;

        Capture(engine, 0, aggregateRows, nodeRows);
        for (var step = 1; step <= TotalSteps; step++)
        {
            var presentation = engine.Step(ControlRoomRunState.Running);
            if (presentation.AnyTripActive)
            {
                tripSteps++;
            }

            var telemetry = engine.LatestCanonicalSnapshot.Control.ProtectedControl.FullPlant
                .IntegratedCycle.PrimaryCircuit.HydraulicNumerics.FourNodeBranchContinuity;
            if (telemetry?.RollbackRequired == true)
            {
                rollbacks++;
            }

            if (step % StepsPerSecond == 0)
            {
                Capture(engine, step, aggregateRows, nodeRows);
            }

            if (step % 3000 == 0)
            {
                AppendProgress($"LR-H1 diagnostic6 exact-v6 whole-cycle census simulated-seconds={step / StepsPerSecond}; logical-step={step}");
            }
        }

        WriteAggregateTrajectory(aggregateRows);
        WriteNodeTrajectory(nodeRows);
        WriteFinalNodeSlopes(nodeRows);
        WriteOwnerSummary(aggregateRows, tripSteps, rollbacks);

        Assert.Equal(TotalSteps, engine.LogicalStep);
        Assert.Equal(0, tripSteps);
        Assert.Equal(0, rollbacks);
        Assert.All(aggregateRows, static item => Assert.True(item.AllFinite));
        Assert.All(nodeRows, static item => Assert.True(item.AllFinite));
    }

    private static void Capture(
        IntegratedAutomaticOperationRuntimeEngine engine,
        int logicalStep,
        List<AggregateSample> aggregateRows,
        List<NodeSample> nodeRows)
    {
        var protectedControl = engine.LatestCanonicalSnapshot.Control.ProtectedControl;
        var fullPlant = protectedControl.FullPlant;
        var cycle = fullPlant.IntegratedCycle;
        var primary = cycle.PrimaryCircuit;
        var plant = fullPlant.CandidatePlant;
        var drum = Assert.Single(primary.SteamDrums.Drums);
        var train = Assert.Single(cycle.CondensateFeedwater.Trains);
        var condenser = Assert.Single(cycle.Condenser.Condensers);
        var heat = cycle.HeatBalance;
        var levelController = protectedControl.TurbineSecondary.ControlAndActuator.Controllers.GetDiagnostic("level-control");
        var hotwellController = protectedControl.TurbineSecondary.ControlAndActuator.Controllers.GetDiagnostic("hotwell-control");

        var returnFlow = drum.IncomingReturnMassFlowRate.KilogramsPerSecond;
        var recirculationFlow = drum.RecirculatedLiquidMassFlowRate.KilogramsPerSecond;
        var separatedSteamFlow = drum.SeparatedSteamMassFlowRate.KilogramsPerSecond;
        var feedwaterPumpFlow = train.FeedwaterPump.MassFlowRate.KilogramsPerSecond;
        var legacyFeedwaterBoundary = primary.TotalFeedwaterMassFlowRate.KilogramsPerSecond;
        var correctedDrumNetMassRate = returnFlow + feedwaterPumpFlow - separatedSteamFlow - recirculationFlow;
        var coupledStoredPowerMegawatts = heat.CoupledStoredEnergyChange.Joules / DeltaTimeSeconds / 1_000_000d;
        var drumNode = plant.GetFluidNode("drum");
        var feedwaterInventory = plant.GetFluidNode("feedwater-inventory");
        var hotwell = plant.GetFluidNode("hotwell");

        aggregateRows.Add(new AggregateSample(
            logicalStep,
            logicalStep / (double)StepsPerSecond,
            drum.LiquidLevelFraction.Fraction,
            drumNode.Mass.Kilograms,
            returnFlow,
            recirculationFlow,
            separatedSteamFlow,
            feedwaterPumpFlow,
            legacyFeedwaterBoundary,
            correctedDrumNetMassRate,
            train.FeedwaterPump.EffectiveSpeed.Fraction,
            train.FeedwaterPump.ActivePressureBoost.Pascals,
            train.CondensatePump.MassFlowRate.KilogramsPerSecond,
            train.CondensatePump.EffectiveSpeed.Fraction,
            condenser.ActualCondensationMassFlowRate.KilogramsPerSecond,
            feedwaterInventory.Mass.Kilograms,
            feedwaterInventory.Pressure.Pascals,
            feedwaterInventory.Temperature.DegreesCelsius,
            hotwell.Mass.Kilograms,
            hotwell.Pressure.Pascals,
            hotwell.Temperature.DegreesCelsius,
            levelController.Error,
            levelController.IntegralTerm,
            levelController.Output,
            hotwellController.Error,
            hotwellController.IntegralTerm,
            hotwellController.Output,
            heat.NuclearHeatInputPower.Megawatts,
            heat.PumpHydraulicPowerExchange.Megawatts,
            heat.CondenserHeatRejectionPower.Megawatts,
            heat.ElectricalExportPower.Megawatts,
            heat.GeneratorConversionLossPower.Megawatts,
            heat.PassiveRotorMechanicalLossPower.Megawatts,
            heat.NetReactorToGridExternalPower.Megawatts,
            coupledStoredPowerMegawatts,
            heat.FullEnergyPathClosureResidualJoules,
            primary.MainCirculation.TotalPumpMassFlowRate.KilogramsPerSecond,
            primary.MainCirculation.TotalChannelMassFlowRate.KilogramsPerSecond,
            primary.MainCirculation.TotalReturnMassFlowRate.KilogramsPerSecond));

        foreach (var nodeId in NodeIds)
        {
            var node = plant.GetFluidNode(nodeId);
            nodeRows.Add(new NodeSample(
                logicalStep,
                logicalStep / (double)StepsPerSecond,
                nodeId,
                node.Mass.Kilograms,
                node.SpecificInternalEnergy.JoulesPerKilogram,
                node.Pressure.Pascals,
                node.Temperature.DegreesCelsius,
                node.Phase.ToString(),
                node.VaporQuality?.Fraction));
        }
    }

    private static void WriteAggregateTrajectory(IEnumerable<AggregateSample> rows)
    {
        var lines = new List<string>
        {
            "logical_step,simulated_seconds,drum_level_fraction,drum_mass_kg,drum_return_kg_s,drum_recirculation_kg_s,drum_separated_steam_kg_s,feedwater_pump_kg_s,legacy_feedwater_boundary_kg_s,corrected_drum_net_mass_rate_kg_s,feedwater_pump_speed_fraction,feedwater_pump_active_boost_pa,condensate_pump_kg_s,condensate_pump_speed_fraction,condenser_condensation_kg_s,feedwater_inventory_mass_kg,feedwater_inventory_pressure_pa,feedwater_inventory_temperature_c,hotwell_mass_kg,hotwell_pressure_pa,hotwell_temperature_c,level_controller_error,level_controller_integral,level_controller_output,hotwell_controller_error,hotwell_controller_integral,hotwell_controller_output,nuclear_heat_mw,pump_hydraulic_mw,condenser_rejection_mw,electrical_export_mw,generator_conversion_loss_mw,passive_rotor_loss_mw,net_external_power_mw,coupled_stored_change_rate_mw,full_energy_closure_residual_j,primary_pump_flow_kg_s,primary_channel_flow_kg_s,primary_return_flow_kg_s"
        };
        lines.AddRange(rows.Select(static item => string.Join(",",
            item.LogicalStep, F(item.SimulatedSeconds), F(item.DrumLevelFraction), F(item.DrumMassKilograms),
            F(item.DrumReturnKilogramsPerSecond), F(item.DrumRecirculationKilogramsPerSecond), F(item.DrumSeparatedSteamKilogramsPerSecond),
            F(item.FeedwaterPumpKilogramsPerSecond), F(item.LegacyFeedwaterBoundaryKilogramsPerSecond), F(item.CorrectedDrumNetMassRateKilogramsPerSecond),
            F(item.FeedwaterPumpSpeedFraction), F(item.FeedwaterPumpActiveBoostPascals), F(item.CondensatePumpKilogramsPerSecond), F(item.CondensatePumpSpeedFraction),
            F(item.CondenserCondensationKilogramsPerSecond), F(item.FeedwaterInventoryMassKilograms), F(item.FeedwaterInventoryPressurePascals),
            F(item.FeedwaterInventoryTemperatureCelsius), F(item.HotwellMassKilograms), F(item.HotwellPressurePascals), F(item.HotwellTemperatureCelsius),
            F(item.LevelControllerError), F(item.LevelControllerIntegral), F(item.LevelControllerOutput), F(item.HotwellControllerError),
            F(item.HotwellControllerIntegral), F(item.HotwellControllerOutput), F(item.NuclearHeatMegawatts), F(item.PumpHydraulicMegawatts),
            F(item.CondenserRejectionMegawatts), F(item.ElectricalExportMegawatts), F(item.GeneratorConversionLossMegawatts),
            F(item.PassiveRotorLossMegawatts), F(item.NetExternalPowerMegawatts), F(item.CoupledStoredChangeRateMegawatts),
            F(item.FullEnergyClosureResidualJoules), F(item.PrimaryPumpFlowKilogramsPerSecond), F(item.PrimaryChannelFlowKilogramsPerSecond),
            F(item.PrimaryReturnFlowKilogramsPerSecond))));
        File.WriteAllLines(Path.Combine(ReportDirectory(), "70-v6-whole-cycle-equilibrium-trajectory.csv"), lines, Utf8WithoutBom);
    }

    private static void WriteNodeTrajectory(IEnumerable<NodeSample> rows)
    {
        var lines = new List<string>
        {
            "logical_step,simulated_seconds,node_id,mass_kg,specific_internal_energy_j_kg,pressure_pa,temperature_c,phase,vapor_quality"
        };
        lines.AddRange(rows.Select(static item => string.Join(",",
            item.LogicalStep, F(item.SimulatedSeconds), item.NodeId, F(item.MassKilograms),
            F(item.SpecificInternalEnergyJoulesPerKilogram), F(item.PressurePascals), F(item.TemperatureCelsius),
            item.Phase, item.VaporQuality.HasValue ? F(item.VaporQuality.Value) : string.Empty)));
        File.WriteAllLines(Path.Combine(ReportDirectory(), "71-v6-node-state-trajectory.csv"), lines, Utf8WithoutBom);
    }

    private static void WriteFinalNodeSlopes(IReadOnlyList<NodeSample> rows)
    {
        var finalStart = rows.Max(static item => item.SimulatedSeconds) - 60d;
        var lines = new List<string>
        {
            "node_id,mass_slope_kg_s,pressure_slope_pa_s,temperature_slope_c_s,specific_u_slope_j_kg_s,final_mass_kg,final_pressure_pa,final_temperature_c,final_phase,final_quality"
        };

        foreach (var nodeId in NodeIds)
        {
            var window = rows.Where(item => item.NodeId == nodeId && item.SimulatedSeconds >= finalStart).ToArray();
            var final = window[^1];
            lines.Add(string.Join(",",
                nodeId,
                F(Slope(window, static item => item.MassKilograms)),
                F(Slope(window, static item => item.PressurePascals)),
                F(Slope(window, static item => item.TemperatureCelsius)),
                F(Slope(window, static item => item.SpecificInternalEnergyJoulesPerKilogram)),
                F(final.MassKilograms), F(final.PressurePascals), F(final.TemperatureCelsius), final.Phase,
                final.VaporQuality.HasValue ? F(final.VaporQuality.Value) : string.Empty));
        }

        File.WriteAllLines(Path.Combine(ReportDirectory(), "72-v6-final60-node-slopes.csv"), lines, Utf8WithoutBom);
    }

    private static void WriteOwnerSummary(IReadOnlyList<AggregateSample> rows, int tripSteps, int rollbacks)
    {
        var finalWindow = rows.Where(static item => item.SimulatedSeconds >= 540d).ToArray();
        var initial = rows[0];
        var final = rows[^1];
        var drumMassSlope = Slope(finalWindow, static item => item.DrumMassKilograms);
        var correctedDrumNet = finalWindow.Average(static item => item.CorrectedDrumNetMassRateKilogramsPerSecond);
        var returnMinusRecirculation = finalWindow.Average(static item => item.DrumReturnKilogramsPerSecond - item.DrumRecirculationKilogramsPerSecond);
        var feedwaterMinusSteam = finalWindow.Average(static item => item.FeedwaterPumpKilogramsPerSecond - item.DrumSeparatedSteamKilogramsPerSecond);
        var legacyBoundary = finalWindow.Average(static item => item.LegacyFeedwaterBoundaryKilogramsPerSecond);
        var meanNetExternal = finalWindow.Average(static item => item.NetExternalPowerMegawatts);
        var meanStoredChange = finalWindow.Average(static item => item.CoupledStoredChangeRateMegawatts);
        var meanClosure = finalWindow.Average(static item => Math.Abs(item.FullEnergyClosureResidualJoules));
        var feedwaterInventoryPressureSlope = Slope(finalWindow, static item => item.FeedwaterInventoryPressurePascals);
        var hotwellPressureSlope = Slope(finalWindow, static item => item.HotwellPressurePascals);

        File.WriteAllLines(Path.Combine(ReportDirectory(), "73-v6-whole-cycle-equilibrium-summary.txt"), new[]
        {
            "scope=Diagnostic 6 exact-v6 analytical whole-cycle equilibrium candidate 600 s census; Diagnostic 5 PASS; exact-v6 is candidate-only; exact-v4 production selector unchanged; replacement long unauthorized;",
            "analytical-primary=q=sqrt(1000000/(25+25+25+25))=100 kg/s; pump path resistance includes both 25 Pa*s^2/kg^2 pipe and 25 Pa*s^2/kg^2 internal resistance;",
            "analytical-secondary=5 MWe / 0.98 generator efficiency + 0.5 MW rotor loss => turbine shaft 5.602040816 MW; 430 kJ/kg effective work => 13.0280018984 kg/s;",
            "analytical-energy=whole-cycle closure requires initial fission power 32.4842538718 MW with current enthalpy paths, pump work and 5 MWe export;",
            FormattableString.Invariant($"primary-initial= pump:{initial.PrimaryPumpFlowKilogramsPerSecond:G17}|channel:{initial.PrimaryChannelFlowKilogramsPerSecond:G17}|return:{initial.PrimaryReturnFlowKilogramsPerSecond:G17} kg/s;"),
            FormattableString.Invariant($"primary-final= pump:{final.PrimaryPumpFlowKilogramsPerSecond:G17}|channel:{final.PrimaryChannelFlowKilogramsPerSecond:G17}|return:{final.PrimaryReturnFlowKilogramsPerSecond:G17} kg/s;"),
            FormattableString.Invariant($"secondary-initial=steam:{initial.DrumSeparatedSteamKilogramsPerSecond:G17}|feedwater:{initial.FeedwaterPumpKilogramsPerSecond:G17}|condensate:{initial.CondensatePumpKilogramsPerSecond:G17} kg/s|electrical:{initial.ElectricalExportMegawatts:G17} MW|nuclear:{initial.NuclearHeatMegawatts:G17} MW;"),
            FormattableString.Invariant($"drum-final=level:{final.DrumLevelFraction:G17}|mass-kg:{final.DrumMassKilograms:G17}|measured-final60-dm-dt:{drumMassSlope:G17}|corrected-algebraic-final60-net:{correctedDrumNet:G17};"),
            FormattableString.Invariant($"drum-owner-decomposition-final60=return-minus-recirculation:{returnMinusRecirculation:G17}|internal-feedwater-minus-separated-steam:{feedwaterMinusSteam:G17}|legacy-feedwater-boundary:{legacyBoundary:G17};"),
            FormattableString.Invariant($"feedwater-final=flow-kg-s:{final.FeedwaterPumpKilogramsPerSecond:G17}|speed-fraction:{final.FeedwaterPumpSpeedFraction:G17}|inventory-pressure-pa:{final.FeedwaterInventoryPressurePascals:G17}|inventory-pressure-slope-pa-s:{feedwaterInventoryPressureSlope:G17}|level-controller-output:{final.LevelControllerOutput:G17}|level-controller-integral:{final.LevelControllerIntegral:G17};"),
            FormattableString.Invariant($"hotwell-final=mass-kg:{final.HotwellMassKilograms:G17}|pressure-pa:{final.HotwellPressurePascals:G17}|pressure-slope-pa-s:{hotwellPressureSlope:G17}|controller-output:{final.HotwellControllerOutput:G17}|controller-integral:{final.HotwellControllerIntegral:G17};"),
            FormattableString.Invariant($"energy-final60=net-external-mw:{meanNetExternal:G17}|stored-change-mw:{meanStoredChange:G17}|mean-abs-closure-residual-j:{meanClosure:G17};"),
            FormattableString.Invariant($"trip-steps={tripSteps}; rollbacks={rollbacks};"),
            "decision-rule=qualify exact-v6 only after returned artifacts show bounded whole-cycle inventories, near-zero late mass/pressure/thermal drift, stable approximately 5 MWe operation and conservative energy closure; this candidate freezes no new drift tolerance and does not activate production;",
        }, Utf8WithoutBom);
    }

    private static double Slope<T>(IReadOnlyList<T> rows, Func<T, double> xSelector, Func<T, double> ySelector)
    {
        if (rows.Count < 2)
        {
            return 0d;
        }

        var meanX = rows.Average(xSelector);
        var meanY = rows.Average(ySelector);
        var numerator = 0d;
        var denominator = 0d;
        foreach (var row in rows)
        {
            var dx = xSelector(row) - meanX;
            numerator += dx * (ySelector(row) - meanY);
            denominator += dx * dx;
        }
        return denominator == 0d ? 0d : numerator / denominator;
    }

    private static double Slope(IReadOnlyList<AggregateSample> rows, Func<AggregateSample, double> selector)
        => Slope(rows, static item => item.SimulatedSeconds, selector);

    private static double Slope(IReadOnlyList<NodeSample> rows, Func<NodeSample, double> selector)
        => Slope(rows, static item => item.SimulatedSeconds, selector);

    private static string F(double value) => value.ToString("G17", CultureInfo.InvariantCulture);

    private static void RequireOptIn()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable(OptInEnvironmentVariable), "1", StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Set {OptInEnvironmentVariable}=1 to run M10 final long Diagnostic 6.");
        }
    }

    private static string ReportDirectory()
        => Path.Combine(FindRepositoryRoot(), "artifacts", "m10-final-long-diagnostic6");

    private static void AppendProgress(string message)
    {
        Directory.CreateDirectory(ReportDirectory());
        File.AppendAllText(Path.Combine(ReportDirectory(), "00-progress.txt"), $"{DateTimeOffset.UtcNow:O} {message}{Environment.NewLine}", Utf8WithoutBom);
    }

    private static void ResetDirectory(string directory)
    {
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, "00-progress.txt"), $"M10 FINAL LONG FAILURE DIAGNOSTIC 6 STARTED{Environment.NewLine}", Utf8WithoutBom);
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

    private sealed record AggregateSample(
        int LogicalStep,
        double SimulatedSeconds,
        double DrumLevelFraction,
        double DrumMassKilograms,
        double DrumReturnKilogramsPerSecond,
        double DrumRecirculationKilogramsPerSecond,
        double DrumSeparatedSteamKilogramsPerSecond,
        double FeedwaterPumpKilogramsPerSecond,
        double LegacyFeedwaterBoundaryKilogramsPerSecond,
        double CorrectedDrumNetMassRateKilogramsPerSecond,
        double FeedwaterPumpSpeedFraction,
        double FeedwaterPumpActiveBoostPascals,
        double CondensatePumpKilogramsPerSecond,
        double CondensatePumpSpeedFraction,
        double CondenserCondensationKilogramsPerSecond,
        double FeedwaterInventoryMassKilograms,
        double FeedwaterInventoryPressurePascals,
        double FeedwaterInventoryTemperatureCelsius,
        double HotwellMassKilograms,
        double HotwellPressurePascals,
        double HotwellTemperatureCelsius,
        double LevelControllerError,
        double LevelControllerIntegral,
        double LevelControllerOutput,
        double HotwellControllerError,
        double HotwellControllerIntegral,
        double HotwellControllerOutput,
        double NuclearHeatMegawatts,
        double PumpHydraulicMegawatts,
        double CondenserRejectionMegawatts,
        double ElectricalExportMegawatts,
        double GeneratorConversionLossMegawatts,
        double PassiveRotorLossMegawatts,
        double NetExternalPowerMegawatts,
        double CoupledStoredChangeRateMegawatts,
        double FullEnergyClosureResidualJoules,
        double PrimaryPumpFlowKilogramsPerSecond,
        double PrimaryChannelFlowKilogramsPerSecond,
        double PrimaryReturnFlowKilogramsPerSecond)
    {
        public bool AllFinite => GetType().GetProperties()
            .Where(static property => property.PropertyType == typeof(double))
            .Select(property => (double)property.GetValue(this)!)
            .All(double.IsFinite);
    }

    private sealed record NodeSample(
        int LogicalStep,
        double SimulatedSeconds,
        string NodeId,
        double MassKilograms,
        double SpecificInternalEnergyJoulesPerKilogram,
        double PressurePascals,
        double TemperatureCelsius,
        string Phase,
        double? VaporQuality)
    {
        public bool AllFinite => double.IsFinite(SimulatedSeconds)
            && double.IsFinite(MassKilograms)
            && double.IsFinite(SpecificInternalEnergyJoulesPerKilogram)
            && double.IsFinite(PressurePascals)
            && double.IsFinite(TemperatureCelsius)
            && (!VaporQuality.HasValue || double.IsFinite(VaporQuality.Value));
    }
}
