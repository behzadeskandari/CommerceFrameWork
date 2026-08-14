param(
    [string]$ResultsDir = "$PSScriptRoot/../../artifacts/test-results/phase-50",
    [switch]$IntegrationOnly
)

$ErrorActionPreference = "Continue"
$root = Resolve-Path (Join-Path $PSScriptRoot "../..")
New-Item -ItemType Directory -Force -Path $ResultsDir | Out-Null

$testProjects = @(
    @{ Name = "Unit"; Path = "tests/Commerce/Commerce.Tests.Unit/Commerce.Tests.Unit.csproj" },
    @{ Name = "Unit.Cache"; Path = "tests/Commerce/Commerce.Tests.Unit.Cache/Commerce.Tests.Unit.Cache.csproj" },
    @{ Name = "Unit.Audit"; Path = "tests/Commerce/Commerce.Tests.Unit.Audit/Commerce.Tests.Unit.Audit.csproj" },
    @{ Name = "Unit.Observability"; Path = "tests/Commerce/Commerce.Tests.Unit.Observability/Commerce.Tests.Unit.Observability.csproj" },
    @{ Name = "Unit.Analytics"; Path = "tests/Commerce/Commerce.Tests.Unit.Analytics/Commerce.Tests.Unit.Analytics.csproj" },
    @{ Name = "Unit.Deployment"; Path = "tests/Commerce/Commerce.Tests.Unit.Deployment/Commerce.Tests.Unit.Deployment.csproj" },
    @{ Name = "Unit.DisasterRecovery"; Path = "tests/Commerce/Commerce.Tests.Unit.DisasterRecovery/Commerce.Tests.Unit.DisasterRecovery.csproj" },
    @{ Name = "Unit.Integration"; Path = "tests/Commerce/Commerce.Tests.Unit.Integration/Commerce.Tests.Unit.Integration.csproj" },
    @{ Name = "Unit.Notifications"; Path = "tests/Commerce/Commerce.Tests.Unit.Notifications/Commerce.Tests.Unit.Notifications.csproj" },
    @{ Name = "Unit.PaymentProviders"; Path = "tests/Commerce/Commerce.Tests.Unit.PaymentProviders/Commerce.Tests.Unit.PaymentProviders.csproj" },
    @{ Name = "Unit.PromotionsSeo"; Path = "tests/Commerce/Commerce.Tests.Unit.PromotionsSeo/Commerce.Tests.Unit.PromotionsSeo.csproj" },
    @{ Name = "Unit.Scheduling"; Path = "tests/Commerce/Commerce.Tests.Unit.Scheduling/Commerce.Tests.Unit.Scheduling.csproj" },
    @{ Name = "Unit.SmartstoreImport"; Path = "tests/Commerce/Commerce.Tests.Unit.SmartstoreImport/Commerce.Tests.Unit.SmartstoreImport.csproj" },
    @{ Name = "Plugin.Sdk"; Path = "tests/Commerce/Commerce.Tests.Plugin.Sdk/Commerce.Tests.Plugin.Sdk.csproj" },
    @{ Name = "Plugins"; Path = "tests/Commerce/Commerce.Tests.Plugins/Commerce.Tests.Plugins.csproj" },
    @{ Name = "Architecture"; Path = "tests/Commerce/Commerce.Tests.Architecture/Commerce.Tests.Architecture.csproj" },
    @{ Name = "Integration"; Path = "tests/Commerce/Commerce.Tests.Integration/Commerce.Tests.Integration.csproj" }
)

if ($IntegrationOnly) {
    $testProjects = $testProjects | Where-Object { $_.Name -eq "Integration" }
}

$summary = @()
Push-Location $root
try {
    foreach ($project in $testProjects) {
        $fullPath = Join-Path $root $project.Path
        if (-not (Test-Path $fullPath)) {
            Write-Warning "Skipping missing project: $($project.Path)"
            $summary += [pscustomobject]@{ Project = $project.Name; Passed = 0; Failed = 0; Skipped = 0; ExitCode = -1; Status = "Missing" }
            continue
        }

        $trx = Join-Path $ResultsDir "$($project.Name).trx"
        Write-Host "`n=== Running $($project.Name) ===" -ForegroundColor Cyan

        dotnet test $fullPath `
            --configuration Release `
            --logger "trx;LogFileName=$trx" `
            --logger "console;verbosity=minimal" `
            --results-directory $ResultsDir

        $exit = $LASTEXITCODE
        $status = if ($exit -eq 0) { "Pass" } else { "Fail" }
        $summary += [pscustomobject]@{ Project = $project.Name; ExitCode = $exit; Status = $status }
    }
}
finally {
    Pop-Location
}

$summaryPath = Join-Path $ResultsDir "summary.json"
$summary | ConvertTo-Json | Set-Content $summaryPath

Write-Host "`n=== Phase 50 Verification Summary ===" -ForegroundColor Yellow
$summary | Format-Table -AutoSize

$failed = @($summary | Where-Object { $_.Status -eq "Fail" })
if ($failed.Count -gt 0) {
    Write-Host "FAILED projects: $($failed.Project -join ', ')" -ForegroundColor Red
    exit 1
}

$missing = @($summary | Where-Object { $_.Status -eq "Missing" })
if ($missing.Count -gt 0) {
    Write-Host "MISSING projects: $($missing.Project -join ', ')" -ForegroundColor Yellow
}

Write-Host "Results: $ResultsDir"
exit 0
