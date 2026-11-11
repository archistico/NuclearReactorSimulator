# M10.9.7.4 Exact-V9 Cross-Host Determinism Contract V2

## Decision

The historical raw V1 transitive fingerprint remains immutable provenance, but it is no longer used as a cross-host frozen
assertion. Returned evidence proved that the 128-step Exact-V9 trajectory is identical on 127/128 steps and differs on
one step only by a host-sensitive floating-point seam that reconverges on the next step. Forcing GitHub to the same
.NET 10.0.5 framework as the local qualification did not change the hosted trace.

The raw V1 contract is retained for exact selector/direct equivalence **within the same process/host**.

A new cross-host V2 fingerprint is introduced:

`sha256-control-room-snapshot-v2-presentation-canonical`

V2 starts from the canonical V1 JSON payload and preserves every byte except finite JSON-number values belonging to a
property named `numericValue`. Those numeric tokens are replaced by `0`; `numericValue:null` remains `null`.

This does not alter simulation state, physics, tolerances, presentation text or the V1 payload. In the presentation
records involved here every `numericValue` has sibling `valueText`, `unit` and `state`. V2 therefore freezes the
cross-host presentation contract while V1 still detects exact same-host numeric divergence.

## Returned evidence

- historical local raw V1 aggregate: `7880AD580179B936C584EB0055BE663E0A1CFA65C5191B0DB8A7F3C514DB5418`
- hosted raw V1 aggregate: `1E8AAF3799059D8C9FB2E930981E26B0B31ECF465673AB6C722B24A95D80FDD7`
- frozen cross-host V2 aggregate: `99B9D27A8F5791A194771D698E8A0740F7024058C172D645E2DF74F1B3C09E73`
- all 128 V2 per-step fingerprints match between the returned local and hosted traces
- runtime-aligned GitHub `.NET 10.0.5` and normal ordinary GitHub traces are byte-identical

## Assertions after V2

The Exact-V9 authoritative test must still require:

1. raw V1 selector aggregate equals raw V1 direct aggregate on the same host;
2. cross-host V2 selector aggregate equals cross-host V2 direct aggregate;
3. cross-host V2 aggregate equals the frozen V2 anchor;
4. all existing health, conservation, rollback, fail-closed and mission assertions remain unchanged.

The historical raw V1 aggregate is not rewritten and is not replaced by a list of host-specific accepted hashes.

## Scope

No reactor physics, thermodynamics, turbine hydraulics, tolerances, production policy, mission binding, VR2 or R3 state
is changed by this contract.

The one-shot hosted runtime-alignment workflow is removed after adjudication because it proved that runtime patch
alignment does not remove the host-sensitive raw V1 drift.
