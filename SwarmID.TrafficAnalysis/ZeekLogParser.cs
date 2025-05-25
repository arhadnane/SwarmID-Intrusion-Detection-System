using SwarmID.Core.Models;
using SwarmID.Core.Interfaces;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Globalization;
using SharpPcap;
using SharpPcap.LibPcap;
using PacketDotNet;
using PacketDotNet.Utils;

namespace SwarmID.TrafficAnalysis;

/// <summary>
/// Zeek log parser for network traffic analysis with dual mode support
/// </summary>
public class ZeekLogParser : ITrafficCollector
{
    public event EventHandler<NetworkTrafficRecord>? TrafficDataReceived;
    private bool _isMonitoring = false;
    private CancellationTokenSource? _monitoringCancellation;
    private LibPcapLiveDevice? _activeDevice;
    private TrafficMonitoringMode _monitoringMode = TrafficMonitoringMode.Simulation;
    private string? _selectedInterface;

    public async Task<IEnumerable<NetworkTrafficRecord>> ParseTrafficDataAsync(string filePath, TrafficDataType dataType)
    {
        var records = new List<NetworkTrafficRecord>();

        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Traffic data file not found: {filePath}");

        switch (dataType)
        {
            case TrafficDataType.ZeekLogs:
                records.AddRange(await ParseZeekLogsAsync(filePath));
                break;
            case TrafficDataType.SnortAlerts:
                records.AddRange(await ParseSnortAlertsAsync(filePath));
                break;            case TrafficDataType.PcapFile:
                records.AddRange(await ParsePcapFileAsync(filePath));
                break;
            default:
                throw new ArgumentException($"Unsupported data type: {dataType}");
        }

        return records;
    }

    public async Task StartMonitoringAsync(CancellationToken cancellationToken = default)
    {
        if (_isMonitoring)
            return;

        _isMonitoring = true;
        _monitoringCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        if (_monitoringMode == TrafficMonitoringMode.Simulation)
        {
            // Simulate real-time monitoring for demo purposes
            _ = Task.Run(async () =>
            {
                var random = new Random();

                while (!_monitoringCancellation.Token.IsCancellationRequested)
                {
                    try
                    {
                        // Generate simulated traffic data
                        var record = GenerateSimulatedTrafficRecord(random);
                        TrafficDataReceived?.Invoke(this, record);

                        await Task.Delay(1000, _monitoringCancellation.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
            }, _monitoringCancellation.Token);
        }
        else if (_monitoringMode == TrafficMonitoringMode.RealTime)
        {
            // Real-time traffic monitoring using LibPcap
            _ = Task.Run(() =>
            {
                try
                {
                    if (_activeDevice == null)
                        throw new InvalidOperationException("No active network device selected for real-time monitoring.");

                    _activeDevice.OnPacketArrival += (sender, e) =>
                    {
                        try
                        {
                            var record = ParsePacket(e.GetPacket());
                            if (record != null)
                            {
                                TrafficDataReceived?.Invoke(this, record);
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error parsing packet: {ex.Message}");
                        }
                    };

                    _activeDevice.Open();
                    _activeDevice.Capture();
                    _activeDevice.Close();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error during real-time monitoring: {ex.Message}");
                }
            }, _monitoringCancellation.Token);
        }

        await Task.CompletedTask;
    }

    public async Task StopMonitoringAsync()
    {
        _isMonitoring = false;
        _monitoringCancellation?.Cancel();
        _monitoringCancellation?.Dispose();
        await Task.CompletedTask;
    }

    private async Task<IEnumerable<NetworkTrafficRecord>> ParseZeekLogsAsync(string filePath)
    {
        var records = new List<NetworkTrafficRecord>();
        var lines = await File.ReadAllLinesAsync(filePath);

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                continue;

            try
            {
                var record = ParseZeekLogLine(line);
                if (record != null)
                    records.Add(record);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing Zeek log line: {ex.Message}");
            }
        }

        return records;
    }    private NetworkTrafficRecord? ParseZeekLogLine(string line)
    {
        // Basic Zeek conn.log format parsing
        // Format: ts id.orig_h id.orig_p id.resp_h id.resp_p proto service duration orig_bytes resp_bytes conn_state local_orig local_resp missed_bytes history
        
        var fields = line.Split('\t');
        if (fields.Length < 15)
            return null;try
        {
            Console.WriteLine($"ParseZeekLogLine: Attempting to parse fields: ts={fields[0]}, src={fields[1]}, sport={fields[2]}, dst={fields[3]}, dport={fields[4]}, proto={fields[5]}");            var record = new NetworkTrafficRecord
            {
                Id = Guid.NewGuid(),
                Timestamp = DateTimeOffset.FromUnixTimeSeconds((long)double.Parse(fields[0], CultureInfo.InvariantCulture)).DateTime,
                SourceIP = fields[1],
                SourcePort = int.TryParse(fields[2], out var srcPort) ? srcPort : 0,
                DestinationIP = fields[3],
                DestinationPort = int.TryParse(fields[4], out var dstPort) ? dstPort : 0,
                Protocol = fields[5],
                Duration = double.TryParse(fields[7], NumberStyles.Float, CultureInfo.InvariantCulture, out var duration) ? duration : 0,
                PacketSize = int.TryParse(fields[8], out var origBytes) ? origBytes : 0,
                AdditionalFeatures = new Dictionary<string, object>
                {
                    ["service"] = fields.Length > 6 ? fields[6] : "",
                    ["conn_state"] = fields.Length > 10 ? fields[10] : "",
                    ["resp_bytes"] = long.TryParse(fields.Length > 9 ? fields[9] : "0", out var respBytes) ? respBytes : 0
                }
            };
            Console.WriteLine($"ParseZeekLogLine: Successfully created record for {record.SourceIP}:{record.SourcePort} -> {record.DestinationIP}:{record.DestinationPort}");
            return record;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ParseZeekLogLine: Exception occurred: {ex.Message}");
            return null;
        }
    }

    private async Task<IEnumerable<NetworkTrafficRecord>> ParseSnortAlertsAsync(string filePath)
    {
        var records = new List<NetworkTrafficRecord>();
        var content = await File.ReadAllTextAsync(filePath);
        
        // Split by alert boundaries (lines starting with [**)
        var alertBlocks = content.Split(new[] { "[**]" }, StringSplitOptions.RemoveEmptyEntries)
                                .Where(block => !string.IsNullOrWhiteSpace(block))
                                .ToArray();

        foreach (var block in alertBlocks)
        {
            try
            {
                var record = ParseSnortAlert(block);
                if (record != null)
                    records.Add(record);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing Snort alert: {ex.Message}");
            }
        }

        return records;
    }

    private NetworkTrafficRecord? ParseSnortAlert(string alertBlock)
    {
        // Parse Snort alert format from multi-line block
        var regex = new Regex(@"(\d+/\d+)-(\d+:\d+:\d+\.\d+)\s+(\d+\.\d+\.\d+\.\d+):(\d+)\s+->\s+(\d+\.\d+\.\d+\.\d+):(\d+)\s*(\w+)?", RegexOptions.Multiline | RegexOptions.IgnoreCase);
        var match = regex.Match(alertBlock);

        if (!match.Success)
            return null;

        try
        {
            var dateStr = match.Groups[1].Value;
            var timeStr = match.Groups[2].Value;
            var srcIp = match.Groups[3].Value;
            var srcPort = int.Parse(match.Groups[4].Value);
            var dstIp = match.Groups[5].Value;
            var dstPort = int.Parse(match.Groups[6].Value);
            var protocol = match.Groups[7].Success ? match.Groups[7].Value.ToLower() : "tcp";

            // Parse timestamp (assuming current year)
            var year = DateTime.Now.Year;
            var timestamp = DateTime.ParseExact($"{year}/{dateStr}-{timeStr}", "yyyy/M/d-H:m:s.ffffff", null);

            return new NetworkTrafficRecord
            {
                Id = Guid.NewGuid(),
                Timestamp = timestamp,
                SourceIP = srcIp,
                SourcePort = srcPort,
                DestinationIP = dstIp,
                DestinationPort = dstPort,
                Protocol = protocol,
                Duration = 0,
                PacketSize = 0,
                AdditionalFeatures = new Dictionary<string, object>
                {
                    ["alert_message"] = alertBlock.Trim(),
                    ["is_alert"] = true
                }
            };
        }
        catch (Exception)
        {
            return null;        }
    }

    private async Task<IEnumerable<NetworkTrafficRecord>> ParsePcapFileAsync(string filePath)
    {
        var records = new List<NetworkTrafficRecord>();

        try
        {
            using var device = new CaptureFileReaderDevice(filePath);
            device.Open();

            await Task.Run(() =>
            {
                device.OnPacketArrival += (sender, e) =>
                {
                    try
                    {
                        var record = ParsePacket(e.GetPacket());
                        if (record != null)
                        {
                            records.Add(record);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error parsing packet: {ex.Message}");
                    }
                };

                device.Capture();
            });

            device.Close();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading PCAP file: {ex.Message}");
            throw;
        }

        return records;
    }

    private NetworkTrafficRecord? ParsePacket(RawCapture rawPacket)
    {
        try
        {
            var packet = Packet.ParsePacket(rawPacket.LinkLayerType, rawPacket.Data);
            
            // Extract Ethernet frame
            var ethernetPacket = packet.Extract<EthernetPacket>();
            if (ethernetPacket == null)
                return null;

            // Extract IP packet
            var ipPacket = ethernetPacket.Extract<IPPacket>();
            if (ipPacket == null)
                return null;

            string sourceIP = ipPacket.SourceAddress.ToString();
            string destinationIP = ipPacket.DestinationAddress.ToString();
            string protocol = ipPacket.Protocol.ToString();
            int packetSize = rawPacket.Data.Length;
            
            int sourcePort = 0;
            int destinationPort = 0;
            var additionalFeatures = new Dictionary<string, object>
            {
                ["ip_version"] = ipPacket.Version.ToString(),
                ["ip_header_length"] = ipPacket.HeaderLength,
                ["ip_total_length"] = ipPacket.TotalLength,
                ["ip_ttl"] = ipPacket.TimeToLive,
                ["pcap_parsed"] = true
            };

            // Extract transport layer information
            var tcpPacket = ipPacket.Extract<TcpPacket>();
            var udpPacket = ipPacket.Extract<UdpPacket>();
            
            if (tcpPacket != null)
            {
                sourcePort = tcpPacket.SourcePort;
                destinationPort = tcpPacket.DestinationPort;
                protocol = "TCP";
                
                additionalFeatures["tcp_sequence"] = tcpPacket.SequenceNumber;
                additionalFeatures["tcp_acknowledgment"] = tcpPacket.AcknowledgmentNumber;
                additionalFeatures["tcp_flags"] = tcpPacket.Flags.ToString();
                additionalFeatures["tcp_window_size"] = tcpPacket.WindowSize;
            }
            else if (udpPacket != null)
            {
                sourcePort = udpPacket.SourcePort;
                destinationPort = udpPacket.DestinationPort;
                protocol = "UDP";
                
                additionalFeatures["udp_length"] = udpPacket.Length;
                additionalFeatures["udp_checksum"] = udpPacket.Checksum;
            }            else if (ipPacket.Protocol == ProtocolType.Icmp)
            {
                var icmpPacket = ipPacket.Extract<IcmpV4Packet>();
                if (icmpPacket != null)
                {
                    protocol = "ICMP";
                    additionalFeatures["icmp_type"] = icmpPacket.TypeCode.ToString();
                    additionalFeatures["icmp_checksum"] = icmpPacket.Checksum;
                }
            }

            return new NetworkTrafficRecord
            {
                Id = Guid.NewGuid(),
                Timestamp = rawPacket.Timeval.Date,
                SourceIP = sourceIP,
                SourcePort = sourcePort,
                DestinationIP = destinationIP,
                DestinationPort = destinationPort,
                Protocol = protocol,
                Duration = 0, // Duration not available from single packet
                PacketSize = packetSize,
                AdditionalFeatures = additionalFeatures
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error parsing individual packet: {ex.Message}");
            return null;
        }
    }

    private NetworkTrafficRecord GenerateSimulatedTrafficRecord(Random random)
    {
        var protocols = new[] { "TCP", "UDP", "ICMP" };
        var srcIps = new[] { "192.168.1.100", "192.168.1.101", "192.168.1.102", "10.0.0.1", "172.16.0.1" };
        var dstIps = new[] { "8.8.8.8", "1.1.1.1", "192.168.1.1", "10.0.0.254", "172.16.0.254" };
        var commonPorts = new[] { 80, 443, 22, 25, 53, 21, 23, 3389, 1433, 3306 };

        var isSuspicious = random.NextDouble() < 0.1;
        
        return new NetworkTrafficRecord
        {
            Id = Guid.NewGuid(),
            Timestamp = DateTime.UtcNow,
            SourceIP = srcIps[random.Next(srcIps.Length)],
            SourcePort = random.Next(1024, 65535),
            DestinationIP = dstIps[random.Next(dstIps.Length)],
            DestinationPort = isSuspicious ? random.Next(1, 1024) : commonPorts[random.Next(commonPorts.Length)],
            Protocol = protocols[random.Next(protocols.Length)],
            Duration = random.NextDouble() * 300,
            PacketSize = random.Next(100, isSuspicious ? 1024 * 1024 : 10240),
            AdditionalFeatures = new Dictionary<string, object>
            {
                ["simulated"] = true,
                ["suspicious"] = isSuspicious,
                ["bytes_received"] = random.Next(100, 10240)
            }
        };
    }

    public void SelectNetworkInterface()
    {
        var devices = CaptureDeviceList.Instance;

        if (devices.Count < 1)
        {
            Console.WriteLine("No network interfaces found.");
            return;
        }

        Console.WriteLine("Available network interfaces:");
        for (int i = 0; i < devices.Count; i++)
        {
            Console.WriteLine($"{i}: {devices[i].Description}");
        }

        Console.Write("Select an interface by number: ");
        if (int.TryParse(Console.ReadLine(), out int selectedIndex) && selectedIndex >= 0 && selectedIndex < devices.Count)
        {
            _activeDevice = devices[selectedIndex] as LibPcapLiveDevice;
            _selectedInterface = _activeDevice?.Description;
            Console.WriteLine($"Selected interface: {_selectedInterface}");
        }
        else
        {
            Console.WriteLine("Invalid selection.");
        }
    }
}

public enum TrafficMonitoringMode
{
    Simulation,
    RealTime
}
