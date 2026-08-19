# M10.9.4.1-I.3 Hotfix 4 Classifier Fix 1 — Targeted-Train Reverse-Flow Classification

I.3 remains unvalidated and I.2 remains the authoritative baseline. This fix changes no plant physics, numerical mathematics, H.30 policy, selector, persistence identity or 10 ms fixed step.

The completed Hotfix 4 100 s / 10 ms comparison produced stronger evidence than the original classifier expected:

- exact v2 generation-drop steps: **338**;
- reverse stop-valve steps: **8**;
- reverse control-valve steps: **0**;
- reverse admission-valve steps: **330**;
- targeted-train reverse-flow steps (`stop < 0 || control < 0 || admission < 0`): **338**;
- explicit drops with targeted reverse flow: **338/338**;
- targeted reverse-flow steps that are generation drops: **338/338**;
- exact v3 generation drops: **0**;
- exact v3 targeted-train reverse-flow steps: **0**;
- exact v3 corrected commits: **1791**, with **0** rollback/fallback/unsafe/untargeted disagreement.

The eight non-admission drop steps form one upstream stop-valve reversal episode at 3.53–3.60 s. The remaining 330 drop steps are admission-valve reversals. The original Hotfix 4 predicate incorrectly required every explicit drop to have negative admission flow, so it rejected a dataset that actually isolates the broader H.18 four-node targeted-train discontinuity perfectly.

Classifier Fix 1 therefore evaluates reverse flow over the full stop/control/admission train. It does not reinterpret a numerical failure as success: it corrects the diagnostic predicate to match the topology that H.18–H.22 were designed to stabilize.

A green result permits an explicit H.30 policy re-review; it does not itself change the production default and does not authorize freezing I.3 tolerance budgets.
