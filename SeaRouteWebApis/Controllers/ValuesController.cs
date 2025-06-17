using Microsoft.AspNetCore.Mvc;
using SeaRouteModel.Models;
using SeaRouteWebApis.Controllers;
using SeaRouteWebApis.Interfaces;

namespace NextGenEngApps.DigitalRules.API.Controllers
{
    [ApiController]
    [Route("calculations/reduction-factors/")]
    public class ReductionFactorCalculation : SeaRouteBaseController<ReductionFactor>
    {
        private readonly IWaveScatterDiagramService _wsdService;
        private readonly ILogger<ReductionFactorCalculation> _logger;
        private readonly IRouteSplitService _routeSplitService;

        public ReductionFactorCalculation(ILoggerFactory loggerFactory, IRepository<ReductionFactor> reductionFactorRepository,
            IWaveScatterDiagramService wsdService, IRouteSplitService routeSplitService)
            : base(loggerFactory, reductionFactorRepository)
        {
            _wsdService = wsdService;
            _logger = loggerFactory.CreateLogger<ReductionFactorCalculation>();
            _routeSplitService = routeSplitService;
        }


        [HttpPost("raw")]
        public IActionResult CalculateRawReductionFactor([FromBody] RawReductionFactorRequest request)
        {
            // Validate input
            if (request.ExceedanceProbability <= 0 || request.ExceedanceProbability >= 1)
            {
                _logger.LogError("Exceedance probability must be between 0 and 1.");
                return BadRequest(new { Message = "Exceedance probability must be between 0 and 1." });
            }

            if (request.DataSource != "ABS" && request.DataSource != "BMT")
            {
                _logger.LogError("Invalid DataSource. Must be 'ABS' or 'BMT'.");
                return BadRequest("Invalid DataSource. Must be 'ABS' or 'BMT'.");
            }

            try
            {
                // Process Coordinates
                var coordinatesProcessed = request.SegmentCoordinates.Select(coord => new Coordinate
                {
                    Longitude = coord.Longitude,
                    Latitude = coord.Latitude
                }).ToList();

                // Retrieve the content root path from HttpContext.Items
                var contentRootPath = HttpContext.Items["ContentRootPath"]?.ToString();
                if (string.IsNullOrEmpty(contentRootPath))
                {
                    return BadRequest("Content root path is not available.");
                }

                var sessionId = HttpContext.Items["sessionId"]?.ToString();
                if (string.IsNullOrEmpty(sessionId))
                {
                    return BadRequest("Session ID is not available.");
                }

                // Define the session folder path
                string sessionFolderPath = Path.Combine(contentRootPath, "temp", sessionId);

                double targetHeight, reductionFactor;
                _wsdService.CalculateReductionFactor(coordinatesProcessed, request.DataSource, request.SeasonType,
                    sessionFolderPath, request.ExceedanceProbability, out targetHeight, out reductionFactor);

                _logger.LogInformation("Raw reduction factor calculated successfully.");

                return Ok(new RawReductionFactorResponse
                {
                    SignificantWaveHeight = targetHeight,
                    RawReductionFactor = reductionFactor
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating raw reduction factor.");
                return StatusCode(500, $"Error calculating raw reduction factor: {ex.Message}");
            }
        }

        [HttpPost("segment")]
        public async Task<IActionResult> CalculateSegmentReductionFactors([FromBody] SegmentReductionFactorRequest request)
        {
            try
            {
                // Step 1: Get annual reduction factor (ABS)
                var annualRequest = new RawReductionFactorRequest
                {
                    DataSource = "ABS",
                    SeasonType = "annual",
                    ExceedanceProbability = request.ExceedanceProbability,
                    SegmentCoordinates = request.Coordinates
                };

                var annualResult = await CallRawReductionFactorInternal(annualRequest);
                if (annualResult == null)
                {
                    return BadRequest("Failed to calculate annual reduction factor.");
                }

                double annualRF = annualResult.RawReductionFactor;
                double annualHs = annualResult.SignificantWaveHeight;

                // Apply correction if requested
                if (request.Correction.HasValue && request.Correction.Value)
                {
                    annualRF = Math.Max(annualRF, 0.80d);
                }

                // Step 2: Get seasonal significant wave heights (BMT)
                var seasons = new[] { "spring", "summer", "fall", "winter" };
                var seasonalFactors = new Dictionary<string, double>();
                var seasonalRFs = new Dictionary<string, double>();

                foreach (var season in seasons)
                {
                    var seasonalRequest = new RawReductionFactorRequest
                    {
                        DataSource = "BMT",
                        SeasonType = season,
                        ExceedanceProbability = request.ExceedanceProbability,
                        SegmentCoordinates = request.Coordinates
                    };

                    var seasonalResult = await CallRawReductionFactorInternal(seasonalRequest);
                    if (seasonalResult == null)
                    {
                        return BadRequest($"Failed to calculate {season} reduction factor.");
                    }

                    // Calculate seasonal factor
                    double seasonalFactor = seasonalResult.SignificantWaveHeight / annualHs;
                    seasonalFactors[season] = seasonalFactor;

                    // Calculate seasonal RF
                    seasonalRFs[season] = seasonalFactor * annualRF;
                }

                return Ok(new RouteReductionFactors
                {
                    ReductionFactors = new ReductionFactors
                    {

                        Annual = annualRF,
                        Spring = seasonalRFs["spring"],
                        Summer = seasonalRFs["summer"],
                        Fall = seasonalRFs["fall"],
                        Winter = seasonalRFs["winter"]
                    }
                }); ;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating segment reduction factors.");
                return StatusCode(500, $"Error calculating segment reduction factors: {ex.Message}");
            }
        }


        [HttpPost("route")]
        public async Task<IActionResult> CalculateVoyageLegReductionFactors([FromBody] VoyageLegReductionFactorRequest request)
        {
            try
            {
                // Step 1: Calculate route reduction factors
                // Combine all coordinates from voyage legs and remove duplicates
                var allCoordinates = new List<Coordinate>();
                var coordinateSet = new HashSet<string>();

                foreach (var leg in request.VoyageLegs)
                {
                    foreach (var coord in leg.Coordinates)
                    {
                        var coordKey = $"{coord.Latitude},{coord.Longitude}";
                        if (coordinateSet.Add(coordKey))
                        {
                            allCoordinates.Add(coord);
                        }
                    }
                }

                // Call segment calculation for the entire route
                var routeResult = await CalculateReductionFactorsForSegment(allCoordinates, request.ExceedanceProbability, request.Correction);
                if (routeResult == null)
                {
                    return BadRequest("Failed to calculate route reduction factors.");
                }

                // Step 2: Calculate reduction factors for each voyage leg
                var voyageLegResults = new List<VoyageLegReductionFactors>();

                foreach (var leg in request.VoyageLegs)
                {
                    var legResult = await CalculateReductionFactorsForSegment(leg.Coordinates, request.ExceedanceProbability, false);
                    if (legResult == null)
                    {
                        return BadRequest($"Failed to calculate reduction factors for voyage leg {leg.VoyageLegOrder}.");
                    }

                    // Step 3: Apply correction to voyage leg reduction factors
                    var correctedFactors = new ReductionFactors
                    {
                        Annual = request.Correction ? Math.Min(legResult.Annual, routeResult.Annual) : legResult.Annual,
                        Spring = request.Correction ? Math.Min(legResult.Spring, routeResult.Spring) : legResult.Spring,
                        Summer = request.Correction ? Math.Min(legResult.Summer, routeResult.Summer) : legResult.Summer,
                        Fall = request.Correction ? Math.Min(legResult.Fall, routeResult.Fall) : legResult.Fall,
                        Winter = request.Correction ? Math.Min(legResult.Winter, routeResult.Winter) : legResult.Winter
                    };

                    voyageLegResults.Add(new VoyageLegReductionFactors
                    {
                        VoyageLegOrder = leg.VoyageLegOrder,
                        ReductionFactors = correctedFactors
                    });
                }

                // Prepare response
                var response = new VoyageLegReductionFactorResponse
                {
                    Route = new RouteReductionFactors
                    {
                        ReductionFactors = new ReductionFactors
                        {
                            Annual = routeResult.Annual,
                            Spring = routeResult.Spring,
                            Summer = routeResult.Summer,
                            Fall = routeResult.Fall,
                            Winter = routeResult.Winter
                        }
                    },
                    VoyageLegs = voyageLegResults
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating voyage leg reduction factors.");
                return StatusCode(500, $"Error calculating voyage leg reduction factors: {ex.Message}");
            }
        }




        // Helper method to calculate reduction factors for a segment
        private async Task<ReductionFactors> CalculateReductionFactorsForSegment(List<Coordinate> coordinates, double exceedanceProbability, bool applyCorrection)
        {
            try
            {
                // Step 1: Get annual reduction factor (ABS)
                var annualRequest = new RawReductionFactorRequest
                {
                    DataSource = "ABS",
                    SeasonType = "annual",
                    ExceedanceProbability = exceedanceProbability,
                    SegmentCoordinates = coordinates
                };

                var annualResult = await CallRawReductionFactorInternal(annualRequest);
                if (annualResult == null)
                {
                    return null;
                }

                double annualRF = annualResult.RawReductionFactor;
                double annualHs = annualResult.SignificantWaveHeight;

                // Apply correction if requested
                if (applyCorrection)
                {
                    annualRF = Math.Max(annualRF, 0.80d);
                }

                // Step 2: Get seasonal significant wave heights (BMT)
                var seasons = new[] { "spring", "summer", "fall", "winter" };
                var seasonalRFs = new Dictionary<string, double>();

                foreach (var season in seasons)
                {
                    var seasonalRequest = new RawReductionFactorRequest
                    {
                        DataSource = "BMT",
                        SeasonType = season,
                        ExceedanceProbability = exceedanceProbability,
                        SegmentCoordinates = coordinates
                    };

                    var seasonalResult = await CallRawReductionFactorInternal(seasonalRequest);
                    if (seasonalResult == null)
                    {
                        return null;
                    }

                    // Calculate seasonal factor
                    double seasonalFactor = seasonalResult.SignificantWaveHeight / annualHs;

                    // Calculate seasonal RF
                    seasonalRFs[season] = seasonalFactor * annualRF;
                }

                return new ReductionFactors
                {
                    Annual = annualRF,
                    Spring = seasonalRFs["spring"],
                    Summer = seasonalRFs["summer"],
                    Fall = seasonalRFs["fall"],
                    Winter = seasonalRFs["winter"]
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating reduction factors for segment.");
                return null;
            }
        }

        private async Task<RawReductionFactorResponse> CallRawReductionFactorInternal(RawReductionFactorRequest request)
        {
            try
            {
                var coordinatesProcessed = request.SegmentCoordinates.Select(coord => new Coordinate
                {
                    Longitude = coord.Longitude,
                    Latitude = coord.Latitude
                }).ToList();

                var contentRootPath = HttpContext.Items["ContentRootPath"]?.ToString();
                var sessionId = HttpContext.Items["sessionId"]?.ToString();

                if (string.IsNullOrEmpty(contentRootPath) || string.IsNullOrEmpty(sessionId))
                {
                    return null;
                }

                string sessionFolderPath = Path.Combine(contentRootPath, "temp", sessionId);

                double targetHeight, reductionFactor;
                _wsdService.CalculateReductionFactor(coordinatesProcessed, request.DataSource, request.SeasonType,
                    sessionFolderPath, request.ExceedanceProbability, out targetHeight, out reductionFactor);

                return new RawReductionFactorResponse
                {
                    SignificantWaveHeight = targetHeight,
                    RawReductionFactor = reductionFactor
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in internal raw reduction factor calculation.");
                return null;
            }
        }
    }
}