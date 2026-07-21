using NuclearReactorSimulator.Application.Scenarios;
using NuclearReactorSimulator.Application.Scenarios.Faults;
using System.Globalization;
using NuclearReactorSimulator.Application.ControlRoom;

namespace NuclearReactorSimulator.Application.Scenarios.Faults.SecondaryTransients;

public sealed class SecondaryTransientFaultApplicatorFactory : IScenarioFaultApplicatorFactory
{
    public SecondaryTransientFaultApplicatorFactory(string faultTypeId)
    {
        if (!SecondaryTransientFaultTypeIds.All.Contains(faultTypeId, StringComparer.Ordinal))
        {
            throw new ArgumentException($"Unsupported M8.4 secondary transient type '{faultTypeId}'.", nameof(faultTypeId));
        }

        FaultTypeId = faultTypeId;
    }

    public string FaultTypeId { get; }

    public IScenarioFaultApplicator Create(IControlRoomRuntimeEngine runtimeEngine)
    {
        if (runtimeEngine is not ISecondaryTransientFaultTarget target)
        {
            throw new InvalidOperationException(
                $"Runtime engine '{runtimeEngine.GetType().Name}' does not expose the M8.4 secondary-transient target.");
        }

        return new Applicator(FaultTypeId, target);
    }

    public static IReadOnlyList<IScenarioFaultApplicatorFactory> CreateBuiltIns()
        => SecondaryTransientFaultTypeIds.All
            .Select(static id => (IScenarioFaultApplicatorFactory)new SecondaryTransientFaultApplicatorFactory(id))
            .ToArray();

    private sealed class Applicator : IScenarioFaultApplicator
    {
        private readonly string _typeId;
        private readonly ISecondaryTransientFaultTarget _target;

        public Applicator(string typeId, ISecondaryTransientFaultTarget target)
        {
            _typeId = typeId;
            _target = target;
        }

        public void Activate(ScenarioFaultDefinition fault)
        {
            if (!string.Equals(fault.FaultTypeId, _typeId, StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"Secondary-transient applicator '{_typeId}' received '{fault.FaultTypeId}'.");
            }

            switch (_typeId)
            {
                case SecondaryTransientFaultTypeIds.TurbineTrip:
                    _target.ActivateTurbineTrip(fault.FaultId, fault.TargetId);
                    break;
                case SecondaryTransientFaultTypeIds.GeneratorTrip:
                    _target.ActivateGeneratorTrip(fault.FaultId, fault.TargetId);
                    break;
                case SecondaryTransientFaultTypeIds.CondenserCoolingDegradation:
                    _target.ActivateCondenserCoolingDegradation(
                        fault.FaultId,
                        fault.TargetId,
                        ReadFraction(fault, "capacityFraction"));
                    break;
                case SecondaryTransientFaultTypeIds.CondenserCoolingLoss:
                    _target.ActivateCondenserCoolingLoss(fault.FaultId, fault.TargetId);
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported secondary transient type '{_typeId}'.");
            }
        }

        public void Deactivate(ScenarioFaultDefinition fault)
            => _target.ClearSecondaryTransientFault(fault.FaultId);

        private static double ReadFraction(ScenarioFaultDefinition fault, string key)
        {
            if (!fault.Parameters.TryGetValue(key, out var raw)
                || !double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
                || !double.IsFinite(value)
                || value < 0d
                || value > 1d)
            {
                throw new ArgumentException(
                    $"Fault '{fault.FaultId}' requires invariant-culture parameter '{key}' in the inclusive range [0,1].");
            }

            return value;
        }
    }
}
