# M10 Final VR2 R3 - Energy-Transport Ownership Repair Implementation 1 Post-Repair Causal-Closure Diagnostic 1

NRS-MARKER:R3-POST-REPAIR-CAUSAL-CLOSURE-DIAGNOSTIC1

Date: 2026-09-20

Status: **CANDIDATE / TEST-EVIDENCE ONLY**

## Why this diagnostic exists

Implementation 1 has now passed static validation, Release build and the focused closure-owned transport-property tests. The next historical 100-step gate returned RED with 84 envelope violations.

That RED cannot be used directly to adjudicate Family B because the exact same fast gate was already RED in the frozen pre-repair evidence: 86 violations, first at step 15. In both pre-repair and post-repair evidence the governor exits `29.27 .. 29.30 %` at step 17, with a maximum pre/post governor difference below `1E-6` percentage points over the full second. The governor criterion is therefore common-mode evidence, not a discriminator for the new energy-transport ownership repair.

No threshold is relaxed by this diagnostic. The old envelope remains recorded exactly as historical evidence. The planning defect is only that a known-RED historical gate was later reused as if zero violations were a reachable Implementation-1 success condition.

## Immediate engineering question

Before changing any dynamic criterion, prove whether the production repair actually closes the causal seam for which it was selected:

`STEAM-DRUM-LIQUID-TRANSPORT-VS-MODE2-SUCTION-TRANSPORT`

The existing Diagnostic 3 REV1 runtime evidence test already reconstructs exactly the required raw -> seed-step1 10 ms scenario. This gate reruns that unchanged test against the repaired production surface and adjudicates only the direct production balance row.

## Required post-repair checks

For the repaired mode-2 seed-step1 scenario:

- suction mass identity residual magnitude <= `1E-9 kg/s`;
- raw suction selected transport vs production drum-liquid advected transport <= `0.001 J/kg`;
- observed suction net-energy rate magnitude <= `0.10223668223103162 W`;
- production predicted-vs-observed energy-rate residual magnitude <= `0.001 W`.

If all four hold, classify `POST-REPAIR-CAUSAL-CLOSURE-CONFIRMED`.

That result does **not** make R3 PASS and does not authorize Requalification 3. It only proves that Family B repaired the selected seam. The residual first-second dynamic-equilibrium problem must then be replanned separately from the already-fixed causal seam.

If any direct causal check fails, classify `POST-REPAIR-CAUSAL-CLOSURE-NOT-CONFIRMED`; Family B Implementation 1 remains unqualified and no dynamic replanning is authorized.

## Frozen 100-step evidence interpretation

Historical pre-repair fast gate:

- total envelope violations: `86`;
- first violation: step `15`;
- primary-flow violations: `86`, beginning step `15`;
- governor violations: `84`, beginning step `17`.

Returned post-repair observation:

- total envelope violations: `84`;
- first violation: step `17`;
- primary-flow violations: `79`, beginning step `22`;
- governor violations: `84`, beginning step `17`;
- electrical violations: `0`;
- drum-level violations: `0`;
- trip steps: `0`;
- breaker-open steps: `0`;
- rollback steps: `0`;
- non-finite steps: `0`.

This diagnostic does not declare those dynamic values acceptable. It only prevents an already-known-RED gate from being misused as evidence that the newly introduced energy-transport repair itself failed.

## Explicit non-authority

No production edit, seed retuning, threshold/envelope change, C4 mutation, default-mode change, exact-v9 reinterpretation, R3 PASS, Requalification 3, R4, VR3 or P3-R1 is authorized by this candidate.
