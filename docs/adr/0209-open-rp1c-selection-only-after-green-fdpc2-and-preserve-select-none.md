# ADR-0209 — Open RP1C selection only after green FDPC2 and preserve SELECT-NONE

## Decision

Returned FDPC2 makes `C4 + AMBIENT-UNSET` selection-ready under the corrected predicate. Open a separate RP1C selection planning/decision sequence with exactly `SELECT-C4 | SELECT-NONE`. Preserve D3 as blocked by its frozen seam maximum.

The selection gate itself performs no new measurement and changes no production code, runtime configuration, thresholds or exact-v9 identity. A later `SELECT-C4` may authorize only R1 implementation planning after returned selection adjudication.
