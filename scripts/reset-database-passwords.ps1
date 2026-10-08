[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$environmentFile = Join-Path $repositoryRoot ".env"

function Read-EnvironmentValue([string]$Name) {
    $line = Get-Content -LiteralPath $environmentFile |
        Where-Object { $_ -like "$Name=*" } |
        Select-Object -First 1
    if (-not $line) {
        throw "Set $Name in the ignored .env file."
    }

    return $line.Substring($Name.Length + 1)
}

if (-not (Test-Path -LiteralPath $environmentFile)) {
    throw "Create .env from .env.example before resetting database passwords."
}

$databaseAdminPassword = Read-EnvironmentValue "MSSQL_SA_PASSWORD"
$databaseApplicationPassword = Read-EnvironmentValue "DATABASE_APP_PASSWORD"
$safePasswordPattern = '^[A-Za-z0-9!@#%^&*()_+\-=]{12,128}$'

if ($databaseAdminPassword -eq $databaseApplicationPassword) {
    throw "MSSQL_SA_PASSWORD and DATABASE_APP_PASSWORD must be different values."
}

if ($databaseAdminPassword -notmatch $safePasswordPattern -or
    $databaseApplicationPassword -notmatch $safePasswordPattern) {
    throw "Database passwords must be 12-128 characters and use letters, numbers, or supported non-quoting symbols."
}

Push-Location $repositoryRoot
try {
    docker compose config --quiet
    if ($LASTEXITCODE -ne 0) {
        throw "Docker Compose configuration is invalid."
    }

    Write-Host "Stopping SQL Server before the offline password reset..."
    docker compose stop --timeout 60 sqlserver
    if ($LASTEXITCODE -ne 0) {
        throw "SQL Server could not be stopped safely."
    }

    Write-Host "Resetting the persisted sa password to MSSQL_SA_PASSWORD from .env..."
    docker compose run --rm --no-deps --entrypoint /opt/mssql/bin/mssql-conf sqlserver -n set-sa-password
    if ($LASTEXITCODE -ne 0) {
        throw "The persisted SQL Server sa password could not be reset. No volume was deleted."
    }

    docker compose up -d sqlserver
    if ($LASTEXITCODE -ne 0) {
        throw "SQL Server could not be restarted after the password reset."
    }

    $healthy = $false
    for ($attempt = 0; $attempt -lt 60; $attempt++) {
        $health = docker inspect --format '{{if .State.Health}}{{.State.Health.Status}}{{else}}{{.State.Status}}{{end}}' xauusd-ai-sqlserver-1 2>$null
        if ($health -eq "healthy") {
            $healthy = $true
            break
        }

        Start-Sleep -Seconds 2
    }

    if (-not $healthy) {
        docker compose logs --tail=100 sqlserver
        throw "SQL Server did not become healthy after the password reset."
    }

    Write-Host "Synchronizing the application login password from .env..."
    docker compose run --rm --no-deps database-init
    if ($LASTEXITCODE -ne 0) {
        throw "The xauai_app password could not be synchronized."
    }

    Write-Host "Database passwords now match the ignored .env file; the existing volume was preserved."
}
finally {
    Pop-Location
}
