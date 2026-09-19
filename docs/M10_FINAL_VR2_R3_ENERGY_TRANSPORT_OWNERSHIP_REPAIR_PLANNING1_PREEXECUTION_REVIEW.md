# M10 Final VR2 R3 - Energy-Transport Ownership Repair Planning 1 - Preimplementation Review

NRS-MARKER:R3-ENERGY-TRANSPORT-REPAIR-PREIMPLEMENTATION-REVIEW

Date: 2026-09-20

Status: **PRE-IMPLEMENTATION REVIEW / PLANNING ONLY**

## Review conclusion

The Diagnostic 3 REV1 returned evidence is sufficient to select the ownership design but not to apply the production repair directly.

The selected design is Family B, `ACTIVE-CLOSURE-TRANSPORT-PROPERTY-CONTRACT`, because:

1. Family A cannot alter the causal quantity with the existing public saturation interface: returned evidence proves the forward provider is bit-identical between mode 1 and mode 2.
2. Family B can consume the existing immutable C4 reference payload without adding runtime IF97 and without making the steam-drum solver closure-mode aware.
3. Family C can be made to work locally, but would leave a mode-2 special case at the consumer and therefore preserve duplicated thermodynamic ownership.
4. The proposed Family B boundary changes transport properties only; geometric void/level behavior remains frozen.
5. The planned pressure-keyed dense-saturation lookup is allocation-neutral and reuses already-loaded immutable payload data.

## Stop conditions before implementation

Implementation 1 must not begin if the planning audit finds any of the following:

- the Diagnostic 3 REV1 evidence hashes do not match;
- local Contract V2 qualification is not PASS;
- the `src/` or `tests/` baseline has drifted from the planning contract;
- any planned implementation requires changing the C4 payload, IF97 runtime dependencies, raw seed, dynamic thresholds or canonical exact-v9 identity;
- the repair cannot remain inside the five-file production surface frozen by Planning 1.

If implementation later fails the focused transport or first-100-step fast gate, return evidence and stop. Do not retune the seed or widen the envelopes inside the same implementation gate.

## Current authority

NRS-MARKER:R3-REPAIR-PLANNING-LOCAL-AUTHORITY

Local project qualification is the authoritative process gate from 2026-09-20 onward. Hosted GitHub CI is advisory and is not a prerequisite for this planning or its successor unless the project owner explicitly restores that requirement.

R3 remains RED. Production code remains unchanged by this planning candidate.
