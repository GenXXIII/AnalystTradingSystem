[CmdletBinding()]
param(
    [int]$RestartDelaySeconds = 3
)

$ErrorActionPreference = "Stop"
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$apiScript = Join-Path $PSScriptRoot "start-mt5-api.ps1"
$runtimeDirectory = Join-Path $repositoryRoot ".data\runtime"
$logDirectory = Join-Path $repositoryRoot "logs"
$supervisorPidFile = Join-Path $runtimeDirectory "supervisor.pid"
$apiPidFile = Join-Path $runtimeDirectory "api-host.pid"
$apiOutputLog = Join-Path $logDirectory "api-host.log"
$apiErrorLog = Join-Path $logDirectory "api-host.error.log"
$createdNew = $false
$mutex = [Threading.Mutex]::new($true, "Local\XauAiRuntimeSupervisor", [ref]$createdNew)

if (-not $createdNew) {
    $mutex.Dispose()
    exit 0
}

New-Item -ItemType Directory -Path $runtimeDirectory -Force | Out-Null
New-Item -ItemType Directory -Path $logDirectory -Force | Out-Null
Set-Content -LiteralPath $supervisorPidFile -Value $PID

function Test-ApiHealth {
    try {
        $response = Invoke-WebRequest -Uri "http://localhost:5081/health" -UseBasicParsing -TimeoutSec 2
        return $response.StatusCode -eq 200
    }
    catch {
        return $false
    }
}

try {
    while ($true) {
        if (Test-ApiHealth) {
            Start-Sleep -Seconds 2
            continue
        }

        $quotedApiScript = '"' + $apiScript + '"'
        $apiProcess = Start-Process `
            -FilePath "powershell.exe" `
            -ArgumentList @("-NoProfile", "-ExecutionPolicy", "Bypass", "-File", $quotedApiScript) `
            -WindowStyle Hidden `
            -RedirectStandardOutput $apiOutputLog `
            -RedirectStandardError $apiErrorLog `
            -PassThru

        Set-Content -LiteralPath $apiPidFile -Value $apiProcess.Id
        $apiProcess.WaitForExit()
        Remove-Item -LiteralPath $apiPidFile -Force -ErrorAction SilentlyContinue
        Start-Sleep -Seconds $RestartDelaySeconds
    }
}
finally {
    Remove-Item -LiteralPath $apiPidFile -Force -ErrorAction SilentlyContinue
    Remove-Item -LiteralPath $supervisorPidFile -Force -ErrorAction SilentlyContinue
    $mutex.ReleaseMutex()
    $mutex.Dispose()
}
