#!/usr/bin/env pwsh
# Test de détection de port scan pour SwarmID
# Ce script teste différentes méthodes de génération de port scan et vérifie la détection

param(
    [switch]$UseNmap = $false,
    [switch]$UseExistingPcap = $true,
    [switch]$GenerateNewPcap = $false,
    [string]$TargetIP = "127.0.0.1",
    [string]$ApiUrl = "http://localhost:5112"
)

Write-Host "=== SwarmID Port Scan Detection Test ===" -ForegroundColor Cyan
Write-Host "API URL: $ApiUrl" -ForegroundColor Gray
Write-Host "Target IP: $TargetIP" -ForegroundColor Gray

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
        Write-Host "❌ File not found: $FilePath" -ForegroundColor Red
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
        
        Write-Host "📤 Uploading to API..." -ForegroundColor Blue
        $response = Invoke-RestMethod -Uri $uri -Method Post -Form $form -ContentType "multipart/form-data"
        
        Write-Host "✅ API Response received" -ForegroundColor Green
        
        # Analyser les résultats
        if ($response -and $response.anomalies) {
            $anomalies = $response.anomalies
            Write-Host "🔍 Detected $($anomalies.Count) anomalies:" -ForegroundColor Green
            
            $portScanCount = 0
            foreach ($anomaly in $anomalies) {
                $typeStr = if ($anomaly.type) { $anomaly.type } else { "Unknown" }
                $scoreStr = if ($anomaly.score) { $anomaly.score.ToString("F1") } else { "N/A" }
                
                Write-Host "   • Type: $typeStr (Score: $scoreStr)" -ForegroundColor White
                Write-Host "     Description: $($anomaly.description)" -ForegroundColor Gray
                
                # Compter les port scans détectés
                if ($typeStr -match "PortScan|Port.?Scan") {
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
            
            # Résumé de détection        if ($portScanCount -gt 0) {
                Write-Host "🎯 Port Scan Detection: SUCCESS ($portScanCount port scan(s) detected)" -ForegroundColor Green
            } else {
                Write-Host "⚠️  Port Scan Detection: No specific port scans detected (but found $($anomalies.Count) other anomalies)" -ForegroundColor Yellow
            }
        } else {
            Write-Host "ℹ️  No anomalies detected" -ForegroundColor Yellow
        }
        
        return $true
        
    } catch {
        Write-Host "❌ Error testing $TestName`: $($_.Exception.Message)" -ForegroundColor Red
        if ($_.Exception.Response) {
            $statusCode = $_.Exception.Response.StatusCode
            Write-Host "   HTTP Status: $statusCode" -ForegroundColor Red
        }
        return $false
    }
}

# Fonction pour générer un port scan avec nmap (si disponible)
function Test-NmapPortScan {
    param([string]$Target)
    
    Write-Host "`n--- Test: Nmap Port Scan ---" -ForegroundColor Yellow
    
    # Vérifier si nmap est disponible
    try {
        $nmapVersion = nmap --version 2>$null
        if (-not $nmapVersion) {
            Write-Host "❌ Nmap n'est pas installé ou disponible dans le PATH" -ForegroundColor Red
            Write-Host "   Vous pouvez télécharger nmap depuis: https://nmap.org/download.html" -ForegroundColor Gray
            return $false
        }
        
        Write-Host "✅ Nmap détecté" -ForegroundColor Green
        
        # Générer un port scan rapide
        $outputFile = "nmap-port-scan-$(Get-Date -Format 'yyyyMMdd-HHmmss').pcap"
        Write-Host "🔍 Exécution d'un port scan nmap sur $Target..." -ForegroundColor Blue
        Write-Host "   Commande: nmap -sS -F $Target" -ForegroundColor Gray
        
        # Note: nmap ne génère pas directement de PCAP, mais on peut l'analyser via la surveillance réseau
        $nmapResult = nmap -sS -F $Target 2>&1
        Write-Host "📊 Résultat nmap:" -ForegroundColor Green
        Write-Host $nmapResult -ForegroundColor White
          Write-Host "ℹ️  Pour capturer le trafic nmap en PCAP, utilisez:" -ForegroundColor Blue
        Write-Host "   tcpdump -i interface -w capture.pcap `& nmap -sS $Target" -ForegroundColor Gray
        
        return $true
        
    } catch {
        Write-Host "❌ Erreur lors de l'exécution de nmap: $($_.Exception.Message)" -ForegroundColor Red
        return $false
    }
}

# Fonction pour générer un nouveau PCAP de port scan
function Generate-NewPortScanPcap {
    Write-Host "`n--- Génération d'un nouveau PCAP de port scan ---" -ForegroundColor Yellow
    
    $sampleDataPath = "sample-data"
    if (-not (Test-Path $sampleDataPath)) {
        Write-Host "❌ Répertoire sample-data non trouvé" -ForegroundColor Red
        return $false
    }
    
    try {
        Set-Location $sampleDataPath
        Write-Host "📦 Génération des fichiers PCAP..." -ForegroundColor Blue
        
        # Compiler et exécuter le générateur de PCAP
        dotnet run --project SamplePcapGenerator.csproj
        
        Write-Host "✅ Nouveaux fichiers PCAP générés" -ForegroundColor Green
        Set-Location ..
        return $true
        
    } catch {
        Write-Host "❌ Erreur lors de la génération: $($_.Exception.Message)" -ForegroundColor Red
        Set-Location ..
        return $false
    }
}

# Tests principaux
Write-Host "`n🚀 Démarrage des tests de détection de port scan..." -ForegroundColor Green

# Test 1: PCAP de port scan existant
if ($UseExistingPcap) {
    $portScanFile = "sample-data\port-scan.pcap"
    Test-PcapForPortScan -FilePath $portScanFile -TestName "Existing Port Scan PCAP" -ExpectedResult "3-5 port scan anomalies"
}

# Test 2: Génération d'un nouveau PCAP
if ($GenerateNewPcap) {
    if (Generate-NewPortScanPcap) {
        $newPortScanFile = "sample-data\port-scan.pcap"
        Test-PcapForPortScan -FilePath $newPortScanFile -TestName "Newly Generated Port Scan PCAP" -ExpectedResult "3-5 port scan anomalies"
    }
}

# Test 3: Test avec nmap (si demandé)
if ($UseNmap) {
    Test-NmapPortScan -Target $TargetIP
}

# Test comparatif avec le trafic normal
Write-Host "`n--- Test comparatif: Trafic Normal ---" -ForegroundColor Yellow
$normalFile = "sample-data\normal-traffic.pcap"
Test-PcapForPortScan -FilePath $normalFile -TestName "Normal Traffic (Baseline)" -ExpectedResult "0-1 anomalies (should NOT detect port scans)"

# Test avec le trafic suspicieux
Write-Host "`n--- Test comparatif: Trafic Suspicieux ---" -ForegroundColor Yellow
$suspiciousFile = "sample-data\suspicious-traffic.pcap"
Test-PcapForPortScan -FilePath $suspiciousFile -TestName "Suspicious Traffic (DDoS)" -ExpectedResult "2-4 anomalies (should detect DDoS, not port scans)"

# Résumé final
Write-Host "`n=== Résumé des Tests de Port Scan ===" -ForegroundColor Cyan
Write-Host "✅ Tests terminés" -ForegroundColor Green
Write-Host "📊 Vérifiez les résultats ci-dessus pour évaluer la précision de détection" -ForegroundColor Blue
Write-Host "🎯 Les port scans devraient être détectés uniquement dans port-scan.pcap" -ForegroundColor Yellow

# Conseils d'amélioration
Write-Host "`n💡 Conseils pour améliorer les tests:" -ForegroundColor Blue
Write-Host "   1. Installez nmap pour tester avec du vrai trafic: choco install nmap" -ForegroundColor Gray
Write-Host "   2. Utilisez Wireshark pour capturer du vrai trafic réseau" -ForegroundColor Gray
Write-Host "   3. Testez avec différents types de scans: -sS, -sT, -sU" -ForegroundColor Gray
Write-Host "   4. Variez les plages de ports: -p 1-1000, --top-ports 100" -ForegroundColor Gray

Write-Host "`nTest terminé ! 🎉" -ForegroundColor Green
