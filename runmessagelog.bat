@echo off
set PDIR=%~dp0
cd %PDIR%Bin\MessagingProfiler

call SS14.Tools.MessagingProfiler.exe %*

cd %PDIR%

set PDIR=

pause

