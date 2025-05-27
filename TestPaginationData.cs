using System;
using System.Threading.Tasks;
using SwarmID.Core.Models;
using SwarmID.Core.Repositories;

Console.WriteLine("=== Pagination Test Data Generator ===");

// Initialize repository
var repository = new LiteDbAnomalyRepository("Data/anomalies.db");

// Generate test anomalies
var random = new Random();
var anomalyTypes = Enum.GetValues<AnomalyType>();
var anomalyStatuses = Enum.GetValues<AnomalyStatus>();
var algorithms = new[] { "Ant Colony Optimization", "Bee Algorithm" };

Console.WriteLine("Generating 75 test anomalies...");

for (int i = 0; i < 75; i++)
{
    var anomaly = new Anomaly
    {
        Id = Guid.NewGuid(),
        DetectedAt = DateTime.UtcNow.AddHours(-random.Next(1, 168)), // Last week
        Type = anomalyTypes[random.Next(anomalyTypes.Length)],
        Score = 50 + random.NextDouble() * 50, // 50-100
        Description = $"Test anomaly {i + 1} - {anomalyTypes[random.Next(anomalyTypes.Length)]} pattern detected",
        SourceIPs = new List<string> { $"192.168.{random.Next(1, 10)}.{random.Next(1, 255)}" },
        DestinationIPs = new List<string> { $"10.0.{random.Next(1, 10)}.{random.Next(1, 255)}" },
        Ports = new List<int> { random.Next(1, 65535), random.Next(1, 65535) },
        Status = anomalyStatuses[random.Next(anomalyStatuses.Length)],
        ConfigurationUsed = "Test Configuration",
        Algorithm = algorithms[random.Next(algorithms.Length)]
    };

    await repository.SaveAnomalyAsync(anomaly);
    
    if ((i + 1) % 10 == 0)
        Console.WriteLine($"Created {i + 1} anomalies...");
}

// Check total count
var totalCount = await repository.GetAnomaliesCountAsync();
Console.WriteLine($"\nTotal anomalies in database: {totalCount}");

// Test pagination
Console.WriteLine("\n=== Testing Pagination ===");
var (page1, total) = await repository.GetAnomaliesPagedAsync(page: 1, pageSize: 10);
Console.WriteLine($"Page 1: {page1.Count()} anomalies (Total: {total})");

var (page2, _) = await repository.GetAnomaliesPagedAsync(page: 2, pageSize: 10);
Console.WriteLine($"Page 2: {page2.Count()} anomalies");

var (lastPage, _) = await repository.GetAnomaliesPagedAsync(page: 8, pageSize: 10);
Console.WriteLine($"Page 8: {lastPage.Count()} anomalies");

Console.WriteLine("\n✅ Test data generation complete!");
Console.WriteLine("Open http://localhost:5121/anomalies to test pagination UI");
