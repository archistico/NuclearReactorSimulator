# M10.9.4.1-D.4.1 Validation Checklist

## Candidate identity

**Milestone:** M10.9.4.1-D.4.1 — Turbine Valve Replay, Reset & Travel Ownership Hardening
**Validated parent:** M10.9.4.1-D.4
**Candidate status:** implementation prepared; local compilation and validation required

## Scope

This candidate changes no turbine thermodynamic law, hydraulic resistance, controller tuning, protection threshold, fixed timestep, generator/grid scale or Phase E contract.

It hardens the validated D.4 valve station by:

- assigning an optional travel-rate contract directly to each turbine stop/isolation valve admission-train definition;
- preserving `null` as the historical instantaneous-travel behavior for legacy definitions, even when other secondary valves are rate-limited;
- appending the new optional factory parameter so existing positional call sites retain their previous meaning;
- removing the runtime dependency on the control-valve actuator when constructing STOP OPEN/CLOSE requests;
- verifying STOP, ADMISSION, AUTO/MANUAL and manual-demand commands through deterministic full replay;
- verifying checkpoint restoration while requested and actual valve positions differ during finite travel;
- verifying trip override preserves STOP OPEN and that accepted canonical reset resumes finite opening without hidden repair.

## Required automated gate

Run from the repository root:

```powershell
scripts\run-turbine-valve-hardening-tests.cmd
```

Then run the complete ordinary suite:

```powershell
dotnet test
```

The candidate passes only when:

- compilation succeeds;
- every ordinary test passes;
- the D.4.1 focused script passes;
- no existing explicit test is weakened, removed or made non-explicit;
- the ordinary suite continues to skip only the intentional long/audit tests.

After the ordinary gate, rerun the complete explicit pack:

```powershell
scripts\run-turbine-admission-authority-audit.cmd
scripts\run-turbine-governor-actuator-tracking-audit.cmd
scripts\run-gameplay-long-tests.cmd
scripts\run-operational-envelope-audit.cmd
scripts\run-reference-plant-scale-audit.cmd
```

## Required regression observations

### Stop-valve travel ownership

- a current definition can declare a STOP travel rate independent of control/admission actuators;
- a legacy definition with no rate remains valid and instantaneous;
- with intentionally different STOP and ADMISSION rates, one deterministic step moves each valve according to its own rate.

### Replay and checkpoint

- the recording contains typed STOP/ADMISSION and control-valve authority commands with target and numeric value preserved;
- full replay reproduces the final fingerprint;
- seek to the in-flight checkpoint reproduces its fingerprint;
- restored requested positions, actual positions, MANUAL mode and manual demand match the checkpoint.

### Trip and reset

- turbine trip forces the effective STOP position closed;
- the requested STOP OPEN target remains 100%;
- reset occurs only through the canonical readiness/acceptance seam;
- after accepted reset, trip override clears and finite opening resumes from the committed closed state;
- no direct state repair or target rewrite occurs.

## Manual TURBINE-station check

Confirm in the desktop application:

- STOP and ADMISSION command enablement is understandable;
- actual and target positions remain visually distinct during travel;
- the manual-demand slider does not dispatch until APPLY;
- pending demand feedback is clear;
- `TRIP OVERRIDE · STOP FORCED CLOSED` remains visible while active;
- after accepted reset, the STOP valve visibly resumes toward the preserved target.

## Promotion rule

D.4.1 is **VALIDATED**. It is the parent baseline for M10.9.4.1-E.2.
