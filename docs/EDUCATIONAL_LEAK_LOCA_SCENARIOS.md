# Educational Leak / LOCA-Class Scenarios

M8.5 extends the validated deterministic fault framework with bounded pressure-driven break boundaries suitable for training-oriented inventory-loss and depressurization demonstrations.

## Pressure-driven break model

A break is attached to one exact canonical `FluidNodeState`. At each fixed simulation step, the committed node pressure is compared with an explicit ambient pressure. Positive pressure difference drives a square-root-scaled mass flow up to a declared reference flow. The effective flow is additionally limited by a declared maximum fraction of the node inventory removable in one fixed step.

The declared inventory fraction is an **upper bound, not a guaranteed removal**. Before applying a pressure-driven break, M8.5 deterministically probes the committed source-node inventory after the proposed carried-energy loss and further limits only the break removal when necessary to keep that inventory inside the already-validated simplified water/steam thermodynamic envelope. This admissibility guard never relaxes the thermodynamic model, changes the committed state, or adds inventory; it can only reduce the requested external loss source term.

The resulting effect is emitted only as:

- negative external mass flow from the source node;
- negative external energy flow equal to the removed mass flow times the source node's committed specific internal energy.

`PlantNetworkOrchestrator` remains the single integration boundary. Pressure, temperature, phase, void, level and downstream flows remain consequences of the existing model.

## Built-in scenarios

### Small Primary-Coolant Leak

A modest pressure-driven leak from the canonical `pressure` node to atmospheric reference pressure. Intended to demonstrate slower inventory loss, evolving pressure and the timing of automatic/operator response.

### Large Break-Class Loss of Coolant

A higher reference-flow version of the same bounded model. It is a **break-class training challenge**, not a detailed pipe-rupture calculation. The explicit per-step inventory bound prevents the lumped node from being numerically emptied in one step.

### Steam-Space Leak / Depressurization

A pressure-driven break from the canonical `steam` node. It demonstrates steam inventory and carried-energy loss with resulting depressurization inside the simplified water/steam envelope.

## What M8.5 does not model

M8.5 does not model:

- critical/choked two-phase break flow;
- break geometry or structural rupture mechanics;
- flashing jet momentum or containment pressure;
- ECCS injection/accumulator systems not already present in the plant model;
- fuel uncover/damage, oxidation, hydrogen or severe-accident progression;
- licensing-grade LOCA acceptance criteria.

Scenario outputs must therefore be interpreted as deterministic educational system-response behavior only.

## Determinism and replay

Break activation/clearance uses the validated M8.1 lifecycle. Break flow depends only on committed plant state, fixed timestep and immutable scenario parameters. No wall clock, randomness or hidden trajectory scripting is used.
