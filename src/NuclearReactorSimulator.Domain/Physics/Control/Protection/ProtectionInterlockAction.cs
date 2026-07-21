namespace NuclearReactorSimulator.Domain.Physics.Control.Protection;

[Flags]
public enum ProtectionInterlockAction
{
    None = 0,
    BlockRodWithdrawal = 1,
    BlockTurbineAdmissionOpening = 2,
    BlockGeneratorBreakerClose = 4,
}
