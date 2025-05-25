# PCAP File Analysis Guide

## Overview

SwarmID now supports PCAP (Packet Capture) file analysis using the industry-standard SharpPcap and PacketDotNet libraries. This functionality allows you to analyze captured network traffic from tools like Wireshark, tcpdump, or any network monitoring solution that produces PCAP files.

## Supported Features

### Network Protocols
- **TCP**: Full TCP packet analysis with sequence numbers, flags, and window size
- **UDP**: UDP packet parsing with length and checksum information  
- **ICMP**: ICMP packet analysis with type codes and checksums
- **IP**: IPv4 packet parsing with header information, TTL, and addressing

### Extracted Information
For each packet, the parser extracts:
- Source and destination IP addresses
- Source and destination ports (for TCP/UDP)
- Protocol type
- Packet size and timing information
- Protocol-specific metadata

## Usage Examples

### Basic PCAP File Parsing

```csharp
using SwarmID.TrafficAnalysis;
using SwarmID.Core.Models;

// Initialize the parser
var parser = new ZeekLogParser();

// Parse a PCAP file
var records = await parser.ParseTrafficDataAsync("network_traffic.pcap", TrafficDataType.PcapFile);

// Process the results
foreach (var record in records)
{
    Console.WriteLine($"Traffic: {record.SourceIP}:{record.SourcePort} -> {record.DestinationIP}:{record.DestinationPort}");
    Console.WriteLine($"Protocol: {record.Protocol}, Size: {record.PacketSize} bytes");
    Console.WriteLine($"Timestamp: {record.Timestamp}");
    
    // Access protocol-specific information
    if (record.AdditionalFeatures.ContainsKey("tcp_flags"))
    {
        Console.WriteLine($"TCP Flags: {record.AdditionalFeatures["tcp_flags"]}");
    }
}
```

### Integration with Swarm Intelligence Detection

```csharp
// Parse PCAP and run through anomaly detection
var parser = new ZeekLogParser();
var acoDetector = new AntColonyOptimizationDetector();
var beeDetector = new BeeAlgorithmDetector();

var trafficRecords = await parser.ParseTrafficDataAsync("suspicious_traffic.pcap", TrafficDataType.PcapFile);

foreach (var record in trafficRecords)
{
    // Run through both swarm intelligence algorithms
    var acoResult = await acoDetector.DetectAnomaliesAsync(new[] { record });
    var beeResult = await beeDetector.DetectAnomaliesAsync(new[] { record });
    
    // Combine results and check for anomalies
    if (acoResult.Any() || beeResult.Any())
    {
        Console.WriteLine($"Potential anomaly detected in traffic from {record.SourceIP}");
    }
}
```

### API Integration

```csharp
// Upload and analyze PCAP file via API
[HttpPost("upload-pcap")]
public async Task<IActionResult> UploadPcapFile(IFormFile pcapFile)
{
    if (pcapFile == null || pcapFile.Length == 0)
        return BadRequest("No file uploaded");

    var tempPath = Path.GetTempFileName();
    using (var stream = new FileStream(tempPath, FileMode.Create))
    {
        await pcapFile.CopyToAsync(stream);
    }

    try
    {
        var parser = new ZeekLogParser();
        var records = await parser.ParseTrafficDataAsync(tempPath, TrafficDataType.PcapFile);
        
        // Store records and run anomaly detection
        foreach (var record in records)
        {
            await _trafficRepository.AddAsync(record);
            // Trigger real-time anomaly detection...
        }

        return Ok(new { ProcessedRecords = records.Count() });
    }
    finally
    {
        File.Delete(tempPath);
    }
}
```

## Protocol-Specific Information

### TCP Packets
Additional features extracted:
- `tcp_sequence`: TCP sequence number
- `tcp_acknowledgment`: TCP acknowledgment number  
- `tcp_flags`: TCP flags (SYN, ACK, FIN, etc.)
- `tcp_window_size`: TCP window size

### UDP Packets  
Additional features extracted:
- `udp_length`: UDP packet length
- `udp_checksum`: UDP checksum value

### ICMP Packets
Additional features extracted:
- `icmp_type`: ICMP message type
- `icmp_checksum`: ICMP checksum value

### IP Headers
Additional features for all packets:
- `ip_version`: IP version (usually 4)
- `ip_header_length`: IP header length
- `ip_total_length`: Total IP packet length
- `ip_ttl`: Time To Live value
- `pcap_parsed`: Flag indicating packet was parsed from PCAP

## Best Practices

### File Size Considerations
- Large PCAP files may consume significant memory during processing
- Consider processing large files in chunks or using streaming approaches
- Monitor memory usage for files larger than 100MB

### Performance Optimization
- PCAP parsing is CPU-intensive; consider async processing for multiple files
- Use file validation before parsing to ensure PCAP format compatibility
- Implement progress reporting for long-running analysis tasks

### Error Handling
```csharp
try
{
    var records = await parser.ParseTrafficDataAsync(pcapPath, TrafficDataType.PcapFile);
}
catch (FileNotFoundException ex)
{
    Console.WriteLine($"PCAP file not found: {ex.Message}");
}
catch (InvalidDataException ex)
{
    Console.WriteLine($"Invalid PCAP format: {ex.Message}");
}
catch (Exception ex)
{
    Console.WriteLine($"Error parsing PCAP: {ex.Message}");
}
```

## Supported PCAP Formats

SwarmID supports standard PCAP formats:
- **Classic PCAP** (.pcap files)
- **PCAP-NG** (.pcapng files) 
- **Compressed PCAP** (with appropriate decompression)

## Integration with Network Tools

### Wireshark Integration
1. Capture traffic in Wireshark
2. Save as .pcap format
3. Upload to SwarmID for analysis

### tcpdump Integration
```bash
# Capture traffic with tcpdump
tcpdump -i eth0 -w capture.pcap

# Analyze with SwarmID
# Upload capture.pcap through the dashboard or API
```

### Network Monitoring Integration
- Integrate PCAP export from network monitoring tools
- Automate PCAP file processing with SwarmID API
- Set up scheduled analysis of captured traffic

## Limitations

- Currently supports IPv4 traffic (IPv6 support planned)
- Memory usage scales with file size
- Processing time depends on packet count and complexity
- Some specialized protocols may not have detailed parsing

## Troubleshooting

### Common Issues
1. **"Invalid PCAP format"**: Ensure file is a valid PCAP/PCAPNG format
2. **High memory usage**: Process large files in smaller chunks
3. **Missing packets**: Check for file corruption or incomplete captures
4. **Slow processing**: Consider file size and available system resources

For support and advanced configuration, refer to the main SwarmID documentation.
