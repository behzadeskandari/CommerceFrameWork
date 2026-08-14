param(
    [Parameter(Mandatory = $true)]
    [string]$SqlFile,

    [int]$ImportRunId = 0
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$testProject = Join-Path $repoRoot "tests/Commerce/Commerce.Tests.Unit.SmartstoreImport/Commerce.Tests.Unit.SmartstoreImport.csproj"

if (-not (Test-Path $SqlFile)) {
    Write-Error "SQL file not found: $SqlFile"
}

Write-Host "Smartstore reconciliation tooling (Phase 47)"
Write-Host "SQL file: $SqlFile"

dotnet test $testProject --filter "FullyQualifiedName~SmartstoreReconciliation" --nologo
exit $LASTEXITCODE
