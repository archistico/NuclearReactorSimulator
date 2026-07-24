using NuclearReactorSimulator.Domain.Physics.Quantities;

namespace NuclearReactorSimulator.Domain.Physics.Electrical;

/// <summary>
/// Optional reduced-order infinite-bus synchronizing correction applied around the dispatched generator load.
/// The phase term provides electrical-angle stiffness while the frequency term damps rotor/grid slip.
/// Power-flow mode versions whether negative correction is clamped at zero (legacy generation-only) or may motor the shaft.
/// A null coupling on <see cref="SynchronousGeneratorDefinition"/> preserves the historical dispatch-torque-only model.
/// </summary>
public sealed class SynchronousGridCouplingDefinition
{
    public SynchronousGridCouplingDefinition(
        Power maximumSynchronizingCorrectionPower,
        Power frequencyDampingPowerAtOneHertzSlip,
        SynchronousGridPowerFlowMode powerFlowMode = SynchronousGridPowerFlowMode.GenerationOnly)
    {
        if (maximumSynchronizingCorrectionPower <= Power.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maximumSynchronizingCorrectionPower),
                maximumSynchronizingCorrectionPower,
                "Maximum synchronizing correction power must be greater than zero.");
        }

        if (frequencyDampingPowerAtOneHertzSlip <= Power.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(frequencyDampingPowerAtOneHertzSlip),
                frequencyDampingPowerAtOneHertzSlip,
                "Frequency-damping power at one hertz slip must be greater than zero.");
        }

        if (!Enum.IsDefined(powerFlowMode))
        {
            throw new ArgumentOutOfRangeException(nameof(powerFlowMode), powerFlowMode, "Unsupported synchronous-grid power-flow mode.");
        }

        MaximumSynchronizingCorrectionPower = maximumSynchronizingCorrectionPower;
        FrequencyDampingPowerAtOneHertzSlip = frequencyDampingPowerAtOneHertzSlip;
        PowerFlowMode = powerFlowMode;
    }

    /// <summary>
    /// Peak signed phase-angle correction magnitude. The applied term is Pmax*sin(delta), where positive delta means generator lead.
    /// </summary>
    public Power MaximumSynchronizingCorrectionPower { get; }

    /// <summary>
    /// Signed damping calibration: this much correction power is added per +1 Hz generator/grid frequency slip.
    /// Negative slip unloads the shaft in generation-only mode and may cross into motoring in bidirectional mode;
    /// positive slip increases electromagnetic loading.
    /// </summary>
    public Power FrequencyDampingPowerAtOneHertzSlip { get; }

    /// <summary>
    /// Whether the coupled grid may only load the shaft as a generator or may also motor the shaft with signed negative load torque.
    /// </summary>
    public SynchronousGridPowerFlowMode PowerFlowMode { get; }
}
