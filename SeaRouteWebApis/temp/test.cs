// 1. Updated Request Model
using Microsoft.AspNetCore.Mvc;
using SeaRouteModel.Models;
using SeaRouteWebApis.Controllers;
using SeaRouteWebApis.Interfaces;

public class BkWxRouteRequest
{
    public string DataSource { get; set; } // Changed from WaveType to DataSource
    public List<Coordinate> Coordinates { get; set; }
    public double ExceedanceProbability { get; set; }
    public double SignificantWaveHeight { get; set; } // Changed from TargetWaveHeight
    public string SeasonType { get; set; } = "annual"; // New field with default value
    // Removed PointNumber as it can be deduced from Coordinates.Count
}

// 2. Updated Response Model
public class BkWxRouteResponse
{
    public string DataSource { get; set; } // Changed from WaveType
    public List<Coordinate> Coordinates { get; set; }
    public double ExceedanceProbability { get; set; }
    public double SignificantWaveHeight { get; set; } // Changed from TargetWaveHeight
    public string SeasonType { get; set; } // New field
    public double ReductionFactor { get; set; }
    // Removed PointNumber
}

// 3. Updated Controller
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using SeaRouteModel.Models;
using SeaRouteWebApis.Controllers;
using SeaRouteWebApis.Interfaces;

namespace NextGenEngApps.DigitalRules.API.Controllers
{
    [ApiController]
    [Route("api/v1/reduction-factor/")]
    public class ReductionFactorCalculation : SeaRouteBaseController<ReductionFactorRecord>
    {
        private readonly IWaveScatterDiagramService _wsdService;
        private readonly ILogger<ReductionFactorCalculation> _logger;
        private readonly IRouteSplitService _routeSplitService;

        public ReductionFactorCalculation(ILoggerFactory loggerFactory, IRepository<ReductionFactorRecord> reductionFactorRepository,
            IWaveScatterDiagramService wsdService, IRouteSplitService routeSplitService)
            : base(loggerFactory, reductionFactorRepository)
        {
            _wsdService = wsdService;
            _logger = loggerFactory.CreateLogger<ReductionFactorCalculation>();
            _routeSplitService = routeSplitService;
        }

        [HttpPost("reduction-factor-calculation/")]
        public IActionResult ProcessRoute([FromBody] BkWxRouteRequest request)
        {
            try
            {
                // Validate exceedance probability
                if (request.ExceedanceProbability <= 0 || request.ExceedanceProbability >= 1)
                {
                    _logger.LogError("Exceedance probability must be between 0 and 1.");
                    return BadRequest(new { Message = "Exceedance probability must be between 0 and 1." });
                }

                // Validate DataSource (changed from WaveType)
                if (request.DataSource != "ABS" && request.DataSource != "BMT")
                {
                    _logger.LogError("Invalid DataSource. Must be 'ABS' or 'BMT'.");
                    return BadRequest(new { Message = "Invalid DataSource. Must be 'ABS' or 'BMT'." });
                }

                // Validate SeasonType
                var validSeasons = new[] { "annual", "spring", "summer", "fall", "winter" };
                if (!validSeasons.Contains(request.SeasonType?.ToLower()))
                {
                    _logger.LogError($"Invalid SeasonType. Must be one of: {string.Join(", ", validSeasons)}");
                    return BadRequest(new { Message = $"Invalid SeasonType. Must be one of: {string.Join(", ", validSeasons)}" });
                }

                // Validate coordinates
                if (request.Coordinates == null || !request.Coordinates.Any())
                {
                    _logger.LogError("Coordinates are required and cannot be empty.");
                    return BadRequest(new { Message = "Coordinates are required and cannot be empty." });
                }

                // Validate significant wave height
                if (request.SignificantWaveHeight <= 0)
                {
                    _logger.LogError("SignificantWaveHeight must be greater than 0.");
                    return BadRequest(new { Message = "SignificantWaveHeight must be greater than 0." });
                }

                // Process Coordinates
                var coordinatesProcessed = request.Coordinates.Select(coord => new Coordinate
                {
                    Longitude = coord.Longitude,
                    Latitude = coord.Latitude
                }).ToList();

                // Retrieve the content root path from HttpContext.Items
                var contentRootPath = HttpContext.Items["ContentRootPath"]?.ToString();
                if (string.IsNullOrEmpty(contentRootPath))
                {
                    return BadRequest(new { Message = "Content root path is not available." });
                }

                var sessionId = HttpContext.Items["sessionId"]?.ToString();
                if (string.IsNullOrEmpty(sessionId))
                {
                    return BadRequest(new { Message = "Session ID is not available." });
                }

                // Define the session folder path
                string sessionFolderPath = Path.Combine(contentRootPath, "temp", sessionId);

                // Call WaveScatterDiagramService with the new seasonType parameter
                double targetHeight, reductionFactor;
                _wsdService.CalculateReductionFactor(
                    coordinatesProcessed,
                    request.DataSource,
                    sessionFolderPath,
                    request.ExceedanceProbability,
                    request.SeasonType, // Pass the new seasonType parameter
                    out targetHeight,
                    out reductionFactor
                );

                _logger.LogInformation($"Route processed successfully with DataSource: {request.DataSource}, SeasonType: {request.SeasonType}");

                // Return updated response with new schema
                return Ok(new BkWxRouteResponse
                {
                    DataSource = request.DataSource,
                    Coordinates = coordinatesProcessed,
                    ExceedanceProbability = request.ExceedanceProbability,
                    SignificantWaveHeight = targetHeight,
                    SeasonType = request.SeasonType,
                    ReductionFactor = reductionFactor
                });
            }
            catch (ArgumentException ex)
            {
                _logger.LogError(ex, "Invalid argument provided.");
                return BadRequest(new { Message = ex.Message });
            }
            catch (FileNotFoundException ex)
            {
                _logger.LogError(ex, "Required wave data file not found.");
                return StatusCode(500, new { Message = $"Wave data file not found: {ex.Message}" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing route.");
                return StatusCode(500, new { Message = $"Error processing route: {ex.Message}" });
            }
        }

        [HttpPost("legs")]
        public IActionResult GetVoyageLegs(List<WaypointModel> waypoints)
        {
            try
            {
                if (waypoints == null)
                    return BadRequest();

                var legs = _routeSplitService.GetVoyageLegs(waypoints);
                return Ok(legs);
            }
            catch (Exception)
            {
                return BadRequest();
            }
        }
    }
}

// 4. Updated IWaveScatterDiagramService Interface
public interface IWaveScatterDiagramService
{
    void CalculateReductionFactor(List<Coordinate> coordinates, string dataSource, string sessionFolderPath,
                                 double exceedanceProbability, string seasonType, out double targetHeight, out double reductionFactor);
}

// 5. Updated WaveScatterDiagramService Implementation
public class WaveScatterDiagramService : IWaveScatterDiagramService
{
    private readonly IBkWxRoutes _bkWxRoutes;
    private readonly ILogger<WaveScatterDiagramService> _logger;

    public WaveScatterDiagramService(IBkWxRoutes bkWxRoutes, ILogger<WaveScatterDiagramService> logger)
    {
        _bkWxRoutes = bkWxRoutes;
        _logger = logger;
    }

    public void CalculateReductionFactor(List<Coordinate> coordinates, string dataSource, string sessionFolderPath,
                                       double exceedanceProbability, string seasonType, out double targetHeight, out double reductionFactor)
    {
        try
        {
            // Log the parameters being passed
            _logger.LogInformation($"Calculating reduction factor with DataSource: {dataSource}, SeasonType: {seasonType}");

            // Validate seasonType
            var validSeasons = new[] { "annual", "spring", "summer", "fall", "winter" };
            if (!validSeasons.Contains(seasonType?.ToLower()))
            {
                throw new ArgumentException($"Invalid season type: {seasonType}. Valid values are: {string.Join(", ", validSeasons)}");
            }

            // Create route files (F101.tra, F101.ctl) from coordinates
            CreateRouteFiles(coordinates, dataSource, sessionFolderPath);

            // Call ProcessWaveData with the new seasonType parameter
            _bkWxRoutes.ProcessWaveData(sessionFolderPath, dataSource, seasonType);

            // Process the generated composite.wsd file to calculate reduction factor
            var compositeWsdPath = Path.Combine(sessionFolderPath, "composite.wsd");
            if (!File.Exists(compositeWsdPath))
            {
                throw new FileNotFoundException($"Composite WSD file not found at: {compositeWsdPath}");
            }

            // Calculate reduction factor from the composite WSD
            CalculateReductionFactorFromWsd(compositeWsdPath, exceedanceProbability, out targetHeight, out reductionFactor);

            _logger.LogInformation($"Reduction factor calculated successfully: {reductionFactor} for season: {seasonType}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error calculating reduction factor for season: {seasonType}");
            throw;
        }
    }

    private void CreateRouteFiles(List<Coordinate> coordinates, string dataSource, string sessionFolderPath)
    {
        // Implementation to create F101.tra and F101.ctl files
        // This method should create the route files needed by ProcessWaveData
        // ... existing implementation ...
    }

    private void CalculateReductionFactorFromWsd(string wsdFilePath, double exceedanceProbability,
                                               out double targetHeight, out double reductionFactor)
    {
        // Implementation to read composite.wsd and calculate reduction factor
        // ... existing implementation ...
        targetHeight = 0.0; // Placeholder
        reductionFactor = 0.0; // Placeholder
    }
}

// 6. Example Request JSON Schema
/*
{
  "dataSource": "BMT",
  "coordinates": [
    {
      "longitude": -74.0059,
      "latitude": 40.7128
    },
    {
      "longitude": -0.1276,
      "latitude": 51.5074
    }
  ],
  "exceedanceProbability": 0.01,
  "significantWaveHeight": 8.5,
  "seasonType": "winter"
}
*/

// 7. Example Response JSON Schema
/*
{
  "dataSource": "BMT",
  "coordinates": [
    {
      "longitude": -74.0059,
      "latitude": 40.7128
    },
    {
      "longitude": -0.1276,
      "latitude": 51.5074
    }
  ],
  "exceedanceProbability": 0.01,
  "significantWaveHeight": 8.5,
  "seasonType": "winter",
  "reductionFactor": 0.85
}
*/