# M10.9.4.1-I.3 Hotfix 4 Classifier Fix 1 — Static Review

## Scope

Classifier-only correction over Hotfix 4 Script Fix 1. I.2 remains the authoritative validated baseline and I.3 remains unvalidated.

## Evidence basis

The locally produced failed Hotfix 4 comparison is frozen under `Evidence/I3_HF4_ClassifierFix1` with canonical fingerprints:

- summary: `5A71965FDBF3BF203B6F9A2BFD321F3588F21FDFE30A33376D521A8AA5535B64`
- 10 ms trace: `8FEA343B6DA0A02179E77A02A18925EE901B9F7F6D2EBBB4D564D3F56213C57F`
- drop comparison: `699444879577332C27B0BB1D691AEA2FF6D2C5E738EBDFE86F27B84C7DAC2796`
- episodes: `8B15C549B109E58C14A0E5BCB889689AE176E6BDA8F4D74EA367FD5F70FA1EAA`

The failed run reported 338 exact-v2 generation-drop steps, 330 reverse-admission steps, 0 corrected drops, 0 corrected reverse-admission steps, 1791 corrected commits, 0 rollback and 0 fallback. Direct inspection of the frozen 10 ms trace shows the remaining 8 drop steps are reverse stop-valve steps; there are 0 reverse control-valve steps. Thus all 338/338 explicit drops coincide with targeted-train reverse flow and exact v3 has 0 targeted-train reverse-flow steps.

## Code isolation

Compared with Hotfix 4 Script Fix 1, under `src/` only `NuclearReactorSimulator.Application/ApplicationDescriptor.cs` changes, for candidate metadata. Numerical/runtime code is unchanged.

The long comparison test changes only its classification model:

- old: every explicit drop must have `admission_flow < 0`;
- new: every explicit drop must have `stop_flow < 0 || control_flow < 0 || admission_flow < 0`;
- one-to-one equivalence is required in both directions between explicit drops and targeted reverse-flow steps;
- exact v3 must have zero drops and zero targeted reverse-flow steps;
- all previous corrected-commit safety conditions remain unchanged.

No shaft-health floor, H.30 policy, timestep, solver tolerance, branch-continuity limit, physical coefficient, selector or persistence identity changes.

## Packaging review

The candidate must not contain `bin`, `obj` or runtime `artifacts` directories. Build, ordinary tests and the focused 100 s comparison remain local validation gates.
