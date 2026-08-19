# M10.9.4.1-D.1 Validation Checklist

## Purpose

Validate the current-v2 turbine admission phase-policy closure without conflating it with the later governor-authority redesign.

## Build and ordinary suite

```text
dotnet clean
dotnet restore
dotnet build --no-restore
dotnet test --no-build
```

Expected: zero warnings/errors and no ordinary regression failures.

## Focused contracts

Verify that automated tests cover:

- legacy stage definitions default to `LegacyUnrestricted`;
- sustained current-v2 desktop and synchronization stages use `VaporMassFractionLimited`;
- pure-liquid current-v2 admission has commanded hydraulic flow but zero effective stage transfer and zero shaft power;
- wet current-v2 admission transfers only vapor-fraction mass;
- wet-steam specific work is not quality-penalized twice;
- turbine thermofluid mass/power residuals remain within existing tolerances.

## Integrated journeys

Run the existing explicit synchronization and long-running operational-envelope gates used by the current repository. Confirm no new turbine, condenser, drum or protection regression.

## Manual observation

At the sustained operating point, verify turbine inlet remains vapor/saturated-vapor-like and normal power is materially unchanged. D.1 is not accepted if normal dry-steam output is unintentionally reduced.

## Non-goals

Do not retune stage resistance, valve resistance, droop, actuator travel or PID gains during this gate. Those belong to D.2/D.3 after authority evidence.
