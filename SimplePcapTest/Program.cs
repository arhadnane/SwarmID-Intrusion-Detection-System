using System;
using System.IO;
using System.Threading.Tasks;
using SwarmID.TrafficAnalysis;
using SwarmID.Core.Interfaces;

namespace SimplePcapTest
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("=== PCAP File Analysis Test ===");
            
            // Test with sample PCAP files
            string[] testFiles = {
                @"..\sample-data\normal-traffic.pcap",
                @"..\sample-data\port-scan.pcap", 
                @"..\sample-data\suspicious-traffic.pcap"
            };

            var parser = new ZeekLogParser();

            foreach (var pcapFile in testFiles)
            {
                Console.WriteLine($"\n--- Testing: {pcapFile} ---");
                
                if (!File.Exists(pcapFile))
                {
                    Console.WriteLine($"File not found: {pcapFile}");
                    continue;
                }                try
                {
                    // Parse the PCAP file using the file path
                    var records = await parser.ParseTrafficDataAsync(pcapFile, TrafficDataType.PcapFile);
                    
                    Console.WriteLine($"Parsed {records.Count()} records");
                    
                    // Display first few records with IP information
                    int count = 0;
                    foreach (var record in records.Take(10))
                    {
                        count++;
                        Console.WriteLine($"  Record {count}:");
                        Console.WriteLine($"    Source IP: {record.SourceIP ?? "N/A"}");
                        Console.WriteLine($"    Destination IP: {record.DestinationIP ?? "N/A"}");
                        Console.WriteLine($"    Source Port: {record.SourcePort}");
                        Console.WriteLine($"    Destination Port: {record.DestinationPort}");
                        Console.WriteLine($"    Protocol: {record.Protocol ?? "Unknown"}");
                        Console.WriteLine($"    Packet Size: {record.PacketSize}");
                        Console.WriteLine($"    Timestamp: {record.Timestamp}");
                        
                        // Show any additional features
                        if (record.AdditionalFeatures?.Any() == true)
                        {
                            Console.WriteLine("    Additional Features:");
                            foreach (var feature in record.AdditionalFeatures)
                            {
                                Console.WriteLine($"      {feature.Key}: {feature.Value}");
                            }
                        }
                        Console.WriteLine();
                    }
                    
                    if (records.Count() > 10)
                    {
                        Console.WriteLine($"    ... and {records.Count() - 10} more records");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing {pcapFile}: {ex.Message}");
                    Console.WriteLine($"Stack trace: {ex.StackTrace}");
                }
            }
            
            Console.WriteLine("\n=== Test Complete ===");
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}
