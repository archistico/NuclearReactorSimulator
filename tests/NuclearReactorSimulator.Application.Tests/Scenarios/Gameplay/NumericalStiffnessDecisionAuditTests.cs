using System.Diagnostics;
using System.Text;
using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios.Training;
using NuclearReactorSimulator.Domain.Physics.Fluids;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay;

/// <summary>
/// M10.9.4.1-H.1 evidence-only fixed-step refinement audit. It never adapts the timestep at runtime and does not modify
/// physical coefficients, controller tuning, protection thresholds or canonical state ownership.
/// </summary>
public sealed class NumericalStiffnessDecisionAuditTests
{
    private static readonly TimeSpan CoarseStep = TimeSpan.FromMilliseconds(10d);
    private static readonly TimeSpan MediumStep = TimeSpan.FromMilliseconds(5d);
    private static readonly TimeSpan FineStep = TimeSpan.FromMilliseconds(2.5d);
    private const double SimulatedSeconds = 5d;

    [Fact(Explicit = true)]
    [Trait("Category", "NumericalStiffnessDecisionAudit")]
    public void CurrentV2FixedStepRefinement_RecordsConvergenceHydraulicStiffnessAndBoundedCostEvidence()
    {
        var runs = new[]
        {
            Run(CoarseStep),
            Run(MediumStep),
            Run(FineStep),
        };

        Assert.Equal(new[] { 10d, 5d, 2.5d }, runs.Select(static run => run.StepMilliseconds).ToArray());
        Assert.All(runs, static run =>
        {
            Assert.True(run.StepCount > 0);
            Assert.True(double.IsFinite(run.WallSecondsPerSimulatedSecond));
            Assert.True(run.WallSecondsPerSimulatedSecond >= 0d);
            Assert.True(double.IsFinite(run.MicrosecondsPerStep));
            Assert.True(run.MicrosecondsPerStep >= 0d);
            Assert.True(run.MaximumMassClosureResidualKilograms <= 1e-6d, run.ToString());
            Assert.True(run.MaximumEnergyClosureResidualJoules <= 1e-2d, run.ToString());
            Assert.True(run.MaximumBalanceMassRateResidualKilogramsPerSecond <= 1e-8d, run.ToString());
            Assert.True(run.MaximumBalancePowerResidualWatts <= 1e-3d, run.ToString());
            Assert.True(double.IsFinite(run.MaximumFractionalNodeMassChangePerStep));
            Assert.True(double.IsFinite(run.MaximumFractionalNodeEnergyChangePerStep));
            Assert.True(double.IsFinite(run.MaximumFractionalSubcooledLiquidPressureChangePerStep));
            Assert.True(double.IsFinite(run.MaximumPumpFlowStepChangeKilogramsPerSecond));
            Assert.True(double.IsFinite(run.MaximumChannelFlowStepChangeKilogramsPerSecond));
            Assert.True(double.IsFinite(run.MaximumReturnFlowStepChangeKilogramsPerSecond));
            Assert.InRange(run.FinalRotorSpeedRpm, 2_900d, 3_100d);
            Assert.InRange(run.FinalGeneratorFrequencyHertz, 48d, 52d);
        });

        var comparisons = BuildConvergenceComparisons(runs[0], runs[1], runs[2]);
        Assert.All(comparisons, static comparison =>
        {
            Assert.True(double.IsFinite(comparison.CoarseValue));
            Assert.True(double.IsFinite(comparison.MediumValue));
            Assert.True(double.IsFinite(comparison.FineValue));
            Assert.True(double.IsFinite(comparison.CoarseToMediumRelativeDifference));
            Assert.True(double.IsFinite(comparison.MediumToFineRelativeDifference));
        });

        WriteAuditReports(runs, comparisons);
    }

    private static RunMetrics Run(TimeSpan step)
    {
        var engine = Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(
            DesktopSustainedGenerationInitialConditionFactory.CreateNumericalStiffnessEvidenceRuntimeEngine(step));
        Assert.Equal(step, engine.FixedDeltaTime);

        var exactStepCount = SimulatedSeconds / step.TotalSeconds;
        Assert.Equal(Math.Round(exactStepCount), exactStepCount, 9);
        var stepCount = checked((int)Math.Round(exactStepCount));
        var accumulator = new RunAccumulator(Capture(engine));

        var stopwatch = Stopwatch.StartNew();
        for (var index = 0; index < stepCount; index++)
        {
            var presentation = engine.Step(ControlRoomRunState.Running);
            Assert.False(presentation.AnyTripActive, $"Unexpected trip at dt={step.TotalMilliseconds:0.###} ms, step={index + 1}.");
            accumulator.Observe(Capture(engine));
        }
        stopwatch.Stop();

        var final = Capture(engine);
        return accumulator.Complete(
            step.TotalMilliseconds,
            stepCount,
            stopwatch.Elapsed.TotalSeconds,
            final);
    }

    private static PlantObservation Capture(IntegratedAutomaticOperationRuntimeEngine engine)
    {
        var fullPlant = engine.LatestCanonicalSnapshot.Control.ProtectedControl.FullPlant;
        var primary = fullPlant.IntegratedCycle.PrimaryCircuit;
        var drum = Assert.Single(primary.SteamDrums.Drums);
        var condenser = Assert.Single(fullPlant.IntegratedCycle.Condenser.Condensers);
        var rotor = Assert.Single(fullPlant.IntegratedCycle.TurbineExpansion.Rotors);
        var stage = Assert.Single(fullPlant.IntegratedCycle.TurbineExpansion.StageGroups);
        var generator = Assert.Single(fullPlant.IntegratedCycle.Generators);
        var heatBalance = fullPlant.HeatBalance;
        var thermofluid = fullPlant.IntegratedCycle.ThermofluidAudit;
        var nodes = fullPlant.CandidatePlant.FluidNodes.ToDictionary(
            static node => node.Id,
            static node => new NodeObservation(
                node.Mass.Kilograms,
                node.InternalEnergy.Joules,
                node.Pressure.Pascals,
                node.Phase),
            StringComparer.Ordinal);

        return new PlantObservation(
            nodes,
            primary.MainCirculation.TotalPumpMassFlowRate.KilogramsPerSecond,
            primary.MainCirculation.TotalChannelMassFlowRate.KilogramsPerSecond,
            primary.MainCirculation.TotalReturnMassFlowRate.KilogramsPerSecond,
            drum.Pressure.Megapascals,
            drum.LiquidLevelFraction.Fraction,
            condenser.FinalSteamSpacePressure.Kilopascals,
            rotor.FinalAngularSpeed.RevolutionsPerMinute,
            generator.FinalElectricalFrequency.Hertz,
            generator.SignedElectricalExchangePower.Megawatts,
            stage.EffectiveMassFlowRate.KilogramsPerSecond,
            Math.Abs(heatBalance.MassClosureResidualKilograms),
            Math.Abs(heatBalance.FullEnergyPathClosureResidualJoules),
            Math.Abs(thermofluid.BalanceMassRateResidualKilogramsPerSecond),
            Math.Abs(thermofluid.BalancePowerResidualWatts));
    }

    private static IReadOnlyList<ConvergenceComparison> BuildConvergenceComparisons(
        RunMetrics coarse,
        RunMetrics medium,
        RunMetrics fine)
        => new[]
        {
            Compare("drum_pressure_mpa", coarse.FinalDrumPressureMegapascals, medium.FinalDrumPressureMegapascals, fine.FinalDrumPressureMegapascals),
            Compare("drum_level_fraction", coarse.FinalDrumLevelFraction, medium.FinalDrumLevelFraction, fine.FinalDrumLevelFraction),
            Compare("condenser_pressure_kpa", coarse.FinalCondenserPressureKilopascals, medium.FinalCondenserPressureKilopascals, fine.FinalCondenserPressureKilopascals),
            Compare("rotor_speed_rpm", coarse.FinalRotorSpeedRpm, medium.FinalRotorSpeedRpm, fine.FinalRotorSpeedRpm),
            Compare("generator_frequency_hz", coarse.FinalGeneratorFrequencyHertz, medium.FinalGeneratorFrequencyHertz, fine.FinalGeneratorFrequencyHertz),
            Compare("stage_flow_kg_per_s", coarse.FinalStageFlowKilogramsPerSecond, medium.FinalStageFlowKilogramsPerSecond, fine.FinalStageFlowKilogramsPerSecond),
            Compare("primary_pump_flow_kg_per_s", coarse.FinalPumpFlowKilogramsPerSecond, medium.FinalPumpFlowKilogramsPerSecond, fine.FinalPumpFlowKilogramsPerSecond),
            Compare("primary_channel_flow_kg_per_s", coarse.FinalChannelFlowKilogramsPerSecond, medium.FinalChannelFlowKilogramsPerSecond, fine.FinalChannelFlowKilogramsPerSecond),
            Compare("primary_return_flow_kg_per_s", coarse.FinalReturnFlowKilogramsPerSecond, medium.FinalReturnFlowKilogramsPerSecond, fine.FinalReturnFlowKilogramsPerSecond),
        };

    private static ConvergenceComparison Compare(string metric, double coarse, double medium, double fine)
    {
        var coarseMediumAbsolute = Math.Abs(coarse - medium);
        var mediumFineAbsolute = Math.Abs(medium - fine);
        var coarseMediumRelative = RelativeDifference(coarse, medium);
        var mediumFineRelative = RelativeDifference(medium, fine);
        var observedOrder = coarseMediumAbsolute > 1e-15d && mediumFineAbsolute > 1e-15d
            ? Math.Log(coarseMediumAbsolute / mediumFineAbsolute, 2d)
            : double.NaN;

        return new ConvergenceComparison(
            metric,
            coarse,
            medium,
            fine,
            coarseMediumAbsolute,
            mediumFineAbsolute,
            coarseMediumRelative,
            mediumFineRelative,
            observedOrder);
    }

    private static double RelativeDifference(double left, double right)
        => Math.Abs(left - right) / Math.Max(Math.Max(Math.Abs(left), Math.Abs(right)), 1e-12d);

    private static void WriteAuditReports(
        IReadOnlyList<RunMetrics> runs,
        IReadOnlyList<ConvergenceComparison> comparisons)
    {
        var directory = Path.Combine(FindRepositoryRoot(), "artifacts", "h1-numerical-stiffness");
        Directory.CreateDirectory(directory);

        const string runStem = "01-current-v2-fixed-step-stiffness-sweep";
        var runCsv = new StringBuilder();
        runCsv.AppendLine("step_ms,step_count,wall_seconds,wall_seconds_per_simulated_second,microseconds_per_step,final_drum_pressure_mpa,final_drum_level_fraction,final_condenser_pressure_kpa,final_rotor_speed_rpm,final_generator_frequency_hz,final_grid_exchange_mw,final_stage_flow_kg_per_s,final_pump_flow_kg_per_s,final_channel_flow_kg_per_s,final_return_flow_kg_per_s,min_pump_flow_kg_per_s,max_pump_flow_kg_per_s,min_channel_flow_kg_per_s,max_channel_flow_kg_per_s,min_return_flow_kg_per_s,max_return_flow_kg_per_s,max_pump_flow_step_change_kg_per_s,max_channel_flow_step_change_kg_per_s,max_return_flow_step_change_kg_per_s,max_fractional_mass_change_per_step,max_fractional_energy_change_per_step,max_fractional_liquid_pressure_change_per_step,max_fractional_mass_node,max_fractional_energy_node,max_fractional_liquid_pressure_node,max_mass_closure_residual_kg,max_energy_closure_residual_j,max_balance_mass_rate_residual_kg_per_s,max_balance_power_residual_w");
        foreach (var run in runs)
        {
            runCsv.AppendLine(FormattableString.Invariant(
                $"{run.StepMilliseconds:0.000000},{run.StepCount},{run.WallSeconds:0.000000},{run.WallSecondsPerSimulatedSecond:0.000000},{run.MicrosecondsPerStep:0.000000},{run.FinalDrumPressureMegapascals:0.000000000},{run.FinalDrumLevelFraction:0.000000000},{run.FinalCondenserPressureKilopascals:0.000000000},{run.FinalRotorSpeedRpm:0.000000000},{run.FinalGeneratorFrequencyHertz:0.000000000},{run.FinalGridExchangeMegawatts:0.000000000},{run.FinalStageFlowKilogramsPerSecond:0.000000000},{run.FinalPumpFlowKilogramsPerSecond:0.000000000},{run.FinalChannelFlowKilogramsPerSecond:0.000000000},{run.FinalReturnFlowKilogramsPerSecond:0.000000000},{run.MinimumPumpFlowKilogramsPerSecond:0.000000000},{run.MaximumPumpFlowKilogramsPerSecond:0.000000000},{run.MinimumChannelFlowKilogramsPerSecond:0.000000000},{run.MaximumChannelFlowKilogramsPerSecond:0.000000000},{run.MinimumReturnFlowKilogramsPerSecond:0.000000000},{run.MaximumReturnFlowKilogramsPerSecond:0.000000000},{run.MaximumPumpFlowStepChangeKilogramsPerSecond:0.000000000},{run.MaximumChannelFlowStepChangeKilogramsPerSecond:0.000000000},{run.MaximumReturnFlowStepChangeKilogramsPerSecond:0.000000000},{run.MaximumFractionalNodeMassChangePerStep:0.000000000},{run.MaximumFractionalNodeEnergyChangePerStep:0.000000000},{run.MaximumFractionalSubcooledLiquidPressureChangePerStep:0.000000000},{Csv(run.MaximumFractionalNodeMassChangeId)},{Csv(run.MaximumFractionalNodeEnergyChangeId)},{Csv(run.MaximumFractionalSubcooledLiquidPressureChangeId)},{run.MaximumMassClosureResidualKilograms:0.000000000},{run.MaximumEnergyClosureResidualJoules:0.000000000},{run.MaximumBalanceMassRateResidualKilogramsPerSecond:0.000000000},{run.MaximumBalancePowerResidualWatts:0.000000000}"));
        }
        File.WriteAllText(Path.Combine(directory, $"{runStem}.csv"), runCsv.ToString(), new UTF8Encoding(false));

        const string convergenceStem = "02-current-v2-final-state-convergence";
        var convergenceCsv = new StringBuilder();
        convergenceCsv.AppendLine("metric,coarse_10ms,medium_5ms,fine_2_5ms,coarse_to_medium_absolute,medium_to_fine_absolute,coarse_to_medium_relative,medium_to_fine_relative,observed_order");
        foreach (var comparison in comparisons)
        {
            convergenceCsv.AppendLine(FormattableString.Invariant(
                $"{comparison.Metric},{comparison.CoarseValue:0.000000000},{comparison.MediumValue:0.000000000},{comparison.FineValue:0.000000000},{comparison.CoarseToMediumAbsoluteDifference:0.000000000},{comparison.MediumToFineAbsoluteDifference:0.000000000},{comparison.CoarseToMediumRelativeDifference:0.000000000},{comparison.MediumToFineRelativeDifference:0.000000000},{FormatOptional(comparison.ObservedOrder)}"));
        }
        File.WriteAllText(Path.Combine(directory, $"{convergenceStem}.csv"), convergenceCsv.ToString(), new UTF8Encoding(false));

        var maximumCoarseMediumRelative = comparisons.Max(static item => item.CoarseToMediumRelativeDifference);
        var maximumMediumFineRelative = comparisons.Max(static item => item.MediumToFineRelativeDifference);
        var refinementImproves = maximumMediumFineRelative <= maximumCoarseMediumRelative;
        var coarse = runs[0];
        var medium = runs[1];
        var fine = runs[2];
        var costRatioMediumToCoarse = SafeRatio(medium.WallSecondsPerSimulatedSecond, coarse.WallSecondsPerSimulatedSecond);
        var costRatioFineToMedium = SafeRatio(fine.WallSecondsPerSimulatedSecond, medium.WallSecondsPerSimulatedSecond);
        var summary = string.Join(Environment.NewLine,
            "=== 01-current-v2-fixed-step-stiffness-sweep ===",
            "Evidence-only fixed-step refinement of the validated current-v2 desktop point; no runtime adaptation, coefficient retuning or hidden nonlinear repair is active.",
            FormattableString.Invariant(
                $"steps=10.000/5.000/2.500 ms; simulated-window={SimulatedSeconds:0.000} s; run-steps={coarse.StepCount}/{medium.StepCount}/{fine.StepCount};"),
            FormattableString.Invariant(
                $"wall-seconds-per-simulated-second={coarse.WallSecondsPerSimulatedSecond:0.000000}/{medium.WallSecondsPerSimulatedSecond:0.000000}/{fine.WallSecondsPerSimulatedSecond:0.000000}; cost-ratios-medium/coarse={FormatOptional(costRatioMediumToCoarse)}; fine/medium={FormatOptional(costRatioFineToMedium)};"),
            FormattableString.Invariant(
                $"max-flow-step-change-pump={runs.Max(static run => run.MaximumPumpFlowStepChangeKilogramsPerSecond):0.000000000} kg/s; channel={runs.Max(static run => run.MaximumChannelFlowStepChangeKilogramsPerSecond):0.000000000} kg/s; return={runs.Max(static run => run.MaximumReturnFlowStepChangeKilogramsPerSecond):0.000000000} kg/s;"),
            FormattableString.Invariant(
                $"max-fractional-change-per-step: mass={runs.Max(static run => run.MaximumFractionalNodeMassChangePerStep):0.000000000}; energy={runs.Max(static run => run.MaximumFractionalNodeEnergyChangePerStep):0.000000000}; subcooled-liquid-pressure={runs.Max(static run => run.MaximumFractionalSubcooledLiquidPressureChangePerStep):0.000000000};"),
            "=== 02-current-v2-final-state-convergence ===",
            FormattableString.Invariant(
                $"metrics={comparisons.Count}; maximum-relative-difference-coarse/medium={maximumCoarseMediumRelative:0.000000000}; medium/fine={maximumMediumFineRelative:0.000000000}; refinement-improves={refinementImproves};"),
            "adaptive-substepping-active=False; semi-implicit-treatment-active=False; production-fixed-step=10.000 ms; decision-deferred-to-H.2=True",
            string.Empty);
        File.WriteAllText(Path.Combine(directory, $"{runStem}.summary.txt"), summary, new UTF8Encoding(false));
    }

    private static string Csv(string value)
        => "\"" + value.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";

    private static string FormatOptional(double value)
        => double.IsFinite(value) ? value.ToString("0.000000", System.Globalization.CultureInfo.InvariantCulture) : "n/a";

    private static double SafeRatio(double numerator, double denominator)
        => denominator > 0d ? numerator / denominator : double.NaN;

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "NuclearReactorSimulator.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate NuclearReactorSimulator.sln from the test base directory.");
    }

    private sealed class RunAccumulator
    {
        private PlantObservation _previous;
        private double _minimumPumpFlow;
        private double _maximumPumpFlow;
        private double _minimumChannelFlow;
        private double _maximumChannelFlow;
        private double _minimumReturnFlow;
        private double _maximumReturnFlow;
        private double _maximumPumpFlowStepChange;
        private double _maximumChannelFlowStepChange;
        private double _maximumReturnFlowStepChange;
        private double _maximumFractionalMassChange;
        private string _maximumFractionalMassChangeId = "—";
        private double _maximumFractionalEnergyChange;
        private string _maximumFractionalEnergyChangeId = "—";
        private double _maximumFractionalLiquidPressureChange;
        private string _maximumFractionalLiquidPressureChangeId = "—";
        private double _maximumMassClosureResidual;
        private double _maximumEnergyClosureResidual;
        private double _maximumBalanceMassRateResidual;
        private double _maximumBalancePowerResidual;

        public RunAccumulator(PlantObservation initial)
        {
            _previous = initial;
            _minimumPumpFlow = initial.PumpFlowKilogramsPerSecond;
            _maximumPumpFlow = initial.PumpFlowKilogramsPerSecond;
            _minimumChannelFlow = initial.ChannelFlowKilogramsPerSecond;
            _maximumChannelFlow = initial.ChannelFlowKilogramsPerSecond;
            _minimumReturnFlow = initial.ReturnFlowKilogramsPerSecond;
            _maximumReturnFlow = initial.ReturnFlowKilogramsPerSecond;
            ObserveResiduals(initial);
        }

        public void Observe(PlantObservation current)
        {
            Assert.Equal(_previous.Nodes.Count, current.Nodes.Count);
            foreach (var (id, previousNode) in _previous.Nodes)
            {
                var currentNode = current.Nodes[id];
                UpdateMaximum(
                    ref _maximumFractionalMassChange,
                    ref _maximumFractionalMassChangeId,
                    id,
                    Math.Abs(currentNode.MassKilograms - previousNode.MassKilograms)
                        / Math.Max(Math.Abs(previousNode.MassKilograms), 1e-9d));
                UpdateMaximum(
                    ref _maximumFractionalEnergyChange,
                    ref _maximumFractionalEnergyChangeId,
                    id,
                    Math.Abs(currentNode.InternalEnergyJoules - previousNode.InternalEnergyJoules)
                        / Math.Max(Math.Abs(previousNode.InternalEnergyJoules), 1d));

                if (previousNode.Phase == FluidPhase.SubcooledLiquid && currentNode.Phase == FluidPhase.SubcooledLiquid)
                {
                    UpdateMaximum(
                        ref _maximumFractionalLiquidPressureChange,
                        ref _maximumFractionalLiquidPressureChangeId,
                        id,
                        Math.Abs(currentNode.PressurePascals - previousNode.PressurePascals)
                            / Math.Max(Math.Abs(previousNode.PressurePascals), 1d));
                }
            }

            _minimumPumpFlow = Math.Min(_minimumPumpFlow, current.PumpFlowKilogramsPerSecond);
            _maximumPumpFlow = Math.Max(_maximumPumpFlow, current.PumpFlowKilogramsPerSecond);
            _minimumChannelFlow = Math.Min(_minimumChannelFlow, current.ChannelFlowKilogramsPerSecond);
            _maximumChannelFlow = Math.Max(_maximumChannelFlow, current.ChannelFlowKilogramsPerSecond);
            _minimumReturnFlow = Math.Min(_minimumReturnFlow, current.ReturnFlowKilogramsPerSecond);
            _maximumReturnFlow = Math.Max(_maximumReturnFlow, current.ReturnFlowKilogramsPerSecond);
            _maximumPumpFlowStepChange = Math.Max(
                _maximumPumpFlowStepChange,
                Math.Abs(current.PumpFlowKilogramsPerSecond - _previous.PumpFlowKilogramsPerSecond));
            _maximumChannelFlowStepChange = Math.Max(
                _maximumChannelFlowStepChange,
                Math.Abs(current.ChannelFlowKilogramsPerSecond - _previous.ChannelFlowKilogramsPerSecond));
            _maximumReturnFlowStepChange = Math.Max(
                _maximumReturnFlowStepChange,
                Math.Abs(current.ReturnFlowKilogramsPerSecond - _previous.ReturnFlowKilogramsPerSecond));
            ObserveResiduals(current);
            _previous = current;
        }

        public RunMetrics Complete(double stepMilliseconds, int stepCount, double wallSeconds, PlantObservation final)
            => new(
                stepMilliseconds,
                stepCount,
                wallSeconds,
                wallSeconds / SimulatedSeconds,
                stepCount > 0 ? (wallSeconds * 1_000_000d) / stepCount : 0d,
                final.DrumPressureMegapascals,
                final.DrumLevelFraction,
                final.CondenserPressureKilopascals,
                final.RotorSpeedRpm,
                final.GeneratorFrequencyHertz,
                final.GridExchangeMegawatts,
                final.StageFlowKilogramsPerSecond,
                final.PumpFlowKilogramsPerSecond,
                final.ChannelFlowKilogramsPerSecond,
                final.ReturnFlowKilogramsPerSecond,
                _minimumPumpFlow,
                _maximumPumpFlow,
                _minimumChannelFlow,
                _maximumChannelFlow,
                _minimumReturnFlow,
                _maximumReturnFlow,
                _maximumPumpFlowStepChange,
                _maximumChannelFlowStepChange,
                _maximumReturnFlowStepChange,
                _maximumFractionalMassChange,
                _maximumFractionalMassChangeId,
                _maximumFractionalEnergyChange,
                _maximumFractionalEnergyChangeId,
                _maximumFractionalLiquidPressureChange,
                _maximumFractionalLiquidPressureChangeId,
                _maximumMassClosureResidual,
                _maximumEnergyClosureResidual,
                _maximumBalanceMassRateResidual,
                _maximumBalancePowerResidual);

        private void ObserveResiduals(PlantObservation observation)
        {
            _maximumMassClosureResidual = Math.Max(_maximumMassClosureResidual, observation.MassClosureResidualKilograms);
            _maximumEnergyClosureResidual = Math.Max(_maximumEnergyClosureResidual, observation.EnergyClosureResidualJoules);
            _maximumBalanceMassRateResidual = Math.Max(_maximumBalanceMassRateResidual, observation.BalanceMassRateResidualKilogramsPerSecond);
            _maximumBalancePowerResidual = Math.Max(_maximumBalancePowerResidual, observation.BalancePowerResidualWatts);
        }

        private static void UpdateMaximum(ref double maximum, ref string ownerId, string candidateId, double candidate)
        {
            Assert.True(double.IsFinite(candidate), $"Non-finite stiffness metric at node '{candidateId}'.");
            if (candidate <= maximum)
            {
                return;
            }

            maximum = candidate;
            ownerId = candidateId;
        }
    }

    private sealed record NodeObservation(
        double MassKilograms,
        double InternalEnergyJoules,
        double PressurePascals,
        FluidPhase Phase);

    private sealed record PlantObservation(
        IReadOnlyDictionary<string, NodeObservation> Nodes,
        double PumpFlowKilogramsPerSecond,
        double ChannelFlowKilogramsPerSecond,
        double ReturnFlowKilogramsPerSecond,
        double DrumPressureMegapascals,
        double DrumLevelFraction,
        double CondenserPressureKilopascals,
        double RotorSpeedRpm,
        double GeneratorFrequencyHertz,
        double GridExchangeMegawatts,
        double StageFlowKilogramsPerSecond,
        double MassClosureResidualKilograms,
        double EnergyClosureResidualJoules,
        double BalanceMassRateResidualKilogramsPerSecond,
        double BalancePowerResidualWatts);

    private sealed record RunMetrics(
        double StepMilliseconds,
        int StepCount,
        double WallSeconds,
        double WallSecondsPerSimulatedSecond,
        double MicrosecondsPerStep,
        double FinalDrumPressureMegapascals,
        double FinalDrumLevelFraction,
        double FinalCondenserPressureKilopascals,
        double FinalRotorSpeedRpm,
        double FinalGeneratorFrequencyHertz,
        double FinalGridExchangeMegawatts,
        double FinalStageFlowKilogramsPerSecond,
        double FinalPumpFlowKilogramsPerSecond,
        double FinalChannelFlowKilogramsPerSecond,
        double FinalReturnFlowKilogramsPerSecond,
        double MinimumPumpFlowKilogramsPerSecond,
        double MaximumPumpFlowKilogramsPerSecond,
        double MinimumChannelFlowKilogramsPerSecond,
        double MaximumChannelFlowKilogramsPerSecond,
        double MinimumReturnFlowKilogramsPerSecond,
        double MaximumReturnFlowKilogramsPerSecond,
        double MaximumPumpFlowStepChangeKilogramsPerSecond,
        double MaximumChannelFlowStepChangeKilogramsPerSecond,
        double MaximumReturnFlowStepChangeKilogramsPerSecond,
        double MaximumFractionalNodeMassChangePerStep,
        string MaximumFractionalNodeMassChangeId,
        double MaximumFractionalNodeEnergyChangePerStep,
        string MaximumFractionalNodeEnergyChangeId,
        double MaximumFractionalSubcooledLiquidPressureChangePerStep,
        string MaximumFractionalSubcooledLiquidPressureChangeId,
        double MaximumMassClosureResidualKilograms,
        double MaximumEnergyClosureResidualJoules,
        double MaximumBalanceMassRateResidualKilogramsPerSecond,
        double MaximumBalancePowerResidualWatts);

    private sealed record ConvergenceComparison(
        string Metric,
        double CoarseValue,
        double MediumValue,
        double FineValue,
        double CoarseToMediumAbsoluteDifference,
        double MediumToFineAbsoluteDifference,
        double CoarseToMediumRelativeDifference,
        double MediumToFineRelativeDifference,
        double ObservedOrder);
}
