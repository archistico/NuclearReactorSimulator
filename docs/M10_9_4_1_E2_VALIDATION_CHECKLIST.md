# M10.9.4.1-E.2 Validation Checklist

## Status

**PLANNED — DO NOT MARK VALIDATED ON THE M10.9.4.1-D.4 SOURCE**

The active source is still pre-E. The current 2/2 reference-scale audit confirms the 1,000 MW / 150 rpm / correction-only contract and explicitly states that bidirectional migration is deferred.

## Implementation gate

Before E.2 can be considered implemented, verify that production source contains all of the following:

- current-v2 generator nameplate = 10 MWe;
- historical/default generator definitions unchanged;
- current-v2 governor normalization explicitly selected and tested;
- versioned `GenerationOnly` and `Bidirectional` coupling modes;
- internal signed generator/grid rotor-torque seam;
- signed electrical exchange with positive conversion losses in both directions;
- bounded motoring and generation power;
- signed current-v2 HMI ranges;
- replay/checkpoint compatibility.

## Required focused tests

- current-v2 10 MWe ownership and 5 MWe = 50%;
- requests above 10 MWe rejected or clamped by the intended owner;
- legacy coupling remains generation-only;
- slower-than-grid connected rotor receives motoring torque;
- faster-than-grid connected rotor receives generating load torque;
- positive conversion loss in both directions;
- public/manual rotor-load input still rejects arbitrary negative torque;
- signed torque is accepted only through the internal generator/grid seam;
- HMI range derives from the active current-v2 definition.

## Promotion gate

Run and pass:

```text
dotnet test --no-build
scriptsun-reference-plant-scale-audit.cmd
scriptsun-turbine-admission-authority-audit.cmd
scriptsun-turbine-governor-actuator-tracking-audit.cmd
scriptsun-gameplay-long-tests.cmd
scriptsun-operational-envelope-audit.cmd
```

Then manually review generator signed-power presentation, motoring semantics and protection supervision. E.3 must not begin before this checklist is genuinely green.
