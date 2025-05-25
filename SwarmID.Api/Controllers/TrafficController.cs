using Microsoft.AspNetCore.Mvc;
using SwarmID.Core.Models;
using SwarmID.Core.Interfaces;

namespace SwarmID.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TrafficController : ControllerBase
{
    private readonly ITrafficCollector _trafficCollector;
    private readonly ISwarmDetector _swarmDetector;
    private readonly IAnomalyRepository _anomalyRepository;
    private readonly ILogger<TrafficController> _logger;

    public TrafficController(
        ITrafficCollector trafficCollector,
        ISwarmDetector swarmDetector,
        IAnomalyRepository anomalyRepository,
        ILogger<TrafficController> logger)
    {
        _trafficCollector = trafficCollector;
        _swarmDetector = swarmDetector;
        _anomalyRepository = anomalyRepository;
        _logger = logger;
    }

    /// <summary>
    /// Upload and analyze traffic data file
    /// </summary>
    [HttpPost("analyze")]
    public async Task<ActionResult<AnalysisResult>> AnalyzeTrafficFile(IFormFile file, [FromQuery] TrafficDataType dataType = TrafficDataType.ZeekLogs)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file provided");

        try
        {
            // Save uploaded file temporarily
            var tempPath = Path.GetTempFileName();
            using (var stream = new FileStream(tempPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Parse traffic data
            var trafficRecords = await _trafficCollector.ParseTrafficDataAsync(tempPath, dataType);
            
            // Detect anomalies
            var anomalies = await _swarmDetector.DetectAnomaliesAsync(trafficRecords);

            // Save detected anomalies
            foreach (var anomaly in anomalies)
            {
                await _anomalyRepository.SaveAnomalyAsync(anomaly);
            }            // Clean up temp file
            System.IO.File.Delete(tempPath);

            var result = new AnalysisResult
            {
                TotalRecordsProcessed = trafficRecords.Count(),
                AnomaliesDetected = anomalies.Count(),
                Anomalies = anomalies.ToList()
            };

            _logger.LogInformation("Analyzed {RecordCount} traffic records, detected {AnomalyCount} anomalies", 
                result.TotalRecordsProcessed, result.AnomaliesDetected);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing traffic file");
            return StatusCode(500, "Error analyzing traffic file");
        }
    }

    /// <summary>
    /// Start real-time traffic monitoring
    /// </summary>
    [HttpPost("monitoring/start")]
    public async Task<IActionResult> StartMonitoring()
    {
        try
        {
            await _trafficCollector.StartMonitoringAsync();
            _logger.LogInformation("Started real-time traffic monitoring");
            return Ok(new { message = "Traffic monitoring started" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting traffic monitoring");
            return StatusCode(500, "Error starting traffic monitoring");
        }
    }

    /// <summary>
    /// Stop real-time traffic monitoring
    /// </summary>
    [HttpPost("monitoring/stop")]
    public async Task<IActionResult> StopMonitoring()
    {
        try
        {
            await _trafficCollector.StopMonitoringAsync();
            _logger.LogInformation("Stopped real-time traffic monitoring");
            return Ok(new { message = "Traffic monitoring stopped" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping traffic monitoring");
            return StatusCode(500, "Error stopping traffic monitoring");
        }
    }
}

public class AnalysisResult
{
    public int TotalRecordsProcessed { get; set; }
    public int AnomaliesDetected { get; set; }
    public List<Anomaly> Anomalies { get; set; } = new();
}
