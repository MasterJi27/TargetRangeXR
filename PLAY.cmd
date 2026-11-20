@echo off
setlocal
cd /d "%~dp0"
if not exist "Builds\Windows\Target Range XR.exe" goto missing
if not exist "Builds\Windows\UnityPlayer.dll" goto missing
if not exist "Builds\Windows\Target Range XR_Data\globalgamemanagers" goto missing
pushd "Builds\Windows"
start "" "Target Range XR.exe" -logFile "%TEMP%\Task2-Player.log"
popd
exit /b 0
:missing
echo Game files are missing. Extract the ENTIRE ZIP first, then run PLAY.cmd.
echo Keep Builds\Windows intact, including the Data folder and UnityPlayer.dll.
pause
exit /b 1
