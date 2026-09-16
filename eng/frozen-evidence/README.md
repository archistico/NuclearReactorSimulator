# Frozen evidence contracts

This directory contains the compact, immutable evidence payload needed by ordinary tests and current lightweight preflight contracts.

- `ordinary/` contains only frozen files smaller than 1 MB that are directly consumed by tests.
- `large-payload-manifest.csv` records canonical SHA-256 identities for large historical traces that ordinary tests only need to authenticate.
- Full generated/historical audit payloads remain external/local and are not bundled in candidate source ZIPs.
- `tests/NuclearReactorSimulator.Application.Tests/Scenarios/Gameplay/Evidence` is not a source-package dependency.

Do not add generated current-run artifacts here. New entries require an explicit evidence-contract review.

## M10 Final VR2 Engineering Repair Planning 1 — RP1B Performance Replanning / Refinement 5

`M10FinalVR2EngineeringRepairPlanning1_RP1B_PerformanceReplanning1_UserReturnedAudit.txt` is the returned `PASS-AS-AUTHORED` audit that authorizes implementation of the frozen Refinement 5 cross-process measurement protocol only. `M10FinalVR2EngineeringRepairPlanning1_RP1B_PerformanceReplanning1_UserReturnedSummary.txt` is the compact navigation summary. Neither artifact authorizes C4 implementation, RP1C selection or production thermodynamic changes.
