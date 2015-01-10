@ECHO OFF

REM detect if BUILD_TYPE should be release or debug
if not %1!==Debug! goto RELEASE
:DEBUG
set BUILD_TYPE=Debug
goto START
:RELEASE
set BUILD_TYPE=Release
goto START


:START
REM set logfile where the infos are written to, and clear that file
set LOG=build_%BUILD_TYPE%_MP2.log
echo. > %LOG%

echo.
echo -= OnlineVideos =-
echo -= build mode: %BUILD_TYPE% =-
echo.

echo.
echo Building OnlineVideos for MediaPortal2 ...
@"%WINDIR%\Microsoft.NET\Framework\v4.0.30319\MSBUILD.exe" /target:Rebuild /property:Configuration=%BUILD_TYPE% ..\OnlineVideos.MediaPortal2.sln >> %LOG%
