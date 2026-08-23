using System.Globalization;
using System.Text;
using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.ControlRoom.Automation;
using NuclearReactorSimulator.Application.Scenarios.Training;
using NuclearReactorSimulator.Domain.Physics.Control.Supervisory;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay;

/// <summary>
/// Evidence-only follow-up after Replacement-Long Failure Diagnostic 2 proved that rod coordination does not improve
/// the exact-v9 5 -> 10 MWe loss-of-synchronism path. This audit closes the remaining M7.6 turbine-governing seam,
/// discriminates automatic-governor response from physical control-valve preloading, and compares the same frozen load
/// step against historical exact-v4. It changes no production source, protection, exact-v9 state, mission pack or
/// replacement-long workload.
/// </summary>
public sealed class M10FinalReplacementLongFailureDiagnostic3Tests
{
    private const string OptInEnvironmentVariable = "NRS_M10_FINAL_REPLACEMENT_LONG_DIAGNOSTIC3";
    private const int StepsPerSecond = 100;
    private const int PrepositionStep = 400;
    private const int LoadRaiseStep = 500;
    private const int TotalSteps = 1_200;
    private static readonly UTF8Encoding Utf8WithoutBom = new(encoderShouldEmitUTF8Identifier: false);

    [Fact(Explicit = true)]
    [Trait("Category", "M10FinalReplacementLongFailureDiagnostic3")]
    public void ExactV9_TurbineGoverningValvePreloadAndHistoricalVersionDiscriminationCensus()
    {
        RequireOptIn();
        ResetDirectory(ReportDirectory());

        ProbeDefinition[] probes =
        [
            new("exact-v9-frozen-supervisory-load-only", 9, PlantControlAuthorityMode.SupervisoryAutomatic, 0, null, null),
            new("exact-v9-assisted-load-only", 9, PlantControlAuthorityMode.Assisted, 0, null, null),
            new("exact-v9-assisted-speed-raise-1x-preload", 9, PlantControlAuthorityMode.Assisted, 1, null, null),
            new("exact-v9-assisted-speed-raise-5x-preload", 9, PlantControlAuthorityMode.Assisted, 5, null, null),
            new("exact-v9-assisted-manual-valve-100-at-load", 9, PlantControlAuthorityMode.Assisted, 0, LoadRaiseStep, 100d),
            new("exact-v9-assisted-manual-valve-55-preload", 9, PlantControlAuthorityMode.Assisted, 0, PrepositionStep, 55d),
            new("exact-v9-assisted-manual-valve-65-preload", 9, PlantControlAuthorityMode.Assisted, 0, PrepositionStep, 65d),
            new("exact-v4-frozen-supervisory-load-only", 4, PlantControlAuthorityMode.SupervisoryAutomatic, 0, null, null),
        ];

        var results = new List<ProbeResult>();
        var trajectory = new List<ProbeSample>();
        foreach (var probe in probes)
        {
            AppendProgress($"probe-start={probe.Id}");
            var result = RunProbe(probe, trajectory);
            results.Add(result);
            AppendProgress(
                $"probe-complete={probe.Id}|executed={result.ExecutedSteps}|trip-step={I(result.FirstTripStep)}|first-latch={result.FirstLatchedFunctionId ?? "none"}|exception={result.ExceptionType ?? "none"}|pre-shaft={F(result.PreLoadShaftMegawatts)}|pre-valve={F(result.PreLoadControlValvePercent)}");
        }

        WriteProbeSummary(results);
        WriteTrajectory(trajectory);
        WriteDecisionSummary(results);

        var frozenV9 = results.Single(static result => result.Id == "exact-v9-frozen-supervisory-load-only");
        Assert.Null(frozenV9.ExceptionType);
        Assert.Equal((long)TotalSteps, frozenV9.ExecutedSteps);
        Assert.Equal(636L, frozenV9.FirstTripStep);
        Assert.Equal("generator-loss-of-synchronism", frozenV9.FirstLatchedFunctionId);

        var assistedV9 = results.Single(static result => result.Id == "exact-v9-assisted-load-only");
        Assert.Null(assistedV9.ExceptionType);
        Assert.Equal((long)TotalSteps, assistedV9.ExecutedSteps);
        Assert.Equal(636L, assistedV9.FirstTripStep);
        Assert.Equal("generator-loss-of-synchronism", assistedV9.FirstLatchedFunctionId);

        Assert.All(results, static result => Assert.True(result.DiagnosticComplete));
    }

    private static ProbeResult RunProbe(ProbeDefinition probe, ICollection<ProbeSample> allTrajectory)
    {
        var engine = CreateEngine(probe.ExactVersion);
        if (probe.Authority == PlantControlAuthorityMode.SupervisoryAutomatic)
        {
            engine.RequestSupervisoryObjective(SupervisoryObjectiveRequest.HoldCurrentOperatingPoint());
        }
        engine.RequestPlantControlAuthority(probe.Authority);

        var initial = engine.CreatePresentationSnapshot(ControlRoomRunState.Paused);
        var generatorId = Assert.Single(initial.Electrical.Generators).GeneratorId;
        var valveId = Assert.Single(initial.TurbineSecondary.AdmissionTrains).ControlValveId;
        var rotorId = Assert.Single(engine.CurrentState.PlantDefinition.TurbineExpansionSystem.Rotors).Id;
        var rows = new List<ProbeSample>();
        long? firstTripStep = null;
        string? firstLatchedFunctionId = null;
        long? firstLatchedFunctionStep = null;
        string? exceptionType = null;
        string? exceptionMessage = null;
        var allFinite = true;

        Capture(probe.Id, engine, rows, allTrajectory);
        for (var nextStep = 1; nextStep <= TotalSteps; nextStep++)
        {
            try
            {
                if (nextStep == PrepositionStep && probe.SpeedRaiseCount > 0)
                {
                    for (var command = 0; command < probe.SpeedRaiseCount; command++)
                    {
                        engine.QueueOperatorCommand(new ControlRoomCommand(
                            ControlRoomCommandKind.TurbineSpeedRaise,
                            rotorId,
                            ControlRoomCommandTargetKind.TurbineRotor));
                    }
                }

                if (probe.ManualValveStep == nextStep)
                {
                    engine.QueueOperatorCommand(new ControlRoomCommand(
                        ControlRoomCommandKind.TurbineControlValveManualMode,
                        valveId,
                        ControlRoomCommandTargetKind.Valve));
                    engine.QueueOperatorCommand(new ControlRoomCommand(
                        ControlRoomCommandKind.TurbineControlValveManualDemandSet,
                        valveId,
                        ControlRoomCommandTargetKind.Valve,
                        probe.ManualValveDemandPercent));
                }

                if (nextStep == LoadRaiseStep)
                {
                    engine.QueueOperatorCommand(new ControlRoomCommand(
                        ControlRoomCommandKind.GeneratorLoadRaise,
                        generatorId,
                        ControlRoomCommandTargetKind.Generator));
                }

                engine.Step(ControlRoomRunState.Running);
                var sample = Capture(probe.Id, engine, rows, allTrajectory);
                allFinite &= sample.AllFinite;
                if (sample.AnyTripActive)
                {
                    firstTripStep ??= sample.LogicalStep;
                }

                if (firstLatchedFunctionId is null)
                {
                    var protection = engine.LatestCanonicalSnapshot.Control.ProtectedControl.Protection;
                    var firstLatch = protection.Functions.FirstOrDefault(static function => function.IsLatched);
                    if (firstLatch is not null)
                    {
                        firstLatchedFunctionId = firstLatch.FunctionId;
                        firstLatchedFunctionStep = sample.LogicalStep;
                    }
                }
            }
            catch (Exception exception)
            {
                exceptionType = exception.GetType().FullName ?? exception.GetType().Name;
                exceptionMessage = exception.Message.Replace('\r', ' ').Replace('\n', ' ');
                break;
            }
        }

        var initialRow = rows[0];
        var preLoad = rows.LastOrDefault(static row => row.LogicalStep <= LoadRaiseStep - 1) ?? rows[^1];
        var atLoad = rows.FirstOrDefault(static row => row.LogicalStep >= LoadRaiseStep) ?? rows[^1];
        var lateRows = rows.Where(static row => row.LogicalStep >= TotalSteps - StepsPerSecond).ToArray();
        var stableTenMweLate = exceptionType is null
            && engine.LogicalStep == TotalSteps
            && firstTripStep is null
            && lateRows.Length == StepsPerSecond + 1
            && lateRows.All(static row => row.BreakerClosed)
            && lateRows.All(static row => row.ElectricalOutputMegawatts is >= 9.5d and <= 10.5d)
            && lateRows.All(static row => row.GeneratorFrequencyHertz is >= 49d and <= 51d);
        var survivesTwoSecondsAfterRaise = exceptionType is null
            && (firstTripStep is null || firstTripStep > LoadRaiseStep + (2 * StepsPerSecond));

        return new ProbeResult(
            probe.Id,
            probe.ExactVersion,
            probe.Authority,
            probe.SpeedRaiseCount,
            probe.ManualValveStep,
            probe.ManualValveDemandPercent,
            engine.LogicalStep,
            firstTripStep,
            firstLatchedFunctionId,
            firstLatchedFunctionStep,
            initialRow.RawSpeedSetpointRpm,
            preLoad.RawSpeedSetpointRpm,
            initialRow.GovernorSetpointRpm,
            preLoad.GovernorSetpointRpm,
            preLoad.TurbineShaftMegawatts,
            preLoad.ControlValvePositionPercent,
            atLoad.TurbineShaftMegawatts,
            atLoad.GeneratorMechanicalInputMegawatts,
            rows.Min(static row => row.GeneratorFrequencyHertz),
            rows.Max(static row => row.TurbineShaftMegawatts),
            rows.Max(static row => row.ControlValvePositionPercent),
            survivesTwoSecondsAfterRaise,
            stableTenMweLate,
            allFinite,
            exceptionType,
            exceptionMessage);
    }

    private static IntegratedAutomaticOperationRuntimeEngine CreateEngine(int exactVersion)
        => exactVersion switch
        {
            9 => Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(
                new DesktopSustainedGenerationPostMoistureEquilibriumCandidateInitialConditionFactory().CreateRuntimeEngine()),
            4 => Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(
                new DesktopSustainedGenerationI5RepairedActivationCandidateInitialConditionFactory().CreateRuntimeEngine()),
            _ => throw new ArgumentOutOfRangeException(nameof(exactVersion), exactVersion, "Unsupported diagnostic exact version."),
        };

    private static ProbeSample Capture(
        string probeId,
        IntegratedAutomaticOperationRuntimeEngine engine,
        ICollection<ProbeSample> localRows,
        ICollection<ProbeSample> allRows)
    {
        var protectedControl = engine.LatestCanonicalSnapshot.Control.ProtectedControl;
        var cycle = protectedControl.FullPlant.IntegratedCycle;
        var turbine = cycle.TurbineExpansion;
        var train = Assert.Single(turbine.MainSteamNetwork.AdmissionTrains);
        var stage = Assert.Single(turbine.StageGroups);
        var rotor = Assert.Single(turbine.Rotors);
        var generator = Assert.Single(cycle.Generators);
        var speed = protectedControl.TurbineSecondary.ControlAndActuator.Controllers.GetDiagnostic("speed-control");
        var rawSpeedInput = engine.PersistentInputs.TurbineSecondaryInputs.Controllers.GetController("speed-control");
        var presentation = engine.CreatePresentationSnapshot(ControlRoomRunState.Paused);
        var presentationGenerator = Assert.Single(presentation.Electrical.Generators);
        var step = engine.LogicalStep;

        var row = new ProbeSample(
            probeId,
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
            rawSpeedInput.Setpoint,
            speed.Setpoint,
            speed.Output,
            100d * train.ControlValve.EffectivePosition.Fraction,
            generator.BreakerFinallyClosed,
            protectedControl.Protection.ReactorScramActive,
            protectedControl.Protection.TurbineTripActive,
            protectedControl.Protection.GeneratorTripActive);
        localRows.Add(row);
        allRows.Add(row);
        return row;
    }

    private static void WriteProbeSummary(IEnumerable<ProbeResult> results)
    {
        var lines = new List<string>
        {
            "probe_id,exact_version,requested_authority,speed_raise_count,manual_valve_step,manual_valve_demand_percent,executed_steps,first_trip_step,first_latched_function,first_latched_step,initial_raw_speed_setpoint_rpm,preload_raw_speed_setpoint_rpm,initial_effective_governor_setpoint_rpm,preload_effective_governor_setpoint_rpm,preload_shaft_mw,preload_control_valve_percent,load_step_shaft_mw,load_step_generator_mechanical_mw,min_frequency_hz,max_shaft_mw,max_control_valve_percent,survives_two_seconds_after_raise,stable_ten_mwe_late,all_finite,exception_type,exception_message"
        };
        lines.AddRange(results.Select(static result => string.Join(',', new[]
        {
            Csv(result.Id), result.ExactVersion.ToString(CultureInfo.InvariantCulture), result.RequestedAuthority.ToString(),
            result.SpeedRaiseCount.ToString(CultureInfo.InvariantCulture), I(result.ManualValveStep), F(result.ManualValveDemandPercent),
            result.ExecutedSteps.ToString(CultureInfo.InvariantCulture), I(result.FirstTripStep), Csv(result.FirstLatchedFunctionId ?? string.Empty), I(result.FirstLatchedFunctionStep),
            F(result.InitialRawSpeedSetpointRpm), F(result.PreLoadRawSpeedSetpointRpm), F(result.InitialGovernorSetpointRpm), F(result.PreLoadGovernorSetpointRpm),
            F(result.PreLoadShaftMegawatts), F(result.PreLoadControlValvePercent), F(result.LoadStepShaftMegawatts), F(result.LoadStepGeneratorMechanicalMegawatts),
            F(result.MinimumFrequencyHertz), F(result.MaximumShaftMegawatts), F(result.MaximumControlValvePercent), result.SurvivesTwoSecondsAfterRaise.ToString(),
            result.StableTenMegawattsLate.ToString(), result.AllFinite.ToString(), Csv(result.ExceptionType ?? string.Empty), Csv(result.ExceptionMessage ?? string.Empty),
        })));
        File.WriteAllLines(Path.Combine(ReportDirectory(), "150-turbine-governing-preload-version-probe-summary.csv"), lines, Utf8WithoutBom);
    }

    private static void WriteTrajectory(IEnumerable<ProbeSample> rows)
    {
        var lines = new List<string>
        {
            "probe_id,logical_step,simulated_seconds,requested_electrical_mwe,electrical_output_mwe,reactor_thermal_mw,generator_mechanical_input_mw,turbine_shaft_mw,rotor_rpm,generator_frequency_hz,generator_phase_difference_rad,raw_speed_setpoint_rpm,effective_governor_setpoint_rpm,governor_output_percent,control_valve_position_percent,breaker_closed,reactor_scram,turbine_trip,generator_trip"
        };
        lines.AddRange(rows.Select(static row => string.Join(',', new[]
        {
            Csv(row.ProbeId), row.LogicalStep.ToString(CultureInfo.InvariantCulture), F(row.SimulatedSeconds), F(row.RequestedElectricalMegawatts),
            F(row.ElectricalOutputMegawatts), F(row.ReactorThermalMegawatts), F(row.GeneratorMechanicalInputMegawatts), F(row.TurbineShaftMegawatts),
            F(row.RotorRpm), F(row.GeneratorFrequencyHertz), F(row.GeneratorPhaseDifferenceRadians), F(row.RawSpeedSetpointRpm),
            F(row.GovernorSetpointRpm), F(row.GovernorOutputPercent), F(row.ControlValvePositionPercent), row.BreakerClosed.ToString(),
            row.ReactorScram.ToString(), row.TurbineTrip.ToString(), row.GeneratorTrip.ToString(),
        })));
        File.WriteAllLines(Path.Combine(ReportDirectory(), "151-turbine-governing-preload-version-trajectories.csv"), lines, Utf8WithoutBom);
    }

    private static void WriteDecisionSummary(IReadOnlyCollection<ProbeResult> results)
    {
        var frozen = results.Single(static result => result.Id == "exact-v9-frozen-supervisory-load-only");
        var assisted = results.Single(static result => result.Id == "exact-v9-assisted-load-only");
        var speed1 = results.Single(static result => result.Id == "exact-v9-assisted-speed-raise-1x-preload");
        var speed5 = results.Single(static result => result.Id == "exact-v9-assisted-speed-raise-5x-preload");
        var manualAtLoad = results.Single(static result => result.Id == "exact-v9-assisted-manual-valve-100-at-load");
        var manualPreloads = results.Where(static result => result.Id.Contains("manual-valve-") && result.ManualValveStep == PrepositionStep).ToArray();
        var historical = results.Single(static result => result.Id == "exact-v4-frozen-supervisory-load-only");
        var preloadSurvivors = manualPreloads.Where(static result => result.SurvivesTwoSecondsAfterRaise).ToArray();
        var stablePreloads = manualPreloads.Where(static result => result.StableTenMegawattsLate).ToArray();

        var lines = new List<string>
        {
            "scope=M10 Final replacement-long failure Diagnostic 3; Diagnostic 2 execution PASS returned 0/5 Assisted rod-coordination survivors and identical loss-of-synchronism latch at step 636; this candidate remains evidence-only and changes no production src, protection, exact-v9, mission @3 or frozen replacement workload; failed Execution 1 remains RED;",
            $"frozen-v9=trip:{I(frozen.FirstTripStep)}|latch:{frozen.FirstLatchedFunctionId}|pre-shaft:{F(frozen.PreLoadShaftMegawatts)}MW|load-shaft:{F(frozen.LoadStepShaftMegawatts)}MW|load-electromagnetic-mechanical:{F(frozen.LoadStepGeneratorMechanicalMegawatts)}MW|min-frequency:{F(frozen.MinimumFrequencyHertz)}Hz;",
            $"assisted-load-only=trip:{I(assisted.FirstTripStep)}|latch:{assisted.FirstLatchedFunctionId}|same-trip-as-frozen:{(assisted.FirstTripStep == frozen.FirstTripStep)};",
            $"paralleled-speed-seam-1x=raw-delta:{F(speed1.PreLoadRawSpeedSetpointRpm - speed1.InitialRawSpeedSetpointRpm)}rpm|effective-delta:{F(speed1.PreLoadGovernorSetpointRpm - speed1.InitialGovernorSetpointRpm)}rpm|trip:{I(speed1.FirstTripStep)}|latch:{speed1.FirstLatchedFunctionId};",
            $"paralleled-speed-seam-5x=raw-delta:{F(speed5.PreLoadRawSpeedSetpointRpm - speed5.InitialRawSpeedSetpointRpm)}rpm|effective-delta:{F(speed5.PreLoadGovernorSetpointRpm - speed5.InitialGovernorSetpointRpm)}rpm|trip:{I(speed5.FirstTripStep)}|latch:{speed5.FirstLatchedFunctionId};",
            $"manual-100-at-load=trip:{I(manualAtLoad.FirstTripStep)}|latch:{manualAtLoad.FirstLatchedFunctionId}|max-valve:{F(manualAtLoad.MaximumControlValvePercent)}%|max-shaft:{F(manualAtLoad.MaximumShaftMegawatts)}MW;",
            $"manual-preload-probes={manualPreloads.Length}|survive-two-seconds:{preloadSurvivors.Length}|stable-ten-mwe-late:{stablePreloads.Length}|survivors:{(preloadSurvivors.Length == 0 ? "none" : string.Join('|', preloadSurvivors.Select(static result => result.Id)))}|stable:{(stablePreloads.Length == 0 ? "none" : string.Join('|', stablePreloads.Select(static result => result.Id)))};",
            $"historical-exact-v4=executed:{historical.ExecutedSteps}|trip:{I(historical.FirstTripStep)}|latch:{historical.FirstLatchedFunctionId ?? "none"}|exception:{historical.ExceptionType ?? "none"}|pre-shaft:{F(historical.PreLoadShaftMegawatts)}MW|load-shaft:{F(historical.LoadStepShaftMegawatts)}MW|min-frequency:{F(historical.MinimumFrequencyHertz)}Hz;",
            "decision-rule=first close the M7.6 turbine seam: while breaker-closed, compare raw SPEED RAISE setpoint motion with the effective governor setpoint. If raw reference moves but effective setpoint remains load-derived and trip timing is unchanged, direct turbine-speed commands are not an effective paralleled coordination seam. Then compare manual valve preloading. If bounded preloading materially delays/avoids the trip, classify the frozen replacement operator policy as missing mechanical-power prepositioning and author a separate operator/workload-policy candidate before a new freeze; do not retune protection. If manual-at-load reproduces the automatic valve trajectory/trip and preloading also fails, use exact-v4 discrimination: a matching v4/v9 failure points to shared generator-load-order/control-granularity semantics and requires a dedicated ramp/torque-coupling diagnostic before runtime repair; a materially healthier v4 points instead to exact-v9 steam-path transient capacity and requires exact-v9-specific transient diagnosis. No outcome here directly authorizes production changes;",
            "authorization=diagnostic-only; second replacement long remains unauthorized;",
        };
        File.WriteAllLines(Path.Combine(ReportDirectory(), "152-turbine-governing-preload-version-decision-summary.txt"), lines, Utf8WithoutBom);
    }

    private static string F(double? value) => value.HasValue ? F(value.Value) : string.Empty;
    private static string F(double value) => double.IsFinite(value) ? value.ToString("G17", CultureInfo.InvariantCulture) : value.ToString(CultureInfo.InvariantCulture);
    private static string I(long? value) => value?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
    private static string I(int? value) => value?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
    private static string Csv(string value) => '"' + value.Replace("\"", "\"\"", StringComparison.Ordinal) + '"';

    private static void RequireOptIn()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable(OptInEnvironmentVariable), "1", StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Set {OptInEnvironmentVariable}=1 to run M10 Final replacement-long failure Diagnostic 3.");
        }
    }

    private static string ReportDirectory()
        => Path.Combine(FindRepositoryRoot(), "artifacts", "m10-final-replacement-long-failure-diagnostic3");

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
        File.WriteAllText(Path.Combine(directory, "00-progress.txt"), "M10 FINAL REPLACEMENT-LONG FAILURE DIAGNOSTIC 3 STARTED" + Environment.NewLine, Utf8WithoutBom);
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

    private sealed record ProbeDefinition(
        string Id,
        int ExactVersion,
        PlantControlAuthorityMode Authority,
        int SpeedRaiseCount,
        int? ManualValveStep,
        double? ManualValveDemandPercent);

    private sealed record ProbeResult(
        string Id,
        int ExactVersion,
        PlantControlAuthorityMode RequestedAuthority,
        int SpeedRaiseCount,
        int? ManualValveStep,
        double? ManualValveDemandPercent,
        long ExecutedSteps,
        long? FirstTripStep,
        string? FirstLatchedFunctionId,
        long? FirstLatchedFunctionStep,
        double InitialRawSpeedSetpointRpm,
        double PreLoadRawSpeedSetpointRpm,
        double InitialGovernorSetpointRpm,
        double PreLoadGovernorSetpointRpm,
        double PreLoadShaftMegawatts,
        double PreLoadControlValvePercent,
        double LoadStepShaftMegawatts,
        double LoadStepGeneratorMechanicalMegawatts,
        double MinimumFrequencyHertz,
        double MaximumShaftMegawatts,
        double MaximumControlValvePercent,
        bool SurvivesTwoSecondsAfterRaise,
        bool StableTenMegawattsLate,
        bool AllFinite,
        string? ExceptionType,
        string? ExceptionMessage)
    {
        public bool DiagnosticComplete => ExceptionType is not null || ExecutedSteps == TotalSteps;
    }

    private sealed record ProbeSample(
        string ProbeId,
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
        double RawSpeedSetpointRpm,
        double GovernorSetpointRpm,
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
            && double.IsFinite(RawSpeedSetpointRpm)
            && double.IsFinite(GovernorSetpointRpm)
            && double.IsFinite(GovernorOutputPercent)
            && double.IsFinite(ControlValvePositionPercent);
    }
}
