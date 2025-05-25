using SwarmID.Core.Models;

namespace SwarmID.Core.Interfaces;

/// <summary>
/// Interface for swarm intelligence anomaly detection algorithms
/// </summary>
public interface ISwarmDetector
{
    /// <summary>
    /// Analyzes network traffic records and detects anomalies
    /// </summary>
    Task<IEnumerable<Anomaly>> DetectAnomaliesAsync(IEnumerable<NetworkTrafficRecord> trafficRecords);
    
    /// <summary>
    /// Updates the algorithm with feedback from analyst
    /// </summary>
    Task UpdateWithFeedbackAsync(Anomaly anomaly, bool isActualAnomaly);
    
    /// <summary>
    /// Gets current algorithm configuration
    /// </summary>
    SwarmConfiguration GetConfiguration();
    
    /// <summary>
    /// Updates algorithm configuration
    /// </summary>
    Task UpdateConfigurationAsync(SwarmConfiguration configuration);
}

/// <summary>
/// Interface for traffic data collection and parsing
/// </summary>
public interface ITrafficCollector
{
    /// <summary>
    /// Parses traffic data from various sources
    /// </summary>
    Task<IEnumerable<NetworkTrafficRecord>> ParseTrafficDataAsync(string filePath, TrafficDataType dataType);
    
    /// <summary>
    /// Starts real-time traffic monitoring
    /// </summary>
    Task StartMonitoringAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Stops real-time traffic monitoring
    /// </summary>
    Task StopMonitoringAsync();
    
    /// <summary>
    /// Event fired when new traffic data is available
    /// </summary>
    event EventHandler<NetworkTrafficRecord> TrafficDataReceived;
}

/// <summary>
/// Interface for storing and retrieving anomalies
/// </summary>
public interface IAnomalyRepository
{
    Task<IEnumerable<Anomaly>> GetAnomaliesAsync(DateTime? from = null, DateTime? to = null);
    Task<Anomaly?> GetAnomalyByIdAsync(string id);
    Task SaveAnomalyAsync(Anomaly anomaly);
    Task UpdateAnomalyAsync(Anomaly anomaly);
    Task DeleteAnomalyAsync(string id);
}

/// <summary>
/// Types of traffic data sources
/// </summary>
public enum TrafficDataType
{
    ZeekLogs,
    SnortAlerts,
    PcapFile,
    LiveCapture
}
