using SwarmID.Core.Models;
using SwarmID.Core.Repositories;

namespace SwarmID.Tests;

public class LiteDbAnomalyRepositoryTests : IDisposable
{
    private readonly LiteDbAnomalyRepository _repository;
    private readonly string _testDbPath;

    public LiteDbAnomalyRepositoryTests()
    {
        _testDbPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.db");
        _repository = new LiteDbAnomalyRepository(_testDbPath);
    }

    [Fact]
    public async Task SaveAnomalyAsync_ShouldAddAnomalyToDatabase()
    {
        // Arrange
        var anomaly = CreateTestAnomaly();

        // Act
        await _repository.SaveAnomalyAsync(anomaly);

        // Assert
        var retrievedAnomaly = await _repository.GetAnomalyByIdAsync(anomaly.Id.ToString());
        Assert.NotNull(retrievedAnomaly);
        Assert.Equal(anomaly.Id, retrievedAnomaly.Id);
        Assert.Equal(anomaly.Type, retrievedAnomaly.Type);
        Assert.Equal(anomaly.Score, retrievedAnomaly.Score);
    }

    [Fact]
    public async Task GetAnomalyByIdAsync_WithNonExistentId_ShouldReturnNull()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid().ToString();

        // Act
        var result = await _repository.GetAnomalyByIdAsync(nonExistentId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAnomaliesAsync_ShouldReturnAllAnomalies()
    {
        // Arrange
        var anomalies = new List<Anomaly>
        {
            CreateTestAnomaly(AnomalyType.PortScan),
            CreateTestAnomaly(AnomalyType.DDoS),
            CreateTestAnomaly(AnomalyType.CommandAndControl)
        };

        foreach (var anomaly in anomalies)
        {
            await _repository.SaveAnomalyAsync(anomaly);
        }

        // Act
        var result = await _repository.GetAnomaliesAsync();

        // Assert
        Assert.Equal(3, result.Count());
        Assert.Contains(result, a => a.Type == AnomalyType.PortScan);
        Assert.Contains(result, a => a.Type == AnomalyType.DDoS);
        Assert.Contains(result, a => a.Type == AnomalyType.CommandAndControl);
    }    [Fact]
    public async Task GetAnomaliesAsync_WithDateRange_ShouldReturnAnomaliesInRange()
    {
        // Arrange
        var baseDate = DateTime.UtcNow.Date;
        var anomaly1 = CreateTestAnomalyWithDate(baseDate.AddDays(-2)); // Should not be included
        var anomaly2 = CreateTestAnomalyWithDate(baseDate.AddDays(-1)); // Should be included
        var anomaly3 = CreateTestAnomalyWithDate(baseDate); // Should be included
        var anomaly4 = CreateTestAnomalyWithDate(baseDate.AddDays(1)); // Should not be included

        await _repository.SaveAnomalyAsync(anomaly1);
        await _repository.SaveAnomalyAsync(anomaly2);
        await _repository.SaveAnomalyAsync(anomaly3);
        await _repository.SaveAnomalyAsync(anomaly4);

        // Act
        var result = await _repository.GetAnomaliesAsync(baseDate.AddDays(-1), baseDate);

        // Assert
        Assert.Equal(2, result.Count());
        Assert.Contains(result, a => a.Id == anomaly2.Id);
        Assert.Contains(result, a => a.Id == anomaly3.Id);
    }

    [Fact]
    public async Task UpdateAnomalyAsync_ShouldUpdateExistingAnomaly()
    {
        // Arrange
        var anomaly = CreateTestAnomaly();
        await _repository.SaveAnomalyAsync(anomaly);

        // Modify the anomaly
        anomaly.Status = AnomalyStatus.Investigated;

        // Act
        await _repository.UpdateAnomalyAsync(anomaly);

        // Assert
        var updatedAnomaly = await _repository.GetAnomalyByIdAsync(anomaly.Id.ToString());
        Assert.NotNull(updatedAnomaly);
        Assert.Equal(AnomalyStatus.Investigated, updatedAnomaly.Status);
    }

    [Fact]
    public async Task DeleteAnomalyAsync_ShouldRemoveAnomalyFromDatabase()
    {
        // Arrange
        var anomaly = CreateTestAnomaly();
        await _repository.SaveAnomalyAsync(anomaly);

        // Verify it exists
        var existingAnomaly = await _repository.GetAnomalyByIdAsync(anomaly.Id.ToString());
        Assert.NotNull(existingAnomaly);

        // Act
        await _repository.DeleteAnomalyAsync(anomaly.Id.ToString());

        // Assert
        var deletedAnomaly = await _repository.GetAnomalyByIdAsync(anomaly.Id.ToString());
        Assert.Null(deletedAnomaly);
    }

    [Fact]
    public async Task GetAnomaliesAsync_WithTypeFilter_ShouldReturnOnlyMatchingTypes()
    {
        // Arrange
        var portScanAnomaly = CreateTestAnomaly(AnomalyType.PortScan);
        var ddosAnomaly = CreateTestAnomaly(AnomalyType.DDoS);
        var c2Anomaly = CreateTestAnomaly(AnomalyType.CommandAndControl);

        await _repository.SaveAnomalyAsync(portScanAnomaly);
        await _repository.SaveAnomalyAsync(ddosAnomaly);
        await _repository.SaveAnomalyAsync(c2Anomaly);

        // Act
        var portScanResults = await _repository.GetAnomaliesAsync(type: AnomalyType.PortScan);
        var ddosResults = await _repository.GetAnomaliesAsync(type: AnomalyType.DDoS);

        // Assert
        Assert.Single(portScanResults);
        Assert.Single(ddosResults);
        Assert.Equal(AnomalyType.PortScan, portScanResults.First().Type);
        Assert.Equal(AnomalyType.DDoS, ddosResults.First().Type);
    }

    [Fact]
    public async Task GetAnomaliesPagedAsync_WithTypeFilter_ShouldReturnOnlyMatchingTypes()
    {
        // Arrange
        var portScanAnomalies = new List<Anomaly>
        {
            CreateTestAnomaly(AnomalyType.PortScan),
            CreateTestAnomaly(AnomalyType.PortScan),
            CreateTestAnomaly(AnomalyType.PortScan)
        };
        var ddosAnomalies = new List<Anomaly>
        {
            CreateTestAnomaly(AnomalyType.DDoS),
            CreateTestAnomaly(AnomalyType.DDoS)
        };

        foreach (var anomaly in portScanAnomalies.Concat(ddosAnomalies))
        {
            await _repository.SaveAnomalyAsync(anomaly);
        }

        // Act
        var (portScanResults, portScanCount) = await _repository.GetAnomaliesPagedAsync(type: AnomalyType.PortScan, page: 1, pageSize: 10);
        var (ddosResults, ddosCount) = await _repository.GetAnomaliesPagedAsync(type: AnomalyType.DDoS, page: 1, pageSize: 10);

        // Assert
        Assert.Equal(3, portScanCount);
        Assert.Equal(2, ddosCount);
        Assert.Equal(3, portScanResults.Count());
        Assert.Equal(2, ddosResults.Count());
        Assert.All(portScanResults, a => Assert.Equal(AnomalyType.PortScan, a.Type));
        Assert.All(ddosResults, a => Assert.Equal(AnomalyType.DDoS, a.Type));
    }

    [Fact]
    public async Task GetAnomaliesCountAsync_WithTypeFilter_ShouldReturnCorrectCount()
    {
        // Arrange
        var anomalies = new List<Anomaly>
        {
            CreateTestAnomaly(AnomalyType.PortScan),
            CreateTestAnomaly(AnomalyType.PortScan),
            CreateTestAnomaly(AnomalyType.DDoS),
            CreateTestAnomaly(AnomalyType.CommandAndControl)
        };

        foreach (var anomaly in anomalies)
        {
            await _repository.SaveAnomalyAsync(anomaly);
        }

        // Act
        var totalCount = await _repository.GetAnomaliesCountAsync();
        var portScanCount = await _repository.GetAnomaliesCountAsync(type: AnomalyType.PortScan);
        var ddosCount = await _repository.GetAnomaliesCountAsync(type: AnomalyType.DDoS);

        // Assert
        Assert.Equal(4, totalCount);
        Assert.Equal(2, portScanCount);
        Assert.Equal(1, ddosCount);
    }

    private Anomaly CreateTestAnomaly(AnomalyType type = AnomalyType.PortScan)
    {
        return new Anomaly
        {
            Id = Guid.NewGuid(),
            DetectedAt = DateTime.UtcNow,
            Type = type,
            Score = 75.0,
            Description = $"Test {type} anomaly",
            SourceIPs = new List<string> { "192.168.1.100" },
            DestinationIPs = new List<string> { "10.0.0.5" },
            Ports = new List<int> { 80, 443, 22 },
            Status = AnomalyStatus.New,
            ConfigurationUsed = "Test Configuration"
        };
    }

    private Anomaly CreateTestAnomalyWithDate(DateTime detectedAt)
    {
        var anomaly = CreateTestAnomaly();
        anomaly.DetectedAt = detectedAt;
        return anomaly;
    }

    public void Dispose()
    {
        _repository?.Dispose();
        if (File.Exists(_testDbPath))
        {
            System.IO.File.Delete(_testDbPath);
        }
    }
}
