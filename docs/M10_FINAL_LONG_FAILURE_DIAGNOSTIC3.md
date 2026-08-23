# M10 Final Long Failure Diagnostic 3 — Exact-v5 Reference Operating-Point Candidate

## Status

**HOTFIX 1 EXECUTION PASS / EXACT-V5 ENGINEERING NOT QUALIFIED — superseded for active investigation by Diagnostic 4.**

The original Diagnostic 3 candidate failed its Debug build with one test-only CS0103 because `HydraulicNumericalCouplingMode` lacked the `NuclearReactorSimulator.Domain.Plant` import. Hotfix 1 corrected only that namespace import. The user then reported the complete Diagnostic-3 script PASS and returned the full artifact folder.

The returned 600 s run is finite, trip-free and rollback-free, but exact-v5 does **not** satisfy the engineering qualification rule written by this document: drum inventory/level and common-mode pressure/thermal state remain materially monotonic. Exact-v5 therefore stays diagnostic-only; exact-v4 remains the authoritative production default.

M10 remains open. M11 remains blocked. The replacement long campaign is still **not authorized**. See `M10_FINAL_LONG_FAILURE_DIAGNOSTIC4.md` for the next evidence step.

## 1. Diagnostic 2 conclusions

### LR-M1 — Hotfix 1 accepted

The incremental live MISSION path preserved semantic equivalence for every synthetic prefix size through 100,000 samples while removing prefix-dependent cost. At 100,000 samples the returned census measured approximately:

- incremental score projection: **3.35 us/call**, about **2,248 B/call**;
- bounded timeline projection: **3.245 us/call**, about **2,600 B/call**;
- recent demand changes retained: **1** for the constant-demand synthetic case;
- `semantic_equivalence=True`.

The same measurements remained in the same microsecond/allocation class from 1,000 through 100,000 samples. LR-M1 is therefore accepted as an Application read-side repair: the live refresh is O(1) with respect to elapsed sample count, while replay/offline full-prefix semantics remain unchanged.

### LR-H1 — controller bias is not the immediate driver

The exact-v4 300 s controller/branch census confirmed the Diagnostic-1 inventory result with canonical flow telemetry. Over the final 60 s:

- `outlet dm/dt = -7.9140055968 kg/s`;
- mean `channel - return = -7.9137916804 kg/s`;
- residual slope is only about `+0.002029 kg/s2`;
- the reactor-primary `flow-control` controller remains **Manual**, output **100%**, integral slope exactly **0**;
- the steam-drum level-controller integral slope is only about `-0.0003839 /s`; its output falls as the already-existing drum level excess grows.

At 300 s the canonical branch values are approximately:

```text
pump    253.272 kg/s
channel 253.227 kg/s
return  261.076 kg/s
residual -7.849 kg/s
```

The near one-to-one agreement between outlet inventory loss and channel-return residual means the immediate owner is the **authored primary reference operating point / branch continuity balance**. The primary flow controller is not integrating the plant away from equilibrium; the level controller is primarily reacting to the redistribution.

This does **not** prove that 253–260 kg/s is the asymptotic plant equilibrium. The observed exact-v4 flow is still moving at 300 s. Diagnostic 3 therefore introduces an explicitly authored **reference-point probe**, not a claimed solved steady state.

## 2. Why exact-v5 instead of editing exact-v4

Exact-v4 is already authoritative replay/provenance and must remain immutable. Any materially different initial inventory/pressure/thermal distribution therefore receives a new exact identity:

```text
integrated-operations-desktop-stable@5
```

Diagnostic 3 does not register @5 as the production selector. Authoritative production remains:

```text
integrated-operations-desktop-stable@4
CorrelationConsistentInverseDomain
FourNodeBranchContinuityCorrectedCommitOptIn
10 ms
```

The new factory is instantiated only by the focused diagnostic route.

## 3. Exact-v5 reference-point construction

The probe chooses **260 kg/s** as a round diagnostic reference inside the late Diagnostic-2 operating region. This is a test point, not a frozen calibration target.

The existing production hydraulic coefficients remain unchanged:

```text
channel resistance = 25 Pa*s2/kg2
return resistance  = 25 Pa*s2/kg2
pump pipe resistance     = 25 Pa*s2/kg2
pump internal resistance = 25 Pa*s2/kg2
rated pump head = 1.0 MPa
```

At 260 kg/s each channel/return leg requires:

```text
DeltaP = R q^2 = 25 * 260^2 = 1.690 MPa
```

With the unchanged 280 °C drum saturation pressure of about `6.416459 MPa`, the authored pressure grade is therefore:

```text
drum     6.416459 MPa
outlet   8.106459 MPa
pressure 9.796459 MPa
suction 12.176459 MPa
```

The pump relation includes both its pipe and internal resistance (`50 Pa*s2/kg2` total), so the suction pressure is selected such that the unchanged 1 MPa active boost also resolves to the same 260 kg/s flow.

The pressure-header and suction nodes remain 280 °C subcooled liquid; only their density/compression seed is changed to represent that pressure grade. The outlet is seeded as a saturated mixture at `8.106459 MPa` with diagnostic quality `0.0358817429` rather than as the old 280 °C compressed-liquid copy.

The existing initial fission power remains **30 MW**. With current-v2 thermal coupling, the unchanged heat split/conductances imply the matching solid-body seed temperatures:

```text
outlet saturation temperature ~= 295.934 °C
fuel     ~= outlet + 21 K = 316.934 °C
structure ~= outlet +  6 K = 301.934 °C
```

No heat-transfer coefficient, fission-power coefficient, hydraulic resistance, pump head, controller tuning, thermodynamic envelope or acceptance tolerance is changed.

## 4. Additive seed seam

`ColdShutdownInitialConditionFactory.CreateRuntimeEngineForOperationalSeed` receives optional seed-only overrides for:

- suction compression fraction;
- pressure-header compression fraction;
- outlet saturation pressure + vapor quality as a pair;
- fuel temperature;
- structure temperature.

All defaults preserve the pre-existing recipe path. Exact-v1..v4 call sites do not supply the new arguments, so their authored semantics are not intentionally changed. The ordinary suite and the explicit production-selector assertion are required to catch any accidental regression.

## 5. Diagnostic 3 — 600 s qualification probe

Run:

```bat
scripts\run-m10-final-long-failure-diagnostic3.cmd
```

The route performs:

1. Debug build with warnings-as-errors;
2. complete ordinary suite;
3. focused LR-M1 Hotfix-1 semantic-equivalence regression;
4. exact-v5 600 s reference operating-point census.

The new 600 s route deliberately crosses the time region in which the original exact-v4 LR-H1 failed (after 300 s and before 600 s).

Once per simulated second it records:

- outlet mass, specific volume, specific internal energy, pressure, temperature, phase and quality;
- suction / pressure-header / outlet / drum pressure grade;
- pump / channel / return flows and channel-return residual;
- drum mass and level;
- fuel and structure temperatures;
- drum level-controller error, integral and output;
- corrected-commit / rollback / trip counts.

Artifacts:

```text
40-v5-reference-trajectory.csv
41-v5-final60-summary.txt
42-v5-initial-reference-point.txt
00-progress.txt
```

Diagnostic 3 intentionally freezes **no new drift threshold** before observing the trajectory. The execution must remain finite, trip-free and rollback-free, but promotion requires engineering review of the returned final-60 s slopes. In particular, the candidate must not merely postpone the same monotonic outlet inventory drift beyond 600 s.

Return the complete:

```text
artifacts\m10-final-long-diagnostic3
```

before any production activation step.

## 6. Returned result and decision

Diagnostic 3 completed, but the exact-v5 candidate is **NOT QUALIFIED**. The 260 kg/s instantaneous pressure-grade seed evolves to a late hydraulic regime near 103 kg/s. Branch continuity improves strongly, yet the full plant remains non-stationary.

Returned final state at 600 s:

```text
outlet mass  1425.894 kg
drum mass    7267.712 kg
drum level   0.956714
pump flow     103.196 kg/s
channel flow  103.023 kg/s
return flow   103.205 kg/s
```

Returned final-60 s slopes include:

```text
outlet mass       -0.17960 kg/s
drum mass         +0.79872 kg/s
drum level        +8.6082e-5 fraction/s
outlet pressure   -1001 Pa/s
drum pressure      -984 Pa/s
fuel temperature  -0.01063 °C/s
structure temp.    -0.01154 °C/s
```

The next step is Diagnostic 4, which leaves exact-v5 unchanged and exposes canonical drum, feedwater/steam-export and coupled energy-balance terms. No production activation is permitted from Diagnostic-3 survival alone.

## 7. Hard non-scope

Diagnostic 3 does not:

- reinterpret or edit exact-v4;
- switch the production selector;
- widen the water/steam state envelope;
- clamp a conserved thermodynamic state;
- change the 19 frozen I.3 budgets or conservation ceilings;
- change hydraulic resistance, pump head, thermal conductance or controller gains;
- declare 260 kg/s to be a validated equilibrium target;
- authorize the replacement long campaign;
- close M10 or unblock M11.

The executable/frozen intent for this probe is recorded in `eng/m10-final-long-diagnostic3-contract.json`.

The historical `eng/m10-final-long-baseline-src.sha256` remains frozen to the failed first long campaign. It must not be rebased for this diagnostic candidate.
