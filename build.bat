@echo off
setlocal enabledelayedexpansion
chcp 65001 >nul

:: 현재 배치 파일이 위치한 디렉토리 가져오기
set BASE_DIR=%~dp0
set OUTPUT_DIR=%BASE_DIR%bin\Publish

:: 프로젝트 파일 경로 (배치 파일과 같은 폴더에 있다고 가정)
set PROJECT_FILE=%BASE_DIR%Code_Nova_Guardian.csproj

:: 실행 파일명 설정
set EXECUTABLE_NAME=cng

:: Windows 64비트 빌드
echo Windows 64비트 실행 파일 빌드 시작
dotnet publish "%PROJECT_FILE%" -r win-x64 -c Release --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=false -o "%OUTPUT_DIR%\win-x64"
rename "%OUTPUT_DIR%\win-x64\YourProjectName.exe" "%EXECUTABLE_NAME%.exe"

:: Linux 64비트 빌드
echo Linux 64비트 실행 파일 빌드 시작
dotnet publish "%PROJECT_FILE%" -r linux-x64 -c Release --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=false -o "%OUTPUT_DIR%\linux-x64"
rename "%OUTPUT_DIR%\linux-x64\YourProjectName" "%EXECUTABLE_NAME%"

echo 빌드 완료! 실행 파일이 "%OUTPUT_DIR%"에 저장되었습니다.
pause
