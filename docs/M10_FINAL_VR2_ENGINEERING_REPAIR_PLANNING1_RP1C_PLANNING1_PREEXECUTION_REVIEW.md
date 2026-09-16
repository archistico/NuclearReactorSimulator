# M10 Final — VR2 Engineering Repair Planning 1 — RP1C Planning 1 Pre-Execution Review

**Review status:** STATIC REVIEW COMPLETE — local Windows PowerShell execution still required.  
**Scope:** planning/selection-policy audit only. No RP1C selection implementation or production change is present.

## 1. Review result

The first RP1C Planning 1 draft was intentionally not frozen as final because the review found a real evidence-boundary issue: returned C4 timing closes the 320-boundary R1 path, but C4 itself has not yet been timed over the complete exact-v9 + four-side seam corpus required by the corrected full RP1C performance predicate.

The corrected planning therefore does **not** mark C4 selection-ready yet.

## 2. D3 historical eligibility correction

Refinement 2 returned D3 with `rp1c_selection_eligible=True`, but the boolean omitted the separately recorded seam worst case. Frozen evidence is:

```text
D3 resolve median = 14.7 us
D3 resolve p95    = 15.3 us
D3 resolve max    = 208.5 us
D3 seam max       = 16504.9 us
strict max        = 409.30666666666673 us
```

Refinement 3 already declared that future selection must include seam maximum. Planning 1 therefore preserves the old flag as provenance while reconstructing current D3 readiness as false.

## 3. C4 evidence boundary

Returned C4 evidence is strong and remains fully accepted:

```text
1,967/1,967 semantic comparisons bit-equivalent
204,800 R1 timing calls
0 unresolved
0 candidate allocation
0 harness allocation
0 fallback
0 strict-max exceedance
worst R1 max = 329.9 us
```

The remaining gap is narrow but material for selection governance: C4 introduces new allocation-neutral mixture/liquid prefixes, so C3's historical exact-v9/non-R1 seam timing cannot be treated as measured C4 timing merely because the output state is bit-equivalent.

## 4. Corrected next evidence gate

The planning now freezes a five-process immutable-C4 confirmation:

```text
360 exact-v9 rows x 64 measured passes = 23,040 calls/process
1,280 seam rows x 16 measured passes   = 20,480 calls/process
                                           43,520 calls/process
5 fresh processes                         217,600 calls total
```

The C4 candidate, corpus and thresholds remain immutable. The harness must remain allocation-neutral. Candidate allocation remains subject to the original median ceiling of 2816 B rather than an artificial zero-allocation requirement on every non-R1 path. All five processes must satisfy the corrected full performance predicate.

## 5. RED-class hardening

The validator was reviewed specifically for recurring project failure modes:

- evidence paths and SHA-256 pins are independent of editable contract values;
- all frozen numeric CSV values are parsed explicitly with invariant culture, avoiding Italian-locale decimal parsing failures;
- exact floating-point contract constants are compared with explicit bounded checks rather than fragile string equality;
- the PowerShell validator is ASCII-only for Windows PowerShell 5.1 stability;
- the future confirmation test and runner are required to be absent from this planning-only package;
- exactly four planning artifacts are required;
- `SELECT-NONE` remains mandatory;
- no authority is inferred from candidate uniqueness;
- negative future performance evidence is not defined as an infrastructure/xUnit failure.

## 6. Authority after a local planning PASS

A local `PASS-AS-AUTHORED` freezes only this planning contract. It does not authorize C4 full-domain confirmation until the complete planning artifact folder has been returned and adjudicated.

RP1C selection, production repair, thresholds, exact-v9, VR3, P3-R1 and second replacement-long remain blocked.
