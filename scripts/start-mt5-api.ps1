[CmdletBinding()]
param(
    [string]$ApiUrl = "http://localhost:5081"
)

$ErrorActionPreference = "Stop"
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$environmentFile = Join-Path $repositoryRoot ".env"

if (-not (Test-Path -LiteralPath $environmentFile)) {
    throw "Create .env from .env.example before starting the Windows-host MT5 API."
}

$values = @{}
Get-Content -LiteralPath $environmentFile |
    Where-Object { $_ -match '^[A-Z][A-Z0-9_]*=' } |
    ForEach-Object {
        $parts = $_ -split '=', 2
        $values[$parts[0]] = $parts[1]
    }

$required = @(
    "MT5_LOGIN",
    "MT5_PASSWORD",
    "MT5_SERVER",
    "MT5_TERMINAL_PATH",
    "MSSQL_SA_PASSWORD",
    "DATABASE_APP_PASSWORD",
    "SQLSERVER_PORT"
)

$missing = $required | Where-Object {
    $value = $values[$_]
    [string]::IsNullOrWhiteSpace($value) -or $value -match 'USER_PROVIDED|YOUR_|REPLACE_WITH'
}

if ($missing) {
    throw "Replace these placeholders in the ignored .env file: $($missing -join ', ')."
}

if (-not (Test-Path -LiteralPath $values["MT5_TERMINAL_PATH"])) {
    throw "MT5_TERMINAL_PATH does not point to terminal64.exe."
}

foreach ($entry in $values.GetEnumerator()) {
    Set-Item -LiteralPath "Env:$($entry.Key)" -Value $entry.Value
}

$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:ASPNETCORE_URLS = $ApiUrl
$env:DATABASE_ENABLED = "true"
$migratorConnection = "Server=localhost,$($values['SQLSERVER_PORT']);Database=XauAi;User Id=sa;Password=$($values['MSSQL_SA_PASSWORD']);TrustServerCertificate=True;Encrypt=True"
$applicationConnection = "Server=localhost,$($values['SQLSERVER_PORT']);Database=XauAi;User Id=xauai_app;Password=$($values['DATABASE_APP_PASSWORD']);TrustServerCertificate=True;Encrypt=True"

Push-Location $repositoryRoot
try {
    $env:DATABASE_CONNECTION_STRING = $migratorConnection
    $env:DATABASE_APPLY_MIGRATIONS_ON_STARTUP = "true"
    dotnet run --project backend/XauAi.Api --no-launch-profile -- --migrate-only
    if ($LASTEXITCODE -ne 0) {
        throw "Database migration failed; the API was not started."
    }

    $env:DATABASE_CONNECTION_STRING = $applicationConnection
    $env:DATABASE_APPLY_MIGRATIONS_ON_STARTUP = "false"
    dotnet run --project backend/XauAi.Api --no-launch-profile --no-build
}
finally {
    Remove-Variable -Name migratorConnection, applicationConnection -ErrorAction SilentlyContinue
    Pop-Location
}
