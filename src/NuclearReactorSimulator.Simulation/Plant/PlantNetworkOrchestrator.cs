using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.Thermal;
using NuclearReactorSimulator.Domain.Plant;
using NuclearReactorSimulator.Simulation.Physics.Fluids;
using NuclearReactorSimulator.Simulation.Physics.Thermal;

namespace NuclearReactorSimulator.Simulation.Plant;

/// <summary>
/// Deterministically orchestrates one composed-plant network step.
/// Every component reads the same committed state, all balances are accumulated first,
/// and each conserved inventory is integrated exactly once after solving is complete.
/// </summary>
public sealed class PlantNetworkOrchestrator
{
    private readonly PipeFlowSolver _pipeFlowSolver;
    private readonly ValveFlowSolver _valveFlowSolver;
    private readonly PumpFlowSolver _pumpFlowSolver;
    private readonly HeatTransferSolver _heatTransferSolver;
    private readonly HeatSourceSolver _heatSourceSolver;
    private readonly FluidNodeIntegrator _fluidNodeIntegrator;
    private readonly ThermalBodyIntegrator _thermalBodyIntegrator;
    private readonly HybridSemiImplicitHydraulicGateSolver? _hybridHydraulicSolver;
    private readonly FourNodeBranchContinuityShadowIntegrationSolver? _fourNodeShadowIntegrationSolver;
    private readonly Func<FourNodeBranchContinuityActivationDecision, FourNodeBranchContinuityActivationDecision>? _fourNodeAuthorityDecisionTransform;
    private readonly FourNodeBranchContinuityCorrectedCommitSeam _fourNodeCorrectedCommitSeam = new();

    public PlantNetworkOrchestrator(IFluidThermodynamicModel thermodynamicModel)
        : this(
            new PipeFlowSolver(),
            new ValveFlowSolver(),
            new PumpFlowSolver(),
            new HeatTransferSolver(),
            new HeatSourceSolver(),
            new FluidNodeIntegrator(thermodynamicModel),
            new ThermalBodyIntegrator(),
            new HybridSemiImplicitHydraulicGateSolver(thermodynamicModel),
            thermodynamicModel is IWaterSteamInverseBranchDiagnosticProvider diagnosticProvider
                ? new FourNodeBranchContinuityShadowIntegrationSolver(thermodynamicModel, diagnosticProvider)
                : null,
            fourNodeAuthorityDecisionTransform: null)
    {
    }

    /// <summary>
    /// H.26 test-only seam for exercising fail-closed consumption of already-typed H.20 authority decisions
    /// inside the real orchestrator. The public production constructor always supplies a null transform.
    /// </summary>
    internal PlantNetworkOrchestrator(
        IFluidThermodynamicModel thermodynamicModel,
        Func<FourNodeBranchContinuityActivationDecision, FourNodeBranchContinuityActivationDecision> fourNodeAuthorityDecisionTransform)
        : this(
            new PipeFlowSolver(),
            new ValveFlowSolver(),
            new PumpFlowSolver(),
            new HeatTransferSolver(),
            new HeatSourceSolver(),
            new FluidNodeIntegrator(thermodynamicModel),
            new ThermalBodyIntegrator(),
            new HybridSemiImplicitHydraulicGateSolver(thermodynamicModel),
            thermodynamicModel is IWaterSteamInverseBranchDiagnosticProvider diagnosticProvider
                ? new FourNodeBranchContinuityShadowIntegrationSolver(thermodynamicModel, diagnosticProvider)
                : null,
            fourNodeAuthorityDecisionTransform)
    {
        ArgumentNullException.ThrowIfNull(fourNodeAuthorityDecisionTransform);
    }

    internal PlantNetworkOrchestrator(
        PipeFlowSolver pipeFlowSolver,
        ValveFlowSolver valveFlowSolver,
        PumpFlowSolver pumpFlowSolver,
        HeatTransferSolver heatTransferSolver,
        HeatSourceSolver heatSourceSolver,
        FluidNodeIntegrator fluidNodeIntegrator,
        ThermalBodyIntegrator thermalBodyIntegrator,
        HybridSemiImplicitHydraulicGateSolver? hybridHydraulicSolver = null,
        FourNodeBranchContinuityShadowIntegrationSolver? fourNodeShadowIntegrationSolver = null,
        Func<FourNodeBranchContinuityActivationDecision, FourNodeBranchContinuityActivationDecision>? fourNodeAuthorityDecisionTransform = null)
    {
        _pipeFlowSolver = pipeFlowSolver ?? throw new ArgumentNullException(nameof(pipeFlowSolver));
        _valveFlowSolver = valveFlowSolver ?? throw new ArgumentNullException(nameof(valveFlowSolver));
        _pumpFlowSolver = pumpFlowSolver ?? throw new ArgumentNullException(nameof(pumpFlowSolver));
        _heatTransferSolver = heatTransferSolver ?? throw new ArgumentNullException(nameof(heatTransferSolver));
        _heatSourceSolver = heatSourceSolver ?? throw new ArgumentNullException(nameof(heatSourceSolver));
        _fluidNodeIntegrator = fluidNodeIntegrator ?? throw new ArgumentNullException(nameof(fluidNodeIntegrator));
        _thermalBodyIntegrator = thermalBodyIntegrator ?? throw new ArgumentNullException(nameof(thermalBodyIntegrator));
        _hybridHydraulicSolver = hybridHydraulicSolver;
        _fourNodeShadowIntegrationSolver = fourNodeShadowIntegrationSolver;
        _fourNodeAuthorityDecisionTransform = fourNodeAuthorityDecisionTransform;
    }

    public PlantNetworkStepResult Step(PlantState committedState, TimeSpan deltaTime)
        => Step(committedState, deltaTime, PlantNetworkSourceTerms.Empty);

    public PlantNetworkStepResult Step(
        PlantState committedState,
        TimeSpan deltaTime,
        PlantNetworkSourceTerms sourceTerms)
    {
        ArgumentNullException.ThrowIfNull(committedState);
        ArgumentNullException.ThrowIfNull(sourceTerms);

        if (deltaTime <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(deltaTime), deltaTime, "Plant-network step time must be greater than zero.");
        }

        if (committedState.Definition.HydraulicNumericalCoupling.Mode
            == HydraulicNumericalCouplingMode.DeterministicHybridSemiImplicit)
        {
            return StepDeterministicHybrid(committedState, deltaTime, sourceTerms);
        }

        if (committedState.Definition.HydraulicNumericalCoupling.Mode
            == HydraulicNumericalCouplingMode.FourNodeBranchContinuityShadowIntegrated)
        {
            return StepFourNodeBranchContinuityIntegrated(
                committedState,
                deltaTime,
                sourceTerms,
                correctedCommitOptIn: false);
        }

        if (committedState.Definition.HydraulicNumericalCoupling.Mode
            == HydraulicNumericalCouplingMode.FourNodeBranchContinuityCorrectedCommitOptIn)
        {
            return StepFourNodeBranchContinuityIntegrated(
                committedState,
                deltaTime,
                sourceTerms,
                correctedCommitOptIn: true);
        }

        var definition = committedState.Definition;
        var committedFluidNodes = committedState.FluidNodes.ToDictionary(static item => item.Id, StringComparer.Ordinal);
        var committedThermalBodies = committedState.ThermalBodies.ToDictionary(static item => item.Id, StringComparer.Ordinal);
        var valveStates = committedState.Valves.ToDictionary(static item => item.ValveId, StringComparer.Ordinal);
        var pumpStates = committedState.Pumps.ToDictionary(static item => item.PumpId, StringComparer.Ordinal);
        var heatSourceStates = committedState.HeatSources.ToDictionary(static item => item.HeatSourceId, StringComparer.Ordinal);

        var fluidBalances = definition.FluidNodes.ToDictionary(
            static item => item.Id,
            static _ => FluidNodeBalance.Zero,
            StringComparer.Ordinal);
        var thermalBalances = definition.ThermalBodies.ToDictionary(
            static item => item.Id,
            static _ => ThermalEnergyBalance.Zero,
            StringComparer.Ordinal);

        ValidateSourceTermTargets(sourceTerms, fluidBalances, thermalBalances);
        AccumulateSourceTerms(sourceTerms, fluidBalances, thermalBalances);

        var pumpHydraulicPowerExchange = Power.Zero;
        var heatSourcePower = Power.Zero;

        foreach (var pipe in definition.Pipes)
        {
            var result = _pipeFlowSolver.Solve(
                pipe,
                committedFluidNodes[pipe.FromNodeId],
                committedFluidNodes[pipe.ToNodeId]);
            AccumulateHydraulicBalances(pipe.FromNodeId, pipe.ToNodeId, result.FromNodeBalance, result.ToNodeBalance, fluidBalances);
        }

        foreach (var valve in definition.Valves)
        {
            var result = _valveFlowSolver.Solve(
                valve,
                valveStates[valve.Id],
                committedFluidNodes[valve.Pipe.FromNodeId],
                committedFluidNodes[valve.Pipe.ToNodeId]);
            AccumulateHydraulicBalances(valve.Pipe.FromNodeId, valve.Pipe.ToNodeId, result.FromNodeBalance, result.ToNodeBalance, fluidBalances);
        }

        foreach (var pump in definition.Pumps)
        {
            var result = _pumpFlowSolver.Solve(
                pump,
                pumpStates[pump.Id],
                committedFluidNodes[pump.Pipe.FromNodeId],
                committedFluidNodes[pump.Pipe.ToNodeId]);
            AccumulateHydraulicBalances(pump.Pipe.FromNodeId, pump.Pipe.ToNodeId, result.FromNodeBalance, result.ToNodeBalance, fluidBalances);
            pumpHydraulicPowerExchange += result.HydraulicPowerExchange;
        }

        foreach (var heatTransfer in definition.HeatTransfers)
        {
            var result = _heatTransferSolver.Solve(
                heatTransfer,
                ResolveCommittedTemperature(heatTransfer.FromDomainId, committedFluidNodes, committedThermalBodies),
                ResolveCommittedTemperature(heatTransfer.ToDomainId, committedFluidNodes, committedThermalBodies));

            AccumulateThermalDomainBalance(
                heatTransfer.FromDomainId,
                result.FromDomainBalance,
                fluidBalances,
                thermalBalances);
            AccumulateThermalDomainBalance(
                heatTransfer.ToDomainId,
                result.ToDomainBalance,
                fluidBalances,
                thermalBalances);
        }

        foreach (var heatSource in definition.HeatSources)
        {
            var balance = _heatSourceSolver.Solve(heatSource, heatSourceStates[heatSource.Id]);
            AccumulateThermalDomainBalance(heatSource.TargetDomainId, balance, fluidBalances, thermalBalances);
            heatSourcePower += balance.NetHeatRate;
        }

        var candidateFluidNodes = committedState.FluidNodes
            .Select(state => _fluidNodeIntegrator.Step(state, fluidBalances[state.Id], deltaTime))
            .ToArray();
        var candidateThermalBodies = committedState.ThermalBodies
            .Select(state => _thermalBodyIntegrator.Step(state, thermalBalances[state.Id], deltaTime))
            .ToArray();

        var candidateState = new PlantState(
            definition,
            candidateFluidNodes,
            committedState.Valves,
            committedState.Pumps,
            candidateThermalBodies,
            committedState.HeatSources);

        var audit = BuildAudit(
            committedState,
            candidateState,
            fluidBalances,
            thermalBalances,
            pumpHydraulicPowerExchange,
            heatSourcePower,
            sourceTerms.ExternalMassFlowRate,
            sourceTerms.ExternalPower,
            deltaTime);

        return new PlantNetworkStepResult(candidateState, audit, fluidBalances, thermalBalances);
    }


    private PlantNetworkStepResult StepFourNodeBranchContinuityIntegrated(
        PlantState committedState,
        TimeSpan deltaTime,
        PlantNetworkSourceTerms sourceTerms,
        bool correctedCommitOptIn)
    {
        var orchestratorStartedTicks = PerformanceAttributionMeasurement.ReadTimestamp();
        var orchestratorAllocatedBefore = PerformanceAttributionMeasurement.ReadAllocatedBytes();
        var explicitPreparationStartedTicks = orchestratorStartedTicks;
        var explicitPreparationAllocatedBefore = orchestratorAllocatedBefore;
        var integrationSolver = _fourNodeShadowIntegrationSolver
            ?? throw new InvalidOperationException(
                "Four-node branch-continuity integration requires the simplified water/steam diagnostic provider.");
        var definition = committedState.Definition;
        var coupling = definition.HydraulicNumericalCoupling;
        var committedFluidNodes = committedState.FluidNodes.ToDictionary(static item => item.Id, StringComparer.Ordinal);
        var committedThermalBodies = committedState.ThermalBodies.ToDictionary(static item => item.Id, StringComparer.Ordinal);
        var valveStates = committedState.Valves.ToDictionary(static item => item.ValveId, StringComparer.Ordinal);
        var pumpStates = committedState.Pumps.ToDictionary(static item => item.PumpId, StringComparer.Ordinal);
        var heatSourceStates = committedState.HeatSources.ToDictionary(static item => item.HeatSourceId, StringComparer.Ordinal);

        // The historical explicit path is always evaluated first and remains the immediate fail-closed fallback.
        // The separate non-hydraulic dictionary lets the H.19/H.20 evaluator reconstruct its qualified candidate.
        var fluidBalances = definition.FluidNodes.ToDictionary(
            static item => item.Id,
            static _ => FluidNodeBalance.Zero,
            StringComparer.Ordinal);
        var historicalHydraulicBalances = new SortedDictionary<string, FluidNodeBalance>(StringComparer.Ordinal);
        foreach (var node in definition.FluidNodes)
        {
            historicalHydraulicBalances.Add(node.Id, FluidNodeBalance.Zero);
        }
        var historicalPipeFlows = new SortedDictionary<string, MassFlowRate>(StringComparer.Ordinal);
        var historicalValveFlows = new SortedDictionary<string, MassFlowRate>(StringComparer.Ordinal);
        var historicalPumpFlows = new SortedDictionary<string, MassFlowRate>(StringComparer.Ordinal);
        var nonHydraulicFluidBalances = definition.FluidNodes.ToDictionary(
            static item => item.Id,
            static _ => FluidNodeBalance.Zero,
            StringComparer.Ordinal);
        var thermalBalances = definition.ThermalBodies.ToDictionary(
            static item => item.Id,
            static _ => ThermalEnergyBalance.Zero,
            StringComparer.Ordinal);

        ValidateSourceTermTargets(sourceTerms, fluidBalances, thermalBalances);
        AccumulateSourceTerms(sourceTerms, fluidBalances, thermalBalances);
        foreach (var entry in sourceTerms.FluidNodeBalances)
        {
            nonHydraulicFluidBalances[entry.Key] += entry.Value;
        }

        var pumpHydraulicPowerExchange = Power.Zero;
        foreach (var pipe in definition.Pipes)
        {
            var result = _pipeFlowSolver.Solve(
                pipe,
                committedFluidNodes[pipe.FromNodeId],
                committedFluidNodes[pipe.ToNodeId]);
            AccumulateHydraulicBalances(
                pipe.FromNodeId,
                pipe.ToNodeId,
                result.FromNodeBalance,
                result.ToNodeBalance,
                fluidBalances);
            AccumulateHydraulicBalances(
                pipe.FromNodeId,
                pipe.ToNodeId,
                result.FromNodeBalance,
                result.ToNodeBalance,
                historicalHydraulicBalances);
            historicalPipeFlows.Add(pipe.Id, result.MassFlowRate);
        }

        foreach (var valve in definition.Valves)
        {
            var result = _valveFlowSolver.Solve(
                valve,
                valveStates[valve.Id],
                committedFluidNodes[valve.Pipe.FromNodeId],
                committedFluidNodes[valve.Pipe.ToNodeId]);
            AccumulateHydraulicBalances(
                valve.Pipe.FromNodeId,
                valve.Pipe.ToNodeId,
                result.FromNodeBalance,
                result.ToNodeBalance,
                fluidBalances);
            AccumulateHydraulicBalances(
                valve.Pipe.FromNodeId,
                valve.Pipe.ToNodeId,
                result.FromNodeBalance,
                result.ToNodeBalance,
                historicalHydraulicBalances);
            historicalValveFlows.Add(valve.Id, result.MassFlowRate);
        }

        foreach (var pump in definition.Pumps)
        {
            var result = _pumpFlowSolver.Solve(
                pump,
                pumpStates[pump.Id],
                committedFluidNodes[pump.Pipe.FromNodeId],
                committedFluidNodes[pump.Pipe.ToNodeId]);
            AccumulateHydraulicBalances(
                pump.Pipe.FromNodeId,
                pump.Pipe.ToNodeId,
                result.FromNodeBalance,
                result.ToNodeBalance,
                fluidBalances);
            AccumulateHydraulicBalances(
                pump.Pipe.FromNodeId,
                pump.Pipe.ToNodeId,
                result.FromNodeBalance,
                result.ToNodeBalance,
                historicalHydraulicBalances);
            historicalPumpFlows.Add(pump.Id, result.MassFlowRate);
            pumpHydraulicPowerExchange += result.HydraulicPowerExchange;
        }

        foreach (var heatTransfer in definition.HeatTransfers)
        {
            var result = _heatTransferSolver.Solve(
                heatTransfer,
                ResolveCommittedTemperature(heatTransfer.FromDomainId, committedFluidNodes, committedThermalBodies),
                ResolveCommittedTemperature(heatTransfer.ToDomainId, committedFluidNodes, committedThermalBodies));

            AccumulateThermalDomainBalance(
                heatTransfer.FromDomainId,
                result.FromDomainBalance,
                fluidBalances,
                thermalBalances);
            AccumulateThermalDomainBalance(
                heatTransfer.ToDomainId,
                result.ToDomainBalance,
                fluidBalances,
                thermalBalances);
            AccumulateFluidDomainBalanceIfPresent(
                heatTransfer.FromDomainId,
                result.FromDomainBalance,
                nonHydraulicFluidBalances);
            AccumulateFluidDomainBalanceIfPresent(
                heatTransfer.ToDomainId,
                result.ToDomainBalance,
                nonHydraulicFluidBalances);
        }

        var heatSourcePower = Power.Zero;
        foreach (var heatSource in definition.HeatSources)
        {
            var balance = _heatSourceSolver.Solve(heatSource, heatSourceStates[heatSource.Id]);
            AccumulateThermalDomainBalance(
                heatSource.TargetDomainId,
                balance,
                fluidBalances,
                thermalBalances);
            AccumulateFluidDomainBalanceIfPresent(
                heatSource.TargetDomainId,
                balance,
                nonHydraulicFluidBalances);
            heatSourcePower += balance.NetHeatRate;
        }

        var historicalHydraulicMassRateClosure = Math.Abs(CompensatedSum(
            committedState.FluidNodes.Select(
                node => historicalHydraulicBalances[node.Id].NetMassFlowRate.KilogramsPerSecond)));
        var historicalHydraulicEnergyRate = CompensatedSum(
            committedState.FluidNodes.Select(
                node => historicalHydraulicBalances[node.Id].NetEnergyRate.Watts));
        var historicalHydraulicEvaluation = new SemiImplicitHydraulicEvaluation(
            historicalHydraulicBalances,
            historicalPipeFlows,
            historicalValveFlows,
            historicalPumpFlows,
            pumpHydraulicPowerExchange,
            historicalHydraulicMassRateClosure,
            Math.Abs(historicalHydraulicEnergyRate - pumpHydraulicPowerExchange.Watts));

        // Materialize the historical explicit fluid-node candidate exactly once. H.28.1-B may reuse
        // individual nodes inside the H.4 predictor only when the historical total balance is exactly
        // equal to the canonical H.4 total balance; otherwise that node is reintegrated by the predictor.
        var explicitCandidateFluidNodes = committedState.FluidNodes
            .Select(state => _fluidNodeIntegrator.Step(state, fluidBalances[state.Id], deltaTime))
            .ToArray();
        var historicalExplicitPredictorCandidateState = new PlantState(
            definition,
            explicitCandidateFluidNodes,
            committedState.Valves,
            committedState.Pumps,
            committedState.ThermalBodies,
            committedState.HeatSources);

        var explicitPreparationElapsedTicks = PerformanceAttributionMeasurement.ReadTimestamp() - explicitPreparationStartedTicks;
        var explicitPreparationAllocatedBytes = Math.Max(0L, PerformanceAttributionMeasurement.ReadAllocatedBytes() - explicitPreparationAllocatedBefore);
        var integration = integrationSolver.StepWithHistoricalExplicitPredictorCandidate(
            committedState,
            deltaTime,
            nonHydraulicFluidBalances,
            historicalHydraulicEvaluation,
            historicalExplicitPredictorCandidateState,
            fluidBalances);
        var commitAndAccountingStartedTicks = PerformanceAttributionMeasurement.ReadTimestamp();
        var commitAndAccountingAllocatedBefore = PerformanceAttributionMeasurement.ReadAllocatedBytes();

        var candidateThermalBodies = committedState.ThermalBodies
            .Select(state => _thermalBodyIntegrator.Step(state, thermalBalances[state.Id], deltaTime))
            .ToArray();
        var explicitCandidateState = new PlantState(
            definition,
            explicitCandidateFluidNodes,
            committedState.Valves,
            committedState.Pumps,
            candidateThermalBodies,
            committedState.HeatSources);

        var authorityDecision = _fourNodeAuthorityDecisionTransform is null
            ? integration.AuthorityDecision
            : _fourNodeAuthorityDecisionTransform(integration.AuthorityDecision)
                ?? throw new InvalidOperationException("H.26 authority-decision transform returned null.");
        var commitDecision = _fourNodeCorrectedCommitSeam.Evaluate(
            authorityDecision,
            integration.ShadowCorrectionEvaluated,
            integration.CorrectedCandidate is not null,
            correctedCommitOptIn);

        var correctedCandidateCommitted = commitDecision.CommitAuthorized;
        PlantState candidateState;
        IReadOnlyDictionary<string, FluidNodeBalance> appliedFluidBalances;
        Power appliedPumpHydraulicPowerExchange;
        if (correctedCandidateCommitted)
        {
            var corrected = integration.CorrectedCandidate
                ?? throw new InvalidOperationException("H.22 authorized a corrected commit without a corrected candidate.");
            var correctedFluidBalances = CombineAppliedFluidBalances(
                definition,
                corrected.AppliedHydraulicBalances,
                nonHydraulicFluidBalances);
            candidateState = new PlantState(
                definition,
                corrected.CandidateState.FluidNodes,
                committedState.Valves,
                committedState.Pumps,
                candidateThermalBodies,
                committedState.HeatSources);
            appliedFluidBalances = correctedFluidBalances;
            appliedPumpHydraulicPowerExchange = corrected.AppliedPumpHydraulicPowerExchange;
        }
        else
        {
            candidateState = explicitCandidateState;
            appliedFluidBalances = fluidBalances;
            appliedPumpHydraulicPowerExchange = pumpHydraulicPowerExchange;
        }

        var audit = BuildAudit(
            committedState,
            candidateState,
            appliedFluidBalances,
            thermalBalances,
            appliedPumpHydraulicPowerExchange,
            heatSourcePower,
            sourceTerms.ExternalMassFlowRate,
            sourceTerms.ExternalPower,
            deltaTime);

        var commitAndAccountingElapsedTicks = PerformanceAttributionMeasurement.ReadTimestamp() - commitAndAccountingStartedTicks;
        var commitAndAccountingAllocatedBytes = Math.Max(0L, PerformanceAttributionMeasurement.ReadAllocatedBytes() - commitAndAccountingAllocatedBefore);
        var orchestratorElapsedTicks = PerformanceAttributionMeasurement.ReadTimestamp() - orchestratorStartedTicks;
        var orchestratorAllocatedBytes = Math.Max(0L, PerformanceAttributionMeasurement.ReadAllocatedBytes() - orchestratorAllocatedBefore);
        FourNodeBranchContinuitySidecarPerformanceAttributionRegistry.TryGet(integration, out var integrationPerformance);
        var performance = integrationPerformance is null
            ? null
            : integrationPerformance with
            {
                OrchestratorElapsedTicks = orchestratorElapsedTicks,
                OrchestratorAllocatedBytes = orchestratorAllocatedBytes,
                HistoricalExplicitPreparationElapsedTicks = explicitPreparationElapsedTicks,
                HistoricalExplicitPreparationAllocatedBytes = explicitPreparationAllocatedBytes,
                CommitAndAccountingElapsedTicks = commitAndAccountingElapsedTicks,
                CommitAndAccountingAllocatedBytes = commitAndAccountingAllocatedBytes,
            };

        var decision = authorityDecision;
        var telemetry = new FourNodeBranchContinuityIntegrationTelemetry(
            decision.TriggerObserved,
            integration.ShadowCorrectionEvaluated,
            decision.ProposedAuthority,
            decision.Reason,
            decision.RollbackRequired,
            decision.ShadowCorrectedCandidateEligible,
            correctedCandidateCommitted,
            integration.UntargetedBranchDisagreementDetected,
            integration.BranchOverrideCount,
            integration.PreviousPhaseHoldCount,
            integration.HysteresisReleaseCount,
            integration.ShadowIterationCount,
            integration.ShadowConverged,
            integration.ShadowLineSearchExhausted,
            integration.ShadowMaximumRelativePressureResidual,
            integration.ShadowMaximumAbsoluteFlowResidualKilogramsPerSecond,
            integration.ShadowMassClosureKilogramsPerSecond,
            integration.ShadowEnergyOwnershipResidualWatts)
        {
            CorrectedCommitArmEnabled = correctedCommitOptIn,
            CorrectedCommitAuthorized = commitDecision.CommitAuthorized,
            CorrectedCommitReason = commitDecision.Reason,
        };
        if (performance is not null)
        {
            FourNodeBranchContinuityPerformanceAttributionRegistry.Set(telemetry, performance);
        }
        var numericalSnapshot = new PlantNetworkHydraulicNumericalSnapshot(
            coupling.Mode,
            UsedSemiImplicitCorrection: correctedCandidateCommitted,
            IterationCount: correctedCandidateCommitted ? integration.ShadowIterationCount : 1,
            Converged: !integration.ShadowCorrectionEvaluated || integration.ShadowConverged,
            integration.Predictor.PredictorMaximumFractionalSubcooledPressureChange,
            integration.Predictor.PredictorMaximumAbsoluteHydraulicFlowChangeKilogramsPerSecond,
            MaximumRelativePressureResidual: correctedCandidateCommitted
                ? integration.ShadowMaximumRelativePressureResidual
                : 0d,
            MaximumAbsoluteFlowResidualKilogramsPerSecond: correctedCandidateCommitted
                ? integration.ShadowMaximumAbsoluteFlowResidualKilogramsPerSecond
                : 0d)
        {
            FourNodeBranchContinuity = telemetry,
        };

        return new PlantNetworkStepResult(
            candidateState,
            audit,
            appliedFluidBalances,
            thermalBalances,
            numericalSnapshot);
    }


    private PlantNetworkStepResult StepDeterministicHybrid(
        PlantState committedState,
        TimeSpan deltaTime,
        PlantNetworkSourceTerms sourceTerms)
    {
        var hybridSolver = _hybridHydraulicSolver
            ?? throw new InvalidOperationException(
                "Hybrid hydraulic coupling requires an orchestrator constructed with a thermodynamic model.");
        var definition = committedState.Definition;
        var coupling = definition.HydraulicNumericalCoupling;
        var committedFluidNodes = committedState.FluidNodes.ToDictionary(static item => item.Id, StringComparer.Ordinal);
        var committedThermalBodies = committedState.ThermalBodies.ToDictionary(static item => item.Id, StringComparer.Ordinal);
        var heatSourceStates = committedState.HeatSources.ToDictionary(static item => item.HeatSourceId, StringComparer.Ordinal);

        var nonHydraulicFluidBalances = definition.FluidNodes.ToDictionary(
            static item => item.Id,
            static _ => FluidNodeBalance.Zero,
            StringComparer.Ordinal);
        var thermalBalances = definition.ThermalBodies.ToDictionary(
            static item => item.Id,
            static _ => ThermalEnergyBalance.Zero,
            StringComparer.Ordinal);

        ValidateSourceTermTargets(sourceTerms, nonHydraulicFluidBalances, thermalBalances);
        AccumulateSourceTerms(sourceTerms, nonHydraulicFluidBalances, thermalBalances);

        foreach (var heatTransfer in definition.HeatTransfers)
        {
            var result = _heatTransferSolver.Solve(
                heatTransfer,
                ResolveCommittedTemperature(heatTransfer.FromDomainId, committedFluidNodes, committedThermalBodies),
                ResolveCommittedTemperature(heatTransfer.ToDomainId, committedFluidNodes, committedThermalBodies));

            AccumulateThermalDomainBalance(
                heatTransfer.FromDomainId,
                result.FromDomainBalance,
                nonHydraulicFluidBalances,
                thermalBalances);
            AccumulateThermalDomainBalance(
                heatTransfer.ToDomainId,
                result.ToDomainBalance,
                nonHydraulicFluidBalances,
                thermalBalances);
        }

        var heatSourcePower = Power.Zero;
        foreach (var heatSource in definition.HeatSources)
        {
            var balance = _heatSourceSolver.Solve(heatSource, heatSourceStates[heatSource.Id]);
            AccumulateThermalDomainBalance(
                heatSource.TargetDomainId,
                balance,
                nonHydraulicFluidBalances,
                thermalBalances);
            heatSourcePower += balance.NetHeatRate;
        }

        var options = new HybridSemiImplicitHydraulicGateOptions(
            coupling.PredictedSubcooledPressureChangeTriggerFraction,
            coupling.PredictedHydraulicFlowChangeTriggerKilogramsPerSecond,
            new SemiImplicitHydraulicPrototypeOptions(
                coupling.MaximumCorrectorIterations,
                coupling.CorrectorRelaxationFactor,
                coupling.CorrectorRelativePressureTolerance,
                coupling.CorrectorAbsoluteFlowToleranceKilogramsPerSecond));
        var hybrid = hybridSolver.Step(committedState, deltaTime, nonHydraulicFluidBalances, options);

        if (hybrid.UsedSemiImplicitCorrection && !hybrid.Converged)
        {
            throw new InvalidOperationException(
                $"Deterministic hybrid hydraulic corrector did not converge within {hybrid.IterationCount} iterations.");
        }

        var fluidBalances = definition.FluidNodes.ToDictionary(
            static item => item.Id,
            item => hybrid.AppliedHydraulicBalances[item.Id] + nonHydraulicFluidBalances[item.Id],
            StringComparer.Ordinal);
        var candidateThermalBodies = committedState.ThermalBodies
            .Select(state => _thermalBodyIntegrator.Step(state, thermalBalances[state.Id], deltaTime))
            .ToArray();
        var candidateState = new PlantState(
            definition,
            hybrid.CandidateState.FluidNodes,
            committedState.Valves,
            committedState.Pumps,
            candidateThermalBodies,
            committedState.HeatSources);

        // The hydraulic balance set actually integrated owns the pump fluid-work contribution. Pipes and valves
        // are internally conservative, so the signed sum of applied hydraulic node-energy balances is precisely
        // the pump hydraulic power exchange required by the plant-network energy audit, including relaxed corrector steps.
        var appliedPumpHydraulicPowerExchange = Power.FromWatts(
            CompensatedSum(
                hybrid.AppliedHydraulicBalances
                    .OrderBy(static item => item.Key, StringComparer.Ordinal)
                    .Select(static item => item.Value.NetEnergyRate.Watts)));
        var audit = BuildAudit(
            committedState,
            candidateState,
            fluidBalances,
            thermalBalances,
            appliedPumpHydraulicPowerExchange,
            heatSourcePower,
            sourceTerms.ExternalMassFlowRate,
            sourceTerms.ExternalPower,
            deltaTime);
        var numericalSnapshot = new PlantNetworkHydraulicNumericalSnapshot(
            coupling.Mode,
            hybrid.UsedSemiImplicitCorrection,
            hybrid.IterationCount,
            hybrid.Converged,
            hybrid.PredictorMaximumFractionalSubcooledPressureChange,
            hybrid.PredictorMaximumAbsoluteHydraulicFlowChangeKilogramsPerSecond,
            hybrid.MaximumRelativePressureResidual,
            hybrid.MaximumAbsoluteFlowResidualKilogramsPerSecond);

        return new PlantNetworkStepResult(
            candidateState,
            audit,
            fluidBalances,
            thermalBalances,
            numericalSnapshot);
    }


    private static void ValidateSourceTermTargets(
        PlantNetworkSourceTerms sourceTerms,
        IReadOnlyDictionary<string, FluidNodeBalance> fluidBalances,
        IReadOnlyDictionary<string, ThermalEnergyBalance> thermalBalances)
    {
        foreach (var nodeId in sourceTerms.FluidNodeBalances.Keys)
        {
            if (!fluidBalances.ContainsKey(nodeId))
            {
                throw new ArgumentException($"Plant-network source terms reference unknown fluid node '{nodeId}'.", nameof(sourceTerms));
            }
        }

        foreach (var bodyId in sourceTerms.ThermalBodyBalances.Keys)
        {
            if (!thermalBalances.ContainsKey(bodyId))
            {
                throw new ArgumentException($"Plant-network source terms reference unknown thermal body '{bodyId}'.", nameof(sourceTerms));
            }
        }
    }

    private static void AccumulateSourceTerms(
        PlantNetworkSourceTerms sourceTerms,
        IDictionary<string, FluidNodeBalance> fluidBalances,
        IDictionary<string, ThermalEnergyBalance> thermalBalances)
    {
        foreach (var entry in sourceTerms.FluidNodeBalances)
        {
            fluidBalances[entry.Key] += entry.Value;
        }

        foreach (var entry in sourceTerms.ThermalBodyBalances)
        {
            thermalBalances[entry.Key] += entry.Value;
        }
    }

    private static void AccumulateHydraulicBalances(
        string fromNodeId,
        string toNodeId,
        FluidNodeBalance fromBalance,
        FluidNodeBalance toBalance,
        IDictionary<string, FluidNodeBalance> fluidBalances)
    {
        fluidBalances[fromNodeId] += fromBalance;
        fluidBalances[toNodeId] += toBalance;
    }

    private static void AccumulateFluidDomainBalanceIfPresent(
        string domainId,
        ThermalEnergyBalance balance,
        IDictionary<string, FluidNodeBalance> fluidBalances)
    {
        if (fluidBalances.TryGetValue(domainId, out var fluidBalance))
        {
            fluidBalances[domainId] = fluidBalance + new FluidNodeBalance(MassFlowRate.Zero, balance.NetHeatRate);
        }
    }

    private static void AccumulateThermalDomainBalance(
        string domainId,
        ThermalEnergyBalance balance,
        IDictionary<string, FluidNodeBalance> fluidBalances,
        IDictionary<string, ThermalEnergyBalance> thermalBalances)
    {
        if (fluidBalances.TryGetValue(domainId, out var fluidBalance))
        {
            fluidBalances[domainId] = fluidBalance + new FluidNodeBalance(MassFlowRate.Zero, balance.NetHeatRate);
            return;
        }

        if (thermalBalances.TryGetValue(domainId, out var thermalBalance))
        {
            thermalBalances[domainId] = thermalBalance + balance;
            return;
        }

        throw new InvalidOperationException($"Unknown thermal domain '{domainId}' reached plant-network orchestration.");
    }

    private static IReadOnlyDictionary<string, FluidNodeBalance> CombineAppliedFluidBalances(
        PlantDefinition definition,
        IReadOnlyDictionary<string, FluidNodeBalance> hydraulicBalances,
        IReadOnlyDictionary<string, FluidNodeBalance> nonHydraulicBalances)
    {
        var combined = new Dictionary<string, FluidNodeBalance>(StringComparer.Ordinal);
        foreach (var node in definition.FluidNodes)
        {
            combined.Add(node.Id, hydraulicBalances[node.Id] + nonHydraulicBalances[node.Id]);
        }

        return combined;
    }

    private static Temperature ResolveCommittedTemperature(
        string domainId,
        IReadOnlyDictionary<string, FluidNodeState> fluidNodes,
        IReadOnlyDictionary<string, ThermalBodyState> thermalBodies)
    {
        if (fluidNodes.TryGetValue(domainId, out var fluidNode))
        {
            return fluidNode.Temperature;
        }

        if (thermalBodies.TryGetValue(domainId, out var thermalBody))
        {
            return thermalBody.Temperature;
        }

        throw new InvalidOperationException($"Unknown thermal domain '{domainId}' reached plant-network orchestration.");
    }


    private static double CompensatedSum(IEnumerable<double> values)
    {
        var sum = 0d;
        var compensation = 0d;
        foreach (var value in values)
        {
            var adjusted = value - compensation;
            var next = sum + adjusted;
            compensation = (next - sum) - adjusted;
            sum = next;
        }

        return sum;
    }

    private static PlantNetworkAudit BuildAudit(
        PlantState committedState,
        PlantState candidateState,
        IReadOnlyDictionary<string, FluidNodeBalance> fluidBalances,
        IReadOnlyDictionary<string, ThermalEnergyBalance> thermalBalances,
        Power pumpHydraulicPowerExchange,
        Power heatSourcePower,
        MassFlowRate supplementalExternalMassFlowRate,
        Power supplementalExternalPower,
        TimeSpan deltaTime)
    {
        var initialMassKilograms = committedState.FluidNodes.Sum(static item => item.Mass.Kilograms);
        var finalMassKilograms = candidateState.FluidNodes.Sum(static item => item.Mass.Kilograms);
        var netMassRateKilogramsPerSecond = fluidBalances
            .OrderBy(static item => item.Key, StringComparer.Ordinal)
            .Sum(static item => item.Value.NetMassFlowRate.KilogramsPerSecond);
        var expectedExternalMassRate = supplementalExternalMassFlowRate;
        var expectedMassChangeKilograms = expectedExternalMassRate.KilogramsPerSecond * deltaTime.TotalSeconds;
        var actualMassChangeKilograms = finalMassKilograms - initialMassKilograms;

        var initialEnergyJoules = committedState.FluidNodes.Sum(static item => item.InternalEnergy.Joules)
            + committedState.ThermalBodies.Sum(static item => item.StoredThermalEnergy.Joules);
        var finalEnergyJoules = candidateState.FluidNodes.Sum(static item => item.InternalEnergy.Joules)
            + candidateState.ThermalBodies.Sum(static item => item.StoredThermalEnergy.Joules);
        var netAccumulatedEnergyRateWatts = fluidBalances
            .OrderBy(static item => item.Key, StringComparer.Ordinal)
            .Sum(static item => item.Value.NetEnergyRate.Watts)
            + thermalBalances
                .OrderBy(static item => item.Key, StringComparer.Ordinal)
                .Sum(static item => item.Value.NetHeatRate.Watts);
        var expectedExternalPower = pumpHydraulicPowerExchange + heatSourcePower + supplementalExternalPower;
        var expectedEnergyChangeJoules = expectedExternalPower.Watts * deltaTime.TotalSeconds;
        var actualEnergyChangeJoules = finalEnergyJoules - initialEnergyJoules;

        return new PlantNetworkAudit(
            Mass.FromKilograms(initialMassKilograms),
            Mass.FromKilograms(finalMassKilograms),
            MassFlowRate.FromKilogramsPerSecond(netMassRateKilogramsPerSecond),
            expectedExternalMassRate,
            supplementalExternalMassFlowRate,
            netMassRateKilogramsPerSecond - expectedExternalMassRate.KilogramsPerSecond,
            actualMassChangeKilograms - expectedMassChangeKilograms,
            Energy.FromJoules(initialEnergyJoules),
            Energy.FromJoules(finalEnergyJoules),
            Power.FromWatts(netAccumulatedEnergyRateWatts),
            expectedExternalPower,
            pumpHydraulicPowerExchange,
            heatSourcePower,
            supplementalExternalPower,
            netAccumulatedEnergyRateWatts - expectedExternalPower.Watts,
            actualEnergyChangeJoules - expectedEnergyChangeJoules);
    }
}
