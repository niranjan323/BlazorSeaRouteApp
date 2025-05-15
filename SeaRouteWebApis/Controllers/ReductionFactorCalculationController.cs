//using System.Linq;
//using Microsoft.AspNetCore.Mvc;
//using SeaRouteModel.Models;
//using SeaRouteWebApis.Controllers;
//using SeaRouteWebApis.Interfaces;

//namespace NextGenEngApps.DigitalRules.API.Controllers
//{
//    [ApiController]
//    [Route("api/v1/reduction-factor/")]
//    public class ReductionFactorCalculation : SeaRouteBaseController<ReductionFactorRecord>
//    {
//        private readonly IWaveScatterDiagramService _wsdService;
//        private readonly ILogger<ReductionFactorCalculation> _logger;
//        private readonly IRouteSplitService _routeSplitService;

//        public ReductionFactorCalculation(ILoggerFactory loggerFactory, IRepository<ReductionFactorRecord> reductionFactorRepository,
//            IWaveScatterDiagramService wsdService, IRouteSplitService routeSplitService)
//            : base(loggerFactory, reductionFactorRepository)
//        {
//            _wsdService = wsdService;
//            _logger = loggerFactory.CreateLogger<ReductionFactorCalculation>();
//            _routeSplitService = routeSplitService;
//        }

//        // Inside the ProcessRoute method
//        [HttpPost("reduction-factor-calculation/")]
//        public IActionResult ProcessRoute([FromBody] BkWxRouteRequest request)
//        {
//            if (request.ExceedanceProbability <= 0 || request.ExceedanceProbability >= 1)
//            {
//                _logger.LogError("Exceedance probability must be between 0 and 1.");
//                return BadRequest(new { Message = "Exceedance probability must be between 0 and 1." });
//            }
//            // Validate input
//            if (request.WaveType != "ABS" && request.WaveType != "BMT")
//            {
//                _logger.LogError("Invalid WAVETYPE. Must be 'ABS' or 'BMT'.");
//                return BadRequest("Invalid WAVETYPE. Must be 'ABS' or 'BMT'.");
//            }

//            if (request.PointNumber != request.Coordinates.Count)
//            {
//                _logger.LogError("PointNumber does not match the number of coordinates provided.");
//                return BadRequest("PointNumber does not match the number of coordinates provided.");
//            }

//            try
//            {
//                // Process Coordinates
//                var coordinatesProcessed = request.Coordinates.Select(coord => new
//                Coordinate
//                {
//                    Longitude = coord.Longitude,
//                    Latitude = coord.Latitude
//                }).ToList();




//                // Retrieve the content root path from HttpContext.Items
//                var contentRootPath = HttpContext.Items["ContentRootPath"]?.ToString();

//                if (string.IsNullOrEmpty(contentRootPath))
//                {
//                    return BadRequest("Content root path is not available.");
//                }


//                var sessionId = HttpContext.Items["sessionId"]?.ToString();
//                if (string.IsNullOrEmpty(sessionId))
//                {
//                    return BadRequest("Session ID is not available.");
//                }

//                // Define the session folder path
//                string sessionFolderPath = Path.Combine(contentRootPath, "temp", sessionId);


//                double targetHeight, reductionFactor;
//                _wsdService.CalculateReductionFactor(coordinatesProcessed, request.WaveType, sessionFolderPath, request.ExceedanceProbability, out targetHeight, out reductionFactor);

//                _logger.LogInformation("Route processed successfully.");

//                return Ok(new
//                {
//                    WaveType = request.WaveType,
//                    Coordinates = coordinatesProcessed,
//                    ExceedanceProbability = request.ExceedanceProbability,
//                    TargetWaveHeight = targetHeight,
//                    ReductionFactor = reductionFactor
//                });
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error processing route.");
//                return StatusCode(500, $"Error processing route: {ex.Message}");
//            }
//        }

//        [HttpPost("legs")]
//        public IActionResult GetVoyageLegs(List<WaypointModel> waypoints)
//        {
//            try
//            {
//                if (waypoints == null)
//                    return BadRequest();

//                var legs = _routeSplitService.GetVoyageLegs(waypoints);

//                return Ok(legs);
//            }
//            catch (Exception)
//            {
//                return BadRequest();
//            }
//        }


//    }
//}
