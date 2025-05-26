using Moq;
using SwarmID.Core.Interfaces;
using SwarmID.Core.Models;
using SwarmID.Core.Repositories;
using SwarmID.Api.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Net;
using Xunit;

namespace SwarmID.Tests
{
    public class PaginationTests : IDisposable
    {
        private readonly LiteDbAnomalyRepository _repository;
        private readonly string _tempDbPath;

        public PaginationTests()
        {
            _tempDbPath = Path.GetTempFileName();
            _repository = new LiteDbAnomalyRepository($"Filename={_tempDbPath}");
        }

        public void Dispose()
        {
            _repository?.Dispose();
            if (File.Exists(_tempDbPath))
                File.Delete(_tempDbPath);
        }        [Fact]
        public async Task GetAnomaliesPagedAsync_ShouldReturnCorrectPageSize()
        {
            // Arrange
            var now = DateTime.UtcNow;
            for (int i = 0; i < 25; i++)
            {
                var anomaly = new Anomaly
                {
                    Id = Guid.NewGuid(),
                    DetectedAt = now.AddMinutes(-i),
                    Type = AnomalyType.PortScan,
                    Score = 75.0 + i,
                    Status = AnomalyStatus.New,
                    SourceIPs = new List<string> { $"192.168.1.{i}" },
                    DestinationIPs = new List<string> { "10.0.0.1" },
                    Ports = new List<int> { 80, 443 },
                    Algorithm = "TestAlgorithm",
                    Description = $"Test anomaly {i}",
                    ConfigurationUsed = "default"
                };
                await _repository.SaveAnomalyAsync(anomaly);
            }

            // Act
            var (anomalies, totalCount) = await _repository.GetAnomaliesPagedAsync(
                from: now.AddHours(-1), 
                to: null, 
                page: 1, 
                pageSize: 10);

            // Assert
            Assert.Equal(25, totalCount);
            Assert.Equal(10, anomalies.Count());
            
            // Verify they are ordered by detection time (newest first)
            var anomaliesList = anomalies.ToList();
            for (int i = 0; i < anomaliesList.Count - 1; i++)
            {
                Assert.True(anomaliesList[i].DetectedAt >= anomaliesList[i + 1].DetectedAt);
            }
        }        [Fact]
        public async Task GetAnomaliesPagedAsync_ShouldReturnCorrectSecondPage()
        {
            // Arrange
            var now = DateTime.UtcNow;
            for (int i = 0; i < 25; i++)
            {
                var anomaly = new Anomaly
                {
                    Id = Guid.NewGuid(),
                    DetectedAt = now.AddMinutes(-i),
                    Type = AnomalyType.PortScan,
                    Score = 75.0 + i,
                    Status = AnomalyStatus.New,
                    SourceIPs = new List<string> { $"192.168.1.{i}" },
                    DestinationIPs = new List<string> { "10.0.0.1" },
                    Ports = new List<int> { 80, 443 },
                    Algorithm = "TestAlgorithm",
                    Description = $"Test anomaly {i}",
                    ConfigurationUsed = "default"
                };
                await _repository.SaveAnomalyAsync(anomaly);
            }

            // Act
            var (page1, _) = await _repository.GetAnomaliesPagedAsync(page: 1, pageSize: 10);
            var (page2, totalCount) = await _repository.GetAnomaliesPagedAsync(page: 2, pageSize: 10);

            // Assert
            Assert.Equal(25, totalCount);
            Assert.Equal(10, page1.Count());
            Assert.Equal(10, page2.Count());
            
            // Verify no overlap between pages
            var page1Ids = page1.Select(a => a.Id).ToHashSet();
            var page2Ids = page2.Select(a => a.Id).ToHashSet();
            Assert.False(page1Ids.Intersect(page2Ids).Any());
        }        [Fact]
        public async Task GetAnomaliesCountAsync_ShouldReturnCorrectCount()
        {
            // Arrange
            var now = DateTime.UtcNow;
            for (int i = 0; i < 15; i++)
            {
                var anomaly = new Anomaly
                {
                    Id = Guid.NewGuid(),
                    DetectedAt = now.AddMinutes(-i),
                    Type = AnomalyType.PortScan,
                    Score = 75.0,
                    Status = AnomalyStatus.New,
                    SourceIPs = new List<string> { "192.168.1.1" },
                    DestinationIPs = new List<string> { "10.0.0.1" },
                    Ports = new List<int> { 80 },
                    Algorithm = "TestAlgorithm",
                    Description = "Test anomaly",
                    ConfigurationUsed = "default"
                };
                await _repository.SaveAnomalyAsync(anomaly);
            }

            // Act
            var count = await _repository.GetAnomaliesCountAsync();
            var countWithDateFilter = await _repository.GetAnomaliesCountAsync(from: now.AddMinutes(-30));

            // Assert
            Assert.Equal(15, count);
            Assert.True(countWithDateFilter <= 15);
            Assert.True(countWithDateFilter > 0);
        }        [Fact]
        public async Task AnomaliesController_GetPagedAnomalies_ShouldReturnPaginatedResponse()
        {
            // Arrange
            var mockRepo = new Mock<IAnomalyRepository>();
            var mockSwarmDetector = new Mock<ISwarmDetector>();
            var mockLogger = new Mock<ILogger<AnomaliesController>>();
            var controller = new AnomaliesController(mockRepo.Object, mockSwarmDetector.Object, mockLogger.Object);
            
            var anomalies = new List<Anomaly>
            {
                new Anomaly 
                { 
                    Id = Guid.NewGuid(), 
                    DetectedAt = DateTime.UtcNow, 
                    Type = AnomalyType.PortScan,
                    Score = 85.0,
                    Status = AnomalyStatus.New,
                    SourceIPs = new List<string> { "192.168.1.1" },
                    DestinationIPs = new List<string> { "10.0.0.1" },
                    Ports = new List<int> { 80 },
                    Algorithm = "TestAlgorithm",
                    Description = "Test anomaly",
                    ConfigurationUsed = "default"
                }
            };            mockRepo.Setup(r => r.GetAnomaliesPagedAsync(
                It.IsAny<DateTime?>(), 
                It.IsAny<DateTime?>(), 
                It.IsAny<AnomalyType?>(),
                It.IsAny<int>(), 
                It.IsAny<int>()))
                .ReturnsAsync((anomalies, 1));

            // Act
            var result = await controller.GetAnomaliesPaged(page: 1, pageSize: 10);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(200, okResult.StatusCode);
            
            var response = okResult.Value;
            Assert.NotNull(response);
        }
    }
}
