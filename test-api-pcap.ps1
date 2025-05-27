# Test Port Scan Detection with API
Write-Host "Testing SwarmID Port Scan Detection..." -ForegroundColor Green

$apiUrl = "http://localhost:5112"

# Function to test PCAP file analysis using correct endpoint
function Test-PcapAnalysis {
    param(
        [string]$FilePath,
        [string]$TestName
    )
    
    Write-Host "`n==== Testing: $TestName ====" -ForegroundColor Yellow
    Write-Host "File: $FilePath" -ForegroundColor Cyan
    
    if (-not (Test-Path $FilePath)) {
        Write-Host "ERROR: File not found: $FilePath" -ForegroundColor Red
        return
    }
    
    try {
        # Prepare the multipart form data
        $boundary = [System.Guid]::NewGuid().ToString()
        $fileName = Split-Path $FilePath -Leaf
        
        # Read file as bytes
        $fileBytes = [System.IO.File]::ReadAllBytes($FilePath)
        
        # Create multipart body
        $body = @"
--$boundary
Content-Disposition: form-data; name="file"; filename="$fileName"
Content-Type: application/octet-stream

"@
        $bodyBytes = [System.Text.Encoding]::UTF8.GetBytes($body)
        $bodyBytes += $fileBytes
        $bodyBytes += [System.Text.Encoding]::UTF8.GetBytes("`r`n--$boundary--`r`n")
        
        # Make the request
        $uri = "$apiUrl/api/traffic/analyze?dataType=PcapFile"
        
        Write-Host "Uploading and analyzing PCAP file..." -ForegroundColor Gray
        
        $response = Invoke-RestMethod -Uri $uri -Method Post -Body $bodyBytes -ContentType "multipart/form-data; boundary=$boundary"
        
        Write-Host "✅ Analysis completed successfully!" -ForegroundColor Green
        Write-Host "📊 Results:" -ForegroundColor Cyan
        Write-Host "  - Records Analyzed: $($response.totalRecordsProcessed)" -ForegroundColor White
        Write-Host "  - Anomalies Detected: $($response.anomaliesDetected)" -ForegroundColor White
        
        if ($response.anomalies -and $response.anomalies.Count -gt 0) {
            Write-Host "🚨 Detected Anomalies:" -ForegroundColor Red
            $portScanCount = 0
            foreach ($anomaly in $response.anomalies) {
                Write-Host "  • Type: $($anomaly.type)" -ForegroundColor Magenta
                Write-Host "    Description: $($anomaly.description)" -ForegroundColor Gray
                Write-Host "    Confidence: $($anomaly.confidence)" -ForegroundColor Gray
                Write-Host "    Source: $($anomaly.sourceIP):$($anomaly.sourcePort) → $($anomaly.destinationIP):$($anomaly.destinationPort)" -ForegroundColor Yellow
                Write-Host ""
                
                if ($anomaly.type -like "*Port*" -or $anomaly.type -like "*Scan*") {
                    $portScanCount++
                }
            }
            
            if ($portScanCount -gt 0) {
                Write-Host "🎯 PORT SCAN DETECTION: SUCCESS! ($portScanCount scan(s) detected)" -ForegroundColor Green
            } else {
                Write-Host "ℹ️  Other anomalies detected, but no specific port scans identified" -ForegroundColor Yellow
            }
        } else {
            Write-Host "ℹ️  No anomalies detected - traffic appears normal" -ForegroundColor Green
        }
        
        return $response
        
    } catch {
        Write-Host "❌ ERROR during analysis: $($_.Exception.Message)" -ForegroundColor Red
        if ($_.Exception.Response) {
            try {
                $reader = [System.IO.StreamReader]::new($_.Exception.Response.GetResponseStream())
                $responseBody = $reader.ReadToEnd()
                Write-Host "API Response: $responseBody" -ForegroundColor Red
            } catch {
                Write-Host "Could not read error response" -ForegroundColor Red
            }
        }
        return $null
    }
}

# Test connectivity first
Write-Host "Testing API connectivity..." -ForegroundColor Cyan
try {
    $anomalies = Invoke-RestMethod -Uri "$apiUrl/api/anomalies" -Method Get
    Write-Host "✅ API is responding. Current anomalies in database: $($anomalies.Count)" -ForegroundColor Green
} catch {
    Write-Host "⚠️  API connectivity test failed: $($_.Exception.Message)" -ForegroundColor Yellow
    Write-Host "Continuing with tests anyway..." -ForegroundColor Gray
}

# Test each PCAP file
$testFiles = @(
    @{ Path = "sample-data\normal-traffic.pcap"; Name = "Normal Traffic Test" },
    @{ Path = "sample-data\port-scan.pcap"; Name = "Port Scan Detection Test" }
)

$results = @()
foreach ($test in $testFiles) {
    $result = Test-PcapAnalysis -FilePath $test.Path -TestName $test.Name
    $results += @{ Test = $test.Name; Result = $result }
    Start-Sleep -Seconds 2
}

# Summary
Write-Host "`n" + "="*60 -ForegroundColor Green
Write-Host "🎯 FINAL TEST SUMMARY" -ForegroundColor Green
Write-Host "="*60 -ForegroundColor Green

foreach ($testResult in $results) {
    if ($testResult.Result) {
        $anomalyCount = $testResult.Result.anomaliesDetected
        $recordCount = $testResult.Result.totalRecordsProcessed
        Write-Host "✅ $($testResult.Test): $recordCount records, $anomalyCount anomalies" -ForegroundColor White
    } else {
        Write-Host "❌ $($testResult.Test): FAILED" -ForegroundColor Red
    }
}

Write-Host "`n🔍 Expected Results:" -ForegroundColor Cyan
Write-Host "  • Normal Traffic: Few or no anomalies" -ForegroundColor Gray
Write-Host "  • Port Scan Traffic: Should detect port scanning patterns" -ForegroundColor Gray

Write-Host "`n🎉 Port Scan Detection Test Complete!" -ForegroundColor Green
