@echo off
set PDIR=%~dp0
cd %PDIR%Bin\Server
call SS13_Server.exe %*
cd %PDIR%
set PDIR=
pause