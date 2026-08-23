using System.Globalization;
using System.Text;
using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.ControlRoom.Automation;
using NuclearReactorSimulator.Application.Scenarios.Training;
using NuclearReactorSimulator.Domain.Physics.Control.Supervisory;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay;

/// <summary>
/// Evidence-only diagnostic after the first authorized exact-v9 replacement long. RL-M1 and RL-R1 both entered
/// protection after the same 5 -> 10 MWe generator-load raise, while healthy/degraded/protection legs, mission
/// projection scaling, replay fingerprints and archive growth were independently green. This test reproduces only
/// the first ten simulated seconds and freezes the exact protection pickup/latch owner before any workload or runtime change.
/// </summary>
public sealed class M10FinalReplacementLongFailureDiagnostic1Tests
{
    private const string OptInEnvironmentVariable = "NRS_M10_FINAL_REPLACEMENT_LONG_DIAGNOSTIC1";
    private const int StepsPerSecond = 100;
    private const int LoadRaiseStep = 500;
    private const int TotalSteps = 1_000;
    private static readonly UTF8Encoding Utf8WithoutBom = new(encoderShouldEmitUTF8Identifier: false);

    [Fact(Explicit = true)]
    [Trait("Category", "M10FinalReplacementLongFailureDiagnostic1")]
    public void RL_M1_R1_ExactV9_LoadRaiseProtectionPickupAndLatchOwnerCensus()
    {
        RequireOptIn();
        ResetDirectory(ReportDirectory());

        var engine = Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(
            new DesktopSustainedGenerationPostMoistureEquilibriumCandidateInitialConditionFactory().CreateRuntimeEngine());
        engine.RequestSupervisoryObjective(SupervisoryObjectiveRequest.HoldCurrentOperatingPoint());
        engine.RequestPlantControlAuthority(PlantControlAuthorityMode.SupervisoryAutomatic);

        var initialPresentation = engine.CreatePresentationSnapshot(ControlRoomRunState.Paused);
        var generatorId = Assert.Single(initialPresentation.Electrical.Generators).GeneratorId;
        var rows = new List<PlantSample>();
        var functionRows = new List<FunctionSample>();

        Capture(engine, rows, functionRows);
        for (var nextStep = 1; nextStep <= TotalSteps; nextStep++)
        {
            if (nextStep == LoadRaiseStep)
            {
                engine.QueueOperatorCommand(new ControlRoomCommand(
                    ControlRoomCommandKind.GeneratorLoadRaise,
                    generatorId,
                    ControlRoomCommandTargetKind.Generator));
                AppendProgress($"load-raise-dispatched-before-logical-step={nextStep}");
            }

            engine.Step(ControlRoomRunState.Running);
            Capture(engine, rows, functionRows);
        }

        WritePlantTrajectory(rows);
        WriteProtectionTrajectory(functionRows);
        WriteSummary(rows, functionRows);

        Assert.Equal(TotalSteps, engine.LogicalStep);
        Assert.All(rows, static row => Assert.True(row.AllFinite));
        var firstTrip = rows.FirstOrDefault(static row => row.AnyTripActive);
        Assert.NotNull(firstTrip);
        Assert.InRange(firstTrip!.LogicalStep, LoadRaiseStep, TotalSteps);
        Assert.Contains(functionRows, row => row.LogicalStep <= firstTrip.LogicalStep && row.IsLatched);
    }

    private static void Capture(
        IntegratedAutomaticOperationRuntimeEngine engine,
        ICollection<PlantSample> rows,
        ICollection<FunctionSample> functionRows)
    {
        var step = engine.LogicalStep;
        var protectedControl = engine.LatestCanonicalSnapshot.Control.ProtectedControl;
        var protection = protectedControl.Protection;
        var cycle = protectedControl.FullPlant.IntegratedCycle;
        var turbine = cycle.TurbineExpansion;
        var network = turbine.MainSteamNetwork;
        var train = Assert.Single(network.AdmissionTrains);
        var stage = Assert.Single(turbine.StageGroups);
        var rotor = Assert.Single(turbine.Rotors);
        var generator = Assert.Single(cycle.Generators);
        var speed = protectedControl.TurbineSecondary.ControlAndActuator.Controllers.GetDiagnostic("speed-control");
        var presentation = engine.CreatePresentationSnapshot(ControlRoomRunState.Paused);
        var presentationGenerator = Assert.Single(presentation.Electrical.Generators);

        rows.Add(new PlantSample(
            step,
            step / (double)StepsPerSecond,
            presentationGenerator.RequestedElectricalPower.NumericValue ?? double.NaN,
            presentationGenerator.ElectricalOutput.NumericValue ?? double.NaN,
            presentation.ReactorCore.ReactorThermalPower.NumericValue ?? double.NaN,
            generator.MechanicalInputPower.Megawatts,
            stage.ShaftPower.Megawatts,
            rotor.FinalAngularSpeed.RevolutionsPerMinute,
            generator.FinalElectricalFrequency.Hertz,
            generator.FinalPhaseDifference.Radians,
            speed.Setpoint,
            speed.Measurement ?? double.NaN,
            speed.Error,
            speed.IntegralTerm,
            speed.Output,
            100d * train.ControlValve.EffectivePosition.Fraction,
            generator.BreakerFinallyClosed,
            protection.ReactorScramActive,
            protection.TurbineTripActive,
            protection.GeneratorTripActive));

        foreach (var function in protection.Functions)
        {
            functionRows.Add(new FunctionSample(
                step,
                step / (double)StepsPerSecond,
                function.FunctionId,
                function.MeasurementChannelId,
                function.Measurement,
                function.SupervisionActive,
                function.TriggerActive,
                function.PickupElapsed.TotalSeconds,
                function.PickupDelay.TotalSeconds,
                function.PickupComplete,
                function.WasLatched,
                function.IsLatched,
                function.Actions.ToString()));
        }
    }

    private static void WritePlantTrajectory(IEnumerable<PlantSample> rows)
    {
        var lines = new List<string>
        {
            "logical_step,simulated_seconds,requested_electrical_mwe,electrical_output_mwe,reactor_thermal_mw,generator_mechanical_input_mw,turbine_shaft_mw,rotor_rpm,generator_frequency_hz,generator_phase_difference_rad,governor_setpoint_rpm,governor_measurement_rpm,governor_error_rpm,governor_integral,governor_output_percent,control_valve_position_percent,breaker_closed,reactor_scram,turbine_trip,generator_trip"
        };
        lines.AddRange(rows.Select(static row => string.Join(',', new[]
        {
            row.LogicalStep.ToString(CultureInfo.InvariantCulture),
            F(row.SimulatedSeconds), F(row.RequestedElectricalMegawatts), F(row.ElectricalOutputMegawatts),
            F(row.ReactorThermalMegawatts), F(row.GeneratorMechanicalInputMegawatts), F(row.TurbineShaftMegawatts),
            F(row.RotorRpm), F(row.GeneratorFrequencyHertz), F(row.GeneratorPhaseDifferenceRadians),
            F(row.GovernorSetpointRpm), F(row.GovernorMeasurementRpm), F(row.GovernorErrorRpm),
            F(row.GovernorIntegral), F(row.GovernorOutputPercent), F(row.ControlValvePositionPercent),
            row.BreakerClosed.ToString(), row.ReactorScram.ToString(), row.TurbineTrip.ToString(), row.GeneratorTrip.ToString(),
        })));
        File.WriteAllLines(Path.Combine(ReportDirectory(), "130-load-raise-protection-trajectory.csv"), lines, Utf8WithoutBom);
    }

    private static void WriteProtectionTrajectory(IEnumerable<FunctionSample> rows)
    {
        var lines = new List<string>
        {
            "logical_step,simulated_seconds,function_id,measurement_channel,measurement,supervision_active,trigger_active,pickup_elapsed_s,pickup_delay_s,pickup_complete,was_latched,is_latched,actions"
        };
        lines.AddRange(rows.Select(static row => string.Join(',', new[]
        {
            row.LogicalStep.ToString(CultureInfo.InvariantCulture), F(row.SimulatedSeconds), Csv(row.FunctionId), Csv(row.MeasurementChannelId),
            row.Measurement.HasValue ? F(row.Measurement.Value) : string.Empty,
            row.SupervisionActive.ToString(), row.TriggerActive.ToString(), F(row.PickupElapsedSeconds), F(row.PickupDelaySeconds),
            row.PickupComplete.ToString(), row.WasLatched.ToString(), row.IsLatched.ToString(), Csv(row.Actions),
        })));
        File.WriteAllLines(Path.Combine(ReportDirectory(), "131-protection-function-pickup.csv"), lines, Utf8WithoutBom);
    }

    private static void WriteSummary(IReadOnlyList<PlantSample> rows, IReadOnlyList<FunctionSample> functionRows)
    {
        var firstTrip = rows.FirstOrDefault(static row => row.AnyTripActive);
        var interestingFunctions = new[]
        {
            "generator-reverse-power",
            "generator-underfrequency",
            "generator-loss-of-synchronism",
            "generator-overfrequency",
            "turbine-overspeed",
            "condenser-high-backpressure",
            "steam-drum-low-low-level",
        };

        var lines = new List<string>
        {
            "scope=M10 Final replacement-long failure Diagnostic 1; evidence-only reproduction of the shared RL-M1/RL-R1 5->10 MWe trip owner on authoritative exact-v9; no runtime/workload/mission semantics changed; failed replacement long remains RED;",
            "returned-long-facts=RL-H1 PASS 900s; RL-D1 PASS; RL-P1 PASS; wall 35.2527 min PASS; RL-M1 projection late/early 0.969616 PASS; RL-R1 final/full-replay/checkpoint fingerprints and recording equivalence PASS; RL-M1 and RL-R1 both fail because load-raise path enters protection;",
            $"load-raise-dispatch-step={LoadRaiseStep}; total-diagnostic-steps={TotalSteps};",
            firstTrip is null
                ? "first-trip=none;"
                : FormattableString.Invariant($"first-trip=step:{firstTrip.LogicalStep}|seconds:{firstTrip.SimulatedSeconds:G17}|requested:{firstTrip.RequestedElectricalMegawatts:G17}MWe|output:{firstTrip.ElectricalOutputMegawatts:G17}MWe|reactor:{firstTrip.ReactorThermalMegawatts:G17}MWth|rotor:{firstTrip.RotorRpm:G17}rpm|frequency:{firstTrip.GeneratorFrequencyHertz:G17}Hz|reactor-scram:{firstTrip.ReactorScram}|turbine-trip:{firstTrip.TurbineTrip}|generator-trip:{firstTrip.GeneratorTrip};"),
        };

        foreach (var functionId in interestingFunctions)
        {
            var rowsForFunction = functionRows.Where(row => string.Equals(row.FunctionId, functionId, StringComparison.Ordinal)).ToArray();
            if (rowsForFunction.Length == 0)
            {
                lines.Add($"function={functionId}|not-present=True;");
                continue;
            }

            var firstTrigger = rowsForFunction.FirstOrDefault(static row => row.TriggerActive);
            var firstPickup = rowsForFunction.FirstOrDefault(static row => row.PickupElapsedSeconds > 0d);
            var firstLatch = rowsForFunction.FirstOrDefault(static row => row.IsLatched);
            lines.Add(
                $"function={functionId}" +
                $"|first-trigger={Point(firstTrigger)}" +
                $"|first-pickup={Point(firstPickup)}" +
                $"|first-latch={Point(firstLatch)}" +
                $"|pickup-delay-s={F(rowsForFunction[0].PickupDelaySeconds)}" +
                $"|actions={rowsForFunction[0].Actions};");
        }

        lines.Add("decision-rule=identify the first protection function whose trigger/pickup/latch chain closes on the exact-v9 load-raise transient. Do not retune protection thresholds, exact-v9 equilibrium, governor or turbine moisture ownership from this diagnostic. Then decide separately whether the failed long used an under-specified operator manoeuvre relative to the existing M7.6 coordinated load-change procedure or exposed a production transient defect requiring a new runtime candidate and new freeze;");
        File.WriteAllLines(Path.Combine(ReportDirectory(), "132-load-raise-protection-owner-summary.txt"), lines, Utf8WithoutBom);
    }

    private static string Point(FunctionSample? row)
        => row is null
            ? "none"
            : FormattableString.Invariant($"step:{row.LogicalStep},seconds:{row.SimulatedSeconds:G17},measurement:{(row.Measurement.HasValue ? row.Measurement.Value.ToString("G17", CultureInfo.InvariantCulture) : "null")},pickup:{row.PickupElapsedSeconds:G17},latched:{row.IsLatched}");

    private static string F(double value) => value.ToString("G17", CultureInfo.InvariantCulture);
    private static string Csv(string value) => '"' + value.Replace("\"", "\"\"", StringComparison.Ordinal) + '"';

    private static void RequireOptIn()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable(OptInEnvironmentVariable), "1", StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Set {OptInEnvironmentVariable}=1 to run M10 Final replacement-long failure Diagnostic 1.");
        }
    }

    private static string ReportDirectory()
        => Path.Combine(FindRepositoryRoot(), "artifacts", "m10-final-replacement-long-failure-diagnostic1");

    private static void AppendProgress(string message)
    {
        Directory.CreateDirectory(ReportDirectory());
        File.AppendAllText(Path.Combine(ReportDirectory(), "00-progress.txt"), message + Environment.NewLine, Utf8WithoutBom);
    }

    private static void ResetDirectory(string directory)
    {
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, "00-progress.txt"), "M10 FINAL REPLACEMENT-LONG FAILURE DIAGNOSTIC 1 STARTED" + Environment.NewLine, Utf8WithoutBom);
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

    private sealed record PlantSample(
        long LogicalStep,
        double SimulatedSeconds,
        double RequestedElectricalMegawatts,
        double ElectricalOutputMegawatts,
        double ReactorThermalMegawatts,
        double GeneratorMechanicalInputMegawatts,
        double TurbineShaftMegawatts,
        double RotorRpm,
        double GeneratorFrequencyHertz,
        double GeneratorPhaseDifferenceRadians,
        double GovernorSetpointRpm,
        double GovernorMeasurementRpm,
        double GovernorErrorRpm,
        double GovernorIntegral,
        double GovernorOutputPercent,
        double ControlValvePositionPercent,
        bool BreakerClosed,
        bool ReactorScram,
        bool TurbineTrip,
        bool GeneratorTrip)
    {
        public bool AnyTripActive => ReactorScram || TurbineTrip || GeneratorTrip;
        public bool AllFinite =>
            double.IsFinite(SimulatedSeconds)
            && double.IsFinite(RequestedElectricalMegawatts)
            && double.IsFinite(ElectricalOutputMegawatts)
            && double.IsFinite(ReactorThermalMegawatts)
            && double.IsFinite(GeneratorMechanicalInputMegawatts)
            && double.IsFinite(TurbineShaftMegawatts)
            && double.IsFinite(RotorRpm)
            && double.IsFinite(GeneratorFrequencyHertz)
            && double.IsFinite(GeneratorPhaseDifferenceRadians)
            && double.IsFinite(GovernorSetpointRpm)
            && double.IsFinite(GovernorMeasurementRpm)
            && double.IsFinite(GovernorErrorRpm)
            && double.IsFinite(GovernorIntegral)
            && double.IsFinite(GovernorOutputPercent)
            && double.IsFinite(ControlValvePositionPercent);
    }

    private sealed record FunctionSample(
        long LogicalStep,
        double SimulatedSeconds,
        string FunctionId,
        string MeasurementChannelId,
        double? Measurement,
        bool SupervisionActive,
        bool TriggerActive,
        double PickupElapsedSeconds,
        double PickupDelaySeconds,
        bool PickupComplete,
        bool WasLatched,
        bool IsLatched,
        string Actions);
}
