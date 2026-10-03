[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$environmentFile = Join-Path $repositoryRoot ".env"

if (-not (Test-Path -LiteralPath $environmentFile)) {
    throw "Create .env from .env.example and replace its guide placeholders before starting containers."
}

$unreplacedGuideLines = Get-Content -LiteralPath $environmentFile |
    Where-Object { $_ -match '^[A-Z][A-Z0-9_]*=YOUR_' }

if ($unreplacedGuideLines) {
    $unreplacedNames = $unreplacedGuideLines |
        ForEach-Object { ($_ -split '=', 2)[0] }
    throw "Replace these guide placeholders in the ignored .env file: $($unreplacedNames -join ', ')."
}

$passwordLine = Get-Content -LiteralPath $environmentFile |
    Where-Object { $_ -match '^MSSQL_SA_PASSWORD=' } |
    Select-Object -First 1

if (-not $passwordLine -or $passwordLine -match 'REPLACE_WITH|USER_PROVIDED') {
    throw "Set MSSQL_SA_PASSWORD in the ignored .env file to a strong local-only value before starting containers."
}

$applicationPasswordLine = Get-Content -LiteralPath $environmentFile |
    Where-Object { $_ -match '^DATABASE_APP_PASSWORD=' } |
    Select-Object -First 1

if (-not $applicationPasswordLine -or $applicationPasswordLine -match 'REPLACE_WITH|USER_PROVIDED') {
    throw "Set DATABASE_APP_PASSWORD in the ignored .env file to a different strong local-only value before starting containers."
}

$databaseAdminPassword = $passwordLine.Substring('MSSQL_SA_PASSWORD='.Length)
$databaseApplicationPassword = $applicationPasswordLine.Substring('DATABASE_APP_PASSWORD='.Length)
$safePasswordPattern = '^[A-Za-z0-9!@#%^&*()_+\-=]{12,128}$'

if ($databaseAdminPassword -eq $databaseApplicationPassword) {
    throw "MSSQL_SA_PASSWORD and DATABASE_APP_PASSWORD must be different values."
}

if ($databaseAdminPassword -notmatch $safePasswordPattern -or $databaseApplicationPassword -notmatch $safePasswordPattern) {
    throw "Database passwords must be 12-128 characters and use letters, numbers, or supported non-quoting symbols."
}

$values = @{}
Get-Content -LiteralPath $environmentFile |
    Where-Object { $_ -match '^[A-Z][A-Z0-9_]*=' } |
    ForEach-Object {
        $parts = $_ -split '=', 2
        $values[$parts[0]] = $parts[1]
    }

$requiredMt5Values = @("MT5_LOGIN", "MT5_PASSWORD", "MT5_SERVER", "MT5_TERMINAL_PATH")
$missingMt5Values = $requiredMt5Values | Where-Object {
    [string]::IsNullOrWhiteSpace($values[$_]) -or $values[$_] -match 'USER_PROVIDED|YOUR_|REPLACE_WITH'
}

if ($values["MT5_ENABLED"] -eq "true" -and $missingMt5Values) {
    throw "Set these MT5 values in the ignored .env file: $($missingMt5Values -join ', ')."
}

if ($values["MT5_ENABLED"] -eq "true" -and -not (Test-Path -LiteralPath $values["MT5_TERMINAL_PATH"])) {
    throw "MT5_TERMINAL_PATH does not point to terminal64.exe."
}

$runtimeDirectory = Join-Path $repositoryRoot ".data\runtime"
$supervisorPidFile = Join-Path $runtimeDirectory "supervisor.pid"
$supervisorScript = Join-Path $PSScriptRoot "runtime-supervisor.ps1"
$supervisorRunning = $false

if (Test-Path -LiteralPath $supervisorPidFile) {
    $savedPid = Get-Content -LiteralPath $supervisorPidFile -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($savedPid -match '^\d+$') {
        $supervisorRunning = $null -ne (Get-Process -Id ([int]$savedPid) -ErrorAction SilentlyContinue)
    }
}

Push-Location $repositoryRoot
try {
    docker compose up --build -d sqlserver redis database-init database-migrator
    docker compose stop api | Out-Null
    docker compose build web
    docker compose up -d --no-deps web

    if (-not $supervisorRunning) {
        New-Item -ItemType Directory -Path $runtimeDirectory -Force | Out-Null
        $quotedSupervisorScript = '"' + $supervisorScript + '"'
        $supervisor = Start-Process `
            -FilePath "powershell.exe" `
            -ArgumentList @("-NoProfile", "-ExecutionPolicy", "Bypass", "-File", $quotedSupervisorScript) `
            -WindowStyle Hidden `
            -PassThru
        Set-Content -LiteralPath $supervisorPidFile -Value $supervisor.Id
    }

    $apiHealthy = $false
    for ($attempt = 0; $attempt -lt 60; $attempt++) {
        try {
            $response = Invoke-WebRequest -Uri "http://localhost:5081/health" -UseBasicParsing -TimeoutSec 2
            if ($response.StatusCode -eq 200) {
                $apiHealthy = $true
                break
            }
        }
        catch {
        }
        Start-Sleep -Seconds 1
    }

    if (-not $apiHealthy) {
        throw "The Windows-host API did not become healthy. Check logs/api-host.error.log."
    }

    docker compose ps sqlserver redis web
    Write-Host "Analyst web: http://localhost:3001"
    Write-Host "API:         http://localhost:5081"
    Write-Host "MT5 is monitored and will reopen automatically while the project runtime is active."
}
finally {
    Pop-Location
}
