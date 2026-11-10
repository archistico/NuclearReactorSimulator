# R3 H Bounded Scaled Local-Model Design 1

NRS-MARKER:R3-H-LOCAL-MODEL-DESIGN1

This implements the offline comparison frozen by
[Numerical Stop Review 1](M10_FINAL_VR2_R3_H_NUMERICAL_STOP_REVIEW1.md).
Execution status lives in [PROJECT.md](PROJECT.md). It changes no physical source,
test, seed, controller, acceptance envelope, residual weight or coordinate bound.

The complete authenticated event-401 residual/Jacobian is reused. Each column is
scaled by its Euclidean norm; a zero column has zero delta. Six authored dimensionless
damping values (0.001, 0.01, 0.1, 1, 10, 100) produce six reported linear comparisons,
without selecting a physical candidate. The recovered physical-coordinate direction
uses one global fraction, at most 99% of the distance to the existing box along that
direction. Individual coordinate clipping and wider bounds are forbidden.

The comparison records uncapped and capped coordinates, each predicted residual,
maximum objective, whether the scaled damping changes the normal-matrix diagonals,
and strict descent of the same maximum scaled local-model residual. All scores are
linear predictions. A smaller predicted score is not a new plant observation.
The three derivative-probe phase changes remain attached to the report; physical
smoothness and nonlinear improvement are not established by coordinate scaling.

Six numerical fixtures cover a known coupled root, a zero column, an ill-scaled
known root, a near-bound outward direction with one global fraction, an outward
direction at the exact bound, and a zero direction. They test the numerical transform
and bound arithmetic without using the plant simulator.

```powershell
.\scripts\run-r3-h-local-model-design1.cmd
```

The standard-library-only runner authenticates inputs, verifies the unchanged
962-file production/406-file test trees, independently repeats the prerequisite
offline review into its own artifact subfolder, and writes provenance, fixtures,
column scales, six comparisons, summary and SHA-256 manifest to
`artifacts/r3-h-local-model1`. It performs zero physical evaluations and no new
ordinary-suite run. The inherited ordinary/current-evidence PASS remains valid.

The next activity is review of this local-model evidence before any separately
frozen nonlinear plan. No result selects a corrected seed, proves H sufficient or
authorizes a nonlinear search, extra physical probes, C/J, bound expansion,
Requalification 3 or R4. R3 remains RED.
