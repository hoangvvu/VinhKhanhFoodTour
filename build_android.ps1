# Build Script for VKFoodTour.Mobile APK
# This script will build a Release APK for Android

$ProjectFile = "VKFoodTour.Mobile/VKFoodTour.Mobile.csproj"
$OutputDirectory = "VKFoodTour.Mobile/bin/Release/net10.0-android/publish/"

Write-Host "--- Bắt đầu đóng gói APK cho .NET 10.0 Android ---" -ForegroundColor Cyan

dotnet publish $ProjectFile -f net10.0-android -c Release /p:AndroidPackageFormat=apk /p:AndroidKeyStore=false

if ($LASTEXITCODE -eq 0) {
    Write-Host "`n--- Đóng gói thành công! ---" -ForegroundColor Green
    Write-Host "File APK của bạn nằm tại:" -ForegroundColor White
    $apkFile = Get-ChildItem -Path $OutputDirectory -Filter "*.apk" | Select-Object -First 1
    if ($apkFile) {
        Write-Host $apkFile.FullName -ForegroundColor Yellow
    } else {
        Write-Host "Không tìm thấy file APK trong thư mục publish. Hãy kiểm tra lại output." -ForegroundColor Red
    }
} else {
    Write-Host "`n--- Build THẤT BẠI. Vui lòng kiểm tra các lỗi ở trên. ---" -ForegroundColor Red
}

Write-Host "`nNhấn phím bất kỳ để thoát..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
