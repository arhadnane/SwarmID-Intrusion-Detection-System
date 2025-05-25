using Microsoft.AspNetCore.Mvc;
using SwarmID.Core.Models;
using SwarmID.Core.Interfaces;

namespace SwarmID.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AnomaliesController : ControllerBase
{
    private readonly IAnomalyRepository _anomalyRepository;
    private readonly ISwarmDetector _swarmDetector;
    private readonly ILogger<AnomaliesController> _logger;

    public AnomaliesController(
        IAnomalyRepository anomalyRepository,
        ISwarmDetector swarmDetector,
        ILogger<AnomaliesController> logger)
    {
        _anomalyRepository = anomalyRepository;
        _swarmDetector = swarmDetector;
        _logger = logger;
    }

    /// <summary>
    /// Get all anomalies with optional date filtering
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Anomaly>>> GetAnomalies(
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        try
        {
            var anomalies = await _anomalyRepository.GetAnomaliesAsync(from, to);
            return Ok(anomalies);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving anomalies");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get a specific anomaly by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<Anomaly>> GetAnomaly(string id)
    {
        try
        {
            var anomaly = await _anomalyRepository.GetAnomalyByIdAsync(id);
            if (anomaly == null)
                return NotFound();

            return Ok(anomaly);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving anomaly {AnomalyId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Update anomaly status with analyst feedback
    /// </summary>
    [HttpPut("{id}/feedback")]
    public async Task<IActionResult> UpdateAnomalyFeedback(string id, [FromBody] AnomalyFeedbackRequest request)
    {
        try
        {
            var anomaly = await _anomalyRepository.GetAnomalyByIdAsync(id);
            if (anomaly == null)
                return NotFound();

            anomaly.Status = request.Status;
            await _anomalyRepository.UpdateAnomalyAsync(anomaly);

            // Update swarm detector with feedback
            await _swarmDetector.UpdateWithFeedbackAsync(anomaly, request.IsActualAnomaly);

            _logger.LogInformation("Updated anomaly {AnomalyId} with feedback: {Status}", id, request.Status);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating anomaly feedback for {AnomalyId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Delete an anomaly
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAnomaly(string id)
    {
        try
        {
            var anomaly = await _anomalyRepository.GetAnomalyByIdAsync(id);
            if (anomaly == null)
                return NotFound();

            await _anomalyRepository.DeleteAnomalyAsync(id);
            _logger.LogInformation("Deleted anomaly {AnomalyId}", id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting anomaly {AnomalyId}", id);
            return StatusCode(500, "Internal server error");
        }
    }
}

public class AnomalyFeedbackRequest
{
    public AnomalyStatus Status { get; set; }
    public bool IsActualAnomaly { get; set; }
}
