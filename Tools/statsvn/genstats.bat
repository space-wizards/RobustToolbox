@echo off
if exist out goto continue

:cleanup
call cleanup.bat
goto gen
:gen
mkdir out
cd ..\..\
svn log -g --xml -v svn://games.ques.to/ss3d/ss3d/trunk > Tools\statsvn\out\svn.log
cd Tools\statsvn\out
java -jar ..\statsvn.jar -threads 5 -include "**/*.cs" svn.log ..\..\..\
cd ..
goto end

:continue
SET /P ANSWER=stat output dir is not empty. Continue (Y/N)?
if /i {%ANSWER%}=={y} (goto cleanup)
if /i {%ANSWER%}=={Y} (goto cleanup)
goto end
:end
