@echo off
chcp 65001 >nul
setlocal enabledelayedexpansion
cd /d "%~dp0"

:: =====================================================
:: 版本要求配置
:: =====================================================
set REQUIRED_DOTNET_MAJOR=7
set TARGET_DOTNET_VERSION=7.0
set APP_NAME=ProjectFileManager

:: =====================================================
echo ============================================
echo   %APP_NAME% - 一键启动脚本
echo   .NET 7.0 + Eto.Forms 桌面应用 (Windows)
echo ============================================
echo.

:: =====================================================
:: Step 1: 检测/安装 .NET SDK
:: =====================================================
echo [1/3] 检测 .NET SDK 环境...

where dotnet >nul 2>&1
if %errorlevel% neq 0 (
    echo   ✗ .NET SDK 未安装
    goto :install_dotnet
)

:: 获取版本
for /f "tokens=*" %%v in ('dotnet --version 2^>nul') do set DOTNET_VERSION=%%v
for /f "tokens=1 delims=." %%a in ("%DOTNET_VERSION%") do set DOTNET_MAJOR=%%a

if %DOTNET_MAJOR% LSS %REQUIRED_DOTNET_MAJOR% (
    echo   ⚠ .NET SDK %DOTNET_VERSION% 版本过低（需要 ^>= 7.0.0）
    goto :install_dotnet
)

echo   ✓ .NET SDK %DOTNET_VERSION% 已安装
goto :build

:install_dotnet
echo   → 正在安装 .NET SDK %TARGET_DOTNET_VERSION%...

:: 检测 winget
where winget >nul 2>&1
if %errorlevel% equ 0 (
    winget install Microsoft.DotNet.SDK.7 --silent --accept-source-agreements --accept-package-agreements
    goto :verify_dotnet
)

:: 检测 Chocolatey
where choco >nul 2>&1
if %errorlevel% equ 0 (
    choco install dotnet-7.0-sdk -y
    goto :verify_dotnet
)

:: 手动下载安装
echo   → 正在打开 .NET SDK 下载页面...
start https://dotnet.microsoft.com/download/dotnet/7.0
echo.
echo   请安装 .NET SDK 7.0 后重新运行此脚本
pause
exit /b 1

:verify_dotnet
call refreshenv 2>nul
where dotnet >nul 2>&1
if %errorlevel% neq 0 (
    echo   ❌ .NET SDK 安装失败，请重启命令行后重试
    pause
    exit /b 1
)
for /f "tokens=*" %%v in ('dotnet --version') do echo   ✓ .NET SDK %%v 安装完成

:: =====================================================
:: Step 2: 构建项目
:: =====================================================
:build
echo.
echo [2/3] 构建项目...

set OUTPUT_DIR=backend\ProjectFileManager.Wpf\bin\Debug\net7.0-windows

if exist "%OUTPUT_DIR%\ProjectFileManager.Wpf.exe" (
    echo   ✓ 项目已构建
    goto :run
)

echo   → 首次构建项目...
dotnet restore backend\ProjectFileManager.sln
if %errorlevel% neq 0 (
    echo   ❌ 依赖还原失败
    pause
    exit /b 1
)

dotnet build backend\ProjectFileManager.Wpf\ProjectFileManager.Wpf.csproj -c Debug
if %errorlevel% neq 0 (
    echo   ❌ 项目构建失败
    pause
    exit /b 1
)

echo   ✓ 构建完成

:: =====================================================
:: Step 3: 启动应用
:: =====================================================
:run
echo.
echo [3/3] 启动应用...
echo.
echo 🚀 正在启动 %APP_NAME%...
echo.

dotnet run --project backend\ProjectFileManager.Wpf\ProjectFileManager.Wpf.csproj
if %errorlevel% neq 0 (
    echo.
    echo ❌ 应用启动失败
    pause
    exit /b 1
)

exit /b 0
