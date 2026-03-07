@echo off
:: NotifyLite One-Click Installer
:: Double-click this file to install NotifyLite

:: Self-elevate to admin
>nul 2>&1 net session
if %errorlevel% neq 0 (
    echo Requesting administrator privileges...
    powershell -Command "Start-Process '%~f0' -Verb RunAs"
    exit /b
)

:: Run the PowerShell installer
cd /d "%~dp0"
powershell -ExecutionPolicy Bypass -File "%~dp0Install.ps1"
