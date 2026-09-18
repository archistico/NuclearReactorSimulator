@echo off
setlocal

echo Restoring packages...
dotnet restore || exit /b 1

echo Building solution...
dotnet build --no-restore || exit /b 1

echo Running tests...
dotnet test --no-build || exit /b 1

echo Verification completed successfully.
