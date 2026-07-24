# M10.9.4.1-C.2 Hotfix 1 validation checklist

## Automated
- `dotnet clean`
- `dotnet restore`
- `dotnet build --no-restore`
- `dotnet test --no-build`
- `scripts\run-gameplay-long-tests.cmd`
- `scripts\run-operational-envelope-audit.cmd`

## Manual primary HMI
- Run the current-v2 sustained desktop point.
- Confirm MCP total/pump flow, core-channel/return flow, drum inlet and liquid recirculation no longer flicker between extreme per-step values in the operator-facing HMI.
- Confirm values still react promptly to real operating changes; 0.5 s lag must not look frozen.
- Confirm model pressures/thermodynamic diagnostics remain available and are not mislabeled as measured flow.

## Manual generator re-synchronization
- From stable parallel operation, reduce load before normal breaker opening where practical.
- Open the generator breaker.
- If phase is outside the synchronization window with near-zero frequency slip, confirm the HMI explicitly explains that waiting alone will not change phase.
- Use SPEED RAISE/LOWER to create phase slip, observe Δf / Δphase / ΔV, return near synchronous speed and verify `SYNC READY` becomes reachable.
- Confirm CLOSE BREAKER remains blocked outside the canonical synchronization window and no hidden auto-synchronizer closes it.
