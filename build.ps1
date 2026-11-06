param([string]$OutputDirectory = (Join-Path $PSScriptRoot 'artifacts\HyruleCompanion'))
$ErrorActionPreference = 'Stop'
$project = Join-Path $PSScriptRoot 'App\HyruleCompanion.csproj'
dotnet publish $project -c Release -o $OutputDirectory --self-contained false
if ($LASTEXITCODE -ne 0) { throw 'Build failed. Close the application before rebuilding.' }
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'assets') -Destination $OutputDirectory -Recurse -Force
foreach ($name in @('README.md','SOURCES.md','CATALOG-LICENSE.txt','VERIFICATION.md','REFONTE.md','TRAVEL.md','MAP-RESOURCE-LICENSE.txt','COMPATIBILITY.md')) {
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot $name) -Destination $OutputDirectory -Force
}
Write-Host "Built Hyrule Companion in $OutputDirectory"
