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
set LOG=build_%BUILD_TYPE%.log
echo. > %LOG%


echo.
echo -= OnlineVideos =-
echo -= build mode: %BUILD_TYPE% =-
echo.

echo.
echo Building OnlineVideos...
"%WINDIR%\Microsoft.NET\Framework\v4.0.30319\MSBUILD.exe" /target:Rebuild /property:Configuration=%BUILD_TYPE% "..\OnlineVideos.MediaPortal1.sln" >> %LOG%

echo Building MPEI
copy "..\MPEI\OnlineVideos.xmp2" "..\MPEI\OnlineVideos_COPY.xmp2"
"%progpath%\Team MediaPortal\MediaPortal\MpeMaker.exe" "..\MPEI\OnlineVideos_COPY.xmp2" /B >> %LOG%
del "..\MPEI\OnlineVideos_COPY.xmp2"