@echo off
set PDIR=%~dp0
cd SS3D_Client\bin\x86\Release
start SpaceStation13.exe %*
cd %PDIR%
set PDIR=
