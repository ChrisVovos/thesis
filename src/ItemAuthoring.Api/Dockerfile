# syntax=docker/dockerfile:1

# Restore is a separate layer keyed on the project files alone, so a source-only change does not
# re-download the package graph.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source

COPY Directory.Build.props Directory.Packages.props ./
COPY src/ItemAuthoring.Domain/ItemAuthoring.Domain.csproj src/ItemAuthoring.Domain/
COPY src/ItemAuthoring.Application/ItemAuthoring.Application.csproj src/ItemAuthoring.Application/
COPY src/ItemAuthoring.Infrastructure/ItemAuthoring.Infrastructure.csproj src/ItemAuthoring.Infrastructure/
COPY src/ItemAuthoring.Api/ItemAuthoring.Api.csproj src/ItemAuthoring.Api/
RUN dotnet restore src/ItemAuthoring.Api/ItemAuthoring.Api.csproj

COPY .editorconfig ./
COPY src/ src/
RUN dotnet publish src/ItemAuthoring.Api/ItemAuthoring.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# curl is installed solely so the container can report its own health; the base image ships without
# any HTTP client.
RUN apt-get update \
    && apt-get install --yes --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/* \
    && useradd --uid 64198 --create-home --shell /usr/sbin/nologin apiuser

# The image runs as a non-root user. Nothing the API does requires elevated privileges, and a
# container that never needs them should never have them.
USER 64198

COPY --from=build --chown=64198:64198 /app ./

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_gcServer=1

EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=5s --start-period=40s --retries=5 \
    CMD curl --fail --silent http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "ItemAuthoring.Api.dll"]
