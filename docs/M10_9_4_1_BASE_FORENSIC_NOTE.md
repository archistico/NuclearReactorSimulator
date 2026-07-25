# M10.9.4.1 continuation-base forensic note

## Why the older PLANT renderer reappeared

The user supplied `NuclearReactorSimulator_M10.9.4.1_nuova.zip` as the continuation base and reported that a previously completed PLANT visual unification was no longer present in later candidates.

A byte-level comparison between that uploaded archive and the D.3.1 candidate found the following files unchanged:

```text
src/NuclearReactorSimulator.App/Views/MainWindow.axaml
SHA-256 a83f8b6c223154a46e9ac174dc0a06de935abd8ab44354339d3821e2f22a5efa

src/NuclearReactorSimulator.App/Controls/ControlRoomPlantMimicControl.cs
SHA-256 a7d257390b938a5027daf69b45f99100f0b97080a83a2d0f5b039af997a0b44b

src/NuclearReactorSimulator.App/Controls/ControlRoomSubsystemSchematicControl.cs
SHA-256 2d4b5b044d317ffa1809585d7933927b022fc7520e67dd0c1698dc41d8ca07
```

The uploaded continuation archive itself used `ControlRoomPlantMimicControl` for PLANT and `ControlRoomSubsystemSchematicControl` for all subsystem engineering schematics. Therefore D.1 through D.3.1 did not introduce a byte-level PLANT rollback; they inherited the older PLANT renderer from the supplied archive.

The process error was accepting the archive as authoritative without verifying the user's stated visual-unification requirement against the actual files. D.3.2 corrects the visible inconsistency and records the required manual PLANT check as a validation gate.
