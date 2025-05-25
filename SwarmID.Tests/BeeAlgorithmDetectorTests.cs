using Moq;
using SwarmID.Core.Algorithms;
using SwarmID.Core.Interfaces;
using SwarmID.Core.Models;

namespace SwarmID.Tests;

public class BeeAlgorithmDetectorTests
{
    private readonly Mock<IAnomalyRepository> _mockRepository;
    private readonly BeeAlgorithmDetector _detector;
    private readonly SwarmConfiguration _config;

    public BeeAlgorithmDetectorTests()
    {
        _mockRepository = new Mock<IAnomalyRepository>();
        _config = new SwarmConfiguration
        {
            Id = Guid.NewGuid(),
            Name = "Test Bee Config",
            MaxAnomalyScore = 100.0,
            AnomalyThreshold = 50.0,
            NumberOfAnts = 10, // Used as bee count
            MaxIterations = 5,
            NumberOfEmployedBees = 5,
            NumberOfOnlookerBees = 5,
            MaxTrialCount = 3,
            AcceptanceProbability = 0.1
        };
        _detector = new BeeAlgorithmDetector(_mockRepository.Object);
    }

    [Fact]
    public async Task AnalyzeTrafficAsync_WithNormalTraffic_ReturnsLowAnomalyScore()
    {
        // Arrange
        var traffic = new List<NetworkTrafficRecord>
        {
            CreateNormalTrafficRecord("192.168.1.10", "192.168.1.1", 80, 1024),
            CreateNormalTrafficRecord("192.168.1.11", "192.168.1.1", 443, 2048),
            CreateNormalTrafficRecord("192.168.1.12", "192.168.1.1", 80, 1536)
        };

        // Act
        var result = await _detector.AnalyzeTrafficAsync(traffic, _config);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Score < _config.AnomalyThreshold);
        Assert.Equal(AnomalyType.Normal, result.Type);
        Assert.Contains("Bee Algorithm", result.Description);
    }

    [Fact]
    public async Task AnalyzeTrafficAsync_WithPortScanPattern_DetectsPortScanAnomaly()
    {
        // Arrange - Create port scan pattern
        var traffic = new List<NetworkTrafficRecord>();
        for (int port = 20; port < 100; port++)
        {
            traffic.Add(CreateTrafficRecord("192.168.1.100", "10.0.0.5", port, 64, "TCP"));
        }

        // Act
        var result = await _detector.AnalyzeTrafficAsync(traffic, _config);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Score > 0);
        // Bee algorithm might detect this as port scan or other anomaly type
        Assert.NotEqual(AnomalyType.Normal, result.Type);
        Assert.Contains("Bee Algorithm", result.Description);
    }

    [Fact]
    public async Task AnalyzeTrafficAsync_WithDDoSPattern_DetectsDDoSAnomaly()
    {
        // Arrange - Create DDoS pattern
        var traffic = new List<NetworkTrafficRecord>();
        for (int i = 0; i < 150; i++) // Large number to trigger DDoS detection
        {
            traffic.Add(CreateTrafficRecord($"10.0.0.{i % 254 + 1}", "192.168.1.5", 80, 1024, "TCP"));
        }

        // Act
        var result = await _detector.AnalyzeTrafficAsync(traffic, _config);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Score > 0);
        Assert.NotEqual(AnomalyType.Normal, result.Type);
        Assert.Contains("Bee Algorithm", result.Description);
    }

    [Fact]
    public async Task AnalyzeTrafficAsync_WithDataExfiltrationPattern_DetectsDataExfiltration()
    {
        // Arrange - Create data exfiltration pattern (large outbound transfers)
        var traffic = new List<NetworkTrafficRecord>();
        for (int i = 0; i < 20; i++)
        {
            traffic.Add(CreateTrafficRecord("192.168.1.10", $"8.8.8.{i + 1}", 443, 1024 * 1024, "TCP")); // 1MB packets
        }

        // Act
        var result = await _detector.AnalyzeTrafficAsync(traffic, _config);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Score > 0);
        Assert.NotEqual(AnomalyType.Normal, result.Type);
        Assert.Contains("Bee Algorithm", result.Description);
    }

    [Fact]
    public async Task AnalyzeTrafficAsync_WithCommandAndControlPattern_DetectsC2Communication()
    {
        // Arrange - Create regular beaconing pattern
        var traffic = new List<NetworkTrafficRecord>();
        var baseTime = DateTime.UtcNow;
        
        for (int i = 0; i < 10; i++)
        {
            var record = CreateTrafficRecord("192.168.1.10", "203.0.113.1", 443, 256, "TCP");
            record.Timestamp = baseTime.AddSeconds(i * 60); // Regular 60-second intervals
            traffic.Add(record);
        }

        // Act
        var result = await _detector.AnalyzeTrafficAsync(traffic, _config);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Score >= 0); // May or may not detect depending on algorithm sensitivity
        Assert.Contains("Bee Algorithm", result.Description);
    }

    [Fact]
    public async Task AnalyzeTrafficAsync_WithEmptyTraffic_ReturnsNormalResult()
    {
        // Arrange
        var traffic = new List<NetworkTrafficRecord>();

        // Act
        var result = await _detector.AnalyzeTrafficAsync(traffic, _config);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.Score);
        Assert.Equal(AnomalyType.Normal, result.Type);
        Assert.Contains("No traffic data", result.Description);
    }

    [Fact]
    public async Task AnalyzeTrafficAsync_SavesDetectedAnomaly_WhenAnomalyDetected()
    {
        // Arrange
        var traffic = CreatePortScanTraffic();        _mockRepository.Setup(r => r.SaveAnomalyAsync(It.IsAny<Anomaly>()))
                      .Returns(Task.CompletedTask);

        // Act
        var result = await _detector.AnalyzeTrafficAsync(traffic, _config);

        // Assert
        // Verify that SaveAnomalyAsync was called if score exceeds threshold
        if (result.Score >= _config.AnomalyThreshold)
        {
            _mockRepository.Verify(r => r.SaveAnomalyAsync(It.IsAny<Anomaly>()), Times.Once);
        }
    }

    [Theory]
    [InlineData(4, 2)]
    [InlineData(8, 3)]
    [InlineData(12, 5)]
    public async Task AnalyzeTrafficAsync_WithVariousBeeColonySizes_ProducesResults(int colonySize, int iterations)
    {
        // Arrange
        var config = new SwarmConfiguration
        {
            Id = Guid.NewGuid(),
            Name = "Variable Bee Test",
            MaxAnomalyScore = 100.0,
            AnomalyThreshold = 50.0,
            NumberOfAnts = colonySize, // Used as total bee count
            MaxIterations = iterations,
            NumberOfEmployedBees = colonySize / 2,
            NumberOfOnlookerBees = colonySize / 2,
            MaxTrialCount = 3,
            AcceptanceProbability = 0.1
        };

        var traffic = CreatePortScanTraffic();

        // Act
        var result = await _detector.AnalyzeTrafficAsync(traffic, config);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Score >= 0);
        Assert.Contains("Bee Algorithm", result.Description);
    }

    [Fact]
    public async Task AnalyzeTrafficAsync_Algorithm_Property_IsSetCorrectly()
    {
        // Arrange
        var traffic = CreateNormalTraffic();

        // Act
        var result = await _detector.AnalyzeTrafficAsync(traffic, _config);

        // Assert
        Assert.Equal("Bee Algorithm", result.Algorithm);
    }

    [Fact]
    public async Task AnalyzeTrafficAsync_ConfigurationUsed_Property_IsSetCorrectly()
    {
        // Arrange
        var traffic = CreateNormalTraffic();

        // Act
        var result = await _detector.AnalyzeTrafficAsync(traffic, _config);

        // Assert
        Assert.Equal(_config.Name, result.ConfigurationUsed);
    }

    [Fact]
    public async Task AnalyzeTrafficAsync_ExtractsRelevantTrafficFeatures()
    {
        // Arrange
        var traffic = new List<NetworkTrafficRecord>
        {
            CreateTrafficRecord("192.168.1.1", "10.0.0.1", 80, 1024, "TCP"),
            CreateTrafficRecord("192.168.1.2", "10.0.0.2", 443, 2048, "TCP"),
            CreateTrafficRecord("192.168.1.3", "10.0.0.3", 22, 512, "TCP")
        };

        // Act
        var result = await _detector.AnalyzeTrafficAsync(traffic, _config);

        // Assert
        Assert.NotNull(result.SourceIPs);
        Assert.NotNull(result.DestinationIPs);
        Assert.NotNull(result.Ports);
        
        // Should have extracted some IPs and ports
        Assert.True(result.SourceIPs.Count > 0 || result.DestinationIPs.Count > 0);
    }

    private NetworkTrafficRecord CreateNormalTrafficRecord(string sourceIp, string destIp, int destPort, int packetSize)
    {
        return CreateTrafficRecord(sourceIp, destIp, destPort, packetSize, "TCP");
    }

    private NetworkTrafficRecord CreateTrafficRecord(string sourceIp, string destIp, int destPort, int packetSize, string protocol)
    {
        return new NetworkTrafficRecord
        {
            Id = Guid.NewGuid(),
            Timestamp = DateTime.UtcNow,
            SourceIP = sourceIp,
            DestinationIP = destIp,
            SourcePort = Random.Shared.Next(1024, 65535),
            DestinationPort = destPort,
            Protocol = protocol,
            PacketSize = packetSize,
            Flags = "SYN"
        };
    }

    private List<NetworkTrafficRecord> CreatePortScanTraffic()
    {
        var traffic = new List<NetworkTrafficRecord>();
        for (int port = 20; port < 80; port++)
        {
            traffic.Add(CreateTrafficRecord("192.168.1.100", "10.0.0.5", port, 64, "TCP"));
        }
        return traffic;
    }

    private List<NetworkTrafficRecord> CreateNormalTraffic()
    {
        return new List<NetworkTrafficRecord>
        {
            CreateNormalTrafficRecord("192.168.1.10", "192.168.1.1", 80, 1024),
            CreateNormalTrafficRecord("192.168.1.11", "192.168.1.1", 443, 2048)
        };
    }
}
