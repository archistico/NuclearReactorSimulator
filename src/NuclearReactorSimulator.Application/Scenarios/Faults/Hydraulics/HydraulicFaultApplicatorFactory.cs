using NuclearReactorSimulator.Application.Scenarios.Faults;
using System.Globalization;
using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Domain.Physics.Quantities;

namespace NuclearReactorSimulator.Application.Scenarios.Faults.Hydraulics;

public sealed class HydraulicFaultApplicatorFactory : IScenarioFaultApplicatorFactory
{
    public HydraulicFaultApplicatorFactory(string faultTypeId)
    {
        if (!HydraulicFaultTypeIds.All.Contains(faultTypeId, StringComparer.Ordinal))
        {
            throw new ArgumentException($"Unsupported M8.2 hydraulic fault type '{faultTypeId}'.", nameof(faultTypeId));
        }
        FaultTypeId = faultTypeId;
    }

    public string FaultTypeId { get; }

    public IScenarioFaultApplicator Create(IControlRoomRuntimeEngine runtimeEngine)
    {
        if (runtimeEngine is not IHydraulicComponentFaultTarget target)
        {
            throw new InvalidOperationException(
                $"Runtime engine '{runtimeEngine.GetType().Name}' does not expose the M8.2 hydraulic fault-effect target.");
        }
        return new Applicator(FaultTypeId, target);
    }

    public static IReadOnlyList<IScenarioFaultApplicatorFactory> CreateBuiltIns()
        => HydraulicFaultTypeIds.All.Select(static id => (IScenarioFaultApplicatorFactory)new HydraulicFaultApplicatorFactory(id)).ToArray();

    private sealed class Applicator : IScenarioFaultApplicator
    {
        private readonly string _typeId;
        private readonly IHydraulicComponentFaultTarget _target;

        public Applicator(string typeId, IHydraulicComponentFaultTarget target)
        {
            _typeId = typeId;
            _target = target;
        }

        public void Activate(ScenarioFaultDefinition fault)
        {
            if (!string.Equals(fault.FaultTypeId, _typeId, StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"Hydraulic applicator '{_typeId}' received '{fault.FaultTypeId}'.");
            }

            switch (_typeId)
            {
                case HydraulicFaultTypeIds.PumpTrip:
                    _target.ActivatePumpTrip(fault.FaultId, fault.TargetId);
                    break;
                case HydraulicFaultTypeIds.PumpDegradation:
                    _target.ActivatePumpDegradation(fault.FaultId, fault.TargetId, ReadFraction(fault, "capacityFraction"));
                    break;
                case HydraulicFaultTypeIds.ValveFailOpen:
                    _target.ActivateValveFailOpen(fault.FaultId, fault.TargetId);
                    break;
                case HydraulicFaultTypeIds.ValveFailClosed:
                    _target.ActivateValveFailClosed(fault.FaultId, fault.TargetId);
                    break;
                case HydraulicFaultTypeIds.ValveStuck:
                    _target.ActivateValveStuck(fault.FaultId, fault.TargetId);
                    break;
                case HydraulicFaultTypeIds.PathRestriction:
                    _target.ActivatePathRestriction(fault.FaultId, fault.TargetId, ReadFraction(fault, "maximumOpenFraction"));
                    break;
                case HydraulicFaultTypeIds.PathBlockage:
                    _target.ActivatePathRestriction(fault.FaultId, fault.TargetId, 0d);
                    break;
                case HydraulicFaultTypeIds.NodeLeak:
                    _target.ActivateLeak(fault.FaultId, fault.TargetId, MassFlowRate.FromKilogramsPerSecond(ReadPositive(fault, "massFlowKgPerSecond")));
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported hydraulic fault type '{_typeId}'.");
            }
        }

        public void Deactivate(ScenarioFaultDefinition fault) => _target.ClearHydraulicFault(fault.FaultId);

        private static double ReadFraction(ScenarioFaultDefinition fault, string key)
        {
            var value = ReadDouble(fault, key);
            if (value < 0d || value > 1d)
            {
                throw new ArgumentOutOfRangeException(key, value, $"Fault '{fault.FaultId}' parameter '{key}' must be in [0,1].");
            }
            return value;
        }

        private static double ReadPositive(ScenarioFaultDefinition fault, string key)
        {
            var value = ReadDouble(fault, key);
            if (value <= 0d)
            {
                throw new ArgumentOutOfRangeException(key, value, $"Fault '{fault.FaultId}' parameter '{key}' must be greater than zero.");
            }
            return value;
        }

        private static double ReadDouble(ScenarioFaultDefinition fault, string key)
        {
            if (!fault.Parameters.TryGetValue(key, out var raw)
                || !double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
                || !double.IsFinite(value))
            {
                throw new ArgumentException($"Fault '{fault.FaultId}' requires finite invariant-culture parameter '{key}'.");
            }
            return value;
        }
    }
}
