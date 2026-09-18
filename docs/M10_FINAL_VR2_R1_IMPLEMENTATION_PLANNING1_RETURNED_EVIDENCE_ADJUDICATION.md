# M10 Final — VR2 — R1 Implementation Planning 1 — Returned-Evidence Adjudication

Adjudicate only the four returned planning artifacts. Do not implement C4 while adjudicating the planning return.

A returned planning PASS requires:

```text
status=PASS-AS-AUTHORED
gate=R1-IMPLEMENTATION-PLANNING1
selection-result=SELECT-C4
selected-candidate=C4-ALLOCATION-NEUTRAL-VAPOR-SEAM-COMPLETE-SURROGATE
new-closure-mode=ReferenceConsistentTabulatedInverseDomain
new-closure-mode-value=2
existing-modes-immutable=True
exact-v9-unchanged=True
direct-if97-production-runtime=False
reference-data-generation=OFFLINE-CSharp-ONLY
reference-payload-schema=NRSVR2C4-v1
reference-payload-logical-name=NuclearReactorSimulator.Simulation.Physics.Fluids.ReferenceData.NRSVR2C4.v1.bin
reference-payload-hash-anchor=COMPILED-CSharp-CONSTANT-IN-PRODUCTION-SOURCE
payload-load-point=MODE2-RESOLVER-CONSTRUCTION
payload-process-cache=STATIC-PROCESS-WIDE-IMMUTABLE
payload-manifest-runtime-authority=False
payload-provenance-output=03-reference-data-provenance.txt
future-existing-production-modifications=3
future-new-production-files=2
future-new-test-files=2
semantic-state-comparisons=1679
semantic-hydraulic-comparisons=288
mode2-dedicated-dispatch=True
simulation-project-resource-include-planned=True
future-gate=R1-SELECTED-C4-OPT-IN-CLOSURE-IMPLEMENTATION1
r1-implementation-authorized=False
production-default-switch-authorized=False
```

If the returned artifacts and frozen selection hashes are intact, adjudication may authorize only the separately versioned R1 implementation candidate. It may not authorize activation, R2 execution, a new exact identity, VR3, P3-R1 or second replacement-long.
