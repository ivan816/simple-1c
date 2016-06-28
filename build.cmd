@echo off
set dotNetBasePath=%windir%\Microsoft.NET\Framework
if exist %dotNetBasePath%64 set dotNetBasePath=%dotNetBasePath%64
for /R %dotNetBasePath% %%i in (*msbuild.exe) do set msbuild=%%i

set target=%~dp0Simple1C.sln

%msbuild% /t:Rebuild /v:m /p:Configuration=Release %target% || exit /b 1