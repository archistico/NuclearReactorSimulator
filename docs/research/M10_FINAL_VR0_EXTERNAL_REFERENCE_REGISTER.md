# M10 Final VR0 — External Reference Register

**Purpose:** bibliographic/provenance register for the frozen VR0 physical-reference contracts. This document records sources and transfer boundaries; it does not itself confer physical validation.

## VR1 — Point kinetics

### Primary source

Alain Hébert, *Applied Reactor Physics*, Third Edition, Presses internationales Polytechnique, 2020, ISBN 978-2-553-01735-3.

Frozen sections:

- §5.4.1, point-kinetics equations;
- Eq. (5.240), point-kinetics ODE system;
- Eq. (5.251), equilibrium precursor initialization;
- Eq. (5.252), analytical matrix-exponential solution for constant parameters;
- Eq. (5.256), inhour equation;
- Exercise 5.10, one-group analytical step-reactivity case;
- Exercise 5.11 and Table 5.4, six-group precursor data and step-reactivity exercise.

Project copy: `03 Applied Reactor Physics (Alain Hébert).pdf` in the user's source library; the book is **not** duplicated into the source candidate ZIP.

### Secondary terminology source

John R. Lamarsh and Anthony J. Baratta, *Introduction to Nuclear Engineering*, Third Edition, Prentice Hall, 2001, Chapter 7 §7.2. Used only for independent terminology/interpretation of dollars, prompt-criticality and delayed-neutron behavior; Hébert remains the numerical VR1 contract source.

### Transfer boundary

The six-group benchmark assesses the generic point-kinetics solver equation implementation. It does not establish that exact-v9's one-group educational plant parameters are RBMK-calibrated.

## VR2 — Water/steam thermodynamic reference

### Primary authority

International Association for the Properties of Water and Steam (IAPWS), **R7-97(2012)**, *Revised Release on the IAPWS Industrial Formulation 1997 for the Thermodynamic Properties of Water and Steam*.

Official release page:

`https://www.iapws.org/relguide/IF97-Rev.html`

Official release PDF:

`https://www.iapws.org/relguide/IF97-Rev.pdf`

IAPWS identifies IF97 as the industrial formulation for ordinary water/steam, including vapor-liquid equilibrium, with region-specific fundamental equations and verification tables. The reference helper must qualify itself against those official verification values before comparing production output.

### Transfer boundary

The production model already uses IF97 Region 4 for the saturation boundary but explicitly simplifies densities and internal energies. VR2 therefore reports a quantitative error map and does not relabel the implementation as full IF97.

No third-party IF97 library is the authority and no new runtime package is added. A test-only C# helper implements the minimum official formulas required by the frozen matrix.

## VR3 — I-135 / Xe-135

### Primary source

John R. Lamarsh and Anthony J. Baratta, *Introduction to Nuclear Engineering*, Third Edition, Prentice Hall, 2001, Chapter 7 §7.5.

Frozen material:

- Eqs. (7.90) and (7.91), iodine/xenon rate equations;
- Tables 7.5–7.6, U-235 thermal-fission yields and decay constants;
- Eqs. (7.92)–(7.93), equilibrium inventories;
- Eqs. (7.101)–(7.103), post-shutdown iodine/xenon solution;
- Figure 7.14 and accompanying text, qualitative peak at about ten hours after shutdown and flux dependence.

Project copy: `Introduction to Nuclear Engineering (3rd Edition) ... .pdf` in the user's File Library; not duplicated into this ZIP.

### Frozen numeric source values

```text
I-135 effective yield for U-235 thermal fission = 0.0639 atoms/fission
Xe-135 direct yield for U-235 thermal fission = 0.00237 atoms/fission
lambda_I = 2.87e-5 1/s
lambda_Xe = 2.09e-5 1/s
Xe-135 2200 m/s absorption cross section = 2.65e6 barn
```

### Transfer boundary

VR3 assesses the generic reduced I/Xe equations when configured from the external reference. The built-in M9.3 `core-poison-m93-v1` constants are intentionally configuration-relative and remain uncalibrated. Exact-v9 does not activate that poison definition in the sustained-generation factory path.

## VR4 — Decay heat

### Current standard/scope authority

American Nuclear Society, **ANSI/ANS-5.1-2014 (R2023), _Decay Heat Power in Light Water Reactors_**.

Current ANS Reactor Physics standards scope page:

`https://www.ans.org/standards/involved/cc/sracc/`

Official 2014 preview:

`https://wx1.ans.org/store/peek/item-240302/`

ANS states that the standard covers decay heat from fission products and U-239/Np-239 following shutdown of LWRs with U-235, U-238 and plutonium, including operating-history/capture treatment and uncertainty. The 2014 foreword notes that standard fission-product tabular data were not changed from the 1994/2005 standards, while methods/guidance and actinide treatment were improved.

### Supporting project-held source

Neil E. Todreas, Mujid S. Kazimi et al., *Nuclear Systems, Volume I — Thermal Hydraulic Fundamentals*, 2nd ed., Chapter 3 §3.9. It discusses post-shutdown decay-energy sources, operating-time dependence, and a figure based on the 2005 ANS decay-power standard.

### Applicability boundary

The repository currently contains a generic configurable `DecayHeatSolver`, but source audit finds no canonical production `DecayHeatDefinition` wiring for exact-v9 and the exact-v9 sustained-generation path carries `TotalDecayHeatPower=0` into the integrated primary circuit.

Therefore VR4 v1 is a **reference-readiness/configuration-gap assessment**. It must not fabricate an ANS-fitted production group set simply to make a numerical comparison possible. A future M12.5 model-definition milestone can select/derive a canonical group representation and then perform a true curve assessment.

## Reference quality hierarchy used by VR0

1. issuing-body standard/release;
2. reproducible graduate/reference textbook equations and tables;
3. issuing-body verification/example material;
4. independent test-only numerical realization of items 1–3.

Production code, blogs, forum posts and a second copy of the same production algorithm are not independent numerical authorities.
