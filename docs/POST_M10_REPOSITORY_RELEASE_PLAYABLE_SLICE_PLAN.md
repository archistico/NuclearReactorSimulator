# Post-M10 — Repository, Release Identity and Playable Vertical Slice Plan

**Status: POST-M10 PLANNING ONLY. Not a prerequisite for M10 closure except where P6 explicitly requires release-state consistency.**

## Goal

Convert the strong engineering core into a maintainable repository and a clearly playable educational product without reopening M10 physics casually.

## 1. Git and version identity

From the next validated gate onward:

- one validated engineering gate/hotfix should normally correspond to one focused commit;
- create annotated tags for major closure checkpoints;
- separate internal engineering identity (`M10/P3-R1/exact-v9`) from user-facing product version (`v0.10.x` style);
- do not fabricate retroactive commit history.

## 2. ADR/evidence/script lifecycle

Classify existing material into:

- durable architecture ADR;
- active ordinary/focused gate;
- scheduled long gate;
- frozen historical evidence;
- superseded temporary diagnostic.

Do not require every expensive long collector to become ordinary xUnit. Do require every active script to have an owner/lifecycle reason.

## 3. Public/release presentation

Before a public release candidate:

- current README quick start;
- screenshot(s) of the control room;
- short explanation of educational fidelity/limitations;
- release package/run instructions;
- GitHub description/topics/release metadata where appropriate.

## 4. Playable vertical slice

Before accepting broad new backend scope after release hardening, demonstrate a manual playable path:

`cold shutdown → pre-start checks → approach to criticality → heat-up/steam raising → turbine roll → synchronization → breaker close → stable generation`.

The slice may be visually imperfect. Its purpose is to ensure the project delivers educational operator experience, not only backend correctness.
