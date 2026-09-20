# M10 Final VR2 R3 Energy-Transport Ownership Repair Implementation 1 - Fresh-Build Hotfix 4

Status: runner/build-hygiene hotfix only.

The previous repeated `Expected: 0 / Actual: 24` result continued to report the historical source line despite the validator proving that the newer focused-test source was present. The qualification runner used an incremental `dotnet build` followed by `dotnet test --no-build`. When a candidate ZIP is overlaid onto a working tree with existing Release outputs, preserved timestamps can allow stale DLL/PDB reuse.

Hotfix 4 therefore changes only the qualification runner:

1. `dotnet clean NuclearReactorSimulator.sln --configuration Release`;
2. `dotnet restore`;
3. `dotnet build --configuration Release --no-restore --no-incremental --warnaserror`;
4. focused tests continue with `--no-build`.

No production source, focused-test source, contract, payload, threshold, physics or R3 authority change is made.
