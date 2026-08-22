# Digital I&C Architecture Invariants — Reviewed Planning Baseline

## Status

Planning contract derived from the 1997 National Research Council digital-I&C review and reconciled with the current Nuclear Reactor Simulator architecture. It is **not** a claim that the simulator itself is a nuclear safety I&C system.

## Purpose

Freeze the architecture rules that must remain true through M11 release hardening and that future feature milestones may change only through an explicit ADR + validation plan.

## Invariants

| ID | Invariant | Source-derived principle | Current project expression | Release consequence |
|---|---|---|---|---|
| DIC-A01 | Plant physics has one canonical owner per subsystem. | Systems-level allocation must be explicit. | M2 kinetics, M3 primary, M4 secondary/electrical. | Application/App/scenario layers may not become second physics owners. |
| DIC-A02 | Normal/local control and protection remain distinct authorities. | Ch. 3 emphasizes protection/control independence. | M5 control vs M5 protection. | Protection override cannot be implemented as a UI/supervisory convention. |
| DIC-A03 | Supervisory automation coordinates existing control seams only. | Multilayer systems use supervisory coordination above local control. | M5 supervisor adjusts bounded objectives/modes/setpoints. | No direct assignment of reactor power, pressure, rotor speed, electrical output, trip state, etc. |
| DIC-A04 | Protection outranks Manual, Assisted and Supervisory Automatic normal control. | Safety/protection path remains independent of normal control. | Existing protection precedence tests and authority state. | Any command that bypasses a protection latch is release-blocking. |
| DIC-A05 | Manual takeover is explicit and deterministic. | Independent/manual fallback is a systems property. | Stop new supervisory decisions, then bumpless handover using committed controller state. | Takeover timing/order belongs to replay identity and acceptance. |
| DIC-A06 | Training assistance is not plant authority. | Human-support function must not be confused with control allocation. | Hidden/Checklist/Guided axis independent of Manual/Assisted/Supervisory. | Guidance changes cannot affect plant evolution. |
| DIC-A07 | Operational automation consumes measured evidence where instrumentation is required. | I&C decisions depend on qualified/available instrumentation. | No silent true-state fallback. | Missing/invalid required measurement degrades fail-closed. |
| DIC-A08 | True state, measured state and model diagnostic are distinct semantic products. | HMI must preserve what information the operator is actually given. | Provenance/quality semantics in presentation contracts. | UI may not silently replace unavailable measurement with model truth. |
| DIC-A09 | Simulation logical time is authoritative and deterministic. | Appendix F: timing is part of correctness for real-time control. | Fixed 10 ms step; action/fault/replay semantics at exact logical steps. | Wall clock or render cadence cannot alter state evolution/order. |
| DIC-A10 | Desktop pacing may lag wall clock but must never silently drop deterministic simulation time. | Timing failure and scheduling/backlog must be explicit. | Cooperative bounded desktop batches; generic catch-up policy remains under M11.3 review. | Any drop/skip policy requires an explicit contract and replay evidence. |
| DIC-A11 | Committed state is immutable at consumer boundaries. | Shared information must remain valid/consistent across consumers. | Immutable snapshots and committed-state projections. | Background work, if introduced, must use immutable handoff. |
| DIC-A12 | Recorder/replay is observational and versioned. | Information consistency across processing/history must be controlled. | M9.1 every-step frames, exact scenario/action reconstruction, fingerprint verification. | No opaque state dump or second replay owner. |
| DIC-A13 | Checkpoints are replay-backed anchors, not mutable-state snapshots. | Rollback/checkpoint communication can invalidate message meaning if histories diverge. | Exact prefix reconstruction + fingerprint check. | Restore must fail closed on divergence. |
| DIC-A14 | Typed intent categories remain separate. | Function allocation/interfaces should be explicit. | Plant command vs training/presentation vs authority/objective vs session lifecycle. | Do not collapse into a generic command channel. |
| DIC-A15 | HMI has presentation authority only. | Human-machine interface must reflect, not secretly implement, system behavior. | Avalonia consumes immutable Application contracts and dispatches typed intents. | No protection/control/physics inference in XAML/ViewModels. |
| DIC-A16 | MISSION/challenge/scoring remains observational with respect to plant authority. | Human-support/evaluation systems must have clear allocation. | Challenge consumes evidence; no plant-command authority. | Score/mission terminal state cannot trip/command plant directly. |
| DIC-A17 | Alarm acknowledge/reset and physical protection reset remain different operations. | Operator interaction and safety actuation must not be conflated. | Existing canonical alarm and protection seams. | HMI text/actions must preserve distinction. |
| DIC-A18 | Redundancy claims require explicit shared-dependency analysis. | Ch. 5: duplication/design diversity does not guarantee independent failures. | Current project generally avoids duplicate safety owners. | Future “backup” logic cannot be labelled independent without evidence. |
| DIC-A19 | Functional diversity is claimed only for different component-level requirements/principles. | Ch. 5 distinction between design and functional diversity. | Not currently claimed as a product feature. | Any future diversity feature needs a dedicated inventory. |
| DIC-A20 | Release hardening may optimize implementation cost only when semantics remain exact. | Assurance/configuration discipline must preserve relationships across changes. | M11.3 measurement-first rule. | Trajectory-changing numerical changes route post-M11. |
| DIC-A21 | Configuration/version identity is part of behavior. | Ch. 4 stresses rigorous configuration management. | Exact profiles, scenario schemas, archive schema, fingerprint algorithm IDs. | Historical identities cannot be reinterpreted. |
| DIC-A22 | Dependency/COTS assurance is proportional to role. | Ch. 8 recommends commensurate assurance. | .NET/Avalonia runtime dependencies vs test-only xUnit. | Runtime dependencies get packaging/support verification; test-only tools do not become runtime claims. |

## Fail-closed architecture review questions

A proposed M11 change fails architecture review if any answer is “yes” without an explicit new design decision:

1. Does it create a second owner for a physical state or conserved inventory?
2. Does it allow UI/Application/MISSION to infer or write a protected physical result?
3. Does it let supervisory automation use hidden true state when required measured evidence is invalid?
4. Does it weaken protection priority or automatically clear/reset protection?
5. Does it make an accepted operator action depend on wall-clock/render timing?
6. Does it introduce mutable shared runtime/session state across threads?
7. Does it reinterpret historical exact-version persisted evidence?
8. Does it silently decimate, truncate or change recording semantics?
9. Does it claim redundancy/diversity without shared-dependency analysis?
10. Does it change trajectory/numerics under the label of release performance cleanup?
