@echo off
REM UnlockOpenFile Example Batch Script
REM This script demonstrates how to open files using UnlockOpenFile from a batch file

REM Set the path to UnlockOpenFile.exe
set UNLOCKER_PATH=C:\Program Files\UnlockOpenFile\UnlockOpenFile.exe

REM Check if UnlockOpenFile.exe exists
if not exist "%UNLOCKER_PATH%" (
    echo Error: UnlockOpenFile.exe not found at %UNLOCKER_PATH%
    echo Please update UNLOCKER_PATH in this script
    pause
    exit /b 1
)

REM Example 1: Open a specific file
echo Opening example.xlsx...
"%UNLOCKER_PATH%" "C:\Data\example.xlsx"

REM Example 2: Open file passed as argument
if not "%~1"=="" (
    echo Opening %~1...
    "%UNLOCKER_PATH%" "%~1"
)

REM Example 3: Open multiple files sequentially
REM for %%f in (*.xlsx) do (
REM     echo Opening %%f...
REM     start "" "%UNLOCKER_PATH%" "%%f"
REM )
