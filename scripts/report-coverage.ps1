<#
.SYNOPSIS
    Summarises backend line coverage and, optionally, fails when it falls below a threshold.

.DESCRIPTION
    Reads every Cobertura report produced by `dotnet test --settings coverlet.runsettings` and reports
    line coverage per assembly and overall.

    The threshold is only meaningful on a machine with a container runtime: without one the
    integration suite is skipped, and the API and infrastructure layers it covers report as untested.
#>
[CmdletBinding()]
param(
    [double]$MinimumPercentage = 0
)

$ErrorActionPreference = 'Stop'
$root = Join-Path $PSScriptRoot '..\artifacts\coverage'

$totals = @{}
Get-ChildItem $root -Recurse -Filter coverage.cobertura.xml | ForEach-Object {
    [xml]$report = Get-Content $_.FullName
    foreach ($package in $report.coverage.packages.package) {
        foreach ($class in $package.classes.class) {
            $assembly = $package.name
            if (-not $totals.ContainsKey($assembly)) {
                $totals[$assembly] = @{ Covered = 0; Total = 0 }
            }
            foreach ($line in $class.lines.line) {
                $totals[$assembly].Total++
                if ([int]$line.hits -gt 0) { $totals[$assembly].Covered++ }
            }
        }
    }
}

$grandCovered = 0
$grandTotal = 0
$totals.GetEnumerator() | Sort-Object Name | ForEach-Object {
    $covered = $_.Value.Covered
    $total = $_.Value.Total
    $grandCovered += $covered
    $grandTotal += $total
    if ($total -gt 0) {
        '{0,-42} {1,6:P1} ({2}/{3})' -f $_.Key, ($covered / $total), $covered, $total
    }
}

if ($grandTotal -gt 0) {
    ''
    '{0,-42} {1,6:P1} ({2}/{3})' -f 'TOTAL', ($grandCovered / $grandTotal), $grandCovered, $grandTotal

    $percentage = 100 * $grandCovered / $grandTotal
    if ($MinimumPercentage -gt 0 -and $percentage -lt $MinimumPercentage) {
        throw ('Line coverage is {0:N1}%, below the required {1:N1}%.' -f $percentage, $MinimumPercentage)
    }
}
else {
    throw 'No coverage reports were found. Run dotnet test with --settings coverlet.runsettings first.'
}
