# ADR 0197 — RP1C uses the corrected full performance predicate and does not infer missing C4 timing evidence

## Status

Accepted for RP1C Planning 1 candidate; not a selection or production activation decision.

## Context

Refinement 2 recorded D3 as `rp1c_selection_eligible=True`, but its boolean did not include the separately recorded seam worst-case timing. D3 returned `seam_max_us=16504.9`, while the frozen strict single-call ceiling is `409.30666666666673 us`. Refinement 3 explicitly required future selection to include that seam maximum.

C4 later preserved C3 semantics bit-for-bit and closed the specific R1 allocation/wall-clock tail with zero exceedances over 204,800 measured calls. C4 was intentionally not a full-domain performance rerun: its timing campaign covered the 320 R1 boundaries that owned the unresolved C3 tail.

Because C4 introduces allocation-neutral mixture/liquid prefixes, semantic bit-equivalence does not by itself prove identical timing on every exact-v9 and non-R1 seam path.

## Decision

RP1C uses the corrected conjunctive predicate including exact-v9 and all-seam-sides single-call maxima.

Under currently frozen evidence:

- D3 is not selection-ready because `16504.9 us > 409.30666666666673 us` on its seam worst case;
- C4 has complete physical/seam semantics and R1 performance closure, but requires one bounded immutable-C4 full-domain performance confirmation before selection-ready status;
- current selection-ready count is zero;
- `NO-SELECTION` remains mandatory.

The confirmation uses five fresh processes and the frozen Refinement-3 exact-v9/seam protocol, without candidate mutation, corpus regeneration or threshold change. Its harness must allocate zero bytes in the measured region, while candidate allocation retains the original median `<=2816 B` qualification ceiling; the R1 zero-allocation result is not generalized to all fallback-capable paths.

## Consequences

RP1C Planning 1 does not authorize a selection gate directly. After returned-planning adjudication it may authorize only `RP1C-C4-FULL-DOMAIN-PERFORMANCE-CONFIRMATION1`.

If that confirmation passes in all five processes, the later RP1C decision space is `SELECT-C4` or `SELECT-NONE`. If it does not pass, the current path cannot select C4.

A later `SELECT-C4` still requires returned selection adjudication before R1 implementation planning. No production source, threshold, exact-v9 identity or runtime policy is changed by this ADR.
