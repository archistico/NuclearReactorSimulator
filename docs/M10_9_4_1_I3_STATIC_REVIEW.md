# M10.9.4.1-I.3 Static Review

## Result

**PASS as candidate static review; executable validation still required locally.**

## Isolation

Compared with I.2, production source changes are limited to `src/NuclearReactorSimulator.Application/ApplicationDescriptor.cs` metadata. No solver, plant definition, controller, protection threshold, H.20/H.22 authority seam, H.29 selector, exact-version factory or persistence contract is changed.

## Evidence design

- user-supplied green I.2 artifacts are copied into test evidence and canonical-SHA256 checked;
- the I.3 exact trajectory contract is stored under `eng/`;
- the long audit uses the existing exact-v2 `DesktopSustainedGenerationInitialConditionFactory`;
- one-second samples observe existing immutable/canonical snapshots only;
- conservation limits reuse already-established model audit limits;
- tolerance budgets are generated only after independent health/conservation gates pass;
- the trajectory contains exactly 301 rows including t=0; t=1..300 are the generation-health operating samples;
- the final-window contract requires exactly 7 finite slopes and 19 finite positive budget entries;
- generated budgets are explicitly internal regression evidence, not historical plant measurements.

## CI placement

`eng\ci-long.cmd` now includes the I.3 300-second baseline gate after the existing gameplay, operational-envelope and reference-scale gates. Ordinary/current-evidence CI remains unchanged, so a 300-second journey is not added to every push/PR.

## Legacy retirement

I.3 does not change the I.2 retirement result: H.5/H.21 source dependencies still exist and `legacy-mode-retirement-authorized=False` remains required.

## Hotfix 1 diagnostic amendment

The initial executable gate revealed a 55 s shaft-power floor violation. Hotfix 1 changes only test/audit behavior plus application metadata: it defers the unchanged health assertion until after artifact generation and adds canonical turbine/steam/admission/phase observations. No solver, production factory, selector, protection threshold, persistence schema or 10 ms timestep is changed.
