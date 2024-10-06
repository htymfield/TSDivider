@echo off

cd /d %~dp0

if "%~1"=="" (
  echo フォルダをドラッグアンドドロップしてください。
  pause
  exit /b
)


..\TSDivider\bin\Release\net8.0\TSDivider.exe "%~1" --dry-run

pause
exit /b