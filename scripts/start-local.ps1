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
$applicationPasswordLine = Get-Content -LiteralPath $environmentFile |
    Where-Object { $_ -match '^DATABASE_APP_PASSWORD=' } |
    Select-Object -First 1

if (-not $passwordLine -or $passwordLine -match 'REPLACE_WITH|USER_PROVIDED') {
    throw "Set MSSQL_SA_PASSWORD in the ignored .env file to a strong local-only value before starting containers."
}

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

Push-Location $repositoryRoot
try {
    docker compose up --build -d

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
        docker compose logs --tail=100 api
        throw "The Docker API did not become healthy."
    }

    docker compose ps
    Write-Host "Analyst web: http://localhost:3001"
    Write-Host "API:         http://localhost:5081"
    Write-Host "Market data: AllTick live with Twelve Data history/reference"
}
finally {
    Pop-Location
}
