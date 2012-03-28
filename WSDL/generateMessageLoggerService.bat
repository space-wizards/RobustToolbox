@ECHO OFF
call "C:\Program Files (x86)\Microsoft Visual Studio 10.0\VC\vcvarsall.bat"
IF EXIST ..\MessagingProfiler\bin\Release\MessagingProfiler.exe GOTO profexists
goto noprofiler
:profexists
echo Removing old message logger code and config files...
del /Q ..\ClientServices\MessageLogging\messageLoggerService.config
del /Q ..\ServerServices\MessageLogging\messageLoggerService.config
del /Q ..\ClientServices\MessageLogging\MessageLoggerService.cs
del /Q ..\ServerServices\MessageLogging\MessageLoggerService.cs
goto generate
:generate
start ..\MessagingProfiler\bin\Release\MessagingProfiler.exe
echo Please press enter once the messaging profiler app has loaded.
pause
echo Generating new service interface code and config files...
svcutil /language:c# /out:MessageLoggerService.cs /config:app.config net.pipe://MessageLoggerService > log.txt
goto copyOutput
:noprofiler
echo You need to build MessagingProfiler in Release config before running this.
exit
:copyOutput
echo Copying service interface code and config files...
copy MessageLoggerService.cs ..\ClientServices\MessageLogging\MessageLoggerService.cs
copy MessageLoggerService.cs ..\ServerServices\MessageLogging\MessageLoggerService.cs
copy app.config ..\ClientServices\MessageLogging\MessageLoggerService.config
copy app.config ..\ServerServices\MessageLogging\MessageLoggerService.config
echo Done. Don't forget to update Client\app.config and Server\app.config with the new ServiceModel bindings if they have changed.
goto end
:end
echo Successfully regenerated service interface and config.
pause
del log.txt
