FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Build.props ./
COPY backend/XauAi.Domain/XauAi.Domain.csproj backend/XauAi.Domain/
COPY backend/XauAi.Application/XauAi.Application.csproj backend/XauAi.Application/
COPY backend/XauAi.Infrastructure/XauAi.Infrastructure.csproj backend/XauAi.Infrastructure/
COPY backend/XauAi.Api/XauAi.Api.csproj backend/XauAi.Api/
RUN dotnet restore backend/XauAi.Api/XauAi.Api.csproj

COPY backend/ backend/
RUN dotnet publish backend/XauAi.Api/XauAi.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
USER root
RUN apt-get update \
    && apt-get install --yes --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
USER $APP_UID
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "XauAi.Api.dll"]
