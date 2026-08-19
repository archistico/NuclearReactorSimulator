# M10.9.4.1-I.3 Hotfix 5 Static Review

- Base candidate: validated Hotfix 4 diagnostic lineage; I.2 remains the last authoritative validated baseline.
- Under `src/`, only `NuclearReactorSimulator.Application/ApplicationDescriptor.cs` differs from the Hotfix 4 Classifier Fix 1 candidate.
- Frozen Hotfix 4 artifact fingerprints verified: summary `AA40086B...696A72`, 10 ms trace `8FEA343B...3C57F`, drop comparison `69944487...2796`, episodes `8B15C549...EAA`.
- New long test is fail-closed behind `Fact(Explicit=true)` plus `NRS_I3_CORRECTED_300S_AUDIT=1`.
- Focused launcher uses .NET 10 Microsoft.Testing.Platform `--project`, `--explicit only`, `--filter-method` and `--parallel none`.
- No `Assert.Single(...Where(...))` analyzer pattern was introduced.
- No `bin`, `obj` or runtime `artifacts` directory is included in the candidate tree.
- No H.30 policy, production selector, physics, H.9/H.20/H.22 mathematics, persistence identity or fixed-step behavior is changed.
