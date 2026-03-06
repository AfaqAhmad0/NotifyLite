# ============================================
#  NotifyLite Uninstaller
# ============================================

Write-Host ""
Write-Host "  Uninstalling NotifyLite..." -ForegroundColor Yellow

$pkg = Get-AppxPackage -Name "NotifyLite" -ErrorAction SilentlyContinue
if ($pkg) {
    Remove-AppxPackage $pkg.PackageFullName
    Write-Host "  [OK] NotifyLite uninstalled." -ForegroundColor Green
}
else {
    Write-Host "  NotifyLite is not installed." -ForegroundColor Gray
}

Write-Host ""
Read-Host "Press Enter to close"
