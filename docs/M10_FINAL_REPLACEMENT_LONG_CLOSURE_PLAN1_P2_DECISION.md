# M10 Final Replacement-Long Closure Plan 1 — P2 Decision Gate 1

**Status: CANDIDATE — PLAN-STOP-INCONCLUSIVE / PLAN AMENDMENT 1.**

P1 returned local execution PASS as an evidence gate, but its primary exact-v9 6 MWe result is `INCONCLUSIVE` after the complete pre-authorized 1,800 s hold. P2 therefore selects neither P3-W nor P3-R. This is the hard planning stop required by the validated P0 contract.

## 1. Frozen P1 evidence

The returned P1 evidence records:

- stable exact-v9 5 MWe reference calibration valid;
- exact-v9 5.5 MWe: primary `STILL-CONVERGING`, final `INCONCLUSIVE`, 900 s hold, no trip, tail output 5.3378567863675768 MWe, output error 0.16214321363242323 MWe and dispatch adequacy -0.16545226865998747 MW;
- exact-v9 6 MWe: primary `STILL-CONVERGING`, bounded continuation invoked, final `INCONCLUSIVE`, 1,800 s hold, no trip, tail frequency 50.000010053044022 Hz, output 5.9312649958700989 MWe, output error 0.068735004129901078 MWe, dispatch adequacy -0.070137744201449845 MW and output slope 3.0659206076729932E-05 MW/s;
- exact-v4 6 MWe historical control: final `INCONCLUSIVE`, no trip, tail frequency 49.660336156824044 Hz, output error 0.69727382575398789 MWe, dispatch adequacy -0.71454414718732129 MW and control valve saturated at 100% in the returned event evidence;
- P1 branch signal `P2-PLAN-STOP-INCONCLUSIVE`.

The exact-v9 6 MWe probe is already inside the P1 amplitude tolerances for frequency, electrical-output error, dispatch adequacy and net rotor acceleration, but it is not stationary under the frozen P1 slope contract. The 900→1,800 s interval materially reduces the phase/dispatch deficit instead of demonstrating a stationary bias. P3-R is therefore not supported. Because stationarity was not demonstrated, P3-W is also not supported.

## 2. P2 decision

P2 records:

- `P3-W-AUTHORIZED = False`;
- `P3-R-AUTHORIZED = False`;
- `P2-DECISION = PLAN-STOP-INCONCLUSIVE`;
- Replacement-Long Execution 1 remains RED immutable evidence;
- no replacement workload, authority, generator-load, protection, exact-v9 or mission semantics may change;
- no second replacement-long baseline may be frozen.

P2 is documentation/engineering governance only. It is not an additional dynamics experiment and does not repair or reinterpret P1.

## 3. Plan Amendment 1 — P1A Asymptotic Closure Extension

The planning stop is resolved by one bounded amendment to the validated route rather than by an ad hoc diagnostic chain.

**P1A question:** do the exact-v9 small-stage trajectories that were still evolving at the end of P1 satisfy the unchanged P1 `CONVERGED` or `BIASED-STATIONARY` criteria when given one final predeclared asymptotic horizon?

P1A is constrained before implementation as follows:

1. Production `src/`, replacement workload, authority policy, generator-load semantics, protection semantics, exact-v9 and mission @3 remain unchanged.
2. Only two physical probes are authorized: exact-v9 5→5.5 MWe and exact-v9 5→6 MWe. The exact-v4 control is frozen from P1 and is not rerun because it is not the current production branch and already does not support an exact-v9-only regression hypothesis.
3. Each exact-v9 probe starts from a clean deterministic state and may hold for at most **3,600 s after its load command**.
4. The 5 MWe calibration and all P1 convergence/stationarity tolerances remain unchanged. No threshold may be relaxed after observing P1A.
5. P1A must reproduce the corresponding P1 checkpoint evidence before consuming the extension: 900 s for 5.5 MWe; 900 s and 1,800 s for 6 MWe, under predeclared deterministic numeric tolerances.
6. Final classification may be only `CONVERGED`, `BIASED-STATIONARY` or `INCONCLUSIVE`. There is no further automatic continuation beyond 3,600 s.
7. P1A may early-exit only after the already-observed P1 horizon for the probe and only after the unchanged convergence or stationary window is satisfied.
8. A trip, non-finite trajectory, checkpoint mismatch or calibration invalidity maps to `INCONCLUSIVE`/gate failure as specified by the executable P1A contract; it never silently selects P3.

## 4. Route amendment

The remaining route becomes:

`P0 VALIDATED → P1 INCONCLUSIVE → P2 PLAN-STOP → P1A → P2R → P3-W/P3-R → P4 → P5 → P6`.

P2R is a decision-only re-entry gate:

- P1A `CONVERGED` → P3-W may be authorized;
- P1A `BIASED-STATIONARY` → P3-R may be authorized;
- P1A `INCONCLUSIVE` → planning stop again; no new diagnostic or repair is authorized without another explicit plan revision.

## 5. Current authorization

After this P2 checkpoint is validated, the **only authorized implementation is P1A — Asymptotic Closure Extension**. P2 itself changes no runtime or workload semantics and does not authorize P3, P4, P5, Replacement-Long Execution 2 or M11.
