# I.3 Hotfix 2 — Authoritative Production Reference Baseline

## Purpose

I.3 resumes after validated H.30 Requalification 1, which changed the authoritative desktop production policy to exact v3 corrected-commit while preserving exact v2 explicit as rollback/reference.

I.3 does not change plant physics or numerical mathematics. It establishes the regression baseline for the production policy that is now actually authoritative.

## Gate

The scheduled/manual gate runs the authoritative production selector for 300 simulated seconds / 30,000 logical steps at the fixed 10 ms step.

Every step checks:

- no trip;
- breaker closed;
- requested power above 4.5 MWe;
- gross electrical output above 4.0 MWe;
- rotor and canonical shaft power above 4.5 MW;
- no reverse flow on stop, control or admission valves;
- finite mass/energy closure and network-balance residuals.

Every second it records the versioned reference trajectory. The final 60 seconds are used to calculate seven inventory/energy slopes and 19 internal regression tolerance budgets.

## Budget rule

Window metrics use the final-60-second mean as target and:

`max(engineering floor, 2 × maximum observed deviation)`

Slope metrics use target zero and:

`max(engineering floor, 2 × |observed linear slope|)`

These are regression budgets, not calibration targets. A future runtime change must not be tuned merely to fit them.

## Evidence retention

Large generated audit CSV/TXT payloads are not bundled in candidate ZIP files. Ordinary tests consume only the bounded immutable prerequisite set under `eng/frozen-evidence/ordinary`; large historical traces that are needed only for identity checks are omitted and represented by canonical hashes in `eng/frozen-evidence/large-payload-manifest.csv`. Decision-level provenance remains under `eng/evidence-manifests/`; original artifact ZIPs remain separate validation records.

## Expected closure

A green I.3 freezes:

- the authoritative v3 300-second trajectory;
- conservation/inventory observations;
- seven final-window slopes;
- 19 versioned tolerance budgets;
- production telemetry and deterministic control fingerprint.

After I.3, proceed to I.4 known-limitations and legacy-retirement review.
