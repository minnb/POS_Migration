@echo off
REM ============================================================
REM  POS.Worker launcher — chay boi Windows Task Scheduler
REM  Yeu cau: .NET 10 Runtime da cai san (framework-dependent).
REM  File .bat nay PHAI nam cung thu muc publish (chua POS.Worker.dll
REM  + appsettings.json). publish-worker.ps1 tu copy no vao do.
REM ============================================================

REM 1. Dua CurrentDirectory ve thu muc chua .bat = thu muc publish.
REM    BAT BUOC: generic host (.NET) doc appsettings.json theo CurrentDirectory;
REM    Task Scheduler mac dinh chay voi CWD = C:\Windows\System32 -> khong tim thay config.
cd /d "%~dp0"

REM 2. Chon bo cau hinh. Development => KHONG co appsettings.Development.json
REM    nen chi appsettings.json (base, host 10.235.x that) duoc ap dung.
if "%DOTNET_ENVIRONMENT%"=="" set DOTNET_ENVIRONMENT=Development

REM 3. Ghi stdout/stderr cua process ra file (soi loi khoi dong).
REM    Serilog van ghi Elasticsearch + file log rieng theo config; day chi la console log.
if not exist logs md logs

echo [%date% %time%] Starting POS.Worker (env=%DOTNET_ENVIRONMENT%)>> logs\worker-console.log

REM 4. Chay o FOREGROUND => Task Scheduler giu task o trang thai "Running".
REM    Uu tien apphost POS.Worker.exe (khong phu thuoc PATH cua 'dotnet' khi chay duoi SYSTEM);
REM    fallback 'dotnet POS.Worker.dll' neu vi ly do nao do khong co .exe.
REM    Process thoat (crash/dung) => task ket thuc => Task Scheduler restart-on-failure.
if exist "POS.Worker.exe" (
    "POS.Worker.exe" >> logs\worker-console.log 2>&1
) else (
    dotnet POS.Worker.dll >> logs\worker-console.log 2>&1
)

echo [%date% %time%] POS.Worker exited with code %ERRORLEVEL%>> logs\worker-console.log
exit /b %ERRORLEVEL%
