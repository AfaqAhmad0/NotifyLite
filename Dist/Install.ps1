# ============================================
#  NotifyLite Installer
#  Run this script as Administrator
# ============================================

Write-Host ""
Write-Host "  ================================" -ForegroundColor Cyan
Write-Host "    NotifyLite Installer" -ForegroundColor Cyan
Write-Host "  ================================" -ForegroundColor Cyan
Write-Host ""

# Check admin
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    Write-Host "  [!] Please run this script as Administrator!" -ForegroundColor Red
    Write-Host "      Right-click PowerShell -> Run as Administrator" -ForegroundColor Yellow
    Write-Host ""
    Read-Host "Press Enter to exit"
    exit 1
}

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$certFile = Join-Path $scriptDir "NotifyLite.cer"
$msixFile = Join-Path $scriptDir "NotifyLite.msix"

# Validate files exist
if (-not (Test-Path $certFile)) {
    Write-Host "  [X] NotifyLite.cer not found!" -ForegroundColor Red
    Read-Host "Press Enter to exit"
    exit 1
}
if (-not (Test-Path $msixFile)) {
    Write-Host "  [X] NotifyLite.msix not found!" -ForegroundColor Red
    Read-Host "Press Enter to exit"
    exit 1
}

# Step 1: Check Developer Mode
Write-Host "  [1/3] Checking Developer Mode..." -ForegroundColor Yellow
$devMode = Get-ItemProperty -Path "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\AppModelUnlock" -Name "AllowDevelopmentWithoutDevLicense" -ErrorAction SilentlyContinue
if (-not $devMode -or $devMode.AllowDevelopmentWithoutDevLicense -ne 1) {
    Write-Host ""
    Write-Host "  Developer Mode needs to be enabled!" -ForegroundColor Red
    Write-Host "  Windows 11: Settings > System > For developers" -ForegroundColor Yellow
    Write-Host "  Windows 10: Settings > Privacy & Security > For developers" -ForegroundColor Yellow
    Write-Host "  Turn ON 'Developer Mode'" -ForegroundColor Yellow
    Write-Host ""
    $proceed = Read-Host "  Have you enabled Developer Mode? (y/n)"
    if ($proceed -ne "y") {
        Write-Host "  Please enable Developer Mode and run this script again." -ForegroundColor Yellow
        Read-Host "Press Enter to exit"
        exit 1
    }
}
Write-Host "  [OK] Developer Mode" -ForegroundColor Green

# Step 2: Trust the certificate
Write-Host "  [2/3] Installing certificate..." -ForegroundColor Yellow
try {
    certutil -addstore TrustedPeople "$certFile" | Out-Null
    Write-Host "  [OK] Certificate trusted" -ForegroundColor Green
}
catch {
    Write-Host "  [!] Certificate install failed: $_" -ForegroundColor Red
    Read-Host "Press Enter to exit"
    exit 1
}

# Step 3: Install the app
Write-Host "  [3/3] Installing NotifyLite..." -ForegroundColor Yellow
try {
    # Remove old version if exists
    $existing = Get-AppxPackage -Name "NotifyLite" -ErrorAction SilentlyContinue
    if ($existing) {
        Write-Host "        Removing previous version..." -ForegroundColor Gray
        Remove-AppxPackage $existing.PackageFullName -ErrorAction SilentlyContinue
    }

    Add-AppxPackage -Path "$msixFile"
    Write-Host "  [OK] NotifyLite installed!" -ForegroundColor Green
}
catch {
    Write-Host "  [!] Installation failed: $_" -ForegroundColor Red
    Read-Host "Press Enter to exit"
    exit 1
}

# Done!
Write-Host ""
Write-Host "  ================================" -ForegroundColor Green
Write-Host "    Installation Complete!" -ForegroundColor Green
Write-Host "  ================================" -ForegroundColor Green
Write-Host ""
Write-Host "  NotifyLite is now installed." -ForegroundColor Cyan
Write-Host "  Launch it from the Start Menu." -ForegroundColor Cyan
Write-Host ""
Write-Host "  On first launch:" -ForegroundColor Yellow
Write-Host "  - Windows will ask for notification access" -ForegroundColor Yellow
Write-Host "  - Click 'Allow' to enable notification interception" -ForegroundColor Yellow
Write-Host "  - The app runs in the system tray (purple N icon)" -ForegroundColor Yellow
Write-Host ""

# Offer to launch now
$launch = Read-Host "  Launch NotifyLite now? (y/n)"
if ($launch -eq "y") {
    $pkg = Get-AppxPackage -Name "NotifyLite"
    if ($pkg) {
        explorer.exe "shell:AppsFolder\$($pkg.PackageFamilyName)!NotifyLiteApp"
        Write-Host "  Launched!" -ForegroundColor Green
    }
}

Write-Host ""
Read-Host "Press Enter to close"
