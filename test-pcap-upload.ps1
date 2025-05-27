# Test PCAP file upload to SwarmID API
Write-Host "Testing PCAP file upload to SwarmID API..." -ForegroundColor Green

$apiUrl = "http://localhost:5112"
$pcapFile = "sample-data\port-scan.pcap"

# Check if file exists
if (-not (Test-Path $pcapFile)) {
    Write-Host "Error: PCAP file not found: $pcapFile" -ForegroundColor Red
    exit 1
}

Write-Host "Uploading file: $pcapFile" -ForegroundColor Cyan

try {
    # Use WebClient for file upload
    $webClient = New-Object System.Net.WebClient
    $webClient.Headers.Add("Content-Type", "multipart/form-data")
    
    # Read file as bytes
    $fileBytes = [System.IO.File]::ReadAllBytes($pcapFile)
    
    # Upload using POST
    $response = $webClient.UploadFile("$apiUrl/api/traffic/analyze", "POST", $pcapFile)
    $responseText = [System.Text.Encoding]::UTF8.GetString($response)
    
    Write-Host "Upload successful!" -ForegroundColor Green
    Write-Host "Response: $responseText" -ForegroundColor Yellow
    
} catch {
    Write-Host "Upload failed: $($_.Exception.Message)" -ForegroundColor Red
    
    # Try alternative method using Invoke-WebRequest
    Write-Host "Trying alternative upload method..." -ForegroundColor Yellow
    
    try {
        $form = @{
            file = Get-Item $pcapFile
        }
        $response = Invoke-WebRequest -Uri "$apiUrl/api/traffic/analyze" -Method Post -Form $form
        Write-Host "Alternative upload successful!" -ForegroundColor Green
        Write-Host "Response: $($response.Content)" -ForegroundColor Yellow
    } catch {
        Write-Host "Alternative upload also failed: $($_.Exception.Message)" -ForegroundColor Red
    }
}

# Check for new anomalies
Write-Host "`nChecking for new anomalies..." -ForegroundColor Cyan
try {
    $anomalies = Invoke-RestMethod -Uri "$apiUrl/api/anomalies" -Method Get
    Write-Host "Total anomalies in database: $($anomalies.Count)" -ForegroundColor Green
    
    if ($anomalies.Count -gt 0) {
        $latest = $anomalies | Sort-Object detectedAt -Descending | Select-Object -First 1
        Write-Host "Latest anomaly: $($latest.description)" -ForegroundColor Yellow
        Write-Host "Detection time: $($latest.detectedAt)" -ForegroundColor Gray
        Write-Host "Score: $($latest.score)" -ForegroundColor Gray
    }
} catch {
    Write-Host "Error retrieving anomalies: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`nTest completed." -ForegroundColor Green
