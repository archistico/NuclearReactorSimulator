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
/// P1A from M10 Final Replacement-Long Closure Plan 1, Plan Amendment 1.
/// It reuses the frozen P1 stationarity calibration, reproduces the returned P1 exact-v9 checkpoints,
/// and then extends only the 5.5 and 6 MWe exact-v9 holds to a hard 3,600 s ceiling.
/// It never selects P3 directly: all final evidence returns to P2R Decision Re-entry.
/// </summary>
public sealed class M10FinalReplacementLongClosurePlan1P1AAsymptoticClosureExtensionTests
{
    private const string OptInEnvironmentVariable = "NRS_M10_FINAL_REPLACEMENT_LONG_P1A";
    private const string ContractFileName = "m10-final-replacement-long-closure-plan1-p1a-contract.json";
    private static readonly UTF8Encoding Utf8WithoutBom = new(encoderShouldEmitUTF8Identifier: false);

    [Fact(Explicit = true)]
    [Trait("Category", "M10FinalReplacementLongClosurePlan1P1A")]
    public void ExactV9_AsymptoticClosureExtension_ReproducesP1AndReturnsToP2R()
    {
        RequireOptIn();
        ResetDirectory(ReportDirectory());

        var contract = LoadContract();
        ValidateContract(contract);
        ValidateP2PrerequisiteEvidence();
        ValidateFrozenP1Evidence(contract);

        var calibration = FrozenP1Calibration(contract);
        WriteCalibration(calibration);

        var trajectoryRows = new List<ProbeSample>();
        var events = new List<ProbeEvent>();

        ProbeDefinition[] probes =
        [
            new("exact-v9-asymptotic-5p5mwe", 9, 0.5d, 5.5d, contract.FivePointFiveMinimumCheckpointHoldSeconds),
            new("exact-v9-asymptotic-6mwe", 9, 1d, 6d, contract.SixPointZeroMinimumCheckpointHoldSeconds),
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
        Assert.All(results, result => Assert.Contains(result.FinalClassification, contract.AllowedFinalClassifications));
    }




    private static ProbeResult RunProbe(
        P1AContract contract,
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
        var maximumHoldSteps = checked(contract.MaxTotalHoldSeconds * contract.StepsPerSecond);
        var minimumCheckpointHoldSteps = checked(probe.MinimumCheckpointHoldSeconds * contract.StepsPerSecond);
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
        var extensionInvoked = false;
        var p1CheckpointsReproduced = true;
        var requiredCheckpointCount = contract.RequiredCheckpoints.Count(item =>
            string.Equals(item.ProbeId, probe.Id, StringComparison.Ordinal));
        var reproducedCheckpointCount = 0;
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
                    var expectedLoadCommandStep = contract.RequiredCheckpoints
                        .Where(item => string.Equals(item.ProbeId, probe.Id, StringComparison.Ordinal))
                        .Select(static item => item.ExpectedLoadCommandStep)
                        .Distinct()
                        .Single();
                    if (loadCommandStep.Value != expectedLoadCommandStep)
                    {
                        throw new InvalidOperationException(
                            $"P1 checkpoint reproduction failed for {probe.Id}: load-command step {loadCommandStep.Value} != expected {expectedLoadCommandStep}.");
                    }

                    engine.QueueOperatorCommand(new ControlRoomCommand(
                        ControlRoomCommandKind.GeneratorLoadRaise,
                        generatorId,
                        ControlRoomCommandTargetKind.Generator));
                    AddEvent(events, probe.Id, loadCommandStep.Value, "load-command", probe.TargetLoadMegawatts, targetThermalMegawatts, before);
                    completionReason = "p1-checkpoint-reproduction";
                    break;
                }

                engine.Step(ControlRoomRunState.Running);
                var sample = Capture(probe.Id, "prepare", probe.TargetLoadMegawatts, targetThermalMegawatts, engine, contract.StepsPerSecond);
                allFinite &= sample.AllFinite;
                MaybeAddTrajectory(trajectoryRows, sample, contract.TrajectorySampleIntervalSteps, force: false);
                CaptureTripAndLatch(engine, sample, ref firstTripStep, ref firstLatchedFunctionId, ref firstLatchedFunctionStep);
                if (sample.AnyTripActive)
                {
                    completionReason = "trip-during-preparation";
                    AddEvent(events, probe.Id, sample.LogicalStep, "trip", probe.TargetLoadMegawatts, targetThermalMegawatts, sample);
                    break;
                }
            }

            if (loadCommandStep.HasValue && firstTripStep is null)
            {
                while (holdExecutedSteps < maximumHoldSteps)
                {
                    engine.Step(ControlRoomRunState.Running);
                    holdExecutedSteps++;
                    var phase = holdExecutedSteps <= minimumCheckpointHoldSteps
                        ? "p1-checkpoint-reproduction"
                        : "p1a-extension";
                    var sample = Capture(probe.Id, phase, probe.TargetLoadMegawatts, targetThermalMegawatts, engine, contract.StepsPerSecond);
                    allFinite &= sample.AllFinite;
                    EnqueueTail(tail, sample, stationaryWindowSteps);
                    MaybeAddTrajectory(trajectoryRows, sample, contract.TrajectorySampleIntervalSteps, force: false);
                    CaptureTripAndLatch(engine, sample, ref firstTripStep, ref firstLatchedFunctionId, ref firstLatchedFunctionStep);

                    if (sample.AnyTripActive)
                    {
                        finalClassification = "INCONCLUSIVE";
                        completionReason = "trip-during-hold";
                        AddEvent(events, probe.Id, sample.LogicalStep, "trip", probe.TargetLoadMegawatts, targetThermalMegawatts, sample);
                        break;
                    }

                    foreach (var checkpoint in contract.RequiredCheckpoints.Where(item =>
                                 string.Equals(item.ProbeId, probe.Id, StringComparison.Ordinal)
                                 && item.HoldSeconds * contract.StepsPerSecond == holdExecutedSteps))
                    {
                        var matches = CheckpointMatches(sample, checkpoint, contract.CheckpointTolerances);
                        p1CheckpointsReproduced &= matches;
                        if (matches)
                        {
                            reproducedCheckpointCount++;
                        }

                        AddEvent(
                            events,
                            probe.Id,
                            sample.LogicalStep,
                            matches ? $"p1-checkpoint-{checkpoint.HoldSeconds}s-pass" : $"p1-checkpoint-{checkpoint.HoldSeconds}s-fail",
                            probe.TargetLoadMegawatts,
                            targetThermalMegawatts,
                            sample);
                        if (!matches)
                        {
                            throw new InvalidOperationException(CheckpointFailureMessage(sample, checkpoint, contract.CheckpointTolerances));
                        }
                    }

                    if (holdExecutedSteps == minimumCheckpointHoldSteps)
                    {
                        strictConsecutiveSteps = 0;
                        extensionInvoked = true;
                        completionReason = "p1a-extension";
                        AddEvent(events, probe.Id, sample.LogicalStep, "p1a-extension-start", probe.TargetLoadMegawatts, targetThermalMegawatts, sample);
                        continue;
                    }

                    if (holdExecutedSteps < minimumCheckpointHoldSteps)
                    {
                        continue;
                    }

                    strictConsecutiveSteps = IsStrictPoint(sample, probe.TargetLoadMegawatts, contract)
                        ? strictConsecutiveSteps + 1
                        : 0;

                    if (holdExecutedSteps % contract.StepsPerSecond != 0)
                    {
                        continue;
                    }

                    var window = tail.ToArray();
                    if (strictConsecutiveSteps >= convergenceWindowSteps
                        && HasConverged(window, convergenceWindowSteps, calibration))
                    {
                        finalClassification = "CONVERGED";
                        completionReason = "converged-during-p1a-extension";
                        AddEvent(events, probe.Id, sample.LogicalStep, "converged", probe.TargetLoadMegawatts, targetThermalMegawatts, sample);
                        break;
                    }

                    if (holdExecutedSteps >= minimumCheckpointHoldSteps + stationaryWindowSteps
                        && IsBiasedStationary(window, probe.TargetLoadMegawatts, contract, calibration))
                    {
                        finalClassification = "BIASED-STATIONARY";
                        completionReason = "biased-stationary-during-p1a-extension";
                        AddEvent(events, probe.Id, sample.LogicalStep, "biased-stationary", probe.TargetLoadMegawatts, targetThermalMegawatts, sample);
                        break;
                    }
                }

                if (holdExecutedSteps >= maximumHoldSteps && finalClassification == "INCONCLUSIVE")
                {
                    completionReason = "p1a-hard-horizon-inconclusive";
                    AddEvent(events, probe.Id, engine.LogicalStep, "p1a-hard-horizon", probe.TargetLoadMegawatts, targetThermalMegawatts, tail.LastOrDefault());
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
            "INCONCLUSIVE",
            finalClassification,
            extensionInvoked,
            completionReason,
            firstTripStep,
            firstLatchedFunctionId,
            firstLatchedFunctionStep,
            tailStatistics,
            allFinite && p1CheckpointsReproduced && reproducedCheckpointCount == requiredCheckpointCount,
            exceptionType,
            exceptionMessage);
    }

    private static bool CheckpointMatches(
        ProbeSample sample,
        P1Checkpoint checkpoint,
        CheckpointTolerances tolerances)
        => sample.LogicalStep == checkpoint.ExpectedLogicalStep
            && Math.Abs(sample.ElectricalOutputMegawatts - checkpoint.OutputMegawatts) <= tolerances.PowerMegawatts
            && Math.Abs(sample.ReactorThermalMegawatts - checkpoint.ThermalMegawatts) <= tolerances.ThermalMegawatts
            && Math.Abs(sample.TurbineShaftMegawatts - checkpoint.ShaftMegawatts) <= tolerances.PowerMegawatts
            && Math.Abs(sample.GeneratorFrequencyHertz - checkpoint.FrequencyHertz) <= tolerances.FrequencyHertz
            && Math.Abs(sample.DispatchMechanicalAdequacyMegawatts - checkpoint.DispatchAdequacyMegawatts) <= tolerances.PowerMegawatts
            && Math.Abs(sample.TurbineSteamFlowKilogramsPerSecond - checkpoint.FlowKilogramsPerSecond) <= tolerances.FlowKilogramsPerSecond
            && Math.Abs(sample.TurbineInletPressureMegapascals - checkpoint.InletPressureMegapascals) <= tolerances.PressureMegapascals;

    private static string CheckpointFailureMessage(
        ProbeSample sample,
        P1Checkpoint checkpoint,
        CheckpointTolerances tolerances)
        => $"P1 checkpoint reproduction failed for {checkpoint.ProbeId} at {checkpoint.HoldSeconds}s; "
            + $"step={sample.LogicalStep}/{checkpoint.ExpectedLogicalStep}; "
            + $"output={F(sample.ElectricalOutputMegawatts)}/{F(checkpoint.OutputMegawatts)} +/- {F(tolerances.PowerMegawatts)}; "
            + $"thermal={F(sample.ReactorThermalMegawatts)}/{F(checkpoint.ThermalMegawatts)} +/- {F(tolerances.ThermalMegawatts)}; "
            + $"shaft={F(sample.TurbineShaftMegawatts)}/{F(checkpoint.ShaftMegawatts)}; "
            + $"frequency={F(sample.GeneratorFrequencyHertz)}/{F(checkpoint.FrequencyHertz)} +/- {F(tolerances.FrequencyHertz)}; "
            + $"dispatch={F(sample.DispatchMechanicalAdequacyMegawatts)}/{F(checkpoint.DispatchAdequacyMegawatts)}; "
            + $"flow={F(sample.TurbineSteamFlowKilogramsPerSecond)}/{F(checkpoint.FlowKilogramsPerSecond)} +/- {F(tolerances.FlowKilogramsPerSecond)}; "
            + $"pressure={F(sample.TurbineInletPressureMegapascals)}/{F(checkpoint.InletPressureMegapascals)} +/- {F(tolerances.PressureMegapascals)}.";



    private static bool IsThermallyReady(ProbeSample sample, double targetThermalMegawatts, P1AContract contract)
        => !sample.AnyTripActive
            && sample.BreakerClosed
            && sample.ReactorThermalMegawatts >= targetThermalMegawatts - contract.ThermalReadinessToleranceMegawatts
            && Math.Abs(sample.GeneratorFrequencyHertz - 50d) <= 0.1d;

    private static bool IsStrictPoint(ProbeSample sample, double targetLoadMegawatts, P1AContract contract)
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
        P1AContract contract,
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
            _ => throw new ArgumentOutOfRangeException(nameof(exactVersion), exactVersion, "P1A authorizes exact-v9 only."),
        };

        if (Math.Abs(loadIncrementMegawatts - 5d) <= 1e-12d)
        {
            return baseline;
        }

        var solverField = typeof(IntegratedAutomaticOperationRuntimeEngine).GetField(
            "_solver",
            BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("P1A test-only runtime clone could not locate the private solver field.");
        var solver = solverField.GetValue(baseline) as IntegratedAutomaticOperationSolver
            ?? throw new InvalidOperationException("P1A test-only runtime clone could not read the integrated solver.");
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
            ?? throw new InvalidOperationException("P1A requires the canonical synchronous-grid coupling definition.");
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
        => $"probe-complete={result.Id}|primary:{result.PrimaryClassification}|final:{result.FinalClassification}|extension:{result.ContinuationInvoked}|hold-steps:{result.HoldExecutedSteps}|trip:{I(result.FirstTripStep)}|output-error:{F(result.Tail.OutputErrorMegawatts)}|dispatch:{F(result.Tail.MeanDispatchAdequacyMegawatts)}|output-slope:{F(result.Tail.OutputSlopeMegawattsPerSecond)}";

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
        File.WriteAllLines(Path.Combine(ReportDirectory(), "01-frozen-p1-calibration.csv"), lines, Utf8WithoutBom);
    }

    private static void WriteProbeSummary(IEnumerable<ProbeResult> results)
    {
        var lines = new List<string>
        {
            "probe_id,exact_version,target_load_mwe,load_increment_mwe,load_command_step,hold_executed_steps,primary_classification,final_classification,extension_invoked,completion_reason,first_trip_step,first_latched_function_id,first_latched_function_step,tail_rows,tail_mean_frequency_hz,tail_mean_output_mwe,tail_output_error_mwe,tail_mean_shaft_mw,tail_mean_dispatch_adequacy_mw,tail_mean_net_acceleration_mw,tail_mean_flow_kg_s,tail_mean_inlet_mpa,tail_output_slope_mw_s,tail_shaft_slope_mw_s,tail_flow_slope_kg_s2,tail_inlet_pressure_slope_mpa_s,all_finite,exception_type,exception_message"
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
        File.WriteAllLines(Path.Combine(ReportDirectory(), "02-p1a-probe-summary.csv"), lines, Utf8WithoutBom);
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
        File.WriteAllLines(Path.Combine(ReportDirectory(), "03-p1a-events.csv"), lines, Utf8WithoutBom);
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
        File.WriteAllLines(Path.Combine(ReportDirectory(), "04-p1a-trajectories.csv"), lines, Utf8WithoutBom);
    }

    private static void WriteDecisionSummary(
        P1AContract contract,
        NoiseCalibration calibration,
        IReadOnlyCollection<ProbeResult> results)
    {
        var v9Half = results.Single(static result => result.Id == "exact-v9-asymptotic-5p5mwe");
        var v9One = results.Single(static result => result.Id == "exact-v9-asymptotic-6mwe");
        var overallClassification = v9Half.FinalClassification == v9One.FinalClassification
            ? v9One.FinalClassification
            : "INCONCLUSIVE";

        var lines = new[]
        {
            "scope=M10 Final Replacement-Long Closure Plan 1 P1A Asymptotic Closure Extension; P2 Decision Gate 1 is validated; this gate changes no production src, replacement workload, authority policy, generator-load semantics, protection semantics, exact-v9 runtime or mission pack;",
            $"contract={contract.ContractId};baseline={contract.Baseline};",
            $"frozen-p1-calibration=output-limit:{F(calibration.OutputSlopeLimit)}|shaft-limit:{F(calibration.ShaftSlopeLimit)}|flow-limit:{F(calibration.SteamFlowSlopeLimit)}|pressure-limit:{F(calibration.InletPressureSlopeLimit)};",
            DecisionLine(v9Half),
            DecisionLine(v9One),
            $"p1a-final-classification={overallClassification};exact-v9-5p5-classification={v9Half.FinalClassification};exact-v9-6-classification={v9One.FinalClassification};p1a-max-total-hold-seconds={contract.MaxTotalHoldSeconds};",
            $"p1-checkpoints-reproduced={results.All(static result => result.AllFinite && result.ExceptionType is null)};exact-v4-rerun=False;further-automatic-continuation-authorized=False;",
            "production-src-changed=False;production-tests-changed=False;replacement-workload-changed=False;runtime-semantics-changed=False;authority-policy-changed=False;generator-load-semantics-changed=False;protection-semantics-changed=False;exact-v9-changed=False;mission-pack-changed=False;",
            "p3-w-authorized=False;p3-r-authorized=False;second-replacement-long-authorized=False;next-authorized-gate=P2R-Decision-Reentry;",
            "authorization=P1A evidence only; P2R must record the branch decision before P3; Replacement-Long Execution 1 remains RED; exact-v9 remains immutable inside P1A;",
            $"m10-final-replacement-long-closure-plan1-p1a-passes={results.All(static result => result.DiagnosticComplete && result.ExceptionType is null)}",
        };
        File.WriteAllLines(Path.Combine(ReportDirectory(), "05-p1a-decision-summary.txt"), lines, Utf8WithoutBom);
    }


    private static string DecisionLine(ProbeResult result)
        => $"probe={result.Id}|primary:{result.PrimaryClassification}|final:{result.FinalClassification}|extension:{result.ContinuationInvoked}|hold-steps:{result.HoldExecutedSteps}|trip:{I(result.FirstTripStep)}|latch:{result.FirstLatchedFunctionId ?? "none"}|tail-frequency:{F(result.Tail.MeanFrequencyHertz)}|tail-output:{F(result.Tail.MeanOutputMegawatts)}|output-error:{F(result.Tail.OutputErrorMegawatts)}|tail-shaft:{F(result.Tail.MeanShaftMegawatts)}|dispatch-adequacy:{F(result.Tail.MeanDispatchAdequacyMegawatts)}|output-slope:{F(result.Tail.OutputSlopeMegawattsPerSecond)}|shaft-slope:{F(result.Tail.ShaftSlopeMegawattsPerSecond)}|flow-slope:{F(result.Tail.SteamFlowSlopeKilogramsPerSecondSquared)}|pressure-slope:{F(result.Tail.InletPressureSlopeMegapascalsPerSecond)};";

    private static P1AContract LoadContract()
    {
        var path = Path.Combine(FindRepositoryRoot(), "eng", ContractFileName);
        return JsonSerializer.Deserialize<P1AContract>(File.ReadAllText(path))
            ?? throw new InvalidOperationException($"Could not deserialize {ContractFileName}.");
    }

    private static void ValidateP2PrerequisiteEvidence()
    {
        var path = Path.Combine(
            FindRepositoryRoot(),
            "eng",
            "frozen-evidence",
            "ordinary",
            "M10FinalReplacementLongClosurePlan1_P2_ValidatedSummary.txt");
        var text = File.ReadAllText(path);
        Assert.Contains("p1-final-classification=INCONCLUSIVE", text, StringComparison.Ordinal);
        Assert.Contains("p2-decision=PLAN-STOP-INCONCLUSIVE", text, StringComparison.Ordinal);
        Assert.Contains("p3-w-authorized=False", text, StringComparison.Ordinal);
        Assert.Contains("p3-r-authorized=False", text, StringComparison.Ordinal);
        Assert.Contains("plan-amendment-1=P1A-ASYMPTOTIC-CLOSURE-EXTENSION", text, StringComparison.Ordinal);
        Assert.Contains("p1a-max-total-hold-seconds=3600", text, StringComparison.Ordinal);
        Assert.Contains("production-src-changed=False", text, StringComparison.Ordinal);
        Assert.Contains("production-tests-changed=False", text, StringComparison.Ordinal);
        Assert.Contains("second-replacement-long-authorized=False", text, StringComparison.Ordinal);
        Assert.Contains("next-authorized-implementation=P1A-Asymptotic-Closure-Extension", text, StringComparison.Ordinal);
        Assert.Contains("m10-final-replacement-long-closure-plan1-p2-passes=True", text, StringComparison.Ordinal);
    }

    private static void ValidateFrozenP1Evidence(P1AContract contract)
    {
        var root = Path.Combine(FindRepositoryRoot(), "eng", "frozen-evidence", "ordinary");
        var decision = File.ReadAllText(Path.Combine(root, "M10FinalReplacementLongClosurePlan1_P1_DecisionSummary.txt"));
        var calibration = File.ReadAllText(Path.Combine(root, "M10FinalReplacementLongClosurePlan1_P1_ReferenceNoiseCalibration.csv"));
        var probes = File.ReadAllText(Path.Combine(root, "M10FinalReplacementLongClosurePlan1_P1_ProbeSummary.csv"));

        Assert.Contains("p1-final-classification=INCONCLUSIVE", decision, StringComparison.Ordinal);
        Assert.Contains("bounded-continuation-invoked=True", decision, StringComparison.Ordinal);
        Assert.Contains("m10-final-replacement-long-closure-plan1-p1-passes=True", decision, StringComparison.Ordinal);
        Assert.Contains("exact-v9-asymptotic-5p5mwe", probes, StringComparison.Ordinal);
        Assert.Contains("exact-v9-asymptotic-6mwe", probes, StringComparison.Ordinal);
        Assert.Contains(F(contract.FrozenP1Calibration.OutputSlopeLimit), calibration, StringComparison.Ordinal);
        Assert.Contains(F(contract.FrozenP1Calibration.ShaftSlopeLimit), calibration, StringComparison.Ordinal);
        Assert.Contains(F(contract.FrozenP1Calibration.SteamFlowSlopeLimit), calibration, StringComparison.Ordinal);
        Assert.Contains(F(contract.FrozenP1Calibration.InletPressureSlopeLimit), calibration, StringComparison.Ordinal);
    }

    private static NoiseCalibration FrozenP1Calibration(P1AContract contract)
        => new(
            true,
            "frozen P1 reference-derived stationarity calibration",
            contract.NoiseTailSeconds * contract.StepsPerSecond,
            contract.FrozenP1Calibration.ReferenceOutputNoiseSlope,
            contract.FrozenP1Calibration.ReferenceShaftNoiseSlope,
            contract.FrozenP1Calibration.ReferenceSteamFlowNoiseSlope,
            contract.FrozenP1Calibration.ReferenceInletPressureNoiseSlope,
            contract.FrozenP1Calibration.OutputSlopeLimit,
            contract.FrozenP1Calibration.ShaftSlopeLimit,
            contract.FrozenP1Calibration.SteamFlowSlopeLimit,
            contract.FrozenP1Calibration.InletPressureSlopeLimit,
            contract.FrozenP1Calibration.ReferenceMeanOutputMegawatts,
            contract.FrozenP1Calibration.ReferenceMeanShaftMegawatts,
            contract.FrozenP1Calibration.ReferenceMeanSteamFlowKilogramsPerSecond,
            contract.FrozenP1Calibration.ReferenceMeanFrequencyHertz,
            true,
            null,
            null);

    private static void ValidateContract(P1AContract contract)
    {
        Assert.Equal("m10-final-replacement-long-closure-plan1-p1a-v1", contract.ContractId);
        Assert.Equal("P2-DECISION-GATE1-VALIDATED", contract.Baseline);
        Assert.Equal(100, contract.StepsPerSecond);
        Assert.Equal(3600, contract.MaxTotalHoldSeconds);
        Assert.Equal(900, contract.FivePointFiveMinimumCheckpointHoldSeconds);
        Assert.Equal(1800, contract.SixPointZeroMinimumCheckpointHoldSeconds);
        Assert.True(contract.StationaryWindowSeconds >= contract.ConvergenceWindowSeconds);
        Assert.Equal(3, contract.RequiredCheckpoints.Length);
        Assert.Contains("CONVERGED", contract.AllowedFinalClassifications);
        Assert.Contains("BIASED-STATIONARY", contract.AllowedFinalClassifications);
        Assert.Contains("INCONCLUSIVE", contract.AllowedFinalClassifications);
        Assert.False(contract.RuntimeChangesAuthorized);
        Assert.False(contract.ReplacementWorkloadChangesAuthorized);
        Assert.False(contract.ExactV4RerunAuthorized);
        Assert.False(contract.FurtherAutomaticContinuationAuthorized);
        Assert.False(contract.SecondReplacementLongAuthorized);
    }




    private static void RequireOptIn()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable(OptInEnvironmentVariable), "1", StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Set {OptInEnvironmentVariable}=1 to run M10 Final Replacement-Long Closure Plan 1 P1A.");
        }
    }

    private static string ReportDirectory()
        => Path.Combine(FindRepositoryRoot(), "artifacts", "m10-final-replacement-long-closure-plan1-p1a");

    private static void ResetDirectory(string directory)
    {
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }

        Directory.CreateDirectory(directory);
        File.WriteAllText(
            Path.Combine(directory, "00-progress.txt"),
            "M10 FINAL REPLACEMENT-LONG CLOSURE PLAN 1 P1A STARTED" + Environment.NewLine,
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
        int MinimumCheckpointHoldSeconds);

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

    private sealed record P1AContract(
        string ContractId,
        string Baseline,
        string Question,
        int StepsPerSecond,
        int NoiseTailSeconds,
        int PreparationTimeoutSeconds,
        int MaxTotalHoldSeconds,
        int FivePointFiveMinimumCheckpointHoldSeconds,
        int SixPointZeroMinimumCheckpointHoldSeconds,
        int ConvergenceWindowSeconds,
        int StationaryWindowSeconds,
        double ThermalReadinessToleranceMegawatts,
        double FrequencyToleranceHertz,
        double OutputToleranceMegawatts,
        double NetAccelerationToleranceMegawatts,
        double DispatchAdequacyToleranceMegawatts,
        int TrajectorySampleIntervalSteps,
        FrozenCalibrationContract FrozenP1Calibration,
        CheckpointTolerances CheckpointTolerances,
        P1Checkpoint[] RequiredCheckpoints,
        string[] AllowedFinalClassifications,
        bool RuntimeChangesAuthorized,
        bool ReplacementWorkloadChangesAuthorized,
        bool ExactV4RerunAuthorized,
        bool FurtherAutomaticContinuationAuthorized,
        bool SecondReplacementLongAuthorized);

    private sealed record FrozenCalibrationContract(
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
        double ReferenceMeanFrequencyHertz);

    private sealed record CheckpointTolerances(
        double PowerMegawatts,
        double ThermalMegawatts,
        double FrequencyHertz,
        double FlowKilogramsPerSecond,
        double PressureMegapascals);

    private sealed record P1Checkpoint(
        string ProbeId,
        int HoldSeconds,
        long ExpectedLoadCommandStep,
        long ExpectedLogicalStep,
        double OutputMegawatts,
        double ThermalMegawatts,
        double ShaftMegawatts,
        double FrequencyHertz,
        double DispatchAdequacyMegawatts,
        double FlowKilogramsPerSecond,
        double InletPressureMegapascals);


}
