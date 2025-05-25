using System;
using System.IO;
using System.Threading.Tasks;
using SwarmID.TrafficAnalysis;
using SwarmID.Core.Interfaces;
using SwarmID.Core.Algorithms;
using SwarmID.Core.Repositories;

Console.WriteLine("=== Full Anomaly Detection Test ===");

// Initialize components
var parser = new ZeekLogParser();
var detector = new BeeAlgorithmDetector();
var repository = new LiteDbAnomalyRepository("test_anomalies.db");

// Test with suspicious PCAP file (most likely to generate anomalies)
string[] testFiles = {
    @"..\sample-data\port-scan.pcap", 
    @"..\sample-data\suspicious-traffic.pcap"
};

foreach (var pcapFile in testFiles)
{
    Console.WriteLine($"\n--- Processing: {pcapFile} ---");
    
    if (!File.Exists(pcapFile))
    {
        Console.WriteLine($"File not found: {pcapFile}");
        continue;
    }

    try
    {
        // Parse traffic data
        var records = await parser.ParseTrafficDataAsync(pcapFile, TrafficDataType.PcapFile);
        Console.WriteLine($"Parsed {records.Count()} traffic records");
        
        // Run anomaly detection
        var anomalies = await detector.DetectAnomaliesAsync(records);
        Console.WriteLine($"Detected {anomalies.Count()} anomalies");
        
        // Display anomaly details
        int count = 0;
        foreach (var anomaly in anomalies.Take(5))
        {
            count++;
            Console.WriteLine($"\n  Anomaly {count}:");
            Console.WriteLine($"    ID: {anomaly.Id}");
            Console.WriteLine($"    Type: {anomaly.Type}");
            Console.WriteLine($"    Source IP: {anomaly.SourceIP ?? "N/A"}");
            Console.WriteLine($"    Destination IP: {anomaly.DestinationIP ?? "N/A"}");
            Console.WriteLine($"    Source Port: {anomaly.SourcePort}");
            Console.WriteLine($"    Destination Port: {anomaly.DestinationPort}");
            Console.WriteLine($"    Protocol: {anomaly.Protocol ?? "Unknown"}");
            Console.WriteLine($"    Score: {anomaly.Score:F2}");
            Console.WriteLine($"    Confidence: {anomaly.Confidence:F2}");
            Console.WriteLine($"    Detected At: {anomaly.DetectedAt}");
            Console.WriteLine($"    Description: {anomaly.Description ?? "No description"}");
            
            // Save to database
            await repository.SaveAnomalyAsync(anomaly);
        }
        
        if (anomalies.Count() > 5)
        {
            Console.WriteLine($"    ... and {anomalies.Count() - 5} more anomalies");
            
            // Save remaining anomalies
            foreach (var anomaly in anomalies.Skip(5))
            {
                await repository.SaveAnomalyAsync(anomaly);
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error processing {pcapFile}: {ex.Message}");
        Console.WriteLine($"Stack trace: {ex.StackTrace}");
    }
}

// Check what's saved in the database
Console.WriteLine("\n--- Database Check ---");
try
{
    var allAnomalies = await repository.GetAnomaliesAsync();
    Console.WriteLine($"Total anomalies in database: {allAnomalies.Count()}");
    
    var recentAnomalies = allAnomalies.Take(3);
    foreach (var anomaly in recentAnomalies)
    {
        Console.WriteLine($"DB Anomaly - Type: {anomaly.Type}, Source: {anomaly.SourceIP ?? "N/A"}, Dest: {anomaly.DestinationIP ?? "N/A"}");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Error checking database: {ex.Message}");
}

Console.WriteLine("\n=== Test Complete ===");
Console.WriteLine("Press any key to exit...");
Console.ReadKey();
