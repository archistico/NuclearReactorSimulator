using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.ControlRoom.Hmi;
using NuclearReactorSimulator.Application.Scenarios;
using NuclearReactorSimulator.Application.Scenarios.Training;
using NuclearReactorSimulator.Domain.Physics.Control.Alarms;
using NuclearReactorSimulator.Domain.Physics.Control.Protection;
using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Physics.Reactor.PrimaryCircuit.SteamDrums;
using NuclearReactorSimulator.Domain.Physics.TurbineIsland.Condenser;
using NuclearReactorSimulator.Domain.Physics.TurbineIsland.Turbine;
using NuclearReactorSimulator.Domain.Plant;
using NuclearReactorSimulator.Simulation.Physics.Control.Protection;
using NuclearReactorSimulator.Simulation.Physics.Instrumentation;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.Scenarios.Training;

public sealed class DesktopSustainedGenerationInitialConditionFactoryTests
{
    [Fact]
    public void Version2_PreservesLegacyVersion1IdentityAndPublishesGenerationReadyMechanicalSupport()
    {
        var legacy = new DesktopIntegratedOperationsInitialConditionFactory();
        var current = new DesktopSustainedGenerationInitialConditionFactory();
        var registry = new VersionedInitialConditionRegistry(new IVersionedInitialConditionFactory[] { legacy, current });

        Assert.Equal(new InitialConditionReference("integrated-operations-desktop-stable", 1), legacy.Descriptor.Reference);
        Assert.Equal(new InitialConditionReference("integrated-operations-desktop-stable", 2), current.Descriptor.Reference);
        Assert.Same(legacy, registry.Resolve(legacy.Descriptor.Reference));
        Assert.Same(current, registry.Resolve(current.Descriptor.Reference));

        var legacyEngine = Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(legacy.CreateRuntimeEngine());
        var legacyStage = Assert.Single(legacyEngine.CurrentState.PlantDefinition.TurbineExpansionSystem.StageGroups);
        Assert.False(legacyStage.ExpansionResistance.HasValue);
        Assert.Null(legacyStage.ThermodynamicWork);
        Assert.Equal(TurbineAdmissionPhasePolicy.LegacyUnrestricted, legacyStage.AdmissionPhasePolicy);
        Assert.Equal(FluidEnergyTransportMode.SpecificInternalEnergy, legacyStage.EnergyTransportMode);
        var legacyRotorDefinition = Assert.Single(legacyEngine.CurrentState.PlantDefinition.TurbineExpansionSystem.Rotors);
        Assert.Null(legacyRotorDefinition.MechanicalLoss);
        var legacyDrum = Assert.Single(legacyEngine.CurrentState.PlantDefinition
            .TurbineExpansionSystem.MainSteamNetwork.PrimaryCircuit.SteamDrumSystem.Drums);
        Assert.Equal(SteamDrumLiquidRecirculationMode.LegacyReturnSplit, legacyDrum.LiquidRecirculationMode);
        Assert.Null(legacyDrum.SteamSource);
        Assert.Empty(legacyEngine.CurrentState.PlantDefinition.PlantDefinition.HeatTransfers);
        Assert.Equal(
            HydraulicNumericalCouplingMode.ExplicitCommittedState,
            legacyEngine.CurrentState.PlantDefinition.PlantDefinition.HydraulicNumericalCoupling.Mode);
        var legacyCondenser = Assert.Single(legacyEngine.CurrentState.PlantDefinition
            .CondensateFeedwaterSystem.CondenserSystem.Condensers);
        Assert.False(legacyCondenser.OverallHeatTransferConductance.HasValue);
        Assert.Equal(CondenserCondensateEnergyMode.LegacyHotwellSpecificInternalEnergy, legacyCondenser.CondensateEnergyMode);
        var legacyCoolingDefinition = Assert.Single(legacyEngine.CurrentState.PlantDefinition
            .CondensateFeedwaterSystem.CondenserSystem.CoolingBoundaries);
        Assert.Null(legacyCoolingDefinition.MaximumInstalledHeatRejectionPower);
        var legacyGeneratorDefinition = Assert.Single(legacyEngine.CurrentState.PlantDefinition.GeneratorGridSystem.Generators);
        Assert.Null(legacyGeneratorDefinition.GridCoupling);
        Assert.False(legacyEngine.CurrentState.PlantDefinition.PlantDefinition.GetPump("condensate-pump").HasDischargeCheckValve);
        Assert.False(legacyEngine.CurrentState.PlantDefinition.PlantDefinition.GetPump("feedwater-pump").HasDischargeCheckValve);
        Assert.Equal(
            100_000d,
            legacyEngine.CurrentState.PlantDefinition.PlantDefinition.GetPipe("channel").Resistance.PascalSecondsSquaredPerKilogramSquared,
            12);
        Assert.Equal(
            100_000_000d,
            legacyEngine.CurrentState.PlantDefinition.PlantDefinition.GetPump("pump").InternalResistance.PascalSecondsSquaredPerKilogramSquared,
            12);
        Assert.All(
            legacyEngine.CurrentState.TurbineSecondaryControlState.Definition.ActuatorSystem.Actuators
                .Where(static actuator => actuator.TargetKind is
                    NuclearReactorSimulator.Domain.Physics.Control.ActuatorTargetKind.Valve
                    or NuclearReactorSimulator.Domain.Physics.Control.ActuatorTargetKind.Pump),
            static actuator => Assert.Null(actuator.TravelRate));
        Assert.Null(legacyEngine.CurrentState.TurbineSecondaryControlState.Definition.GovernorDroop);

        var currentEngine = Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(current.CreateRuntimeEngine());
        var currentPlant = currentEngine.CurrentState.PlantDefinition.PlantDefinition;
        var currentHydraulicCoupling = currentPlant.HydraulicNumericalCoupling;
        Assert.Equal(HydraulicNumericalCouplingMode.ExplicitCommittedState, currentHydraulicCoupling.Mode);
        Assert.Equal(1, currentHydraulicCoupling.MaximumCorrectorIterations);
        Assert.All(
            new[] { "steam", "header", "stop-out", "control-out", "turbine-inlet" },
            nodeId => Assert.Equal(100d, currentPlant.GetFluidNode(nodeId).Volume.CubicMetres, 12));
        var currentDrumInventory = currentEngine.CurrentState.PlantState.PlantState.GetFluidNode("drum");
        Assert.Equal(FluidPhase.SaturatedMixture, currentDrumInventory.Phase);
        Assert.NotNull(currentDrumInventory.VaporQuality);
        Assert.InRange(
            currentEngine.CurrentState.MeasuredSignals.GetSignal("level").EngineeringValue ?? double.NaN,
            0.49d,
            0.51d);
        var stageDefinition = Assert.Single(currentEngine.CurrentState.PlantDefinition.TurbineExpansionSystem.StageGroups);
        Assert.True(stageDefinition.ExpansionResistance.HasValue);
        Assert.Equal(0.86d, stageDefinition.Efficiency.Fraction, 12);
        Assert.Equal(
            21_400d,
            stageDefinition.ExpansionResistance.GetValueOrDefault().PascalSecondsSquaredPerKilogramSquared);
        var thermodynamicWork = Assert.IsType<NuclearReactorSimulator.Domain.Physics.TurbineIsland.Turbine.TurbineThermodynamicWorkDefinition>(
            stageDefinition.ThermodynamicWork);
        Assert.Equal(2.1d, thermodynamicWork.VaporSpecificHeatAtConstantPressure.KilojoulesPerKilogramKelvin, 12);
        Assert.Equal(1.3d, thermodynamicWork.HeatCapacityRatio, 12);
        Assert.Equal(0.8d, thermodynamicWork.MaximumInletInternalEnergyExtractionFraction, 12);
        Assert.Equal(TurbineAdmissionPhasePolicy.VaporMassFractionLimited, stageDefinition.AdmissionPhasePolicy);
        Assert.Equal(FluidEnergyTransportMode.SpecificEnthalpy, stageDefinition.EnergyTransportMode);
        var currentRotorDefinition = Assert.Single(currentEngine.CurrentState.PlantDefinition.TurbineExpansionSystem.Rotors);
        var currentMechanicalLoss = Assert.IsType<TurbineRotorMechanicalLossDefinition>(currentRotorDefinition.MechanicalLoss);
        Assert.Equal(0.5d, currentMechanicalLoss.RatedSpeedLossPower.Megawatts, 12);
        var currentDrum = Assert.Single(currentEngine.CurrentState.PlantDefinition
            .TurbineExpansionSystem.MainSteamNetwork.PrimaryCircuit.SteamDrumSystem.Drums);
        Assert.Equal(SteamDrumLiquidRecirculationMode.CirculationDemandBalanced, currentDrum.LiquidRecirculationMode);
        var currentSteamSource = Assert.IsType<SteamDrumSteamSourceDefinition>(currentDrum.SteamSource);
        Assert.Equal(
            100d,
            currentSteamSource.HydraulicResistance.PascalSecondsSquaredPerKilogramSquared,
            12);
        var currentCoreThermalLinks = currentEngine.CurrentState.PlantDefinition.PlantDefinition.HeatTransfers;
        Assert.Collection(
            currentCoreThermalLinks,
            link =>
            {
                Assert.Equal("fuel-to-coolant", link.Id);
                Assert.Equal("fuel", link.FromDomainId);
                Assert.Equal("outlet", link.ToDomainId);
                Assert.Equal(1d, link.Conductance.MegawattsPerKelvin, 12);
            },
            link =>
            {
                Assert.Equal("structure-to-coolant", link.Id);
                Assert.Equal("structure", link.FromDomainId);
                Assert.Equal("outlet", link.ToDomainId);
                Assert.Equal(0.5d, link.Conductance.MegawattsPerKelvin, 12);
            });
        var currentCondenserDefinition = Assert.Single(currentEngine.CurrentState.PlantDefinition
            .CondensateFeedwaterSystem.CondenserSystem.Condensers);
        Assert.True(currentCondenserDefinition.OverallHeatTransferConductance.HasValue);
        Assert.Equal(
            1.225d,
            currentCondenserDefinition.OverallHeatTransferConductance.GetValueOrDefault().MegawattsPerKelvin,
            12);
        Assert.Equal(20d, currentCondenserDefinition.MaximumCondensationMassFlowRate.KilogramsPerSecond, 12);
        var currentCoolingDefinition = Assert.Single(currentEngine.CurrentState.PlantDefinition
            .CondensateFeedwaterSystem.CondenserSystem.CoolingBoundaries);
        Assert.Equal(40d, currentCoolingDefinition.MaximumInstalledHeatRejectionPower.GetValueOrDefault().Megawatts, 12);
        Assert.Equal(
            CondenserCondensateEnergyMode.SaturatedLiquidAtSteamSpacePressure,
            currentCondenserDefinition.CondensateEnergyMode);
        var currentCoolingBoundary = Assert.Single(
            currentEngine.PersistentInputs
                .PlantInputs
                .GeneratorGridInputs
                .CondensateFeedwaterInputs
                .CondenserInputs
                .CoolingBoundaryInputs);
        Assert.Equal(40d, currentCoolingBoundary.AvailableHeatRejectionPower.Megawatts, 12);
        var currentGeneratorDefinition = Assert.Single(currentEngine.CurrentState.PlantDefinition.GeneratorGridSystem.Generators);
        var gridCoupling = Assert.IsType<NuclearReactorSimulator.Domain.Physics.Electrical.SynchronousGridCouplingDefinition>(
            currentGeneratorDefinition.GridCoupling);
        Assert.Equal(0.5d, gridCoupling.MaximumSynchronizingCorrectionPower.Megawatts, 12);
        Assert.Equal(2d, gridCoupling.FrequencyDampingPowerAtOneHertzSlip.Megawatts, 12);
        Assert.Equal(10d, currentGeneratorDefinition.MaximumElectricalPower.Megawatts, 12);
        Assert.Equal(
            NuclearReactorSimulator.Domain.Physics.Electrical.SynchronousGridPowerFlowMode.Bidirectional,
            gridCoupling.PowerFlowMode);
        Assert.True(currentEngine.CurrentState.PlantDefinition.PlantDefinition.GetPump("condensate-pump").HasDischargeCheckValve);
        Assert.True(currentEngine.CurrentState.PlantDefinition.PlantDefinition.GetPump("feedwater-pump").HasDischargeCheckValve);
        Assert.Equal(
            25d,
            currentEngine.CurrentState.PlantDefinition.PlantDefinition.GetPipe("channel").Resistance.PascalSecondsSquaredPerKilogramSquared,
            12);
        Assert.Equal(
            25d,
            currentEngine.CurrentState.PlantDefinition.PlantDefinition.GetPipe("return").Resistance.PascalSecondsSquaredPerKilogramSquared,
            12);
        Assert.Equal(
            25d,
            currentEngine.CurrentState.PlantDefinition.PlantDefinition.GetPump("pump").InternalResistance.PascalSecondsSquaredPerKilogramSquared,
            12);
        Assert.Equal(
            28d,
            currentEngine.PersistentInputs.TurbineSecondaryInputs.Controllers.GetController("speed-control").ManualOutput,
            12);
        var currentActuators = currentEngine.CurrentState.TurbineSecondaryControlState.Definition.ActuatorSystem;
        Assert.Equal(
            0.5d,
            currentActuators.GetActuator("speed-actuator").TravelRate.GetValueOrDefault().FractionPerSecond,
            12);
        Assert.Equal(
            0.5d,
            currentActuators.GetActuator("pressure-actuator").TravelRate.GetValueOrDefault().FractionPerSecond,
            12);
        Assert.Equal(
            0.25d,
            currentActuators.GetActuator("feedwater-actuator").TravelRate.GetValueOrDefault().FractionPerSecond,
            12);
        Assert.Equal(
            0.25d,
            currentActuators.GetActuator("condensate-actuator").TravelRate.GetValueOrDefault().FractionPerSecond,
            12);
        var currentGovernorDroop = Assert.IsType<NuclearReactorSimulator.Domain.Physics.Control.TurbineSecondary.TurbineGovernorDroopDefinition>(
            currentEngine.CurrentState.TurbineSecondaryControlState.Definition.GovernorDroop);
        Assert.Equal("speed-control", currentGovernorDroop.SpeedControllerId);
        Assert.Equal("generator", currentGovernorDroop.GeneratorId);
        Assert.Equal(1.5d, currentGovernorDroop.FullLoadSpeedReferenceRise.RevolutionsPerMinute, 12);

        var currentInstrumentation = currentEngine.CurrentState.InstrumentationState.Definition;
        Assert.Equal(
            TimeSpan.FromSeconds(0.5d),
            currentInstrumentation.GetChannel("primary-display-loop-loop-flow").LagTimeConstant);
        Assert.Equal(
            TimeSpan.FromSeconds(0.5d),
            currentInstrumentation.GetChannel("primary-display-pump-pump-flow").LagTimeConstant);
        Assert.Equal(
            TimeSpan.FromSeconds(0.5d),
            currentInstrumentation.GetChannel("primary-display-branch-loop-group-channel-flow").LagTimeConstant);
        Assert.Equal(
            TimeSpan.FromSeconds(0.5d),
            currentInstrumentation.GetChannel("primary-display-branch-loop-group-return-flow").LagTimeConstant);
        Assert.Equal(
            TimeSpan.FromSeconds(0.5d),
            currentInstrumentation.GetChannel("primary-display-drum-drum-a-inlet-flow").LagTimeConstant);
        Assert.Equal(
            TimeSpan.FromSeconds(0.5d),
            currentInstrumentation.GetChannel("primary-display-drum-drum-a-recirculation-flow").LagTimeConstant);

        var coordinator = new ControlRoomRuntimeCoordinator(currentEngine);
        var snapshot = coordinator.Current;
        var primaryLoop = Assert.Single(snapshot.PrimaryCircuit.Loops);
        var primaryPump = Assert.Single(primaryLoop.Pumps);
        var primaryBranch = Assert.Single(primaryLoop.Branches);
        var primaryDrum = Assert.Single(snapshot.PrimaryCircuit.SteamDrums);
        Assert.Equal(ControlRoomInstrumentProvenance.Measured, primaryLoop.TotalPumpFlow.Provenance);
        Assert.Equal(ControlRoomInstrumentProvenance.Measured, primaryPump.MassFlow.Provenance);
        Assert.Equal(ControlRoomInstrumentProvenance.Measured, primaryBranch.ChannelFlow.Provenance);
        Assert.Equal(ControlRoomInstrumentProvenance.Measured, primaryBranch.ReturnFlow.Provenance);
        Assert.Equal(ControlRoomInstrumentProvenance.Measured, primaryDrum.IncomingReturnFlow.Provenance);
        Assert.Equal(ControlRoomInstrumentProvenance.Measured, primaryDrum.RecirculationFlow.Provenance);

        var generator = Assert.Single(snapshot.Electrical.Generators);
        var steamLine = Assert.Single(snapshot.TurbineSecondary.SteamLines);
        var rotor = Assert.Single(snapshot.TurbineSecondary.Rotors);
        var stage = Assert.Single(snapshot.TurbineSecondary.StageGroups);
        var canonicalExpansion = currentEngine.LatestCanonicalSnapshot
            .Control
            .ProtectedControl
            .FullPlant
            .IntegratedCycle
            .TurbineExpansion;
        var canonicalTrain = Assert.Single(canonicalExpansion.MainSteamNetwork.AdmissionTrains);
        var canonicalMainSteamLine = Assert.Single(canonicalExpansion.MainSteamNetwork.SteamLines);
        var canonicalStage = Assert.Single(canonicalExpansion.StageGroups);
        var condenser = Assert.Single(snapshot.TurbineSecondary.Condensers);
        var feedwater = Assert.Single(snapshot.TurbineSecondary.FeedwaterTrains);

        Assert.True(generator.BreakerClosed);
        Assert.InRange(generator.RequestedElectricalPower.NumericValue ?? double.NaN, 4.9d, 5.1d);
        Assert.True((steamLine.MassFlow.NumericValue ?? 0d) > 10d);
        Assert.True((steamLine.PressureDifference.NumericValue ?? 0d) > 0d);
        Assert.InRange(rotor.Speed.NumericValue ?? double.NaN, 2_980d, 3_020d);
        Assert.InRange(stage.SteamFlow.NumericValue ?? double.NaN, 12.5d, 30d);
        Assert.Equal(
            850d,
            currentEngine.CurrentState.PlantDefinition.PlantDefinition
                .GetPipe(canonicalMainSteamLine.PipeId)
                .Resistance.PascalSecondsSquaredPerKilogramSquared,
            9);
        Assert.InRange(canonicalTrain.StopValve.PressureDifference.Kilopascals, 150d, 250d);
        Assert.InRange(canonicalTrain.StopValve.MassFlowRate.KilogramsPerSecond, 12.5d, 14d);
        Assert.InRange(canonicalTrain.ControlValve.MassFlowRate.KilogramsPerSecond, 12.5d, 14.5d);
        var stopToControlFlowMismatchKilogramsPerSecond = Math.Abs(
            canonicalTrain.StopValve.MassFlowRate.KilogramsPerSecond
            - canonicalTrain.ControlValve.MassFlowRate.KilogramsPerSecond);
        Assert.True(
            stopToControlFlowMismatchKilogramsPerSecond <= 0.5d,
            FormattableString.Invariant(
                $"Initial stop/control flow mismatch must be at most 0.5 kg/s; stop={canonicalTrain.StopValve.MassFlowRate.KilogramsPerSecond:F6} kg/s, control={canonicalTrain.ControlValve.MassFlowRate.KilogramsPerSecond:F6} kg/s, mismatch={stopToControlFlowMismatchKilogramsPerSecond:F6} kg/s."));
        Assert.True(
            canonicalTrain.AdmissionValve.MassFlowRate.KilogramsPerSecond
            > canonicalTrain.StopValve.MassFlowRate.KilogramsPerSecond,
            FormattableString.Invariant(
                $"Initial admission-valve capacity must exceed stop-valve inflow; admission={canonicalTrain.AdmissionValve.MassFlowRate.KilogramsPerSecond:F6} kg/s, stop={canonicalTrain.StopValve.MassFlowRate.KilogramsPerSecond:F6} kg/s."));
        Assert.InRange(canonicalStage.CommandedMassFlowRate.KilogramsPerSecond, 12.5d, 14d);
        Assert.True(stage.ThermodynamicWorkModelActive);
        Assert.InRange(stage.AvailableSpecificWork.NumericValue ?? double.NaN, 450d, 500d);
        Assert.InRange(stage.ExtractedSpecificWork.NumericValue ?? double.NaN, 350d, 450d);
        Assert.InRange(rotor.ShaftPower.NumericValue ?? double.NaN, 4.5d, 20d);
        Assert.InRange(snapshot.TurbineSecondary.EffectiveTurbineSteamFlow.NumericValue ?? double.NaN, 12.5d, 30d);
        Assert.True((condenser.CondensationFlow.NumericValue ?? 0d) > 10d);
        Assert.True((condenser.CondensateSpecificInternalEnergy.NumericValue ?? double.NaN) >= 0d);
        Assert.True((condenser.SpecificCondensationEnergyDrop.NumericValue ?? double.NaN) > 0d);
        Assert.False(string.IsNullOrWhiteSpace(condenser.CondensationLimitStatus));
        Assert.Equal(40d, condenser.InstalledCoolingCapacity.NumericValue ?? double.NaN, 9);
        Assert.Equal(40d, condenser.AvailableCoolingCapacity.NumericValue ?? double.NaN, 9);
        Assert.True((condenser.SurfaceHeatTransferLimit.NumericValue ?? 0d) > 0d);
        Assert.False(string.IsNullOrWhiteSpace(condenser.HeatRejectionLimitStatus));
        Assert.True((feedwater.CondensatePump.MassFlow.NumericValue ?? 0d) > 10d);
        Assert.True((feedwater.FeedwaterPump.MassFlow.NumericValue ?? 0d) > 10d);
    }

    [Fact]
    public void Version2_TurbineExpansionDrainKeepsAdmissionTrainInventoryBounded()
    {
        var engine = Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(
            new DesktopSustainedGenerationInitialConditionFactory().CreateRuntimeEngine());
        var initialTrainInventoryKilograms = AdmissionTrainInventoryKilograms(engine);
        Assert.True(initialTrainInventoryKilograms > 0d);
        var coordinator = new ControlRoomRuntimeCoordinator(engine);
        var sampledInventories = new List<double> { initialTrainInventoryKilograms };

        coordinator.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.Run));
        for (var step = 0; step < 200; step++)
        {
            _ = coordinator.AdvanceRunning(stepCount: 1, publicationStride: 1);
            sampledInventories.Add(AdmissionTrainInventoryKilograms(engine));
        }

        var finalTrainInventoryKilograms = sampledInventories[^1];
        Assert.InRange(
            finalTrainInventoryKilograms,
            0.95d * initialTrainInventoryKilograms,
            1.05d * initialTrainInventoryKilograms);

        // The historical min(stop, control, admission) stage law produced a material admission-train inventory
        // ratchet. The current aggregated 100 m³ steam-path nodes intentionally smooth pressure transients, so a
        // sign change is no longer a meaningful two-second contract. Bound the accumulated drift tightly instead.
        var inventoryDeltas = sampledInventories
            .Zip(sampledInventories.Skip(1), static (before, after) => after - before)
            .ToArray();
        var relativeInventoryDrift = Math.Abs(finalTrainInventoryKilograms - initialTrainInventoryKilograms)
            / initialTrainInventoryKilograms;
        Assert.True(
            relativeInventoryDrift < 0.0001d,
            FormattableString.Invariant(
                $"Pressure-driven turbine drain must keep two-second admission-train inventory drift below 0.01%; drift={100d * relativeInventoryDrift:F6}%, minimum delta={inventoryDeltas.Min():E6} kg, maximum delta={inventoryDeltas.Max():E6} kg, initial={initialTrainInventoryKilograms:F6} kg, final={finalTrainInventoryKilograms:F6} kg."));

        var admissionTrain = Assert.Single(coordinator.Current.TurbineSecondary.AdmissionTrains);
        var stage = Assert.Single(coordinator.Current.TurbineSecondary.StageGroups);
        var admissionFlow = admissionTrain.AdmissionFlow.NumericValue ?? double.NaN;
        var stageFlow = stage.SteamFlow.NumericValue ?? double.NaN;
        Assert.True(double.IsFinite(admissionFlow));
        Assert.True(double.IsFinite(stageFlow));
        Assert.InRange(admissionFlow, 10d, 30d);
        Assert.InRange(stageFlow, 10d, 30d);
    }


    [Fact]
    public void Version2_EnablesMeaningfulSecondaryProtectionsWhileLegacyVersion1RemainsMinimal()
    {
        var legacyEngine = Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(
            new DesktopIntegratedOperationsInitialConditionFactory().CreateRuntimeEngine());
        var currentEngine = Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(
            new DesktopSustainedGenerationInitialConditionFactory().CreateRuntimeEngine());

        var legacyProtection = legacyEngine.CurrentState.ProtectionState.Definition;
        Assert.Single(legacyProtection.TripFunctions);
        Assert.Throws<KeyNotFoundException>(() => legacyProtection.GetTripFunction("turbine-overspeed"));
        Assert.Throws<KeyNotFoundException>(() => legacyProtection.GetTripFunction("condenser-high-backpressure"));
        Assert.Throws<KeyNotFoundException>(() => legacyProtection.GetTripFunction("generator-overfrequency"));
        Assert.Throws<KeyNotFoundException>(() => legacyProtection.GetTripFunction("steam-drum-low-low-level"));
        Assert.Throws<KeyNotFoundException>(() => legacyEngine.CurrentState.AlarmState.Definition.GetAlarm("steam-drum-level-low"));

        var expectedMeasuredChannelIds = currentEngine.CurrentState.InstrumentationDefinition.Channels
            .Select(static channel => channel.Id)
            .OrderBy(static id => id, StringComparer.Ordinal)
            .ToArray();
        var actualMeasuredChannelIds = currentEngine.CurrentState.MeasuredSignals.Signals
            .Select(static signal => signal.ChannelId)
            .OrderBy(static id => id, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(expectedMeasuredChannelIds, actualMeasuredChannelIds);
        Assert.InRange(
            currentEngine.CurrentState.MeasuredSignals.GetSignal("condenser-pressure").EngineeringValue ?? double.NaN,
            1_000d,
            20_000d);
        var currentGenerator = Assert.Single(currentEngine.CurrentState.PlantDefinition.GeneratorGridSystem.Generators);
        var currentRotorState = Assert.Single(currentEngine.CurrentState.PlantState.TurbineState.Rotors);
        var expectedGeneratorFrequencyHertz = currentGenerator.ElectricalFrequencyAt(currentRotorState.AngularSpeed).Hertz;
        Assert.Equal(
            expectedGeneratorFrequencyHertz,
            currentEngine.CurrentState.MeasuredSignals.GetSignal("generator-frequency").EngineeringValue ?? double.NaN,
            9);
        Assert.InRange(expectedGeneratorFrequencyHertz, 49.9d, 50.1d);
        Assert.Equal(
            1d,
            currentEngine.CurrentState.MeasuredSignals.GetSignal("generator-breaker-closed").EngineeringValue ?? double.NaN,
            12);
        Assert.InRange(
            currentEngine.CurrentState.MeasuredSignals.GetSignal("generator-absolute-frequency-slip").EngineeringValue ?? double.NaN,
            0d,
            0.2d);

        var currentProtection = currentEngine.CurrentState.ProtectionState.Definition;
        Assert.Equal(8, currentProtection.TripFunctions.Count);

        AssertProtection(
            currentProtection.GetTripFunction("turbine-overspeed"),
            "speed",
            ProtectionComparison.High,
            3_300d,
            3_150d,
            ProtectionAction.TurbineTrip | ProtectionAction.GeneratorTrip);
        AssertProtection(
            currentProtection.GetTripFunction("condenser-high-backpressure"),
            "condenser-pressure",
            ProtectionComparison.High,
            30_000d,
            20_000d,
            ProtectionAction.TurbineTrip | ProtectionAction.GeneratorTrip);
        AssertProtection(
            currentProtection.GetTripFunction("generator-overfrequency"),
            "generator-frequency",
            ProtectionComparison.High,
            53d,
            51.5d,
            ProtectionAction.GeneratorTrip);
        AssertProtection(
            currentProtection.GetTripFunction("steam-drum-low-low-level"),
            "level",
            ProtectionComparison.Low,
            0.10d,
            0.20d,
            ProtectionAction.ReactorScram | ProtectionAction.TurbineTrip | ProtectionAction.GeneratorTrip);

        var lowLevelAlarm = currentEngine.CurrentState.AlarmState.Definition.GetAlarm("steam-drum-level-low");
        var lowLevelCondition = Assert.IsType<MeasuredAlarmConditionDefinition>(lowLevelAlarm.Condition);
        Assert.Equal(AlarmSeverity.Warning, lowLevelAlarm.Severity);
        Assert.Equal(AlarmLatchingMode.NonLatching, lowLevelAlarm.LatchingMode);
        Assert.Equal("level", lowLevelCondition.MeasurementChannelId);
        Assert.Equal(AlarmComparison.Low, lowLevelCondition.Comparison);
        Assert.Equal(0.25d, lowLevelCondition.Threshold, 12);

        Assert.False(new ControlRoomRuntimeCoordinator(currentEngine).Current.AnyTripActive);
    }

    [Fact]
    public void Version2_SecondaryProtectionFunctionsLatchFromMeasuredSignals()
    {
        var engine = Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(
            new DesktopSustainedGenerationInitialConditionFactory().CreateRuntimeEngine());
        var definition = engine.CurrentState.ProtectionState.Definition;
        var solver = new ProtectionSystemSolver(definition);

        var cases = new[]
        {
            new ProtectionTriggerCase(
                "turbine-overspeed",
                "speed",
                3_350d,
                ProtectionAction.TurbineTrip | ProtectionAction.GeneratorTrip),
            new ProtectionTriggerCase(
                "condenser-high-backpressure",
                "condenser-pressure",
                35_000d,
                ProtectionAction.TurbineTrip | ProtectionAction.GeneratorTrip),
            new ProtectionTriggerCase(
                "generator-overfrequency",
                "generator-frequency",
                54d,
                ProtectionAction.GeneratorTrip),
            new ProtectionTriggerCase(
                "steam-drum-low-low-level",
                "level",
                0.05d,
                ProtectionAction.ReactorScram | ProtectionAction.TurbineTrip | ProtectionAction.GeneratorTrip),
        };

        foreach (var testCase in cases)
        {
            var signals = ReplaceMeasuredSignal(
                engine.CurrentState.MeasuredSignals,
                testCase.ChannelId,
                testCase.EngineeringValue);
            var result = solver.Step(
                signals,
                ProtectionSystemState.CreateInitial(definition),
                new ProtectionSystemInputs(definition));

            Assert.True(result.CandidateState.IsFunctionLatched(testCase.FunctionId));
            Assert.Equal(
                testCase.ExpectedActions,
                result.Snapshot.LatchedActions & testCase.ExpectedActions);
        }
    }

    [Fact]
    public void Version2_ProductionRuntimeRetainsTenMillisecondFixedStep()
    {
        var engine = Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(
            new DesktopSustainedGenerationInitialConditionFactory().CreateRuntimeEngine());

        Assert.Equal(TimeSpan.FromMilliseconds(10d), engine.FixedDeltaTime);
    }

    [Fact]
    public void Version2_NumericalStiffnessEvidenceRuntimeCanUseDeterministicFiveMillisecondSubstep()
    {
        var engine = Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(
            DesktopSustainedGenerationInitialConditionFactory.CreateNumericalStiffnessEvidenceRuntimeEngine(
                TimeSpan.FromMilliseconds(5d)));

        Assert.Equal(TimeSpan.FromMilliseconds(5d), engine.FixedDeltaTime);
        Assert.Equal(0, engine.LogicalStep);

        var snapshot = engine.Step(ControlRoomRunState.Running);
        Assert.Equal(1, snapshot.LogicalStep);
        Assert.False(snapshot.AnyTripActive);
    }

    private static void AssertProtection(
        ProtectionFunctionDefinition definition,
        string channelId,
        ProtectionComparison comparison,
        double tripThreshold,
        double resetThreshold,
        ProtectionAction actions)
    {
        Assert.Equal(channelId, definition.MeasurementChannelId);
        Assert.Equal(comparison, definition.Comparison);
        Assert.Equal(tripThreshold, definition.TripThreshold, 12);
        Assert.Equal(resetThreshold, definition.ResetThreshold, 12);
        Assert.Equal(actions, definition.Actions);
    }

    private static MeasuredSignalFrame ReplaceMeasuredSignal(
        MeasuredSignalFrame source,
        string channelId,
        double engineeringValue)
        => new(
            source.Definition,
            source.Signals.Select(signal => signal.ChannelId == channelId
                ? signal with { EngineeringValue = engineeringValue, ScaledValue = engineeringValue }
                : signal));

    private sealed record ProtectionTriggerCase(
        string FunctionId,
        string ChannelId,
        double EngineeringValue,
        ProtectionAction ExpectedActions);

    private static double AdmissionTrainInventoryKilograms(IntegratedAutomaticOperationRuntimeEngine engine)
    {
        var plant = engine.CurrentState.PlantState.PlantState;
        return plant.GetFluidNode("stop-out").Mass.Kilograms
            + plant.GetFluidNode("control-out").Mass.Kilograms
            + plant.GetFluidNode("turbine-inlet").Mass.Kilograms;
    }
}
