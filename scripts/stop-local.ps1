[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$runtimeDirectory = Join-Path $repositoryRoot ".data\runtime"

function Stop-ProcessTree([int]$ProcessId) {
    $children = Get-CimInstance Win32_Process -Filter "ParentProcessId = $ProcessId" -ErrorAction SilentlyContinue
    foreach ($child in $children) {
        Stop-ProcessTree -ProcessId $child.ProcessId
    }
    Stop-Process -Id $ProcessId -Force -ErrorAction SilentlyContinue
}

foreach ($name in @("supervisor.pid", "api-host.pid")) {
    $pidFile = Join-Path $runtimeDirectory $name
    if (-not (Test-Path -LiteralPath $pidFile)) {
        continue
    }

    $savedPid = Get-Content -LiteralPath $pidFile -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($savedPid -match '^\d+$') {
        Stop-ProcessTree -ProcessId ([int]$savedPid)
    }
    Remove-Item -LiteralPath $pidFile -Force -ErrorAction SilentlyContinue
}

Push-Location $repositoryRoot
try {
    docker compose stop web api
}
finally {
    Pop-Location
}

Write-Host "Local analyst runtime stopped. SQL Server and Redis remain available."
