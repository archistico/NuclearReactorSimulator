# M10.9.4.1-G.4 Validation Checklist

## Automated gate

Run from the repository root:

```bat
APPLY_UPDATE.cmd
dotnet build
scripts\run-turbine-expansion-enthalpy-tests.cmd
dotnet test
```

Expected ordinary discovery after the four new non-explicit G.4 regressions:

```text
passed:   1029
failed:   0
skipped:  33 explicit
total:    1062
```

Then run the cumulative hardening gates already required by M10.9.4.1.

## Required G.4 evidence

- both current-v2 sustained profiles configure turbine stage groups with `SpecificEnthalpy`;
- legacy/current-v1 turbine stage groups remain `SpecificInternalEnergy`;
- current-v2 inlet advection is `h*m_dot`;
- current-v2 exhaust advection is inlet enthalpy transport minus shaft work;
- turbine energy-ownership residual closes to numerical tolerance;
- thermofluid balance remains conservative;
- mechanical rotor audit remains conservative;
- thermodynamic turbine work, efficiency, governor and protection settings are unchanged;
- the explicit CSV and summary are produced under `artifacts\g4-turbine-expansion-enthalpy`;
- ordinary suite and cumulative long-running/operational-envelope gates remain green.

## Promotion rule

Do not mark G.4 or Phase G validated until the user confirms compilation, focused G.4 tests, ordinary suite and the requested cumulative gates. Phase H starts only from that validated checkpoint.
