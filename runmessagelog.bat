@echo off
set PDIR=%~dp0
cd %PDIR%Bin\MessagingProfiler

call MessagingProfiler.exe %*

cd %PDIR%

set PDIR=

pause

