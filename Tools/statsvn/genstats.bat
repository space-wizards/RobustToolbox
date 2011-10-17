if exist out goto continue

:cleanup
call cleanup.bat
goto gen
:gen
mkdir out
cd ..\..\
svn log --xml -r228:HEAD -v > Tools\statsvn\out\svn.log
cd Tools\statsvn\out
java -jar ..\statsvn.jar -include "SS3D_Client/**/*.cs:SS3d_server/**/*.cs:SS3D_shared/**/*.cs" svn.log ..\..\..\
cd ..
goto end

:continue
SET /P ANSWER=stat output dir is not empty. Continue (Y/N)?
if /i {%ANSWER%}=={y} (goto cleanup)
if /i {%ANSWER%}=={Y} (goto cleanup)
goto end
:end
