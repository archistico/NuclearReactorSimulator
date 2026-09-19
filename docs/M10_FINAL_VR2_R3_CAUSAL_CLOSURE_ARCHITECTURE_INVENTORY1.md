# M10 Final VR2 R3 - Causal Closure Architecture Inventory 1

<!-- NRS-MARKER:R3-CAUSAL-CLOSURE-ARCHITECTURE-INVENTORY1 -->

## Status and non-authority

`DOCUMENTATION-AND-ARCHITECTURE-INVENTORY-ONLY`

Diagnostic 3 REV1 returned evidence is independently adjudicated `CAUSAL-CLOSURE-CONFIRMED`. The causal seam is localized to `STEAM-DRUM-LIQUID-TRANSPORT-VS-MODE2-SUCTION-TRANSPORT`; `repair-owner=UNSELECTED` and R3 remains RED.

This inventory does not open `R3 Energy-Transport Ownership Repair Planning 1`. Hosted GitHub `ordinary-ci` remains the explicit entry hold. The only next authority remains `CONFIRM-HOSTED-ORDINARY-CI-GREEN`.

No production repair, repair-family selection, seed retuning, threshold/envelope change, C4 payload mutation, canonical exact-v9 change, R3 PASS, R3 Requalification 3 or R4 Planning 1 is authorized here.

<!-- NRS-MARKER:R3-ARCHITECTURE-INVENTORY-HOSTED-CI-HOLD -->

## Confirmed causal evidence

Returned REV1 evidence establishes all of the following:

- the first causal checkpoint is seed-step1 at 10 ms;
- the first causal node is `suction`;
- candidate mass bookkeeping is closed;
- production candidate suction energy rate is `-5216271.784591675 W`;
- the production bookkeeping identity closes to `6.383657455444336E-05 W`;
- mode1 and mode2 public forward saturation properties are IEEE-754 bit-identical at the evaluated drum state;
- the independently evaluated IAPWS-IF97 transport state differs from mode2 raw suction transport by only `0.00014754291623830795 J/kg`;
- the independently evaluated reference transport differs from the historical production liquid transport by `52162.71184104541 J/kg`;
- the reference-consistent counterfactual leaves only `0.014754295349121094 W`, within the frozen derived component budget `0.10223668223103162 W`;
- the counterfactual rate identity closes to `2.068356699455598E-09 W`.

The approximately 5.216 MW discontinuity is therefore a transport-ownership mismatch, not an unexplained mass imbalance or governor effect.

## Current ownership map

### Conserved inventory and inverse closure

`IntegratedPrimaryCircuitSolver` receives the selected `IFluidThermodynamicModel` and passes it to `PlantNetworkOrchestrator`. `FluidNodeIntegrator` therefore resolves post-step conserved inventories with the selected model. In mode2 that is `ReferenceConsistentTabulatedInverseDomain`.

### Steam-drum forward properties

`SteamDrumSeparationSolver`, however, currently constructs its own private default:

`new SimplifiedWaterSteamThermodynamicModel()`

That default owns both the `WaterSteamVoidFractionSolver` used by the drum and the saturation properties used by `ResolvePhaseSplit()` / `ResolveSteamSource()`.

For liquid recirculation, the production path is:

```text
historical/common saturation u,rho
        + drum pressure p
        -> FluidEnergyTransport: h = u + p/rho
        -> liquid recirculation source power
        -> suction conserved inventory
        -> selected mode2 inverse closure
```

The source transport ownership and the receiving conserved-inventory ownership are therefore different in the mode2 path.

### Important provider fact

Purely injecting `new SimplifiedWaterSteamThermodynamicModel(mode2)` into the drum is not, by itself, a causal repair. REV1 proved that the current public forward saturation provider is bit-identical between mode1 and mode2. Injection without a change in forward/transport semantics would preserve the same approximately 52.16 kJ/kg transport mismatch.

<!-- NRS-MARKER:R3-ARCHITECTURE-INVENTORY-PURE-INJECTION-NOOP -->

## Impact surface of existing saturation ownership

There are currently 10 production call sites of `GetSaturationProperties()` outside the provider implementation itself. They include:

- cold/pre-start initial-condition construction;
- thermodynamic switching localization analysis;
- condenser behavior;
- turbine expansion behavior;
- two steam-drum paths;
- water/steam void-fraction resolution.

The full-plant model passes the selected thermodynamic model into secondary-cycle solvers as well as the primary network. Therefore globally changing mode2 `IWaterSteamSaturationPropertyProvider` semantics could affect condenser/turbine/diagnostic behavior in addition to the primary steam drum.

Inside `SteamDrumSeparationSolver`, saturation ownership is also used for more than advected energy: `ResolvePhaseSplit()` and the drum void-fraction/level calculation depend on the same historical/common property model. A future repair plan must therefore explicitly decide whether the ownership change is:

1. a complete forward saturation-ownership alignment; or
2. a bounded transport-property alignment that deliberately leaves phase-split/void-fraction ownership unchanged.

That distinction must be explicit rather than emerging accidentally from implementation convenience.

## Frozen repair-family inventory - descriptive only

The deep-review planning record already requires at least three families. This inventory refines their technical meaning without selecting or ranking them.

### Family A - selected model/provider ownership injection

Potential shape: `IntegratedPrimaryCircuitSolver` passes selected thermodynamic ownership into `SteamDrumSeparationSolver` rather than allowing the drum to instantiate an independent default model.

Important viability condition: **pure injection is a no-op for the confirmed seam under current provider semantics**. A viable Family-A design must additionally define closure-aware forward/transport properties for mode2.

If that is implemented by changing the existing mode2 `IWaterSteamSaturationPropertyProvider`, the change has a broad consumer surface and must assess condenser, turbine, void-fraction, analyzer and initial-condition effects.

### Family B - explicit closure-aware transport-property contract

Potential shape: introduce a dedicated transport-property capability whose semantics are explicitly "properties carried by an advected water/steam stream under the selected closure". The steam drum consumes this capability only where source/sink energy is formed; the existing general saturation provider need not change globally.

This family separates three responsibilities that are currently conflated:

- conserved-inventory inverse resolution;
- saturation/phase topology and void-fraction support;
- advected stream transport properties.

A later planning gate must determine whether keeping phase-split/void-fraction on historical properties while transport is reference-consistent is physically and architecturally acceptable. This document does not decide that question.

### Family C - bounded primary-circuit mode2 transport bridge

Potential shape: localize reference-consistent transport ownership at the integrated-primary / steam-drum seam, backed by the existing embedded reference payload and without globally changing the public saturation-provider contract.

This can constrain runtime impact to the causal seam, but it can also couple the primary solver to a concrete closure or duplicate ownership knowledge. A later planning gate must test whether the bridge remains singular, explicit and maintainable rather than becoming a second hidden thermodynamic model.

## Explicitly non-candidate mechanisms

The following are not repair families for the future ownership planning gate unless a new authority record says otherwise:

- raw-seed retuning to match the historical forward transport;
- phase-label clamping or tolerance/envelope relaxation;
- changing the C4 payload merely to absorb the source-term mismatch;
- using receiver `suction` energy as an unconditional source-stream property without a source-state physical justification;
- introducing direct production-runtime IAPWS-IF97 dependency;
- weakening the existing fast-gate or R3 envelopes.

These either violate existing authority or hide the confirmed ownership mismatch instead of assigning it explicitly.

## Entry evidence required once hosted CI is GREEN

When and only when hosted `ordinary-ci` is explicitly GREEN, Repair Planning 1 should start from this inventory and the returned REV1 evidence. Before selecting a family it should produce:

1. a source-of-truth ownership diagram for inverse state, saturation topology, phase split, void fraction and stream transport;
2. an exact consumer matrix for any interface whose semantics would change;
3. a modes 0/1 preservation strategy with bitwise or frozen-evidence checks where appropriate;
4. a mode2 frozen-point calculation showing how the proposed owner produces the reference-consistent liquid transport without IF97 runtime dependency;
5. a conservation proof for drum inventory, steam outlet and suction source terms;
6. a performance/allocation assessment demonstrating no resolve-time resource I/O or payload decoding and no unjustified hot-path allocation;
7. a revalidation matrix covering steam-drum unit tests, integrated primary tests, exact-v9/current-evidence audits, complete ordinary suite, bounded seed fast gate and subsequent R3 requalification;
8. a stop rule if the selected design changes secondary-cycle saturation behavior outside the justified causal seam.

No option score, preferred family or implementation owner is assigned by this inventory.

## Current authority

Current next authority remains:

`CONFIRM-HOSTED-ORDINARY-CI-GREEN`

After that confirmation, and only through a separate authority gate, `R3 Energy-Transport Ownership Repair Planning 1` may begin.

<!-- NRS-MARKER:R3-ARCHITECTURE-INVENTORY-NO-SELECTION -->
