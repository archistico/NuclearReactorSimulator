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
/// P1B from M10 Final Replacement-Long Closure Plan 1 / Plan Amendment 2.
/// This is an observation-only owner-localization gate. It repeats the already-demonstrated exact-v9 5->6 MWe
/// path at the same 3,600 s ceiling, adds a 600 s 5 MWe background reference, reproduces all P1A checkpoints,
/// and emits canonical inventory/control/hydraulic/steam/turbine/generator evidence. It cannot select P3.
/// </summary>
public sealed class M10FinalReplacementLongClosurePlan1P1BSlowStateOwnerQualificationTests
{
    private const string OptInEnvironmentVariable = "NRS_M10_FINAL_REPLACEMENT_LONG_P1B";
    private const string ContractFileName = "m10-final-replacement-long-closure-plan1-p1b-contract.json";
    private static readonly UTF8Encoding Utf8WithoutBom = new(encoderShouldEmitUTF8Identifier: false);

    [Fact(Explicit = true)]
    [Trait("Category", "M10FinalReplacementLongClosurePlan1P1B")]
    public void ExactV9_SlowStateClosureAndPhenomenonOwnerQualification_ReturnsEvidenceToP2R2()
    {
        RequireOptIn();
        ResetDirectory(ReportDirectory());

        var contract = LoadContract();
        ValidateContract(contract);
        ValidatePlanningPrerequisiteEvidence();

        var evidence = new EvidenceStore();
        AppendProgress("background-reference-start=exact-v9-5mwe-600s");
        var background = RunBackgroundReference(contract, evidence);
        AppendProgress($"background-reference-complete=execution-pass:{background.ExecutionPass}");

        AppendProgress("load-probe-start=exact-v9-5-to-6mwe");
        var probe = RunLoadProbe(contract, evidence);
        AppendProgress($"load-probe-complete=execution-pass:{probe.ExecutionPass}|checkpoints:{probe.ReproducedCheckpointCount}/{contract.RequiredCheckpoints.Length}");

        var trends = BuildTrendEvidence(contract, evidence.WholeSamples);
        var ownerDomain = ClassifyOwnerDomain(contract, background, probe, trends);

        WriteBackgroundSummary(contract, background, evidence.WholeSamples, trends);
        WriteCheckpointTable(contract, probe);
        WriteWholeTrajectory(evidence.WholeSamples);
        WriteFluidNodeEvidence(evidence.NodeSamples);
        WriteThermalBodyEvidence(evidence.ThermalBodySamples);
        WritePrimaryDrumEvidence(evidence.PrimaryDrumSamples);
        WriteBranchEvidence(evidence.BranchSamples);
        WriteControllerEvidence(evidence.ControllerSamples);
        WriteActuatorEvidence(evidence.ActuatorSamples);
        WriteTurbineGeneratorEvidence(evidence.TurbineGeneratorSamples);
        WriteTrendEvidence(trends);
        WriteEvents(evidence.Events, background.Sentinels, probe.Sentinels);
        WriteEngineeringSummary(contract, background, probe, ownerDomain, trends);

        Assert.True(background.ExecutionPass, background.FailureMessage);
        Assert.True(probe.ExecutionPass, probe.FailureMessage);
        Assert.Equal(contract.RequiredCheckpoints.Length, probe.ReproducedCheckpointCount);
        Assert.Contains(ownerDomain, contract.AllowedOwnerDomains);
    }

    private static RunResult RunBackgroundReference(P1BContract contract, EvidenceStore evidence)
    {
        var engine = CreateEngine(loadIncrementMegawatts: 5d);
        engine.RequestPlantControlAuthority(PlantControlAuthorityMode.SupervisoryAutomatic);
        engine.RequestSupervisoryObjective(SupervisoryObjectiveRequest.HoldCurrentOperatingPoint());

        var totalSteps = checked(contract.BackgroundReferenceSeconds * contract.StepsPerSecond);
        var sampleIntervalSteps = checked(contract.TrajectorySampleSeconds * contract.StepsPerSecond);
        var sentinels = new SentinelSummary("background-reference");
        var executionPass = true;
        string failureMessage = string.Empty;

        try
        {
            CaptureObservation("background-reference", 0d, engine, evidence);
            for (var step = 1; step <= totalSteps; step++)
            {
                engine.Step(ControlRoomRunState.Running);
                AuditPerStepSentinels(engine, sentinels, evidence.Events, "background-reference", step / (double)contract.StepsPerSecond);
                if (sentinels.AnyTripOrNumericalFailure)
                {
                    executionPass = false;
                    failureMessage = "Background reference encountered a protection or numerical sentinel failure.";
                    break;
                }

                if (step % sampleIntervalSteps == 0)
                {
                    CaptureObservation("background-reference", step / (double)contract.StepsPerSecond, engine, evidence);
                }
            }
        }
        catch (Exception exception)
        {
            executionPass = false;
            failureMessage = $"{exception.GetType().FullName}: {Flatten(exception.Message)}";
            evidence.Events.Add(new EventSample("background-reference", engine.LogicalStep, engine.LogicalStep / (double)contract.StepsPerSecond, "exception", failureMessage));
        }

        return new RunResult("background-reference", executionPass, failureMessage, null, 0, sentinels);
    }

    private static RunResult RunLoadProbe(P1BContract contract, EvidenceStore evidence)
    {
        var engine = CreateEngine(contract.LoadIncrementMegawatts);
        engine.RequestPlantControlAuthority(PlantControlAuthorityMode.SupervisoryAutomatic);

        var presentation = engine.CreatePresentationSnapshot(ControlRoomRunState.Paused);
        var generatorId = Assert.Single(presentation.Electrical.Generators).GeneratorId;
        var initialRequestedMegawatts = Assert.Single(presentation.Electrical.Generators).RequestedElectricalPower.NumericValue
            ?? throw new InvalidOperationException("Initial generator requested load is unavailable.");
        var initialThermalMegawatts = presentation.ReactorCore.ReactorThermalPower.NumericValue
            ?? throw new InvalidOperationException("Initial reactor thermal power is unavailable.");
        var targetThermalMegawatts = initialThermalMegawatts / initialRequestedMegawatts * contract.TargetLoadMegawatts;
        var preparationTimeoutSteps = checked(contract.PreparationTimeoutSeconds * contract.StepsPerSecond);
        var maximumHoldSteps = checked(contract.MaximumHoldSecondsAfterLoad * contract.StepsPerSecond);
        var sampleIntervalSteps = checked(contract.TrajectorySampleSeconds * contract.StepsPerSecond);
        var sentinels = new SentinelSummary("exact-v9-5-to-6mwe");
        var checkpointResults = new List<CheckpointResult>();
        long? loadCommandStep = null;
        var executionPass = true;
        string failureMessage = string.Empty;

        try
        {
            engine.RequestSupervisoryObjective(SupervisoryObjectiveRequest.HoldReactorPower(targetThermalMegawatts * 1_000_000d));
            evidence.Events.Add(new EventSample("exact-v9-5-to-6mwe", engine.LogicalStep, 0d, "prepare-start", F(targetThermalMegawatts)));

            for (var preparationIndex = 0; preparationIndex < preparationTimeoutSteps; preparationIndex++)
            {
                var sample = CaptureWhole("exact-v9-5-to-6mwe", 0d, engine);
                if (IsThermallyReady(sample, targetThermalMegawatts, contract))
                {
                    loadCommandStep = engine.LogicalStep + 1;
                    if (loadCommandStep.Value != contract.ExpectedLoadCommandLogicalStep)
                    {
                        executionPass = false;
                        failureMessage = $"P1B load-command step {loadCommandStep.Value} != expected {contract.ExpectedLoadCommandLogicalStep}.";
                        break;
                    }

                    engine.QueueOperatorCommand(new ControlRoomCommand(
                        ControlRoomCommandKind.GeneratorLoadRaise,
                        generatorId,
                        ControlRoomCommandTargetKind.Generator));
                    evidence.Events.Add(new EventSample("exact-v9-5-to-6mwe", loadCommandStep.Value, 0d, "load-command", $"target-load-mwe={F(contract.TargetLoadMegawatts)};target-thermal-mw={F(targetThermalMegawatts)}"));
                    break;
                }

                engine.Step(ControlRoomRunState.Running);
                AuditPerStepSentinels(engine, sentinels, evidence.Events, "exact-v9-5-to-6mwe", 0d);
                if (sentinels.AnyTripOrNumericalFailure)
                {
                    executionPass = false;
                    failureMessage = "Load probe encountered a protection or numerical sentinel failure during preparation.";
                    break;
                }
            }

            if (!loadCommandStep.HasValue && executionPass)
            {
                executionPass = false;
                failureMessage = "P1B thermal preparation timed out before the load command.";
            }

            if (executionPass)
            {
                for (var holdStep = 1; holdStep <= maximumHoldSteps; holdStep++)
                {
                    engine.Step(ControlRoomRunState.Running);
                    var holdSeconds = holdStep / (double)contract.StepsPerSecond;
                    AuditPerStepSentinels(engine, sentinels, evidence.Events, "exact-v9-5-to-6mwe", holdSeconds);
                    if (sentinels.AnyTripOrNumericalFailure)
                    {
                        executionPass = false;
                        failureMessage = "Load probe encountered a protection or numerical sentinel failure during hold.";
                        break;
                    }

                    var checkpoint = contract.RequiredCheckpoints.FirstOrDefault(item => item.HoldSeconds * contract.StepsPerSecond == holdStep);
                    var sampleDue = holdStep % sampleIntervalSteps == 0;
                    if (sampleDue || checkpoint is not null)
                    {
                        CaptureObservation("exact-v9-5-to-6mwe", holdSeconds, engine, evidence);
                    }

                    if (checkpoint is not null)
                    {
                        var sample = evidence.WholeSamples[^1];
                        var matches = CheckpointMatches(sample, checkpoint, contract.CheckpointTolerances);
                        checkpointResults.Add(new CheckpointResult(checkpoint.HoldSeconds, sample.LogicalStep, matches, sample));
                        evidence.Events.Add(new EventSample(
                            "exact-v9-5-to-6mwe",
                            sample.LogicalStep,
                            holdSeconds,
                            matches ? "p1a-checkpoint-pass" : "p1a-checkpoint-fail",
                            CheckpointDetail(sample, checkpoint, contract.CheckpointTolerances)));
                        if (!matches)
                        {
                            executionPass = false;
                            failureMessage = "P1A checkpoint reproduction mismatch; owner localization is invalid.";
                            break;
                        }
                    }
                }
            }
        }
        catch (Exception exception)
        {
            executionPass = false;
            failureMessage = $"{exception.GetType().FullName}: {Flatten(exception.Message)}";
            evidence.Events.Add(new EventSample("exact-v9-5-to-6mwe", engine.LogicalStep, 0d, "exception", failureMessage));
        }

        if (checkpointResults.Count != contract.RequiredCheckpoints.Length)
        {
            executionPass = false;
            if (string.IsNullOrEmpty(failureMessage))
            {
                failureMessage = $"Only {checkpointResults.Count}/{contract.RequiredCheckpoints.Length} required P1A checkpoints were reproduced.";
            }
        }

        return new RunResult("exact-v9-5-to-6mwe", executionPass, failureMessage, loadCommandStep, checkpointResults.Count, sentinels, checkpointResults.ToArray());
    }

    private static void CaptureObservation(string probeId, double elapsedSeconds, IntegratedAutomaticOperationRuntimeEngine engine, EvidenceStore evidence)
    {
        var whole = CaptureWhole(probeId, elapsedSeconds, engine);
        if (!whole.AllFinite)
        {
            throw new InvalidOperationException($"Non-finite required P1B whole-domain state at step {whole.LogicalStep}.");
        }
        evidence.WholeSamples.Add(whole);

        var protectedControl = engine.LatestCanonicalSnapshot.Control.ProtectedControl;
        var fullPlant = protectedControl.FullPlant;
        var cycle = fullPlant.IntegratedCycle;
        var primary = cycle.PrimaryCircuit;
        var plant = fullPlant.CandidatePlant;
        var drum = Assert.Single(primary.SteamDrums.Drums);

        foreach (var node in plant.FluidNodes.OrderBy(static item => item.Id, StringComparer.Ordinal))
        {
            var row = new NodeSample(
                probeId, whole.LogicalStep, elapsedSeconds, node.Id,
                node.Mass.Kilograms, node.InternalEnergy.Joules, node.SpecificInternalEnergy.JoulesPerKilogram,
                node.Pressure.Pascals, node.Temperature.DegreesCelsius, node.Density.KilogramsPerCubicMetre,
                node.Phase.ToString(), node.VaporQuality?.Fraction);
            if (!row.AllFinite) throw new InvalidOperationException($"Non-finite node state '{node.Id}' at step {whole.LogicalStep}.");
            evidence.NodeSamples.Add(row);
        }

        foreach (var body in plant.ThermalBodies.OrderBy(static item => item.Id, StringComparer.Ordinal))
        {
            var row = new ThermalBodySample(probeId, whole.LogicalStep, elapsedSeconds, body.Id, body.StoredThermalEnergy.Joules, body.Temperature.DegreesCelsius);
            if (!row.AllFinite) throw new InvalidOperationException($"Non-finite thermal body '{body.Id}' at step {whole.LogicalStep}.");
            evidence.ThermalBodySamples.Add(row);
        }

        evidence.PrimaryDrumSamples.Add(new PrimaryDrumSample(
            probeId, whole.LogicalStep, elapsedSeconds,
            primary.TotalPlantMass.Kilograms, primary.TotalStoredEnergy.Joules,
            primary.TotalFeedwaterMassFlowRate.KilogramsPerSecond, primary.TotalSteamExportMassFlowRate.KilogramsPerSecond,
            primary.Audit.BalanceMassRateResidualKilogramsPerSecond, primary.Audit.MassClosureResidualKilograms,
            primary.Audit.BalancePowerResidualWatts, primary.Audit.EnergyClosureResidualJoules,
            drum.InventoryMass.Kilograms, drum.InventoryInternalEnergy.Joules, drum.Pressure.Pascals,
            drum.LiquidLevelFraction.Fraction, drum.IncomingReturnMassFlowRate.KilogramsPerSecond,
            drum.SeparatedSteamMassFlowRate.KilogramsPerSecond, drum.RecirculatedLiquidMassFlowRate.KilogramsPerSecond,
            drum.SeparableLiquidInventoryMass.Kilograms, drum.SeparationMassResidualKilogramsPerSecond,
            drum.SeparationEnergyResidualWatts, drum.SteamSourcePressureDrivenCapacityMassFlowRate.KilogramsPerSecond,
            drum.SteamSourceAvailableMassFlowRate.KilogramsPerSecond, drum.SteamSourcePressureLimited,
            drum.SteamSourceAvailabilityLimited));

        foreach (var loop in primary.MainCirculation.Loops)
        {
            foreach (var branch in loop.Branches)
            {
                evidence.BranchSamples.Add(new BranchSample(
                    probeId, whole.LogicalStep, elapsedSeconds, loop.LoopId, branch.FuelChannelGroupId,
                    loop.SuctionHeaderPressure.Pascals, loop.PressureHeaderPressure.Pascals, loop.HeaderPressureRise.Pascals,
                    loop.TotalPumpMassFlowRate.KilogramsPerSecond, loop.TotalChannelMassFlowRate.KilogramsPerSecond,
                    loop.TotalReturnMassFlowRate.KilogramsPerSecond, loop.PumpToChannelContinuityResidual.KilogramsPerSecond,
                    loop.ChannelToReturnContinuityResidual.KilogramsPerSecond,
                    branch.ChannelMassFlowRate.KilogramsPerSecond, branch.ReturnMassFlowRate.KilogramsPerSecond,
                    branch.PerChannelMassFlowRate.KilogramsPerSecond, branch.BranchContinuityResidual.KilogramsPerSecond,
                    branch.ChannelPressureDifference.Pascals, branch.ReturnPressureDifference.Pascals,
                    branch.OutletPhase.ToString(), branch.OutletVaporQuality?.Fraction, branch.OutletVoidFraction?.Fraction));
            }
        }

        CaptureControllers(probeId, whole.LogicalStep, elapsedSeconds, "reactor-primary", protectedControl.ReactorPrimary.ControlAndActuator, plant, evidence);
        CaptureControllers(probeId, whole.LogicalStep, elapsedSeconds, "turbine-secondary", protectedControl.TurbineSecondary.ControlAndActuator, plant, evidence);

        var turbine = cycle.TurbineExpansion;
        var stage = Assert.Single(turbine.StageGroups);
        var rotor = Assert.Single(turbine.Rotors);
        var generator = Assert.Single(cycle.Generators);
        var train = Assert.Single(turbine.MainSteamNetwork.AdmissionTrains);
        var heat = cycle.HeatBalance;
        var generatorDefinition = cycle.Definition.GeneratorGridSystem.GetGenerator(generator.GeneratorId);
        var requestedMechanical = generator.RequestedElectricalPower.Megawatts / generatorDefinition.Efficiency.Fraction;
        var dispatchAdequacy = rotor.ShaftPower.Megawatts - rotor.PassiveMechanicalLossPower.Megawatts - requestedMechanical;
        var netAcceleration = rotor.ShaftPower.Megawatts - rotor.ExternalLoadPower.Megawatts - rotor.PassiveMechanicalLossPower.Megawatts;
        evidence.TurbineGeneratorSamples.Add(new TurbineGeneratorSample(
            probeId, whole.LogicalStep, elapsedSeconds,
            turbine.MainSteamNetwork.TotalSteamLineMassFlowRate.KilogramsPerSecond,
            turbine.MainSteamNetwork.TotalTurbineAdmissionMassFlowRate.KilogramsPerSecond,
            turbine.MainSteamNetwork.TotalReliefMassFlowRate.KilogramsPerSecond,
            stage.CommandedMassFlowRate.KilogramsPerSecond, stage.EffectiveMassFlowRate.KilogramsPerSecond,
            stage.MoistureDrainMassFlowRate.KilogramsPerSecond, stage.TotalTransferredMassFlowRate.KilogramsPerSecond,
            stage.InletPressure.Pascals, stage.InletTemperature.DegreesCelsius, stage.EffectiveIdealSpecificWork.JoulesPerKilogram,
            train.StopValve.EffectivePosition.Percent, train.ControlValve.EffectivePosition.Percent, train.AdmissionValve.EffectivePosition.Percent,
            train.StopToControlContinuityResidual.KilogramsPerSecond, train.ControlToAdmissionContinuityResidual.KilogramsPerSecond,
            rotor.ShaftPower.Megawatts, rotor.PassiveMechanicalLossPower.Megawatts, netAcceleration,
            rotor.FinalKineticEnergy.Joules, rotor.NetTorque.NewtonMetres,
            generator.RequestedElectricalPower.Megawatts, generator.ElectricalOutputPower.Megawatts,
            generator.FinalElectricalFrequency.Hertz, whole.GeneratorFrequencySlipHertz, whole.SignedPhaseLeadRadians,
            generator.CommandedElectromagneticTorque.NewtonMetres, generator.EffectiveElectromagneticTorque.NewtonMetres,
            dispatchAdequacy, heat.NuclearHeatInputPower.Megawatts, heat.NetReactorToGridExternalPower.Megawatts,
            heat.CoupledStoredEnergyChange.Joules, heat.FullEnergyPathClosureResidualJoules));
    }

    private static WholeSample CaptureWhole(string probeId, double elapsedSeconds, IntegratedAutomaticOperationRuntimeEngine engine)
    {
        var protectedControl = engine.LatestCanonicalSnapshot.Control.ProtectedControl;
        var fullPlant = protectedControl.FullPlant;
        var cycle = fullPlant.IntegratedCycle;
        var primary = cycle.PrimaryCircuit;
        var plant = fullPlant.CandidatePlant;
        var reactor = protectedControl.ReactorPrimary;
        var turbine = cycle.TurbineExpansion;
        var stage = Assert.Single(turbine.StageGroups);
        var rotor = Assert.Single(turbine.Rotors);
        var generator = Assert.Single(cycle.Generators);
        var grid = cycle.GeneratorGrid.Grid;
        var train = Assert.Single(turbine.MainSteamNetwork.AdmissionTrains);
        var drum = Assert.Single(primary.SteamDrums.Drums);
        var heat = cycle.HeatBalance;
        var generatorDefinition = cycle.Definition.GeneratorGridSystem.GetGenerator(generator.GeneratorId);
        var coupling = generatorDefinition.GridCoupling ?? throw new InvalidOperationException("P1B requires canonical synchronous-grid coupling.");
        var signedPhaseLead = SignedShortestPhaseLeadRadians(generator.FinalElectricalPhaseAngle.Radians, grid.FinalPhaseAngle.Radians);
        var frequencySlip = generator.FinalElectricalFrequency.Hertz - grid.Frequency.Hertz;
        var phaseCorrection = coupling.MaximumSynchronizingCorrectionPower.Megawatts * Math.Sin(signedPhaseLead);
        var frequencyCorrection = coupling.FrequencyDampingPowerAtOneHertzSlip.Megawatts * frequencySlip;
        var requestedMechanical = generator.RequestedElectricalPower.Megawatts / generatorDefinition.Efficiency.Fraction;
        var dispatchAdequacy = rotor.ShaftPower.Megawatts - rotor.PassiveMechanicalLossPower.Megawatts - requestedMechanical;
        var netAcceleration = rotor.ShaftPower.Megawatts - rotor.ExternalLoadPower.Megawatts - rotor.PassiveMechanicalLossPower.Megawatts;
        var thermalBodyEnergy = plant.ThermalBodies.Sum(static item => item.StoredThermalEnergy.Joules);
        var coupledStoredEnergy = heat.FinalThermofluidStoredEnergy.Joules + heat.FinalRotorKineticEnergy.Joules;
        var flowController = protectedControl.ReactorPrimary.ControlAndActuator.Controllers.GetDiagnostic("flow-control");
        var speedController = protectedControl.TurbineSecondary.ControlAndActuator.Controllers.GetDiagnostic("speed-control");
        var levelController = protectedControl.TurbineSecondary.ControlAndActuator.Controllers.GetDiagnostic("level-control");
        var hotwellController = protectedControl.TurbineSecondary.ControlAndActuator.Controllers.GetDiagnostic("hotwell-control");

        return new WholeSample(
            probeId, engine.LogicalStep, elapsedSeconds,
            reactor.TotalReactivityUsed.DeltaKOverK, reactor.NonRodReactivity.DeltaKOverK,
            reactor.CandidateRodReactivity.Total.DeltaKOverK, reactor.PointKinetics.NeutronPopulation.Relative,
            reactor.FissionPower.TotalFissionThermalPower.Megawatts, fullPlant.ReactorThermalPower.Megawatts,
            primary.TotalNuclearHeatPower.Megawatts,
            primary.TotalPlantMass.Kilograms, primary.TotalStoredEnergy.Joules, thermalBodyEnergy, coupledStoredEnergy,
            primary.TotalFeedwaterMassFlowRate.KilogramsPerSecond, primary.TotalSteamExportMassFlowRate.KilogramsPerSecond,
            drum.InventoryMass.Kilograms, drum.InventoryInternalEnergy.Joules, drum.LiquidLevelFraction.Fraction,
            primary.MainCirculation.TotalPumpMassFlowRate.KilogramsPerSecond,
            primary.MainCirculation.TotalChannelMassFlowRate.KilogramsPerSecond,
            primary.MainCirculation.TotalReturnMassFlowRate.KilogramsPerSecond,
            stage.EffectiveMassFlowRate.KilogramsPerSecond, stage.InletPressure.Megapascals,
            train.ControlValve.EffectivePosition.Percent,
            rotor.ShaftPower.Megawatts, rotor.PassiveMechanicalLossPower.Megawatts, netAcceleration, rotor.FinalKineticEnergy.Joules,
            generator.RequestedElectricalPower.Megawatts, generator.ElectricalOutputPower.Megawatts,
            generator.FinalElectricalFrequency.Hertz, frequencySlip, signedPhaseLead, phaseCorrection, frequencyCorrection,
            generator.CommandedElectromagneticTorque.NewtonMetres, generator.EffectiveElectromagneticTorque.NewtonMetres,
            dispatchAdequacy,
            flowController.IntegralTerm, flowController.Output,
            speedController.IntegralTerm, speedController.Output,
            levelController.IntegralTerm, levelController.Output,
            hotwellController.IntegralTerm, hotwellController.Output,
            primary.Audit.BalanceMassRateResidualKilogramsPerSecond, primary.Audit.BalancePowerResidualWatts,
            heat.FullEnergyPathClosureResidualJoules,
            generator.BreakerFinallyClosed,
            protectedControl.Protection.ReactorScramActive,
            protectedControl.Protection.TurbineTripActive,
            protectedControl.Protection.GeneratorTripActive);
    }

    private static void CaptureControllers(
        string probeId,
        long logicalStep,
        double elapsedSeconds,
        string systemId,
        NuclearReactorSimulator.Simulation.Physics.Control.ControlAndActuatorSnapshot snapshot,
        NuclearReactorSimulator.Simulation.Plant.PlantSnapshot plant,
        EvidenceStore evidence)
    {
        foreach (var item in snapshot.Controllers.Diagnostics)
        {
            evidence.ControllerSamples.Add(new ControllerSample(
                probeId, logicalStep, elapsedSeconds, systemId, item.ControllerId, item.Mode.ToString(),
                item.Setpoint, item.Measurement, item.Error, item.ProportionalTerm, item.IntegralTerm,
                item.DerivativeTerm, item.UnsaturatedOutput, item.Output, item.IsSaturated,
                item.AntiWindupActive, item.BumplessTransferApplied, item.Status.ToString()));
        }

        foreach (var command in snapshot.ActuatorCommands.ValveCommands)
        {
            var physical = plant.Valves.FirstOrDefault(item => string.Equals(item.ValveId, command.ValveId, StringComparison.Ordinal));
            evidence.ActuatorSamples.Add(new ActuatorSample(
                probeId, logicalStep, elapsedSeconds, systemId, command.ActuatorId, "valve", command.ValveId,
                command.RequestedPosition.Percent, physical?.Position.Percent, physical?.IsFailSafeActive, null));
        }

        foreach (var command in snapshot.ActuatorCommands.PumpCommands)
        {
            var physical = plant.Pumps.FirstOrDefault(item => string.Equals(item.PumpId, command.PumpId, StringComparison.Ordinal));
            evidence.ActuatorSamples.Add(new ActuatorSample(
                probeId, logicalStep, elapsedSeconds, systemId, command.ActuatorId, "pump", command.PumpId,
                command.RequestedSpeed.Fraction, physical?.Speed.Fraction, null, command.RunCommand.ToString()));
        }

        foreach (var command in snapshot.ActuatorCommands.RodCommands)
        {
            evidence.ActuatorSamples.Add(new ActuatorSample(
                probeId, logicalStep, elapsedSeconds, systemId, command.ActuatorId, "rod", command.ActuatorId,
                null, null, null, command.Command.ToString()));
        }
    }

    private static void AuditPerStepSentinels(
        IntegratedAutomaticOperationRuntimeEngine engine,
        SentinelSummary sentinels,
        ICollection<EventSample> events,
        string probeId,
        double elapsedSeconds)
    {
        var protectedControl = engine.LatestCanonicalSnapshot.Control.ProtectedControl;
        var primary = protectedControl.FullPlant.IntegratedCycle.PrimaryCircuit;
        var numerical = primary.HydraulicNumerics;
        sentinels.TotalSteps++;
        if (!numerical.Converged) sentinels.NonConvergedSteps++;
        if (!double.IsFinite(numerical.MaximumRelativePressureResidual)
            || !double.IsFinite(numerical.MaximumAbsoluteFlowResidualKilogramsPerSecond))
        {
            sentinels.NonFiniteNumericalSteps++;
        }

        var telemetry = numerical.FourNodeBranchContinuity;
        if (telemetry is not null)
        {
            if (telemetry.RollbackRequired) sentinels.RollbackSteps++;
            if (telemetry.ShadowLineSearchExhausted) sentinels.LineSearchExhaustedSteps++;
            if (telemetry.UntargetedBranchDisagreementDetected) sentinels.UntargetedDisagreementSteps++;
            if (!telemetry.ShadowConverged && telemetry.ShadowCorrectionEvaluated) sentinels.ShadowNonConvergedSteps++;
        }

        var protection = protectedControl.Protection;
        var anyTrip = protection.ReactorScramActive || protection.TurbineTripActive || protection.GeneratorTripActive;
        if (anyTrip)
        {
            sentinels.TripSteps++;
            if (!sentinels.TripEventWritten)
            {
                sentinels.TripEventWritten = true;
                var latch = protection.Functions.FirstOrDefault(static item => item.IsLatched);
                events.Add(new EventSample(probeId, engine.LogicalStep, elapsedSeconds, "trip-or-latch",
                    $"reactor={protection.ReactorScramActive};turbine={protection.TurbineTripActive};generator={protection.GeneratorTripActive};latch={latch?.FunctionId ?? "none"}"));
            }
        }
    }

    private static IReadOnlyList<TrendEvidence> BuildTrendEvidence(P1BContract contract, IReadOnlyList<WholeSample> rows)
    {
        var reference = rows.Where(static item => item.ProbeId == "background-reference").ToArray();
        var load = rows.Where(static item => item.ProbeId == "exact-v9-5-to-6mwe").ToArray();
        var referenceWindow = reference.Where(item => item.ElapsedSeconds >= contract.BackgroundReferenceSeconds - contract.LateSubwindowSeconds).ToArray();
        var descriptors = TrendDescriptors();
        var output = new List<TrendEvidence>();

        foreach (var descriptor in descriptors)
        {
            var referenceSlope = Slope(referenceWindow, descriptor.Selector);
            var referenceMean = Mean(referenceWindow, descriptor.Selector);
            var loadSlopes = new double[4];
            for (var index = 0; index < 4; index++)
            {
                var start = contract.MaximumHoldSecondsAfterLoad - contract.LateAnalysisSeconds + index * contract.LateSubwindowSeconds;
                var end = start + contract.LateSubwindowSeconds;
                var window = load.Where(item => item.ElapsedSeconds > start && item.ElapsedSeconds <= end).ToArray();
                loadSlopes[index] = Slope(window, descriptor.Selector);
            }

            var guard = Math.Max(
                Math.Abs(referenceSlope) * contract.TrendReferenceMultiplier,
                contract.TrendRelativeMagnitudeGuardPerSecond * Math.Max(1d, Math.Abs(referenceMean)));
            var positive = loadSlopes.Count(value => value > guard);
            var negative = loadSlopes.Count(value => value < -guard);
            var persistent = Math.Max(positive, negative) >= contract.TrendMinimumConsistentLateWindows;
            var medianAbs = Median(loadSlopes.Select(static value => Math.Abs(value)).ToArray());
            var loadSpecific = persistent && medianAbs > guard;
            output.Add(new TrendEvidence(
                descriptor.Domain, descriptor.Observable, descriptor.Units,
                referenceMean, referenceSlope, guard,
                loadSlopes[0], loadSlopes[1], loadSlopes[2], loadSlopes[3],
                persistent, loadSpecific));
        }

        return output;
    }

    private static string ClassifyOwnerDomain(P1BContract contract, RunResult background, RunResult probe, IReadOnlyList<TrendEvidence> trends)
    {
        if (!background.ExecutionPass || !probe.ExecutionPass)
        {
            return "INCONCLUSIVE";
        }

        var active = trends.Where(static item => item.LoadSpecificPersistent)
            .Select(static item => item.Domain)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        if (active.Length == 0) return "NO-MATERIAL-LATE-DRIFT";
        if (active.Length == 1 && contract.AllowedOwnerDomains.Contains(active[0], StringComparer.Ordinal)) return active[0];
        return "COUPLED-MULTI-DOMAIN";
    }

    private static TrendDescriptor[] TrendDescriptors() =>
    [
        new("NEUTRONICS-THERMAL-SOURCE", "total-reactivity", "dk/k/s", static row => row.TotalReactivityDeltaKOverK),
        new("NEUTRONICS-THERMAL-SOURCE", "neutron-population", "relative/s", static row => row.NeutronPopulationRelative),
        new("NEUTRONICS-THERMAL-SOURCE", "fission-power", "MW/s", static row => row.FissionPowerMegawatts),
        new("NEUTRONICS-THERMAL-SOURCE", "reactor-thermal-power", "MW/s", static row => row.ReactorThermalMegawatts),
        new("NEUTRONICS-THERMAL-SOURCE", "thermal-body-stored-energy", "J/s", static row => row.ThermalBodyStoredEnergyJoules),

        new("PRIMARY-INVENTORY-HYDRAULIC", "total-plant-mass", "kg/s", static row => row.TotalPlantMassKilograms),
        new("PRIMARY-INVENTORY-HYDRAULIC", "thermofluid-stored-energy", "J/s", static row => row.TotalThermofluidStoredEnergyJoules),
        new("PRIMARY-INVENTORY-HYDRAULIC", "drum-mass", "kg/s", static row => row.DrumMassKilograms),
        new("PRIMARY-INVENTORY-HYDRAULIC", "drum-internal-energy", "J/s", static row => row.DrumInternalEnergyJoules),
        new("PRIMARY-INVENTORY-HYDRAULIC", "drum-level", "fraction/s", static row => row.DrumLevelFraction),
        new("PRIMARY-INVENTORY-HYDRAULIC", "primary-channel-flow", "kg/s2", static row => row.PrimaryChannelFlowKilogramsPerSecond),
        new("PRIMARY-INVENTORY-HYDRAULIC", "primary-return-flow", "kg/s2", static row => row.PrimaryReturnFlowKilogramsPerSecond),

        new("STEAM-PATH-TURBINE", "turbine-inlet-pressure", "MPa/s", static row => row.TurbineInletPressureMegapascals),
        new("STEAM-PATH-TURBINE", "stage-steam-flow", "kg/s2", static row => row.TurbineSteamFlowKilogramsPerSecond),
        new("STEAM-PATH-TURBINE", "control-valve-position", "percent/s", static row => row.ControlValvePercent),
        new("STEAM-PATH-TURBINE", "shaft-power", "MW/s", static row => row.TurbineShaftMegawatts),
        new("STEAM-PATH-TURBINE", "rotor-kinetic-energy", "J/s", static row => row.RotorKineticEnergyJoules),

        new("CONTROL-ACTUATOR-MEMORY", "flow-controller-integral", "controller-unit/s", static row => row.FlowControllerIntegral),
        new("CONTROL-ACTUATOR-MEMORY", "speed-controller-integral", "controller-unit/s", static row => row.SpeedControllerIntegral),
        new("CONTROL-ACTUATOR-MEMORY", "level-controller-integral", "controller-unit/s", static row => row.LevelControllerIntegral),
        new("CONTROL-ACTUATOR-MEMORY", "hotwell-controller-integral", "controller-unit/s", static row => row.HotwellControllerIntegral),

        new("ELECTROMECHANICAL-GRID", "electrical-output", "MW/s", static row => row.ElectricalOutputMegawatts),
        new("ELECTROMECHANICAL-GRID", "frequency-slip", "Hz/s", static row => row.GeneratorFrequencySlipHertz),
        new("ELECTROMECHANICAL-GRID", "phase-lead", "rad/s", static row => row.SignedPhaseLeadRadians),
        new("ELECTROMECHANICAL-GRID", "dispatch-adequacy", "MW/s", static row => row.DispatchMechanicalAdequacyMegawatts),
    ];

    private static bool IsThermallyReady(WholeSample sample, double targetThermalMegawatts, P1BContract contract)
        => !sample.AnyTripActive
            && sample.BreakerClosed
            && sample.ReactorThermalMegawatts >= targetThermalMegawatts - contract.ThermalReadinessToleranceMegawatts
            && Math.Abs(sample.GeneratorFrequencyHertz - 50d) <= 0.1d;

    private static bool CheckpointMatches(WholeSample sample, P1BCheckpoint checkpoint, CheckpointTolerances tolerances)
        => sample.LogicalStep == checkpoint.ExpectedLogicalStep
            && Math.Abs(sample.ElectricalOutputMegawatts - checkpoint.OutputMegawatts) <= tolerances.PowerMegawatts
            && Math.Abs(sample.ReactorThermalMegawatts - checkpoint.ThermalMegawatts) <= tolerances.ThermalMegawatts
            && Math.Abs(sample.TurbineShaftMegawatts - checkpoint.ShaftMegawatts) <= tolerances.PowerMegawatts
            && Math.Abs(sample.GeneratorFrequencyHertz - checkpoint.FrequencyHertz) <= tolerances.FrequencyHertz
            && Math.Abs(sample.DispatchMechanicalAdequacyMegawatts - checkpoint.DispatchAdequacyMegawatts) <= tolerances.PowerMegawatts
            && Math.Abs(sample.TurbineSteamFlowKilogramsPerSecond - checkpoint.FlowKilogramsPerSecond) <= tolerances.FlowKilogramsPerSecond
            && Math.Abs(sample.TurbineInletPressureMegapascals - checkpoint.InletPressureMegapascals) <= tolerances.PressureMegapascals;

    private static string CheckpointDetail(WholeSample sample, P1BCheckpoint checkpoint, CheckpointTolerances tolerances)
        => $"expected-step={checkpoint.ExpectedLogicalStep};actual-step={sample.LogicalStep};output={F(sample.ElectricalOutputMegawatts)}/{F(checkpoint.OutputMegawatts)};thermal={F(sample.ReactorThermalMegawatts)}/{F(checkpoint.ThermalMegawatts)};shaft={F(sample.TurbineShaftMegawatts)}/{F(checkpoint.ShaftMegawatts)};frequency={F(sample.GeneratorFrequencyHertz)}/{F(checkpoint.FrequencyHertz)};dispatch={F(sample.DispatchMechanicalAdequacyMegawatts)}/{F(checkpoint.DispatchAdequacyMegawatts)};flow={F(sample.TurbineSteamFlowKilogramsPerSecond)}/{F(checkpoint.FlowKilogramsPerSecond)};pressure={F(sample.TurbineInletPressureMegapascals)}/{F(checkpoint.InletPressureMegapascals)};tol-power={F(tolerances.PowerMegawatts)};tol-thermal={F(tolerances.ThermalMegawatts)};tol-frequency={F(tolerances.FrequencyHertz)};tol-flow={F(tolerances.FlowKilogramsPerSecond)};tol-pressure={F(tolerances.PressureMegapascals)}";

    private static IntegratedAutomaticOperationRuntimeEngine CreateEngine(double loadIncrementMegawatts)
    {
        var baseline = Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(
            new DesktopSustainedGenerationPostMoistureEquilibriumCandidateInitialConditionFactory().CreateRuntimeEngine());
        if (Math.Abs(loadIncrementMegawatts - 5d) <= 1e-12d)
        {
            return baseline;
        }

        var solverField = typeof(IntegratedAutomaticOperationRuntimeEngine).GetField("_solver", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("P1B test-only runtime clone could not locate the private solver field.");
        var solver = solverField.GetValue(baseline) as IntegratedAutomaticOperationSolver
            ?? throw new InvalidOperationException("P1B test-only runtime clone could not read the integrated solver.");
        var commandPolicy = new ControlRoomRuntimeCommandPolicy(
            ControlRoomRuntimeCommandPolicy.Default.TurbineSpeedSetpointIncrementRpm,
            loadIncrementMegawatts * 1_000_000d);
        return new IntegratedAutomaticOperationRuntimeEngine(
            solver, baseline.CurrentState, baseline.PersistentInputs, baseline.LatestCanonicalSnapshot,
            baseline.FixedDeltaTime, baseline.LogicalStep, commandPolicy);
    }

    private static double SignedShortestPhaseLeadRadians(double generatorRadians, double gridRadians)
    {
        var difference = generatorRadians - gridRadians;
        var fullTurn = 2d * Math.PI;
        difference = (difference + Math.PI) % fullTurn;
        if (difference < 0d) difference += fullTurn;
        return difference - Math.PI;
    }

    private static double Slope(IReadOnlyList<WholeSample> rows, Func<WholeSample, double> selector)
    {
        if (rows.Count < 2) return double.NaN;
        var meanX = rows.Average(static row => row.ElapsedSeconds);
        var meanY = rows.Average(selector);
        var numerator = 0d;
        var denominator = 0d;
        foreach (var row in rows)
        {
            var dx = row.ElapsedSeconds - meanX;
            numerator += dx * (selector(row) - meanY);
            denominator += dx * dx;
        }
        return denominator > 0d ? numerator / denominator : double.NaN;
    }

    private static double Mean(IReadOnlyList<WholeSample> rows, Func<WholeSample, double> selector)
        => rows.Count == 0 ? double.NaN : rows.Average(selector);

    private static double Median(double[] values)
    {
        if (values.Length == 0) return double.NaN;
        Array.Sort(values);
        return values.Length % 2 == 1 ? values[values.Length / 2] : 0.5d * (values[values.Length / 2 - 1] + values[values.Length / 2]);
    }

    private static void WriteBackgroundSummary(P1BContract contract, RunResult background, IReadOnlyList<WholeSample> rows, IReadOnlyList<TrendEvidence> trends)
    {
        var reference = rows.Where(static item => item.ProbeId == "background-reference").ToArray();
        var tail = reference.Where(item => item.ElapsedSeconds >= contract.BackgroundReferenceSeconds - contract.LateSubwindowSeconds).ToArray();
        var lines = new List<string>
        {
            "observable,mean,raw_slope_per_s,diagnostic_guard_per_s,domain",
        };
        foreach (var trend in trends)
        {
            lines.Add(string.Join(",", Csv(trend.Observable), F(trend.ReferenceMean), F(trend.ReferenceSlope), F(trend.DiagnosticGuard), Csv(trend.Domain)));
        }
        lines.Add($"execution-pass,{background.ExecutionPass},,,");
        lines.Add($"tail-row-count,{tail.Length},,,");
        File.WriteAllLines(Path.Combine(ReportDirectory(), "01-background-reference-late-drift.csv"), lines, Utf8WithoutBom);
    }

    private static void WriteCheckpointTable(P1BContract contract, RunResult probe)
    {
        var lines = new List<string> { "hold_seconds,expected_step,actual_step,matches,output_mwe,thermal_mw,shaft_mw,frequency_hz,dispatch_mw,flow_kg_s,inlet_mpa" };
        foreach (var expected in contract.RequiredCheckpoints)
        {
            var actual = probe.Checkpoints.FirstOrDefault(item => item.HoldSeconds == expected.HoldSeconds);
            lines.Add(actual is null
                ? $"{expected.HoldSeconds},{expected.ExpectedLogicalStep},,False,,,,,,,,"
                : string.Join(",", expected.HoldSeconds, expected.ExpectedLogicalStep, actual.ActualLogicalStep, actual.Matches,
                    F(actual.Sample.ElectricalOutputMegawatts), F(actual.Sample.ReactorThermalMegawatts), F(actual.Sample.TurbineShaftMegawatts),
                    F(actual.Sample.GeneratorFrequencyHertz), F(actual.Sample.DispatchMechanicalAdequacyMegawatts),
                    F(actual.Sample.TurbineSteamFlowKilogramsPerSecond), F(actual.Sample.TurbineInletPressureMegapascals)));
        }
        File.WriteAllLines(Path.Combine(ReportDirectory(), "02-p1a-checkpoint-reproduction.csv"), lines, Utf8WithoutBom);
    }

    private static void WriteWholeTrajectory(IEnumerable<WholeSample> rows)
    {
        var lines = new List<string>
        {
            "probe_id,logical_step,elapsed_s,total_reactivity_dkk,nonrod_reactivity_dkk,rod_reactivity_dkk,neutron_population,fission_mw,reactor_thermal_mw,total_nuclear_heat_mw,total_plant_mass_kg,thermofluid_stored_j,thermal_body_stored_j,coupled_stored_j,feedwater_kg_s,steam_export_kg_s,drum_mass_kg,drum_internal_j,drum_level,primary_pump_kg_s,primary_channel_kg_s,primary_return_kg_s,turbine_flow_kg_s,turbine_inlet_mpa,control_valve_percent,shaft_mw,passive_loss_mw,net_accel_mw,rotor_ke_j,requested_mwe,output_mwe,frequency_hz,frequency_slip_hz,phase_lead_rad,phase_correction_mw,frequency_correction_mw,commanded_em_torque_nm,effective_em_torque_nm,dispatch_adequacy_mw,flow_i,flow_output,speed_i,speed_output,level_i,level_output,hotwell_i,hotwell_output,mass_balance_residual_kg_s,power_balance_residual_w,full_energy_closure_residual_j,breaker_closed,reactor_scram,turbine_trip,generator_trip"
        };
        lines.AddRange(rows.Select(static r => string.Join(",",
            Csv(r.ProbeId), r.LogicalStep, F(r.ElapsedSeconds), F(r.TotalReactivityDeltaKOverK), F(r.NonRodReactivityDeltaKOverK), F(r.RodReactivityDeltaKOverK),
            F(r.NeutronPopulationRelative), F(r.FissionPowerMegawatts), F(r.ReactorThermalMegawatts), F(r.TotalNuclearHeatMegawatts),
            F(r.TotalPlantMassKilograms), F(r.TotalThermofluidStoredEnergyJoules), F(r.ThermalBodyStoredEnergyJoules), F(r.CoupledStoredEnergyJoules),
            F(r.FeedwaterFlowKilogramsPerSecond), F(r.SteamExportFlowKilogramsPerSecond), F(r.DrumMassKilograms), F(r.DrumInternalEnergyJoules), F(r.DrumLevelFraction),
            F(r.PrimaryPumpFlowKilogramsPerSecond), F(r.PrimaryChannelFlowKilogramsPerSecond), F(r.PrimaryReturnFlowKilogramsPerSecond),
            F(r.TurbineSteamFlowKilogramsPerSecond), F(r.TurbineInletPressureMegapascals), F(r.ControlValvePercent),
            F(r.TurbineShaftMegawatts), F(r.PassiveMechanicalLossMegawatts), F(r.NetRotorAccelerationMegawatts), F(r.RotorKineticEnergyJoules),
            F(r.RequestedElectricalMegawatts), F(r.ElectricalOutputMegawatts), F(r.GeneratorFrequencyHertz), F(r.GeneratorFrequencySlipHertz), F(r.SignedPhaseLeadRadians),
            F(r.PhaseCorrectionMegawatts), F(r.FrequencyCorrectionMegawatts), F(r.CommandedElectromagneticTorqueNewtonMetres), F(r.EffectiveElectromagneticTorqueNewtonMetres),
            F(r.DispatchMechanicalAdequacyMegawatts), F(r.FlowControllerIntegral), F(r.FlowControllerOutput), F(r.SpeedControllerIntegral), F(r.SpeedControllerOutput),
            F(r.LevelControllerIntegral), F(r.LevelControllerOutput), F(r.HotwellControllerIntegral), F(r.HotwellControllerOutput),
            F(r.BalanceMassRateResidualKilogramsPerSecond), F(r.BalancePowerResidualWatts), F(r.FullEnergyPathClosureResidualJoules),
            r.BreakerClosed, r.ReactorScram, r.TurbineTrip, r.GeneratorTrip)));
        File.WriteAllLines(Path.Combine(ReportDirectory(), "03-whole-domain-trajectory-1s.csv"), lines, Utf8WithoutBom);
    }

    private static void WriteFluidNodeEvidence(IEnumerable<NodeSample> rows)
    {
        var lines = new List<string> { "probe_id,logical_step,elapsed_s,node_id,mass_kg,internal_energy_j,specific_u_j_kg,pressure_pa,temperature_c,density_kg_m3,phase,vapor_quality" };
        lines.AddRange(rows.Select(static r => string.Join(",", Csv(r.ProbeId), r.LogicalStep, F(r.ElapsedSeconds), Csv(r.NodeId), F(r.MassKilograms), F(r.InternalEnergyJoules), F(r.SpecificInternalEnergyJoulesPerKilogram), F(r.PressurePascals), F(r.TemperatureCelsius), F(r.DensityKilogramsPerCubicMetre), Csv(r.Phase), F(r.VaporQuality))));
        File.WriteAllLines(Path.Combine(ReportDirectory(), "04-fluid-node-inventory-evidence.csv"), lines, Utf8WithoutBom);
    }

    private static void WriteThermalBodyEvidence(IEnumerable<ThermalBodySample> rows)
    {
        var lines = new List<string> { "probe_id,logical_step,elapsed_s,body_id,stored_energy_j,temperature_c" };
        lines.AddRange(rows.Select(static r => string.Join(",", Csv(r.ProbeId), r.LogicalStep, F(r.ElapsedSeconds), Csv(r.BodyId), F(r.StoredEnergyJoules), F(r.TemperatureCelsius))));
        File.WriteAllLines(Path.Combine(ReportDirectory(), "05-thermal-body-evidence.csv"), lines, Utf8WithoutBom);
    }

    private static void WritePrimaryDrumEvidence(IEnumerable<PrimaryDrumSample> rows)
    {
        var lines = new List<string> { "probe_id,logical_step,elapsed_s,total_mass_kg,total_stored_j,feedwater_kg_s,steam_export_kg_s,balance_mass_residual_kg_s,mass_closure_kg,balance_power_residual_w,energy_closure_j,drum_mass_kg,drum_internal_j,drum_pressure_pa,drum_level,drum_return_kg_s,drum_steam_kg_s,drum_recirc_kg_s,separable_liquid_kg,separation_mass_residual_kg_s,separation_energy_residual_w,steam_pressure_capacity_kg_s,steam_available_kg_s,steam_pressure_limited,steam_availability_limited" };
        lines.AddRange(rows.Select(static r => string.Join(",", Csv(r.ProbeId), r.LogicalStep, F(r.ElapsedSeconds), F(r.TotalMassKilograms), F(r.TotalStoredEnergyJoules), F(r.FeedwaterKilogramsPerSecond), F(r.SteamExportKilogramsPerSecond), F(r.BalanceMassResidualKilogramsPerSecond), F(r.MassClosureKilograms), F(r.BalancePowerResidualWatts), F(r.EnergyClosureJoules), F(r.DrumMassKilograms), F(r.DrumInternalEnergyJoules), F(r.DrumPressurePascals), F(r.DrumLevelFraction), F(r.DrumReturnKilogramsPerSecond), F(r.DrumSteamKilogramsPerSecond), F(r.DrumRecirculationKilogramsPerSecond), F(r.SeparableLiquidKilograms), F(r.SeparationMassResidualKilogramsPerSecond), F(r.SeparationEnergyResidualWatts), F(r.SteamPressureCapacityKilogramsPerSecond), F(r.SteamAvailableKilogramsPerSecond), r.SteamPressureLimited, r.SteamAvailabilityLimited)));
        File.WriteAllLines(Path.Combine(ReportDirectory(), "06-primary-drum-inventory-flow-evidence.csv"), lines, Utf8WithoutBom);
    }

    private static void WriteBranchEvidence(IEnumerable<BranchSample> rows)
    {
        var lines = new List<string> { "probe_id,logical_step,elapsed_s,loop_id,group_id,suction_pa,pressure_header_pa,header_rise_pa,total_pump_kg_s,total_channel_kg_s,total_return_kg_s,pump_channel_residual_kg_s,channel_return_residual_kg_s,branch_channel_kg_s,branch_return_kg_s,per_channel_kg_s,branch_continuity_residual_kg_s,channel_dp_pa,return_dp_pa,outlet_phase,outlet_quality,outlet_void" };
        lines.AddRange(rows.Select(static r => string.Join(",", Csv(r.ProbeId), r.LogicalStep, F(r.ElapsedSeconds), Csv(r.LoopId), Csv(r.GroupId), F(r.SuctionPressurePascals), F(r.PressureHeaderPascals), F(r.HeaderRisePascals), F(r.TotalPumpKilogramsPerSecond), F(r.TotalChannelKilogramsPerSecond), F(r.TotalReturnKilogramsPerSecond), F(r.PumpChannelResidualKilogramsPerSecond), F(r.ChannelReturnResidualKilogramsPerSecond), F(r.BranchChannelKilogramsPerSecond), F(r.BranchReturnKilogramsPerSecond), F(r.PerChannelKilogramsPerSecond), F(r.BranchContinuityResidualKilogramsPerSecond), F(r.ChannelPressureDifferencePascals), F(r.ReturnPressureDifferencePascals), Csv(r.OutletPhase), F(r.OutletQuality), F(r.OutletVoid))));
        File.WriteAllLines(Path.Combine(ReportDirectory(), "07-primary-branch-flow-evidence.csv"), lines, Utf8WithoutBom);
    }

    private static void WriteControllerEvidence(IEnumerable<ControllerSample> rows)
    {
        var lines = new List<string> { "probe_id,logical_step,elapsed_s,system_id,controller_id,mode,setpoint,measurement,error,p_term,i_term,d_term,unsaturated_output,output,is_saturated,anti_windup,bumpless,status" };
        lines.AddRange(rows.Select(static r => string.Join(",", Csv(r.ProbeId), r.LogicalStep, F(r.ElapsedSeconds), Csv(r.SystemId), Csv(r.ControllerId), Csv(r.Mode), F(r.Setpoint), F(r.Measurement), F(r.Error), F(r.ProportionalTerm), F(r.IntegralTerm), F(r.DerivativeTerm), F(r.UnsaturatedOutput), F(r.Output), r.IsSaturated, r.AntiWindup, r.BumplessTransfer, Csv(r.Status))));
        File.WriteAllLines(Path.Combine(ReportDirectory(), "08-controller-diagnostics.csv"), lines, Utf8WithoutBom);
    }

    private static void WriteActuatorEvidence(IEnumerable<ActuatorSample> rows)
    {
        var lines = new List<string> { "probe_id,logical_step,elapsed_s,system_id,actuator_id,kind,target_id,requested_value,physical_value,fail_safe_active,command_text" };
        lines.AddRange(rows.Select(static r => string.Join(",", Csv(r.ProbeId), r.LogicalStep, F(r.ElapsedSeconds), Csv(r.SystemId), Csv(r.ActuatorId), Csv(r.Kind), Csv(r.TargetId), F(r.RequestedValue), F(r.PhysicalValue), r.FailSafeActive?.ToString() ?? string.Empty, Csv(r.CommandText ?? string.Empty))));
        File.WriteAllLines(Path.Combine(ReportDirectory(), "09-actuator-command-physical-evidence.csv"), lines, Utf8WithoutBom);
    }

    private static void WriteTurbineGeneratorEvidence(IEnumerable<TurbineGeneratorSample> rows)
    {
        var lines = new List<string> { "probe_id,logical_step,elapsed_s,steam_line_kg_s,turbine_admission_kg_s,relief_kg_s,stage_commanded_kg_s,stage_effective_kg_s,moisture_drain_kg_s,total_transferred_kg_s,inlet_pa,inlet_c,specific_work_j_kg,stop_valve_percent,control_valve_percent,admission_valve_percent,stop_control_residual_kg_s,control_admission_residual_kg_s,shaft_mw,passive_loss_mw,net_accel_mw,rotor_ke_j,net_torque_nm,requested_mwe,output_mwe,frequency_hz,slip_hz,phase_lead_rad,commanded_em_torque_nm,effective_em_torque_nm,dispatch_adequacy_mw,nuclear_heat_mw,net_external_mw,coupled_stored_change_j,full_energy_closure_j" };
        lines.AddRange(rows.Select(static r => string.Join(",", Csv(r.ProbeId), r.LogicalStep, F(r.ElapsedSeconds), F(r.SteamLineKilogramsPerSecond), F(r.TurbineAdmissionKilogramsPerSecond), F(r.ReliefKilogramsPerSecond), F(r.StageCommandedKilogramsPerSecond), F(r.StageEffectiveKilogramsPerSecond), F(r.MoistureDrainKilogramsPerSecond), F(r.TotalTransferredKilogramsPerSecond), F(r.InletPressurePascals), F(r.InletTemperatureCelsius), F(r.SpecificWorkJoulesPerKilogram), F(r.StopValvePercent), F(r.ControlValvePercent), F(r.AdmissionValvePercent), F(r.StopControlResidualKilogramsPerSecond), F(r.ControlAdmissionResidualKilogramsPerSecond), F(r.ShaftMegawatts), F(r.PassiveLossMegawatts), F(r.NetAccelerationMegawatts), F(r.RotorKineticEnergyJoules), F(r.NetTorqueNewtonMetres), F(r.RequestedMegawatts), F(r.OutputMegawatts), F(r.FrequencyHertz), F(r.FrequencySlipHertz), F(r.PhaseLeadRadians), F(r.CommandedElectromagneticTorqueNewtonMetres), F(r.EffectiveElectromagneticTorqueNewtonMetres), F(r.DispatchAdequacyMegawatts), F(r.NuclearHeatMegawatts), F(r.NetExternalMegawatts), F(r.CoupledStoredChangeJoules), F(r.FullEnergyClosureJoules))));
        File.WriteAllLines(Path.Combine(ReportDirectory(), "10-turbine-generator-balance-evidence.csv"), lines, Utf8WithoutBom);
    }

    private static void WriteTrendEvidence(IEnumerable<TrendEvidence> rows)
    {
        var lines = new List<string> { "domain,observable,units,reference_mean,reference_slope_per_s,diagnostic_guard_per_s,load_window1_slope,load_window2_slope,load_window3_slope,load_window4_slope,persistent_direction,load_specific_persistent" };
        lines.AddRange(rows.Select(static r => string.Join(",", Csv(r.Domain), Csv(r.Observable), Csv(r.Units), F(r.ReferenceMean), F(r.ReferenceSlope), F(r.DiagnosticGuard), F(r.LoadWindow1Slope), F(r.LoadWindow2Slope), F(r.LoadWindow3Slope), F(r.LoadWindow4Slope), r.PersistentDirection, r.LoadSpecificPersistent)));
        File.WriteAllLines(Path.Combine(ReportDirectory(), "11-domain-late-window-trends.csv"), lines, Utf8WithoutBom);
    }

    private static void WriteEvents(IEnumerable<EventSample> events, SentinelSummary background, SentinelSummary probe)
    {
        var lines = new List<string> { "probe_id,logical_step,elapsed_s,event_kind,detail" };
        lines.AddRange(events.Select(static r => string.Join(",", Csv(r.ProbeId), r.LogicalStep, F(r.ElapsedSeconds), Csv(r.EventKind), Csv(r.Detail))));
        lines.Add(string.Join(",", Csv(background.ProbeId), "", "", Csv("sentinel-summary"), Csv(background.ToEvidenceString())));
        lines.Add(string.Join(",", Csv(probe.ProbeId), "", "", Csv("sentinel-summary"), Csv(probe.ToEvidenceString())));
        File.WriteAllLines(Path.Combine(ReportDirectory(), "12-events-protection-numerical-sentinels.csv"), lines, Utf8WithoutBom);
    }

    private static void WriteEngineeringSummary(P1BContract contract, RunResult background, RunResult probe, string ownerDomain, IReadOnlyList<TrendEvidence> trends)
    {
        var active = trends.Where(static item => item.LoadSpecificPersistent).ToArray();
        var lines = new List<string>
        {
            "scope=M10 Final Replacement-Long Closure Plan 1 P1B Slow-State Closure & Phenomenon-Owner Qualification; observation-only; exact-v9 unchanged; no workload/protection/authority/generator-load/mission change;",
            $"contract={contract.ContractId};baseline={contract.Baseline};",
            $"background-reference=execution-pass:{background.ExecutionPass}|seconds:{contract.BackgroundReferenceSeconds};",
            $"load-probe=execution-pass:{probe.ExecutionPass}|load-command-step:{I(probe.LoadCommandStep)}|checkpoints:{probe.ReproducedCheckpointCount}/{contract.RequiredCheckpoints.Length}|max-hold-seconds:{contract.MaximumHoldSecondsAfterLoad};",
            $"p1b-owner-evidence-label={ownerDomain};active-domain-count={active.Select(static item => item.Domain).Distinct(StringComparer.Ordinal).Count()};active-observable-count={active.Length};",
            "owner-label-semantics=evidence only; largest downstream slope is not root-cause proof; conserved inventory/transfer evidence precedes downstream inference; no scalar cross-domain owner score is used;",
            "one-second-sampling=slow-state-downsample-only; protection/numerical sentinels audited at canonical step/event cadence;",
            "tier-c-physics-synthesized=False;blind-extension-beyond-3600s=False;stationarity-threshold-relaxed=False;",
            "production-src-changed=False;pre-existing-tests-changed=False;replacement-workload-changed=False;runtime-semantics-changed=False;authority-policy-changed=False;generator-load-semantics-changed=False;protection-semantics-changed=False;exact-v9-changed=False;mission-pack-changed=False;",
            "p3-w-authorized=False;p3-r-authorized=False;second-replacement-long-authorized=False;next-authorized-gate=P2R2-Decision-Reentry-2;",
            $"m10-final-replacement-long-closure-plan1-p1b-passes={background.ExecutionPass && probe.ExecutionPass}",
        };
        foreach (var domain in active.GroupBy(static item => item.Domain, StringComparer.Ordinal))
        {
            lines.Add($"active-domain={domain.Key};observables={string.Join('|', domain.Select(static item => item.Observable))};");
        }
        File.WriteAllLines(Path.Combine(ReportDirectory(), "13-p1b-engineering-summary.txt"), lines, Utf8WithoutBom);
    }

    private static P1BContract LoadContract()
    {
        var path = Path.Combine(FindRepositoryRoot(), "eng", ContractFileName);
        return JsonSerializer.Deserialize<P1BContract>(File.ReadAllText(path))
            ?? throw new InvalidOperationException($"Could not deserialize {ContractFileName}.");
    }

    private static void ValidateContract(P1BContract contract)
    {
        Assert.Equal("m10-final-replacement-long-closure-plan1-p1b-v1", contract.ContractId);
        Assert.Equal(100, contract.StepsPerSecond);
        Assert.Equal(600, contract.BackgroundReferenceSeconds);
        Assert.Equal(3600, contract.MaximumHoldSecondsAfterLoad);
        Assert.Equal(1, contract.TrajectorySampleSeconds);
        Assert.Equal(1200, contract.LateAnalysisSeconds);
        Assert.Equal(300, contract.LateSubwindowSeconds);
        Assert.Equal(2785, contract.ExpectedLoadCommandLogicalStep);
        Assert.Equal(3, contract.RequiredCheckpoints.Length);
        Assert.Equal(new[] { "flow-control" }, contract.RequiredReactorPrimaryControllerIds);
        Assert.Equal(new[] { "speed-control", "level-control", "hotwell-control" }, contract.RequiredTurbineSecondaryControllerIds);
        Assert.False(contract.NormalizedCrossDomainOwnerScoreEnabled);
        Assert.True(contract.OneSecondSamplingIsSlowStateDownsample);
        Assert.True(contract.PerStepProtectionAndNumericalSentinelsRequired);
        Assert.False(contract.NewConstitutivePhysicsAuthorized);
        Assert.False(contract.BlindHoldExtensionAuthorized);
        Assert.False(contract.DirectP3SelectionAuthorized);
        Assert.False(contract.SecondReplacementLongAuthorized);
        Assert.Contains("INCONCLUSIVE", contract.AllowedOwnerDomains);
        Assert.Contains("COUPLED-MULTI-DOMAIN", contract.AllowedOwnerDomains);
    }

    private static void ValidatePlanningPrerequisiteEvidence()
    {
        var root = Path.Combine(FindRepositoryRoot(), "eng", "frozen-evidence", "ordinary");
        var p1a = File.ReadAllText(Path.Combine(root, "M10FinalReplacementLongClosurePlan1_P1A_DecisionSummary.txt"));
        var amendment = File.ReadAllText(Path.Combine(root, "M10FinalReplacementLongClosurePlan1_P2R_PlanAmendment2_ValidatedSummary.txt"));
        var pass2 = File.ReadAllText(Path.Combine(root, "PreM11_TodreasKazimi_DeepReviewPass2_ValidatedSummary.txt"));
        Assert.Contains("p1a-final-classification=INCONCLUSIVE", p1a, StringComparison.Ordinal);
        Assert.Contains("exact-v9-6-classification=INCONCLUSIVE", p1a, StringComparison.Ordinal);
        Assert.Contains("p1-checkpoints-reproduced=True", p1a, StringComparison.Ordinal);
        Assert.Contains("p2r-decision=PLAN-STOP-INCONCLUSIVE", amendment, StringComparison.Ordinal);
        Assert.Contains("plan-amendment-2=P1B-SLOW-STATE-CLOSURE-PHENOMENON-OWNER-QUALIFICATION", amendment, StringComparison.Ordinal);
        Assert.Contains("pre-m11-todreas-kazimi-deep-review-pass2-passes=True", pass2, StringComparison.Ordinal);
        Assert.Contains("disposition=PASS-AS-AUTHORED", pass2, StringComparison.Ordinal);
        Assert.Contains("next-authorized-implementation=P1B-Implementation", pass2, StringComparison.Ordinal);
    }

    private static void RequireOptIn()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable(OptInEnvironmentVariable), "1", StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Set {OptInEnvironmentVariable}=1 to run M10 Final Replacement-Long Closure Plan 1 P1B.");
        }
    }

    private static string ReportDirectory()
        => Path.Combine(FindRepositoryRoot(), "artifacts", "m10-final-replacement-long-closure-plan1-p1b");

    private static void ResetDirectory(string directory)
    {
        if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, "00-progress.txt"), "M10 FINAL REPLACEMENT-LONG CLOSURE PLAN 1 P1B STARTED" + Environment.NewLine, Utf8WithoutBom);
    }

    private static void AppendProgress(string message)
        => File.AppendAllText(Path.Combine(ReportDirectory(), "00-progress.txt"), message + Environment.NewLine, Utf8WithoutBom);

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "NuclearReactorSimulator.sln"))) return directory.FullName;
            directory = directory.Parent;
        }
        throw new DirectoryNotFoundException("Could not locate NuclearReactorSimulator.sln from the test base directory.");
    }

    private static string Flatten(string value) => value.Replace('\r', ' ').Replace('\n', ' ');
    private static string F(double? value) => value.HasValue && double.IsFinite(value.Value) ? value.Value.ToString("G17", CultureInfo.InvariantCulture) : string.Empty;
    private static string F(double value) => double.IsFinite(value) ? value.ToString("G17", CultureInfo.InvariantCulture) : string.Empty;
    private static string I(long? value) => value?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
    private static string Csv(string value) => '"' + value.Replace("\"", "\"\"", StringComparison.Ordinal) + '"';

    private sealed class EvidenceStore
    {
        public List<WholeSample> WholeSamples { get; } = [];
        public List<NodeSample> NodeSamples { get; } = [];
        public List<ThermalBodySample> ThermalBodySamples { get; } = [];
        public List<PrimaryDrumSample> PrimaryDrumSamples { get; } = [];
        public List<BranchSample> BranchSamples { get; } = [];
        public List<ControllerSample> ControllerSamples { get; } = [];
        public List<ActuatorSample> ActuatorSamples { get; } = [];
        public List<TurbineGeneratorSample> TurbineGeneratorSamples { get; } = [];
        public List<EventSample> Events { get; } = [];
    }

    private sealed class SentinelSummary(string probeId)
    {
        public string ProbeId { get; } = probeId;
        public long TotalSteps { get; set; }
        public long TripSteps { get; set; }
        public long NonConvergedSteps { get; set; }
        public long NonFiniteNumericalSteps { get; set; }
        public long RollbackSteps { get; set; }
        public long LineSearchExhaustedSteps { get; set; }
        public long UntargetedDisagreementSteps { get; set; }
        public long ShadowNonConvergedSteps { get; set; }
        public bool TripEventWritten { get; set; }
        public bool AnyTripOrNumericalFailure => TripSteps > 0 || NonConvergedSteps > 0 || NonFiniteNumericalSteps > 0 || RollbackSteps > 0;
        public string ToEvidenceString() => $"steps={TotalSteps};trip={TripSteps};nonconverged={NonConvergedSteps};nonfinite={NonFiniteNumericalSteps};rollback={RollbackSteps};line-search-exhausted={LineSearchExhaustedSteps};untargeted-disagreement={UntargetedDisagreementSteps};shadow-nonconverged={ShadowNonConvergedSteps}";
    }

    private sealed record RunResult(string ProbeId, bool ExecutionPass, string FailureMessage, long? LoadCommandStep, int ReproducedCheckpointCount, SentinelSummary Sentinels, CheckpointResult[]? CheckpointArray = null)
    {
        public IReadOnlyList<CheckpointResult> Checkpoints => CheckpointArray ?? Array.Empty<CheckpointResult>();
    }

    private sealed record CheckpointResult(int HoldSeconds, long ActualLogicalStep, bool Matches, WholeSample Sample);
    private sealed record EventSample(string ProbeId, long LogicalStep, double ElapsedSeconds, string EventKind, string Detail);
    private sealed record TrendDescriptor(string Domain, string Observable, string Units, Func<WholeSample, double> Selector);
    private sealed record TrendEvidence(string Domain, string Observable, string Units, double ReferenceMean, double ReferenceSlope, double DiagnosticGuard, double LoadWindow1Slope, double LoadWindow2Slope, double LoadWindow3Slope, double LoadWindow4Slope, bool PersistentDirection, bool LoadSpecificPersistent);

    private sealed record WholeSample(
        string ProbeId, long LogicalStep, double ElapsedSeconds,
        double TotalReactivityDeltaKOverK, double NonRodReactivityDeltaKOverK, double RodReactivityDeltaKOverK,
        double NeutronPopulationRelative, double FissionPowerMegawatts, double ReactorThermalMegawatts, double TotalNuclearHeatMegawatts,
        double TotalPlantMassKilograms, double TotalThermofluidStoredEnergyJoules, double ThermalBodyStoredEnergyJoules, double CoupledStoredEnergyJoules,
        double FeedwaterFlowKilogramsPerSecond, double SteamExportFlowKilogramsPerSecond,
        double DrumMassKilograms, double DrumInternalEnergyJoules, double DrumLevelFraction,
        double PrimaryPumpFlowKilogramsPerSecond, double PrimaryChannelFlowKilogramsPerSecond, double PrimaryReturnFlowKilogramsPerSecond,
        double TurbineSteamFlowKilogramsPerSecond, double TurbineInletPressureMegapascals, double ControlValvePercent,
        double TurbineShaftMegawatts, double PassiveMechanicalLossMegawatts, double NetRotorAccelerationMegawatts, double RotorKineticEnergyJoules,
        double RequestedElectricalMegawatts, double ElectricalOutputMegawatts, double GeneratorFrequencyHertz, double GeneratorFrequencySlipHertz,
        double SignedPhaseLeadRadians, double PhaseCorrectionMegawatts, double FrequencyCorrectionMegawatts,
        double CommandedElectromagneticTorqueNewtonMetres, double EffectiveElectromagneticTorqueNewtonMetres, double DispatchMechanicalAdequacyMegawatts,
        double FlowControllerIntegral, double FlowControllerOutput, double SpeedControllerIntegral, double SpeedControllerOutput,
        double LevelControllerIntegral, double LevelControllerOutput, double HotwellControllerIntegral, double HotwellControllerOutput,
        double BalanceMassRateResidualKilogramsPerSecond, double BalancePowerResidualWatts, double FullEnergyPathClosureResidualJoules,
        bool BreakerClosed, bool ReactorScram, bool TurbineTrip, bool GeneratorTrip)
    {
        public bool AnyTripActive => ReactorScram || TurbineTrip || GeneratorTrip;
        public bool AllFinite => new[]
        {
            ElapsedSeconds, TotalReactivityDeltaKOverK, NonRodReactivityDeltaKOverK, RodReactivityDeltaKOverK,
            NeutronPopulationRelative, FissionPowerMegawatts, ReactorThermalMegawatts, TotalNuclearHeatMegawatts,
            TotalPlantMassKilograms, TotalThermofluidStoredEnergyJoules, ThermalBodyStoredEnergyJoules, CoupledStoredEnergyJoules,
            FeedwaterFlowKilogramsPerSecond, SteamExportFlowKilogramsPerSecond, DrumMassKilograms, DrumInternalEnergyJoules, DrumLevelFraction,
            PrimaryPumpFlowKilogramsPerSecond, PrimaryChannelFlowKilogramsPerSecond, PrimaryReturnFlowKilogramsPerSecond,
            TurbineSteamFlowKilogramsPerSecond, TurbineInletPressureMegapascals, ControlValvePercent,
            TurbineShaftMegawatts, PassiveMechanicalLossMegawatts, NetRotorAccelerationMegawatts, RotorKineticEnergyJoules,
            RequestedElectricalMegawatts, ElectricalOutputMegawatts, GeneratorFrequencyHertz, GeneratorFrequencySlipHertz,
            SignedPhaseLeadRadians, PhaseCorrectionMegawatts, FrequencyCorrectionMegawatts, CommandedElectromagneticTorqueNewtonMetres,
            EffectiveElectromagneticTorqueNewtonMetres, DispatchMechanicalAdequacyMegawatts,
            FlowControllerIntegral, FlowControllerOutput, SpeedControllerIntegral, SpeedControllerOutput,
            LevelControllerIntegral, LevelControllerOutput, HotwellControllerIntegral, HotwellControllerOutput,
            BalanceMassRateResidualKilogramsPerSecond, BalancePowerResidualWatts, FullEnergyPathClosureResidualJoules,
        }.All(double.IsFinite);
    }

    private sealed record NodeSample(string ProbeId, long LogicalStep, double ElapsedSeconds, string NodeId, double MassKilograms, double InternalEnergyJoules, double SpecificInternalEnergyJoulesPerKilogram, double PressurePascals, double TemperatureCelsius, double DensityKilogramsPerCubicMetre, string Phase, double? VaporQuality)
    {
        public bool AllFinite => double.IsFinite(ElapsedSeconds) && double.IsFinite(MassKilograms) && double.IsFinite(InternalEnergyJoules) && double.IsFinite(SpecificInternalEnergyJoulesPerKilogram) && double.IsFinite(PressurePascals) && double.IsFinite(TemperatureCelsius) && double.IsFinite(DensityKilogramsPerCubicMetre) && (!VaporQuality.HasValue || double.IsFinite(VaporQuality.Value));
    }
    private sealed record ThermalBodySample(string ProbeId, long LogicalStep, double ElapsedSeconds, string BodyId, double StoredEnergyJoules, double TemperatureCelsius)
    {
        public bool AllFinite => double.IsFinite(ElapsedSeconds) && double.IsFinite(StoredEnergyJoules) && double.IsFinite(TemperatureCelsius);
    }
    private sealed record PrimaryDrumSample(string ProbeId, long LogicalStep, double ElapsedSeconds, double TotalMassKilograms, double TotalStoredEnergyJoules, double FeedwaterKilogramsPerSecond, double SteamExportKilogramsPerSecond, double BalanceMassResidualKilogramsPerSecond, double MassClosureKilograms, double BalancePowerResidualWatts, double EnergyClosureJoules, double DrumMassKilograms, double DrumInternalEnergyJoules, double DrumPressurePascals, double DrumLevelFraction, double DrumReturnKilogramsPerSecond, double DrumSteamKilogramsPerSecond, double DrumRecirculationKilogramsPerSecond, double SeparableLiquidKilograms, double SeparationMassResidualKilogramsPerSecond, double SeparationEnergyResidualWatts, double SteamPressureCapacityKilogramsPerSecond, double SteamAvailableKilogramsPerSecond, bool SteamPressureLimited, bool SteamAvailabilityLimited);
    private sealed record BranchSample(string ProbeId, long LogicalStep, double ElapsedSeconds, string LoopId, string GroupId, double SuctionPressurePascals, double PressureHeaderPascals, double HeaderRisePascals, double TotalPumpKilogramsPerSecond, double TotalChannelKilogramsPerSecond, double TotalReturnKilogramsPerSecond, double PumpChannelResidualKilogramsPerSecond, double ChannelReturnResidualKilogramsPerSecond, double BranchChannelKilogramsPerSecond, double BranchReturnKilogramsPerSecond, double PerChannelKilogramsPerSecond, double BranchContinuityResidualKilogramsPerSecond, double ChannelPressureDifferencePascals, double ReturnPressureDifferencePascals, string OutletPhase, double? OutletQuality, double? OutletVoid);
    private sealed record ControllerSample(string ProbeId, long LogicalStep, double ElapsedSeconds, string SystemId, string ControllerId, string Mode, double Setpoint, double? Measurement, double Error, double ProportionalTerm, double IntegralTerm, double DerivativeTerm, double UnsaturatedOutput, double Output, bool IsSaturated, bool AntiWindup, bool BumplessTransfer, string Status);
    private sealed record ActuatorSample(string ProbeId, long LogicalStep, double ElapsedSeconds, string SystemId, string ActuatorId, string Kind, string TargetId, double? RequestedValue, double? PhysicalValue, bool? FailSafeActive, string? CommandText);
    private sealed record TurbineGeneratorSample(string ProbeId, long LogicalStep, double ElapsedSeconds, double SteamLineKilogramsPerSecond, double TurbineAdmissionKilogramsPerSecond, double ReliefKilogramsPerSecond, double StageCommandedKilogramsPerSecond, double StageEffectiveKilogramsPerSecond, double MoistureDrainKilogramsPerSecond, double TotalTransferredKilogramsPerSecond, double InletPressurePascals, double InletTemperatureCelsius, double SpecificWorkJoulesPerKilogram, double StopValvePercent, double ControlValvePercent, double AdmissionValvePercent, double StopControlResidualKilogramsPerSecond, double ControlAdmissionResidualKilogramsPerSecond, double ShaftMegawatts, double PassiveLossMegawatts, double NetAccelerationMegawatts, double RotorKineticEnergyJoules, double NetTorqueNewtonMetres, double RequestedMegawatts, double OutputMegawatts, double FrequencyHertz, double FrequencySlipHertz, double PhaseLeadRadians, double CommandedElectromagneticTorqueNewtonMetres, double EffectiveElectromagneticTorqueNewtonMetres, double DispatchAdequacyMegawatts, double NuclearHeatMegawatts, double NetExternalMegawatts, double CoupledStoredChangeJoules, double FullEnergyClosureJoules);

    private sealed record P1BContract(string ContractId, string Baseline, string Question, int StepsPerSecond, int BackgroundReferenceSeconds, int PreparationTimeoutSeconds, int MaximumHoldSecondsAfterLoad, int TrajectorySampleSeconds, int LateAnalysisSeconds, int LateSubwindowSeconds, double ThermalReadinessToleranceMegawatts, double TargetLoadMegawatts, double LoadIncrementMegawatts, long ExpectedLoadCommandLogicalStep, double TrendReferenceMultiplier, double TrendRelativeMagnitudeGuardPerSecond, int TrendMinimumConsistentLateWindows, CheckpointTolerances CheckpointTolerances, P1BCheckpoint[] RequiredCheckpoints, string[] RequiredReactorPrimaryControllerIds, string[] RequiredTurbineSecondaryControllerIds, string[] AllowedOwnerDomains, bool OneSecondSamplingIsSlowStateDownsample, bool PerStepProtectionAndNumericalSentinelsRequired, bool NormalizedCrossDomainOwnerScoreEnabled, bool NewConstitutivePhysicsAuthorized, bool BlindHoldExtensionAuthorized, bool DirectP3SelectionAuthorized, bool SecondReplacementLongAuthorized);
    private sealed record CheckpointTolerances(double PowerMegawatts, double ThermalMegawatts, double FrequencyHertz, double FlowKilogramsPerSecond, double PressureMegapascals);
    private sealed record P1BCheckpoint(int HoldSeconds, long ExpectedLogicalStep, double OutputMegawatts, double ThermalMegawatts, double ShaftMegawatts, double FrequencyHertz, double DispatchAdequacyMegawatts, double FlowKilogramsPerSecond, double InletPressureMegapascals);
}
