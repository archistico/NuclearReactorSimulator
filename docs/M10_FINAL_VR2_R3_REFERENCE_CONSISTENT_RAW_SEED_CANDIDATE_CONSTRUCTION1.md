# M10 Final VR2 — R3 Reference-Consistent Raw-Seed Candidate Construction 1

## Purpose

Construct a deterministic test-only conserved-inventory vector for the 12 frozen exact-v9 operating-point target states under the existing C4 mode-2 inverse closure.

## Construction

The test reads the embedded `NRSVR2C4.v1.bin` payload directly, without adding a production forward API.

For saturated-mixture targets it builds candidates from C4 prefix-saturation interpolation by target pressure and target temperature.

For subcooled-liquid targets it attempts:
- C4 liquid-table inversion at target `(T,P)`;
- C4 near-boundary-liquid construction;
- the historical raw inventory as a comparison start.

Every start is evaluated by the actual production `ReferenceConsistentTabulatedInverseResolver`; the deterministically lowest-residual resolved candidate is retained.

## Evidence

The gate emits:
1. the 12-node mass/internal-energy candidate vector;
2. target-vs-resolved pressure, temperature, phase and quality residuals;
3. the 8 target-vs-candidate hydraulic pressure heads;
4. aggregate residual maxima;
5. a pre-integration review.

No physical residual threshold is introduced in this gate. Candidate Construction 1 establishes representability and measured residuals; returned evidence decides whether Seed Integration Planning 1 is justified.

## Non-authority

No production source, C4 payload/resolver, canonical exact-v9 identity, new exact-version identity, threshold, R3 PASS or R4 authority is changed.
