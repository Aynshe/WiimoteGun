@echo off
setlocal
:: Build both native OS payloads before the universal managed SDK.
for %%A in (x64 arm64) do (
    call "%~dp0build.cmd" %%A
    if errorlevel 1 exit /b 1
    call "%~dp0build_companion.cmd" %%A
    if errorlevel 1 exit /b 1
)
call "%~dp0build_openvr.cmd"
if errorlevel 1 exit /b 1

:: Two passes, and both are required. The EmbeddedResource globs are
:: evaluated before PackResources runs, so the first pass only stages
:: Resources\ and the second is what embeds the staged bytes. A single
:: pass on a clean tree produces a small assembly with no driver in it.
dotnet build "%~dp0..\sdk\HIDMaestro.Core\HIDMaestro.Core.csproj" -nologo -v minimal
if errorlevel 1 exit /b 1
dotnet build "%~dp0..\sdk\HIDMaestro.Core\HIDMaestro.Core.csproj" -nologo -v minimal
if errorlevel 1 exit /b 1
echo BUILD SUCCEEDED: x64 and ARM64 native payloads, universal HIDMaestro.Core.dll
exit /b 0
