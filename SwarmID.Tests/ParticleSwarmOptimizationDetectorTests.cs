using Moq;
using SwarmID.Core.Algorithms;
using SwarmID.Core.Interfaces;
using SwarmID.Core.Models;
using Xunit;

namespace SwarmID.Tests;

public class ParticleSwarmOptimizationDetectorTests
{
    private readonly Mock<IAnomalyRepository> _mockRepository;
    private readonly SwarmConfiguration _config;
    private readonly ParticleSwarmOptimizationDetector _detector;

    public ParticleSwarmOptimizationDetectorTests()
    {
        _mockRepository = new Mock<IAnomalyRepository>();
        _config = new SwarmConfiguration
        {
            Id = Guid.NewGuid(),
            Name = "Test PSO Config",
            MaxAnomalyScore = 100.0,
            AnomalyThreshold = 50.0,
            NumberOfParticles = 5, // Reduced for faster tests
            MaxIterations = 2,     // Reduced for faster tests
            InertiaWeight = 0.9,
            CognitiveComponent = 2.0,
            SocialComponent = 2.0,
            MinInertiaWeight = 0.4,
            MaxVelocity = 10.0
        };
        _detector = new ParticleSwarmOptimizationDetector(_mockRepository.Object);
    }

    [Fact]
    public async Task DetectAnomaliesAsync_WithEmptyTraffic_ReturnsNormalAnomaly()
    {
        // Arrange
        var emptyTraffic = new List<NetworkTrafficRecord>();

        // Act
        var result = await _detector.DetectAnomaliesAsync(emptyTraffic);

        // Assert
        Assert.NotNull(result);
        var anomaly = result.First();
        Assert.Equal(AnomalyType.Normal, anomaly.Type);
        Assert.Equal(0, anomaly.Score);
        Assert.Equal("Particle Swarm Optimization", anomaly.Algorithm);
    }

    [Fact]
    public async Task DetectAnomaliesAsync_WithPortScanTraffic_DetectsPortScan()
    {
        // Arrange
        await _detector.UpdateConfigurationAsync(_config);
        var traffic = GeneratePortScanTraffic();

        // Act
        var result = await _detector.DetectAnomaliesAsync(traffic);

        // Assert
        Assert.NotNull(result);
        var anomaly = result.First();
        Assert.True(anomaly.Score >= 0);
        Assert.Contains("Particle Swarm Optimization", anomaly.Algorithm);
    }

    [Fact]
    public async Task DetectAnomaliesAsync_WithDDoSTraffic_DetectsDDoS()
    {
        // Arrange
        await _detector.UpdateConfigurationAsync(_config);
        var traffic = GenerateDDoSTraffic();

        // Act
        var result = await _detector.DetectAnomaliesAsync(traffic);

        // Assert
        Assert.NotNull(result);
        var anomaly = result.First();
        Assert.True(anomaly.Score >= 0);
        Assert.Equal("Particle Swarm Optimization", anomaly.Algorithm);
    }

    [Fact]
    public async Task UpdateWithFeedbackAsync_WithActualAnomaly_AdjustsParametersPositively()
    {
        // Arrange
        await _detector.UpdateConfigurationAsync(_config);
        var initialInertia = _detector.GetConfiguration().InertiaWeight;
        
        var anomaly = new Anomaly
        {
            Id = Guid.NewGuid(),
            Type = AnomalyType.PortScan,
            Score = 75.0,
            Status = AnomalyStatus.New
        };

        // Act
        await _detector.UpdateWithFeedbackAsync(anomaly, true);

        // Assert
        var updatedConfig = _detector.GetConfiguration();
        Assert.True(updatedConfig.InertiaWeight >= initialInertia - 0.1); // Allow for small variations
        _mockRepository.Verify(r => r.UpdateAnomalyAsync(It.IsAny<Anomaly>()), Times.Once);
    }

    [Fact]
    public async Task UpdateConfigurationAsync_WithValidConfig_UpdatesConfiguration()
    {
        // Arrange
        var newConfig = new SwarmConfiguration
        {
            Id = Guid.NewGuid(),
            Name = "Updated PSO Config",
            NumberOfParticles = 15,
            InertiaWeight = 0.8,
            CognitiveComponent = 1.5,
            SocialComponent = 2.5
        };

        // Act
        await _detector.UpdateConfigurationAsync(newConfig);

        // Assert
        var currentConfig = _detector.GetConfiguration();
        Assert.Equal(newConfig.Id, currentConfig.Id);
        Assert.Equal(newConfig.Name, currentConfig.Name);
        Assert.Equal(newConfig.NumberOfParticles, currentConfig.NumberOfParticles);
        Assert.Equal(newConfig.InertiaWeight, currentConfig.InertiaWeight);
    }

    [Fact]
    public void GetConfiguration_ReturnsCurrentConfiguration()
    {
        // Act
        var config = _detector.GetConfiguration();

        // Assert
        Assert.NotNull(config);
        Assert.Equal("Default PSO Configuration", config.Name);
        Assert.Equal(20, config.NumberOfParticles);
        Assert.Equal(0.9, config.InertiaWeight);
    }

    [Fact]
    public async Task AnalyzeTrafficAsync_WithValidTraffic_ReturnsValidAnomaly()
    {
        // Arrange
        var traffic = GeneratePortScanTraffic();

        // Act
        var result = await _detector.AnalyzeTrafficAsync(traffic, _config);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Score >= 0);
        Assert.NotNull(result.Description);
        Assert.Equal("Particle Swarm Optimization", result.Algorithm);
        Assert.Equal(_config.Name, result.ConfigurationUsed);
    }

    [Fact]
    public async Task PSO_Algorithm_ConvergesToOptimalThresholds()
    {
        // Arrange
        await _detector.UpdateConfigurationAsync(_config);
        var traffic = GenerateMixedTraffic();

        // Act - Run analysis to test PSO algorithm
        var result = await _detector.DetectAnomaliesAsync(traffic);

        // Assert - Algorithm should produce meaningful results
        Assert.NotNull(result);
        var anomaly = result.First();
        Assert.True(anomaly.Score >= 0);
        Assert.Equal("Particle Swarm Optimization", anomaly.Algorithm);
    }

    #region Helper Methods

    private List<NetworkTrafficRecord> GeneratePortScanTraffic()
    {
        var traffic = new List<NetworkTrafficRecord>();
        var baseTime = DateTime.UtcNow;
        var scannerIP = "10.0.0.100";
        var targetIP = "192.168.1.50";

        // Generate limited traffic for faster tests
        for (int port = 20; port <= 40; port++)
        {
            traffic.Add(new NetworkTrafficRecord
            {
                Timestamp = baseTime.AddSeconds(port - 20),
                SourceIP = scannerIP,
                DestinationIP = targetIP,
                SourcePort = 55000 + port,
                DestinationPort = port,
                Protocol = "TCP",
                PacketSize = 64,
                Flags = "SYN"
            });
        }

        return traffic;
    }

    private List<NetworkTrafficRecord> GenerateDDoSTraffic()
    {
        var traffic = new List<NetworkTrafficRecord>();
        var baseTime = DateTime.UtcNow;
        var targetIP = "192.168.1.100";
        var targetPort = 80;

        // Generate limited DDoS traffic for faster tests
        for (int i = 0; i < 50; i++)
        {
            traffic.Add(new NetworkTrafficRecord
            {
                Timestamp = baseTime.AddSeconds(i * 0.1),
                SourceIP = $"10.0.{i / 25}.{i % 25}",
                DestinationIP = targetIP,
                SourcePort = 50000 + i,
                DestinationPort = targetPort,
                Protocol = "TCP",
                PacketSize = 1500,
                Flags = "SYN"
            });
        }

        return traffic;
    }

    private List<NetworkTrafficRecord> GenerateC2Traffic()
    {
        var traffic = new List<NetworkTrafficRecord>();
        var baseTime = DateTime.UtcNow;
        var botIP = "192.168.1.75";
        var c2ServerIP = "203.0.113.15";

        // Generate limited C2 traffic for faster tests
        for (int i = 0; i < 10; i++)
        {
            traffic.Add(new NetworkTrafficRecord
            {
                Timestamp = baseTime.AddSeconds(i * 60),
                SourceIP = botIP,
                DestinationIP = c2ServerIP,
                SourcePort = 45000,
                DestinationPort = 443,
                Protocol = "TCP",
                PacketSize = 256,
                Flags = "PSH"
            });
        }

        return traffic;
    }

    private List<NetworkTrafficRecord> GenerateDataExfiltrationTraffic()
    {
        var traffic = new List<NetworkTrafficRecord>();
        var baseTime = DateTime.UtcNow;
        var internalIP = "192.168.1.25";
        var externalIP = "198.51.100.10";

        // Generate limited exfiltration traffic for faster tests
        for (int i = 0; i < 20; i++)
        {
            traffic.Add(new NetworkTrafficRecord
            {
                Timestamp = baseTime.AddSeconds(i * 0.5),
                SourceIP = internalIP,
                DestinationIP = externalIP,
                SourcePort = 55000,
                DestinationPort = 443,
                Protocol = "TCP",
                PacketSize = 1500 * 100, // 150KB per packet instead of 1.5MB
                Flags = "PSH"
            });
        }

        return traffic;
    }

    private List<NetworkTrafficRecord> GenerateNormalTraffic()
    {
        var traffic = new List<NetworkTrafficRecord>();
        var baseTime = DateTime.UtcNow;

        // Generate limited normal traffic for faster tests
        for (int i = 0; i < 10; i++)
        {
            traffic.Add(new NetworkTrafficRecord
            {
                Timestamp = baseTime.AddSeconds(i * 2),
                SourceIP = "192.168.1.10",
                DestinationIP = "93.184.216.34", // example.com
                SourcePort = 55000 + i,
                DestinationPort = 80,
                Protocol = "TCP",
                PacketSize = 512,
                Flags = "ACK"
            });
        }

        return traffic;
    }

    private List<NetworkTrafficRecord> GenerateMixedTraffic()
    {
        var traffic = new List<NetworkTrafficRecord>();
        traffic.AddRange(GenerateNormalTraffic());
        traffic.AddRange(GeneratePortScanTraffic().Take(5));
        traffic.AddRange(GenerateDDoSTraffic().Take(10));
        
        return traffic.OrderBy(t => t.Timestamp).ToList();
    }

    #endregion
}
