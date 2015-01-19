@echo off
set PDIR=%~dp0
cd SS3d_server\bin\x86\Release
call SS13_Server.exe %*
cd %PDIR%
set PDIR=
pause