using NuclearReactorSimulator.Application.Scenarios;
using NuclearReactorSimulator.Application.Scenarios.Faults;
using NuclearReactorSimulator.Application.ControlRoom;

namespace NuclearReactorSimulator.Application.Scenarios.Faults.ElectricalLoss;

public sealed class ElectricalLossFaultApplicatorFactory : IScenarioFaultApplicatorFactory
{
    public ElectricalLossFaultApplicatorFactory(string faultTypeId)
    {
        if (!ElectricalLossFaultTypeIds.All.Contains(faultTypeId, StringComparer.Ordinal))
        {
            throw new ArgumentException($"Unsupported M8.6 electrical-loss fault type '{faultTypeId}'.", nameof(faultTypeId));
        }

        FaultTypeId = faultTypeId;
    }

    public string FaultTypeId { get; }

    public IScenarioFaultApplicator Create(IControlRoomRuntimeEngine runtimeEngine)
    {
        if (runtimeEngine is not IElectricalLossFaultTarget target)
        {
            throw new InvalidOperationException(
                $"Runtime engine '{runtimeEngine.GetType().Name}' does not expose the M8.6 electrical-loss fault target.");
        }

        return new Applicator(FaultTypeId, target);
    }

    public static IReadOnlyList<IScenarioFaultApplicatorFactory> CreateBuiltIns()
        => ElectricalLossFaultTypeIds.All
            .Select(static id => (IScenarioFaultApplicatorFactory)new ElectricalLossFaultApplicatorFactory(id))
            .ToArray();

    private sealed class Applicator : IScenarioFaultApplicator
    {
        private readonly string _typeId;
        private readonly IElectricalLossFaultTarget _target;

        public Applicator(string typeId, IElectricalLossFaultTarget target)
        {
            _typeId = typeId;
            _target = target;
        }

        public void Activate(ScenarioFaultDefinition fault)
        {
            if (!string.Equals(fault.FaultTypeId, _typeId, StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"Electrical-loss applicator '{_typeId}' received '{fault.FaultTypeId}'.");
            }

            switch (_typeId)
            {
                case ElectricalLossFaultTypeIds.ExternalSupplyLoss:
                    _target.ActivateExternalSupplyLoss(fault.FaultId, fault.TargetId);
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported electrical-loss type '{_typeId}'.");
            }
        }

        public void Deactivate(ScenarioFaultDefinition fault)
            => _target.ClearElectricalLossFault(fault.FaultId);
    }
}
