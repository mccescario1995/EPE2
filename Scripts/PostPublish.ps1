param(
    [string]$PublishDir
)

$ErrorActionPreference = "Stop"

# ==============================
# CONFIGURATION
# ==============================

$ProjectName = "EPEAPI"

# Where the ZIP files will be stored
$DeploymentFolder = "D:\Private\Sites\FUM Upload"

# How many ZIP backups to keep
$KeepBackups = 3

# ==============================
# PREPARE
# ==============================

Write-Host ""
Write-Host "========================================"
Write-Host "        EPEAPI DEPLOYMENT"
Write-Host "========================================"
Write-Host ""

Write-Host "Published folder:"
Write-Host $PublishDir

# Create deployment folder if it doesn't exist
if (!(Test-Path $DeploymentFolder)) {
    Write-Host "Creating deployment folder..."

    New-Item `
        -ItemType Directory `
        -Path $DeploymentFolder `
        -Force | Out-Null
}

# ==============================
# CREATE ZIP NAME
# ==============================

$Timestamp = Get-Date -Format "yyyyMMdd_HHmmss"

$ZipFileName = "${ProjectName}_${Timestamp}.zip"

$ZipPath = Join-Path `
    $DeploymentFolder `
    $ZipFileName

# ==============================
# COMPRESS PUBLISHED FILES
# ==============================

Write-Host ""
Write-Host "Creating ZIP..."
Write-Host $ZipPath

Compress-Archive `
    -Path "$PublishDir\*" `
    -DestinationPath $ZipPath `
    -Force

Write-Host ""
Write-Host "ZIP created successfully."

# ==============================
# CLEAN OLD BACKUPS
# ==============================

Write-Host ""
Write-Host "Cleaning old backups..."

$ZipFiles = Get-ChildItem `
    -Path $DeploymentFolder `
    -Filter "*.zip" `
    -File |
    Sort-Object LastWriteTime -Descending

if ($ZipFiles.Count -gt $KeepBackups) {

    $OldFiles = $ZipFiles | `
        Select-Object -Skip $KeepBackups

    foreach ($File in $OldFiles) {

        Write-Host "Deleting old backup:"
        Write-Host $File.FullName

        Remove-Item `
            $File.FullName `
            -Force
    }
}

# ==============================
# COMPLETE
# ==============================

Write-Host ""
Write-Host "========================================"
Write-Host "       DEPLOYMENT COMPLETE"
Write-Host "========================================"
Write-Host ""

Write-Host "ZIP:"
Write-Host $ZipPath

Write-Host ""