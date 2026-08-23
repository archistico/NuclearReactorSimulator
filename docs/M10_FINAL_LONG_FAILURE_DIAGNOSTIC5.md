# M10 Final Long Failure Diagnostic 5 — Exact-v5 Whole-Cycle Authored-State Owner Census

**CANDIDATE — evidence-only; exact-v5 remains NOT QUALIFIED; exact-v4 remains production; replacement long unauthorized.**

## 1. Why Diagnostic 5 exists

Diagnostic 4 completed successfully and closed the two high-level conservation questions without justifying a new operating-point seed.

The returned 540–600 s evidence shows that the model is conservative, but exact-v5 is not a full-plant equilibrium:

- measured drum inventory slope is about `+0.79872 kg/s`;
- the correct M4.4 closed-cycle drum balance is `return + internal feedwater pump - separated steam - recirculation`, not the legacy M3 primary feedwater boundary;
- `return - recirculation` contributes only about `+0.01270 kg/s`;
- `internal feedwater pump - separated steam` contributes about `+0.78540 kg/s`, approximately 98.4% of the drum accumulation;
- the legacy primary feedwater boundary is zero by design in the M4.4 closed secondary cycle and must not be used as the physical feedwater owner;
- full energy-path closure remains essentially exact: mean late `NetReactorToGridExternalPower` and coupled stored-energy change are both about `-2.477 MW`, while the full closure residual is microscopic.

Therefore the remaining problem is not hidden numerical mass/energy creation. The authored cycle inventories are relaxing toward a different secondary-cycle state while the drum level controller is compensating.

## 2. Why an exact-v6 seed is still premature

A seed that changes only primary pressure, primary flow, drum level or the level-controller bias is incomplete.

The feedwater pump is a pressure-source pump between `feedwater-inventory` and `drum`. During the exact-v5 transient the feedwater inventory itself becomes pressurized. A new controller bias derived from the 600 s pump output cannot be transplanted into the original low-pressure 40 °C feedwater-inventory seed and still represent the same hydraulic point.

The same applies to the remainder of the closed cycle: `steam`, staged steam-path inventories, `exhaust`, `hotwell` and `feedwater-inventory` all participate in the mass/energy operating point.

Diagnostic 5 therefore records the complete authored thermofluid state before any exact-v6 exists.

## 3. Scope

Diagnostic 5 reuses exact-v5 unchanged for the same 600 s horizon and records once per simulated second:

- the corrected drum mass balance using the internal M4.4 feedwater-pump flow;
- feedwater/condensate pump flow, effective speed and feedwater active pressure boost;
- level and hotwell controller error, integral and output;
- condenser condensation flow and the existing full energy-path terms;
- node-level mass, specific internal energy, pressure, temperature, phase and vapor quality for `suction`, `pressure`, `outlet`, `drum`, `steam`, `header`, `stop-out`, `control-out`, `turbine-inlet`, `exhaust`, `hotwell`, and `feedwater-inventory`;
- final-60 s node slopes for mass, pressure, temperature and specific internal energy.

No new operating-point value is declared by this diagnostic.

## 4. Artifacts

The run writes:

```text
artifacts\m10-final-long-diagnostic5
  00-progress.txt
  60-v5-whole-cycle-owner-trajectory.csv
  61-v5-node-state-trajectory.csv
  62-v5-final60-node-slopes.csv
  63-v5-whole-cycle-owner-summary.txt
```

## 5. Decision rule

After the artifacts are returned, an exact-v6 may be authored only if the evidence permits a mutually coherent initialization of the primary loop, drum, steam path, condenser/hotwell/feedwater train, controller biases and thermal inventories.

The seed must preserve conservation owners and may not reinterpret the zero legacy primary feedwater boundary as zero physical feedwater flow.

If significant node-state drift remains unresolved, the next step remains diagnostic rather than production activation.

## 6. Hard non-scope

Diagnostic 5 does not modify exact-v4 or exact-v5 runtime semantics, create exact-v6, switch the production selector, change controller gains, change hydraulic resistance or pump head, widen the thermodynamic envelope, alter I.3/conservation budgets, authorize a replacement long, close M10 or unblock M11.
