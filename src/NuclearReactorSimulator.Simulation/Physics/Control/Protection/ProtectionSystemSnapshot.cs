using System.Collections.ObjectModel;
using NuclearReactorSimulator.Domain.Physics.Control.Protection;

namespace NuclearReactorSimulator.Simulation.Physics.Control.Protection;

public sealed class ProtectionSystemSnapshot
{
    public ProtectionSystemSnapshot(
        ProtectionSystemDefinition definition,
        IEnumerable<ProtectionFunctionSnapshot> functions,
        IEnumerable<ProtectionInterlockSnapshot> interlocks,
        IEnumerable<ProtectionPermissiveSnapshot> resetPermissives,
        ProtectionAction latchedActions,
        ProtectionInterlockAction activeInterlocks,
        bool resetRequested,
        bool resetAccepted)
    {
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        Functions = new ReadOnlyCollection<ProtectionFunctionSnapshot>(functions.OrderBy(static item => item.FunctionId, StringComparer.Ordinal).ToArray());
        Interlocks = new ReadOnlyCollection<ProtectionInterlockSnapshot>(interlocks.OrderBy(static item => item.InterlockId, StringComparer.Ordinal).ToArray());
        ResetPermissives = new ReadOnlyCollection<ProtectionPermissiveSnapshot>(resetPermissives.OrderBy(static item => item.PermissiveId, StringComparer.Ordinal).ToArray());
        LatchedActions = latchedActions;
        ActiveInterlocks = activeInterlocks;
        ResetRequested = resetRequested;
        ResetAccepted = resetAccepted;
    }

    public ProtectionSystemDefinition Definition { get; }
    public IReadOnlyList<ProtectionFunctionSnapshot> Functions { get; }
    public IReadOnlyList<ProtectionInterlockSnapshot> Interlocks { get; }
    public IReadOnlyList<ProtectionPermissiveSnapshot> ResetPermissives { get; }
    public ProtectionAction LatchedActions { get; }
    public ProtectionInterlockAction ActiveInterlocks { get; }
    public bool ResetRequested { get; }
    public bool ResetAccepted { get; }
    public bool ResetRejected => ResetRequested && !ResetAccepted;
    public bool ReactorScramActive => (LatchedActions & ProtectionAction.ReactorScram) != ProtectionAction.None;
    public bool TurbineTripActive => (LatchedActions & ProtectionAction.TurbineTrip) != ProtectionAction.None;
    public bool GeneratorTripActive => (LatchedActions & ProtectionAction.GeneratorTrip) != ProtectionAction.None;
    public bool RodWithdrawalInhibited => (ActiveInterlocks & ProtectionInterlockAction.BlockRodWithdrawal) != ProtectionInterlockAction.None;
    public bool TurbineAdmissionOpeningInhibited => (ActiveInterlocks & ProtectionInterlockAction.BlockTurbineAdmissionOpening) != ProtectionInterlockAction.None;
    public bool GeneratorBreakerCloseInhibited => (ActiveInterlocks & ProtectionInterlockAction.BlockGeneratorBreakerClose) != ProtectionInterlockAction.None;
}
