# Simple Port Scan Detection Test
Write-Host "Testing SwarmID Port Scan Detection System..." -ForegroundColor Green

$apiUrl = "http://localhost:5112"

# Function to test PCAP file analysis
function Test-PcapFile {
    param(
        [string]$FilePath,
        [string]$TestName
    )
    
    Write-Host "`nTesting: $TestName" -ForegroundColor Yellow
    Write-Host "File: $FilePath" -ForegroundColor Cyan
    
    if (-not (Test-Path $FilePath)) {
        Write-Host "ERROR: File not found: $FilePath" -ForegroundColor Red
        return
    }
    
    try {
        # Upload and analyze the PCAP file
        $uploadUrl = "$apiUrl/api/traffic/upload-pcap"
        $boundary = [System.Guid]::NewGuid().ToString()
        $fileName = Split-Path $FilePath -Leaf
        
        # Read file content
        $fileBytes = [System.IO.File]::ReadAllBytes($FilePath)
        $fileContent = [System.Text.Encoding]::GetEncoding("iso-8859-1").GetString($fileBytes)
        
        # Create multipart form data
        $bodyLines = @(
            "--$boundary",
            "Content-Disposition: form-data; name=`"file`"; filename=`"$fileName`"",
            "Content-Type: application/octet-stream",
            "",
            $fileContent,
            "--$boundary--"
        )
        $body = $bodyLines -join "`r`n"
        
        # Make the request
        $headers = @{
            "Content-Type" = "multipart/form-data; boundary=$boundary"
        }
        
        Write-Host "Uploading PCAP file..." -ForegroundColor Gray
        $response = Invoke-RestMethod -Uri $uploadUrl -Method Post -Body $body -Headers $headers
        
        Write-Host "Analysis Result:" -ForegroundColor Green
        Write-Host "- Records Analyzed: $($response.totalRecords)" -ForegroundColor White
        Write-Host "- Anomalies Found: $($response.anomaliesDetected)" -ForegroundColor White
        
        if ($response.anomalies -and $response.anomalies.Count -gt 0) {
            Write-Host "- Anomaly Types:" -ForegroundColor White
            foreach ($anomaly in $response.anomalies) {
                Write-Host "  * $($anomaly.type): $($anomaly.description)" -ForegroundColor Magenta
                Write-Host "    Confidence: $($anomaly.confidence)" -ForegroundColor Gray
            }
        } else {
            Write-Host "- No specific anomalies detected" -ForegroundColor Gray
        }
        
        return $response
        
    } catch {
        Write-Host "ERROR during test: $($_.Exception.Message)" -ForegroundColor Red
        if ($_.Exception.Response) {
            $reader = [System.IO.StreamReader]::new($_.Exception.Response.GetResponseStream())
            $responseBody = $reader.ReadToEnd()
            Write-Host "Response: $responseBody" -ForegroundColor Red
        }
    }
}

# Test each PCAP file
$testFiles = @(
    @{ Path = "sample-data\normal-traffic.pcap"; Name = "Normal Traffic (should have few/no anomalies)" },
    @{ Path = "sample-data\port-scan.pcap"; Name = "Port Scan Traffic (should detect port scan)" },
    @{ Path = "sample-data\suspicious-traffic.pcap"; Name = "Suspicious Traffic (should detect various anomalies)" }
)

foreach ($test in $testFiles) {
    Test-PcapFile -FilePath $test.Path -TestName $test.Name
    Start-Sleep -Seconds 1
}

Write-Host "`n=== Port Scan Detection Test Summary ===" -ForegroundColor Green
Write-Host "1. Normal traffic should show minimal anomalies" -ForegroundColor White
Write-Host "2. Port scan traffic should clearly identify port scanning behavior" -ForegroundColor White
Write-Host "3. Suspicious traffic should detect various network anomalies" -ForegroundColor White
Write-Host "`nTest completed!" -ForegroundColor Green
