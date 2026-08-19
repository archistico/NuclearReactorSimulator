# M10.9.4.1-C.2 Validation Checklist

## Scope

Validate explicit installed-capacity ownership on top of locally green B.3 + C.1. C.2 must not retune the validated 40 MW / 20 kg/s operating values.

## Automated gate

```text
dotnet clean
dotnet restore
dotnet build --no-restore
dotnet test --no-build
scripts\run-gameplay-long-tests.cmd
scripts\run-operational-envelope-audit.cmd
```

Expected:

- build: zero warnings / zero errors;
- ordinary suite: all non-explicit tests green;
- existing explicit 60-second gameplay journeys green;
- 300-second operational-envelope audit green;
- no new replay/fingerprint-v1 regression.

## Focused semantic checks

- current-v2 sustained profiles: installed cooling = 40 MW; initial available cooling = 40 MW; maximum condensation flow = 20 kg/s; UA = 1.225 MW/K;
- legacy/v1 definitions: no explicit installed-capacity property; historical input-only ceiling preserved;
- installed-capacity test: installed < available => `INSTALLED CAPACITY` is limiting;
- availability test: available < installed => `AVAILABLE COOLING` is limiting;
- UA test: `UA·ΔT` below both capacities => `SURFACE UA` is limiting;
- runtime fault overlays change available capacity only, not definition-owned installed capacity.

## Manual HMI check

In `TURBINE & SECONDARY CYCLE`, verify the condenser card clearly distinguishes:

- `INSTALLED COOLING · MODEL`;
- `AVAILABLE COOLING · MODEL`;
- `SURFACE UA LIMIT · MODEL`;
- `ACTIVE HEAT-REJECTION LIMIT · MODEL`.

These values are diagnostics, not operator commands.
