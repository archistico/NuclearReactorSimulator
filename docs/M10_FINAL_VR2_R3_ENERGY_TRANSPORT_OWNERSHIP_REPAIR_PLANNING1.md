# M10 Final VR2 R3 - Energy-Transport Ownership Repair Planning 1

NRS-MARKER:R3-ENERGY-TRANSPORT-REPAIR-PLANNING1

Date: 2026-09-20

Status: **CANDIDATE / PLANNING-ONLY / FAMILY-B SELECTED FOR AUDIT**

## Purpose

Select the narrowest production ownership repair after Diagnostic 3 REV1 returned and was independently adjudicated `CAUSAL-CLOSURE-CONFIRMED` at:

`STEAM-DRUM-LIQUID-TRANSPORT-VS-MODE2-SUCTION-TRANSPORT`

The returned evidence proves the approximately `-5.2162718 MW` first-step anomaly is an energy-transport ownership discontinuity, not a mass-balance, controller, C4 inverse-domain, or seed-threshold defect. The independent IF97 counterfactual reduces the discontinuity to `0.014754295349121094 W`, within the frozen derived budget `0.10223668223103162 W`.

This gate selects an implementation design. It contains **no production repair**.

## Process-authority supersession

NRS-MARKER:R3-LOCAL-QUALIFICATION-AUTHORITY

The earlier Diagnostic 3 adjudication held production repair planning behind GitHub hosted `ordinary-ci`. On 2026-09-20 the project owner explicitly superseded that process hold: local qualification is now authoritative for project advancement; hosted CI is advisory evidence unless explicitly requested again.

The locally returned Exact-V9 Cross-Host Determinism Contract V2 qualification is frozen in this candidate and records:

- static validator PASS;
- build PASS;
- Fingerprint V1 focused PASS;
- Fingerprint V2 focused PASS;
- authoritative Exact-V9 V2 PASS;
- overall `status=PASS`.

This policy change does not weaken any physics, conservation, deterministic same-host, fast-gate, or R3 acceptance criterion.

## Frozen causal evidence

Diagnostic 3 REV1 adjudication establishes:

- first causal checkpoint: `seed-step1`;
- first causal node: `suction`;
- raw mode-2 suction inverse path: `C2LiquidTablePrefix`;
- step-1 mode-2 suction inverse path: `C2MixturePrefix`;
- production observed suction energy rate: `-5216271.784591675 W`;
- production energy identity residual: `6.383657455444336E-05 W`;
- forward mode-1/mode-2 saturation provider: IEEE-754 bit-identical;
- IF97 saturation-pressure delta: `0 Pa`;
- IF97 vs mode-2 suction transport delta: `0.00014754291623830795 J/kg`;
- IF97 minus historical drum-liquid transport: `52162.71184104541 J/kg`;
- IF97 counterfactual net-energy magnitude: `0.014754295349121094 W`;
- counterfactual rate-identity residual: `2.068356699455598E-09 W`.

The critical architectural fact is that `SteamDrumSeparationSolver` constructs an independent default `SimplifiedWaterSteamThermodynamicModel` for saturated liquid/vapor properties, while `IntegratedPrimaryCircuitSolver` gives the selected active closure to `PlantNetworkOrchestrator` and therefore to conserved-inventory resolution.

## Ownership-family comparison

NRS-MARKER:R3-REPAIR-FAMILY-MATRIX

### Family A - model/provider ownership injection

Injecting the currently exposed `IWaterSteamSaturationPropertyProvider` into the steam-drum solver is **insufficient as authored**. Diagnostic 3 REV1 proved that the public forward saturation provider is bit-identical between mode 1 and mode 2. Merely injecting that provider would preserve the historical transport value and would not close the `~52.163 kJ/kg` transport gap.

Family A therefore fails the causal-effect criterion unless it is extended with a new closure-aware transport capability. Once extended that way, its substantive design becomes Family B.

### Family B - explicit closure-aware transport-property contract

Introduce an explicit, allocation-neutral water/steam **transport** capability owned by the active thermodynamic closure. Steam-drum separation consumes this capability only for the specific internal energies and densities used to build advected transport energy. Geometric/presentation saturation behavior (void fraction and drum level) remains on the existing historical contract and is not changed by this repair.

Historical modes 0/1 return their existing transport properties bit-for-bit. Mode 2 obtains saturated liquid/vapor transport properties from the already-versioned `NRSVR2C4.v1.bin` dense saturation payload through a pressure-keyed, allocation-neutral lookup. No IF97 library or test helper enters production.

**Disposition: SELECTED.**

### Family C - bounded mode-2 transport bridge

A mode-2-specific bridge inside `SteamDrumSeparationSolver` could close the observed seam with a smaller local conditional, but it would encode closure identity at the consumer, preserve duplicated ownership, and create a second mode-specific thermodynamic policy outside the active closure. This is less auditable and more likely to drift when future closures are added.

**Disposition: NOT SELECTED.** Retain only as a contingency if the explicit provider design cannot satisfy the frozen fast gate without broadening scope.

## Selected repair owner

NRS-MARKER:R3-REPAIR-OWNER-SELECTED

`repair-owner=ACTIVE-CLOSURE-TRANSPORT-PROPERTY-CONTRACT`

The consumer remains `SteamDrumSeparationSolver`; ownership of the transport properties moves to the active water/steam thermodynamic closure.

The composition root is `IntegratedPrimaryCircuitSolver`, which already receives the active `IFluidThermodynamicModel`. It will pass the explicit transport capability to the steam-drum solver. This injection is wiring; the selected design semantics remain Family B.

## Planned production surface

Implementation 1 may modify exactly these existing production files and add exactly one new production file:

1. **NEW** `src/NuclearReactorSimulator.Simulation/Physics/Fluids/IWaterSteamPhaseTransportPropertyProvider.cs`
2. `src/NuclearReactorSimulator.Simulation/Physics/Fluids/ReferenceConsistentTabulatedInverseResolver.cs`
3. `src/NuclearReactorSimulator.Simulation/Physics/Fluids/SimplifiedWaterSteamThermodynamicModel.cs`
4. `src/NuclearReactorSimulator.Simulation/Physics/Reactor/PrimaryCircuit/SteamDrums/SteamDrumSeparationSolver.cs`
5. `src/NuclearReactorSimulator.Simulation/Physics/Reactor/PrimaryCircuit/Integration/IntegratedPrimaryCircuitSolver.cs`

No other `src/` file is authorized by this plan.

### New capability

The new internal capability returns an allocation-free value type containing only:

- saturated-liquid density;
- saturated-vapor density;
- saturated-liquid specific internal energy;
- saturated-vapor specific internal energy.

It is a transport-property contract, not a replacement for `IWaterSteamSaturationPropertyProvider`.

### Historical modes 0/1

For `HistoricalCorrelationTopology` and `CorrelationConsistentInverseDomain`, the transport capability must reproduce the existing simplified forward saturation transport values bit-for-bit. Default construction and canonical exact-v9 remain unchanged.

### Mode 2

For `ReferenceConsistentTabulatedInverseDomain`, the capability must use the immutable dense saturation data already loaded by `ReferenceConsistentTabulatedInverseResolver` from `NRSVR2C4.v1.bin`.

The planned lookup is:

`PRESSURE-KEYED-DENSE-SATURATION-LINEAR-INTERPOLATION`

It must:

- binary-search the monotonic dense saturation pressure axis;
- linearly interpolate liquid/vapor specific volume and specific internal energy;
- derive densities as `1 / specificVolume`;
- perform zero resource I/O and zero payload decoding at resolve time;
- allocate zero managed bytes per steady-state lookup after payload initialization;
- leave the payload bytes and SHA-256 unchanged.

### Steam-drum consumption boundary

`SteamDrumSeparationSolver` shall use the closure-owned transport properties only where it currently constructs liquid/vapor advected energy from specific internal energy, pressure, and density.

The repair must **not** change:

- `WaterSteamVoidFractionSolver`;
- drum-level geometry;
- the public forward saturation-provider semantics;
- steam-source hydraulic resistance/capacity;
- recirculation mass-flow policy;
- fluid-node conserved-inventory integration equations.

This separation is intentional: the proven causal seam is transport energy, not drum geometric presentation.

## Why the selected design closes the causal seam

For mode 2 the current steam-drum liquid source uses historical forward liquid properties, while the destination suction inventory is resolved in the reference-consistent C4 inverse domain. The returned independent reference evidence places the physically consistent liquid transport at essentially the mode-2 suction transport value, not at the historical drum value.

Making the active closure own transport properties therefore aligns source and destination energy bookkeeping without:

- copying the destination suction state into the source;
- retuning the raw seed;
- modifying C4 data;
- adding IF97 as a runtime dependency;
- relaxing any dynamic acceptance envelope.

## Implementation 1 qualification contract

NRS-MARKER:R3-REPAIR-IMPLEMENTATION-FAST-GATE

A future Implementation 1 PASS must prove all of the following locally.

### Static / architecture

- only the five planned production paths above change;
- no C4 payload byte changes;
- no default closure-mode change;
- no canonical exact-v9 factory/source reinterpretation;
- no threshold or envelope changes;
- no production IF97 dependency;
- no new per-step managed allocation in the mode-2 transport lookup after payload warmup.

### Focused thermodynamic transport

At the frozen Diagnostic 3 drum state:

- reference-consistent mode-2 liquid transport vs frozen IF97: `<= 0.001 J/kg` absolute delta;
- reference-consistent mode-2 liquid transport vs raw mode-2 suction selected transport: `<= 0.001 J/kg` absolute delta;
- historical mode 0/1 transport values: bit-identical to the pre-repair baseline;
- payload load/decode/I/O invariants remain unchanged.

### Seed-step causal seam

For the exact returned seed-step1 scenario:

- suction mass identity residual `<= 1E-9 kg/s`;
- production energy identity residual `<= 0.001 W`;
- corrected transport identity residual `<= 1E-9 J/kg`;
- corrected counterfactual-equivalent net-energy magnitude `<= 0.10223668223103162 W`;
- energy-rate identity residual `<= 1E-6 W`;
- the historical approximately `-5.216 MW` discontinuity must not remain.

### Frozen first-100-step fast gate

Run the same opt-in mode-2 exact-v9-equivalent candidate for 100 Running steps at 10 ms. Every step must remain finite with zero trip, breaker opening or rollback, and **zero** envelope violations:

- electrical export: `4.99 .. 5.01 MWe`;
- primary-pump mass flow: `99.9 .. 100.1 kg/s`;
- steam-drum liquid level: `0.49 .. 0.51`;
- governor output: `29.27 .. 29.30 %`.

The fast gate may not introduce a new tolerance or special startup exemption.

### Local regression

- build PASS;
- complete ordinary local suite PASS;
- current Exact-V9 Determinism Contract V2 focused/audit tests PASS;
- historical steam-drum and fluid-energy-transport tests PASS.

## Successor sequence

A returned local PASS of this planning audit authorizes only:

`R3-ENERGY-TRANSPORT-OWNERSHIP-REPAIR-IMPLEMENTATION1`

A returned Implementation 1 fast-gate PASS may authorize:

`R3-SHORT-EXACT-V9-EQUIVALENT-SHADOW-COMPOSITION-REQUALIFICATION3`

R3 remains RED until that 120 s / 12,000-step qualification returns and is adjudicated PASS. R4 Planning 1 remains blocked until then.

## Explicit non-authority

This planning candidate does **not** authorize a production edit by itself. It does not authorize seed retuning, threshold changes, C4/payload mutation, default mode-2 activation, canonical exact-v9 reinterpretation, R3 PASS, R4, VR3 or P3-R1.
