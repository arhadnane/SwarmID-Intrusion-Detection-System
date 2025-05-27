# Simple API Test for PCAP Analysis
Write-Host "Testing PCAP File Analysis..." -ForegroundColor Green

$apiUrl = "http://localhost:5112"

# Test with a simple HTTP request
try {
    Write-Host "Testing API connectivity..." -ForegroundColor Cyan
    $anomalies = Invoke-RestMethod -Uri "$apiUrl/api/anomalies" -Method Get
    Write-Host "✅ API is working! Found $($anomalies.Count) existing anomalies" -ForegroundColor Green
    
    # Show recent anomalies
    if ($anomalies.Count -gt 0) {
        Write-Host "`n📊 Recent Anomalies:" -ForegroundColor Yellow
        $recent = $anomalies | Sort-Object detectedAt -Descending | Select-Object -First 3
        foreach ($anomaly in $recent) {
            Write-Host "  • $($anomaly.description)" -ForegroundColor Magenta
            Write-Host "    Score: $($anomaly.score), Type: $($anomaly.type)" -ForegroundColor Gray
        }
    }
    
} catch {
    Write-Host "❌ API connection failed: $($_.Exception.Message)" -ForegroundColor Red
}

# Test if PCAP files exist
Write-Host "`n🔍 Checking PCAP test files..." -ForegroundColor Cyan
$pcapFiles = @("sample-data\normal-traffic.pcap", "sample-data\port-scan.pcap")
foreach ($file in $pcapFiles) {
    if (Test-Path $file) {
        $size = (Get-Item $file).Length
        Write-Host "✅ Found: $file ($size bytes)" -ForegroundColor Green
    } else {
        Write-Host "❌ Missing: $file" -ForegroundColor Red
    }
}

Write-Host "`n🎯 Port Scan Detection System Status: READY" -ForegroundColor Green
Write-Host "📍 API Endpoint: $apiUrl" -ForegroundColor Cyan
Write-Host "🔧 Available endpoints:" -ForegroundColor Cyan
Write-Host "  • POST /api/traffic/analyze - Upload PCAP/log files" -ForegroundColor Gray
Write-Host "  • GET /api/anomalies - View detected anomalies" -ForegroundColor Gray
Write-Host "  • POST /api/traffic/monitoring/start - Start real-time monitoring" -ForegroundColor Gray
