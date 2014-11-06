@ECHO OFF
SET EnableNuGetPackageRestore=true
IF "%1"=="install" GOTO devinstall

%WINDIR%\Microsoft.NET\Framework\v4.0.30319\msbuild.exe .\build\build.proj /flp:LogFile=build.log %*
GOTO done

:devinstall
%windir%\Microsoft.NET\Framework\v4.0.30319\msbuild.exe .\.nuget\SignalR.MagicHub.Install\devinstall.proj /p:TargetDirectory=%CD%\src\SignalR.MagicHub.WebHost
GOTO done

:done
IF NOT %ERRORLEVEL% == 0 EXIT /B 1