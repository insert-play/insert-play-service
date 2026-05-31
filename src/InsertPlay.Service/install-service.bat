@echo off
:: Self-elevating installer for InsertPlay Windows Service.
:: Just double-click this file — it will request elevation and run the PowerShell script.

net session >nul 2>&1
if %errorLevel% == 0 goto :run

:: Not elevated — relaunch with elevation
powershell -Command "Start-Process -FilePath '%~f0' -Verb RunAs"
exit /b

:run
powershell -ExecutionPolicy Bypass -File "%~dp0install-service.ps1"
pause
