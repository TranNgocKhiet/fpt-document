@echo off
setlocal enabledelayedexpansion

:: Ghép tất cả các tham số thành một message đầy đủ
set msg=%1
:loop
shift
if "%~1"=="" goto continue
set msg=%msg% %1
goto loop

:continue
if "%msg%"=="" (
    echo Vui long nhap commit message.
    exit /b 1
)

git add .
git commit -m "%msg%"
git push origin main