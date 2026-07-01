# ============================================================
#  Publish POS.Worker (framework-dependent) ra thu muc deploy
#  + copy run-worker.bat vao cung thu muc.
#  Yeu cau may deploy da cai .NET 10 Runtime.
#
#  Vi du:  .\publish-worker.ps1 -Output "D:\POS\Worker"
# ============================================================
param(
    [string]$Output = "D:\POS\Worker"
)

$ErrorActionPreference = "Stop"

$proj = Resolve-Path (Join-Path $PSScriptRoot "..\..\src\POS.Worker\POS.Worker.csproj")
$bat  = Join-Path $PSScriptRoot "run-worker.bat"

Write-Host "==> Publishing $proj -> $Output (framework-dependent, Release)"
dotnet publish $proj -c Release -o $Output
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed ($LASTEXITCODE)" }

Copy-Item $bat $Output -Force
Write-Host "==> Copied run-worker.bat -> $Output"

Write-Host ""
Write-Host "DONE. Publish folder: $Output"
Write-Host "  - Kiem tra: dotnet --list-runtimes  (phai co Microsoft.NETCore.App 10.x)"
Write-Host "  - Dang ky Task Scheduler:  schtasks /Create /TN ""POS.Worker"" /XML ""$Output\POS.Worker.Task.xml"" /F"
Write-Host "    (sua duong dan Command/WorkingDirectory trong XML neu Output khac D:\POS\Worker)"
