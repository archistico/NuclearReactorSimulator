# M10 Final VR2 — R3 Reference-Consistent Seed Integration Fast-Gate Dynamic Equilibrium Diagnostic 1

## Purpose
Localize the returned Implementation 1 fast-gate RED without changing production code or acceptance thresholds.

## Frozen facts
- raw mode-2 conserved-inventory seed resolves 12/12 nodes with 12/12 phase matches;
- no non-finite state, trip, breaker opening or rollback occurred in the first 100 Running steps;
- primary flow first exits the unchanged envelope at step 15;
- governor output first exits at step 17;
- 86/100 steps are outside the key envelope;
- electrical export and drum level remain inside their frozen ranges through step 100.

## Diagnostic design
Run canonical exact-v9 mode 1 and the reference-consistent mode-2 candidate side by side for 100 steps at 10 ms. Record post-seed node coordinates, primary pump/channel/return flows, eight hydraulic heads, speed-controller setpoint/measurement/error/integral/output, turbine commanded/transferred flow, moisture drain and shaft power.

The diagnostic is evidence-only. It introduces no new threshold and must not mutate the frozen seed vector, C4, canonical exact-v9 or production source.
