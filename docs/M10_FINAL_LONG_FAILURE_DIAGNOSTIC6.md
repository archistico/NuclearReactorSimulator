# M10 Final Long Failure Diagnostic 6 — Exact-v6 Analytical Whole-Cycle Equilibrium Candidate

**CANDIDATE — exact-v6 is diagnostic/qualification-only; exact-v4 remains production; replacement long unauthorized.**

## 1. Evidence inherited from Diagnostic 5

Diagnostic 5 completed successfully and returned the full whole-cycle owner census for exact-v5. It confirms that exact-v5 survives 600 s but remains a transient rather than an equilibrium:

- the primary loop moves from the authored ~260 kg/s probe toward ~103 kg/s;
- the drum reaches ~0.9567 level and remains overfilled;
- the liquid secondary side is close to mass closure by 600 s, with condensate-pump and condenser-condensation flows both near 12.97 kg/s;
- feedwater remains slightly above separated steam and the historical drum overfill is largely accumulated transient inventory;
- multiple pressure nodes still drift at about the kPa/s scale, so the 600 s snapshot must not be copied as a seed;
- full mass/energy ownership remains conservative.

Diagnostic 6 therefore authors a new exact-version state from the unchanged model equations rather than from a terminal transient snapshot.

## 2. Correction to the exact-v5 260 kg/s interpretation

The earlier shorthand that the 260 kg/s probe had “omitted pump internal resistance” is incorrect and is superseded by this document.

Exact-v5 already used the main pump path resistance and internal resistance. Its actual defect was the authored pressure reservoir between `suction` (~12.176 MPa) and `drum` (~6.416 MPa). Because steam-drum liquid recirculation transfers liquid from the drum inventory to the suction header, a stationary authored state cannot rely on that ~5.76 MPa suction/drum separation remaining indefinitely.

For a stationary candidate we set:

```text
P_suction = P_drum
```

and close the unchanged hydraulic equations:

```text
pump path resistance      25 Pa·s²/kg²
pump internal resistance  25 Pa·s²/kg²
channel resistance        25 Pa·s²/kg²
return resistance         25 Pa·s²/kg²
rated pump head             1,000,000 Pa

1,000,000 = (25 + 25 + 25 + 25) q²
q = 100 kg/s
```

This yields the authored primary pressure grade:

```text
suction/drum  6.416459281680372 MPa
pressure      6.916459281680372 MPa
outlet        6.666459281680372 MPa
```

The pressure-header liquid state includes the pump work, and the outlet state includes the unchanged core heat deposition.

## 3. Secondary mechanical operating point

At synchronous 3000 rpm, 5 MWe requested electrical output and 98% generator efficiency require:

```text
5 / 0.98 = 5.102040816 MW generator mechanical input
+ 0.500000000 MW passive rotor loss
= 5.602040816 MW turbine shaft power
```

The unchanged turbine work model is capped at:

```text
500 kJ/kg nominal × 0.86 efficiency = 430 kJ/kg
```

so the authored secondary throughput is:

```text
q_secondary = 5.602040816 MW / 430 kJ/kg
            = 13.028001898433793 kg/s
```

The unchanged steam-source, main-steam-line, valve and turbine-expansion resistances then determine the pressure grade. The control-valve opening required by that grade is ~27.3123%, close to the historical loaded bias but derived from the current model rather than copied from it.

## 4. Condenser and feedwater closure

The condenser exhaust temperature is solved from the unchanged UA relation using the same 13.0280 kg/s throughput, 20 °C cooling-water boundary and turbine exhaust enthalpy:

```text
T_exhaust = 42.1258335170 °C
P_exhaust = 8263.444140 Pa
Q_condenser ≈ 27.104146 MW
```

The hotwell is seeded as saturated liquid at the same pressure. The unchanged 42% condensate-pump bias then determines the feedwater-inventory state. The feedwater-pump speed required to deliver the same 13.0280 kg/s into the 6.416459 MPa drum is ~96.8891%.

Thus the initial level and hotwell controller outputs are bumpless at the analytically closed flows instead of being copied from the late exact-v5 transient.

## 5. Nuclear/thermal closure

With the unchanged component equations and 5 MWe export, 30 MW is not the stationary whole-cycle heat input. Closing steam enthalpy rise, primary-pump work and feedwater-pump work gives:

```text
fission power = 32.48425387176408 MW
neutron population relative = 0.3248425387176408
```

The current 70% fuel / 10% structure / 20% direct-coolant deposition model and unchanged conductances then give:

```text
outlet saturation temperature  282.5453255101 °C
outlet quality                    0.2118679002
fuel temperature                305.2843032203 °C
structure temperature           289.0421762844 °C
```

This is an authored-state change only. No heat-transfer coefficient, turbine efficiency, pump head, valve resistance, condenser UA, control gain, thermodynamic envelope or conservation budget is changed.

## 6. Exact-version and production rules

Diagnostic 6 adds the distinct exact-version identity:

```text
integrated-operations-desktop-stable@6
```

The following remain immutable:

- exact-v4 authoritative production identity;
- exact-v5 failed diagnostic identity;
- LR-M1 Hotfix 1 semantics;
- fixed 10 ms production timestep;
- corrected-commit hydraulic ownership and rollback behavior;
- CorrelationConsistentInverseDomain thermodynamic closure;
- all physical component coefficients and control gains.

The production selector remains exact-v4. Merely compiling or completing the 600 s Diagnostic-6 run does not activate exact-v6.

## 7. Diagnostic 6 gate

Run:

```bat
scripts\run-m10-final-long-failure-diagnostic6.cmd
```

The script performs:

1. Debug build with warnings-as-errors;
2. complete ordinary suite;
3. LR-M1 Hotfix 1 semantic-equivalence regression;
4. explicit 600 s exact-v6 whole-cycle equilibrium census.

Artifacts:

```text
artifacts\m10-final-long-diagnostic6
  00-progress.txt
  70-v6-whole-cycle-equilibrium-trajectory.csv
  71-v6-node-state-trajectory.csv
  72-v6-final60-node-slopes.csv
  73-v6-whole-cycle-equilibrium-summary.txt
```

## 8. Decision rule

The execution test requires finite evidence, zero active-trip steps and zero corrected-commit rollbacks. It deliberately does **not** invent a new numerical drift tolerance before evidence exists.

After the returned artifacts are reviewed, exact-v6 may advance only if the whole cycle is demonstrably bounded: primary and secondary mass flows remain mutually coherent, drum/hotwell inventories remain bounded, pressure/temperature slopes no longer show the material monotonic drift seen in exact-v5, approximately 5 MWe operation is preserved, and the existing full energy-path closure remains conservative.

If those conditions are not met, exact-v6 remains failed diagnostic evidence and the next step remains owner-specific diagnosis. If they are met, the next step is a **separate production-activation/requalification candidate**, not the replacement long itself.

## 9. Hard non-scope

Diagnostic 6 does not change the production selector, widen the water/steam domain, retune hydraulic resistance, pump head, controller gains, condenser UA, turbine work, I.3 budgets or conservation ceilings. It does not authorize the replacement long, close M10 or unblock M11.
