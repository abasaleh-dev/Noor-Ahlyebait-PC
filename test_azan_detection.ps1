# Test script to verify Azan file detection
Write-Host "=== Azan File Detection Test ===" -ForegroundColor Green

$azanFolder = "NoorAhlulBayt.Companion\Azan"
Write-Host "Checking Azan folder: $azanFolder" -ForegroundColor Yellow

if (Test-Path $azanFolder) {
    Write-Host "✅ Azan folder exists" -ForegroundColor Green
    
    Write-Host "`nFiles in Azan folder:" -ForegroundColor Yellow
    Get-ChildItem $azanFolder -File | ForEach-Object {
        Write-Host "  📄 $($_.Name)" -ForegroundColor Cyan
    }
    
    Write-Host "`nLooking for 'yet_another_adhan_by_mishary_rashid_alafasy' files:" -ForegroundColor Yellow
    $searchPattern = "*yet_another_adhan_by_mishary_rashid_alafasy*"
    $matchingFiles = Get-ChildItem $azanFolder -File -Name $searchPattern
    
    if ($matchingFiles) {
        Write-Host "✅ Found matching files:" -ForegroundColor Green
        $matchingFiles | ForEach-Object {
            Write-Host "  🎵 $_" -ForegroundColor Green
        }
    } else {
        Write-Host "❌ No matching files found for pattern: $searchPattern" -ForegroundColor Red
    }
    
    Write-Host "`nTesting file access:" -ForegroundColor Yellow
    $testFile = Join-Path $azanFolder "yet_another_adhan_by_mishary_rashid_alafasy.wav"
    if (Test-Path $testFile) {
        $fileInfo = Get-Item $testFile
        Write-Host "✅ File accessible: $($fileInfo.Name)" -ForegroundColor Green
        Write-Host "   Size: $([math]::Round($fileInfo.Length / 1MB, 2)) MB" -ForegroundColor Cyan
        Write-Host "   Full path: $($fileInfo.FullName)" -ForegroundColor Cyan
    } else {
        Write-Host "❌ File not accessible: $testFile" -ForegroundColor Red
    }
    
} else {
    Write-Host "❌ Azan folder not found: $azanFolder" -ForegroundColor Red
}

Write-Host "`n=== Test Complete ===" -ForegroundColor Green
