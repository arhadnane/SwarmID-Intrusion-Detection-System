# Test de detection de port scan pour SwarmID - Version avec chemins absolus
param(
    [string]$ApiUrl = "http://localhost:5112"
)

$BasePath = "d:\Coding\VSCodeProject\Anomaly-Based Intrusion Detection System Using Swarm Intelligence"

Write-Host "=== SwarmID Port Scan Detection Test ===" -ForegroundColor Cyan
Write-Host "API URL: $ApiUrl" -ForegroundColor Gray
Write-Host "Base Path: $BasePath" -ForegroundColor Gray

# Fonction pour tester un fichier PCAP
function Test-PcapForPortScan {
    param(
        [string]$FilePath,
        [string]$TestName,
        [string]$ExpectedResult
    )
    
    Write-Host "`n--- Test: $TestName ---" -ForegroundColor Yellow
    Write-Host "File: $FilePath" -ForegroundColor Gray
    Write-Host "Expected: $ExpectedResult" -ForegroundColor Gray
    
    if (-not (Test-Path $FilePath)) {
        Write-Host "Error: File not found: $FilePath" -ForegroundColor Red
        return $false
    }
    
    $fileSize = (Get-Item $FilePath).Length
    Write-Host "File size: $fileSize bytes" -ForegroundColor Gray
    
    try {
        # Upload du fichier via API
        $uri = "$ApiUrl/api/traffic/analyze"
        $form = @{
            file = Get-Item -Path $FilePath
            dataType = "PcapFile"
        }
        
        Write-Host "Uploading to API..." -ForegroundColor Blue
        $response = Invoke-RestMethod -Uri $uri -Method Post -Form $form -ContentType "multipart/form-data"
        
        Write-Host "API Response received" -ForegroundColor Green
        Write-Host "Records processed: $($response.totalRecordsProcessed)" -ForegroundColor Blue
        Write-Host "Total anomalies: $($response.anomaliesDetected)" -ForegroundColor Blue
        
        # Analyser les resultats
        if ($response -and $response.anomalies) {
            $anomalies = $response.anomalies
            Write-Host "Detected $($anomalies.Count) anomalies:" -ForegroundColor Green
            
            $portScanCount = 0
            foreach ($anomaly in $anomalies) {
                $typeStr = if ($anomaly.type) { $anomaly.type } else { "Unknown" }
                $scoreStr = if ($anomaly.score) { $anomaly.score.ToString("F1") } else { "N/A" }
                $confidenceStr = if ($anomaly.confidence) { $anomaly.confidence.ToString("F1") } else { "N/A" }
                
                Write-Host "   Type: $typeStr (Score: $scoreStr, Confidence: $confidenceStr)" -ForegroundColor White
                Write-Host "     Description: $($anomaly.description)" -ForegroundColor Gray
                
                # Compter les port scans detectes
                if ($typeStr -match "PortScan|Port.?Scan|Scan") {
                    $portScanCount++
                }
                
                # Afficher les IPs et ports si disponibles
                if ($anomaly.sourceIPs -and $anomaly.sourceIPs.Count -gt 0) {
                    Write-Host "     Source IPs: $($anomaly.sourceIPs -join ', ')" -ForegroundColor Green
                }
                if ($anomaly.destinationIPs -and $anomaly.destinationIPs.Count -gt 0) {
                    Write-Host "     Target IPs: $($anomaly.destinationIPs -join ', ')" -ForegroundColor Green
                }
                if ($anomaly.ports -and $anomaly.ports.Count -gt 0) {
                    Write-Host "     Ports: $($anomaly.ports -join ', ')" -ForegroundColor Green
                }
            }
            
            # Resume de detection
            if ($portScanCount -gt 0) {
                Write-Host "Port Scan Detection: SUCCESS ($portScanCount port scan(s) detected)" -ForegroundColor Green
            } else {
                Write-Host "Port Scan Detection: No specific port scans detected (but found $($anomalies.Count) other anomalies)" -ForegroundColor Yellow
            }
            
        } else {
            Write-Host "No anomalies detected" -ForegroundColor Yellow
        }
        
        return $true
        
    } catch {
        Write-Host "Error testing $TestName : $($_.Exception.Message)" -ForegroundColor Red
        if ($_.Exception.Response) {
            $statusCode = $_.Exception.Response.StatusCode
            Write-Host "   HTTP Status: $statusCode" -ForegroundColor Red
        }
        return $false
    }
}

# Tests principaux
Write-Host "`nStarting port scan detection tests..." -ForegroundColor Green

# Test 1: PCAP de port scan
$portScanFile = "$BasePath\sample-data\port-scan.pcap"
Test-PcapForPortScan -FilePath $portScanFile -TestName "Port Scan PCAP" -ExpectedResult "3-5 port scan anomalies"

# Test comparatif avec le trafic normal
Write-Host "`n--- Comparative Test: Normal Traffic ---" -ForegroundColor Yellow
$normalFile = "$BasePath\sample-data\normal-traffic.pcap"
Test-PcapForPortScan -FilePath $normalFile -TestName "Normal Traffic (Baseline)" -ExpectedResult "0-1 anomalies (should NOT detect port scans)"

# Test avec le trafic suspicieux
Write-Host "`n--- Comparative Test: Suspicious Traffic ---" -ForegroundColor Yellow
$suspiciousFile = "$BasePath\sample-data\suspicious-traffic.pcap"
Test-PcapForPortScan -FilePath $suspiciousFile -TestName "Suspicious Traffic (DDoS)" -ExpectedResult "2-4 anomalies (should detect DDoS, not port scans)"

# Resume final
Write-Host "`n=== Port Scan Test Summary ===" -ForegroundColor Cyan
Write-Host "Tests completed" -ForegroundColor Green
Write-Host "Check results above to evaluate detection accuracy" -ForegroundColor Blue
Write-Host "Port scans should be detected only in port-scan.pcap" -ForegroundColor Yellow

Write-Host "`nTest completed!" -ForegroundColor Green
