# build-msix.ps1
# Builds, packages, signs, and installs NotifyLite as a proper MSIX app.
# Requires: .NET 8 SDK + Windows 10 SDK (standalone, no Visual Studio).
#
# Usage:  powershell -ExecutionPolicy Bypass -File Scripts\build-msix.ps1
#
# This is the EXACT same pipeline that Visual Studio uses internally:
#   1. dotnet publish (self-contained)
#   2. MakeAppx.exe pack (creates .msix)
#   3. SignTool.exe sign (signs with dev cert)
#   4. Add-AppxPackage (installs)

$ErrorActionPreference = "Stop"

$ProjectRoot = Split-Path -Parent $PSScriptRoot
$ProjectFile = Join-Path $ProjectRoot "NotifyLite\NotifyLite.csproj"
$MsixManifest = Join-Path $ProjectRoot "NotifyLite\MsixPackage\AppxManifest.xml"
$AssetsDir = Join-Path $ProjectRoot "NotifyLite\Assets"
$PublishDir = Join-Path $ProjectRoot "publish"
$StagingDir = Join-Path $ProjectRoot "msix-staging"
$OutputMsix = Join-Path $ProjectRoot "NotifyLite.msix"
$CertSubject = "CN=NotifyLiteDev"

# Locate Windows SDK tools
$makeAppx = Get-ChildItem "${env:ProgramFiles(x86)}\Windows Kits\10\bin\*\x64\makeappx.exe" -ErrorAction SilentlyContinue |
Sort-Object -Descending | Select-Object -First 1
$signTool = Get-ChildItem "${env:ProgramFiles(x86)}\Windows Kits\10\bin\*\x64\signtool.exe" -ErrorAction SilentlyContinue |
Sort-Object -Descending | Select-Object -First 1

if (-not $makeAppx -or -not $signTool) {
    Write-Host "ERROR: Windows SDK not found. Install from:" -ForegroundColor Red
    Write-Host "  https://developer.microsoft.com/en-us/windows/downloads/windows-sdk/" -ForegroundColor Yellow
    exit 1
}

Write-Host ""
Write-Host "  =============================" -ForegroundColor Magenta
Write-Host "   NotifyLite MSIX Builder" -ForegroundColor Magenta
Write-Host "  =============================" -ForegroundColor Magenta
Write-Host ""

# --- Step 1: Publish self-contained ---
Write-Host "[1/5] Publishing self-contained app..." -ForegroundColor Yellow
if (Test-Path $PublishDir) { Remove-Item $PublishDir -Recurse -Force }

dotnet publish $ProjectFile -c Release -r win-x64 --self-contained -o $PublishDir /p:DebugType=none /p:DebugSymbols=false 2>&1 | Out-Null
if ($LASTEXITCODE -ne 0) {
    Write-Host "  Publish failed!" -ForegroundColor Red
    exit 1
}
Write-Host "  Published to: $PublishDir" -ForegroundColor Green

# --- Step 2: Stage the MSIX contents ---
Write-Host "[2/5] Staging MSIX package..." -ForegroundColor Yellow
if (Test-Path $StagingDir) { Remove-Item $StagingDir -Recurse -Force }
New-Item -ItemType Directory -Path $StagingDir -Force | Out-Null

# Copy all published files (app + runtime)
Copy-Item "$PublishDir\*" $StagingDir -Recurse -Force

# Copy the AppxManifest
Copy-Item $MsixManifest (Join-Path $StagingDir "AppxManifest.xml") -Force

# Copy assets
$stageAssets = Join-Path $StagingDir "Assets"
New-Item -ItemType Directory -Path $stageAssets -Force | Out-Null
Copy-Item "$AssetsDir\*" $stageAssets -Force

Write-Host "  Staging complete" -ForegroundColor Green

# --- Step 3: Create MSIX with MakeAppx ---
Write-Host "[3/5] Creating MSIX package..." -ForegroundColor Yellow
if (Test-Path $OutputMsix) { Remove-Item $OutputMsix -Force }

& $makeAppx.FullName pack /d $StagingDir /p $OutputMsix /nv /o
if ($LASTEXITCODE -ne 0) {
    Write-Host "  MakeAppx failed!" -ForegroundColor Red
    exit 1
}
$msixSize = [math]::Round((Get-Item $OutputMsix).Length / 1MB, 1)
Write-Host "  Created: $OutputMsix ($msixSize MB)" -ForegroundColor Green

# --- Step 4: Sign the MSIX ---
Write-Host "[4/5] Signing package..." -ForegroundColor Yellow

# Find or create the dev certificate
$cert = Get-ChildItem Cert:\CurrentUser\My | Where-Object { $_.Subject -eq $CertSubject } | Select-Object -First 1
if (-not $cert) {
    Write-Host "  Creating self-signed certificate..." -ForegroundColor Yellow
    $cert = New-SelfSignedCertificate -Type Custom -Subject $CertSubject `
        -KeyUsage DigitalSignature -FriendlyName "NotifyLite Dev Certificate" `
        -CertStoreLocation "Cert:\CurrentUser\My" `
        -TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.3", "2.5.29.19={text}")
}

& $signTool.FullName sign /fd SHA256 /sha1 $cert.Thumbprint $OutputMsix
if ($LASTEXITCODE -ne 0) {
    Write-Host "  Signing failed!" -ForegroundColor Red
    exit 1
}
Write-Host "  Signed with cert: $($cert.Thumbprint)" -ForegroundColor Green

# Trust the certificate in LocalMachine\TrustedPeople (required for MSIX install)
$lmCert = Get-ChildItem Cert:\LocalMachine\TrustedPeople -ErrorAction SilentlyContinue | Where-Object { $_.Subject -eq $CertSubject }
if (-not $lmCert) {
    Write-Host "  Trusting certificate (admin prompt)..." -ForegroundColor Yellow
    $certPath = Join-Path $env:TEMP "NotifyLiteDev.cer"
    Export-Certificate -Cert $cert -FilePath $certPath -Force | Out-Null
    Start-Process -FilePath "certutil.exe" -ArgumentList "-addstore TrustedPeople `"$certPath`"" -Verb RunAs -Wait
    Remove-Item $certPath -ErrorAction SilentlyContinue
    Write-Host "  Certificate trusted" -ForegroundColor Green
}
else {
    Write-Host "  Certificate already trusted" -ForegroundColor Green
}

# --- Step 5: Install the MSIX ---
Write-Host "[5/5] Installing MSIX package..." -ForegroundColor Yellow

# Remove old version
Get-AppxPackage -Name "NotifyLite" -ErrorAction SilentlyContinue | Remove-AppxPackage -ErrorAction SilentlyContinue

Add-AppxPackage -Path $OutputMsix -ForceApplicationShutdown -ErrorAction Stop

$pkg = Get-AppxPackage -Name "NotifyLite"
if ($pkg) {
    Write-Host "  Installed: $($pkg.PackageFullName)" -ForegroundColor Green
    Write-Host "  Status: $($pkg.Status)" -ForegroundColor Green
}
else {
    Write-Host "  Installation failed!" -ForegroundColor Red
    exit 1
}

# --- Done! ---
Write-Host ""
Write-Host "  =============================" -ForegroundColor Green
Write-Host "   Build Complete!" -ForegroundColor Green
Write-Host "  =============================" -ForegroundColor Green
Write-Host ""
Write-Host "  Launch NotifyLite from the Start Menu," -ForegroundColor Cyan
Write-Host "  or run:" -ForegroundColor Cyan
Write-Host "    explorer.exe shell:AppsFolder\$($pkg.PackageFamilyName)!NotifyLiteApp" -ForegroundColor White
Write-Host ""
Write-Host "  Windows will ask for notification access" -ForegroundColor Cyan
Write-Host "  on first launch - click Allow." -ForegroundColor Cyan

# Clean up temp build folders
Remove-Item $StagingDir -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item $PublishDir -Recurse -Force -ErrorAction SilentlyContinue
