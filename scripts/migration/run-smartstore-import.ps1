param(
    [Parameter(Mandatory = $true)]
    [string]$SqlFile,

    [switch]$InspectOnly,
    [switch]$AllowDuplicateRun,
    [switch]$StopOnFirstError
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$project = Join-Path $repoRoot "src/Commerce/Modules/SmartstoreImport/Commerce.SmartstoreImport.Infrastructure/Commerce.SmartstoreImport.Infrastructure.csproj"

if (-not (Test-Path $SqlFile)) {
    Write-Error "SQL file not found: $SqlFile"
}

Write-Host "Smartstore import tooling (Phase 46)"
Write-Host "SQL file: $SqlFile"

if ($InspectOnly) {
    dotnet test (Join-Path $repoRoot "tests/Commerce/Commerce.Tests.Unit.SmartstoreImport/Commerce.Tests.Unit.SmartstoreImport.csproj") --filter "FullyQualifiedName~InspectSchema" --nologo
    exit $LASTEXITCODE
}

Write-Host "Run import through Commerce Host admin API or integration tests once host build is green."
Write-Host "For local verification, execute: dotnet test tests/Commerce/Commerce.Tests.Unit.SmartstoreImport"
dotnet test (Join-Path $repoRoot "tests/Commerce/Commerce.Tests.Unit.SmartstoreImport/Commerce.Tests.Unit.SmartstoreImport.csproj") --nologo
exit $LASTEXITCODE
