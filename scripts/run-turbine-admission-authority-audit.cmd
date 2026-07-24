@echo off
dotnet test --project "%~dp0..\tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj" --no-build -- --explicit only --filter-trait "Category=TurbineAdmissionAuthorityAudit" --parallel none
exit /b %ERRORLEVEL%
