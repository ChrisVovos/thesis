<#
.SYNOPSIS
    Exports the OpenAPI document and the GraphQL schema that the Angular client generates from.

.DESCRIPTION
    The API is started in export mode, which builds the OpenAPI document and the GraphQL schema and
    exits without opening a port or touching the database. Committing the two artefacts is what makes
    `npm run codegen` — and therefore the client build — reproducible offline and in CI.
#>
[CmdletBinding()]
param(
    [string]$OutputDirectory = (Join-Path $PSScriptRoot '..\artifacts')
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = Resolve-Path (Join-Path $PSScriptRoot '..')
$resolvedOutput = [System.IO.Path]::GetFullPath($OutputDirectory)

New-Item -ItemType Directory -Path $resolvedOutput -Force | Out-Null

Push-Location $repositoryRoot
try {
    $env:ASPNETCORE_ENVIRONMENT = 'Development'
    dotnet run --project src/ItemAuthoring.Api -- --export-contracts $resolvedOutput
    if ($LASTEXITCODE -ne 0) {
        throw "The contract export failed with exit code $LASTEXITCODE."
    }
}
finally {
    Pop-Location
}

Write-Host "Contracts written to $resolvedOutput" -ForegroundColor Green
