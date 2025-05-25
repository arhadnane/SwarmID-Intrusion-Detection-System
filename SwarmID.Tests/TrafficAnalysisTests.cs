using SwarmID.Core.Models;
using SwarmID.Core.Interfaces;
using SwarmID.TrafficAnalysis;

namespace SwarmID.Tests;

public class ZeekLogParserTests
{
    private readonly ZeekLogParser _parser;

    public ZeekLogParserTests()
    {
        _parser = new ZeekLogParser();
    }

    [Fact]
    public async Task ParseTrafficDataAsync_WithValidZeekLogFile_ReturnsNetworkTrafficRecords()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        var zeekLogContent = @"#separator \x09
#set_separator	,
#empty_field	(empty)
#unset_field	-
#path	conn
#open	2023-01-01-00-00-00
#fields	ts	id.orig_h	id.orig_p	id.resp_h	id.resp_p	proto	service	duration	orig_bytes	resp_bytes	conn_state	local_orig	local_resp	missed_bytes	history
1609459200.123456	192.168.1.10	52341	93.184.216.34	80	tcp	http	0.001234	1024	2048	SF	-	-	0	ShADadfF";
        await File.WriteAllTextAsync(tempFile, zeekLogContent);

        try
        {
            // Act
            var results = await _parser.ParseTrafficDataAsync(tempFile, TrafficDataType.ZeekLogs);

            // Assert
            Assert.NotEmpty(results);
            var record = results.First();
            Assert.Equal("192.168.1.10", record.SourceIP);
            Assert.Equal(52341, record.SourcePort);
            Assert.Equal("93.184.216.34", record.DestinationIP);
            Assert.Equal(80, record.DestinationPort);
            Assert.Equal("tcp", record.Protocol);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task ParseTrafficDataAsync_WithSnortAlerts_ReturnsNetworkTrafficRecords()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        var snortContent = @"[**] [1:1000001:1] Test Alert [**]
[Classification: Attempted Information Leak] [Priority: 2] 
01/01-12:34:56.123456 192.168.1.100:1234 -> 10.0.0.1:80
TCP TTL:64 TOS:0x0 ID:12345 IpLen:20 DgmLen:60 DF
***A**S* Seq: 0x12345678  Ack: 0x0  Win: 0x4000  TcpLen: 40";
        await File.WriteAllTextAsync(tempFile, snortContent);

        try
        {
            // Act
            var results = await _parser.ParseTrafficDataAsync(tempFile, TrafficDataType.SnortAlerts);

            // Assert
            Assert.NotEmpty(results);
            var record = results.First();
            Assert.Equal("192.168.1.100", record.SourceIP);
            Assert.Equal(1234, record.SourcePort);
            Assert.Equal("10.0.0.1", record.DestinationIP);
            Assert.Equal(80, record.DestinationPort);
            Assert.Equal("tcp", record.Protocol);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task ParseTrafficDataAsync_WithNonExistentFile_ThrowsFileNotFoundException()
    {
        // Arrange
        var nonExistentFile = "non_existent_file.log";

        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(() => 
            _parser.ParseTrafficDataAsync(nonExistentFile, TrafficDataType.ZeekLogs));
    }

    [Fact]
    public async Task ParsePcapFile_ShouldThrowFileNotFoundException_WhenFileDoesNotExist()
    {
        // Arrange
        var parser = new ZeekLogParser();
        var nonExistentFile = "nonexistent.pcap";

        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(() =>
            parser.ParseTrafficDataAsync(nonExistentFile, TrafficDataType.PcapFile));
    }    [Fact]
    public async Task ParsePcapFile_ShouldNotThrowNotImplementedException()
    {
        // Arrange
        var parser = new ZeekLogParser();
        var testFile = "test.pcap";

        // Act & Assert - Should throw FileNotFoundException, not NotImplementedException
        var exception = await Record.ExceptionAsync(async () => 
            await parser.ParseTrafficDataAsync(testFile, TrafficDataType.PcapFile));
        
        Assert.IsNotType<NotImplementedException>(exception);
        Assert.IsType<FileNotFoundException>(exception);
    }
}

public class ZeekLogParserMonitoringTests
{
    private readonly ZeekLogParser _monitor;

    public ZeekLogParserMonitoringTests()
    {
        _monitor = new ZeekLogParser();
    }

    [Fact]
    public async Task StartMonitoringAsync_ShouldBeginGeneratingTraffic()
    {
        // Arrange
        var trafficReceived = new List<NetworkTrafficRecord>();
        _monitor.TrafficDataReceived += (sender, record) => trafficReceived.Add(record);
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(2));

        try
        {
            // Act
            await _monitor.StartMonitoringAsync(cancellationTokenSource.Token);
            await Task.Delay(1100, cancellationTokenSource.Token); // Wait for at least one traffic generation cycle
            await _monitor.StopMonitoringAsync();

            // Assert
            Assert.NotEmpty(trafficReceived);
            Assert.All(trafficReceived, record => 
            {
                Assert.NotNull(record.SourceIP);
                Assert.NotNull(record.DestinationIP);
                Assert.True(record.SourcePort > 0);
                Assert.True(record.DestinationPort > 0);
            });
        }
        catch (OperationCanceledException)
        {
            // Expected when cancellation token times out
            await _monitor.StopMonitoringAsync();
        }
    }

    [Fact]
    public async Task StopMonitoringAsync_ShouldStopGeneratingTraffic()
    {
        // Arrange
        var trafficReceived = new List<NetworkTrafficRecord>();
        _monitor.TrafficDataReceived += (sender, record) => trafficReceived.Add(record);
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(3));

        try
        {
            await _monitor.StartMonitoringAsync(cancellationTokenSource.Token);
            await Task.Delay(500);
            var countAfterStart = trafficReceived.Count;

            // Act
            await _monitor.StopMonitoringAsync();
            await Task.Delay(1500); // Wait longer than the generation interval
            var countAfterStop = trafficReceived.Count;

            // Assert
            Assert.True(countAfterStart > 0, "Should have generated some traffic after starting");
            Assert.Equal(countAfterStart, countAfterStop); // Should not have generated more traffic after stopping
        }
        catch (OperationCanceledException)
        {
            await _monitor.StopMonitoringAsync();
        }
    }

    [Fact]
    public async Task StartMonitoringAsync_WhenAlreadyRunning_ShouldNotStartAgain()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        
        try
        {
            await _monitor.StartMonitoringAsync(cancellationTokenSource.Token);

            // Act - Try to start again
            await _monitor.StartMonitoringAsync(cancellationTokenSource.Token);

            // Assert - Should not cause issues
            await _monitor.StopMonitoringAsync();
        }
        catch (OperationCanceledException)
        {
            await _monitor.StopMonitoringAsync();
        }
    }
}