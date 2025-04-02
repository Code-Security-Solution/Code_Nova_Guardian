@echo off
setlocal enabledelayedexpansion
chcp 65001 >nul

:: .NET SDK 버전 지정
set DOTNET_VERSION=8.0.0

:: 현재 배치 파일이 위치한 디렉토리 가져오기
set BASE_DIR=%~dp0
set OUTPUT_DIR=%BASE_DIR%bin\Publish

:: 프로젝트 파일 경로 (배치 파일과 같은 폴더에 있다고 가정)
set PROJECT_FILE=%BASE_DIR%Code_Nova_Guardian.csproj

:: 실행 파일명 설정
set EXECUTABLE_NAME=cng

:: .NET 8.0 강제 사용 (dotnet CLI에서 버전 지정하여 실행)
set DOTNET_CMD=dotnet

:: Windows 64비트 빌드
echo Windows 64비트 실행 파일 빌드 시작
%DOTNET_CMD% publish "%PROJECT_FILE%" -r win-x64 -f net8.0 -c Release --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=false -o "%OUTPUT_DIR%\win-x64"
move /Y "%OUTPUT_DIR%\win-x64\cng.exe" "%OUTPUT_DIR%\win-x64\%EXECUTABLE_NAME%.exe"

:: Linux 64비트 빌드
echo Linux 64비트 실행 파일 빌드 시작
%DOTNET_CMD% publish "%PROJECT_FILE%" -r linux-x64 -f net8.0 -c Release --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=false -o "%OUTPUT_DIR%\linux-x64"
move /Y "%OUTPUT_DIR%\linux-x64\cng" "%OUTPUT_DIR%\linux-x64\%EXECUTABLE_NAME%"

:: macOS 64비트 빌드
echo macOS 64비트 실행 파일 빌드 시작
%DOTNET_CMD% publish "%PROJECT_FILE%" -r osx-x64 -f net8.0 -c Release --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=false -o "%OUTPUT_DIR%\osx-x64"
move /Y "%OUTPUT_DIR%\osx-x64\cng" "%OUTPUT_DIR%\osx-x64\%EXECUTABLE_NAME%"

echo 빌드 완료! 실행 파일이 "%OUTPUT_DIR%"에 저장되었습니다.
pause
