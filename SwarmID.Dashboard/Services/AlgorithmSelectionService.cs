using SwarmID.Core.Interfaces;
using SwarmID.Core.Algorithms;
using SwarmID.Core.Models;

namespace SwarmID.Dashboard.Services;

public interface IAlgorithmSelectionService
{
    string CurrentAlgorithm { get; }
    SwarmConfiguration CurrentConfiguration { get; }
    ISwarmDetector GetCurrentDetector();
    Task<bool> SwitchAlgorithmAsync(string algorithmType, SwarmConfiguration? configuration = null);
    event EventHandler<AlgorithmChangedEventArgs>? AlgorithmChanged;
    List<string> GetAvailableAlgorithms();
    SwarmConfiguration GetDefaultConfiguration(string algorithmType);
}

public class AlgorithmChangedEventArgs : EventArgs
{
    public string PreviousAlgorithm { get; set; } = string.Empty;
    public string NewAlgorithm { get; set; } = string.Empty;
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
}

public class AlgorithmSelectionService : IAlgorithmSelectionService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IAnomalyRepository _repository;
    private readonly ILogger<AlgorithmSelectionService> _logger;
    
    private string _currentAlgorithm = "PSO";
    private SwarmConfiguration _currentConfiguration;
    private ISwarmDetector? _currentDetector;

    public event EventHandler<AlgorithmChangedEventArgs>? AlgorithmChanged;

    public string CurrentAlgorithm => _currentAlgorithm;
    public SwarmConfiguration CurrentConfiguration => _currentConfiguration;

    public AlgorithmSelectionService(
        IServiceProvider serviceProvider,
        IAnomalyRepository repository,
        IConfiguration configuration,
        ILogger<AlgorithmSelectionService> logger)
    {
        _serviceProvider = serviceProvider;
        _repository = repository;
        _logger = logger;

        // Initialize with current configuration
        _currentAlgorithm = configuration["SwarmAlgorithm"] ?? "PSO";
        _currentConfiguration = LoadConfigurationFromAppSettings(configuration);
        _currentDetector = CreateDetector(_currentAlgorithm);
    }

    public ISwarmDetector GetCurrentDetector()
    {
        return _currentDetector ?? CreateDetector(_currentAlgorithm);
    }

    public async Task<bool> SwitchAlgorithmAsync(string algorithmType, SwarmConfiguration? configuration = null)
    {
        try
        {
            var previousAlgorithm = _currentAlgorithm;
            
            // Validate algorithm type
            if (!GetAvailableAlgorithms().Contains(algorithmType.ToUpper()))
            {
                _logger.LogWarning("Invalid algorithm type: {AlgorithmType}", algorithmType);
                return false;
            }

            // Use provided configuration or get default
            var newConfiguration = configuration ?? GetDefaultConfiguration(algorithmType);

            // Create new detector
            var newDetector = CreateDetector(algorithmType, newConfiguration);
            if (newDetector == null)
            {
                _logger.LogError("Failed to create detector for algorithm: {AlgorithmType}", algorithmType);
                return false;
            }

            // Update current values
            _currentAlgorithm = algorithmType.ToUpper();
            _currentConfiguration = newConfiguration;
            _currentDetector = newDetector;

            _logger.LogInformation("Successfully switched algorithm from {PreviousAlgorithm} to {NewAlgorithm}", 
                previousAlgorithm, _currentAlgorithm);

            // Notify subscribers
            AlgorithmChanged?.Invoke(this, new AlgorithmChangedEventArgs
            {
                PreviousAlgorithm = previousAlgorithm,
                NewAlgorithm = _currentAlgorithm,
                ChangedAt = DateTime.UtcNow            });

            await Task.CompletedTask; // Make method properly async
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error switching algorithm to {AlgorithmType}", algorithmType);
            return false;
        }
    }

    public List<string> GetAvailableAlgorithms()
    {
        return new List<string> { "ACO", "BEE", "PSO" };
    }

    public SwarmConfiguration GetDefaultConfiguration(string algorithmType)
    {
        return algorithmType.ToUpper() switch
        {
            "ACO" => new SwarmConfiguration
            {
                NumberOfAnts = 50,
                MaxIterations = 100,
                PheromoneEvaporationRate = 0.1,
                AnomalyThreshold = 70.0,
                Alpha = 1.0,
                Beta = 2.0
            },
            "BEE" => new SwarmConfiguration
            {
                NumberOfEmployedBees = 20,
                NumberOfOnlookerBees = 30,
                MaxTrialCount = 10,
                AcceptanceProbability = 0.8,
                MaxIterations = 100,
                AnomalyThreshold = 70.0
            },
            "PSO" => new SwarmConfiguration
            {
                NumberOfParticles = 30,
                MaxIterations = 100,
                InertiaWeight = 0.729,
                CognitiveComponent = 1.494,
                SocialComponent = 1.494,
                MinInertiaWeight = 0.4,
                MaxVelocity = 4.0,
                AnomalyThreshold = 0.7,
                FeedbackWeight = 0.1
            },
            _ => throw new ArgumentException($"Unknown algorithm type: {algorithmType}")
        };
    }

    private ISwarmDetector CreateDetector(string algorithmType, SwarmConfiguration? configuration = null)
    {
        var config = configuration ?? _currentConfiguration;
        
        return algorithmType.ToUpper() switch
        {
            "ACO" => new AntColonyOptimizationDetector(_repository),
            "BEE" => new BeeAlgorithmDetector(_repository),
            "PSO" => new ParticleSwarmOptimizationDetector(_repository),
            _ => throw new ArgumentException($"Unknown algorithm type: {algorithmType}")
        };
    }

    private SwarmConfiguration LoadConfigurationFromAppSettings(IConfiguration configuration)
    {
        var algorithmType = configuration["SwarmAlgorithm"] ?? "PSO";
        var defaultConfig = GetDefaultConfiguration(algorithmType);

        // Try to load from configuration section
        var configSection = configuration.GetSection("SwarmConfiguration");
        if (configSection.Exists())
        {
            // Update default config with values from appsettings.json
            if (double.TryParse(configSection["NumberOfParticles"], out var numberOfParticles))
                defaultConfig.NumberOfParticles = (int)numberOfParticles;
            if (double.TryParse(configSection["MaxIterations"], out var maxIterations))
                defaultConfig.MaxIterations = (int)maxIterations;
            if (double.TryParse(configSection["InertiaWeight"], out var inertiaWeight))
                defaultConfig.InertiaWeight = inertiaWeight;
            if (double.TryParse(configSection["CognitiveComponent"], out var cognitiveComponent))
                defaultConfig.CognitiveComponent = cognitiveComponent;
            if (double.TryParse(configSection["SocialComponent"], out var socialComponent))
                defaultConfig.SocialComponent = socialComponent;
            if (double.TryParse(configSection["MinInertiaWeight"], out var minInertiaWeight))
                defaultConfig.MinInertiaWeight = minInertiaWeight;
            if (double.TryParse(configSection["MaxVelocity"], out var maxVelocity))
                defaultConfig.MaxVelocity = maxVelocity;
            if (double.TryParse(configSection["AnomalyThreshold"], out var anomalyThreshold))
                defaultConfig.AnomalyThreshold = anomalyThreshold;
            if (double.TryParse(configSection["FeedbackWeight"], out var feedbackWeight))
                defaultConfig.FeedbackWeight = feedbackWeight;
        }

        return defaultConfig;
    }
}
