# Script PowerShell pour aider à organiser les captures d'écran SwarmID
# Utilisation: .\PrepareScreenshots.ps1

$projectRoot = $PSScriptRoot
$imagesDir = Join-Path $projectRoot "docs\images"

Write-Host "🖼️  Guide de Préparation des Captures d'Écran SwarmID" -ForegroundColor Cyan
Write-Host "=" * 60

# Vérifier que les services sont en cours d'exécution
Write-Host "`n🔍 Vérification des services..." -ForegroundColor Yellow

$apiUrl = "http://localhost:5112"
$dashboardUrl = "http://localhost:5121"

try {
    $apiTest = Invoke-WebRequest -Uri "$apiUrl/swagger" -TimeoutSec 5 -UseBasicParsing
    Write-Host "✅ API SwarmID accessible sur $apiUrl" -ForegroundColor Green
} catch {
    Write-Host "❌ API SwarmID non accessible sur $apiUrl" -ForegroundColor Red
    Write-Host "   Démarrez l'API avec: cd SwarmID.Api; dotnet run" -ForegroundColor Gray
}

try {
    $dashboardTest = Invoke-WebRequest -Uri $dashboardUrl -TimeoutSec 5 -UseBasicParsing
    Write-Host "✅ Dashboard SwarmID accessible sur $dashboardUrl" -ForegroundColor Green
} catch {
    Write-Host "❌ Dashboard SwarmID non accessible sur $dashboardUrl" -ForegroundColor Red
    Write-Host "   Démarrez le Dashboard avec: cd SwarmID.Dashboard; dotnet run" -ForegroundColor Gray
}

# Vérifier la structure des dossiers
Write-Host "`n📁 Vérification de la structure des dossiers..." -ForegroundColor Yellow

if (Test-Path $imagesDir) {
    Write-Host "✅ Dossier docs/images existe" -ForegroundColor Green
} else {
    New-Item -ItemType Directory -Path $imagesDir -Force | Out-Null
    Write-Host "✅ Dossier docs/images créé" -ForegroundColor Green
}

# Liste des captures d'écran requises
$screenshots = @(
    @{ 
        Name = "dashboard-main.png"
        Description = "Tableau de bord principal avec métriques"
        URL = "$dashboardUrl/"
    },
    @{ 
        Name = "traffic-analysis.png"
        Description = "Page d'analyse de trafic"
        URL = "$dashboardUrl/traffic"
    },
    @{ 
        Name = "anomaly-management.png"
        Description = "Gestion des anomalies"
        URL = "$dashboardUrl/anomalies"
    },
    @{ 
        Name = "api-swagger.png"
        Description = "Documentation API Swagger"
        URL = "$apiUrl/swagger"
    },
    @{ 
        Name = "realtime-monitoring.png"
        Description = "Monitoring temps réel"
        URL = "$dashboardUrl/traffic"
    },
    @{ 
        Name = "pcap-upload.png"
        Description = "Interface d'upload PCAP"
        URL = "$dashboardUrl/traffic"
    }
)

Write-Host "`n📋 Captures d'écran à prendre:" -ForegroundColor Yellow
$index = 1
foreach ($screenshot in $screenshots) {
    $filePath = Join-Path $imagesDir $screenshot.Name
    $exists = Test-Path $filePath
    $status = if ($exists) { "✅" } else { "⏳" }
    
    Write-Host "   $index. $status $($screenshot.Name)" -ForegroundColor $(if ($exists) { "Green" } else { "White" })
    Write-Host "      📝 $($screenshot.Description)" -ForegroundColor Gray
    Write-Host "      🔗 $($screenshot.URL)" -ForegroundColor Gray
    Write-Host ""
    $index++
}

# Vérifier les fichiers PCAP de test
Write-Host "📦 Vérification des fichiers PCAP de test..." -ForegroundColor Yellow
$sampleDataDir = Join-Path $projectRoot "sample-data"
$pcapFiles = @("normal-traffic.pcap", "port-scan.pcap", "suspicious-traffic.pcap")

foreach ($pcapFile in $pcapFiles) {
    $pcapPath = Join-Path $sampleDataDir $pcapFile
    if (Test-Path $pcapPath) {
        $size = (Get-Item $pcapPath).Length
        Write-Host "✅ $pcapFile ($([math]::Round($size/1KB, 1)) KB)" -ForegroundColor Green
    } else {
        Write-Host "❌ $pcapFile manquant" -ForegroundColor Red
    }
}

# Instructions pour les captures d'écran
Write-Host "`n🎯 Instructions pour les captures d'écran:" -ForegroundColor Cyan
Write-Host "1. Utilisez Windows + Shift + S pour capturer une zone" -ForegroundColor White
Write-Host "2. Sauvegardez chaque image dans docs/images/ avec le nom exact" -ForegroundColor White
Write-Host "3. Format recommandé: PNG, qualité élevée" -ForegroundColor White
Write-Host "4. Résolution recommandée: 1200-1920 pixels de largeur" -ForegroundColor White

Write-Host "`n🚀 URLs à visiter pour les captures:" -ForegroundColor Cyan
Write-Host "   Dashboard: $dashboardUrl" -ForegroundColor White
Write-Host "   API Docs:  $apiUrl/swagger" -ForegroundColor White

Write-Host "`n📖 Consultez docs/SCREENSHOT_GUIDE.md pour les instructions détaillées" -ForegroundColor Green

# Ouvrir automatiquement les URLs dans le navigateur
$openBrowser = Read-Host "`nVoulez-vous ouvrir les URLs dans le navigateur? (y/N)"
if ($openBrowser -eq "y" -or $openBrowser -eq "Y") {
    Start-Process $dashboardUrl
    Start-Sleep -Seconds 2
    Start-Process "$apiUrl/swagger"
    Write-Host "✅ URLs ouvertes dans le navigateur" -ForegroundColor Green
}
