# PowerShell script to add test anomalies via API
$baseUrl = "http://localhost:5112/api"

Write-Host "Adding test anomalies to demonstrate pagination..."

$anomalyTypes = @("Port Scan", "DDoS Attack", "Suspicious Traffic", "Malware Communication", "Data Exfiltration")
$severities = @("Low", "Medium", "High", "Critical")
$protocols = @("TCP", "UDP", "ICMP")

for ($i = 1; $i -le 20; $i++) {
    $anomaly = @{
        Id = [guid]::NewGuid().ToString()
        SourceIp = "192.168.1.$([System.Random]::new().Next(1, 255))"
        DestinationIp = "10.0.0.$([System.Random]::new().Next(1, 255))"
        SourcePort = [System.Random]::new().Next(1024, 65535)
        DestinationPort = [System.Random]::new().Next(1, 1024)
        Protocol = $protocols[[System.Random]::new().Next(0, 3)]
        AnomalyType = $anomalyTypes[[System.Random]::new().Next(0, 5)]
        Severity = $severities[[System.Random]::new().Next(0, 4)]
        DetectedAt = (Get-Date).AddMinutes(-[System.Random]::new().Next(1, 1440)).ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
        Description = "Test anomaly #$i - Automated detection for pagination testing"
        SwarmScore = [Math]::Round([System.Random]::new().NextDouble() * 100, 2)
        PacketCount = [System.Random]::new().Next(10, 10000)
        ByteCount = [System.Random]::new().Next(1024, 1048576)
    }
    
    try {
        $json = $anomaly | ConvertTo-Json -Depth 3
        $response = Invoke-RestMethod -Uri "$baseUrl/anomalies" -Method POST -Body $json -ContentType "application/json"
        Write-Host "✓ Added anomaly #$i" -ForegroundColor Green
    }
    catch {
        Write-Host "✗ Failed to add anomaly #$i: $($_.Exception.Message)" -ForegroundColor Red
    }
}

Write-Host "`nTest data generation complete!"

# Test the pagination
Write-Host "`nTesting pagination:"
$paginatedResult = Invoke-RestMethod -Uri "$baseUrl/anomalies/paged?page=1&pageSize=10"
Write-Host "Total anomalies: $($paginatedResult.pagination.totalCount)"
Write-Host "Total pages (size 10): $($paginatedResult.pagination.totalPages)"
