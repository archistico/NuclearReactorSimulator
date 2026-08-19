# Frozen evidence contracts

This directory contains the compact, immutable evidence payload needed by ordinary tests and current lightweight preflight contracts.

- `ordinary/` contains only frozen files smaller than 1 MB that are directly consumed by tests.
- `large-payload-manifest.csv` records canonical SHA-256 identities for large historical traces that ordinary tests only need to authenticate.
- Full generated/historical audit payloads remain external/local and are not bundled in candidate source ZIPs.
- `tests/NuclearReactorSimulator.Application.Tests/Scenarios/Gameplay/Evidence` is not a source-package dependency.

Do not add generated current-run artifacts here. New entries require an explicit evidence-contract review.
