using SwarmID.Core.Interfaces;
using SwarmID.Core.Models;

namespace SwarmID.Core.Algorithms;

/// <summary>
/// Particle Swarm Optimization implementation for anomaly detection in network traffic.
/// Uses particles to explore threshold optimization space for detecting network anomalies.
/// Each particle represents a set of detection thresholds optimized for different anomaly types.
/// </summary>
public class ParticleSwarmOptimizationDetector : ISwarmDetector
{
    private readonly IAnomalyRepository _repository;
    private SwarmConfiguration _currentConfiguration;
    private readonly Random _random;

    public ParticleSwarmOptimizationDetector(IAnomalyRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _random = new Random();
        
        // Initialize with default PSO configuration
        _currentConfiguration = new SwarmConfiguration
        {
            Id = Guid.NewGuid(),
            Name = "Default PSO Configuration",
            MaxAnomalyScore = 100.0,
            AnomalyThreshold = 50.0,
            NumberOfParticles = 20,
            MaxIterations = 10,
            InertiaWeight = 0.9,
            CognitiveComponent = 2.0,
            SocialComponent = 2.0,
            MinInertiaWeight = 0.4,
            MaxVelocity = 10.0
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
        // For PSO, we adjust the algorithm parameters based on feedback
        if (isActualAnomaly)
        {
            // Increase exploration for actual anomalies
            _currentConfiguration.InertiaWeight = Math.Min(_currentConfiguration.InertiaWeight * 1.05, 0.95);
            _currentConfiguration.CognitiveComponent = Math.Min(_currentConfiguration.CognitiveComponent * 1.02, 3.0);
        }
        else
        {
            // Decrease sensitivity for false positives
            _currentConfiguration.InertiaWeight = Math.Max(_currentConfiguration.InertiaWeight * 0.95, _currentConfiguration.MinInertiaWeight);
            _currentConfiguration.SocialComponent = Math.Min(_currentConfiguration.SocialComponent * 1.02, 3.0);
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
        await Task.CompletedTask; // Async operation placeholder
    }

    /// <summary>
    /// Analyzes network traffic using PSO to optimize detection thresholds
    /// </summary>
    public Task<Anomaly> AnalyzeTrafficAsync(IEnumerable<NetworkTrafficRecord> traffic, SwarmConfiguration config)
    {
        var trafficList = traffic.ToList();
        if (!trafficList.Any())
        {
            return Task.FromResult(CreateNormalAnomaly(config));
        }

        // Initialize particle swarm
        var swarm = InitializeSwarm(trafficList, config);
        var globalBest = InitializeGlobalBest();

        // Run PSO iterations
        for (int iteration = 0; iteration < config.MaxIterations; iteration++)
        {
            // Evaluate fitness for each particle
            foreach (var particle in swarm)
            {
                particle.Fitness = EvaluateFitness(particle.Position, trafficList, config);
                
                // Update personal best
                if (particle.Fitness > particle.PersonalBestFitness)
                {
                    particle.PersonalBestFitness = particle.Fitness;
                    particle.PersonalBestPosition = particle.Position.Copy();
                }
                
                // Update global best
                if (particle.Fitness > globalBest.Fitness)
                {
                    globalBest.Fitness = particle.Fitness;
                    globalBest.Position = particle.Position.Copy();
                }
            }

            // Update velocities and positions
            var currentInertia = CalculateInertiaWeight(iteration, config);
            foreach (var particle in swarm)
            {
                UpdateParticleVelocity(particle, globalBest, currentInertia, config);
                UpdateParticlePosition(particle, config);
            }
        }

        // Analyze the best solution found
        var anomaly = InterpretSolution(globalBest, trafficList, config);
        
        // Note: Anomaly persistence is handled by the calling controller
        return Task.FromResult(anomaly);
    }

    private List<Particle> InitializeSwarm(List<NetworkTrafficRecord> traffic, SwarmConfiguration config)
    {
        var swarm = new List<Particle>();
        
        for (int i = 0; i < config.NumberOfParticles; i++)
        {
            var particle = new Particle
            {
                Id = i,
                Position = GenerateRandomPosition(traffic),
                Velocity = GenerateRandomVelocity(config),
                Fitness = 0.0,
                PersonalBestFitness = 0.0
            };
            
            particle.PersonalBestPosition = particle.Position.Copy();
            swarm.Add(particle);
        }

        return swarm;
    }

    private ParticlePosition GenerateRandomPosition(List<NetworkTrafficRecord> traffic)
    {
        // Initialize thresholds randomly within reasonable bounds
        return new ParticlePosition
        {
            PortScanThreshold = _random.NextDouble() * 50 + 10,    // 10-60 ports
            DDoSThreshold = _random.NextDouble() * 80 + 20,       // 20-100 connections
            C2RegularityThreshold = _random.NextDouble() * 50 + 50, // 50-100% regularity
            DataExfilThreshold = _random.NextDouble() * 20 + 5,   // 5-25 MB
            TimeWindow = _random.Next(30, 301),                   // 30-300 seconds
            PatternSensitivity = _random.NextDouble() * 0.5 + 0.5 // 0.5-1.0
        };
    }

    private ParticleVelocity GenerateRandomVelocity(SwarmConfiguration config)
    {
        var maxVel = config.MaxVelocity;
        return new ParticleVelocity
        {
            PortScanVelocity = (_random.NextDouble() - 0.5) * maxVel,
            DDoSVelocity = (_random.NextDouble() - 0.5) * maxVel,
            C2RegularityVelocity = (_random.NextDouble() - 0.5) * maxVel,
            DataExfilVelocity = (_random.NextDouble() - 0.5) * maxVel,
            TimeWindowVelocity = (_random.NextDouble() - 0.5) * maxVel,
            PatternSensitivityVelocity = (_random.NextDouble() - 0.5) * maxVel * 0.1
        };
    }

    private GlobalBest InitializeGlobalBest()
    {
        return new GlobalBest
        {
            Position = new ParticlePosition
            {
                PortScanThreshold = 25,
                DDoSThreshold = 50,
                C2RegularityThreshold = 75,
                DataExfilThreshold = 10,
                TimeWindow = 60,
                PatternSensitivity = 0.8
            },
            Fitness = 0.0
        };
    }

    private double EvaluateFitness(ParticlePosition position, List<NetworkTrafficRecord> traffic, SwarmConfiguration config)
    {
        var fitness = 0.0;
        
        // Apply time window filter
        var windowStart = traffic.Min(t => t.Timestamp);
        var windowEnd = windowStart.AddSeconds(position.TimeWindow);
        var windowTraffic = traffic.Where(t => t.Timestamp >= windowStart && t.Timestamp <= windowEnd).ToList();
        
        if (!windowTraffic.Any()) return 0.0;

        // Evaluate different anomaly detection patterns
        fitness += EvaluatePortScanDetection(windowTraffic, position);
        fitness += EvaluateDDoSDetection(windowTraffic, position);
        fitness += EvaluateC2Detection(windowTraffic, position);
        fitness += EvaluateDataExfiltrationDetection(windowTraffic, position);

        return Math.Min(fitness, config.MaxAnomalyScore);
    }

    private double EvaluatePortScanDetection(List<NetworkTrafficRecord> traffic, ParticlePosition position)
    {
        var sourceGroups = traffic.GroupBy(t => t.SourceIP);
        var maxPortsPerSource = sourceGroups.Any() ? sourceGroups.Max(g => g.Select(t => t.DestinationPort).Distinct().Count()) : 0;
        
        // Score based on how well the threshold detects port scans
        if (maxPortsPerSource > position.PortScanThreshold)
        {
            return 30.0 * position.PatternSensitivity * (maxPortsPerSource / position.PortScanThreshold);
        }
        
        return maxPortsPerSource > position.PortScanThreshold * 0.7 ? 15.0 * position.PatternSensitivity : 0.0;
    }

    private double EvaluateDDoSDetection(List<NetworkTrafficRecord> traffic, ParticlePosition position)
    {
        var destGroups = traffic.GroupBy(t => new { t.DestinationIP, t.DestinationPort });
        var maxConnectionsPerDest = destGroups.Any() ? destGroups.Max(g => g.Count()) : 0;
        
        // Score based on how well the threshold detects DDoS
        if (maxConnectionsPerDest > position.DDoSThreshold)
        {
            return 35.0 * position.PatternSensitivity * (maxConnectionsPerDest / position.DDoSThreshold);
        }
        
        return maxConnectionsPerDest > position.DDoSThreshold * 0.7 ? 20.0 * position.PatternSensitivity : 0.0;
    }

    private double EvaluateC2Detection(List<NetworkTrafficRecord> traffic, ParticlePosition position)
    {
        if (traffic.Count < 3) return 0.0;

        // Analyze communication regularity
        var ipPairs = traffic.GroupBy(t => new { t.SourceIP, t.DestinationIP });
        var maxRegularity = 0.0;

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
            
            maxRegularity = Math.Max(maxRegularity, regularity);
        }

        // Score based on how well the threshold detects C2 communication
        if (maxRegularity > position.C2RegularityThreshold)
        {
            return 25.0 * position.PatternSensitivity * (maxRegularity / position.C2RegularityThreshold);
        }

        return 0.0;
    }

    private double EvaluateDataExfiltrationDetection(List<NetworkTrafficRecord> traffic, ParticlePosition position)
    {
        // Look for large outbound data transfers
        var outboundTraffic = traffic.Where(t => IsInternalIP(t.SourceIP) && !IsInternalIP(t.DestinationIP));
        var totalOutboundSizeMB = outboundTraffic.Sum(t => t.PacketSize) / (1024.0 * 1024.0);
        
        // Score based on how well the threshold detects data exfiltration
        if (totalOutboundSizeMB > position.DataExfilThreshold)
        {
            return 20.0 * position.PatternSensitivity * (totalOutboundSizeMB / position.DataExfilThreshold);
        }
        
        return totalOutboundSizeMB > position.DataExfilThreshold * 0.7 ? 10.0 * position.PatternSensitivity : 0.0;
    }

    private bool IsInternalIP(string ip)
    {
        return ip.StartsWith("192.168.") || ip.StartsWith("10.") || ip.StartsWith("172.");
    }

    private double CalculateInertiaWeight(int iteration, SwarmConfiguration config)
    {
        // Linearly decrease inertia weight from initial value to minimum
        var ratio = (double)iteration / config.MaxIterations;
        return config.InertiaWeight - (config.InertiaWeight - config.MinInertiaWeight) * ratio;
    }

    private void UpdateParticleVelocity(Particle particle, GlobalBest globalBest, double inertiaWeight, SwarmConfiguration config)
    {
        var r1 = _random.NextDouble();
        var r2 = _random.NextDouble();
        
        var cognitive = config.CognitiveComponent;
        var social = config.SocialComponent;

        // Update velocity components
        particle.Velocity.PortScanVelocity = inertiaWeight * particle.Velocity.PortScanVelocity +
            cognitive * r1 * (particle.PersonalBestPosition.PortScanThreshold - particle.Position.PortScanThreshold) +
            social * r2 * (globalBest.Position.PortScanThreshold - particle.Position.PortScanThreshold);

        particle.Velocity.DDoSVelocity = inertiaWeight * particle.Velocity.DDoSVelocity +
            cognitive * r1 * (particle.PersonalBestPosition.DDoSThreshold - particle.Position.DDoSThreshold) +
            social * r2 * (globalBest.Position.DDoSThreshold - particle.Position.DDoSThreshold);

        particle.Velocity.C2RegularityVelocity = inertiaWeight * particle.Velocity.C2RegularityVelocity +
            cognitive * r1 * (particle.PersonalBestPosition.C2RegularityThreshold - particle.Position.C2RegularityThreshold) +
            social * r2 * (globalBest.Position.C2RegularityThreshold - particle.Position.C2RegularityThreshold);

        particle.Velocity.DataExfilVelocity = inertiaWeight * particle.Velocity.DataExfilVelocity +
            cognitive * r1 * (particle.PersonalBestPosition.DataExfilThreshold - particle.Position.DataExfilThreshold) +
            social * r2 * (globalBest.Position.DataExfilThreshold - particle.Position.DataExfilThreshold);

        particle.Velocity.TimeWindowVelocity = inertiaWeight * particle.Velocity.TimeWindowVelocity +
            cognitive * r1 * (particle.PersonalBestPosition.TimeWindow - particle.Position.TimeWindow) +
            social * r2 * (globalBest.Position.TimeWindow - particle.Position.TimeWindow);

        particle.Velocity.PatternSensitivityVelocity = inertiaWeight * particle.Velocity.PatternSensitivityVelocity +
            cognitive * r1 * (particle.PersonalBestPosition.PatternSensitivity - particle.Position.PatternSensitivity) +
            social * r2 * (globalBest.Position.PatternSensitivity - particle.Position.PatternSensitivity);

        // Clamp velocities to prevent explosion
        ClampVelocity(particle.Velocity, config.MaxVelocity);
    }

    private void ClampVelocity(ParticleVelocity velocity, double maxVelocity)
    {
        velocity.PortScanVelocity = Math.Max(-maxVelocity, Math.Min(maxVelocity, velocity.PortScanVelocity));
        velocity.DDoSVelocity = Math.Max(-maxVelocity, Math.Min(maxVelocity, velocity.DDoSVelocity));
        velocity.C2RegularityVelocity = Math.Max(-maxVelocity, Math.Min(maxVelocity, velocity.C2RegularityVelocity));
        velocity.DataExfilVelocity = Math.Max(-maxVelocity, Math.Min(maxVelocity, velocity.DataExfilVelocity));
        velocity.TimeWindowVelocity = Math.Max(-maxVelocity, Math.Min(maxVelocity, velocity.TimeWindowVelocity));
        velocity.PatternSensitivityVelocity = Math.Max(-maxVelocity * 0.1, Math.Min(maxVelocity * 0.1, velocity.PatternSensitivityVelocity));
    }

    private void UpdateParticlePosition(Particle particle, SwarmConfiguration config)
    {
        // Update positions based on velocities
        particle.Position.PortScanThreshold += particle.Velocity.PortScanVelocity;
        particle.Position.DDoSThreshold += particle.Velocity.DDoSVelocity;
        particle.Position.C2RegularityThreshold += particle.Velocity.C2RegularityVelocity;
        particle.Position.DataExfilThreshold += particle.Velocity.DataExfilVelocity;
        particle.Position.TimeWindow += (int)particle.Velocity.TimeWindowVelocity;
        particle.Position.PatternSensitivity += particle.Velocity.PatternSensitivityVelocity;

        // Clamp positions to valid ranges
        ClampPosition(particle.Position);
    }

    private void ClampPosition(ParticlePosition position)
    {
        position.PortScanThreshold = Math.Max(5, Math.Min(100, position.PortScanThreshold));
        position.DDoSThreshold = Math.Max(10, Math.Min(200, position.DDoSThreshold));
        position.C2RegularityThreshold = Math.Max(30, Math.Min(100, position.C2RegularityThreshold));
        position.DataExfilThreshold = Math.Max(1, Math.Min(50, position.DataExfilThreshold));
        position.TimeWindow = Math.Max(10, Math.Min(600, position.TimeWindow));
        position.PatternSensitivity = Math.Max(0.1, Math.Min(1.0, position.PatternSensitivity));
    }

    private Anomaly InterpretSolution(GlobalBest globalBest, List<NetworkTrafficRecord> traffic, SwarmConfiguration config)
    {
        var position = globalBest.Position;
        var score = globalBest.Fitness;
        
        // Determine anomaly type based on the optimized thresholds
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
            Algorithm = "Particle Swarm Optimization"
        };

        return anomaly;
    }

    private AnomalyType DetermineAnomalyType(List<NetworkTrafficRecord> traffic, ParticlePosition position)
    {
        var portScanScore = EvaluatePortScanDetection(traffic, position);
        var ddosScore = EvaluateDDoSDetection(traffic, position);
        var c2Score = EvaluateC2Detection(traffic, position);
        var exfilScore = EvaluateDataExfiltrationDetection(traffic, position);

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

    private string GenerateDescription(AnomalyType type, double score, ParticlePosition position)
    {
        return type switch
        {
            AnomalyType.PortScan => $"PSO detected port scan activity with score {score:F1}. Optimized threshold: {position.PortScanThreshold:F1} ports",
            AnomalyType.DDoS => $"PSO detected DDoS attack with score {score:F1}. Optimized threshold: {position.DDoSThreshold:F1} connections",
            AnomalyType.CommandAndControl => $"PSO detected C&C communication with score {score:F1}. Regularity threshold: {position.C2RegularityThreshold:F1}%",
            AnomalyType.DataExfiltration => $"PSO detected data exfiltration with score {score:F1}. Size threshold: {position.DataExfilThreshold:F1}MB",
            _ => $"PSO threshold optimization complete. Score: {score:F1} (Normal traffic pattern detected)"
        };
    }

    private List<string> ExtractSourceIPs(List<NetworkTrafficRecord> traffic, ParticlePosition position)
    {
        return traffic.Select(t => t.SourceIP).Distinct().Take(10).ToList();
    }

    private List<string> ExtractDestinationIPs(List<NetworkTrafficRecord> traffic, ParticlePosition position)
    {
        return traffic.Select(t => t.DestinationIP).Distinct().Take(10).ToList();
    }

    private List<int> ExtractPorts(List<NetworkTrafficRecord> traffic, ParticlePosition position)
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
            Description = "No traffic data available for PSO analysis",
            SourceIPs = new List<string>(),
            DestinationIPs = new List<string>(),
            Ports = new List<int>(),
            Status = AnomalyStatus.FalsePositive,
            ConfigurationUsed = config.Name,
            Algorithm = "Particle Swarm Optimization"
        };
    }
}

// Supporting classes for PSO
public class Particle
{
    public int Id { get; set; }
    public ParticlePosition Position { get; set; } = new();
    public ParticleVelocity Velocity { get; set; } = new();
    public double Fitness { get; set; }
    public ParticlePosition PersonalBestPosition { get; set; } = new();
    public double PersonalBestFitness { get; set; }
}

public class ParticlePosition
{
    public double PortScanThreshold { get; set; }
    public double DDoSThreshold { get; set; }
    public double C2RegularityThreshold { get; set; }
    public double DataExfilThreshold { get; set; }
    public int TimeWindow { get; set; }
    public double PatternSensitivity { get; set; }

    public ParticlePosition Copy()
    {
        return new ParticlePosition
        {
            PortScanThreshold = this.PortScanThreshold,
            DDoSThreshold = this.DDoSThreshold,
            C2RegularityThreshold = this.C2RegularityThreshold,
            DataExfilThreshold = this.DataExfilThreshold,
            TimeWindow = this.TimeWindow,
            PatternSensitivity = this.PatternSensitivity
        };
    }
}

public class ParticleVelocity
{
    public double PortScanVelocity { get; set; }
    public double DDoSVelocity { get; set; }
    public double C2RegularityVelocity { get; set; }
    public double DataExfilVelocity { get; set; }
    public double TimeWindowVelocity { get; set; }
    public double PatternSensitivityVelocity { get; set; }
}

public class GlobalBest
{
    public ParticlePosition Position { get; set; } = new();
    public double Fitness { get; set; }
}
