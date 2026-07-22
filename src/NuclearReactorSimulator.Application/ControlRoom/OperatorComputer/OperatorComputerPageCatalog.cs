using System.Collections.ObjectModel;

namespace NuclearReactorSimulator.Application.ControlRoom.OperatorComputer;

public static class OperatorComputerPageCatalog
{
    public static IReadOnlyList<OperatorComputerPageDescriptor> Default { get; } =
        new ReadOnlyCollection<OperatorComputerPageDescriptor>(new[]
        {
            new OperatorComputerPageDescriptor(OperatorComputerPageId.Guidance, "GUIDANCE", "Operational Guidance", "Projects the active canonical M7 guidance plan through a generic M10.2 contract while preserving TrainingGuidanceMode suppression semantics."),
            new OperatorComputerPageDescriptor(OperatorComputerPageId.Info, "INFO", "Plant Information", "Compact M10.2 plant-information projection over already-published control-room values with explicit measured/model/state/unavailable provenance."),
            new OperatorComputerPageDescriptor(OperatorComputerPageId.Alarms, "ALARMS", "Alarm Workstation", "M10.3 read-only projection of canonical annunciator state and bounded alarm-event history. ACK/RESET remains owned by existing typed command seams and is available through the separate M10.4 COMMANDS console."),
            new OperatorComputerPageDescriptor(OperatorComputerPageId.Commands, "COMMANDS", "Contextual Command Console", "M10.4 contextual typed-command catalog with target, current state, availability and blocking reason. Dispatch still crosses IControlRoomCommandDispatcher and runtime validation remains authoritative/fail-closed."),
            new OperatorComputerPageDescriptor(OperatorComputerPageId.Modes, "MODES", "Assistance & Control Modes", "M10.5/M10.6 independent training-assistance and plant-control-authority controls with requested/effective/degraded state, per-loop modes and deterministic M5-owned supervisory operation."),
            new OperatorComputerPageDescriptor(OperatorComputerPageId.Diagnostics, "DIAGNOSTICS", "Configuration Diagnostics", "M10.2 scenario/procedure readiness projection through the active canonical checklist evaluator; no universal diagnostic engine is invented."),
            new OperatorComputerPageDescriptor(OperatorComputerPageId.Log, "LOG", "Operational Log", "M10.3 unified read-only LIVE/SESSION/INCIDENT evidence view over M6.6 history plus optional M9.1/M9.2 sources without duplicating storage."),
            new OperatorComputerPageDescriptor(OperatorComputerPageId.Session, "SESSION", "Session & Replay", "Exact-version scenario/session/checkpoint/replay entry point. Restoration remains replay-backed and version verified."),
        });
}
