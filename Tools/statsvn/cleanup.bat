@echo off
if not exist out goto END
rmdir/s /q out
:END
echo Cleanup Executed successfully.
