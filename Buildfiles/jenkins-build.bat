call "c:\Program Files (x86)\Microsoft Visual Studio 10.0\VC\vcvarsall.bat" x86
@echo on
call prebuild-2010.cmd
call msbuild SpaceStation14.sln /t:Build /p:Configuration=Release;Platform=x86
