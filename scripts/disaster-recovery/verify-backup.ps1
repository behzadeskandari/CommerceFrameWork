param(
    [Parameter(Mandatory = $true)]
    [long]$BackupId,

    [string]$BaseUrl = "https://localhost:5100",
    [string]$Token = ""
)

$headers = @{}
if ($Token) {
    $headers["Authorization"] = "Bearer $Token"
}

Write-Host "Verifying backup $BackupId..."
$verify = Invoke-RestMethod -Method Post -Uri "$BaseUrl/api/admin/disaster-recovery/backups/$BackupId/verify" -Headers $headers
if (-not $verify.success) {
    Write-Error "Checksum verification failed."
    exit 1
}

Write-Host "Running recovery test..."
$test = Invoke-RestMethod -Method Post -Uri "$BaseUrl/api/admin/disaster-recovery/backups/$BackupId/recovery-test" -Headers $headers
if (-not $test.success) {
    Write-Error "Recovery test request failed."
    exit 1
}

if ($test.data.status -ne "Passed") {
    Write-Error "Recovery test did not pass. Backup is NOT valid for production recovery."
    Write-Host ($test | ConvertTo-Json -Depth 6)
    exit 1
}

Write-Host "Backup $BackupId passed recovery testing and is valid for recovery."
