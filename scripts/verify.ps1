[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"
$repositoryRoot = Split-Path -Parent $PSScriptRoot

Push-Location $repositoryRoot
try {
    dotnet tool restore
    dotnet build XauAi.slnx --configuration Release
    dotnet test tests/XauAi.UnitTests --configuration Release --no-build
    dotnet test tests/XauAi.IntegrationTests --configuration Release --no-build
    dotnet test tests/XauAi.ArchitectureTests --configuration Release --no-build
    dotnet format XauAi.slnx --verify-no-changes --severity warn --no-restore
    dotnet tool run dotnet-ef migrations has-pending-model-changes `
        --no-build `
        --project backend/XauAi.Infrastructure/XauAi.Infrastructure.csproj `
        --startup-project backend/XauAi.Infrastructure/XauAi.Infrastructure.csproj `
        --context XauAiDbContext

    Push-Location frontend/xau-ai-web
    try {
        npm ci
        npm run lint
        npm run build
    }
    finally {
        Pop-Location
    }

    docker compose config --quiet
}
finally {
    Pop-Location
}
