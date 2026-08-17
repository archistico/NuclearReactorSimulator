using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.TurbineIsland.Turbine;
using NuclearReactorSimulator.Domain.Plant;
using NuclearReactorSimulator.Simulation.Physics.Fluids;
using NuclearReactorSimulator.Simulation.Physics.Thermal;
using NuclearReactorSimulator.Simulation.Physics.TurbineIsland.MainSteam;
using NuclearReactorSimulator.Simulation.Plant;

namespace NuclearReactorSimulator.Simulation.Physics.TurbineIsland.Turbine;

/// <summary>
/// Deterministic M4.2 committed-state turbine expansion and rotor solver.
/// Steam expansion is staged as an internal inlet-to-exhaust mass transfer plus explicit thermofluid-to-shaft energy transfer.
/// Plant inventories are still integrated exactly once by the inherited M3/M4 network boundary; rotor kinetic energy is integrated once here.
/// </summary>
public sealed class TurbineExpansionSolver
{
    private readonly TurbineExpansionSystemDefinition _definition;
    private readonly MainSteamNetworkSolver _mainSteamSolver;

    public TurbineExpansionSolver(
        TurbineExpansionSystemDefinition definition,
        IFluidThermodynamicModel thermodynamicModel)
    {
        _definition = definition ?? throw new ArgumentNullException(nameof(definition));
        ArgumentNullException.ThrowIfNull(thermodynamicModel);
        _mainSteamSolver = new MainSteamNetworkSolver(definition.MainSteamNetwork, thermodynamicModel);
    }

    public TurbineExpansionSystemDefinition Definition => _definition;

    public TurbineExpansionStepResult Step(
        PlantState committedPlantState,
        TurbineExpansionState committedTurbineState,
        TurbineExpansionInputs inputs,
        TimeSpan deltaTime)
        => Step(committedPlantState, committedTurbineState, inputs, deltaTime, PlantNetworkSourceTerms.Empty);

    /// <summary>
    /// Higher M4 composition seam. Condenser/feedwater/generator phases may stage additional thermofluid source terms
    /// before the same single plant-network integration boundary while this solver remains the sole rotor-state integrator.
    /// </summary>
    public TurbineExpansionStepResult Step(
        PlantState committedPlantState,
        TurbineExpansionState committedTurbineState,
        TurbineExpansionInputs inputs,
        TimeSpan deltaTime,
        PlantNetworkSourceTerms supplementalSourceTerms)
    {
        ArgumentNullException.ThrowIfNull(committedPlantState);
        ArgumentNullException.ThrowIfNull(committedTurbineState);
        ArgumentNullException.ThrowIfNull(inputs);
        ArgumentNullException.ThrowIfNull(supplementalSourceTerms);

        if (deltaTime <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(deltaTime), deltaTime, "Turbine expansion step time must be greater than zero.");
        }

        if (!ReferenceEquals(committedPlantState.Definition, _definition.PlantDefinition))
        {
            throw new ArgumentException("Committed plant state does not use the turbine expansion system's canonical plant definition.", nameof(committedPlantState));
        }

        if (!ReferenceEquals(committedTurbineState.Definition, _definition))
        {
            throw new ArgumentException("Committed turbine state does not use this solver's canonical turbine expansion definition.", nameof(committedTurbineState));
        }

        if (!ReferenceEquals(inputs.Definition, _definition))
        {
            throw new ArgumentException("Turbine expansion inputs do not use this solver's canonical definition.", nameof(inputs));
        }

        var seconds = deltaTime.TotalSeconds;
        var stageWorking = BuildStageWorkingSet(committedPlantState, committedTurbineState, inputs);
        var rotorWorking = SolveRotors(committedTurbineState, inputs, stageWorking, seconds);
        var stageSolution = SolveStages(committedPlantState, stageWorking, rotorWorking);
        var sourceTerms = PlantNetworkSourceTerms.Combine(
            BuildSourceTerms(stageSolution),
            supplementalSourceTerms);
        var mainSteamStep = _mainSteamSolver.Step(
            committedPlantState,
            inputs.MainSteamInputs,
            deltaTime,
            sourceTerms);

        var candidateTurbineState = new TurbineExpansionState(
            _definition,
            rotorWorking.Values
                .OrderBy(static item => item.Definition.Id, StringComparer.Ordinal)
                .Select(static item => new TurbineRotorState(item.Definition.Id, item.FinalAngularSpeed)));
        var rotorSnapshots = rotorWorking.Values
            .OrderBy(static item => item.Definition.Id, StringComparer.Ordinal)
            .Select(BuildRotorSnapshot)
            .ToArray();
        var mechanicalAudit = BuildMechanicalAudit(rotorSnapshots, deltaTime);
        var snapshot = new TurbineExpansionSnapshot(
            _definition,
            mainSteamStep.Snapshot,
            stageSolution.Select(static item => item.Snapshot),
            rotorSnapshots,
            mechanicalAudit);

        return new TurbineExpansionStepResult(mainSteamStep, candidateTurbineState, snapshot);
    }

    private IReadOnlyDictionary<string, StageWorking> BuildStageWorkingSet(
        PlantState committedPlantState,
        TurbineExpansionState committedState,
        TurbineExpansionInputs inputs)
    {
        var result = new Dictionary<string, StageWorking>(StringComparer.Ordinal);
        foreach (var stage in _definition.StageGroups)
        {
            var rotor = _definition.GetRotor(stage.RotorId);
            var rotorState = committedState.GetRotor(rotor.Id);
            var rotorInput = inputs.GetRotorInput(rotor.Id);
            var stageInput = inputs.GetStageGroupInput(stage.Id);
            var boundary = _definition.MainSteamNetwork.GetTurbineAdmissionBoundary(stage.AdmissionBoundaryId);
            var inlet = committedPlantState.GetFluidNode(boundary.SourceNodeId);
            var exhaust = committedPlantState.GetFluidNode(stage.ExhaustNodeId);
            var admissionVaporFraction = ResolveAdmissionVaporFraction(stage, inlet);
            var phaseLimitedFlow = MassFlowRate.FromKilogramsPerSecond(
                stageInput.MassFlowRate.KilogramsPerSecond * admissionVaporFraction);
            var effectiveFlow = rotorInput.TripCommand ? MassFlowRate.Zero : phaseLimitedFlow;
            var specificWork = ResolveSpecificWork(stage, inlet, exhaust, admissionVaporFraction);
            var availableShaftPower = specificWork.EffectiveIdealSpecificWork
                * effectiveFlow
                * stage.Efficiency.Fraction;
            var torqueReferenceSpeed = stage.ThermodynamicWork is null
                ? rotor.RatedAngularSpeed
                : AngularSpeed.FromRadiansPerSecond(Math.Max(
                    rotor.RatedAngularSpeed.RadiansPerSecond,
                    rotorState.AngularSpeed.RadiansPerSecond));
            var torque = Torque.FromNewtonMetres(
                availableShaftPower.Watts / torqueReferenceSpeed.RadiansPerSecond);

            result.Add(
                stage.Id,
                new StageWorking(
                    stage,
                    rotorState,
                    rotorInput,
                    stageInput.MassFlowRate,
                    effectiveFlow,
                    specificWork,
                    torque));
        }

        return result;
    }

    private static SpecificWorkResolution ResolveSpecificWork(
        TurbineStageGroupDefinition stage,
        FluidNodeState inlet,
        FluidNodeState exhaust,
        double admissionVaporFraction)
    {
        if (stage.ThermodynamicWork is not { } work)
        {
            return new SpecificWorkResolution(
                false,
                stage.NominalSpecificWork,
                stage.NominalSpecificWork,
                stage.NominalSpecificWork,
                false);
        }

        // Under the current-v2 vapor-fraction-limited policy, mass flow has already been reduced to the admitted vapor
        // fraction. Work is therefore evaluated per kilogram of admitted vapor rather than applying vapor quality twice.
        // Legacy definitions preserve the historical specific-work degradation by inlet vapor mass fraction.
        var workVaporFraction = stage.AdmissionPhasePolicy == TurbineAdmissionPhasePolicy.VaporMassFractionLimited
            ? (admissionVaporFraction > 0d ? 1d : 0d)
            : inlet.Thermodynamics.VaporMassFraction ?? 0d;
        var pressureTemperatureAvailableJoulesPerKilogram = 0d;
        if (workVaporFraction > 0d && inlet.Pressure > exhaust.Pressure)
        {
            var pressureRatio = Math.Clamp(
                exhaust.Pressure.Pascals / inlet.Pressure.Pascals,
                1e-12d,
                1d);
            var isentropicExponent = (work.HeatCapacityRatio - 1d) / work.HeatCapacityRatio;
            var idealTemperatureDropFraction = 1d - Math.Pow(pressureRatio, isentropicExponent);
            pressureTemperatureAvailableJoulesPerKilogram =
                work.VaporSpecificHeatAtConstantPressure.JoulesPerKilogramKelvin
                * inlet.Temperature.Kelvins
                * idealTemperatureDropFraction
                * workVaporFraction;
        }

        var pressureTemperatureAvailable = SpecificEnergy.FromJoulesPerKilogram(
            Math.Max(0d, pressureTemperatureAvailableJoulesPerKilogram));
        var inletEnergyBounded = SpecificEnergy.FromJoulesPerKilogram(
            Math.Max(
                0d,
                inlet.SpecificInternalEnergy.JoulesPerKilogram
                    * work.MaximumInletInternalEnergyExtractionFraction));
        var effectiveIdeal = SpecificEnergy.FromJoulesPerKilogram(Math.Min(
            stage.NominalSpecificWork.JoulesPerKilogram,
            Math.Min(
                pressureTemperatureAvailable.JoulesPerKilogram,
                inletEnergyBounded.JoulesPerKilogram)));
        var limited = effectiveIdeal.JoulesPerKilogram
            < stage.NominalSpecificWork.JoulesPerKilogram - 1e-9d;

        return new SpecificWorkResolution(
            true,
            pressureTemperatureAvailable,
            inletEnergyBounded,
            effectiveIdeal,
            limited);
    }


    private static double ResolveAdmissionVaporFraction(
        TurbineStageGroupDefinition stage,
        FluidNodeState inlet)
    {
        if (stage.AdmissionPhasePolicy == TurbineAdmissionPhasePolicy.LegacyUnrestricted)
        {
            return 1d;
        }

        return Math.Clamp(inlet.Thermodynamics.VaporMassFraction ?? 0d, 0d, 1d);
    }

    private IReadOnlyDictionary<string, RotorWorking> SolveRotors(
        TurbineExpansionState committedState,
        TurbineExpansionInputs inputs,
        IReadOnlyDictionary<string, StageWorking> stageWorking,
        double seconds)
    {
        var result = new Dictionary<string, RotorWorking>(StringComparer.Ordinal);
        foreach (var rotor in _definition.Rotors)
        {
            var rotorState = committedState.GetRotor(rotor.Id);
            var rotorInput = inputs.GetRotorInput(rotor.Id);
            var turbineTorque = Torque.FromNewtonMetres(
                stageWorking.Values
                    .Where(item => string.Equals(item.Definition.RotorId, rotor.Id, StringComparison.Ordinal))
                    .OrderBy(static item => item.Definition.Id, StringComparer.Ordinal)
                    .Sum(static item => item.ShaftTorque.NewtonMetres));

            var passiveMechanicalLossTorque = rotor.MechanicalLoss?.ResolveTorque(
                    rotorState.AngularSpeed,
                    rotor.RatedAngularSpeed)
                ?? Torque.Zero;
            var maximumLoadWithoutReverse = turbineTorque.NewtonMetres
                + (rotor.MomentOfInertia.KilogramSquareMetres * rotorState.AngularSpeed.RadiansPerSecond / seconds);
            var effectiveExternalLoadNewtonMetres = Math.Min(
                rotorInput.ExternalLoadTorque.NewtonMetres,
                maximumLoadWithoutReverse);
            var remainingLoadCapacityNewtonMetres = Math.Max(
                0d,
                maximumLoadWithoutReverse - effectiveExternalLoadNewtonMetres);
            var effectivePassiveLossNewtonMetres = Math.Min(
                passiveMechanicalLossTorque.NewtonMetres,
                remainingLoadCapacityNewtonMetres);
            var loadLimitedAtZeroSpeed = rotorInput.ExternalLoadTorque.NewtonMetres > effectiveExternalLoadNewtonMetres;
            var passiveLossLimitedAtZeroSpeed = passiveMechanicalLossTorque.NewtonMetres > effectivePassiveLossNewtonMetres;
            var effectiveLoadTorque = Torque.FromNewtonMetres(effectiveExternalLoadNewtonMetres);
            var effectivePassiveLossTorque = Torque.FromNewtonMetres(effectivePassiveLossNewtonMetres);
            var netTorque = turbineTorque - effectiveLoadTorque - effectivePassiveLossTorque;
            var finalRadiansPerSecond = rotorState.AngularSpeed.RadiansPerSecond
                + (netTorque.NewtonMetres / rotor.MomentOfInertia.KilogramSquareMetres * seconds);

            // When the anti-reverse limiter is active, its analytical solution is exactly zero speed.
            // Canonicalize only round-off around that constrained stop; do not erase genuine low-speed motion.
            if ((loadLimitedAtZeroSpeed || passiveLossLimitedAtZeroSpeed)
                && Math.Abs(finalRadiansPerSecond) <= 1e-12d)
            {
                finalRadiansPerSecond = 0d;
            }

            if (!double.IsFinite(finalRadiansPerSecond) || finalRadiansPerSecond < 0d)
            {
                throw new InvalidOperationException($"Turbine rotor '{rotor.Id}' integration produced an invalid angular speed.");
            }

            var finalAngularSpeed = AngularSpeed.FromRadiansPerSecond(finalRadiansPerSecond);
            var averageAngularSpeed = AngularSpeed.FromRadiansPerSecond(
                0.5d * (rotorState.AngularSpeed.RadiansPerSecond + finalRadiansPerSecond));
            var shaftPower = turbineTorque.At(averageAngularSpeed);
            var loadPower = effectiveLoadTorque.At(averageAngularSpeed);
            var passiveMechanicalLossPower = effectivePassiveLossTorque.At(averageAngularSpeed);

            result.Add(
                rotor.Id,
                new RotorWorking(
                    rotor,
                    rotorState,
                    rotorInput,
                    turbineTorque,
                    effectiveLoadTorque,
                    effectivePassiveLossTorque,
                    netTorque,
                    finalAngularSpeed,
                    averageAngularSpeed,
                    shaftPower,
                    loadPower,
                    passiveMechanicalLossPower,
                    loadLimitedAtZeroSpeed,
                    passiveLossLimitedAtZeroSpeed));
        }

        return result;
    }

    private IReadOnlyList<StageSolution> SolveStages(
        PlantState committedPlantState,
        IReadOnlyDictionary<string, StageWorking> stageWorking,
        IReadOnlyDictionary<string, RotorWorking> rotorWorking)
    {
        var result = new List<StageSolution>(_definition.StageGroups.Count);
        foreach (var stage in _definition.StageGroups)
        {
            var working = stageWorking[stage.Id];
            var rotor = rotorWorking[stage.RotorId];
            var boundary = _definition.MainSteamNetwork.GetTurbineAdmissionBoundary(stage.AdmissionBoundaryId);
            var inlet = committedPlantState.GetFluidNode(boundary.SourceNodeId);
            SpecificEnergy inletSpecificFlowWork;
            SpecificEnergy inletSpecificEnthalpy;
            SpecificEnergy inletAdvectedSpecificEnergy;
            if (inlet.Density > Density.Zero)
            {
                inletSpecificFlowWork = FluidEnergyTransport.ResolveSpecificFlowWork(inlet.Pressure, inlet.Density);
                inletSpecificEnthalpy = FluidEnergyTransport.ResolveSpecificEnthalpy(
                    inlet.SpecificInternalEnergy,
                    inlet.Pressure,
                    inlet.Density);
                inletAdvectedSpecificEnergy = FluidEnergyTransport.ResolveSelectedSpecificEnergy(
                    stage.EnergyTransportMode,
                    inlet);
            }
            else if (working.EffectiveMassFlowRate == MassFlowRate.Zero)
            {
                // Empty/zero-density inlet with zero turbine flow carries no advected energy. Preserve legacy/extreme
                // no-flow resolvability instead of requiring an undefined p/rho diagnostic.
                inletSpecificFlowWork = SpecificEnergy.Zero;
                inletSpecificEnthalpy = inlet.SpecificInternalEnergy;
                inletAdvectedSpecificEnergy = inlet.SpecificInternalEnergy;
            }
            else
            {
                throw new InvalidOperationException(
                    $"Turbine stage group '{stage.Id}' cannot advect positive mass flow from a zero-density inlet.");
            }
            var inletEnergyFlow = inletAdvectedSpecificEnergy * working.EffectiveMassFlowRate;
            var shaftPower = working.ShaftTorque.At(rotor.AverageAngularSpeed);
            var extractedSpecificWork = working.EffectiveMassFlowRate == MassFlowRate.Zero
                ? SpecificEnergy.Zero
                : SpecificEnergy.FromJoulesPerKilogram(
                    shaftPower.Watts / working.EffectiveMassFlowRate.KilogramsPerSecond);

            // The current thermodynamic-work law remains deliberately unchanged in G.4 and is still bounded by the
            // historical inlet-internal-energy extraction fraction. This guard therefore remains valid for both modes.
            if (extractedSpecificWork > inlet.SpecificInternalEnergy)
            {
                throw new InvalidOperationException(
                    $"Turbine stage group '{stage.Id}' would extract {extractedSpecificWork.KilojoulesPerKilogram:F3} kJ/kg from an inlet containing only {inlet.SpecificInternalEnergy.KilojoulesPerKilogram:F3} kJ/kg internal energy.");
            }

            // Retain the historical diagnostic for backward-compatible snapshots. Under SpecificEnthalpy this value is
            // not the advected exhaust term; the canonical G.4 transport term is exposed separately below.
            var exhaustSpecificInternalEnergy = SpecificEnergy.FromJoulesPerKilogram(
                inlet.SpecificInternalEnergy.JoulesPerKilogram - extractedSpecificWork.JoulesPerKilogram);
            var exhaustAdvectedSpecificEnergy = SpecificEnergy.FromJoulesPerKilogram(
                inletAdvectedSpecificEnergy.JoulesPerKilogram - extractedSpecificWork.JoulesPerKilogram);
            if (exhaustAdvectedSpecificEnergy < SpecificEnergy.Zero)
            {
                throw new InvalidOperationException(
                    $"Turbine stage group '{stage.Id}' produced a negative advected exhaust specific energy.");
            }

            var exhaustEnergyFlow = exhaustAdvectedSpecificEnergy * working.EffectiveMassFlowRate;
            if (exhaustEnergyFlow < Power.Zero)
            {
                throw new InvalidOperationException($"Turbine stage group '{stage.Id}' produced a negative exhaust energy flow.");
            }

            var flowWorkRate = inletSpecificFlowWork * working.EffectiveMassFlowRate;
            var ownershipResidual = inletEnergyFlow - exhaustEnergyFlow - shaftPower;
            var snapshot = new TurbineStageGroupSnapshot(
                stage.Id,
                stage.AdmissionBoundaryId,
                boundary.SourceNodeId,
                stage.ExhaustNodeId,
                stage.RotorId,
                working.RotorInput.TripCommand,
                working.CommandedMassFlowRate,
                working.EffectiveMassFlowRate,
                inlet.Pressure,
                inlet.Temperature,
                inlet.Phase,
                inlet.VaporQuality,
                inlet.SpecificInternalEnergy,
                committedPlantState.GetFluidNode(stage.ExhaustNodeId).Pressure,
                committedPlantState.GetFluidNode(stage.ExhaustNodeId).Temperature,
                working.SpecificWork.ThermodynamicWorkModelActive,
                working.SpecificWork.PressureTemperatureAvailableSpecificWork,
                working.SpecificWork.InletEnergyBoundedSpecificWork,
                working.SpecificWork.EffectiveIdealSpecificWork,
                working.SpecificWork.ThermodynamicWorkLimited,
                stage.NominalSpecificWork,
                extractedSpecificWork,
                exhaustSpecificInternalEnergy,
                inletEnergyFlow,
                exhaustEnergyFlow,
                shaftPower,
                working.ShaftTorque)
            {
                EnergyTransportMode = stage.EnergyTransportMode,
                InletSpecificFlowWork = inletSpecificFlowWork,
                InletSpecificEnthalpy = inletSpecificEnthalpy,
                InletAdvectedSpecificEnergy = inletAdvectedSpecificEnergy,
                ExhaustAdvectedSpecificEnergy = exhaustAdvectedSpecificEnergy,
                FlowWorkRate = flowWorkRate,
                TurbineEnergyOwnershipResidual = ownershipResidual,
            };
            result.Add(new StageSolution(snapshot));
        }

        return result;
    }

    private static PlantNetworkSourceTerms BuildSourceTerms(IEnumerable<StageSolution> stageSolutions)
    {
        var balances = new Dictionary<string, FluidNodeBalance>(StringComparer.Ordinal);
        var totalShaftPower = Power.Zero;

        foreach (var solution in stageSolutions.OrderBy(static item => item.Snapshot.StageGroupId, StringComparer.Ordinal))
        {
            var stage = solution.Snapshot;
            AddBalance(
                balances,
                stage.InletNodeId,
                new FluidNodeBalance(-stage.EffectiveMassFlowRate, -stage.InletEnergyFlowRate));
            AddBalance(
                balances,
                stage.ExhaustNodeId,
                new FluidNodeBalance(stage.EffectiveMassFlowRate, stage.ExhaustEnergyFlowRate));
            totalShaftPower += stage.ShaftPower;
        }

        return new PlantNetworkSourceTerms(
            balances,
            new Dictionary<string, ThermalEnergyBalance>(StringComparer.Ordinal),
            MassFlowRate.Zero,
            -totalShaftPower);
    }

    private static TurbineRotorSnapshot BuildRotorSnapshot(RotorWorking rotor)
    {
        return new TurbineRotorSnapshot(
            rotor.Definition.Id,
            rotor.Definition.MomentOfInertia,
            rotor.CommittedState.AngularSpeed,
            rotor.FinalAngularSpeed,
            rotor.AverageAngularSpeed,
            rotor.Definition.RatedAngularSpeed,
            rotor.Definition.OverspeedThreshold,
            rotor.TurbineTorque,
            rotor.Input.ExternalLoadTorque,
            rotor.EffectiveExternalLoadTorque,
            rotor.NetTorque,
            rotor.ShaftPower,
            rotor.ExternalLoadPower,
            rotor.Definition.MomentOfInertia.KineticEnergyAt(rotor.CommittedState.AngularSpeed),
            rotor.Definition.MomentOfInertia.KineticEnergyAt(rotor.FinalAngularSpeed),
            rotor.Input.TripCommand,
            rotor.CommittedState.AngularSpeed >= rotor.Definition.OverspeedThreshold,
            rotor.FinalAngularSpeed >= rotor.Definition.OverspeedThreshold,
            rotor.ExternalLoadTorqueLimitedAtZeroSpeed)
        {
            PassiveMechanicalLossTorque = rotor.PassiveMechanicalLossTorque,
            PassiveMechanicalLossPower = rotor.PassiveMechanicalLossPower,
            PassiveMechanicalLossTorqueLimitedAtZeroSpeed = rotor.PassiveMechanicalLossTorqueLimitedAtZeroSpeed,
        };
    }

    private static TurbineMechanicalAudit BuildMechanicalAudit(
        IEnumerable<TurbineRotorSnapshot> rotorSnapshots,
        TimeSpan deltaTime)
    {
        var canonical = rotorSnapshots.OrderBy(static item => item.RotorId, StringComparer.Ordinal).ToArray();
        var initialEnergy = Energy.FromJoules(canonical.Sum(static item => item.InitialKineticEnergy.Joules));
        var finalEnergy = Energy.FromJoules(canonical.Sum(static item => item.FinalKineticEnergy.Joules));
        var shaftPower = Power.FromWatts(canonical.Sum(static item => item.ShaftPower.Watts));
        var loadPower = Power.FromWatts(canonical.Sum(static item => item.ExternalLoadPower.Watts));
        var passiveMechanicalLossPower = Power.FromWatts(
            canonical.Sum(static item => item.PassiveMechanicalLossPower.Watts));
        var expectedDeltaJoules = (shaftPower - loadPower - passiveMechanicalLossPower).Over(deltaTime).Joules;
        var actualDeltaJoules = finalEnergy.Joules - initialEnergy.Joules;

        return new TurbineMechanicalAudit(
            initialEnergy,
            finalEnergy,
            shaftPower,
            loadPower,
            actualDeltaJoules - expectedDeltaJoules)
        {
            TotalPassiveMechanicalLossPower = passiveMechanicalLossPower,
        };
    }

    private static void AddBalance(
        IDictionary<string, FluidNodeBalance> balances,
        string nodeId,
        FluidNodeBalance balance)
    {
        balances[nodeId] = balances.TryGetValue(nodeId, out var existing)
            ? existing + balance
            : balance;
    }

    private sealed record StageWorking(
        TurbineStageGroupDefinition Definition,
        TurbineRotorState RotorState,
        TurbineRotorInput RotorInput,
        MassFlowRate CommandedMassFlowRate,
        MassFlowRate EffectiveMassFlowRate,
        SpecificWorkResolution SpecificWork,
        Torque ShaftTorque);

    private sealed record SpecificWorkResolution(
        bool ThermodynamicWorkModelActive,
        SpecificEnergy PressureTemperatureAvailableSpecificWork,
        SpecificEnergy InletEnergyBoundedSpecificWork,
        SpecificEnergy EffectiveIdealSpecificWork,
        bool ThermodynamicWorkLimited);

    private sealed record RotorWorking(
        TurbineRotorDefinition Definition,
        TurbineRotorState CommittedState,
        TurbineRotorInput Input,
        Torque TurbineTorque,
        Torque EffectiveExternalLoadTorque,
        Torque PassiveMechanicalLossTorque,
        Torque NetTorque,
        AngularSpeed FinalAngularSpeed,
        AngularSpeed AverageAngularSpeed,
        Power ShaftPower,
        Power ExternalLoadPower,
        Power PassiveMechanicalLossPower,
        bool ExternalLoadTorqueLimitedAtZeroSpeed,
        bool PassiveMechanicalLossTorqueLimitedAtZeroSpeed);

    private sealed record StageSolution(TurbineStageGroupSnapshot Snapshot);
}
