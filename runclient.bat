@echo off
set PDIR=%~dp0
cd %PDIR%Bin\Client
start SpaceStation14.exe %*
cd %PDIR%
set PDIR=
