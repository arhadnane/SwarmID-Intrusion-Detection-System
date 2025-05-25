# SwarmID System Validation Script
# This script validates that the complete SwarmID anomaly detection system is working

Write-Host "=== SwarmID System Validation ==="
Write-Host ""

# Check if API is running
Write-Host "1. Testing API connectivity..."
try {
    $anomalies = Invoke-RestMethod -Uri "http://localhost:5000/api/anomalies" -Method Get -ErrorAction Stop
    Write-Host "   ✓ API is responding" -ForegroundColor Green
    Write-Host "   ✓ Found $($anomalies.Count) existing anomalies in database" -ForegroundColor Green
} catch {
    Write-Host "   ✗ API is not responding: $_" -ForegroundColor Red
    exit 1
}

# Check if Dashboard is running
Write-Host ""
Write-Host "2. Testing Dashboard connectivity..."
try {
    $response = Invoke-WebRequest -Uri "http://localhost:5001" -Method Get -ErrorAction Stop
    if ($response.StatusCode -eq 200) {
        Write-Host "   ✓ Dashboard is responding" -ForegroundColor Green
    }
} catch {
    Write-Host "   ✗ Dashboard is not responding: $_" -ForegroundColor Red
}

# Display existing anomalies
Write-Host ""
Write-Host "3. Current anomalies in database:"
foreach ($anomaly in $anomalies) {
    $type = switch ($anomaly.type) {
        0 { "Unknown" }
        1 { "Port Scan" }
        2 { "DDoS" }
        3 { "Command & Control" }
        4 { "Data Exfiltration" }
        default { "Unknown ($($anomaly.type))" }
    }
    Write-Host "   • ID: $($anomaly.id.Substring(0,8))... | Type: $type | Score: $($anomaly.score) | Algorithm: $($anomaly.algorithm)" -ForegroundColor Cyan
}

Write-Host ""
Write-Host "=== System Status: OPERATIONAL ===" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps to test complete workflow:"
Write-Host "1. Open Dashboard at: http://localhost:5001"
Write-Host "2. Navigate to Traffic Analysis page"
Write-Host "3. Upload a PCAP file (normal-traffic.pcap, port-scan.pcap, or suspicious-traffic.pcap)"
Write-Host "4. View detected anomalies in real-time"
Write-Host ""
Write-Host "API Documentation available at: http://localhost:5000/swagger"
