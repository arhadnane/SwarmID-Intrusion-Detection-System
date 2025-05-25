using System;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using SharpPcap;
using SharpPcap.LibPcap;
using PacketDotNet;

public class SimplePcapGenerator
{
    public static void Main()
    {
        Console.WriteLine("Generating sample PCAP files for SwarmID...");
        Console.WriteLine("==========================================");
        
        try
        {
            GenerateNormalTraffic();
            GeneratePortScanTraffic();
            GenerateSuspiciousTraffic();
            Console.WriteLine("\n✓ All sample PCAP files created successfully!");
            Console.WriteLine("\nFiles generated:");
            Console.WriteLine("- normal-traffic.pcap");
            Console.WriteLine("- port-scan.pcap");  
            Console.WriteLine("- suspicious-traffic.pcap");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }
      static void GenerateNormalTraffic()
    {
        Console.WriteLine("Generating normal-traffic.pcap...");
        using var writer = new CaptureFileWriterDevice("normal-traffic.pcap");
        writer.Open();
        
        var baseTime = new PosixTimeval((ulong)DateTimeOffset.Now.ToUnixTimeSeconds(), 0);
        
        // Normal HTTPS traffic
        for (int i = 0; i < 10; i++)
        {
            var packet = CreateTcpPacket("192.168.1.100", "104.16.132.229", 
                                       (ushort)(50000 + i), 443, true, false);
            var timestamp = new PosixTimeval(baseTime.Seconds + (ulong)(i * 30), 0);
            var capture = new RawCapture(LinkLayers.Ethernet, timestamp, packet);
            writer.Write(capture);
        }
        
        writer.Close();
        Console.WriteLine("✓ normal-traffic.pcap created");
    }
      static void GeneratePortScanTraffic()
    {
        Console.WriteLine("Generating port-scan.pcap...");
        using var writer = new CaptureFileWriterDevice("port-scan.pcap");
        writer.Open();
        
        var baseTime = new PosixTimeval((ulong)DateTimeOffset.Now.ToUnixTimeSeconds(), 0);
        ushort[] ports = { 21, 22, 23, 25, 53, 80, 135, 139, 443, 445, 993, 995, 1433, 3306, 3389 };
        
        // Port scan from single source to multiple ports
        for (int i = 0; i < ports.Length; i++)
        {
            var packet = CreateTcpPacket("10.0.0.100", "192.168.1.200", 
                                       (ushort)(60000 + i), ports[i], true, false);
            var timestamp = new PosixTimeval(baseTime.Seconds + (ulong)(i * 2), 0);
            var capture = new RawCapture(LinkLayers.Ethernet, timestamp, packet);
            writer.Write(capture);
        }
        
        writer.Close();
        Console.WriteLine("✓ port-scan.pcap created");
    }
      static void GenerateSuspiciousTraffic()
    {
        Console.WriteLine("Generating suspicious-traffic.pcap...");
        using var writer = new CaptureFileWriterDevice("suspicious-traffic.pcap");
        writer.Open();
        
        var baseTime = new PosixTimeval((ulong)DateTimeOffset.Now.ToUnixTimeSeconds(), 0);
        
        // High frequency connections (potential DDoS)
        for (int i = 0; i < 50; i++)
        {
            var packet = CreateTcpPacket($"172.16.{i % 256}.{i % 256}", "192.168.1.100", 
                                       (ushort)(40000 + i), 80, true, false);
            var timestamp = new PosixTimeval(baseTime.Seconds + (ulong)(i / 10), (ulong)(i % 10 * 100000));
            var capture = new RawCapture(LinkLayers.Ethernet, timestamp, packet);
            writer.Write(capture);
        }
        
        writer.Close();
        Console.WriteLine("✓ suspicious-traffic.pcap created");
    }
    
    static byte[] CreateTcpPacket(string srcIp, string dstIp, ushort srcPort, ushort dstPort, bool syn, bool ack)
    {
        // Create Ethernet header
        var ethernetPacket = new EthernetPacket(
            PhysicalAddress.Parse("00-11-22-33-44-55"),
            PhysicalAddress.Parse("66-77-88-99-AA-BB"),
            EthernetType.IPv4
        );
        
        // Create IP header
        var ipPacket = new IPv4Packet(IPAddress.Parse(srcIp), IPAddress.Parse(dstIp))
        {
            Protocol = ProtocolType.Tcp,
            TimeToLive = 64,
            Id = 1234
        };
        
        // Create TCP header
        var tcpPacket = new TcpPacket(srcPort, dstPort)
        {
            WindowSize = 8192,
            SequenceNumber = 1000
        };
        
        // Set TCP flags using properties
        if (syn) tcpPacket.Synchronize = true;
        if (ack) tcpPacket.Acknowledgment = true;
        
        // Assemble packet
        ipPacket.PayloadPacket = tcpPacket;
        ethernetPacket.PayloadPacket = ipPacket;
        
        return ethernetPacket.Bytes;
    }
}
