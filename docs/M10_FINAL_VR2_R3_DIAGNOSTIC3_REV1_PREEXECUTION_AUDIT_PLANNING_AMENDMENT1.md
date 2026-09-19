# M10 Final VR2 R3 — Diagnostic 3 REV1 Preexecution Audit / Planning Amendment 1

Date: 2026-09-19  
Status: **READY FOR AUDIT — PREEXECUTION AMENDMENT**

<!-- NRS-MARKER:DIAG3-REV1-AMENDMENT1-SCOPE -->

## Why this amendment exists

A full preexecution review of the implemented Diagnostic 3 REV1 found two issues before any build or runtime evidence was accepted.

First, the REV1 validator and adjudicator used the PowerShell cmdlet `Get-FileHash`. The user environment that already executed the Planning 1 validator successfully does not expose that cmdlet. The planning validator itself already demonstrated a compatible solution: SHA-256 through `System.Security.Cryptography.SHA256`. REV1 therefore standardizes on a .NET stream-based SHA-256 helper and removes the cmdlet dependency from both execution-time PowerShell scripts.

Second, the originally frozen independent-reference guards combined:

- `|href_IF97 - hsuction_mode2| <= 0.001 J/kg`; and
- `|counterfactual net suction energy rate| <= 0.01 W`.

At approximately `100 kg/s`, those limits are not mutually consistent: the first permits up to approximately `0.1 W` of rate difference before even accounting for the already-frozen mass-flow identity tolerance.

## Analytical preflight from frozen evidence

The inconsistency is not hypothetical. Using only the frozen raw candidate state and the existing independent test-only IF97 equations:

- drum temperature = `553.15 K`;
- drum pressure = `6416459.281680372 Pa`;
- raw suction pressure = `6416459.393982445 Pa`;
- drum-to-suction pressure difference = approximately `0.112302073 Pa`;
- mode-2 raw suction selected transport energy = approximately `1236671.000850998 J/kg`;
- independent IF97 Region-1 transport at the drum state = approximately `1236671.000703455 J/kg`;
- difference = approximately `-0.000147543 J/kg`.

That difference is comfortably inside the already-frozen `0.001 J/kg` reference-alignment guard, but at approximately `100.00001123 kg/s` it corresponds to approximately `-0.0147543 W`. Therefore a fixed `0.01 W` counterfactual-net guard could reject a physically consistent reference closure for a fifteen-milliwatt residual while the production discontinuity under investigation is approximately `-5.216 MW`.

<!-- NRS-MARKER:DIAG3-REV1-AMENDMENT1-GUARD -->

## Amended counterfactual rate guard

The fixed `0.01 W` net-rate ceiling is superseded before execution. No production/model threshold is changed.

REV1 now requires the counterfactual net rate to fit a budget derived from the already-authorized component guards:

```text
counterfactual_budget_W
  = transport_delta_ceiling_J_per_kg * |recirculation_flow_kg_per_s|
  + mass_identity_ceiling_kg_per_s * max(|h_reference|, |h_suction|)
  + roundoff_budget_W
```

with:

```text
transport_delta_ceiling = 0.001 J/kg
mass_identity_ceiling    = 1E-9 kg/s
roundoff_budget          = 0.001 W
```

At the representative frozen flow the resulting budget is approximately `0.102237 W`.

The adjudicator also checks the exact algebraic decomposition:

```text
net = (h_reference - h_suction) * recirculation_flow
    + h_suction * (recirculation_flow - pump_flow)
```

with an identity residual ceiling of `1E-6 W`.

This does not weaken the reference-state requirement. `|href_IF97 - hsuction_mode2| <= 0.001 J/kg` remains unchanged and is still the primary thermodynamic closure criterion.

## PowerShell compatibility amendment

<!-- NRS-MARKER:DIAG3-REV1-AMENDMENT1-POWERSHELL -->

REV1 validator and adjudicator now use only a .NET SHA-256 stream helper. `Get-FileHash` is not an allowed dependency for this gate. Both scripts remain ASCII-only and stay within the PowerShell feature subset already proven by the successful Planning 1 audit.

The general project authoring rule is updated accordingly: future validators/adjudicators must not introduce a cmdlet dependency unless that cmdlet is already proven in the target environment or the script contains a compatible fallback.

## Authority

<!-- NRS-MARKER:DIAG3-REV1-AMENDMENT1-AUTHORITY -->

This amendment changes only test-only harness portability and the internal consistency of a diagnostic adjudication guard. It does not change production physics, seeds, model thresholds, C4, canonical exact-v9, historical test semantics, R3 status or R4 authority.

After this amendment audit passes, the only next activity remains:

`EXECUTE-DIAGNOSTIC3-REV1-TEST-ONLY`

`repair-owner=UNSELECTED` remains mandatory.
