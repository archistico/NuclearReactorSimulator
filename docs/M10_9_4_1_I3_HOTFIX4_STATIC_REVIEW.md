# M10.9.4.1-I.3 Hotfix 4 — Static Review

- Base: I.3 Hotfix 3 candidate; I.2 remains last validated baseline.
- Production runtime delta vs I.2: `ApplicationDescriptor.cs` only.
- No changes to plant physics, H.9/H.20/H.22 mathematics, H.30 selector/policy, persistence identities or 10 ms fixed step.
- Frozen red I.3 evidence fingerprints verified for summary, health violations and shaft-drop episodes.
- New comparison test is double opt-in (`Explicit=true` plus `NRS_I3_BRANCH_COMPARISON_AUDIT=1`) and cannot execute its 20,000-step comparison in ordinary CI without the environment opt-in.
- xUnit2031 scan: no `Assert.Single(...Where(...))` pattern introduced.
- The diagnostic writes reports before its final classification assertion.
- No legacy H.5/H.21 source deletion is authorized.
