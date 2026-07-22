using System.Collections.ObjectModel;
using NuclearReactorSimulator.Domain.Physics.Control.ReactorPrimary;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.Reactor.ControlRods;
using NuclearReactorSimulator.Simulation.Physics.Control;
using NuclearReactorSimulator.Simulation.Physics.Reactor;
using NuclearReactorSimulator.Simulation.Physics.Reactor.IodineXenon;
using NuclearReactorSimulator.Simulation.Physics.Reactor.Core.Spatial;
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
        : this(
            definition,
            controlAndActuator,
            initialRodState,
            candidateRodState,
            committedRodReactivity,
            candidateRodReactivity,
            nonRodReactivity,
            nonRodReactivity,
            totalReactivityUsed,
            pointKinetics,
            fissionPower,
            committedIodineXenon: null,
            candidateIodineXenon: null,
            loops: loops,
            quasiSpatialCoreFeedback: null)
    {
    }

    public ReactorPrimaryControlSnapshot(
        ReactorPrimaryControlSystemDefinition definition,
        ControlAndActuatorSnapshot controlAndActuator,
        ControlRodSystemState initialRodState,
        ControlRodSystemState candidateRodState,
        ReactivityBreakdownSnapshot committedRodReactivity,
        ReactivityBreakdownSnapshot candidateRodReactivity,
        Reactivity externalNonRodReactivity,
        Reactivity nonRodReactivity,
        Reactivity totalReactivityUsed,
        PointKineticsSnapshot pointKinetics,
        FissionPowerSnapshot fissionPower,
        IodineXenonSnapshot? committedIodineXenon,
        IodineXenonSnapshot? candidateIodineXenon,
        IEnumerable<ReactorPrimaryLoopDiagnosticSnapshot> loops,
        QuasiSpatialCoreFeedbackSnapshot? quasiSpatialCoreFeedback = null)
    {
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        ControlAndActuator = controlAndActuator ?? throw new ArgumentNullException(nameof(controlAndActuator));
        InitialRodState = initialRodState ?? throw new ArgumentNullException(nameof(initialRodState));
        CandidateRodState = candidateRodState ?? throw new ArgumentNullException(nameof(candidateRodState));
        CommittedRodReactivity = committedRodReactivity ?? throw new ArgumentNullException(nameof(committedRodReactivity));
        CandidateRodReactivity = candidateRodReactivity ?? throw new ArgumentNullException(nameof(candidateRodReactivity));
        ExternalNonRodReactivity = externalNonRodReactivity;
        NonRodReactivity = nonRodReactivity;
        TotalReactivityUsed = totalReactivityUsed;
        PointKinetics = pointKinetics ?? throw new ArgumentNullException(nameof(pointKinetics));
        FissionPower = fissionPower ?? throw new ArgumentNullException(nameof(fissionPower));
        CommittedIodineXenon = committedIodineXenon;
        CandidateIodineXenon = candidateIodineXenon;
        QuasiSpatialCoreFeedback = quasiSpatialCoreFeedback;
        Loops = new ReadOnlyCollection<ReactorPrimaryLoopDiagnosticSnapshot>(
            loops.OrderBy(static item => item.LoopId, StringComparer.Ordinal).ToArray());
    }

    public ReactorPrimaryControlSystemDefinition Definition { get; }
    public ControlAndActuatorSnapshot ControlAndActuator { get; }
    public ControlRodSystemState InitialRodState { get; }
    public ControlRodSystemState CandidateRodState { get; }
    public ReactivityBreakdownSnapshot CommittedRodReactivity { get; }
    public ReactivityBreakdownSnapshot CandidateRodReactivity { get; }
    public Reactivity ExternalNonRodReactivity { get; }
    public Reactivity NonRodReactivity { get; }
    public Reactivity TotalReactivityUsed { get; }
    public PointKineticsSnapshot PointKinetics { get; }
    public FissionPowerSnapshot FissionPower { get; }
    public IodineXenonSnapshot? CommittedIodineXenon { get; }
    public IodineXenonSnapshot? CandidateIodineXenon { get; }
    public QuasiSpatialCoreFeedbackSnapshot? QuasiSpatialCoreFeedback { get; }
    public IReadOnlyList<ReactorPrimaryLoopDiagnosticSnapshot> Loops { get; }
}
