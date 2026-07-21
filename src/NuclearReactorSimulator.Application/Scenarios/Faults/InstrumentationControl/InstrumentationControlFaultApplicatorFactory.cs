using System.Globalization;
using NuclearReactorSimulator.Application.ControlRoom;

namespace NuclearReactorSimulator.Application.Scenarios.Faults.InstrumentationControl;

public sealed class InstrumentationControlFaultApplicatorFactory : IScenarioFaultApplicatorFactory
{
    public InstrumentationControlFaultApplicatorFactory(string faultTypeId)
    {
        if (!InstrumentationControlFaultTypeIds.All.Contains(faultTypeId, StringComparer.Ordinal))
        {
            throw new ArgumentException($"Unsupported M8.3 instrumentation/control fault type '{faultTypeId}'.", nameof(faultTypeId));
        }

        FaultTypeId = faultTypeId;
    }

    public string FaultTypeId { get; }

    public IScenarioFaultApplicator Create(IControlRoomRuntimeEngine runtimeEngine)
    {
        if (runtimeEngine is not IInstrumentationControlFaultTarget target)
        {
            throw new InvalidOperationException(
                $"Runtime engine '{runtimeEngine.GetType().Name}' does not expose the M8.3 instrumentation/control fault-effect target.");
        }

        return new Applicator(FaultTypeId, target);
    }

    public static IReadOnlyList<IScenarioFaultApplicatorFactory> CreateBuiltIns()
        => InstrumentationControlFaultTypeIds.All
            .Select(static id => (IScenarioFaultApplicatorFactory)new InstrumentationControlFaultApplicatorFactory(id))
            .ToArray();

    private sealed class Applicator : IScenarioFaultApplicator
    {
        private readonly string _typeId;
        private readonly IInstrumentationControlFaultTarget _target;

        public Applicator(string typeId, IInstrumentationControlFaultTarget target)
        {
            _typeId = typeId;
            _target = target;
        }

        public void Activate(ScenarioFaultDefinition fault)
        {
            if (!string.Equals(fault.FaultTypeId, _typeId, StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"Instrumentation/control applicator '{_typeId}' received '{fault.FaultTypeId}'.");
            }

            switch (_typeId)
            {
                case InstrumentationControlFaultTypeIds.SensorBias:
                    _target.ActivateSensorBias(fault.FaultId, fault.TargetId, ReadFinite(fault, "biasEngineeringUnits"));
                    break;
                case InstrumentationControlFaultTypeIds.SensorFreeze:
                    _target.ActivateSensorFreeze(fault.FaultId, fault.TargetId);
                    break;
                case InstrumentationControlFaultTypeIds.SensorFailedLow:
                    _target.ActivateSensorFailedLow(fault.FaultId, fault.TargetId);
                    break;
                case InstrumentationControlFaultTypeIds.SensorFailedHigh:
                    _target.ActivateSensorFailedHigh(fault.FaultId, fault.TargetId);
                    break;
                case InstrumentationControlFaultTypeIds.SensorUnavailable:
                    _target.ActivateSensorUnavailable(fault.FaultId, fault.TargetId);
                    break;
                case InstrumentationControlFaultTypeIds.ControllerOutputFreeze:
                    _target.ActivateControllerOutputFreeze(fault.FaultId, fault.TargetId);
                    break;
                case InstrumentationControlFaultTypeIds.ControllerOutputFailLow:
                    _target.ActivateControllerOutputFailLow(fault.FaultId, fault.TargetId);
                    break;
                case InstrumentationControlFaultTypeIds.ControllerOutputFailHigh:
                    _target.ActivateControllerOutputFailHigh(fault.FaultId, fault.TargetId);
                    break;
                case InstrumentationControlFaultTypeIds.ActuatorCommandFreeze:
                    _target.ActivateActuatorCommandFreeze(fault.FaultId, fault.TargetId);
                    break;
                case InstrumentationControlFaultTypeIds.ActuatorCommandFailLow:
                    _target.ActivateActuatorCommandFailLow(fault.FaultId, fault.TargetId);
                    break;
                case InstrumentationControlFaultTypeIds.ActuatorCommandFailHigh:
                    _target.ActivateActuatorCommandFailHigh(fault.FaultId, fault.TargetId);
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported instrumentation/control fault type '{_typeId}'.");
            }
        }

        public void Deactivate(ScenarioFaultDefinition fault)
            => _target.ClearInstrumentationControlFault(fault.FaultId);

        private static double ReadFinite(ScenarioFaultDefinition fault, string key)
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
