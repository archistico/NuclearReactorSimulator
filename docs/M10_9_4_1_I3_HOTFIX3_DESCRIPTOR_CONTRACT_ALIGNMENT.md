# M10.9.4.1-I.3 Hotfix 3 — Descriptor Contract Alignment

## Purpose

Hotfix 2 correctly isolated the 300-second I.3 collector behind `NRS_I3_LONG_AUDIT=1`, but the application descriptor milestone title no longer contained the base I.3 contract phrases asserted by `ApplicationDescriptorTests`. The focused script therefore failed in its descriptor preflight before the long collector could run and no artifacts were produced.

## Change

Hotfix 3 is metadata/test-contract only. It restores the base I.3 title terms in `ApplicationDescriptor.Current.Milestone`, retains an explicit Hotfix 3 suffix, and restores `final-window slopes` to the status text. `ApplicationDescriptorTests` now checks both the base I.3 identity and the Hotfix 3 descriptor-alignment identity.

No plant runtime, numerical method, physics, production selector, persistence contract, 10 ms fixed step, long-audit health threshold, environment opt-in or diagnostic collector behavior changes.

I.2 remains the latest validated baseline. I.3 remains candidate until ordinary build/test and the focused 300-second gate pass.
