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
    }    /// <summary>
    /// Get all anomalies with optional date and type filtering
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Anomaly>>> GetAnomalies(
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] AnomalyType? type = null)
    {
        try
        {
            var anomalies = await _anomalyRepository.GetAnomaliesAsync(from, to, type);
            return Ok(anomalies);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving anomalies");
            return StatusCode(500, "Internal server error");
        }
    }    /// <summary>
    /// Get paginated anomalies with optional date and type filtering
    /// </summary>
    [HttpGet("paged")]
    public async Task<ActionResult<object>> GetAnomaliesPaged(
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] AnomalyType? type = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            var (anomalies, totalCount) = await _anomalyRepository.GetAnomaliesPagedAsync(from, to, type, page, pageSize);
            
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
            
            return Ok(new
            {
                Data = anomalies,
                Pagination = new
                {
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalCount = totalCount,
                    TotalPages = totalPages,
                    HasPrevious = page > 1,
                    HasNext = page < totalPages
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving paginated anomalies");
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
