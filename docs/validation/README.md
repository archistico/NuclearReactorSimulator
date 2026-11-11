# Validation summary

M10 brings together plant integration, operator workflows, deterministic replay
and qualification against independent references. This page summarizes the results;
[PROJECT.md](../PROJECT.md) holds the current checkpoint and execution commands.

## Operator capabilities delivered through M10

- A unified operator computer combines plant information, guidance, diagnostics,
  alarms, incident logs and contextual commands.
- Assistance and control authority are represented separately, including requested
  and effective control ownership during supervisory automatic operation.
- Session workspaces support checkpoints, replay, saving and control-state
  inspection with deterministic logical time.
- Instrument gauges, a plant mimic and subsystem schematics present engineering
  state; command consequences, challenges, missions and performance views connect
  operator actions with their observed results.

The original M10.1–M10.9 milestone sheets remain available with the engineering
records. Their individual closure status does not imply final M10 closure.

## What the evidence establishes

| Area | Established result | Limit of the claim |
| --- | --- | --- |
| Operator workflows | Integrated controls, missions, scoring, checkpoints and replay have dedicated regression coverage. | A passed automated gate does not substitute for its required manual acceptance. |
| Exact-version behaviour | Canonical Exact-V9 remains the authoritative baseline, with historical identities preserved. | Later physical-model options do not silently replace existing replay identities. |
| Physical references | Independent reference assessment separates solver behaviour, parameter applicability and model discrepancies. | Subsystem agreement does not establish full-plant fidelity. |
| C4 thermodynamics | R1 returned PASS for the explicit mode-2 implementation; R2 returned PASS for its focused qualification. | Mode 2 remains opt-in; these results do not qualify R3 equilibrium. |
| R3 integration | Diagnostic and repair evidence identifies distinct closure, transport and initial-state issues. | R3 remains RED; earlier successful implementation gates do not imply successful full-plant qualification. |
| Bounded H investigation | The test-only search completed with authenticated evidence and an inconclusive outcome. | No qualified equilibrium seed was found within the frozen bounds. |
| Offline numerical review | Replay of the terminal linear model and the scaled local-model comparison pass their evidence checks. | Predicted model descent is not a nonlinear plant observation. |
| Local Trial 1 | All six frozen directions produced real descent; all seven points reproduced identically in two runs. | No trial satisfied the complete operating envelope; no seed was qualified. |
| Bounded scaled search H2 | 143 recipes produced one accepted update, reducing the maximum residual to 4.907 million. Full offline replay and physical residual reconstruction pass. | Search stopped without a root; all 24 terminal trials crossed the hotwell phase boundary and worsened the objective. |

## The unresolved equilibrium problem

The H search varied 24 fluid mass/energy coordinates against 75 residuals. It used
527 budgeted attempts, with seven accepted updates and no confirmation run. Its
maximum scaled residual decreased from about 7.849 million to 7.226 million,
remaining above the root criterion of one. The result is
`H-INCONCLUSIVE-NO-CANDIDATE-IN-BOUNDS`, not proof of physical infeasibility.

The terminal review found that all six recorded full trial directions were
identical. All 78 terminal trial rejections occurred before plant evaluation at
the unchanged suction-energy bound. Three derivative probes changed phase labels,
so local physical smoothness remains unproven.

The subsequent offline comparison scales Jacobian columns, compares six fixed
dampings and uses one global fraction to the original coordinate box. All six
capped directions predict strict local-model descent. Local Trial 1 subsequently
tested all six directions, using 14 repeated recipes: the best reduced the actual
maximum to 6.909 million, closely matching its prediction.

H2 then solved bounded scaled subproblems with explicit active constraints. Its
143 recipes and five setup batches used 439 physical steps. One update reduced
the actual maximum to 4.907 million before `NO-STRICT-DESCENT`. Hotwell mass
storage now dominates, followed by turbine-inlet energy and hotwell energy.
The accepted point is healthy but outside the primary-flow envelope.

All 24 terminal model steps crossed the hotwell liquid boundary and worsened the
objective; a recorded small inward derivative probe reached 4.481 million without
changing phase. The next numerical design must therefore address local step size
and deeper backtracking before another search. This does not establish physical
infeasibility or a controller defect. Requalification 3, R4 and M10 closure remain
blocked by the unqualified equilibrium.

The Release build passes with warnings treated as errors. The ordinary suite
passes 1,387 tests, with 162 explicit tests excluded and no failures. Production
and all pre-existing tests remain byte-identical. The current offline review is
`python eng/review-r3-h-scaled-search2.py`; earlier gates retain historical tree
baselines and are not current-checkout gates.

## Evidence and detailed contracts

- [Engineering decisions](MODEL_DECISIONS.md) records the rules that remain useful
  beyond the individual diagnostic chain.
- [Machine-readable and exact-text inputs](../../eng/contracts/m10/README.md) are
  maintained with the engineering tooling. Their original bytes are retained for
  validator compatibility and hash authentication.
- Compact returned evidence is under `eng/frozen-evidence/`; it remains unchanged
  by documentation reorganization.
- [Historical documentation](../history/README.md) retains earlier chronology and
  migration audits.

Use [model limitations](../KNOWN_MODEL_LIMITATIONS.md) when assessing the scope of
the educational simulator. Successful numerical checks do not establish industrial
or licensing-grade fidelity.
