# Initial Research References

These references informed the initial design direction. They are research inputs, not code dependencies.

## IAEA — Nuclear reactor simulators for education and training

https://www.iaea.org/topics/nuclear-power-reactors/nuclear-reactor-simulators-for-education-and-training

Key design takeaway: build an educational simulator that exposes plant behaviour, normal operation and transients without claiming plant-specific professional training fidelity.

## hartrusion/RbmkSimulator

https://github.com/hartrusion/RbmkSimulator

Key design takeaways:

- keep calculations outside the GUI;
- use an explicit simulation loop;
- simplify plant scope enough for a single operator;
- use mnemonic displays and trends;
- keep reactivity contributions conceptually separable.

The repository is GPL-3.0. Nuclear Reactor Simulator is an independent implementation; source code must not be copied unless licensing implications are deliberately accepted.

## Chernobyl: The Legacy Continues — Information and User Guide

https://web.archive.org/web/20210201092546/https://simgenics.com/downloads/Chernobyl_The_Legacy_Continues_Information_and_Users_Guide.pdf

Key design takeaways:

- full plant-cycle thinking;
- structured startup procedures;
- initial-condition files/scenarios;
- reactor, turbine and electrical systems treated as one operational experience.

## IAPWS — Industrial Formulation 1997 (IF97)

https://iapws.org/documents/release/IF97-Rev

Key M1.7 takeaway: use the official Region-4 saturation-pressure relation as a trustworthy saturation boundary reference while keeping the current simulator closure explicitly simplified. M1.7 is not a complete IF97 implementation; future higher-fidelity thermodynamic backends should remain replaceable behind `IFluidThermodynamicModel`.


## U.S. NRC — Low Power Reactor Dynamics training module

https://www.nrc.gov/docs/ml1214/ml12142a098.pdf

Key M2.3 takeaway: point-reactor dynamics treats reactivity, prompt response and delayed-neutron precursor groups as the core time-dependent neutronic model. Nuclear Reactor Simulator keeps the parameter set injected and plant-independent; the reference informs the model boundary rather than supplying hardcoded plant constants.


## U.S. NRC — Decay Heat (DCH) Package Users' Guide

https://www.nrc.gov/docs/ML0101/ML010190162.pdf

Key M2.5 takeaway: decay heat is a stateful consequence of radioactive fission-product inventories and operating history, not a fixed post-shutdown percentage. The simulator uses a deliberately simplified equivalent-group model rather than implementing MELCOR/ANS decay-heat correlations.

## OSTI — Scalable modular dynamic molten salt reactor system model

https://www.osti.gov/servlets/purl/1850560

Key M2.5 takeaway: reduced-order dynamic reactor models can approximate decay-heat behavior with equivalent decay-heat-producing groups analogous in structure to precursor-group dynamics. Nuclear Reactor Simulator keeps all such group coefficients configurable and plant-independent.


## M2.7 void feedback references

- IAPWS thermodynamic property formulations and vapor-liquid equilibrium background: https://iapws.org/documents/newform
- U.S. NRC glossary — Void coefficient of reactivity: https://www.nrc.gov/reading-rm/basic-ref/glossary/void-coefficient-of-reactivity

M2.7 uses these only as conceptual/reference boundaries. The current homogeneous-equilibrium quality-to-void conversion and linear void-reactivity coefficient are educational approximations, not licensing or engineering-analysis models.

## Pre-M11 engineering review books

These three books were reviewed during final M10 / pre-M11 planning. They are engineering guidance inputs, not runtime/code dependencies and not licensing evidence.

### Wang, Li, Allison, Hohorst (eds.) — Nuclear Power Plant Design and Analysis Codes

Jun Wang, Xin Li, Chris Allison, Judy Hohorst (eds.), *Nuclear Power Plant Design and Analysis Codes: Development, Validation, and Application*, Woodhead Publishing / Elsevier, 2021.

Project use: V&V taxonomy, phenomenon-scoped qualification, separate/integral evidence, multiphysics coupling qualification, non-regression and explicit limitations. See `../PRE_M11_NUCLEAR_CODE_VV_REVIEW.md`.

### National Research Council — Digital Instrumentation and Control Systems

Committee on Application of Digital Instrumentation and Control Systems to Nuclear Power Plant Operations and Safety, *Digital Instrumentation and Control Systems in Nuclear Power Plants: Safety and Reliability Issues*, National Academy Press, 1997.

Project use: protection/control separation, human–automation allocation, common-mode/diversity reasoning, software assurance, HMI failure modes, deterministic timing semantics and proportional dependency/COTS assurance. Historical regulatory discussion is not treated as current regulatory authority. See `../PRE_M11_DIGITAL_IC_HUMAN_SYSTEM_SAFETY_REVIEW.md`.

### Lamarsh & Baratta — Introduction to Nuclear Engineering

John R. Lamarsh and Anthony J. Baratta, *Introduction to Nuclear Engineering*, 3rd ed., Prentice Hall, 2001.

Project use: coupled operating-point self-consistency as the conceptual basis for the planned residual/equilibrium qualification work. Other candidate topics remain research backlog unless separately promoted. See `../REFERENCE_OPERATING_POINT_EQUILIBRIUM_AND_STABILITY_PLAN.md` and `LAMARSH_FOLLOW_UP_CANDIDATES.md`.
