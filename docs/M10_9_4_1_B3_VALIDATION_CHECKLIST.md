# M10.9.4.1-B.3 — Validation Checklist

## Candidate

**Steam-drum low-inventory diagnostics and low-low-level protection**

B.2 is the last user-validated checkpoint. B.3 must not be promoted until every gate below is green in the local .NET 10 environment.

## 1. Clean build

```text
dotnet clean
dotnet restore
dotnet build --no-restore
```

Required: 0 warnings, 0 errors.

## 2. Ordinary suite

```text
dotnet test --no-build
```

Required: no unexpected failures.

## 3. Focused B.3 regressions

Verify:

- historical v1/minimal-protection profile has no `steam-drum-level-low` alarm and no `steam-drum-low-low-level` protection;
- current-v2 warning threshold is 25% measured level;
- current-v2 low-low trip threshold is 10% with 20% reset threshold;
- low-low actions are ReactorScram + TurbineTrip + GeneratorTrip;
- a forced 5% measured level latches the low-low protection;
- fully vaporized drum reports zero separable liquid, committed-liquid depletion and unavailable water/steam separation;
- inventory-limited recirculation exposes a positive deficit;
- HMI level scale shows the 10–25% warning band and 10% low-low protection marker;
- HMI MODEL diagnostics expose separable-liquid mass/fraction without becoming protection inputs.

## 4. Explicit 60-second journeys

```text
scripts\run-gameplay-long-tests.cmd
```

Required: existing sustained-generation and synchronization journeys remain green with no unexpected low-level alarm or trip.

## 5. 300-second operational-envelope audit

```text
scripts\run-operational-envelope-audit.cmd
```

Required:

- full 300 s reference journey passes;
- no unexpected low-level warning/trip at the healthy 5 MWe point;
- B.1/B.2 inventory/source diagnostics remain finite;
- mass/energy conservation remains within existing budgets;
- no new oscillatory or monotonic depletion introduced by presentation/protection changes.

## 6. Manual HMI check

On the Primary Circuit / steam-drum area verify:

- drum-level gauge visibly marks the low warning region and low-low protection point;
- `SEPARABLE LIQUID · MODEL` and `LIQUID INVENTORY · MODEL` are readable;
- inventory status text is understandable;
- no MEASURED/MODEL provenance is confused.

Promote B.3 only after all gates are green. If B.3 is green, Phase B can close and development may move to Phase C condenser phase-change closure. General node pressure/design-envelope diagnostics remain separately tracked and must not be silently folded into the condenser change.
