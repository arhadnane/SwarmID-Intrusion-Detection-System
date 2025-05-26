using Moq;
using SwarmID.Core;
using SwarmID.Core.Algorithms;
using SwarmID.Core.Interfaces;
using SwarmID.Core.Models;

namespace SwarmID.Tests;

public class AntColonyOptimizationDetectorTests
{
    private readonly Mock<IAnomalyRepository> _mockRepository;
    private readonly AntColonyOptimizationDetector _detector;
    private readonly SwarmConfiguration _config;

    public AntColonyOptimizationDetectorTests()
    {
        _mockRepository = new Mock<IAnomalyRepository>();
        _config = new SwarmConfiguration
        {
            Id = Guid.NewGuid(),
            Name = "Test ACO Config",
            MaxAnomalyScore = 100.0,
            AnomalyThreshold = 50.0,
            NumberOfAnts = 10,
            MaxIterations = 5,
            PheromoneEvaporationRate = 0.1,
            Alpha = 1.0,
            Beta = 2.0,
            Q = 100.0
        };
        _detector = new AntColonyOptimizationDetector(_mockRepository.Object);
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
        Assert.Contains("Normal traffic", result.Description);
    }

    [Fact]
    public async Task AnalyzeTrafficAsync_WithPortScanPattern_DetectsPortScanAnomaly()
    {
        // Arrange - Create port scan pattern (same source, multiple destinations, sequential ports)
        var traffic = new List<NetworkTrafficRecord>();
        for (int port = 20; port < 100; port++)
        {
            traffic.Add(CreateTrafficRecord("192.168.1.100", "10.0.0.5", port, 64, "TCP"));
        }

        // Act
        var result = await _detector.AnalyzeTrafficAsync(traffic, _config);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Score >= _config.AnomalyThreshold);
        Assert.Equal(AnomalyType.PortScan, result.Type);
        Assert.Contains("Port scan", result.Description);
    }

    [Fact]
    public async Task AnalyzeTrafficAsync_WithDDoSPattern_DetectsDDoSAnomaly()
    {
        // Arrange - Create DDoS pattern (multiple sources, same destination)
        var traffic = new List<NetworkTrafficRecord>();
        for (int i = 0; i < 50; i++)
        {
            traffic.Add(CreateTrafficRecord($"10.0.0.{i}", "192.168.1.5", 80, 1024, "TCP"));
        }

        // Act
        var result = await _detector.AnalyzeTrafficAsync(traffic, _config);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Score >= _config.AnomalyThreshold);
        Assert.Equal(AnomalyType.DDoS, result.Type);
        Assert.Contains("DDoS", result.Description);
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
    }    [Fact]
    public async Task AnalyzeTrafficAsync_DetectsAnomalyWithCorrectProperties_WhenAnomalyDetected()
    {
        // Arrange
        var traffic = CreatePortScanTraffic();

        // Act
        var result = await _detector.AnalyzeTrafficAsync(traffic, _config);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(AnomalyType.PortScan, result.Type);
        Assert.True(result.Score >= _config.AnomalyThreshold);
        Assert.Contains("Port scan", result.Description);
        Assert.Equal("Ant Colony Optimization", result.Algorithm);
        
        // Note: Anomaly persistence is handled by calling controllers, not by the detector itself
    }

    [Theory]
    [InlineData(5, 2)]
    [InlineData(10, 3)]
    [InlineData(20, 5)]
    public async Task AnalyzeTrafficAsync_WithVariousAntCounts_ProducesConsistentResults(int antCount, int iterations)
    {
        // Arrange
        var config = new SwarmConfiguration
        {
            Id = Guid.NewGuid(),
            Name = "Variable Ant Test",
            MaxAnomalyScore = 100.0,
            AnomalyThreshold = 50.0,
            NumberOfAnts = antCount,
            MaxIterations = iterations,
            PheromoneEvaporationRate = 0.1,
            Alpha = 1.0,
            Beta = 2.0,
            Q = 100.0
        };

        var traffic = CreatePortScanTraffic();

        // Act
        var result = await _detector.AnalyzeTrafficAsync(traffic, config);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Score > 0);
        Assert.NotEqual(AnomalyType.Normal, result.Type);
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
}
