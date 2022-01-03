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
REM Select program path based on current machine environment
set progpath=%ProgramFiles%
if not "%ProgramFiles(x86)%".=="". set progpath=%ProgramFiles(x86)%

REM set logfile where the infos are written to, and clear that file
set LOG=build_%BUILD_TYPE%_MP2.log
echo. > %LOG%

echo.
echo -= OnlineVideos =-
echo -= build mode: %BUILD_TYPE% =-
echo.

echo.
echo Building OnlineVideos for MediaPortal2 ...
@"%progpath%\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBUILD.exe" /target:Rebuild /property:Configuration=%BUILD_TYPE% ..\OnlineVideos.MediaPortal2.sln >> %LOG%
