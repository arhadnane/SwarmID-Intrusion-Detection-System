# Test PCAP Analysis Script
# This script uploads PCAP files to the SwarmID API and analyzes the results

$apiUrl = "http://localhost:5112/api/traffic/analyze"
$sampleDataPath = "d:\Coding\VSCodeProject\Anomaly-Based Intrusion Detection System Using Swarm Intelligence\sample-data"

# Function to test PCAP file analysis
function Test-PcapFile {
    param(
        [string]$FilePath,
        [string]$FileName
    )
    
    Write-Host "`n=== Testing $FileName ===" -ForegroundColor Green
    
    try {
        # Create multipart form data
        $boundary = [System.Guid]::NewGuid().ToString()
        $LF = "`r`n"
        
        # Read file content
        $fileBytes = [System.IO.File]::ReadAllBytes($FilePath)
        $fileContent = [System.Text.Encoding]::GetEncoding('iso-8859-1').GetString($fileBytes)
        
        # Create form data
        $bodyLines = (
            "--$boundary",
            "Content-Disposition: form-data; name=`"file`"; filename=`"$FileName`"",
            "Content-Type: application/octet-stream$LF",
            $fileContent,
            "--$boundary--$LF"
        ) -join $LF
        
        # Make the request
        $response = Invoke-RestMethod -Uri "$apiUrl?dataType=PcapFile" -Method Post -Body $bodyLines -ContentType "multipart/form-data; boundary=$boundary"
        
        Write-Host "Analysis Result:" -ForegroundColor Yellow
        Write-Host "  Total Records Processed: $($response.totalRecordsProcessed)" -ForegroundColor Cyan
        Write-Host "  Anomalies Detected: $($response.anomaliesDetected)" -ForegroundColor Cyan
        
        if ($response.anomalies -and $response.anomalies.Count -gt 0) {
            Write-Host "  Detected Anomalies:" -ForegroundColor Yellow
            foreach ($anomaly in $response.anomalies) {
                Write-Host "    - Type: $($anomaly.type), Score: $($anomaly.score)" -ForegroundColor White
                Write-Host "      Description: $($anomaly.description)" -ForegroundColor Gray
                
                # Check if IP addresses are properly extracted
                if ($anomaly.sourceIPs -and $anomaly.sourceIPs.Count -gt 0) {
                    Write-Host "      Source IPs: $($anomaly.sourceIPs -join ', ')" -ForegroundColor Green
                } else {
                    Write-Host "      Source IPs: N/A (No IPs extracted)" -ForegroundColor Red
                }
                
                if ($anomaly.destinationIPs -and $anomaly.destinationIPs.Count -gt 0) {
                    Write-Host "      Destination IPs: $($anomaly.destinationIPs -join ', ')" -ForegroundColor Green
                } else {
                    Write-Host "      Destination IPs: N/A (No IPs extracted)" -ForegroundColor Red
                }
                
                if ($anomaly.ports -and $anomaly.ports.Count -gt 0) {
                    Write-Host "      Ports: $($anomaly.ports -join ', ')" -ForegroundColor Green
                } else {
                    Write-Host "      Ports: N/A (No ports extracted)" -ForegroundColor Red
                }
            }
        } else {
            Write-Host "  No anomalies detected (this may be normal for baseline traffic)" -ForegroundColor Yellow
        }
        
        return $true
    }
    catch {
        Write-Host "Error testing $FileName`: $($_.Exception.Message)" -ForegroundColor Red
        return $false
    }
}

# Test all PCAP files
Write-Host "Starting PCAP Analysis Test..." -ForegroundColor Green
Write-Host "API Endpoint: $apiUrl" -ForegroundColor Gray

$testResults = @{}

# Test each PCAP file
$pcapFiles = @(
    @{ Name = "normal-traffic.pcap"; Expected = "0-1 anomalies (baseline traffic)" },
    @{ Name = "port-scan.pcap"; Expected = "3-5 anomalies (port scanning)" },
    @{ Name = "suspicious-traffic.pcap"; Expected = "2-4 anomalies (DDoS patterns)" }
)

foreach ($pcapFile in $pcapFiles) {
    $filePath = Join-Path $sampleDataPath $pcapFile.Name
    if (Test-Path $filePath) {
        Write-Host "`nExpected: $($pcapFile.Expected)" -ForegroundColor Gray
        $testResults[$pcapFile.Name] = Test-PcapFile -FilePath $filePath -FileName $pcapFile.Name
    } else {
        Write-Host "File not found: $filePath" -ForegroundColor Red
        $testResults[$pcapFile.Name] = $false
    }
}

# Summary
Write-Host "`n=== Test Summary ===" -ForegroundColor Green
foreach ($result in $testResults.GetEnumerator()) {
    $status = if ($result.Value) { "PASSED" } else { "FAILED" }
    $color = if ($result.Value) { "Green" } else { "Red" }
    Write-Host "  $($result.Key): $status" -ForegroundColor $color
}

Write-Host "`nTest completed. Check the results above to see if IP addresses are being extracted correctly." -ForegroundColor Yellow
