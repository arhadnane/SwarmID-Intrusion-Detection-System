namespace SwarmID.Core.Models;

/// <summary>
/// Represents a network traffic record extracted from logs
/// </summary>
public class NetworkTrafficRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime Timestamp { get; set; }
    public string SourceIP { get; set; } = string.Empty;
    public string DestinationIP { get; set; } = string.Empty;
    public int SourcePort { get; set; }
    public int DestinationPort { get; set; }
    public string Protocol { get; set; } = string.Empty;
    public int PacketSize { get; set; }
    public string Flags { get; set; } = string.Empty;
    public double Duration { get; set; }
    public Dictionary<string, object> AdditionalFeatures { get; set; } = new();
}

/// <summary>
/// Represents a detected anomaly in network traffic
/// </summary>
public class Anomaly
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
    public AnomalyType Type { get; set; }
    public double Score { get; set; }
    public string Description { get; set; } = string.Empty;
    public List<string> SourceIPs { get; set; } = new();
    public List<string> DestinationIPs { get; set; } = new();
    public List<int> Ports { get; set; } = new();
    public AnomalyStatus Status { get; set; } = AnomalyStatus.New;
    public string? AnalystFeedback { get; set; }
    public string ConfigurationUsed { get; set; } = string.Empty;
    public string Algorithm { get; set; } = string.Empty;
}

/// <summary>
/// Types of network anomalies that can be detected
/// </summary>
public enum AnomalyType
{
    Normal,
    PortScan,
    DDoS,
    CommandAndControl,
    DataExfiltration,
    UnusualTraffic
}

/// <summary>
/// Status of an anomaly investigation
/// </summary>
public enum AnomalyStatus
{
    New,
    Investigating,
    Investigated,
    Confirmed,
    FalsePositive,
    Resolved
}

/// <summary>
/// Configuration for swarm intelligence algorithms
/// </summary>
public class SwarmConfiguration
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public double MaxAnomalyScore { get; set; } = 100.0;
    public double AnomalyThreshold { get; set; } = 50.0;
    
    // Ant Colony Optimization parameters
    public int NumberOfAnts { get; set; } = 20;
    public int MaxIterations { get; set; } = 100;
    public double PheromoneEvaporationRate { get; set; } = 0.1;
    public double Alpha { get; set; } = 1.0; // Pheromone importance
    public double Beta { get; set; } = 2.0;  // Heuristic importance
    public double Q { get; set; } = 100.0;   // Pheromone deposit factor
    
    // Bee Algorithm parameters
    public int NumberOfEmployedBees { get; set; } = 10;
    public int NumberOfOnlookerBees { get; set; } = 10;
    public int MaxTrialCount { get; set; } = 5;
    public double AcceptanceProbability { get; set; } = 0.1;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
}
