@echo off
set PDIR=%~dp0
cd %PDIR%Bin\Server
call SpaceStation14_Server.exe %*
cd %PDIR%
set PDIR=
pause