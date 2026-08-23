using System.Globalization;
using System.Text;
using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios;
using NuclearReactorSimulator.Application.Scenarios.Training;
using NuclearReactorSimulator.Domain.Physics.Fluids;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay;

/// <summary>
/// M10 final LR-H1 Diagnostic 9. Diagnostic 8 proves the versioned synchronous integral reference removes the
/// dominant breaker-closed governor windup, but exact-v7 remains materially non-stationary. This evidence-only
/// census freezes canonical mass ownership across admission -> turbine stage -> condenser -> hotwell -> feedwater
/// before any exact-v8 or turbine-admission semantic change is authored.
/// </summary>
public sealed class M10FinalLongFailureDiagnostic9Tests
{
    private const string OptInEnvironmentVariable = "NRS_M10_FINAL_LONG_DIAGNOSTIC9";
    private const int StepsPerSecond = 100;
    private const int SampleStrideSteps = 10;
    private const int TotalSteps = 18_000;
    private static readonly UTF8Encoding Utf8WithoutBom = new(encoderShouldEmitUTF8Identifier: false);

    [Fact]
    public void Diagnostic9_ReusesExactV7AndKeepsExactV4AsProductionDefault()
    {
        var v4 = new DesktopSustainedGenerationI5RepairedActivationCandidateInitialConditionFactory();
        var v7 = new DesktopSustainedGenerationGridDroopIntegralReferenceCandidateInitialConditionFactory();
        var registry = new VersionedInitialConditionRegistry(new IVersionedInitialConditionFactory[] { v4, v7 });

        Assert.Same(v4, registry.Resolve(new InitialConditionReference("integrated-operations-desktop-stable", 4)));
        Assert.Same(v7, registry.Resolve(new InitialConditionReference("integrated-operations-desktop-stable", 7)));

        var production = DesktopHydraulicProductionPolicySelector.Resolve(
            DesktopHydraulicProductionPolicySelector.AuthoritativeDefaultPolicy);
        Assert.Equal(DesktopSustainedGenerationI5RepairedActivationCandidateInitialConditionFactory.Reference, production.InitialCondition);
    }

    [Fact(Explicit = true)]
    [Trait("Category", "M10FinalLongDiagnostic9")]
    public void LR_H1_ExactV7_TurbineAdmissionAndClosedCycleMassOwnerCensus()
    {
        RequireOptIn();
        ResetDirectory(ReportDirectory());

        var engine = Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(
            new DesktopSustainedGenerationGridDroopIntegralReferenceCandidateInitialConditionFactory().CreateRuntimeEngine());
        var rows = new List<Sample>();
        var trips = 0;
        var rollbacks = 0;

        Capture(engine, 0, rows);
        for (var step = 1; step <= TotalSteps; step++)
        {
            var presentation = engine.Step(ControlRoomRunState.Running);
            if (presentation.AnyTripActive)
            {
                trips++;
            }

            var telemetry = engine.LatestCanonicalSnapshot.Control.ProtectedControl.FullPlant
                .IntegratedCycle.PrimaryCircuit.HydraulicNumerics.FourNodeBranchContinuity;
            if (telemetry?.RollbackRequired == true)
            {
                rollbacks++;
            }

            if (step % SampleStrideSteps == 0)
            {
                Capture(engine, step, rows);
            }

            if (step % 3000 == 0)
            {
                AppendProgress($"LR-H1 diagnostic9 exact-v7 turbine-admission/closed-cycle mass-owner census simulated-seconds={step / StepsPerSecond}; logical-step={step}");
            }
        }

        WriteTrajectory(rows);
        WriteSummary(rows, trips, rollbacks);

        Assert.Equal(TotalSteps, engine.LogicalStep);
        Assert.Equal(0, trips);
        Assert.Equal(0, rollbacks);
        Assert.All(rows, static item => Assert.True(item.AllFinite));
    }

    private static void Capture(IntegratedAutomaticOperationRuntimeEngine engine, int logicalStep, List<Sample> rows)
    {
        var protectedControl = engine.LatestCanonicalSnapshot.Control.ProtectedControl;
        var fullPlant = protectedControl.FullPlant;
        var cycle = fullPlant.IntegratedCycle;
        var turbine = cycle.TurbineExpansion;
        var train = Assert.Single(turbine.MainSteamNetwork.AdmissionTrains);
        var stage = Assert.Single(turbine.StageGroups);
        var condenser = Assert.Single(cycle.Condenser.Condensers);
        var condensateFeedwater = Assert.Single(cycle.CondensateFeedwater.Trains);
        var drum = Assert.Single(cycle.PrimaryCircuit.SteamDrums.Drums);
        var plant = fullPlant.CandidatePlant;
        var speed = protectedControl.TurbineSecondary.ControlAndActuator.Controllers.GetDiagnostic("speed-control");

        var inletQuality = ResolveVaporFraction(stage.InletPhase, stage.InletVaporQuality);
        var admission = train.AdmissionValve.MassFlowRate.KilogramsPerSecond;
        var commanded = stage.CommandedMassFlowRate.KilogramsPerSecond;
        var effective = stage.EffectiveMassFlowRate.KilogramsPerSecond;
        var condensation = condenser.ActualCondensationMassFlowRate.KilogramsPerSecond;
        var condensatePump = condensateFeedwater.CondensatePump.MassFlowRate.KilogramsPerSecond;
        var feedwaterPump = condensateFeedwater.FeedwaterPump.MassFlowRate.KilogramsPerSecond;
        var returnFlow = drum.IncomingReturnMassFlowRate.KilogramsPerSecond;
        var recirculation = drum.RecirculatedLiquidMassFlowRate.KilogramsPerSecond;
        var separatedSteam = drum.SeparatedSteamMassFlowRate.KilogramsPerSecond;

        rows.Add(new Sample(
            logicalStep,
            logicalStep / (double)StepsPerSecond,
            speed.IntegralTerm,
            speed.Output,
            100d * train.ControlValve.EffectivePosition.Fraction,
            admission,
            commanded,
            effective,
            inletQuality,
            admission - commanded,
            commanded - effective,
            commanded * (1d - inletQuality),
            admission - effective,
            plant.GetFluidNode("turbine-inlet").Mass.Kilograms,
            condensation,
            effective - condensation,
            plant.GetFluidNode("exhaust").Mass.Kilograms,
            condenser.ThermalLimitedCondensationMassFlowRate.KilogramsPerSecond,
            condenser.HeatRejectionPower.Megawatts,
            condensatePump,
            condensation - condensatePump,
            plant.GetFluidNode("hotwell").Mass.Kilograms,
            feedwaterPump,
            condensatePump - feedwaterPump,
            plant.GetFluidNode("feedwater-inventory").Mass.Kilograms,
            returnFlow,
            recirculation,
            separatedSteam,
            returnFlow + feedwaterPump - separatedSteam - recirculation,
            plant.GetFluidNode("drum").Mass.Kilograms,
            plant.GetFluidNode("turbine-inlet").Pressure.Pascals,
            plant.GetFluidNode("turbine-inlet").Temperature.DegreesCelsius,
            plant.GetFluidNode("exhaust").Pressure.Pascals,
            plant.GetFluidNode("exhaust").Temperature.DegreesCelsius,
            stage.ShaftPower.Megawatts,
            fullPlant.HeatBalance.ElectricalExportPower.Megawatts));
    }

    private static double ResolveVaporFraction(FluidPhase phase, VaporQuality? quality)
        => phase switch
        {
            FluidPhase.SuperheatedVapor => 1d,
            FluidPhase.SaturatedMixture => quality?.Fraction ?? 0d,
            _ => 0d,
        };

    private static void WriteTrajectory(IReadOnlyList<Sample> rows)
    {
        var lines = new List<string>
        {
            "logical_step,simulated_seconds,governor_integral,governor_output_percent,control_valve_position_percent,admission_valve_kg_s,stage_commanded_kg_s,stage_effective_kg_s,turbine_inlet_vapor_fraction,admission_minus_commanded_kg_s,commanded_minus_effective_kg_s,commanded_times_one_minus_vapor_fraction_kg_s,admission_minus_effective_kg_s,turbine_inlet_mass_kg,condensation_kg_s,effective_minus_condensation_kg_s,exhaust_mass_kg,thermal_limited_condensation_kg_s,condenser_heat_rejection_mw,condensate_pump_kg_s,condensation_minus_condensate_kg_s,hotwell_mass_kg,feedwater_pump_kg_s,condensate_minus_feedwater_kg_s,feedwater_inventory_mass_kg,drum_return_kg_s,drum_recirculation_kg_s,separated_steam_kg_s,corrected_drum_net_kg_s,drum_mass_kg,turbine_inlet_pressure_pa,turbine_inlet_temperature_c,exhaust_pressure_pa,exhaust_temperature_c,stage_shaft_mw,electrical_export_mw"
        };

        lines.AddRange(rows.Select(static item => string.Join(",",
            item.LogicalStep,
            F(item.SimulatedSeconds),
            F(item.GovernorIntegral), F(item.GovernorOutputPercent), F(item.ControlValvePositionPercent),
            F(item.AdmissionValveKilogramsPerSecond), F(item.StageCommandedKilogramsPerSecond), F(item.StageEffectiveKilogramsPerSecond),
            F(item.TurbineInletVaporFraction), F(item.AdmissionMinusCommandedKilogramsPerSecond), F(item.CommandedMinusEffectiveKilogramsPerSecond),
            F(item.CommandedTimesOneMinusVaporFractionKilogramsPerSecond), F(item.AdmissionMinusEffectiveKilogramsPerSecond), F(item.TurbineInletMassKilograms),
            F(item.CondensationKilogramsPerSecond), F(item.EffectiveMinusCondensationKilogramsPerSecond), F(item.ExhaustMassKilograms),
            F(item.ThermalLimitedCondensationKilogramsPerSecond), F(item.CondenserHeatRejectionMegawatts),
            F(item.CondensatePumpKilogramsPerSecond), F(item.CondensationMinusCondensateKilogramsPerSecond), F(item.HotwellMassKilograms),
            F(item.FeedwaterPumpKilogramsPerSecond), F(item.CondensateMinusFeedwaterKilogramsPerSecond), F(item.FeedwaterInventoryMassKilograms),
            F(item.DrumReturnKilogramsPerSecond), F(item.DrumRecirculationKilogramsPerSecond), F(item.SeparatedSteamKilogramsPerSecond),
            F(item.CorrectedDrumNetKilogramsPerSecond), F(item.DrumMassKilograms),
            F(item.TurbineInletPressurePascals), F(item.TurbineInletTemperatureCelsius), F(item.ExhaustPressurePascals), F(item.ExhaustTemperatureCelsius),
            F(item.StageShaftMegawatts), F(item.ElectricalExportMegawatts))));

        File.WriteAllLines(Path.Combine(ReportDirectory(), "100-v7-turbine-admission-mass-owner-trajectory.csv"), lines, Utf8WithoutBom);
    }

    private static void WriteSummary(IReadOnlyList<Sample> rows, int trips, int rollbacks)
    {
        var initial = rows[0];
        var ten = Closest(rows, 10d);
        var sixty = Closest(rows, 60d);
        var final = rows[^1];
        var late = rows.Where(static item => item.SimulatedSeconds >= 120d).ToArray();

        var inletMeasured = Slope(late, static x => x.TurbineInletMassKilograms);
        var inletAlgebraic = late.Average(static x => x.AdmissionMinusEffectiveKilogramsPerSecond);
        var exhaustMeasured = Slope(late, static x => x.ExhaustMassKilograms);
        var exhaustAlgebraic = late.Average(static x => x.EffectiveMinusCondensationKilogramsPerSecond);
        var hotwellMeasured = Slope(late, static x => x.HotwellMassKilograms);
        var hotwellAlgebraic = late.Average(static x => x.CondensationMinusCondensateKilogramsPerSecond);
        var feedwaterMeasured = Slope(late, static x => x.FeedwaterInventoryMassKilograms);
        var feedwaterAlgebraic = late.Average(static x => x.CondensateMinusFeedwaterKilogramsPerSecond);
        var drumMeasured = Slope(late, static x => x.DrumMassKilograms);
        var drumAlgebraic = late.Average(static x => x.CorrectedDrumNetKilogramsPerSecond);

        File.WriteAllLines(Path.Combine(ReportDirectory(), "101-v7-turbine-admission-mass-owner-summary.txt"), new[]
        {
            "scope=Diagnostic 9 exact-v7 turbine-admission + closed-secondary-cycle mass-owner census; Diagnostic 8 execution PASS but exact-v7 engineering NOT QUALIFIED; exact-v4 production selector unchanged; no exact-v8 or production activation is included;",
            "diagnostic8-returned-evidence=governor late output/control-valve drift reduced to about +0.000240 %/s, but primary flow rises 100->122.7 kg/s, electrical export falls to 4.564 MW and late stored-energy rate remains about +2.40 MW; exact-v7 is therefore not an equilibrium operating point;",
            "code-owner-observation=current VaporMassFractionLimited turbine expansion transfers stage commanded mass multiplied by inlet vapor fraction; any commanded-minus-effective mass remains in turbine-inlet unless another owner removes it; condenser removes mass from exhaust independently through its condensation law;",
            FormatPoint("initial", initial),
            FormatPoint("10s", ten),
            FormatPoint("60s", sixty),
            FormatPoint("final", final),
            FormattableString.Invariant($"late-120-180-stage-means=admission:{late.Average(static x => x.AdmissionValveKilogramsPerSecond):G17}|commanded:{late.Average(static x => x.StageCommandedKilogramsPerSecond):G17}|effective:{late.Average(static x => x.StageEffectiveKilogramsPerSecond):G17}|vapor-fraction:{late.Average(static x => x.TurbineInletVaporFraction):G17}|admission-minus-commanded:{late.Average(static x => x.AdmissionMinusCommandedKilogramsPerSecond):G17}|commanded-minus-effective:{late.Average(static x => x.CommandedMinusEffectiveKilogramsPerSecond):G17}|commanded*(1-x):{late.Average(static x => x.CommandedTimesOneMinusVaporFractionKilogramsPerSecond):G17}|admission-minus-effective:{inletAlgebraic:G17} kg/s;"),
            FormattableString.Invariant($"late-120-180-turbine-inlet-closure=measured-dm-dt:{inletMeasured:G17}|algebraic-admission-minus-effective:{inletAlgebraic:G17}|difference:{(inletMeasured - inletAlgebraic):G17} kg/s;"),
            FormattableString.Invariant($"late-120-180-exhaust-closure=measured-dm-dt:{exhaustMeasured:G17}|algebraic-effective-minus-condensation:{exhaustAlgebraic:G17}|difference:{(exhaustMeasured - exhaustAlgebraic):G17} kg/s;"),
            FormattableString.Invariant($"late-120-180-hotwell-closure=measured-dm-dt:{hotwellMeasured:G17}|algebraic-condensation-minus-condensate:{hotwellAlgebraic:G17}|difference:{(hotwellMeasured - hotwellAlgebraic):G17} kg/s;"),
            FormattableString.Invariant($"late-120-180-feedwater-inventory-closure=measured-dm-dt:{feedwaterMeasured:G17}|algebraic-condensate-minus-feedwater:{feedwaterAlgebraic:G17}|difference:{(feedwaterMeasured - feedwaterAlgebraic):G17} kg/s;"),
            FormattableString.Invariant($"late-120-180-drum-closure=measured-dm-dt:{drumMeasured:G17}|corrected-algebraic:{drumAlgebraic:G17}|difference:{(drumMeasured - drumAlgebraic):G17} kg/s;"),
            FormattableString.Invariant($"late-120-180-energy-observables=stage-shaft-mw:{late.Average(static x => x.StageShaftMegawatts):G17}|condenser-rejection-mw:{late.Average(static x => x.CondenserHeatRejectionMegawatts):G17}|electrical-export-mw:{late.Average(static x => x.ElectricalExportMegawatts):G17};"),
            FormattableString.Invariant($"late-120-180-governor-slopes=integral-per-s:{Slope(late, static x => x.GovernorIntegral):G17}|output-percent-per-s:{Slope(late, static x => x.GovernorOutputPercent):G17}|control-valve-percent-per-s:{Slope(late, static x => x.ControlValvePositionPercent):G17};"),
            FormattableString.Invariant($"trip-steps={trips}; rollbacks={rollbacks};"),
            "decision-rule=if admission-minus-effective matches turbine-inlet dm/dt and commanded-minus-effective matches commanded*(1-vapor-fraction), classify vapor-fraction-limited stage mass ownership as a structural contributor to exact-v7 steam-path drift; do not author a seed-only exact-v8. Then separately decide whether the physically intended repair is total-mass turbine transport with vapor-limited work, an explicit moisture drain/separator owner, or another already-modeled path. If the closures do not match, continue owner diagnosis without changing turbine semantics;",
        }, Utf8WithoutBom);
    }

    private static string FormatPoint(string label, Sample item)
        => FormattableString.Invariant(
            $"{label}=t:{item.SimulatedSeconds:G17}s|admission/commanded/effective:{item.AdmissionValveKilogramsPerSecond:G17}/{item.StageCommandedKilogramsPerSecond:G17}/{item.StageEffectiveKilogramsPerSecond:G17}kg/s|x:{item.TurbineInletVaporFraction:G17}|inlet-net:{item.AdmissionMinusEffectiveKilogramsPerSecond:G17}|inlet-mass:{item.TurbineInletMassKilograms:G17}kg|condensation:{item.CondensationKilogramsPerSecond:G17}|exhaust-net:{item.EffectiveMinusCondensationKilogramsPerSecond:G17}|exhaust-mass:{item.ExhaustMassKilograms:G17}kg|stage-shaft:{item.StageShaftMegawatts:G17}MW|electrical:{item.ElectricalExportMegawatts:G17}MW;");

    private static Sample Closest(IReadOnlyList<Sample> rows, double seconds)
        => rows.OrderBy(item => Math.Abs(item.SimulatedSeconds - seconds)).First();

    private static double Slope(IReadOnlyList<Sample> rows, Func<Sample, double> selector)
    {
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
            throw new InvalidOperationException($"Set {OptInEnvironmentVariable}=1 to run M10 final long Diagnostic 9.");
        }
    }

    private static string ReportDirectory()
        => Path.Combine(FindRepositoryRoot(), "artifacts", "m10-final-long-diagnostic9");

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
        File.WriteAllText(Path.Combine(directory, "00-progress.txt"), $"M10 FINAL LONG FAILURE DIAGNOSTIC 9 STARTED{Environment.NewLine}", Utf8WithoutBom);
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
        double GovernorIntegral,
        double GovernorOutputPercent,
        double ControlValvePositionPercent,
        double AdmissionValveKilogramsPerSecond,
        double StageCommandedKilogramsPerSecond,
        double StageEffectiveKilogramsPerSecond,
        double TurbineInletVaporFraction,
        double AdmissionMinusCommandedKilogramsPerSecond,
        double CommandedMinusEffectiveKilogramsPerSecond,
        double CommandedTimesOneMinusVaporFractionKilogramsPerSecond,
        double AdmissionMinusEffectiveKilogramsPerSecond,
        double TurbineInletMassKilograms,
        double CondensationKilogramsPerSecond,
        double EffectiveMinusCondensationKilogramsPerSecond,
        double ExhaustMassKilograms,
        double ThermalLimitedCondensationKilogramsPerSecond,
        double CondenserHeatRejectionMegawatts,
        double CondensatePumpKilogramsPerSecond,
        double CondensationMinusCondensateKilogramsPerSecond,
        double HotwellMassKilograms,
        double FeedwaterPumpKilogramsPerSecond,
        double CondensateMinusFeedwaterKilogramsPerSecond,
        double FeedwaterInventoryMassKilograms,
        double DrumReturnKilogramsPerSecond,
        double DrumRecirculationKilogramsPerSecond,
        double SeparatedSteamKilogramsPerSecond,
        double CorrectedDrumNetKilogramsPerSecond,
        double DrumMassKilograms,
        double TurbineInletPressurePascals,
        double TurbineInletTemperatureCelsius,
        double ExhaustPressurePascals,
        double ExhaustTemperatureCelsius,
        double StageShaftMegawatts,
        double ElectricalExportMegawatts)
    {
        public bool AllFinite => GetType().GetProperties()
            .Where(static property => property.PropertyType == typeof(double))
            .Select(property => (double)property.GetValue(this)!)
            .All(double.IsFinite);
    }
}
