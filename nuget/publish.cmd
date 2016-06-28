@echo off
set nugetPath=C:\tools\nuget.exe
del *.nupkg
call ..\build.cmd || exit /b -1
call %nugetPath% pack simple1C.nuspec || exit /b -1
%nugetPath% push *.nupkg -Source https://www.nuget.org/api/v2/package