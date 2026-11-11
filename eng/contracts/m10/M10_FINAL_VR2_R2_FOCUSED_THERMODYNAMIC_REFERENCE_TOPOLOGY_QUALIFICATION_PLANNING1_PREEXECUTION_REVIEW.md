# M10 Final — VR2 — R2 Focused Thermodynamic / Reference / Topology Qualification — Planning 1 — Preexecution Review

## Status

**STATIC PLANNING REVIEW CONTRACT**

The planning audit must establish all of the following before the future R2 executable gate can be authored:

1. returned R1 implementation adjudication is PASS;
2. all seven returned R1 artifacts are frozen by SHA-256;
3. production mode 2 exists and remains explicit opt-in value `2`;
4. payload SHA-256 remains `EF49B1D097FC63F1F1254E425F46C6ACA837B82C58EFBA9C7774727C51C82267`;
5. default construction and exact-v9 call sites remain unchanged;
6. R2 is test/reference-only and production source changes are forbidden;
7. the independent IAPWS helper remains unchanged and production-independent;
8. official reference self-check ceiling remains `1e-8`;
9. the original VR2 blocking ceiling remains `25%`;
10. the Planning 1 compressed-liquid/hot-primary pressure target remains `10%`;
11. the frozen RP1A VR2 corpus remains exactly 40 rows, of which 39 are inverse-applicable and one is Region-4 pressure-only;
12. the frozen exact-v9 topology corpus remains exactly 360 rows and five node identities × 72 observations;
13. the frozen seam map remains exactly 1,280 rows / 320 boundaries / four required sides;
14. future R2 requires zero unresolved and zero phase mismatch on all inverse/topology rows;
15. exact-v9 state phase agreement remains 100%;
16. continuity ceilings are inherited from qualified C3/C4 evidence and may not be widened in R2;
17. deterministic repeat is required across independent mode-2 model instances;
18. ordinary Release is required before the focused R2 test;
19. R2 must not use C4 output as the physical acceptance oracle;
20. R2 does not run an exact-v9 scenario or hydraulic long materiality campaign;
21. the future R2 implementation adds exactly one test source and no `src/` file;
22. the future R2 evidence tree contains exactly nine files;
23. returned R2 evidence must be adjudicated before R3 planning;
24. default activation, exact-v9 activation, new exact identity, VR3, P3-R1 and second replacement-long remain unauthorized;
25. this Planning 1 candidate itself contains no `src/` or `tests/` modification.
