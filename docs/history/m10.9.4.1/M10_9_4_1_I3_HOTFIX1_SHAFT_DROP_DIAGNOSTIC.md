# M10.9.4.1-I.3 Hotfix 1 — Full-Horizon Shaft-Drop Diagnostic

## Status

**CANDIDATE / DIAGNOSTIC ONLY.** I.2 remains the authoritative validated baseline. The initial I.3 candidate is failed evidence, not a baseline.

## Trigger

The first I.3 focused run compiled but stopped at logical step 5,500 / 55 simulated seconds:

```text
trip=False
breaker=True
request=5 MWe
gross=4.435 MWe
rotor shaft=0 MW
rotor speed=2996.119 rpm
condenser=7.602 kPa
drum level=57.588%
```

The failure is not automatically classified as a bad test. The pre-existing `OperationalEnvelopeExtendedAuditTests` 300-second steady test also requires rotor shaft power `> 4.5 MW` at every one-second healthy-parallel sample.

## Hotfix contract

Hotfix 1 keeps the health predicate unchanged. It removes only the **early** assertion so that all 300 simulated seconds are observed before the final pass/fail decision.

The trajectory now additionally records:

- canonical total turbine shaft power;
- total turbine steam flow;
- admission-valve flow;
- control/admission valve positions;
- turbine-inlet pressure and temperature;
- turbine-inlet phase.

It writes these additional artifacts:

```text
06-generation-health-violations.csv
07-shaft-drop-episodes.csv
```

The focused script prints the summary and diagnostic paths even when the final unchanged health gate fails.

## Decision rule

Do **not** freeze I.3 tolerance budgets while any generation-health violation remains. Do **not** lower the shaft-power floor merely to make I.3 green.

Use the full-horizon evidence to distinguish at least these cases:

1. **steam/admission collapse at the same samples** — likely real runtime/thermodynamic path discontinuity requiring a separately scoped correction;
2. **canonical turbine shaft remains healthy while presentation rotor shaft alone drops** — observational/projection defect;
3. **isolated mechanical zero with sustained steam and electrical output** — inspect turbine/rotor energy-transfer ownership and sampling semantics before changing a contract;
4. **repeated or multi-second episodes** — treat as operational regression until explained and corrected.

No production code is changed by Hotfix 1 except application metadata.
