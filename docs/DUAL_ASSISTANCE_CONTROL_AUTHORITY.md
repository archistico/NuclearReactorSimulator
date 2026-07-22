# Dual Assistance & Control Authority

M10.5 formalizes two orthogonal operator-facing concepts that must never be conflated.

## Training assistance

`TrainingGuidanceMode` remains presentation/training state:

- `Hidden` — no step-by-step guidance;
- `ChecklistOnly` — checklist/readiness information without guided procedure prose;
- `Guided` — full guidance presentation.

Changing this axis never changes controller modes, physics, protection or training scoring semantics.

## Plant control authority

`PlantControlAuthorityMode` is physical-control authority metadata owned by M5:

- `Manual` — all local controller inputs are transferred to manual using committed last outputs for bumpless handover;
- `Assisted` — operator owns the operating decisions while existing local controller modes/setpoints remain as configured;
- `SupervisoryAutomatic` — an M5 supervisor may coordinate selected existing local loops toward a bounded objective.

Requested and effective authority are separate. A requested Supervisory mode can become effectively Assisted when there is no valid objective, a required measured signal is invalid, or protection suspends supervisory decisions.

The presentation contract therefore exposes requested authority, effective authority, health, degradation reason, semantic transition sequence and per-controller mode/setpoint state.

## Replay

Authority/objective selections are semantic Application intents, not physical plant commands. They are journaled separately and replayed at the deterministic next-step boundary. They are not encoded as fake `ControlRoomCommandKind` values.
