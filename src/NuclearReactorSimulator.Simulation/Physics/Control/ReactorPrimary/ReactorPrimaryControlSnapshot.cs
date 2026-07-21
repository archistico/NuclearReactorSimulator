using System.Collections.ObjectModel;
using NuclearReactorSimulator.Domain.Physics.Control.ReactorPrimary;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.Reactor.ControlRods;
using NuclearReactorSimulator.Simulation.Physics.Control;
using NuclearReactorSimulator.Simulation.Physics.Reactor;
using NuclearReactorSimulator.Simulation.Physics.Reactor.Neutronics;
using NuclearReactorSimulator.Simulation.Physics.Reactor.ThermalPower;

namespace NuclearReactorSimulator.Simulation.Physics.Control.ReactorPrimary;

public sealed class ReactorPrimaryControlSnapshot
{
    public ReactorPrimaryControlSnapshot(
        ReactorPrimaryControlSystemDefinition definition,
        ControlAndActuatorSnapshot controlAndActuator,
        ControlRodSystemState initialRodState,
        ControlRodSystemState candidateRodState,
        ReactivityBreakdownSnapshot committedRodReactivity,
        ReactivityBreakdownSnapshot candidateRodReactivity,
        Reactivity nonRodReactivity,
        Reactivity totalReactivityUsed,
        PointKineticsSnapshot pointKinetics,
        FissionPowerSnapshot fissionPower,
        IEnumerable<ReactorPrimaryLoopDiagnosticSnapshot> loops)
    {
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        ControlAndActuator = controlAndActuator ?? throw new ArgumentNullException(nameof(controlAndActuator));
        InitialRodState = initialRodState ?? throw new ArgumentNullException(nameof(initialRodState));
        CandidateRodState = candidateRodState ?? throw new ArgumentNullException(nameof(candidateRodState));
        CommittedRodReactivity = committedRodReactivity ?? throw new ArgumentNullException(nameof(committedRodReactivity));
        CandidateRodReactivity = candidateRodReactivity ?? throw new ArgumentNullException(nameof(candidateRodReactivity));
        NonRodReactivity = nonRodReactivity;
        TotalReactivityUsed = totalReactivityUsed;
        PointKinetics = pointKinetics ?? throw new ArgumentNullException(nameof(pointKinetics));
        FissionPower = fissionPower ?? throw new ArgumentNullException(nameof(fissionPower));
        Loops = new ReadOnlyCollection<ReactorPrimaryLoopDiagnosticSnapshot>(
            loops.OrderBy(static item => item.LoopId, StringComparer.Ordinal).ToArray());
    }

    public ReactorPrimaryControlSystemDefinition Definition { get; }
    public ControlAndActuatorSnapshot ControlAndActuator { get; }
    public ControlRodSystemState InitialRodState { get; }
    public ControlRodSystemState CandidateRodState { get; }
    public ReactivityBreakdownSnapshot CommittedRodReactivity { get; }
    public ReactivityBreakdownSnapshot CandidateRodReactivity { get; }
    public Reactivity NonRodReactivity { get; }
    public Reactivity TotalReactivityUsed { get; }
    public PointKineticsSnapshot PointKinetics { get; }
    public FissionPowerSnapshot FissionPower { get; }
    public IReadOnlyList<ReactorPrimaryLoopDiagnosticSnapshot> Loops { get; }
}
