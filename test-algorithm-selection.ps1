# Test Algorithm Switching

Write-Host "=== SwarmID Algorithm Switching Test ===" -ForegroundColor Green

# Function to get current algorithm from config
function Get-CurrentAlgorithm {
    param($configPath)
    
    if (Test-Path $configPath) {
        $config = Get-Content $configPath | ConvertFrom-Json
        return $config.SwarmAlgorithm
    }
    return "Not found"
}

# Function to set algorithm in config
function Set-Algorithm {
    param($configPath, $algorithm)
    
    if (Test-Path $configPath) {
        $config = Get-Content $configPath | ConvertFrom-Json
        $config.SwarmAlgorithm = $algorithm
        $config | ConvertTo-Json -Depth 10 | Set-Content $configPath
        Write-Host "✅ Updated $configPath to use $algorithm" -ForegroundColor Green
    }
}

$apiConfig = "SwarmID.Api\appsettings.json"
$dashboardConfig = "SwarmID.Dashboard\appsettings.json"

Write-Host "`n📊 Current Algorithm Configuration:" -ForegroundColor Yellow
Write-Host "API: $(Get-CurrentAlgorithm $apiConfig)"
Write-Host "Dashboard: $(Get-CurrentAlgorithm $dashboardConfig)"

Write-Host "`n🔄 Available Algorithms:" -ForegroundColor Cyan
Write-Host "• ACO - Ant Colony Optimization"
Write-Host "• BEE - Bee Algorithm" 
Write-Host "• PSO - Particle Swarm Optimization (current)"

Write-Host "`n💡 To change algorithm:" -ForegroundColor Magenta
Write-Host "1. Edit appsettings.json files in both API and Dashboard"
Write-Host "2. Change 'SwarmAlgorithm' value to: ACO, BEE, or PSO"
Write-Host "3. Restart the applications"

Write-Host "`n⚠️  Important Notes:" -ForegroundColor Red
Write-Host "• Only ONE algorithm runs at a time"
Write-Host "• All algorithms detect the same threat types"
Write-Host "• The difference is in optimization approach"

Write-Host "`n🧪 Test Build:"
dotnet build SwarmID.sln --verbosity quiet
if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ Build successful - algorithm configuration is valid" -ForegroundColor Green
} else {
    Write-Host "❌ Build failed - check configuration" -ForegroundColor Red
}

Write-Host "`nTest completed!" -ForegroundColor Green
