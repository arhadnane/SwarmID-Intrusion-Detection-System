using System;
using System.IO;
using System.Threading.Tasks;
using SwarmID.TrafficAnalysis;
using SwarmID.Core.Models;
using SwarmID.Core.Algorithms;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("SwarmID PCAP Analysis Test");
        Console.WriteLine("=========================");
        
        var sampleDataPath = @"d:\Coding\VSCodeProject\Anomaly-Based Intrusion Detection System Using Swarm Intelligence\sample-data";
        var parser = new ZeekLogParser();
        var acoDetector = new AntColonyOptimizationDetector();
        var beeDetector = new BeeAlgorithmDetector();
        
        var pcapFiles = new[]
        {
            new { Name = "port-scan.pcap", Expected = "3-5 anomalies (port scanning)" },
            new { Name = "suspicious-traffic.pcap", Expected = "2-4 anomalies (DDoS patterns)" },
            new { Name = "normal-traffic.pcap", Expected = "0-1 anomalies (baseline traffic)" }
        };
        
        foreach (var pcapFile in pcapFiles)
        {
            var filePath = Path.Combine(sampleDataPath, pcapFile.Name);
            
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"❌ File not found: {filePath}");
                continue;
            }
            
            Console.WriteLine($"\n🔍 Testing {pcapFile.Name}");
            Console.WriteLine($"Expected: {pcapFile.Expected}");
            Console.WriteLine(new string('-', 50));
            
            try
            {
                // Parse PCAP file
                Console.WriteLine("📋 Parsing PCAP file...");
                var trafficRecords = await parser.ParseTrafficDataAsync(filePath, TrafficDataType.PcapFile);
                var recordsList = trafficRecords.ToList();
                
                Console.WriteLine($"✅ Parsed {recordsList.Count} traffic records");
                
                // Show sample traffic records
                if (recordsList.Count > 0)
                {
                    Console.WriteLine("📊 Sample traffic records:");
                    for (int i = 0; i < Math.Min(3, recordsList.Count); i++)
                    {
                        var record = recordsList[i];
                        Console.WriteLine($"   {i+1}. {record.SourceIP}:{record.SourcePort} → {record.DestinationIP}:{record.DestinationPort} ({record.Protocol})");
                    }
                }
                
                if (recordsList.Count == 0)
                {
                    Console.WriteLine("⚠️  No traffic records parsed - this could indicate an issue with PCAP parsing");
                    continue;
                }
                
                // Test ACO detector
                Console.WriteLine("\n🐜 Running Ant Colony Optimization detector...");
                var acoAnomalies = await acoDetector.DetectAnomaliesAsync(recordsList);
                var acoAnomaliesList = acoAnomalies.ToList();
                
                Console.WriteLine($"✅ ACO detected {acoAnomaliesList.Count} anomalies");
                
                // Test Bee Algorithm detector
                Console.WriteLine("🐝 Running Bee Algorithm detector...");
                var beeAnomalies = await beeDetector.DetectAnomaliesAsync(recordsList);
                var beeAnomaliesList = beeAnomalies.ToList();
                
                Console.WriteLine($"✅ Bee Algorithm detected {beeAnomaliesList.Count} anomalies");
                
                // Combine and analyze results
                var allAnomalies = acoAnomaliesList.Concat(beeAnomaliesList).ToList();
                var uniqueAnomalies = allAnomalies.GroupBy(a => new { a.Type, a.Description }).Select(g => g.First()).ToList();
                
                Console.WriteLine($"\n📈 Analysis Results:");
                Console.WriteLine($"   Total unique anomalies: {uniqueAnomalies.Count}");
                
                foreach (var anomaly in uniqueAnomalies.Take(5)) // Show first 5 anomalies
                {
                    Console.WriteLine($"\n   🚨 Anomaly: {anomaly.Type}");
                    Console.WriteLine($"      Score: {anomaly.Score:F2}");
                    Console.WriteLine($"      Description: {anomaly.Description}");
                    
                    // Check IP extraction
                    if (anomaly.SourceIPs != null && anomaly.SourceIPs.Count > 0)
                    {
                        Console.WriteLine($"      ✅ Source IPs: {string.Join(", ", anomaly.SourceIPs)}");
                    }
                    else
                    {
                        Console.WriteLine($"      ❌ Source IPs: N/A (No IPs extracted)");
                    }
                    
                    if (anomaly.DestinationIPs != null && anomaly.DestinationIPs.Count > 0)
                    {
                        Console.WriteLine($"      ✅ Destination IPs: {string.Join(", ", anomaly.DestinationIPs)}");
                    }
                    else
                    {
                        Console.WriteLine($"      ❌ Destination IPs: N/A (No IPs extracted)");
                    }
                    
                    if (anomaly.Ports != null && anomaly.Ports.Count > 0)
                    {
                        Console.WriteLine($"      ✅ Ports: {string.Join(", ", anomaly.Ports)}");
                    }
                    else
                    {
                        Console.WriteLine($"      ❌ Ports: N/A (No ports extracted)");
                    }
                }
                
                // Check for any anomalies with missing IP data
                var anomaliesWithoutIPs = uniqueAnomalies.Where(a => 
                    (a.SourceIPs == null || a.SourceIPs.Count == 0) && 
                    (a.DestinationIPs == null || a.DestinationIPs.Count == 0)).ToList();
                
                if (anomaliesWithoutIPs.Count > 0)
                {
                    Console.WriteLine($"\n⚠️  Found {anomaliesWithoutIPs.Count} anomalies without IP addresses:");
                    foreach (var anomaly in anomaliesWithoutIPs.Take(3))
                    {
                        Console.WriteLine($"   - {anomaly.Type}: {anomaly.Description}");
                    }
                }
                else if (uniqueAnomalies.Count > 0)
                {
                    Console.WriteLine($"\n✅ All detected anomalies have IP addresses properly extracted!");
                }
                
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error testing {pcapFile.Name}: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
        
        Console.WriteLine("\n🏁 Test completed!");
        Console.WriteLine("\nPress any key to exit...");
        Console.ReadKey();
    }
}
