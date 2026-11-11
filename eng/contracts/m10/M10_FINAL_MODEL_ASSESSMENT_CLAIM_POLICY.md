# M10 Final — Model Assessment and Claim Policy

**Status: PLANNING POLICY CANDIDATE.**

This policy prevents engineering-gate words such as `VALIDATED` from being misread as blanket physical validation of every subsystem.

## Claim ladder

| Term | Meaning | External reference required? |
| --- | --- | --- |
| TEST PASS | a specific automated/manual test completed successfully | no |
| VERIFIED | implementation/equation/invariant correctness established within declared software/model contract | not necessarily |
| MODEL-ASSESSED | quantitative independent reference comparison exists and error is reported | yes |
| QUALIFIED | accepted for an explicit operating range/use case with stated evidence and limitations | often, but integral qualification may also be system-evidence based |
| PHYSICALLY VALIDATED | strong external evidence supports quantitative physical fidelity for the stated domain | yes; use sparingly |

## Rules

1. Never write `physically validated` from ordinary xUnit PASS alone.
2. `VALIDATED milestone` means the milestone gate is validated; it does not automatically upgrade every contained physical model.
3. External benchmark errors must be published, including unfavorable results.
4. Reduced-order models may remain useful when errors are bounded and claims are correspondingly bounded.
5. A model-assessment discrepancy is not automatically a software defect.
6. A production repair may be authorized only after impact analysis connects the discrepancy to an intended project claim or active trajectory.
7. Historical exact-version evidence is immutable; a repaired semantic model receives a new exact version.
