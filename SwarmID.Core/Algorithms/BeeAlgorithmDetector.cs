using SwarmID.Core.Interfaces;
using SwarmID.Core.Models;

namespace SwarmID.Core.Algorithms;

/// <summary>
/// Bee Algorithm implementation for anomaly detection in network traffic.
/// Uses employed bees, onlooker bees, and scout bees to explore the solution space.
/// </summary>
public class BeeAlgorithmDetector : ISwarmDetector
{
    private readonly IAnomalyRepository _repository;
    private SwarmConfiguration _currentConfiguration;

    public BeeAlgorithmDetector(IAnomalyRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        
        // Initialize with default configuration
        _currentConfiguration = new SwarmConfiguration
        {
            Id = Guid.NewGuid(),
            Name = "Default Bee Configuration",
            MaxAnomalyScore = 100.0,
            AnomalyThreshold = 50.0,
            NumberOfAnts = 10, // Used as bee count
            MaxIterations = 5,
            NumberOfEmployedBees = 5,
            NumberOfOnlookerBees = 5,
            MaxTrialCount = 3,
            AcceptanceProbability = 0.1
        };
    }

    /// <summary>
    /// Implementation of ISwarmDetector.DetectAnomaliesAsync
    /// </summary>
    public async Task<IEnumerable<Anomaly>> DetectAnomaliesAsync(IEnumerable<NetworkTrafficRecord> trafficRecords)
    {
        var anomaly = await AnalyzeTrafficAsync(trafficRecords, _currentConfiguration);
        return new[] { anomaly };
    }

    /// <summary>
    /// Implementation of ISwarmDetector.UpdateWithFeedbackAsync
    /// </summary>
    public async Task UpdateWithFeedbackAsync(Anomaly anomaly, bool isActualAnomaly)
    {
        // For Bee Algorithm, we adjust the configuration parameters based on feedback
        if (isActualAnomaly)
        {
            // Increase sensitivity for actual anomalies
            _currentConfiguration.AcceptanceProbability *= 1.1;
            _currentConfiguration.MaxTrialCount = Math.Min(_currentConfiguration.MaxTrialCount + 1, 10);
        }
        else
        {
            // Decrease sensitivity for false positives
            _currentConfiguration.AcceptanceProbability *= 0.9;
            _currentConfiguration.MaxTrialCount = Math.Max(_currentConfiguration.MaxTrialCount - 1, 1);
        }

        // Ensure values stay within reasonable bounds
        _currentConfiguration.AcceptanceProbability = Math.Max(0.01, Math.Min(0.5, _currentConfiguration.AcceptanceProbability));

        // Update the anomaly status in repository
        anomaly.Status = isActualAnomaly ? AnomalyStatus.Confirmed : AnomalyStatus.FalsePositive;
        await _repository.UpdateAnomalyAsync(anomaly);
    }

    /// <summary>
    /// Implementation of ISwarmDetector.GetConfiguration
    /// </summary>
    public SwarmConfiguration GetConfiguration()
    {
        return _currentConfiguration;
    }

    /// <summary>
    /// Implementation of ISwarmDetector.UpdateConfigurationAsync
    /// </summary>
    public async Task UpdateConfigurationAsync(SwarmConfiguration configuration)
    {
        _currentConfiguration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        await Task.CompletedTask; // Async operation placeholder
    }    public Task<Anomaly> AnalyzeTrafficAsync(IEnumerable<NetworkTrafficRecord> traffic, SwarmConfiguration config)
    {
        var trafficList = traffic.ToList();
        if (!trafficList.Any())
        {
            return Task.FromResult(CreateNormalAnomaly(config));
        }

        // Initialize bee colony
        var colony = InitializeBeeColony(trafficList, config);
        
        // Run bee algorithm iterations
        for (int iteration = 0; iteration < config.MaxIterations; iteration++)
        {
            // Employed bees phase
            EmployedBeesPhase(colony, trafficList, config);
            
            // Calculate fitness probabilities
            CalculateFitnessProbabilities(colony);
            
            // Onlooker bees phase
            OnlookerBeesPhase(colony, trafficList, config);
            
            // Scout bees phase
            ScoutBeesPhase(colony, trafficList, config);
            
            // Update best solution
            UpdateBestSolution(colony);
        }

        // Analyze the best solution found
        var bestSolution = colony.OrderByDescending(b => b.Fitness).First();
        var anomaly = InterpretSolution(bestSolution, trafficList, config);        

        // Note: Anomaly persistence is handled by the calling controller
        return Task.FromResult(anomaly);
    }

    private List<Bee> InitializeBeeColony(List<NetworkTrafficRecord> traffic, SwarmConfiguration config)
    {
        var colony = new List<Bee>();
        var random = new Random();

        // Create employed bees (half of the colony)
        var employedBeeCount = config.NumberOfAnts / 2; // Reusing NumberOfAnts for bee count
        
        for (int i = 0; i < employedBeeCount; i++)
        {
            var bee = new Bee
            {
                Id = i,
                Type = BeeType.Employed,
                Position = GenerateRandomPosition(traffic, random),
                Fitness = 0.0,
                TrialCount = 0
            };
            
            bee.Fitness = CalculateFitness(bee.Position, traffic, config);
            colony.Add(bee);
        }

        return colony;
    }

    private BeePosition GenerateRandomPosition(List<NetworkTrafficRecord> traffic, Random random)
    {
        var uniqueIPs = traffic.SelectMany(t => new[] { t.SourceIP, t.DestinationIP }).Distinct().ToList();
        var uniquePorts = traffic.SelectMany(t => new[] { t.SourcePort, t.DestinationPort }).Distinct().ToList();

        return new BeePosition
        {
            FocusIP = uniqueIPs.Any() ? uniqueIPs[random.Next(uniqueIPs.Count)] : "0.0.0.0",
            FocusPort = uniquePorts.Any() ? uniquePorts[random.Next(uniquePorts.Count)] : 80,
            TimeWindow = random.Next(1, 301), // 1-300 seconds
            PatternSensitivity = random.NextDouble() * 0.5 + 0.5, // 0.5-1.0
            AnomalyWeight = random.NextDouble()
        };
    }

    private double CalculateFitness(BeePosition position, List<NetworkTrafficRecord> traffic, SwarmConfiguration config)
    {
        var fitness = 0.0;
        var windowStart = traffic.Min(t => t.Timestamp);
        var windowEnd = windowStart.AddSeconds(position.TimeWindow);
        
        var windowTraffic = traffic.Where(t => t.Timestamp >= windowStart && t.Timestamp <= windowEnd).ToList();
        
        if (!windowTraffic.Any()) return 0.0;

        // Calculate various anomaly indicators
        fitness += DetectPortScanPattern(windowTraffic, position) * position.AnomalyWeight;
        fitness += DetectDDoSPattern(windowTraffic, position) * position.AnomalyWeight;
        fitness += DetectCommandAndControlPattern(windowTraffic, position) * position.AnomalyWeight;
        fitness += DetectDataExfiltrationPattern(windowTraffic, position) * position.AnomalyWeight;

        return Math.Min(fitness, config.MaxAnomalyScore);
    }

    private double DetectPortScanPattern(List<NetworkTrafficRecord> traffic, BeePosition position)
    {
        var sourceGroups = traffic.GroupBy(t => t.SourceIP);
        var maxPortsPerSource = sourceGroups.Max(g => g.Select(t => t.DestinationPort).Distinct().Count());
        
        // High port diversity from single source indicates port scan
        if (maxPortsPerSource > 20)
        {
            return 30.0 * position.PatternSensitivity;
        }
        
        return maxPortsPerSource > 10 ? 15.0 * position.PatternSensitivity : 0.0;
    }

    private double DetectDDoSPattern(List<NetworkTrafficRecord> traffic, BeePosition position)
    {
        var destGroups = traffic.GroupBy(t => new { t.DestinationIP, t.DestinationPort });
        var maxConnectionsPerDest = destGroups.Max(g => g.Count());
        
        // High connection count to single destination indicates DDoS
        if (maxConnectionsPerDest > 100)
        {
            return 35.0 * position.PatternSensitivity;
        }
        
        return maxConnectionsPerDest > 50 ? 20.0 * position.PatternSensitivity : 0.0;
    }

    private double DetectCommandAndControlPattern(List<NetworkTrafficRecord> traffic, BeePosition position)
    {
        // Look for regular, periodic communication patterns
        var timestamps = traffic.OrderBy(t => t.Timestamp).Select(t => t.Timestamp).ToList();
        if (timestamps.Count < 3) return 0.0;

        var intervals = new List<double>();
        for (int i = 1; i < timestamps.Count; i++)
        {
            intervals.Add((timestamps[i] - timestamps[i - 1]).TotalSeconds);
        }

        // Check for regular intervals (potential C&C beaconing)
        var averageInterval = intervals.Average();
        var variance = intervals.Sum(i => Math.Pow(i - averageInterval, 2)) / intervals.Count;
        var standardDeviation = Math.Sqrt(variance);

        // Low variance in intervals suggests regular beaconing
        if (standardDeviation < averageInterval * 0.1 && averageInterval > 30)
        {
            return 25.0 * position.PatternSensitivity;
        }

        return 0.0;
    }

    private double DetectDataExfiltrationPattern(List<NetworkTrafficRecord> traffic, BeePosition position)
    {
        // Look for large outbound data transfers
        var outboundTraffic = traffic.Where(t => IsInternalIP(t.SourceIP) && !IsInternalIP(t.DestinationIP));
        var totalOutboundSize = outboundTraffic.Sum(t => t.PacketSize);
        
        // Large outbound transfers may indicate data exfiltration
        if (totalOutboundSize > 10 * 1024 * 1024) // 10MB
        {
            return 20.0 * position.PatternSensitivity;
        }
        
        return totalOutboundSize > 5 * 1024 * 1024 ? 10.0 * position.PatternSensitivity : 0.0;
    }

    private bool IsInternalIP(string ip)
    {
        return ip.StartsWith("192.168.") || ip.StartsWith("10.") || ip.StartsWith("172.");
    }

    private void EmployedBeesPhase(List<Bee> colony, List<NetworkTrafficRecord> traffic, SwarmConfiguration config)
    {
        var random = new Random();
        
        foreach (var bee in colony.Where(b => b.Type == BeeType.Employed))
        {
            // Generate a new solution near the current position
            var newPosition = GenerateNeighborPosition(bee.Position, traffic, random);
            var newFitness = CalculateFitness(newPosition, traffic, config);
            
            // Greedy selection
            if (newFitness > bee.Fitness)
            {
                bee.Position = newPosition;
                bee.Fitness = newFitness;
                bee.TrialCount = 0;
            }
            else
            {
                bee.TrialCount++;
            }
        }
    }

    private BeePosition GenerateNeighborPosition(BeePosition current, List<NetworkTrafficRecord> traffic, Random random)
    {
        var neighbor = new BeePosition
        {
            FocusIP = current.FocusIP,
            FocusPort = current.FocusPort,
            TimeWindow = current.TimeWindow,
            PatternSensitivity = current.PatternSensitivity,
            AnomalyWeight = current.AnomalyWeight
        };

        // Randomly modify one parameter
        switch (random.Next(5))
        {
            case 0:
                var uniqueIPs = traffic.SelectMany(t => new[] { t.SourceIP, t.DestinationIP }).Distinct().ToList();
                if (uniqueIPs.Any())
                    neighbor.FocusIP = uniqueIPs[random.Next(uniqueIPs.Count)];
                break;
            case 1:
                var uniquePorts = traffic.SelectMany(t => new[] { t.SourcePort, t.DestinationPort }).Distinct().ToList();
                if (uniquePorts.Any())
                    neighbor.FocusPort = uniquePorts[random.Next(uniquePorts.Count)];
                break;
            case 2:
                neighbor.TimeWindow = Math.Max(1, current.TimeWindow + random.Next(-30, 31));
                break;
            case 3:
                neighbor.PatternSensitivity = Math.Max(0.1, Math.Min(1.0, current.PatternSensitivity + (random.NextDouble() - 0.5) * 0.2));
                break;
            case 4:
                neighbor.AnomalyWeight = Math.Max(0.0, Math.Min(1.0, current.AnomalyWeight + (random.NextDouble() - 0.5) * 0.2));
                break;
        }

        return neighbor;
    }

    private void CalculateFitnessProbabilities(List<Bee> colony)
    {
        var totalFitness = colony.Sum(b => b.Fitness);
        if (totalFitness == 0) return;

        foreach (var bee in colony)
        {
            bee.SelectionProbability = bee.Fitness / totalFitness;
        }
    }

    private void OnlookerBeesPhase(List<Bee> colony, List<NetworkTrafficRecord> traffic, SwarmConfiguration config)
    {
        var random = new Random();
        var employedBees = colony.Where(b => b.Type == BeeType.Employed).ToList();
        var onlookerCount = employedBees.Count; // Same number as employed bees

        for (int i = 0; i < onlookerCount; i++)
        {
            // Select an employed bee based on fitness probability
            var selectedBee = RouletteWheelSelection(employedBees, random);
            if (selectedBee == null) continue;

            // Generate new solution around selected bee's position
            var newPosition = GenerateNeighborPosition(selectedBee.Position, traffic, random);
            var newFitness = CalculateFitness(newPosition, traffic, config);

            // If better, replace the selected bee's solution
            if (newFitness > selectedBee.Fitness)
            {
                selectedBee.Position = newPosition;
                selectedBee.Fitness = newFitness;
                selectedBee.TrialCount = 0;
            }
            else
            {
                selectedBee.TrialCount++;
            }
        }
    }

    private Bee? RouletteWheelSelection(List<Bee> bees, Random random)
    {
        var totalFitness = bees.Sum(b => b.SelectionProbability);
        if (totalFitness == 0) return bees.FirstOrDefault();

        var randomValue = random.NextDouble() * totalFitness;
        var cumulativeProbability = 0.0;

        foreach (var bee in bees)
        {
            cumulativeProbability += bee.SelectionProbability;
            if (randomValue <= cumulativeProbability)
                return bee;
        }

        return bees.LastOrDefault();
    }

    private void ScoutBeesPhase(List<Bee> colony, List<NetworkTrafficRecord> traffic, SwarmConfiguration config)
    {
        var random = new Random();
        var maxTrials = config.MaxIterations / 2; // Abandon after half the max iterations

        foreach (var bee in colony.Where(b => b.TrialCount > maxTrials))
        {
            // Convert to scout bee and generate new random solution
            bee.Type = BeeType.Scout;
            bee.Position = GenerateRandomPosition(traffic, random);
            bee.Fitness = CalculateFitness(bee.Position, traffic, config);
            bee.TrialCount = 0;
            bee.Type = BeeType.Employed; // Convert back to employed bee
        }
    }

    private void UpdateBestSolution(List<Bee> colony)
    {
        // The best solution is tracked implicitly by sorting the colony
        // In a full implementation, you might maintain a global best solution
    }

    private Anomaly InterpretSolution(Bee bestBee, List<NetworkTrafficRecord> traffic, SwarmConfiguration config)
    {
        var position = bestBee.Position;
        var score = bestBee.Fitness;
        
        // Determine anomaly type based on the best solution characteristics
        var anomalyType = DetermineAnomalyType(traffic, position);
        
        var anomaly = new Anomaly
        {
            Id = Guid.NewGuid(),
            DetectedAt = DateTime.UtcNow,
            Type = anomalyType,
            Score = score,
            Description = GenerateDescription(anomalyType, score, position),
            SourceIPs = ExtractSourceIPs(traffic, position),
            DestinationIPs = ExtractDestinationIPs(traffic, position),
            Ports = ExtractPorts(traffic, position),
            Status = score >= config.AnomalyThreshold ? AnomalyStatus.New : AnomalyStatus.FalsePositive,
            ConfigurationUsed = config.Name,
            Algorithm = "Bee Algorithm"
        };

        return anomaly;
    }

    private AnomalyType DetermineAnomalyType(List<NetworkTrafficRecord> traffic, BeePosition position)
    {
        var portScanScore = DetectPortScanPattern(traffic, position);
        var ddosScore = DetectDDoSPattern(traffic, position);
        var c2Score = DetectCommandAndControlPattern(traffic, position);
        var exfilScore = DetectDataExfiltrationPattern(traffic, position);

        var maxScore = Math.Max(Math.Max(portScanScore, ddosScore), Math.Max(c2Score, exfilScore));

        if (maxScore == portScanScore && portScanScore > 0)
            return AnomalyType.PortScan;
        if (maxScore == ddosScore && ddosScore > 0)
            return AnomalyType.DDoS;
        if (maxScore == c2Score && c2Score > 0)
            return AnomalyType.CommandAndControl;
        if (maxScore == exfilScore && exfilScore > 0)
            return AnomalyType.DataExfiltration;

        return AnomalyType.Normal;
    }

    private string GenerateDescription(AnomalyType type, double score, BeePosition position)
    {
        return type switch
        {
            AnomalyType.PortScan => $"Bee Algorithm detected port scan activity with score {score:F1}. Focus IP: {position.FocusIP}",
            AnomalyType.DDoS => $"Bee Algorithm detected DDoS attack with score {score:F1}. Time window: {position.TimeWindow}s",
            AnomalyType.CommandAndControl => $"Bee Algorithm detected C&C communication with score {score:F1}. Pattern sensitivity: {position.PatternSensitivity:F2}",
            AnomalyType.DataExfiltration => $"Bee Algorithm detected data exfiltration with score {score:F1}. Focus port: {position.FocusPort}",
            _ => $"Bee Algorithm analysis complete. Score: {score:F1} (Normal traffic pattern detected)"
        };
    }

    private List<string> ExtractSourceIPs(List<NetworkTrafficRecord> traffic, BeePosition position)
    {
        return traffic.Select(t => t.SourceIP).Distinct().Take(10).ToList();
    }

    private List<string> ExtractDestinationIPs(List<NetworkTrafficRecord> traffic, BeePosition position)
    {
        return traffic.Select(t => t.DestinationIP).Distinct().Take(10).ToList();
    }

    private List<int> ExtractPorts(List<NetworkTrafficRecord> traffic, BeePosition position)
    {
        return traffic.SelectMany(t => new[] { t.SourcePort, t.DestinationPort })
                     .Distinct().Take(20).ToList();
    }

    private Anomaly CreateNormalAnomaly(SwarmConfiguration config)
    {
        return new Anomaly
        {
            Id = Guid.NewGuid(),
            DetectedAt = DateTime.UtcNow,
            Type = AnomalyType.Normal,
            Score = 0,
            Description = "No traffic data available for analysis",
            SourceIPs = new List<string>(),
            DestinationIPs = new List<string>(),
            Ports = new List<int>(),
            Status = AnomalyStatus.FalsePositive,
            ConfigurationUsed = config.Name,
            Algorithm = "Bee Algorithm"
        };
    }
}

// Supporting classes for Bee Algorithm
public class Bee
{
    public int Id { get; set; }
    public BeeType Type { get; set; }
    public BeePosition Position { get; set; } = new();
    public double Fitness { get; set; }
    public int TrialCount { get; set; }
    public double SelectionProbability { get; set; }
}

public class BeePosition
{
    public string FocusIP { get; set; } = string.Empty;
    public int FocusPort { get; set; }
    public int TimeWindow { get; set; } // in seconds
    public double PatternSensitivity { get; set; }
    public double AnomalyWeight { get; set; }
}

public enum BeeType
{
    Employed,
    Onlooker,
    Scout
}
