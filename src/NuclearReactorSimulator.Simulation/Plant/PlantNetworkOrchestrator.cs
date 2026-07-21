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

    public PlantNetworkOrchestrator(IFluidThermodynamicModel thermodynamicModel)
        : this(
            new PipeFlowSolver(),
            new ValveFlowSolver(),
            new PumpFlowSolver(),
            new HeatTransferSolver(),
            new HeatSourceSolver(),
            new FluidNodeIntegrator(thermodynamicModel),
            new ThermalBodyIntegrator())
    {
    }

    internal PlantNetworkOrchestrator(
        PipeFlowSolver pipeFlowSolver,
        ValveFlowSolver valveFlowSolver,
        PumpFlowSolver pumpFlowSolver,
        HeatTransferSolver heatTransferSolver,
        HeatSourceSolver heatSourceSolver,
        FluidNodeIntegrator fluidNodeIntegrator,
        ThermalBodyIntegrator thermalBodyIntegrator)
    {
        _pipeFlowSolver = pipeFlowSolver ?? throw new ArgumentNullException(nameof(pipeFlowSolver));
        _valveFlowSolver = valveFlowSolver ?? throw new ArgumentNullException(nameof(valveFlowSolver));
        _pumpFlowSolver = pumpFlowSolver ?? throw new ArgumentNullException(nameof(pumpFlowSolver));
        _heatTransferSolver = heatTransferSolver ?? throw new ArgumentNullException(nameof(heatTransferSolver));
        _heatSourceSolver = heatSourceSolver ?? throw new ArgumentNullException(nameof(heatSourceSolver));
        _fluidNodeIntegrator = fluidNodeIntegrator ?? throw new ArgumentNullException(nameof(fluidNodeIntegrator));
        _thermalBodyIntegrator = thermalBodyIntegrator ?? throw new ArgumentNullException(nameof(thermalBodyIntegrator));
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
