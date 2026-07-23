@echo off
dotnet test --project "%~dp0..\tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj" --no-build -- --explicit only --filter-trait "Category=GameplayLong" --parallel none
exit /b %ERRORLEVEL%
