using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.Json;
using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.ControlRoom.Automation;
using NuclearReactorSimulator.Application.Scenarios.Training;
using NuclearReactorSimulator.Domain.Physics.Control.Supervisory;
using NuclearReactorSimulator.Simulation.Physics.Control.Integration;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay;

/// <summary>
/// P1 from M10 Final Replacement-Long Closure Plan 1. This is the last planned purely exploratory dynamics gate.
/// It calibrates stationarity limits from the unchanged exact-v9 5 MWe reference and then classifies the D6 first-stage
/// holds as CONVERGED, BIASED-STATIONARY or INCONCLUSIVE; a trip is preserved as an INCONCLUSIVE evidence outcome, not a new P1 branch class. Only the exact-v9 6 MWe probe may consume the
/// pre-authorized bounded continuation when the 900 s tail is still monotonically converging above the reference noise band.
/// </summary>
public sealed class M10FinalReplacementLongClosurePlan1P1AsymptoticFirstStageQualificationTests
{
    private const string OptInEnvironmentVariable = "NRS_M10_FINAL_REPLACEMENT_LONG_P1";
    private const string ContractFileName = "m10-final-replacement-long-closure-plan1-p1-contract.json";
    private static readonly UTF8Encoding Utf8WithoutBom = new(encoderShouldEmitUTF8Identifier: false);

    [Fact(Explicit = true)]
    [Trait("Category", "M10FinalReplacementLongClosurePlan1P1")]
    public void ExactV9_AsymptoticFirstStageQualification_ClassifiesPlannedDecisionBranch()
    {
        RequireOptIn();
        ResetDirectory(ReportDirectory());

        var contract = LoadContract();
        ValidateContract(contract);
        ValidateP0PrerequisiteEvidence();

        var trajectoryRows = new List<ProbeSample>();
        var events = new List<ProbeEvent>();

        AppendProgress("calibration-start=exact-v9-stable-5mwe-reference");
        var calibration = RunNoiseCalibration(contract, trajectoryRows, events);
        WriteCalibration(calibration);
        AppendProgress($"calibration-complete=valid:{calibration.Valid}|output-limit:{F(calibration.OutputSlopeLimit)}|shaft-limit:{F(calibration.ShaftSlopeLimit)}|flow-limit:{F(calibration.SteamFlowSlopeLimit)}|pressure-limit:{F(calibration.InletPressureSlopeLimit)}");

        Assert.True(calibration.Valid, calibration.ValidationMessage);

        ProbeDefinition[] probes =
        [
            new("exact-v9-asymptotic-5p5mwe", 9, 0.5d, 5.5d, false),
            new("exact-v9-asymptotic-6mwe", 9, 1d, 6d, true),
            new("exact-v4-asymptotic-6mwe", 4, 1d, 6d, false),
        ];

        var results = new List<ProbeResult>();
        foreach (var probe in probes)
        {
            AppendProgress($"probe-start={probe.Id}");
            var result = RunProbe(contract, calibration, probe, trajectoryRows, events);
            results.Add(result);
            AppendProgress(Progress(result));
        }

        WriteProbeSummary(results);
        WriteEvents(events);
        WriteTrajectory(trajectoryRows);
        WriteDecisionSummary(contract, calibration, results);

        Assert.All(results, static result => Assert.True(result.DiagnosticComplete));
        Assert.All(results, static result => Assert.Null(result.ExceptionType));
        Assert.All(results, result => Assert.Contains(result.PrimaryClassification, contract.AllowedPrimaryClassifications));
        Assert.All(results, result => Assert.Contains(result.FinalClassification, contract.AllowedFinalClassifications));
    }

    private static NoiseCalibration RunNoiseCalibration(
        P1Contract contract,
        ICollection<ProbeSample> trajectoryRows,
        ICollection<ProbeEvent> events)
    {
        var engine = CreateEngine(exactVersion: 9, loadIncrementMegawatts: 5d);
        engine.RequestPlantControlAuthority(PlantControlAuthorityMode.SupervisoryAutomatic);
        engine.RequestSupervisoryObjective(SupervisoryObjectiveRequest.HoldCurrentOperatingPoint());

        var steps = checked(contract.NoiseControlSeconds * contract.StepsPerSecond);
        var tailSteps = checked(contract.NoiseTailSeconds * contract.StepsPerSecond);
        var subwindowSteps = checked(contract.NoiseSubwindowSeconds * contract.StepsPerSecond);
        var tail = new Queue<ProbeSample>(tailSteps + 1);
        var allFinite = true;
        string? exceptionType = null;
        string? exceptionMessage = null;

        AddEvent(events, "exact-v9-stable-5mwe-reference", engine.LogicalStep, "noise-control-start", null, null, null);

        try
        {
            for (var index = 0; index < steps; index++)
            {
                engine.Step(ControlRoomRunState.Running);
                var sample = Capture("exact-v9-stable-5mwe-reference", "noise-control", null, null, engine, contract.StepsPerSecond);
                allFinite &= sample.AllFinite;
                EnqueueTail(tail, sample, tailSteps);
                MaybeAddTrajectory(trajectoryRows, sample, contract.TrajectorySampleIntervalSteps, force: index == steps - 1);
            }
        }
        catch (Exception exception)
        {
            exceptionType = exception.GetType().FullName ?? exception.GetType().Name;
            exceptionMessage = Flatten(exception.Message);
        }

        var rows = tail.ToArray();
        var outputNoise = MaximumAbsoluteSubwindowSlope(rows, subwindowSteps, static row => row.ElectricalOutputMegawatts);
        var shaftNoise = MaximumAbsoluteSubwindowSlope(rows, subwindowSteps, static row => row.TurbineShaftMegawatts);
        var flowNoise = MaximumAbsoluteSubwindowSlope(rows, subwindowSteps, static row => row.TurbineSteamFlowKilogramsPerSecond);
        var pressureNoise = MaximumAbsoluteSubwindowSlope(rows, subwindowSteps, static row => row.TurbineInletPressureMegapascals);

        var outputLimit = DerivedSlopeLimit(outputNoise, contract.NoiseMultiplier, contract.SlopeBands.OutputMegawattsPerSecond);
        var shaftLimit = DerivedSlopeLimit(shaftNoise, contract.NoiseMultiplier, contract.SlopeBands.ShaftMegawattsPerSecond);
        var flowLimit = DerivedSlopeLimit(flowNoise, contract.NoiseMultiplier, contract.SlopeBands.SteamFlowKilogramsPerSecondSquared);
        var pressureLimit = DerivedSlopeLimit(pressureNoise, contract.NoiseMultiplier, contract.SlopeBands.InletPressureMegapascalsPerSecond);

        var valid = exceptionType is null
            && allFinite
            && rows.Length == tailSteps
            && rows.All(static row => row.BreakerClosed && !row.AnyTripActive)
            && outputLimit <= contract.SlopeBands.OutputMegawattsPerSecond.Ceiling
            && shaftLimit <= contract.SlopeBands.ShaftMegawattsPerSecond.Ceiling
            && flowLimit <= contract.SlopeBands.SteamFlowKilogramsPerSecondSquared.Ceiling
            && pressureLimit <= contract.SlopeBands.InletPressureMegapascalsPerSecond.Ceiling;

        var validationMessage = valid
            ? "reference-derived stationarity calibration valid"
            : $"reference-derived stationarity calibration invalid; exception={exceptionType ?? "none"}; finite={allFinite}; tail={rows.Length}/{tailSteps}; limits={F(outputLimit)}|{F(shaftLimit)}|{F(flowLimit)}|{F(pressureLimit)}";

        AddEvent(events, "exact-v9-stable-5mwe-reference", engine.LogicalStep, "noise-control-complete", null, null, rows.LastOrDefault());

        return new NoiseCalibration(
            valid,
            validationMessage,
            rows.Length,
            outputNoise,
            shaftNoise,
            flowNoise,
            pressureNoise,
            outputLimit,
            shaftLimit,
            flowLimit,
            pressureLimit,
            Mean(rows, static row => row.ElectricalOutputMegawatts),
            Mean(rows, static row => row.TurbineShaftMegawatts),
            Mean(rows, static row => row.TurbineSteamFlowKilogramsPerSecond),
            Mean(rows, static row => row.GeneratorFrequencyHertz),
            allFinite,
            exceptionType,
            exceptionMessage);
    }

    private static ProbeResult RunProbe(
        P1Contract contract,
        NoiseCalibration calibration,
        ProbeDefinition probe,
        ICollection<ProbeSample> trajectoryRows,
        ICollection<ProbeEvent> events)
    {
        var engine = CreateEngine(probe.ExactVersion, probe.LoadIncrementMegawatts);
        engine.RequestPlantControlAuthority(PlantControlAuthorityMode.SupervisoryAutomatic);

        var presentation = engine.CreatePresentationSnapshot(ControlRoomRunState.Paused);
        var generatorId = Assert.Single(presentation.Electrical.Generators).GeneratorId;
        var initialRequestedMegawatts = Assert.Single(presentation.Electrical.Generators).RequestedElectricalPower.NumericValue
            ?? throw new InvalidOperationException("Initial generator requested load is unavailable.");
        var initialThermalMegawatts = presentation.ReactorCore.ReactorThermalPower.NumericValue
            ?? throw new InvalidOperationException("Initial reactor thermal power is unavailable.");
        var targetThermalMegawatts = initialThermalMegawatts / initialRequestedMegawatts * probe.TargetLoadMegawatts;

        var stationaryWindowSteps = checked(contract.StationaryWindowSeconds * contract.StepsPerSecond);
        var convergenceWindowSteps = checked(contract.ConvergenceWindowSeconds * contract.StepsPerSecond);
        var primaryHoldSteps = checked(contract.PrimaryHoldSeconds * contract.StepsPerSecond);
        var continuationMaxSteps = checked(contract.ContinuationMaxTotalHoldSeconds * contract.StepsPerSecond);
        var minimumStationarySteps = checked(contract.MinimumStationaryAssessmentSeconds * contract.StepsPerSecond);
        var preparationTimeoutSteps = checked(contract.PreparationTimeoutSeconds * contract.StepsPerSecond);
        var tail = new Queue<ProbeSample>(stationaryWindowSteps + 1);

        long? loadCommandStep = null;
        long? firstTripStep = null;
        string? firstLatchedFunctionId = null;
        long? firstLatchedFunctionStep = null;
        string? exceptionType = null;
        string? exceptionMessage = null;
        var allFinite = true;
        var strictConsecutiveSteps = 0;
        var continuationInvoked = false;
        var primaryClassification = "INCONCLUSIVE";
        var finalClassification = "INCONCLUSIVE";
        var completionReason = "preparation-timeout";
        var holdExecutedSteps = 0;

        engine.RequestSupervisoryObjective(SupervisoryObjectiveRequest.HoldReactorPower(targetThermalMegawatts * 1_000_000d));
        AddEvent(events, probe.Id, engine.LogicalStep, "prepare-start", probe.TargetLoadMegawatts, targetThermalMegawatts, null);

        try
        {
            for (var preparationIndex = 0; preparationIndex < preparationTimeoutSteps; preparationIndex++)
            {
                var before = Capture(probe.Id, "prepare", probe.TargetLoadMegawatts, targetThermalMegawatts, engine, contract.StepsPerSecond);
                if (IsThermallyReady(before, targetThermalMegawatts, contract))
                {
                    loadCommandStep = engine.LogicalStep + 1;
                    engine.QueueOperatorCommand(new ControlRoomCommand(
                        ControlRoomCommandKind.GeneratorLoadRaise,
                        generatorId,
                        ControlRoomCommandTargetKind.Generator));
                    AddEvent(events, probe.Id, loadCommandStep.Value, "load-command", probe.TargetLoadMegawatts, targetThermalMegawatts, before);
                    completionReason = "primary-hold";
                    break;
                }

                engine.Step(ControlRoomRunState.Running);
                var sample = Capture(probe.Id, "prepare", probe.TargetLoadMegawatts, targetThermalMegawatts, engine, contract.StepsPerSecond);
                allFinite &= sample.AllFinite;
                MaybeAddTrajectory(trajectoryRows, sample, contract.TrajectorySampleIntervalSteps, force: false);
                CaptureTripAndLatch(engine, sample, ref firstTripStep, ref firstLatchedFunctionId, ref firstLatchedFunctionStep);
                if (sample.AnyTripActive)
                {
                    primaryClassification = "INCONCLUSIVE";
                    finalClassification = "INCONCLUSIVE";
                    completionReason = "trip-during-preparation";
                    AddEvent(events, probe.Id, sample.LogicalStep, "trip", probe.TargetLoadMegawatts, targetThermalMegawatts, sample);
                    break;
                }
            }

            if (loadCommandStep.HasValue && firstTripStep is null)
            {
                var maximumHoldSteps = primaryHoldSteps;
                while (holdExecutedSteps < maximumHoldSteps)
                {
                    engine.Step(ControlRoomRunState.Running);
                    holdExecutedSteps++;
                    var phase = holdExecutedSteps <= primaryHoldSteps ? "primary-hold" : "bounded-continuation";
                    var sample = Capture(probe.Id, phase, probe.TargetLoadMegawatts, targetThermalMegawatts, engine, contract.StepsPerSecond);
                    allFinite &= sample.AllFinite;
                    EnqueueTail(tail, sample, stationaryWindowSteps);
                    MaybeAddTrajectory(trajectoryRows, sample, contract.TrajectorySampleIntervalSteps, force: false);
                    CaptureTripAndLatch(engine, sample, ref firstTripStep, ref firstLatchedFunctionId, ref firstLatchedFunctionStep);

                    if (sample.AnyTripActive)
                    {
                        primaryClassification = holdExecutedSteps <= primaryHoldSteps ? "INCONCLUSIVE" : primaryClassification;
                        finalClassification = "INCONCLUSIVE";
                        completionReason = "trip-during-hold";
                        AddEvent(events, probe.Id, sample.LogicalStep, "trip", probe.TargetLoadMegawatts, targetThermalMegawatts, sample);
                        break;
                    }

                    strictConsecutiveSteps = IsStrictPoint(sample, probe.TargetLoadMegawatts, contract)
                        ? strictConsecutiveSteps + 1
                        : 0;

                    if (holdExecutedSteps % contract.StepsPerSecond == 0)
                    {
                        var window = tail.ToArray();
                        if (strictConsecutiveSteps >= convergenceWindowSteps
                            && HasConverged(window, convergenceWindowSteps, calibration))
                        {
                            if (holdExecutedSteps <= primaryHoldSteps)
                            {
                                primaryClassification = "CONVERGED";
                            }

                            finalClassification = "CONVERGED";
                            completionReason = continuationInvoked ? "converged-during-continuation" : "converged-during-primary";
                            AddEvent(events, probe.Id, sample.LogicalStep, "converged", probe.TargetLoadMegawatts, targetThermalMegawatts, sample);
                            break;
                        }

                        if (holdExecutedSteps >= minimumStationarySteps
                            && IsBiasedStationary(window, probe.TargetLoadMegawatts, contract, calibration))
                        {
                            if (holdExecutedSteps <= primaryHoldSteps)
                            {
                                primaryClassification = "BIASED-STATIONARY";
                            }

                            finalClassification = "BIASED-STATIONARY";
                            completionReason = continuationInvoked ? "biased-stationary-during-continuation" : "biased-stationary-during-primary";
                            AddEvent(events, probe.Id, sample.LogicalStep, "biased-stationary", probe.TargetLoadMegawatts, targetThermalMegawatts, sample);
                            break;
                        }
                    }

                    if (holdExecutedSteps == primaryHoldSteps)
                    {
                        var window = tail.ToArray();
                        var stillConverging = IsStillConverging(window, probe.TargetLoadMegawatts, contract, calibration);
                        primaryClassification = stillConverging ? contract.PrimaryContinuationClassification : "INCONCLUSIVE";
                        AddEvent(events, probe.Id, sample.LogicalStep, "primary-horizon", probe.TargetLoadMegawatts, targetThermalMegawatts, sample);

                        if (probe.AllowContinuation
                            && string.Equals(probe.Id, contract.ContinuationProbeId, StringComparison.Ordinal)
                            && stillConverging)
                        {
                            continuationInvoked = true;
                            maximumHoldSteps = continuationMaxSteps;
                            completionReason = "bounded-continuation";
                            AddEvent(events, probe.Id, sample.LogicalStep, "bounded-continuation-start", probe.TargetLoadMegawatts, targetThermalMegawatts, sample);
                        }
                        else
                        {
                            finalClassification = "INCONCLUSIVE";
                            completionReason = stillConverging ? "still-converging-without-authorized-continuation" : "primary-inconclusive";
                            break;
                        }
                    }
                }

                if (holdExecutedSteps >= continuationMaxSteps
                    && continuationInvoked
                    && finalClassification == "INCONCLUSIVE")
                {
                    completionReason = "continuation-horizon-inconclusive";
                    AddEvent(events, probe.Id, engine.LogicalStep, "continuation-horizon", probe.TargetLoadMegawatts, targetThermalMegawatts, tail.LastOrDefault());
                }
            }
        }
        catch (Exception exception)
        {
            exceptionType = exception.GetType().FullName ?? exception.GetType().Name;
            exceptionMessage = Flatten(exception.Message);
            completionReason = "exception";
        }

        var finalTail = tail.ToArray();
        var tailStatistics = CalculateTailStatistics(finalTail, probe.TargetLoadMegawatts);
        if (finalTail.Length > 0)
        {
            MaybeAddTrajectory(trajectoryRows, finalTail[^1], contract.TrajectorySampleIntervalSteps, force: true);
        }

        return new ProbeResult(
            probe.Id,
            probe.ExactVersion,
            probe.TargetLoadMegawatts,
            probe.LoadIncrementMegawatts,
            loadCommandStep,
            holdExecutedSteps,
            primaryClassification,
            finalClassification,
            continuationInvoked,
            completionReason,
            firstTripStep,
            firstLatchedFunctionId,
            firstLatchedFunctionStep,
            tailStatistics,
            allFinite,
            exceptionType,
            exceptionMessage);
    }

    private static bool IsThermallyReady(ProbeSample sample, double targetThermalMegawatts, P1Contract contract)
        => !sample.AnyTripActive
            && sample.BreakerClosed
            && sample.ReactorThermalMegawatts >= targetThermalMegawatts - contract.ThermalReadinessToleranceMegawatts
            && Math.Abs(sample.GeneratorFrequencyHertz - 50d) <= 0.1d;

    private static bool IsStrictPoint(ProbeSample sample, double targetLoadMegawatts, P1Contract contract)
        => !sample.AnyTripActive
            && sample.BreakerClosed
            && Math.Abs(sample.GeneratorFrequencySlipHertz) <= contract.FrequencyToleranceHertz
            && Math.Abs(sample.ElectricalOutputMegawatts - targetLoadMegawatts) <= contract.OutputToleranceMegawatts
            && Math.Abs(sample.NetRotorAccelerationPowerMegawatts) <= contract.NetAccelerationToleranceMegawatts
            && Math.Abs(sample.DispatchMechanicalAdequacyMegawatts) <= contract.DispatchAdequacyToleranceMegawatts;

    private static bool HasConverged(
        IReadOnlyList<ProbeSample> rows,
        int convergenceWindowSteps,
        NoiseCalibration calibration)
    {
        if (rows.Count < convergenceWindowSteps)
        {
            return false;
        }

        var window = rows.Skip(rows.Count - convergenceWindowSteps).ToArray();
        return SlopesWithinStationaryBand(window, calibration);
    }

    private static bool IsBiasedStationary(
        IReadOnlyList<ProbeSample> rows,
        double targetLoadMegawatts,
        P1Contract contract,
        NoiseCalibration calibration)
    {
        if (rows.Count < contract.StationaryWindowSeconds * contract.StepsPerSecond)
        {
            return false;
        }

        if (!rows.All(static row => row.BreakerClosed && !row.AnyTripActive))
        {
            return false;
        }

        if (!SlopesWithinStationaryBand(rows, calibration))
        {
            return false;
        }

        var meanAbsoluteSlip = rows.Average(static row => Math.Abs(row.GeneratorFrequencySlipHertz));
        var meanAbsoluteAcceleration = rows.Average(static row => Math.Abs(row.NetRotorAccelerationPowerMegawatts));
        var meanOutputError = Math.Abs(targetLoadMegawatts - Mean(rows, static row => row.ElectricalOutputMegawatts));
        var meanDispatchAdequacy = Math.Abs(Mean(rows, static row => row.DispatchMechanicalAdequacyMegawatts));

        return meanAbsoluteSlip <= contract.FrequencyToleranceHertz
            && meanAbsoluteAcceleration <= contract.NetAccelerationToleranceMegawatts
            && (meanOutputError > contract.OutputToleranceMegawatts
                || meanDispatchAdequacy > contract.DispatchAdequacyToleranceMegawatts);
    }

    private static bool IsStillConverging(
        IReadOnlyList<ProbeSample> rows,
        double targetLoadMegawatts,
        P1Contract contract,
        NoiseCalibration calibration)
    {
        var requiredRows = contract.StationaryWindowSeconds * contract.StepsPerSecond;
        if (rows.Count < requiredRows || rows.Any(static row => row.AnyTripActive || !row.BreakerClosed))
        {
            return false;
        }

        var meanOutput = Mean(rows, static row => row.ElectricalOutputMegawatts);
        var error = targetLoadMegawatts - meanOutput;
        var outputSlope = Slope(rows, static row => row.ElectricalOutputMegawatts);
        var shaftSlope = Slope(rows, static row => row.TurbineShaftMegawatts);
        var flowSlope = Slope(rows, static row => row.TurbineSteamFlowKilogramsPerSecond);
        if (Math.Abs(error) <= contract.OutputToleranceMegawatts
            || Math.Sign(error) * outputSlope <= calibration.OutputSlopeLimit)
        {
            return false;
        }

        if (shaftSlope <= calibration.ShaftSlopeLimit && flowSlope <= calibration.SteamFlowSlopeLimit)
        {
            return false;
        }

        var subwindowSteps = contract.NoiseSubwindowSeconds * contract.StepsPerSecond;
        var towardTargetWindows = 0;
        var totalWindows = 0;
        for (var start = 0; start + subwindowSteps <= rows.Count; start += subwindowSteps)
        {
            var window = rows.Skip(start).Take(subwindowSteps).ToArray();
            var windowSlope = Slope(window, static row => row.ElectricalOutputMegawatts);
            if (Math.Sign(error) * windowSlope > 0d)
            {
                towardTargetWindows++;
            }

            totalWindows++;
        }

        return totalWindows > 0 && towardTargetWindows >= Math.Max(1, totalWindows - 1);
    }

    private static bool SlopesWithinStationaryBand(IReadOnlyList<ProbeSample> rows, NoiseCalibration calibration)
        => Math.Abs(Slope(rows, static row => row.ElectricalOutputMegawatts)) <= calibration.OutputSlopeLimit
            && Math.Abs(Slope(rows, static row => row.TurbineShaftMegawatts)) <= calibration.ShaftSlopeLimit
            && Math.Abs(Slope(rows, static row => row.TurbineSteamFlowKilogramsPerSecond)) <= calibration.SteamFlowSlopeLimit
            && Math.Abs(Slope(rows, static row => row.TurbineInletPressureMegapascals)) <= calibration.InletPressureSlopeLimit;

    private static TailStatistics CalculateTailStatistics(IReadOnlyList<ProbeSample> rows, double targetLoadMegawatts)
        => new(
            rows.Count,
            Mean(rows, static row => row.GeneratorFrequencyHertz),
            Mean(rows, static row => row.ElectricalOutputMegawatts),
            targetLoadMegawatts - Mean(rows, static row => row.ElectricalOutputMegawatts),
            Mean(rows, static row => row.TurbineShaftMegawatts),
            Mean(rows, static row => row.DispatchMechanicalAdequacyMegawatts),
            Mean(rows, static row => row.NetRotorAccelerationPowerMegawatts),
            Mean(rows, static row => row.TurbineSteamFlowKilogramsPerSecond),
            Mean(rows, static row => row.TurbineInletPressureMegapascals),
            Slope(rows, static row => row.ElectricalOutputMegawatts),
            Slope(rows, static row => row.TurbineShaftMegawatts),
            Slope(rows, static row => row.TurbineSteamFlowKilogramsPerSecond),
            Slope(rows, static row => row.TurbineInletPressureMegapascals));

    private static double DerivedSlopeLimit(double referenceNoiseSlope, double multiplier, SlopeBand band)
        => Math.Max(band.Floor, referenceNoiseSlope * multiplier);

    private static double MaximumAbsoluteSubwindowSlope(
        IReadOnlyList<ProbeSample> rows,
        int subwindowSteps,
        Func<ProbeSample, double> selector)
    {
        if (rows.Count < subwindowSteps || subwindowSteps <= 1)
        {
            return double.NaN;
        }

        var maximum = 0d;
        for (var start = 0; start + subwindowSteps <= rows.Count; start += subwindowSteps)
        {
            var window = rows.Skip(start).Take(subwindowSteps).ToArray();
            maximum = Math.Max(maximum, Math.Abs(Slope(window, selector)));
        }

        return maximum;
    }

    private static double Slope(IReadOnlyList<ProbeSample> rows, Func<ProbeSample, double> selector)
    {
        if (rows.Count < 2)
        {
            return double.NaN;
        }

        var meanX = rows.Average(static row => row.SimulatedSeconds);
        var meanY = rows.Average(selector);
        var numerator = 0d;
        var denominator = 0d;
        foreach (var row in rows)
        {
            var dx = row.SimulatedSeconds - meanX;
            numerator += dx * (selector(row) - meanY);
            denominator += dx * dx;
        }

        return denominator > 0d ? numerator / denominator : double.NaN;
    }

    private static double Mean(IReadOnlyList<ProbeSample> rows, Func<ProbeSample, double> selector)
        => rows.Count == 0 ? double.NaN : rows.Average(selector);

    private static void EnqueueTail(Queue<ProbeSample> queue, ProbeSample sample, int capacity)
    {
        queue.Enqueue(sample);
        while (queue.Count > capacity)
        {
            queue.Dequeue();
        }
    }

    private static IntegratedAutomaticOperationRuntimeEngine CreateEngine(int exactVersion, double loadIncrementMegawatts)
    {
        var baseline = exactVersion switch
        {
            9 => Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(
                new DesktopSustainedGenerationPostMoistureEquilibriumCandidateInitialConditionFactory().CreateRuntimeEngine()),
            4 => Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(
                new DesktopSustainedGenerationI5RepairedActivationCandidateInitialConditionFactory().CreateRuntimeEngine()),
            _ => throw new ArgumentOutOfRangeException(nameof(exactVersion), exactVersion, "Unsupported P1 exact version."),
        };

        if (Math.Abs(loadIncrementMegawatts - 5d) <= 1e-12d)
        {
            return baseline;
        }

        var solverField = typeof(IntegratedAutomaticOperationRuntimeEngine).GetField(
            "_solver",
            BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("P1 test-only runtime clone could not locate the private solver field.");
        var solver = solverField.GetValue(baseline) as IntegratedAutomaticOperationSolver
            ?? throw new InvalidOperationException("P1 test-only runtime clone could not read the integrated solver.");
        var commandPolicy = new ControlRoomRuntimeCommandPolicy(
            ControlRoomRuntimeCommandPolicy.Default.TurbineSpeedSetpointIncrementRpm,
            loadIncrementMegawatts * 1_000_000d);

        return new IntegratedAutomaticOperationRuntimeEngine(
            solver,
            baseline.CurrentState,
            baseline.PersistentInputs,
            baseline.LatestCanonicalSnapshot,
            baseline.FixedDeltaTime,
            baseline.LogicalStep,
            commandPolicy);
    }

    private static ProbeSample Capture(
        string probeId,
        string phase,
        double? targetLoadMegawatts,
        double? targetThermalMegawatts,
        IntegratedAutomaticOperationRuntimeEngine engine,
        int stepsPerSecond)
    {
        var protectedControl = engine.LatestCanonicalSnapshot.Control.ProtectedControl;
        var cycle = protectedControl.FullPlant.IntegratedCycle;
        var turbine = cycle.TurbineExpansion;
        var stage = Assert.Single(turbine.StageGroups);
        var rotor = Assert.Single(turbine.Rotors);
        var generator = Assert.Single(cycle.Generators);
        var grid = cycle.GeneratorGrid.Grid;
        var generatorDefinition = cycle.Definition.GeneratorGridSystem.GetGenerator(generator.GeneratorId);
        var coupling = generatorDefinition.GridCoupling
            ?? throw new InvalidOperationException("P1 requires the canonical synchronous-grid coupling definition.");
        var train = Assert.Single(turbine.MainSteamNetwork.AdmissionTrains);
        var presentation = engine.CreatePresentationSnapshot(ControlRoomRunState.Paused);
        var presentationGenerator = Assert.Single(presentation.Electrical.Generators);
        var step = engine.LogicalStep;
        var signedPhaseLeadRadians = SignedShortestPhaseLeadRadians(
            generator.FinalElectricalPhaseAngle.Radians,
            grid.FinalPhaseAngle.Radians);
        var frequencySlipHertz = generator.FinalElectricalFrequency.Hertz - grid.Frequency.Hertz;
        var phaseCorrectionMegawatts = coupling.MaximumSynchronizingCorrectionPower.Megawatts * Math.Sin(signedPhaseLeadRadians);
        var frequencyCorrectionMegawatts = coupling.FrequencyDampingPowerAtOneHertzSlip.Megawatts * frequencySlipHertz;
        var requestedMechanicalMegawatts = generator.RequestedElectricalPower.Megawatts / generatorDefinition.Efficiency.Fraction;
        var netRotorAccelerationMegawatts = rotor.ShaftPower.Megawatts
            - rotor.ExternalLoadPower.Megawatts
            - rotor.PassiveMechanicalLossPower.Megawatts;
        var dispatchMechanicalAdequacyMegawatts = rotor.ShaftPower.Megawatts
            - rotor.PassiveMechanicalLossPower.Megawatts
            - requestedMechanicalMegawatts;

        return new ProbeSample(
            probeId,
            step,
            step / (double)stepsPerSecond,
            phase,
            targetLoadMegawatts,
            targetThermalMegawatts,
            presentationGenerator.RequestedElectricalPower.NumericValue ?? double.NaN,
            presentationGenerator.ElectricalOutput.NumericValue ?? double.NaN,
            presentation.ReactorCore.ReactorThermalPower.NumericValue ?? double.NaN,
            generator.MechanicalInputPower.Megawatts,
            rotor.ShaftPower.Megawatts,
            rotor.PassiveMechanicalLossPower.Megawatts,
            netRotorAccelerationMegawatts,
            requestedMechanicalMegawatts,
            dispatchMechanicalAdequacyMegawatts,
            stage.EffectiveMassFlowRate.KilogramsPerSecond,
            stage.InletPressure.Megapascals,
            stage.EffectiveIdealSpecificWork.JoulesPerKilogram,
            train.ControlValve.EffectivePosition.Percent,
            turbine.MainSteamNetwork.TotalReliefMassFlowRate.KilogramsPerSecond,
            rotor.FinalAngularSpeed.RevolutionsPerMinute,
            generator.FinalElectricalFrequency.Hertz,
            frequencySlipHertz,
            signedPhaseLeadRadians,
            phaseCorrectionMegawatts,
            frequencyCorrectionMegawatts,
            generator.CommandedElectromagneticTorque.NewtonMetres,
            generator.EffectiveElectromagneticTorque.NewtonMetres,
            generator.BreakerFinallyClosed,
            protectedControl.Protection.ReactorScramActive,
            protectedControl.Protection.TurbineTripActive,
            protectedControl.Protection.GeneratorTripActive);
    }

    private static double SignedShortestPhaseLeadRadians(double generatorRadians, double gridRadians)
    {
        var difference = generatorRadians - gridRadians;
        var fullTurn = 2d * Math.PI;
        difference = (difference + Math.PI) % fullTurn;
        if (difference < 0d)
        {
            difference += fullTurn;
        }

        return difference - Math.PI;
    }

    private static void CaptureTripAndLatch(
        IntegratedAutomaticOperationRuntimeEngine engine,
        ProbeSample sample,
        ref long? firstTripStep,
        ref string? firstLatchedFunctionId,
        ref long? firstLatchedFunctionStep)
    {
        if (sample.AnyTripActive)
        {
            firstTripStep ??= sample.LogicalStep;
        }

        if (firstLatchedFunctionId is not null)
        {
            return;
        }

        var firstLatch = engine.LatestCanonicalSnapshot.Control.ProtectedControl.Protection.Functions
            .FirstOrDefault(static function => function.IsLatched);
        if (firstLatch is not null)
        {
            firstLatchedFunctionId = firstLatch.FunctionId;
            firstLatchedFunctionStep = sample.LogicalStep;
        }
    }

    private static void MaybeAddTrajectory(
        ICollection<ProbeSample> rows,
        ProbeSample sample,
        int intervalSteps,
        bool force)
    {
        if (force || sample.LogicalStep % intervalSteps == 0)
        {
            rows.Add(sample);
        }
    }

    private static void AddEvent(
        ICollection<ProbeEvent> events,
        string probeId,
        long logicalStep,
        string eventKind,
        double? targetLoadMegawatts,
        double? targetThermalMegawatts,
        ProbeSample? sample)
        => events.Add(new ProbeEvent(
            probeId,
            logicalStep,
            eventKind,
            targetLoadMegawatts,
            targetThermalMegawatts,
            sample?.RequestedElectricalMegawatts,
            sample?.ElectricalOutputMegawatts,
            sample?.ReactorThermalMegawatts,
            sample?.TurbineShaftMegawatts,
            sample?.GeneratorFrequencyHertz,
            sample?.GeneratorFrequencySlipHertz,
            sample?.NetRotorAccelerationPowerMegawatts,
            sample?.DispatchMechanicalAdequacyMegawatts,
            sample?.TurbineSteamFlowKilogramsPerSecond,
            sample?.TurbineInletPressureMegapascals,
            sample?.ControlValvePercentOpen));

    private static string Progress(ProbeResult result)
        => $"probe-complete={result.Id}|primary:{result.PrimaryClassification}|final:{result.FinalClassification}|continuation:{result.ContinuationInvoked}|hold-steps:{result.HoldExecutedSteps}|trip:{I(result.FirstTripStep)}|output-error:{F(result.Tail.OutputErrorMegawatts)}|dispatch:{F(result.Tail.MeanDispatchAdequacyMegawatts)}|output-slope:{F(result.Tail.OutputSlopeMegawattsPerSecond)}";

    private static void WriteCalibration(NoiseCalibration calibration)
    {
        var lines = new[]
        {
            "valid,validation_message,tail_rows,reference_output_noise_slope_mw_s,reference_shaft_noise_slope_mw_s,reference_flow_noise_slope_kg_s2,reference_pressure_noise_slope_mpa_s,derived_output_limit_mw_s,derived_shaft_limit_mw_s,derived_flow_limit_kg_s2,derived_pressure_limit_mpa_s,reference_mean_output_mwe,reference_mean_shaft_mw,reference_mean_flow_kg_s,reference_mean_frequency_hz,all_finite,exception_type,exception_message",
            string.Join(",", new[]
            {
                calibration.Valid.ToString(),
                Csv(calibration.ValidationMessage),
                calibration.TailRows.ToString(CultureInfo.InvariantCulture),
                F(calibration.ReferenceOutputNoiseSlope),
                F(calibration.ReferenceShaftNoiseSlope),
                F(calibration.ReferenceSteamFlowNoiseSlope),
                F(calibration.ReferenceInletPressureNoiseSlope),
                F(calibration.OutputSlopeLimit),
                F(calibration.ShaftSlopeLimit),
                F(calibration.SteamFlowSlopeLimit),
                F(calibration.InletPressureSlopeLimit),
                F(calibration.ReferenceMeanOutputMegawatts),
                F(calibration.ReferenceMeanShaftMegawatts),
                F(calibration.ReferenceMeanSteamFlowKilogramsPerSecond),
                F(calibration.ReferenceMeanFrequencyHertz),
                calibration.AllFinite.ToString(),
                Csv(calibration.ExceptionType ?? string.Empty),
                Csv(calibration.ExceptionMessage ?? string.Empty),
            }),
        };
        File.WriteAllLines(Path.Combine(ReportDirectory(), "01-reference-noise-calibration.csv"), lines, Utf8WithoutBom);
    }

    private static void WriteProbeSummary(IEnumerable<ProbeResult> results)
    {
        var lines = new List<string>
        {
            "probe_id,exact_version,target_load_mwe,load_increment_mwe,load_command_step,hold_executed_steps,primary_classification,final_classification,continuation_invoked,completion_reason,first_trip_step,first_latched_function_id,first_latched_function_step,tail_rows,tail_mean_frequency_hz,tail_mean_output_mwe,tail_output_error_mwe,tail_mean_shaft_mw,tail_mean_dispatch_adequacy_mw,tail_mean_net_acceleration_mw,tail_mean_flow_kg_s,tail_mean_inlet_mpa,tail_output_slope_mw_s,tail_shaft_slope_mw_s,tail_flow_slope_kg_s2,tail_inlet_pressure_slope_mpa_s,all_finite,exception_type,exception_message"
        };
        lines.AddRange(results.Select(static result => string.Join(",", new[]
        {
            Csv(result.Id),
            result.ExactVersion.ToString(CultureInfo.InvariantCulture),
            F(result.TargetLoadMegawatts),
            F(result.LoadIncrementMegawatts),
            I(result.LoadCommandStep),
            result.HoldExecutedSteps.ToString(CultureInfo.InvariantCulture),
            Csv(result.PrimaryClassification),
            Csv(result.FinalClassification),
            result.ContinuationInvoked.ToString(),
            Csv(result.CompletionReason),
            I(result.FirstTripStep),
            Csv(result.FirstLatchedFunctionId ?? string.Empty),
            I(result.FirstLatchedFunctionStep),
            result.Tail.RowCount.ToString(CultureInfo.InvariantCulture),
            F(result.Tail.MeanFrequencyHertz),
            F(result.Tail.MeanOutputMegawatts),
            F(result.Tail.OutputErrorMegawatts),
            F(result.Tail.MeanShaftMegawatts),
            F(result.Tail.MeanDispatchAdequacyMegawatts),
            F(result.Tail.MeanNetAccelerationMegawatts),
            F(result.Tail.MeanSteamFlowKilogramsPerSecond),
            F(result.Tail.MeanInletPressureMegapascals),
            F(result.Tail.OutputSlopeMegawattsPerSecond),
            F(result.Tail.ShaftSlopeMegawattsPerSecond),
            F(result.Tail.SteamFlowSlopeKilogramsPerSecondSquared),
            F(result.Tail.InletPressureSlopeMegapascalsPerSecond),
            result.AllFinite.ToString(),
            Csv(result.ExceptionType ?? string.Empty),
            Csv(result.ExceptionMessage ?? string.Empty),
        })));
        File.WriteAllLines(Path.Combine(ReportDirectory(), "02-asymptotic-probe-summary.csv"), lines, Utf8WithoutBom);
    }

    private static void WriteEvents(IEnumerable<ProbeEvent> events)
    {
        var lines = new List<string>
        {
            "probe_id,logical_step,event_kind,target_load_mwe,target_thermal_mw,requested_mwe,output_mwe,thermal_mw,shaft_mw,frequency_hz,frequency_slip_hz,net_acceleration_mw,dispatch_adequacy_mw,flow_kg_s,inlet_mpa,control_valve_percent"
        };
        lines.AddRange(events.Select(static item => string.Join(",", new[]
        {
            Csv(item.ProbeId),
            item.LogicalStep.ToString(CultureInfo.InvariantCulture),
            Csv(item.EventKind),
            F(item.TargetLoadMegawatts),
            F(item.TargetThermalMegawatts),
            F(item.RequestedMegawatts),
            F(item.OutputMegawatts),
            F(item.ThermalMegawatts),
            F(item.ShaftMegawatts),
            F(item.FrequencyHertz),
            F(item.FrequencySlipHertz),
            F(item.NetAccelerationMegawatts),
            F(item.DispatchAdequacyMegawatts),
            F(item.FlowKilogramsPerSecond),
            F(item.InletPressureMegapascals),
            F(item.ControlValvePercentOpen),
        })));
        File.WriteAllLines(Path.Combine(ReportDirectory(), "03-asymptotic-events.csv"), lines, Utf8WithoutBom);
    }

    private static void WriteTrajectory(IEnumerable<ProbeSample> rows)
    {
        var lines = new List<string>
        {
            "probe_id,logical_step,simulated_seconds,phase,target_load_mwe,target_thermal_mw,requested_electrical_mwe,electrical_output_mwe,reactor_thermal_mw,generator_external_load_mw,turbine_shaft_mw,passive_mechanical_loss_mw,net_rotor_acceleration_power_mw,requested_mechanical_dispatch_mw,dispatch_mechanical_adequacy_mw,turbine_flow_kg_s,turbine_inlet_mpa,effective_specific_work_j_kg,control_valve_percent,relief_flow_kg_s,rotor_rpm,generator_frequency_hz,frequency_slip_hz,signed_phase_lead_rad,phase_correction_power_mw,frequency_correction_power_mw,commanded_em_torque_nm,effective_em_torque_nm,breaker_closed,reactor_scram,turbine_trip,generator_trip"
        };
        lines.AddRange(rows.Select(static row => string.Join(",", new[]
        {
            Csv(row.ProbeId),
            row.LogicalStep.ToString(CultureInfo.InvariantCulture),
            F(row.SimulatedSeconds),
            Csv(row.Phase),
            F(row.TargetLoadMegawatts),
            F(row.TargetThermalMegawatts),
            F(row.RequestedElectricalMegawatts),
            F(row.ElectricalOutputMegawatts),
            F(row.ReactorThermalMegawatts),
            F(row.GeneratorMechanicalInputMegawatts),
            F(row.TurbineShaftMegawatts),
            F(row.PassiveMechanicalLossMegawatts),
            F(row.NetRotorAccelerationPowerMegawatts),
            F(row.RequestedMechanicalDispatchMegawatts),
            F(row.DispatchMechanicalAdequacyMegawatts),
            F(row.TurbineSteamFlowKilogramsPerSecond),
            F(row.TurbineInletPressureMegapascals),
            F(row.EffectiveSpecificWorkJoulesPerKilogram),
            F(row.ControlValvePercentOpen),
            F(row.ReliefMassFlowKilogramsPerSecond),
            F(row.RotorRpm),
            F(row.GeneratorFrequencyHertz),
            F(row.GeneratorFrequencySlipHertz),
            F(row.SignedPhaseLeadRadians),
            F(row.PhaseCorrectionPowerMegawatts),
            F(row.FrequencyCorrectionPowerMegawatts),
            F(row.CommandedElectromagneticTorqueNewtonMetres),
            F(row.EffectiveElectromagneticTorqueNewtonMetres),
            row.BreakerClosed.ToString(),
            row.ReactorScram.ToString(),
            row.TurbineTrip.ToString(),
            row.GeneratorTrip.ToString(),
        })));
        File.WriteAllLines(Path.Combine(ReportDirectory(), "04-asymptotic-trajectories.csv"), lines, Utf8WithoutBom);
    }

    private static void WriteDecisionSummary(
        P1Contract contract,
        NoiseCalibration calibration,
        IReadOnlyCollection<ProbeResult> results)
    {
        var v9Half = results.Single(static result => result.Id == "exact-v9-asymptotic-5p5mwe");
        var v9One = results.Single(static result => result.Id == "exact-v9-asymptotic-6mwe");
        var v4One = results.Single(static result => result.Id == "exact-v4-asymptotic-6mwe");
        var branchSignal = v9One.FinalClassification switch
        {
            "CONVERGED" => "P3-W-WORKLOAD-PROCEDURE-CANDIDATE",
            "BIASED-STATIONARY" => "P3-R-RUNTIME-OWNERSHIP-CANDIDATE",
            _ => "P2-PLAN-STOP-INCONCLUSIVE",
        };

        var lines = new[]
        {
            "scope=M10 Final Replacement-Long Closure Plan 1 P1 Asymptotic First-Stage Qualification; P0 Hotfix 2 is validated; this gate changes no production src, replacement workload, authority policy, generator-load semantics, protection semantics, exact-v9 runtime or mission pack;",
            $"contract={contract.ContractId};baseline={contract.Baseline};",
            $"reference-calibration=valid:{calibration.Valid}|output-limit:{F(calibration.OutputSlopeLimit)}|shaft-limit:{F(calibration.ShaftSlopeLimit)}|flow-limit:{F(calibration.SteamFlowSlopeLimit)}|pressure-limit:{F(calibration.InletPressureSlopeLimit)};",
            DecisionLine(v9Half),
            DecisionLine(v9One),
            DecisionLine(v4One),
            $"p1-primary-decision-probe={v9One.Id};p1-final-classification={v9One.FinalClassification};p1-primary-classification={v9One.PrimaryClassification};bounded-continuation-invoked={v9One.ContinuationInvoked};",
            $"p2-branch-signal={branchSignal};next-authorized-gate=P2-Decision-Gate;",
            "authorization=P1 evidence only; P2 must record the branch decision before P3; Replacement-Long Execution 1 remains RED; second replacement-long freeze remains unauthorized; exact-v9 remains immutable inside P1;",
            $"m10-final-replacement-long-closure-plan1-p1-passes={calibration.Valid && results.All(static result => result.DiagnosticComplete && result.ExceptionType is null)}",
        };
        File.WriteAllLines(Path.Combine(ReportDirectory(), "05-p1-decision-summary.txt"), lines, Utf8WithoutBom);
    }

    private static string DecisionLine(ProbeResult result)
        => $"probe={result.Id}|primary:{result.PrimaryClassification}|final:{result.FinalClassification}|continuation:{result.ContinuationInvoked}|hold-steps:{result.HoldExecutedSteps}|trip:{I(result.FirstTripStep)}|latch:{result.FirstLatchedFunctionId ?? "none"}|tail-frequency:{F(result.Tail.MeanFrequencyHertz)}|tail-output:{F(result.Tail.MeanOutputMegawatts)}|output-error:{F(result.Tail.OutputErrorMegawatts)}|tail-shaft:{F(result.Tail.MeanShaftMegawatts)}|dispatch-adequacy:{F(result.Tail.MeanDispatchAdequacyMegawatts)}|output-slope:{F(result.Tail.OutputSlopeMegawattsPerSecond)}|shaft-slope:{F(result.Tail.ShaftSlopeMegawattsPerSecond)}|flow-slope:{F(result.Tail.SteamFlowSlopeKilogramsPerSecondSquared)}|pressure-slope:{F(result.Tail.InletPressureSlopeMegapascalsPerSecond)};";

    private static P1Contract LoadContract()
    {
        var path = Path.Combine(FindRepositoryRoot(), "eng", ContractFileName);
        return JsonSerializer.Deserialize<P1Contract>(File.ReadAllText(path))
            ?? throw new InvalidOperationException($"Could not deserialize {ContractFileName}.");
    }

    private static void ValidateP0PrerequisiteEvidence()
    {
        var path = Path.Combine(
            FindRepositoryRoot(),
            "eng",
            "frozen-evidence",
            "ordinary",
            "M10FinalReplacementLongClosurePlan1_P0Hotfix2_ValidatedSummary.txt");
        var text = File.ReadAllText(path);
        Assert.Contains("validator-hotfix=P0-HOTFIX2-PROJECT-MARKER-ALIGNMENT", text, StringComparison.Ordinal);
        Assert.Contains("production-src-changed=False", text, StringComparison.Ordinal);
        Assert.Contains("production-tests-changed=False", text, StringComparison.Ordinal);
        Assert.Contains("second-replacement-long-authorized=False", text, StringComparison.Ordinal);
        Assert.Contains("next-authorized-implementation=P1-Asymptotic-First-Stage-Qualification", text, StringComparison.Ordinal);
        Assert.Contains("m10-final-replacement-long-closure-plan1-p0-passes=True", text, StringComparison.Ordinal);
    }

    private static void ValidateContract(P1Contract contract)
    {
        Assert.Equal("m10-final-replacement-long-closure-plan1-p1-v1", contract.ContractId);
        Assert.Equal("P0-HOTFIX2-VALIDATED", contract.Baseline);
        Assert.Equal(100, contract.StepsPerSecond);
        Assert.True(contract.PrimaryHoldSeconds > contract.MinimumStationaryAssessmentSeconds);
        Assert.True(contract.ContinuationMaxTotalHoldSeconds >= contract.PrimaryHoldSeconds);
        Assert.True(contract.StationaryWindowSeconds >= contract.ConvergenceWindowSeconds);
        Assert.True(contract.NoiseTailSeconds >= contract.StationaryWindowSeconds);
        Assert.Contains("CONVERGED", contract.AllowedFinalClassifications);
        Assert.Contains("BIASED-STATIONARY", contract.AllowedFinalClassifications);
        Assert.Contains("INCONCLUSIVE", contract.AllowedFinalClassifications);
        Assert.Equal("STILL-CONVERGING", contract.PrimaryContinuationClassification);
        Assert.Contains("STILL-CONVERGING", contract.AllowedPrimaryClassifications);
        Assert.False(contract.RuntimeChangesAuthorized);
        Assert.False(contract.ReplacementWorkloadChangesAuthorized);
        Assert.False(contract.SecondReplacementLongAuthorized);
    }

    private static void RequireOptIn()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable(OptInEnvironmentVariable), "1", StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Set {OptInEnvironmentVariable}=1 to run M10 Final Replacement-Long Closure Plan 1 P1.");
        }
    }

    private static string ReportDirectory()
        => Path.Combine(FindRepositoryRoot(), "artifacts", "m10-final-replacement-long-closure-plan1-p1");

    private static void ResetDirectory(string directory)
    {
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }

        Directory.CreateDirectory(directory);
        File.WriteAllText(
            Path.Combine(directory, "00-progress.txt"),
            "M10 FINAL REPLACEMENT-LONG CLOSURE PLAN 1 P1 STARTED" + Environment.NewLine,
            Utf8WithoutBom);
    }

    private static void AppendProgress(string message)
        => File.AppendAllText(Path.Combine(ReportDirectory(), "00-progress.txt"), message + Environment.NewLine, Utf8WithoutBom);

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

    private static string Flatten(string value)
        => value.Replace('\r', ' ').Replace('\n', ' ');

    private static string F(double? value)
        => value.HasValue && double.IsFinite(value.Value)
            ? value.Value.ToString("G17", CultureInfo.InvariantCulture)
            : string.Empty;

    private static string F(double value)
        => double.IsFinite(value) ? value.ToString("G17", CultureInfo.InvariantCulture) : string.Empty;

    private static string I(long? value)
        => value?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;

    private static string Csv(string value)
        => '"' + value.Replace("\"", "\"\"", StringComparison.Ordinal) + '"';

    private sealed record ProbeDefinition(
        string Id,
        int ExactVersion,
        double LoadIncrementMegawatts,
        double TargetLoadMegawatts,
        bool AllowContinuation);

    private sealed record ProbeResult(
        string Id,
        int ExactVersion,
        double TargetLoadMegawatts,
        double LoadIncrementMegawatts,
        long? LoadCommandStep,
        int HoldExecutedSteps,
        string PrimaryClassification,
        string FinalClassification,
        bool ContinuationInvoked,
        string CompletionReason,
        long? FirstTripStep,
        string? FirstLatchedFunctionId,
        long? FirstLatchedFunctionStep,
        TailStatistics Tail,
        bool AllFinite,
        string? ExceptionType,
        string? ExceptionMessage)
    {
        public bool DiagnosticComplete => AllFinite && HoldExecutedSteps >= 0 && FinalClassification.Length > 0;
    }

    private sealed record TailStatistics(
        int RowCount,
        double MeanFrequencyHertz,
        double MeanOutputMegawatts,
        double OutputErrorMegawatts,
        double MeanShaftMegawatts,
        double MeanDispatchAdequacyMegawatts,
        double MeanNetAccelerationMegawatts,
        double MeanSteamFlowKilogramsPerSecond,
        double MeanInletPressureMegapascals,
        double OutputSlopeMegawattsPerSecond,
        double ShaftSlopeMegawattsPerSecond,
        double SteamFlowSlopeKilogramsPerSecondSquared,
        double InletPressureSlopeMegapascalsPerSecond);

    private sealed record NoiseCalibration(
        bool Valid,
        string ValidationMessage,
        int TailRows,
        double ReferenceOutputNoiseSlope,
        double ReferenceShaftNoiseSlope,
        double ReferenceSteamFlowNoiseSlope,
        double ReferenceInletPressureNoiseSlope,
        double OutputSlopeLimit,
        double ShaftSlopeLimit,
        double SteamFlowSlopeLimit,
        double InletPressureSlopeLimit,
        double ReferenceMeanOutputMegawatts,
        double ReferenceMeanShaftMegawatts,
        double ReferenceMeanSteamFlowKilogramsPerSecond,
        double ReferenceMeanFrequencyHertz,
        bool AllFinite,
        string? ExceptionType,
        string? ExceptionMessage);

    private sealed record ProbeEvent(
        string ProbeId,
        long LogicalStep,
        string EventKind,
        double? TargetLoadMegawatts,
        double? TargetThermalMegawatts,
        double? RequestedMegawatts,
        double? OutputMegawatts,
        double? ThermalMegawatts,
        double? ShaftMegawatts,
        double? FrequencyHertz,
        double? FrequencySlipHertz,
        double? NetAccelerationMegawatts,
        double? DispatchAdequacyMegawatts,
        double? FlowKilogramsPerSecond,
        double? InletPressureMegapascals,
        double? ControlValvePercentOpen);

    private sealed record ProbeSample(
        string ProbeId,
        long LogicalStep,
        double SimulatedSeconds,
        string Phase,
        double? TargetLoadMegawatts,
        double? TargetThermalMegawatts,
        double RequestedElectricalMegawatts,
        double ElectricalOutputMegawatts,
        double ReactorThermalMegawatts,
        double GeneratorMechanicalInputMegawatts,
        double TurbineShaftMegawatts,
        double PassiveMechanicalLossMegawatts,
        double NetRotorAccelerationPowerMegawatts,
        double RequestedMechanicalDispatchMegawatts,
        double DispatchMechanicalAdequacyMegawatts,
        double TurbineSteamFlowKilogramsPerSecond,
        double TurbineInletPressureMegapascals,
        double EffectiveSpecificWorkJoulesPerKilogram,
        double ControlValvePercentOpen,
        double ReliefMassFlowKilogramsPerSecond,
        double RotorRpm,
        double GeneratorFrequencyHertz,
        double GeneratorFrequencySlipHertz,
        double SignedPhaseLeadRadians,
        double PhaseCorrectionPowerMegawatts,
        double FrequencyCorrectionPowerMegawatts,
        double CommandedElectromagneticTorqueNewtonMetres,
        double EffectiveElectromagneticTorqueNewtonMetres,
        bool BreakerClosed,
        bool ReactorScram,
        bool TurbineTrip,
        bool GeneratorTrip)
    {
        public bool AnyTripActive => ReactorScram || TurbineTrip || GeneratorTrip;

        public bool AllFinite => double.IsFinite(SimulatedSeconds)
            && double.IsFinite(RequestedElectricalMegawatts)
            && double.IsFinite(ElectricalOutputMegawatts)
            && double.IsFinite(ReactorThermalMegawatts)
            && double.IsFinite(GeneratorMechanicalInputMegawatts)
            && double.IsFinite(TurbineShaftMegawatts)
            && double.IsFinite(PassiveMechanicalLossMegawatts)
            && double.IsFinite(NetRotorAccelerationPowerMegawatts)
            && double.IsFinite(RequestedMechanicalDispatchMegawatts)
            && double.IsFinite(DispatchMechanicalAdequacyMegawatts)
            && double.IsFinite(TurbineSteamFlowKilogramsPerSecond)
            && double.IsFinite(TurbineInletPressureMegapascals)
            && double.IsFinite(EffectiveSpecificWorkJoulesPerKilogram)
            && double.IsFinite(ControlValvePercentOpen)
            && double.IsFinite(ReliefMassFlowKilogramsPerSecond)
            && double.IsFinite(RotorRpm)
            && double.IsFinite(GeneratorFrequencyHertz)
            && double.IsFinite(GeneratorFrequencySlipHertz)
            && double.IsFinite(SignedPhaseLeadRadians)
            && double.IsFinite(PhaseCorrectionPowerMegawatts)
            && double.IsFinite(FrequencyCorrectionPowerMegawatts)
            && double.IsFinite(CommandedElectromagneticTorqueNewtonMetres)
            && double.IsFinite(EffectiveElectromagneticTorqueNewtonMetres);
    }

    private sealed record P1Contract(
        string ContractId,
        string Baseline,
        string Question,
        int StepsPerSecond,
        int NoiseControlSeconds,
        int NoiseTailSeconds,
        int NoiseSubwindowSeconds,
        double NoiseMultiplier,
        int PreparationTimeoutSeconds,
        int PrimaryHoldSeconds,
        int ContinuationMaxTotalHoldSeconds,
        string ContinuationProbeId,
        int ConvergenceWindowSeconds,
        int StationaryWindowSeconds,
        int MinimumStationaryAssessmentSeconds,
        double ThermalReadinessToleranceMegawatts,
        double FrequencyToleranceHertz,
        double OutputToleranceMegawatts,
        double NetAccelerationToleranceMegawatts,
        double DispatchAdequacyToleranceMegawatts,
        int TrajectorySampleIntervalSteps,
        P1SlopeBands SlopeBands,
        string[] AllowedFinalClassifications,
        string[] AllowedPrimaryClassifications,
        string PrimaryContinuationClassification,
        bool RuntimeChangesAuthorized,
        bool ReplacementWorkloadChangesAuthorized,
        bool SecondReplacementLongAuthorized);

    private sealed record P1SlopeBands(
        SlopeBand OutputMegawattsPerSecond,
        SlopeBand ShaftMegawattsPerSecond,
        SlopeBand SteamFlowKilogramsPerSecondSquared,
        SlopeBand InletPressureMegapascalsPerSecond);

    private sealed record SlopeBand(double Floor, double Ceiling);
}
