start cleanup.bat
cd ..\..\
svn log --xml -r228:HEAD -v > Tools\statsvn\svn.log
cd Tools\statsvn
java -jar statsvn.jar -include "SS3D_Client/**/*.cs:SS3d_server/**/*.cs:SS3D_shared/**/*.cs" svn.log ..\..\