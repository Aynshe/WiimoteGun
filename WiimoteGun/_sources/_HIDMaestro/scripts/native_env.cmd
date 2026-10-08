@echo off
:: Shared native compiler setup. Called within the build script's setlocal.
set "HM_ARCH=%~1"
if not defined HM_ARCH set "HM_ARCH=x64"
if /i "%HM_ARCH%"=="x64" (set "HM_ARCH=x64") else if /i "%HM_ARCH%"=="arm64" (set "HM_ARCH=arm64") else (
    echo ERROR: architecture must be x64 or arm64.
    exit /b 1
)

set "WDK=C:\Program Files (x86)\Windows Kits\10"
if not defined HM_WDK_VERSION set "HM_WDK_VERSION=10.0.26100.0"
set "WDK_VER=%HM_WDK_VERSION%"
set "UMDF_VER=2.15"
set "OUT_DIR=%~dp0..\build"
set "HM_ARCH_DEFINE=_AMD64_"
if "%HM_ARCH%"=="arm64" (
    set "OUT_DIR=%~dp0..\build\arm64"
    set "HM_ARCH_DEFINE=_ARM64_"
)

set "VCVARS="
for /d %%A in ("C:\Program Files\Microsoft Visual Studio\*") do (
    for /d %%B in ("%%A\*") do (
        if exist "%%B\VC\Auxiliary\Build\vcvarsall.bat" set "VCVARS=%%B\VC\Auxiliary\Build\vcvarsall.bat"
    )
)
if not defined VCVARS (
    echo ERROR: Visual Studio C++ build tools are required.
    exit /b 1
)

set "HM_HOST=x64"
if /i "%PROCESSOR_ARCHITECTURE%"=="ARM64" set "HM_HOST=arm64"
set "HM_VC_ARCH=%HM_HOST%"
if not "%HM_HOST%"=="%HM_ARCH%" set "HM_VC_ARCH=%HM_HOST%_%HM_ARCH%"
:: An explicit compiler/CRT pair supports an unpacked Microsoft toolchain.
if "%HM_ARCH%"=="arm64" if defined HM_ARM64_COMPILER_DIR set "HM_VC_ARCH=%HM_HOST%"
set "PATH=%ProgramFiles(x86)%\Microsoft Visual Studio\Installer;%PATH%"
call "%VCVARS%" %HM_VC_ARCH% >nul
if errorlevel 1 exit /b 1

if "%HM_ARCH%"=="arm64" if defined HM_ARM64_COMPILER_DIR (
    if not exist "%HM_ARM64_COMPILER_DIR%\cl.exe" (
        echo ERROR: HM_ARM64_COMPILER_DIR must contain the ARM64 cross-compiler.
        exit /b 1
    )
    if not exist "%HM_ARM64_CRT_DIR%\libcmt.lib" (
        echo ERROR: HM_ARM64_CRT_DIR must contain the ARM64 C runtime libraries.
        exit /b 1
    )
    if not exist "%HM_ARM64_CRT_DIR%\oldnames.lib" (
        echo ERROR: HM_ARM64_CRT_DIR must include the complete ARM64 CRT library set.
        exit /b 1
    )
    set "PATH=%HM_ARM64_COMPILER_DIR%;%PATH%"
    set "LIB=%HM_ARM64_CRT_DIR%;%WDK%\Lib\%WDK_VER%\ucrt\arm64;%WDK%\Lib\%WDK_VER%\um\arm64"
)

set "RC=%WDK%\bin\%WDK_VER%\%HM_HOST%\rc.exe"
if not exist "%OUT_DIR%" mkdir "%OUT_DIR%"
exit /b 0
