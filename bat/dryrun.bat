@echo off

cd /d %~dp0

if "%~1"=="" (
  echo �t�H���_���h���b�O�A���h�h���b�v���Ă��������B
  pause
  exit /b
)


..\TSDivider\bin\Release\net8.0\TSDivider.exe "%~1" --dry-run

pause
exit /b