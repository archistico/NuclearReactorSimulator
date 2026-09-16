# M10 Final — VR2 Engineering Repair Planning 1 — RP1B Refinement 1 Hotfix 1 Pre-Execution Review

**Scope:** static pre-execution review after Refinement 1 Attempt 1 static-preflight RED.  
**Engineering authority:** validation-harness hardening only. No C2/D2 retuning, production repair, tolerance change, exact-v9 change, RP1C selection, VR3, P3-R1 or second replacement-long authorization.

## 1. Attempt 1 classification

Attempt 1 stopped in `[1/3]` before ordinary build and before the focused C2/D2 matrix. The failing check compared the JSON-deserialized p95 ceiling to an exact PowerShell floating literal with `-ne` and reported drift for the frozen value `158.80666666666667 us`.

This is a validator defect. It produced no C2/D2 evidence and does not alter any RP1A/RP1B engineering conclusion.

## 2. Confirmed blocker corrected

The Refinement 1 validator had regressed from the already-validated first-generation RP1B validator, which used bounded `Math.Abs(...)` comparisons for long decimal values. Hotfix 1 replaces every floating contract equality with a finite-double check plus explicit absolute tolerance. The JSON contract values themselves are unchanged.

The hardened validator now checks, without exact floating equality:

- 25% existing VR2 blocking ceiling;
- 10% planning target;
- 100% exact-v9 phase-agreement target;
- `1e-9 kg/s` frozen-law replay self-check tolerance;
- 94.8 us median ceiling;
- 158.80666666666667 us p95 ceiling;
- 409.30666666666673 us max ceiling;
- 2816 B median-allocation ceiling.

## 3. Preflight protections restored from validated RP1B

The Refinement 1 fork had also dropped checks already present in the validated RP1B Hotfix 2 validator. Hotfix 1 restores them before the next run:

- exact contract output count and ordering;
- all RP1A CSV headers;
- the `VR2-SAT-360C-PONLY` boundary-only identity;
- 40 / 39+1 VR2 shape;
- 360 reference-resolved exact-v9 node rows;
- 288 reference-resolved hydraulic rows;
- 1280 seam rows / 320 boundaries / 320 rows per seam side;
- compound node/hydraulic/seam key uniqueness;
- candidate warmup/measured-pass protocol;
- deterministic-repeat requirement;
- frozen hydraulic replay and seam-corpus requirements;
- Region-2 B23 domain guard;
- first-generation evidence freeze;
- complete no-authority flag set;
- null-safe UTF-8 text reads;
- a fail-closed guard against reintroducing the xUnit2031 `Assert.Single(...Where(...))` pattern.

## 4. C2/D2 and focused-test static review

The Hotfix does not change C2, D2, the focused test, the runner or the machine-readable engineering contract. Static review found:

- no C2/D2 identity under `src/`;
- `src/` byte-identical to validated RP1B Hotfix 2;
- first-generation B1/C1/D1 test/reference files, contract, validator and runner byte-identical to validated RP1B Hotfix 2;
- frozen returned RP1B evidence byte-identical 9/9 to the user-returned files;
- C2 remains initialization-derived/tabulated and does not call IF97 directly from its resolve path;
- D2 remains a test-only direct-IF97 comparator seeded by C2;
- Region-2 pressure bounds continue to use the saturation boundary below 623.15 K and B23 through 863.15 K;
- hydraulic replay uses `(probe_id, logical_step, node_id/path_id)` identities and the already-qualified frozen law;
- no `Assert.Single(...Where(...))` analyzer pattern remains;
- all candidate loops/refinement paths inspected are explicitly bounded;
- runner filter method, opt-in environment variable and all nine artifact names align with the focused test and contract.

## 5. Frozen-corpus audit repeated

Independent static parsing of the packaged RP1A evidence confirms:

```text
VR2 rows                 40
inverse-applicable        39
boundary-only              1
exact-v9 node rows        360
hydraulic rows            288
seam rows                1280
seam boundaries           320
R1-SIDE rows              320
R4-LIQUID-SIDE rows       320
R4-VAPOR-SIDE rows        320
R2-SIDE rows              320
```

All node, hydraulic and seam compound keys are unique and all node/hydraulic reference rows are resolved.

## 6. What static review cannot certify

This environment does not provide the repository's .NET/Windows-PowerShell execution stack. Therefore Hotfix 1 does **not** pre-claim:

- Release build/analyzer PASS;
- actual C2/D2 initialization or resolve timing;
- physical accuracy or seam completeness;
- RP1C selection eligibility.

Those remain outputs of `[2/3]` and `[3/3]` on the validation workstation.

## 7. Decision

Refinement 1 Attempt 1 is frozen as preflight-only RED. Hotfix 1 is the replacement execution candidate. No engineering threshold, C2/D2 equation/table, corpus coordinate, first-generation result or production source has been changed.
