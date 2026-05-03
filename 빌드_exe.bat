@echo off
chcp 65001 > nul
echo.
echo  ==========================================
echo   날씨위젯 EXE 빌드
echo  ==========================================
echo.

rem .NET Framework CSC 컴파일러 찾기 (Windows 기본 내장)
set CSC=
if exist "%SystemRoot%\Microsoft.NET\Framework64\v4.0.30319\csc.exe" (
    set CSC=%SystemRoot%\Microsoft.NET\Framework64\v4.0.30319\csc.exe
) else if exist "%SystemRoot%\Microsoft.NET\Framework\v4.0.30319\csc.exe" (
    set CSC=%SystemRoot%\Microsoft.NET\Framework\v4.0.30319\csc.exe
)

if not defined CSC (
    echo [오류] .NET Framework 4.x를 찾을 수 없습니다.
    echo Windows 10 / 11 에는 기본 설치되어 있어야 합니다.
    pause
    exit /b 1
)

echo C# 컴파일러: %CSC%
echo.
echo 빌드 중...
echo.

"%CSC%" ^
    /target:winexe ^
    /out:날씨위젯.exe ^
    /utf8output ^
    /win32icon:app.ico ^
    /reference:System.Windows.Forms.dll ^
    /reference:System.Drawing.dll ^
    WeatherWidget.cs

if %ERRORLEVEL% neq 0 (
    echo.
    echo [오류] 빌드 실패. 위의 오류 메시지를 확인하세요.
    pause
    exit /b 1
)

echo.
echo  ==========================================
echo   성공!  날씨위젯.exe 파일이 생성됐습니다.
echo   이 폴더에서 날씨위젯.exe 를 실행하세요.
echo  ==========================================
echo.
pause
