# Change Impact and Revalidation Policy

## Status

**PLANNING — adopt after M10 closure.**

Purpose: make revalidation proportional to what changed while remaining fail-closed. This prevents both under-testing important semantic changes and wasting multi-hour gates for documentation-only edits.

`PROJECT.md` remains the current baseline source. This policy determines the minimum evidence required before a candidate may become the next validated baseline.

## 1. Change classes

| Class | Typical examples | Minimum required evidence |
| --- | --- | --- |
| **D0 Documentation only** | docs, indexes, planning, wording, no executable contract change | documentation/link validator; proof `src/` and `tests/` unchanged. |
| **T1 Test/validator/script only** | test discovery fix, audit routing, validator portability | build if relevant; ordinary/focused route proving harness behavior; production source unchanged proof. |
| **A2 Application/presentation observational** | projection, HMI list stability, read-only view, non-semantic notification optimization | build + ordinary + owner-focused tests; replay/checkpoint if evidence shape/order can change; manual HMI if user-visible. |
| **P3 Persistence/replay/version contract** | archive reader/writer, checkpoint, fingerprint algorithm routing, serializer | build + ordinary + compatibility fixtures + full replay + checkpoint continuation + non-destructive failure tests. |
| **C4 Control/protection/authority semantics** | control mode, protection observation boundary, manual takeover, command ownership | build + ordinary + control/protection owner gates + degraded/protection matrix + replay/checkpoint + manual HMI where visible. |
| **S5 Simulation/Domain numerical or physical** | equations, constitutive law, thermodynamics, hydraulics, kinetics, conservation ownership | build + ordinary + focused model verification + affected integral/reference gates + determinism/replay + performance if hot path; long gate when the change can accumulate or affect whole-plant stability. |
| **R6 Release/package/runtime dependency** | .NET/Avalonia update, publish settings, package contents, runtime config | build/test on supported target + package audit + representative packaged tasks + compatibility/replay if runtime semantics can change. |

A candidate can belong to more than one class; the required evidence is the union.

## 2. Cumulative-gate invalidation rules

After a final/cumulative gate has been validated:

- any production `src/` semantic change invalidates that cumulative result;
- a baseline test change that alters what is being asserted normally invalidates the corresponding evidence and may require cumulative rerun;
- a harness-only fix does not invalidate production evidence if byte-identity of production and baseline tests is proven and the frozen workload/criteria are unchanged;
- documentation-only promotion does not invalidate executable evidence.

## 3. Long-gate rerun rules

A long gate must be rerun in full when:

- production simulation/control/persistence semantics change in a way exercised by the long workload;
- a long acceptance criterion changes;
- the frozen workload changes;
- the long harness previously failed before executing all required legs and a corrected harness is introduced.

Do not resume at the failed leg and combine separate partial runs as a single authoritative pass unless the contract explicitly defines that behavior before execution.

## 4. Compatibility-sensitive changes

Any change to persisted or replayed evidence must explicitly answer:

- what exact historical identities remain supported;
- whether bytes/schema/fingerprint algorithm changed;
- whether semantic reconstruction changed;
- whether old supported inputs replay identically;
- how future/unknown versions fail;
- whether migration creates a new explicit version rather than reinterpreting the old one.

## 5. HMI-sensitive changes

A user-visible HMI change requires manual acceptance when it can affect:

- command target selection;
- authority/protection understanding;
- alarm acknowledgement/reset interpretation;
- critical status visibility across workspaces;
- keyboard behavior;
- minimum-window usability;
- save/replay workflow;
- mode transparency.

Cosmetic-only changes may use a lighter review if they cannot alter these behaviors.

## 6. Performance-only changes

A claimed optimization must provide:

1. pre-change measurement;
2. frozen representative workload;
3. post-change measurement;
4. exact semantic equivalence for the affected representative outputs;
5. owner tests proving failure modes remain distinguishable;
6. no hidden reduction of work, history, logical time or evidence.

An optimization that changes the physical trajectory is not performance-only.

## 7. Documentation rule

Every closure artifact records:

- change class(es);
- exact baseline;
- required gates;
- gates actually executed;
- any intentionally omitted historical evidence and why;
- known limitations/non-claims;
- next authorized step.


## Equilibrium / reference-operating-point special routing

The new equilibrium plan does not weaken the normal change classes.

- **Test-only residual census / artifact writer** with byte-identical `src/`: classify as **T1**; focused diagnostic + ordinary suite are required, and the existing cumulative prerequisite may remain authoritative only when no production semantic changes.
- **Read-only Application validation inspector** added to `src/` but not consumed by runtime/HMI/physics: classify by actual dependency impact, normally **A2**; prove it cannot alter canonical state/order and rerun its owner + ordinary suite.
- **New validation-only plant-hold factory seam**: at least **A2/P3-style compatibility review** if it exposes exact-version construction internals; it may not change registered runtime identities.
- **Changed initial condition / controller memory / actuator bias used by production**: **S5 production semantic change** and new exact version; rerun affected owners, replay/checkpoint, final cumulative and full long.
- **Changed thermodynamic/hydraulic/control law** discovered through equilibrium work: use the existing **S5/C4** production-change ladder; the equilibrium tool is diagnostic evidence, not a waiver.
- **Changed V&V/equilibrium acceptance budget**: requires an explicit contract decision and cannot be used as a hotfix for a failing trajectory.

