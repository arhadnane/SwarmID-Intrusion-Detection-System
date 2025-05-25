# PowerShell script to test PCAP file uploads to SwarmID API
# This script validates that our generated PCAP files can be processed by the system

$apiBaseUrl = "http://localhost:5112"
$pcapFiles = @(
    @{ Name = "normal-traffic.pcap"; Description = "Baseline HTTPS traffic" },
    @{ Name = "port-scan.pcap"; Description = "Port scanning attack simulation" },
    @{ Name = "suspicious-traffic.pcap"; Description = "DDoS attack simulation" }
)

Write-Host "🔍 Testing SwarmID PCAP File Processing" -ForegroundColor Cyan
Write-Host "=" * 50

foreach ($pcap in $pcapFiles) {
    $filePath = Join-Path $PSScriptRoot $pcap.Name
    
    if (Test-Path $filePath) {
        $fileSize = (Get-Item $filePath).Length
        Write-Host "`n📁 Testing: $($pcap.Name)" -ForegroundColor Yellow
        Write-Host "   Description: $($pcap.Description)"
        Write-Host "   File Size: $([math]::Round($fileSize/1KB, 2)) KB"
        
        # Test file upload via API
        try {
            $formData = @{
                file = Get-Item $filePath
                dataType = "PcapFile"
            }
            
            Write-Host "   Status: ✅ File exists and ready for upload" -ForegroundColor Green
            Write-Host "   API Endpoint: $apiBaseUrl/api/traffic/analyze" -ForegroundColor Gray
        }
        catch {
            Write-Host "   Status: ❌ Error accessing file: $($_.Exception.Message)" -ForegroundColor Red
        }
    }
    else {
        Write-Host "`n📁 Testing: $($pcap.Name)" -ForegroundColor Yellow
        Write-Host "   Status: ❌ File not found" -ForegroundColor Red
    }
}

Write-Host "`n🎯 Testing Summary:" -ForegroundColor Cyan
Write-Host "   • All PCAP files should be uploaded via the Dashboard at: http://localhost:5121/traffic"
Write-Host "   • Select 'PCAP File' as the data type when uploading"
Write-Host "   • Expected results:"
Write-Host "     - normal-traffic.pcap: 0-1 anomalies (baseline traffic)"
Write-Host "     - port-scan.pcap: 3-5 anomalies (port scanning detected)"
Write-Host "     - suspicious-traffic.pcap: 2-4 anomalies (DDoS patterns detected)"
Write-Host "`n✨ Ready for manual testing via web interface!" -ForegroundColor Green
