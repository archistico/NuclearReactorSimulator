using System.Globalization;
using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios.Faults;
using NuclearReactorSimulator.Domain.Physics.Quantities;

namespace NuclearReactorSimulator.Application.Scenarios.Faults.LossOfCoolant;

public sealed class LossOfCoolantFaultApplicatorFactory : IScenarioFaultApplicatorFactory
{
    private const double DefaultAmbientPressureKilopascals = 101.325d;
    private const double DefaultReferencePressureDifferenceMegapascals = 1d;
    private const double DefaultMaximumInventoryFractionPerStep = 0.0005d;

    public LossOfCoolantFaultApplicatorFactory(string faultTypeId)
    {
        if (!LossOfCoolantFaultTypeIds.All.Contains(faultTypeId, StringComparer.Ordinal))
        {
            throw new ArgumentException($"Unsupported M8.5 loss-of-coolant fault type '{faultTypeId}'.", nameof(faultTypeId));
        }

        FaultTypeId = faultTypeId;
    }

    public string FaultTypeId { get; }

    public IScenarioFaultApplicator Create(IControlRoomRuntimeEngine runtimeEngine)
    {
        if (runtimeEngine is not ILossOfCoolantFaultTarget target)
        {
            throw new InvalidOperationException(
                $"Runtime engine '{runtimeEngine.GetType().Name}' does not expose the M8.5 loss-of-coolant fault target.");
        }

        return new Applicator(FaultTypeId, target);
    }

    public static IReadOnlyList<IScenarioFaultApplicatorFactory> CreateBuiltIns()
        => LossOfCoolantFaultTypeIds.All
            .Select(static id => (IScenarioFaultApplicatorFactory)new LossOfCoolantFaultApplicatorFactory(id))
            .ToArray();

    private sealed class Applicator : IScenarioFaultApplicator
    {
        private readonly string _typeId;
        private readonly ILossOfCoolantFaultTarget _target;

        public Applicator(string typeId, ILossOfCoolantFaultTarget target)
        {
            _typeId = typeId;
            _target = target;
        }

        public void Activate(ScenarioFaultDefinition fault)
        {
            if (!string.Equals(fault.FaultTypeId, _typeId, StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"Loss-of-coolant applicator '{_typeId}' received '{fault.FaultTypeId}'.");
            }

            _target.ActivatePressureDrivenBreak(
                fault.FaultId,
                fault.TargetId,
                MassFlowRate.FromKilogramsPerSecond(ReadPositive(fault, "referenceMassFlowKgPerSecond")),
                Pressure.FromKilopascals(ReadNonNegativeOrDefault(fault, "ambientPressureKilopascals", DefaultAmbientPressureKilopascals)),
                PressureDifference.FromMegapascals(ReadPositiveOrDefault(
                    fault,
                    "referencePressureDifferenceMegapascals",
                    DefaultReferencePressureDifferenceMegapascals)),
                ReadFractionOrDefault(
                    fault,
                    "maximumInventoryFractionPerStep",
                    DefaultMaximumInventoryFractionPerStep,
                    maximumInclusive: 0.01d));
        }

        public void Deactivate(ScenarioFaultDefinition fault)
            => _target.ClearLossOfCoolantFault(fault.FaultId);

        private static double ReadPositive(ScenarioFaultDefinition fault, string key)
        {
            var value = ReadRequiredDouble(fault, key);
            if (value <= 0d)
            {
                throw new ArgumentOutOfRangeException(key, value, $"Fault '{fault.FaultId}' parameter '{key}' must be greater than zero.");
            }

            return value;
        }

        private static double ReadPositiveOrDefault(ScenarioFaultDefinition fault, string key, double defaultValue)
        {
            var value = ReadOptionalDouble(fault, key, defaultValue);
            if (value <= 0d)
            {
                throw new ArgumentOutOfRangeException(key, value, $"Fault '{fault.FaultId}' parameter '{key}' must be greater than zero.");
            }

            return value;
        }

        private static double ReadNonNegativeOrDefault(ScenarioFaultDefinition fault, string key, double defaultValue)
        {
            var value = ReadOptionalDouble(fault, key, defaultValue);
            if (value < 0d)
            {
                throw new ArgumentOutOfRangeException(key, value, $"Fault '{fault.FaultId}' parameter '{key}' must be non-negative.");
            }

            return value;
        }

        private static double ReadFractionOrDefault(
            ScenarioFaultDefinition fault,
            string key,
            double defaultValue,
            double maximumInclusive)
        {
            var value = ReadOptionalDouble(fault, key, defaultValue);
            if (value <= 0d || value > maximumInclusive)
            {
                throw new ArgumentOutOfRangeException(
                    key,
                    value,
                    $"Fault '{fault.FaultId}' parameter '{key}' must be in (0,{maximumInclusive.ToString(CultureInfo.InvariantCulture)}].");
            }

            return value;
        }

        private static double ReadRequiredDouble(ScenarioFaultDefinition fault, string key)
        {
            if (!fault.Parameters.TryGetValue(key, out var raw)
                || !double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
                || !double.IsFinite(value))
            {
                throw new ArgumentException($"Fault '{fault.FaultId}' requires finite invariant-culture parameter '{key}'.");
            }

            return value;
        }

        private static double ReadOptionalDouble(ScenarioFaultDefinition fault, string key, double defaultValue)
        {
            if (!fault.Parameters.TryGetValue(key, out var raw))
            {
                return defaultValue;
            }

            if (!double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) || !double.IsFinite(value))
            {
                throw new ArgumentException($"Fault '{fault.FaultId}' parameter '{key}' must be finite invariant-culture numeric text.");
            }

            return value;
        }
    }
}
