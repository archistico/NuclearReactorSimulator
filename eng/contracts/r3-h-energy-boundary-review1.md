# Local C4 hotwell energy-boundary review

This protocol performs no plant calls and creates no seed. Its inputs are the
frozen, repeated Trials 6–8. It authenticates their execution identities and
reconstructs every hotwell observation, including both repetitions and all four
checkpoints. This is an explanation of the observed neighbourhood, not a global
replacement for the C4 resolver.

For the existing hotwell volume of 10 m³, recover the liquid-volume crossing of
the authenticated C4 prefix at `v = V/M`. Interpolate the boundary liquid energy
`u_b` with the same linear prefix interpolation. The local signed margin is
`u_b - U/M + max(1e-5, abs(U/M)*1e-10)` in J/kg. The tolerance is the resolver's
existing energy-root tolerance. No residual criterion or thermodynamic tolerance
is changed. Conversion of this margin to MJ/kg changes only its unit.

Restrict reconstruction to observed temperatures 300–340 K and the nearest
liquid boundary within 1 K. Other regions, multiple distant crossings, vapour
boundaries and the global inverse algorithm are outside scope. Synthetic
fixtures cover the real prefix boundary, either energy side, and unit conversion.

The returned evidence must reproduce every observed mixture/liquid label in
these samples and preserve the counterexample where positive volume reachability
alone accompanies a liquid result. This is an empirical agreement check against
frozen C4 outputs; it is not a proof of algorithmic equivalence throughout the
declared temperature interval.

Trial 8 also has to reproduce the small phase-compatible improvement whose
residual-model error exceeds its actual reduction, followed by reversal at the
smallest tested fraction. Consequently an energy-margin optimizer guard remains
unqualified. Review residual and energy-margin derivatives over several stencil
widths before proposing a new search. The root criterion remains maximum absolute
scaled residual ≤1, followed by independent confirmation, envelope checks and R3.

Commands: `python eng/review-r3-h-energy-boundary1.py execute`, then freeze
`01-analysis.json` using the shared authenticated evidence mechanism, and
`python eng/review-r3-h-energy-boundary1.py review` for independent replay.
