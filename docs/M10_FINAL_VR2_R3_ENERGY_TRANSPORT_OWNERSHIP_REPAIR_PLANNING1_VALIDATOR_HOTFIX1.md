# M10 Final VR2 R3 Energy-Transport Ownership Repair Planning 1 - Validator Hotfix 1

NRS-MARKER:M10-FINAL-VR2-R3-ENERGY-TRANSPORT-OWNERSHIP-REPAIR-PLANNING1-VALIDATOR-HOTFIX1

## Scope

Validator-only correction. No production repair, test change, retuning, threshold change, C4 payload change, Exact-V9 reinterpretation or R3 PASS.

## Root cause

The planning contract freezes 961 source files and tree SHA-256 `0ECBD950C034FC18E650A934DC449C4B41D0574434A96BE3EC1720DEDF177EE3`. The same candidate source tree reproduces that hash when repository-relative paths are ordered with ordinal case-sensitive comparison. Windows PowerShell `Sort-Object` uses host/culture case-insensitive ordering by default, which changes the aggregate path/hash sequence while every file remains byte-identical.

## Repair

`Get-FrozenTree` now collects normalized repository-relative paths and sorts them with `System.StringComparer.Ordinal` before constructing the aggregate hash. The contract and expected tree SHA remain unchanged.

## Authority

A PASS after this hotfix has exactly the same authority as the authored Planning 1 gate: it may authorize only `R3-ENERGY-TRANSPORT-OWNERSHIP-REPAIR-IMPLEMENTATION1`. Production remains unchanged and R3 remains RED until its later requalification gate passes.
