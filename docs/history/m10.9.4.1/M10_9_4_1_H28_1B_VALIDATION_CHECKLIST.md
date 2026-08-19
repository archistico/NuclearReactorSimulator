# M10.9.4.1-H.28.1-B — Validation Checklist

## Baseline / provenance

- [ ] H.28.1-C Hotfix 2 is treated as validated.
- [ ] Frozen H.28.1-C summary/steps/cost-centers/metrics fingerprints match the user-supplied artifacts.
- [ ] H.28 remains failed evidence, not a validated activation baseline.

## Build and ordinary regression

- [ ] `APPLY_UPDATE.cmd`
- [ ] `dotnet build`
- [ ] `dotnet test`

## Focused gate

- [ ] `scripts\run-historical-explicit-predictor-reuse-audit.cmd`
- [ ] Full-reuse predictor unit contract is exactly equivalent to legacy H.4 predictor evaluation.
- [ ] A deliberately mismatched historical balance reintegrates only that node and still reproduces the legacy predictor exactly.
- [ ] 20 trigger / 20 commit events.
- [ ] 35 hydraulic evaluations / 32 probes / Jacobian dimension 32 on every trigger.
- [ ] zero rollback, unsafe commit and fallback-commit violation.
- [ ] historical predictor node reuse count is non-zero and total node count is recorded.
- [ ] deterministic fingerprint remains `518BA948637F0C270C7F8228AB97FEF9148E29A4F5CE4376319AB5D1CFBE7F38`.
- [ ] non-trigger predictor wall fraction of H.28.1-C <= 0.80.
- [ ] non-trigger predictor allocation fraction of H.28.1-C <= 0.85.
- [ ] H.28.1-C Jacobian/H.9 allocation improvement remains green.

## Architecture / scope

- [ ] standard factory remains `ExplicitCommittedState`.
- [ ] no retuning of P060/F040, H.9, hysteresis, target set or physical coefficients.
- [ ] historical explicit fluid-node integration occurs only once per step.
- [ ] reuse is exact-balance selective; mismatched nodes use the unchanged H.4 integration path.
- [ ] end-of-predictor hydraulic evaluation remains present for F040.
- [ ] H.29 remains blocked.
- [ ] H.24 is not rerun in this development gate.
