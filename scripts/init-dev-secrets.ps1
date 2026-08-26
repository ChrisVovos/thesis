<#
.SYNOPSIS
    Generates the development secrets the API needs and stores them with dotnet user-secrets.

.DESCRIPTION
    The repository deliberately contains no signing key and no administrator password. This script
    generates strong values, writes them to the per-developer secret store outside the repository,
    and prints the administrator credentials once so they can be recorded in a password manager.

    Run it again with -Force to rotate the values.
#>
[CmdletBinding()]
param(
    [switch]$Force
)

$ErrorActionPreference = 'Stop'
$project = Join-Path $PSScriptRoot '..\src\ItemAuthoring.Api\ItemAuthoring.Api.csproj' | Resolve-Path

function New-RandomSecret {
    param([int]$ByteCount = 48)
    $bytes = [byte[]]::new($ByteCount)
    [System.Security.Cryptography.RandomNumberGenerator]::Fill($bytes)
    return [Convert]::ToBase64String($bytes)
}

$existing = & dotnet user-secrets list --project $project 2>$null
if ($LASTEXITCODE -ne 0) {
    & dotnet user-secrets init --project $project | Out-Null
    $existing = ''
}

if (($existing -match 'Jwt:SigningKey') -and -not $Force) {
    Write-Host 'Development secrets already exist. Re-run with -Force to rotate them.'
    return
}

$signingKey = New-RandomSecret -ByteCount 48
# The administrator password must satisfy the platform password policy: 12+ characters with an
# upper case letter, a lower case letter, a digit and a symbol.
$administratorPassword = 'Aa1!' + (New-RandomSecret -ByteCount 12)

& dotnet user-secrets set 'Jwt:SigningKey' $signingKey --project $project | Out-Null
& dotnet user-secrets set 'Seed:AdministratorPassword' $administratorPassword --project $project | Out-Null

Write-Host ''
Write-Host 'Development secrets written to the user secret store.' -ForegroundColor Green
Write-Host 'Administrator e-mail    : administrator@itemauthoring.local'
Write-Host "Administrator password  : $administratorPassword"
Write-Host ''
Write-Host 'Record the password now; it is not stored anywhere else in plain text.' -ForegroundColor Yellow
