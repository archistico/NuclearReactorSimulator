# M10.9.4.1-E.1 Validation Checklist

E.1 is a contract/decision checkpoint. It intentionally changes no production physics.

## Required checks

- [ ] `docs/REFERENCE_PLANT_SCALE_CONTRACT.md` marks the 10 MWe reduced-scale direction as accepted for the current-v2 educational reference plant.
- [ ] `docs/REFERENCE_PLANT_SCALE_MIGRATION_PLAN.md` records the exact scale basis and E.2 migration gates.
- [ ] ADR 0102 is present and consistent with the migration plan.
- [ ] D.3 is recorded as validated: max actuator lag 23.418 pp, integral excursion 0.134 pp, no D.3.1 anti-windup change.
- [ ] E.1 changes no `src/` or test physics.
- [ ] The active source remains pre-migration until E.2; documentation must not claim that 10 MWe is already live in runtime definitions.
- [ ] Historical/v1 compatibility remains explicitly protected.

## Next gate

Proceed to E.2 only with a coordinated implementation covering:

1. 10 MWe current-v2 nameplate;
2. retained/verified 1,000 kg·m² rotor inertia;
3. explicitly selected governor normalization;
4. signed bidirectional generator/grid coupling;
5. positive conversion losses in both power directions;
6. HMI/range migration;
7. ordinary suite + explicit synchronization + healthy 300-second sustained journey.
