[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"
$repositoryRoot = Split-Path -Parent $PSScriptRoot
Push-Location $repositoryRoot
try {
    docker compose stop web api
}
finally {
    Pop-Location
}

Write-Host "Local analyst runtime stopped. SQL Server and Redis remain available."
