param(
    [string]$ComposeFile = "$PSScriptRoot/../../deploy/docker/docker-compose.yml",
    [string]$EnvFile = "$PSScriptRoot/../../deploy/docker/.env",
    [int]$TimeoutSeconds = 600
)

$ErrorActionPreference = "Stop"
$root = Resolve-Path (Join-Path $PSScriptRoot "../..")

if (-not (Test-Path $EnvFile)) {
    Copy-Item (Join-Path (Split-Path $EnvFile) ".env.example") $EnvFile
    Write-Host "Created $EnvFile from .env.example"
}

function Wait-HttpOk {
    param([string]$Url, [int]$TimeoutSec)
    $deadline = (Get-Date).AddSeconds($TimeoutSec)
    while ((Get-Date) -lt $deadline) {
        try {
            $response = Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec 5
            if ($response.StatusCode -ge 200 -and $response.StatusCode -lt 300) {
                return $true
            }
        }
        catch {
            Start-Sleep -Seconds 3
        }
    }
    return $false
}

Push-Location $root
try {
    Write-Host "Tearing down previous stack..."
    docker compose -f $ComposeFile --env-file $EnvFile down -v --remove-orphans | Out-Null

    Write-Host "Building and starting stack..."
    docker compose -f $ComposeFile --env-file $EnvFile up -d --build
    if ($LASTEXITCODE -ne 0) { throw "docker compose up failed" }

    $baseUrl = (Get-Content $EnvFile | Where-Object { $_ -match '^COMMERCE_BASE_URL=' }) -replace '^COMMERCE_BASE_URL=', ''
    if ([string]::IsNullOrWhiteSpace($baseUrl)) { $baseUrl = "http://localhost:8080" }

    Write-Host "Waiting for liveness at $baseUrl/health/live ..."
    if (-not (Wait-HttpOk "$baseUrl/health/live" $TimeoutSeconds)) {
        throw "Commerce host did not become live within ${TimeoutSeconds}s"
    }

    Write-Host "Running installation bootstrap..."
    $envVars = @{}
    Get-Content $EnvFile | ForEach-Object {
        if ($_ -match '^\s*([^#=]+)=(.*)$') {
            $envVars[$matches[1].Trim()] = $matches[2].Trim()
        }
    }

    $adminEmail = $envVars["COMMERCE_ADMIN_EMAIL"]
    $adminUser = $envVars["COMMERCE_ADMIN_USERNAME"]
    $adminPassword = $envVars["COMMERCE_ADMIN_PASSWORD"]
    $storeName = $envVars["COMMERCE_STORE_NAME"]
    $storeHost = $envVars["COMMERCE_STORE_HOST"]

    if ([string]::IsNullOrWhiteSpace($adminEmail)) { throw "COMMERCE_ADMIN_EMAIL missing in .env" }

    $password = $envVars["MSSQL_SA_PASSWORD"]
    if ([string]::IsNullOrWhiteSpace($password)) { throw "MSSQL_SA_PASSWORD missing in .env" }

    $connectionString = "Server=sqlserver,1433;Database=Commerce;User Id=sa;Password=$password;TrustServerCertificate=True;Encrypt=True;"

    Invoke-RestMethod -Method Post -Uri "$baseUrl/installation/requirements" | Out-Null

    Invoke-RestMethod -Method Post -Uri "$baseUrl/installation/database" -ContentType "application/json" -Body (@{
        provider = "SqlServer"
        connectionString = $connectionString
    } | ConvertTo-Json) | Out-Null

    Invoke-RestMethod -Method Post -Uri "$baseUrl/installation/migrate" | Out-Null
    Invoke-RestMethod -Method Post -Uri "$baseUrl/installation/seed" | Out-Null

    Invoke-RestMethod -Method Post -Uri "$baseUrl/installation/admin" -ContentType "application/json" -Body (@{
        email = $adminEmail
        username = $adminUser
        password = $adminPassword
    } | ConvertTo-Json) | Out-Null

    Invoke-RestMethod -Method Post -Uri "$baseUrl/installation/store" -ContentType "application/json" -Body (@{
        name = $storeName
        url = $baseUrl
        hosts = $storeHost
    } | ConvertTo-Json) | Out-Null

    Invoke-RestMethod -Method Post -Uri "$baseUrl/installation/language" -ContentType "application/json" -Body (@{
        name = "English"
        culture = "en-US"
        rtl = $false
        isDefault = $true
    } | ConvertTo-Json) | Out-Null

    Invoke-RestMethod -Method Post -Uri "$baseUrl/installation/currency" -ContentType "application/json" -Body (@{
        name = "US Dollar"
        currencyCode = "USD"
        rate = 1
        isPrimary = $true
    } | ConvertTo-Json) | Out-Null

    Invoke-RestMethod -Method Post -Uri "$baseUrl/installation/complete" | Out-Null

    Write-Host "Verifying readiness..."
    $ready = Invoke-RestMethod -Uri "$baseUrl/health/ready"
    if ($ready.status -ne "Healthy") {
        Write-Warning "Readiness status: $($ready.status) - review /health/ready entries"
    }
    else {
        Write-Host "Clean installation succeeded. Status: Healthy"
    }

    $rootResponse = Invoke-RestMethod -Uri $baseUrl
    if ($rootResponse.status -ne "installed") {
        throw "Expected installed status at / but got $($rootResponse | ConvertTo-Json -Compress)"
    }

    Write-Host "PASS - clean Docker installation verified."
}
finally {
    Pop-Location
}
