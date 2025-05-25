using SwarmID.Core.Interfaces;
using SwarmID.Core.Models;

namespace SwarmID.Core.Algorithms;

/// <summary>
/// Ant Colony Optimization implementation for anomaly detection in network traffic.
/// Uses virtual ants to explore traffic patterns and deposit pheromones on suspicious paths.
/// </summary>
public class AntColonyOptimizationDetector : ISwarmDetector
{
    private readonly IAnomalyRepository _repository;
    private readonly Dictionary<string, double> _pheromoneTrails;
    private readonly Random _random;
    private SwarmConfiguration _currentConfiguration;

    public AntColonyOptimizationDetector(IAnomalyRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _pheromoneTrails = new Dictionary<string, double>();
        _random = new Random();
        
        // Initialize with default configuration
        _currentConfiguration = new SwarmConfiguration
        {
            Id = Guid.NewGuid(),
            Name = "Default ACO Configuration",
            MaxAnomalyScore = 100.0,
            AnomalyThreshold = 50.0,
            NumberOfAnts = 10,
            MaxIterations = 5,
            Alpha = 1.0,
            Beta = 2.0,
            Q = 100.0,
            PheromoneEvaporationRate = 0.1
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
        // Update pheromone trails based on feedback
        var multiplier = isActualAnomaly ? 1.5 : 0.5;
        
        // Strengthen or weaken relevant pheromone trails
        foreach (var sourceIP in anomaly.SourceIPs)
        {
            if (_pheromoneTrails.ContainsKey(sourceIP))
            {
                _pheromoneTrails[sourceIP] *= multiplier;
            }
        }

        foreach (var destIP in anomaly.DestinationIPs)
        {
            if (_pheromoneTrails.ContainsKey(destIP))
            {
                _pheromoneTrails[destIP] *= multiplier;
            }
        }

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
        
        // Reset pheromone trails when configuration changes significantly
        if (configuration.PheromoneEvaporationRate != _currentConfiguration.PheromoneEvaporationRate ||
            configuration.NumberOfAnts != _currentConfiguration.NumberOfAnts)
        {
            _pheromoneTrails.Clear();
        }
        
        await Task.CompletedTask; // Async operation placeholder
    }    public Task<Anomaly> AnalyzeTrafficAsync(IEnumerable<NetworkTrafficRecord> traffic, SwarmConfiguration config)
    {
        var trafficList = traffic.ToList();
        if (!trafficList.Any())
        {
            return Task.FromResult(CreateNormalAnomaly(config));
        }

        // Initialize pheromone trails
        InitializePheromoneTrails(trafficList);        

        // Run ACO algorithm
        var bestAnomaly = RunAntColonyOptimization(trafficList, config);

        // Note: Anomaly persistence is handled by the calling controller
        return Task.FromResult(bestAnomaly);
    }

    private Anomaly RunAntColonyOptimization(List<NetworkTrafficRecord> traffic, SwarmConfiguration config)
    {
        var bestAnomaly = CreateNormalAnomaly(config);
        double bestScore = 0.0;

        for (int iteration = 0; iteration < config.MaxIterations; iteration++)
        {
            // Deploy ants to explore traffic patterns
            var ants = new List<Ant>();
            for (int i = 0; i < config.NumberOfAnts; i++)
            {
                var ant = new Ant(i);
                ants.Add(ant);
            }            // Each ant explores different traffic patterns
            foreach (var ant in ants)
            {
                var anomaly = ExploreWithAnt(ant, traffic, config);
                
                if (anomaly.Score > bestScore)
                {
                    bestScore = anomaly.Score;
                    bestAnomaly = anomaly;
                }

                // Update pheromone trails based on findings
                UpdatePheromoneTrails(ant, anomaly.Score, config);
            }

            // Evaporate pheromones
            EvaporatePheromones(config.PheromoneEvaporationRate);
        }

        return bestAnomaly;
    }

    private Anomaly ExploreWithAnt(Ant ant, List<NetworkTrafficRecord> traffic, SwarmConfiguration config)
    {
        // Ant explores traffic patterns looking for anomalies
        var anomalyScore = 0.0;
        var detectedType = AnomalyType.Normal;
        var description = "Normal traffic pattern detected";        // Check for port scan patterns
        var portScanScore = DetectPortScanPattern(traffic, ant);
        if (portScanScore > anomalyScore && portScanScore >= 10.0) // Minimum threshold for port scan
        {
            anomalyScore = portScanScore;
            detectedType = AnomalyType.PortScan;
            description = $"Port scan detected by ant {ant.Id} with score {portScanScore:F1}";
        }

        // Check for DDoS patterns
        var ddosScore = DetectDDoSPattern(traffic, ant);
        if (ddosScore > anomalyScore && ddosScore >= 10.0) // Minimum threshold for DDoS
        {
            anomalyScore = ddosScore;
            detectedType = AnomalyType.DDoS;
            description = $"DDoS attack detected by ant {ant.Id} with score {ddosScore:F1}";
        }

        // Check for Command & Control patterns
        var c2Score = DetectCommandAndControlPattern(traffic, ant);
        if (c2Score > anomalyScore && c2Score >= 10.0) // Minimum threshold for C2
        {
            anomalyScore = c2Score;
            detectedType = AnomalyType.CommandAndControl;
            description = $"Command & Control communication detected by ant {ant.Id} with score {c2Score:F1}";
        }

        // Limit score to maximum
        anomalyScore = Math.Min(anomalyScore, config.MaxAnomalyScore);

        return new Anomaly
        {
            Id = Guid.NewGuid(),
            DetectedAt = DateTime.UtcNow,
            Type = detectedType,
            Score = anomalyScore,
            Description = description,
            SourceIPs = ExtractSourceIPs(traffic),
            DestinationIPs = ExtractDestinationIPs(traffic),
            Ports = ExtractPorts(traffic),
            Status = anomalyScore >= config.AnomalyThreshold ? AnomalyStatus.New : AnomalyStatus.FalsePositive,
            ConfigurationUsed = config.Name,
            Algorithm = "Ant Colony Optimization"
        };
    }    private double DetectPortScanPattern(List<NetworkTrafficRecord> traffic, Ant ant)
    {
        // Group by source IP to detect port scanning
        var sourceGroups = traffic.GroupBy(t => t.SourceIP);
        var maxPortsScanned = 0;
        var suspiciousSource = "";

        foreach (var group in sourceGroups)
        {
            var uniquePorts = group.Select(t => t.DestinationPort).Distinct().Count();
            if (uniquePorts > maxPortsScanned)
            {
                maxPortsScanned = uniquePorts;
                suspiciousSource = group.Key;
            }
        }

        // Add pheromone influence
        var pheromoneBonus = GetPheromoneStrength(suspiciousSource) * 10;

        // Update ant's explored paths
        ant.ExplorePath($"portscan_{suspiciousSource}");

        // Port scan scoring - made less sensitive for normal traffic
        if (maxPortsScanned > 50)
            return 85.0 + pheromoneBonus;
        if (maxPortsScanned > 30)
            return 70.0 + pheromoneBonus;
        if (maxPortsScanned > 20)
            return 55.0 + pheromoneBonus;
        if (maxPortsScanned > 10)
            return 35.0 + pheromoneBonus;
        if (maxPortsScanned > 5)  // Only flag as suspicious if more than 5 ports
            return Math.Max(0, maxPortsScanned * 2.0 + pheromoneBonus);

        return Math.Max(0, pheromoneBonus); // Return just pheromone bonus for <= 5 ports
    }    private double DetectDDoSPattern(List<NetworkTrafficRecord> traffic, Ant ant)
    {
        // Group by destination to detect DDoS
        var destGroups = traffic.GroupBy(t => new { t.DestinationIP, t.DestinationPort });
        var maxConnections = 0;
        var targetKey = "";

        foreach (var group in destGroups)
        {
            var connectionCount = group.Count();
            if (connectionCount > maxConnections)
            {
                maxConnections = connectionCount;
                targetKey = $"{group.Key.DestinationIP}:{group.Key.DestinationPort}";
            }
        }

        // Add pheromone influence
        var pheromoneBonus = GetPheromoneStrength(targetKey) * 10;

        // Update ant's explored paths
        ant.ExplorePath($"ddos_{targetKey}");        // DDoS scoring - only consider as DDoS if there are significant connections
        if (maxConnections > 100)
            return 90.0 + pheromoneBonus;
        if (maxConnections > 50)
            return 75.0 + pheromoneBonus;
        if (maxConnections > 25)
            return 60.0 + pheromoneBonus;
        if (maxConnections > 15)
            return 45.0 + pheromoneBonus;
        if (maxConnections > 10)
            return 30.0 + pheromoneBonus;

        // Don't consider normal traffic (< 5 connections) as DDoS
        if (maxConnections <= 5)
            return 0.0;

        return Math.Max(0, (maxConnections - 5) * 2.0 + pheromoneBonus);
    }    private double DetectCommandAndControlPattern(List<NetworkTrafficRecord> traffic, Ant ant)
    {
        if (traffic.Count < 3) return 0.0;

        // Look for regular communication patterns
        var ipPairs = traffic.GroupBy(t => new { t.SourceIP, t.DestinationIP });
        var maxRegularity = 0.0;
        var suspiciousKey = "";

        foreach (var pair in ipPairs)
        {
            var timestamps = pair.OrderBy(t => t.Timestamp).Select(t => t.Timestamp).ToList();
            if (timestamps.Count < 3) continue;

            // Calculate interval regularity
            var intervals = new List<double>();
            for (int i = 1; i < timestamps.Count; i++)
            {
                intervals.Add((timestamps[i] - timestamps[i - 1]).TotalSeconds);
            }

            var avgInterval = intervals.Average();
            var variance = intervals.Sum(x => Math.Pow(x - avgInterval, 2)) / intervals.Count;
            var regularity = avgInterval > 0 ? (1.0 / (1.0 + variance / (avgInterval * avgInterval))) * 100 : 0;

            if (regularity > maxRegularity)
            {
                maxRegularity = regularity;
                suspiciousKey = $"{pair.Key.SourceIP}->{pair.Key.DestinationIP}";
            }
        }

        // Add pheromone influence
        var pheromoneBonus = GetPheromoneStrength(suspiciousKey) * 5;

        // Update ant's explored paths
        if (!string.IsNullOrEmpty(suspiciousKey))
        {
            ant.ExplorePath($"c2_{suspiciousKey}");
        }

        // C2 communication scoring (high regularity indicates potential beaconing)
        if (maxRegularity > 80)
            return 70.0 + pheromoneBonus;
        if (maxRegularity > 60)
            return 50.0 + pheromoneBonus;
        if (maxRegularity > 40)
            return 30.0 + pheromoneBonus;

        return Math.Max(0, maxRegularity * 0.5 + pheromoneBonus);
    }

    private void InitializePheromoneTrails(List<NetworkTrafficRecord> traffic)
    {
        _pheromoneTrails.Clear();
        
        // Initialize with small amounts for all unique IPs and IP pairs
        var uniqueIPs = traffic.SelectMany(t => new[] { t.SourceIP, t.DestinationIP }).Distinct();
        foreach (var ip in uniqueIPs)
        {
            _pheromoneTrails[ip] = 0.1;
        }

        var ipPairs = traffic.Select(t => $"{t.SourceIP}->{t.DestinationIP}").Distinct();
        foreach (var pair in ipPairs)
        {
            _pheromoneTrails[pair] = 0.1;
        }
    }

    private void UpdatePheromoneTrails(Ant ant, double score, SwarmConfiguration config)
    {
        // Deposit pheromones on the paths the ant explored based on the anomaly score
        var pheromoneDeposit = (score / config.MaxAnomalyScore) * config.Q;

        foreach (var path in ant.ExploredPaths)
        {
            if (_pheromoneTrails.ContainsKey(path))
            {
                _pheromoneTrails[path] += pheromoneDeposit;
            }
            else
            {
                _pheromoneTrails[path] = pheromoneDeposit;
            }
        }
    }

    private void EvaporatePheromones(double evaporationRate)
    {
        var keys = _pheromoneTrails.Keys.ToList();
        foreach (var key in keys)
        {
            _pheromoneTrails[key] *= (1.0 - evaporationRate);
            
            // Remove very weak trails
            if (_pheromoneTrails[key] < 0.01)
            {
                _pheromoneTrails.Remove(key);
            }
        }
    }

    private double GetPheromoneStrength(string path)
    {
        return _pheromoneTrails.ContainsKey(path) ? _pheromoneTrails[path] : 0.0;
    }

    private List<string> ExtractSourceIPs(List<NetworkTrafficRecord> traffic)
    {
        return traffic.Select(t => t.SourceIP).Distinct().Take(10).ToList();
    }

    private List<string> ExtractDestinationIPs(List<NetworkTrafficRecord> traffic)
    {
        return traffic.Select(t => t.DestinationIP).Distinct().Take(10).ToList();
    }

    private List<int> ExtractPorts(List<NetworkTrafficRecord> traffic)
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
            Description = "Normal traffic pattern detected by ACO analysis",
            SourceIPs = new List<string>(),
            DestinationIPs = new List<string>(),
            Ports = new List<int>(),
            Status = AnomalyStatus.FalsePositive,
            ConfigurationUsed = config.Name,
            Algorithm = "Ant Colony Optimization"
        };
    }
}

// Supporting classes for ACO
public class Ant
{
    public int Id { get; set; }
    public List<string> ExploredPaths { get; set; }
    public double CurrentScore { get; set; }

    public Ant(int id)
    {
        Id = id;
        ExploredPaths = new List<string>();
        CurrentScore = 0.0;
    }

    public void ExplorePath(string path)
    {
        ExploredPaths.Add(path);
    }
}
