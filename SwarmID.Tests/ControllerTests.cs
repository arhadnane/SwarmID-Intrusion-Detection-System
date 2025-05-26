using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using SwarmID.Api.Controllers;
using SwarmID.Core.Interfaces;
using SwarmID.Core.Models;

namespace SwarmID.Tests;

public class AnomaliesControllerTests
{
    private readonly Mock<IAnomalyRepository> _mockRepository;
    private readonly Mock<ISwarmDetector> _mockSwarmDetector;
    private readonly Mock<ILogger<AnomaliesController>> _mockLogger;
    private readonly AnomaliesController _controller;

    public AnomaliesControllerTests()
    {
        _mockRepository = new Mock<IAnomalyRepository>();
        _mockSwarmDetector = new Mock<ISwarmDetector>();
        _mockLogger = new Mock<ILogger<AnomaliesController>>();
        _controller = new AnomaliesController(_mockRepository.Object, _mockSwarmDetector.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task GetAnomalies_ReturnsOkWithAnomalies()
    {
        // Arrange
        var anomalies = new List<Anomaly>
        {
            CreateTestAnomaly(AnomalyType.PortScan),
            CreateTestAnomaly(AnomalyType.DDoS)
        };
        _mockRepository.Setup(r => r.GetAnomaliesAsync(null, null, null)).ReturnsAsync(anomalies);

        // Act
        var result = await _controller.GetAnomalies();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedAnomalies = Assert.IsType<List<Anomaly>>(okResult.Value);
        Assert.Equal(2, returnedAnomalies.Count);
    }

    [Fact]
    public async Task GetAnomaly_WithValidId_ReturnsOkWithAnomaly()
    {
        // Arrange
        var anomaly = CreateTestAnomaly();
        _mockRepository.Setup(r => r.GetAnomalyByIdAsync(anomaly.Id.ToString())).ReturnsAsync(anomaly);

        // Act
        var result = await _controller.GetAnomaly(anomaly.Id.ToString());

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedAnomaly = Assert.IsType<Anomaly>(okResult.Value);
        Assert.Equal(anomaly.Id, returnedAnomaly.Id);
    }

    [Fact]
    public async Task GetAnomaly_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var invalidId = Guid.NewGuid().ToString();
        _mockRepository.Setup(r => r.GetAnomalyByIdAsync(invalidId)).ReturnsAsync((Anomaly?)null);

        // Act
        var result = await _controller.GetAnomaly(invalidId);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task UpdateAnomalyFeedback_WithValidData_ReturnsNoContent()
    {
        // Arrange
        var anomaly = CreateTestAnomaly();
        var updateRequest = new AnomalyFeedbackRequest
        {
            Status = AnomalyStatus.Investigated,
            IsActualAnomaly = false
        };

        _mockRepository.Setup(r => r.GetAnomalyByIdAsync(anomaly.Id.ToString())).ReturnsAsync(anomaly);
        _mockRepository.Setup(r => r.UpdateAnomalyAsync(It.IsAny<Anomaly>())).Returns(Task.CompletedTask);
        _mockSwarmDetector.Setup(s => s.UpdateWithFeedbackAsync(It.IsAny<Anomaly>(), It.IsAny<bool>()))
                          .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.UpdateAnomalyFeedback(anomaly.Id.ToString(), updateRequest);

        // Assert
        Assert.IsType<NoContentResult>(result);
        _mockRepository.Verify(r => r.UpdateAnomalyAsync(It.Is<Anomaly>(a => 
            a.Status == AnomalyStatus.Investigated)), Times.Once);
        _mockSwarmDetector.Verify(s => s.UpdateWithFeedbackAsync(anomaly, false), Times.Once);
    }

    [Fact]
    public async Task UpdateAnomalyFeedback_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var invalidId = Guid.NewGuid().ToString();
        var updateRequest = new AnomalyFeedbackRequest
        {
            Status = AnomalyStatus.Investigated,
            IsActualAnomaly = true
        };

        _mockRepository.Setup(r => r.GetAnomalyByIdAsync(invalidId)).ReturnsAsync((Anomaly?)null);

        // Act
        var result = await _controller.UpdateAnomalyFeedback(invalidId, updateRequest);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task DeleteAnomaly_WithValidId_ReturnsNoContent()
    {
        // Arrange
        var anomaly = CreateTestAnomaly();
        _mockRepository.Setup(r => r.GetAnomalyByIdAsync(anomaly.Id.ToString())).ReturnsAsync(anomaly);
        _mockRepository.Setup(r => r.DeleteAnomalyAsync(anomaly.Id.ToString())).Returns(Task.CompletedTask);

        // Act
        var result = await _controller.DeleteAnomaly(anomaly.Id.ToString());

        // Assert
        Assert.IsType<NoContentResult>(result);
        _mockRepository.Verify(r => r.DeleteAnomalyAsync(anomaly.Id.ToString()), Times.Once);
    }

    [Fact]
    public async Task DeleteAnomaly_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var invalidId = Guid.NewGuid().ToString();
        _mockRepository.Setup(r => r.GetAnomalyByIdAsync(invalidId)).ReturnsAsync((Anomaly?)null);

        // Act
        var result = await _controller.DeleteAnomaly(invalidId);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }    [Fact]
    public async Task GetAnomalies_WithTypeFilter_ReturnsFilteredAnomalies()
    {
        // Arrange
        var anomalies = new List<Anomaly>
        {
            CreateTestAnomaly(AnomalyType.PortScan),
            CreateTestAnomaly(AnomalyType.DDoS)
        };
        _mockRepository.Setup(r => r.GetAnomaliesAsync(null, null, AnomalyType.PortScan))
                       .ReturnsAsync(anomalies.Where(a => a.Type == AnomalyType.PortScan).ToList());

        // Act
        var result = await _controller.GetAnomalies(type: AnomalyType.PortScan);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedAnomalies = Assert.IsType<List<Anomaly>>(okResult.Value);
        Assert.Single(returnedAnomalies);
        Assert.Equal(AnomalyType.PortScan, returnedAnomalies.First().Type);
    }

    [Fact]
    public async Task GetAnomaliesPaged_WithTypeFilter_ReturnsFilteredPagedAnomalies()
    {
        // Arrange
        var anomalies = new List<Anomaly>
        {
            CreateTestAnomaly(AnomalyType.PortScan),
            CreateTestAnomaly(AnomalyType.PortScan),
            CreateTestAnomaly(AnomalyType.DDoS)
        };
        var filteredAnomalies = anomalies.Where(a => a.Type == AnomalyType.PortScan);
        _mockRepository.Setup(r => r.GetAnomaliesPagedAsync(null, null, AnomalyType.PortScan, 1, 20))
                      .ReturnsAsync((filteredAnomalies, 2));

        // Act
        var result = await _controller.GetAnomaliesPaged(type: AnomalyType.PortScan);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = okResult.Value;
        Assert.NotNull(response);
        
        // Check the structure using reflection since it's an anonymous type
        var dataProperty = response.GetType().GetProperty("Data");
        var paginationProperty = response.GetType().GetProperty("Pagination");
        
        Assert.NotNull(dataProperty);
        Assert.NotNull(paginationProperty);
        
        var data = dataProperty.GetValue(response) as IEnumerable<Anomaly>;
        Assert.NotNull(data);
        Assert.Equal(2, data.Count());
        Assert.All(data, a => Assert.Equal(AnomalyType.PortScan, a.Type));
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
}

public class TrafficControllerTests
{
    private readonly Mock<ISwarmDetector> _mockDetector;
    private readonly Mock<ITrafficCollector> _mockCollector;
    private readonly Mock<IAnomalyRepository> _mockRepository;
    private readonly Mock<ILogger<TrafficController>> _mockLogger;
    private readonly TrafficController _controller;

    public TrafficControllerTests()
    {
        _mockDetector = new Mock<ISwarmDetector>();
        _mockCollector = new Mock<ITrafficCollector>();
        _mockRepository = new Mock<IAnomalyRepository>();
        _mockLogger = new Mock<ILogger<TrafficController>>();
        _controller = new TrafficController(_mockCollector.Object, _mockDetector.Object, _mockRepository.Object, _mockLogger.Object);
    }    [Fact]
    public async Task StartMonitoring_ReturnsOkResult()
    {
        // Arrange
        _mockCollector.Setup(c => c.StartMonitoringAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act
        var result = await _controller.StartMonitoring();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        _mockCollector.Verify(c => c.StartMonitoringAsync(It.IsAny<CancellationToken>()), Times.Once);
    }    [Fact]
    public async Task StopMonitoring_ReturnsOkResult()
    {
        // Arrange
        _mockCollector.Setup(c => c.StopMonitoringAsync()).Returns(Task.CompletedTask);

        // Act
        var result = await _controller.StopMonitoring();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        _mockCollector.Verify(c => c.StopMonitoringAsync(), Times.Once);
    }
}
