using Microsoft.AspNetCore.Mvc;
using NextGenEngApps.DigitalRules.CRoute.DAL.Models;
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

        // Task Two: Segment reduction factors calculation
        // Endpoint matches task specification: "/calculations/reduction-factors/segment"
        [HttpPost("segment")]
        public async Task<IActionResult> CalculateSegmentReductionFactors([FromBody] SegmentReductionFactorRequest request)
        {
            try
            {
                var result = await CalculateReductionFactorsForSegment(request.Coordinates, request.ExceedanceProbability, request.Correction);
                if (result == null)
                {
                    return BadRequest("Failed to calculate segment reduction factors.");
                }

                return Ok(new RouteReductionFactors
                {
                    ReductionFactors = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating segment reduction factors.");
                return StatusCode(500, $"Error calculating segment reduction factors: {ex.Message}");
            }
        }

        // Task One: Route and voyage leg reduction factors calculation
        // Note: Task specification mentions "/segment" endpoint, but this is logically a route-level operation
        // Using "/route" endpoint for clarity and to avoid conflicts with segment endpoint
        [HttpPost("route")]
        public async Task<IActionResult> ApplyVoyageLegCorrection([FromBody] VoyageLegReductionFactorRequest request)
        {
            try
            {
                // Step 1: Calculate route reduction factors
                var routeCoordinates = CreateRouteCoordinates(request.VoyageLegs);
                var routeResult = await CalculateReductionFactorsForSegment(routeCoordinates, request.ExceedanceProbability, request.Correction);
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
                    // Modified by Niranjan: Added proper upper bound correction on annual RF and voyage leg correction implementation
                    var correctedFactors = ApplyVoyageLegCorrection(legResult, routeResult);

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
                        ReductionFactors = routeResult
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

        // Modified by Niranjan: Abstracted route coordinate creation as requested in feedback point 5.1
        private List<Coordinate> CreateRouteCoordinates(List<VoyageLeg> voyageLegs)
        {
            var allCoordinates = new List<Coordinate>();
            var coordinateSet = new HashSet<string>();

            foreach (var leg in voyageLegs)
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

            return allCoordinates;
        }

        // Helper method to calculate reduction factors for a segment
        private async Task<ReductionFactors> CalculateReductionFactorsForSegment(List<Coordinate> coordinates, double exceedanceProbability, bool? applyCorrection)
        {
            try
            {
                // Step 1: Calculate ABS annual reduction factor
                var absAnnualRF = await CalculateABSAnnualReductionFactor(coordinates, exceedanceProbability, applyCorrection);
                if (!absAnnualRF.HasValue)
                {
                    return null;
                }

                // Step 2: Calculate BMT seasonal factors
                var bmtSeasonalFactors = await CalculateBMTSeasonalFactors(coordinates, exceedanceProbability);
                if (bmtSeasonalFactors == null)
                {
                    return null;
                }

                // Step 3: Calculate seasonal reduction factors
                var seasonalRFs = CalculateSeasonalReductionFactors(absAnnualRF.Value, bmtSeasonalFactors);

                return new ReductionFactors
                {
                    Annual = absAnnualRF.Value,
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

        // Modified by Niranjan: Abstracted ABS annual reduction factor calculation as requested in feedback point 5.2
        private async Task<double?> CalculateABSAnnualReductionFactor(List<Coordinate> coordinates, double exceedanceProbability, bool? applyCorrection)
        {
            try
            {
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

                // Modified by Niranjan: Implemented upper bound correction on annual RF as requested in feedback point 1
                if (applyCorrection.HasValue && applyCorrection.Value)
                {
                    annualRF = ApplyAnnualCorrection(annualRF);
                }

                return annualRF;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating ABS annual reduction factor.");
                return null;
            }
        }

        // Modified by Niranjan: Abstracted BMT seasonal factors calculation as requested in feedback point 5.2
        private async Task<Dictionary<string, double>> CalculateBMTSeasonalFactors(List<Coordinate> coordinates, double exceedanceProbability)
        {
            try
            {
                // Get BMT annual wave height
                var bmtAnnualRequest = new RawReductionFactorRequest
                {
                    DataSource = "BMT",
                    SeasonType = "annual",
                    ExceedanceProbability = exceedanceProbability,
                    SegmentCoordinates = coordinates
                };

                var bmtAnnualResult = await CallRawReductionFactorInternal(bmtAnnualRequest);
                if (bmtAnnualResult == null)
                {
                    return null;
                }

                double bmtAnnualHs = bmtAnnualResult.SignificantWaveHeight;

                // Get seasonal significant wave heights and calculate factors
                var seasons = new[] { "spring", "summer", "fall", "winter" };
                var seasonalFactors = new Dictionary<string, double>();

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

                    // Calculate uncorrected seasonal factor
                    double uncorrectedSeasonalFactor = seasonalResult.SignificantWaveHeight / bmtAnnualHs;

                    // Modified by Niranjan: Implemented correction on seasonal factor as requested in feedback point 2
                    double correctedSeasonalFactor = ApplySeasonalFactorCorrection(uncorrectedSeasonalFactor);

                    seasonalFactors[season] = correctedSeasonalFactor;
                }

                return seasonalFactors;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating BMT seasonal factors.");
                return null;
            }
        }

        // Modified by Niranjan: Abstracted seasonal reduction factors calculation as requested in feedback point 5.2
        private Dictionary<string, double> CalculateSeasonalReductionFactors(double absAnnualRF, Dictionary<string, double> bmtSeasonalFactors)
        {
            var seasonalRFs = new Dictionary<string, double>();

            foreach (var season in bmtSeasonalFactors.Keys)
            {
                // Calculate uncorrected seasonal RF
                double uncorrectedSeasonalRF = absAnnualRF * bmtSeasonalFactors[season];

                // Modified by Niranjan: Implemented correction on seasonal RF as requested in feedback point 3
                double correctedSeasonalRF = ApplySeasonalRFCorrection(uncorrectedSeasonalRF);

                seasonalRFs[season] = correctedSeasonalRF;
            }

            return seasonalRFs;
        }

        // Modified by Niranjan: Implemented upper bound correction on annual RF as requested in feedback point 1
        private double ApplyAnnualCorrection(double annualRF)
        {
            // Apply upper bound correction: no less than 0.8 but no greater than 1
            return Math.Min(Math.Max(annualRF, 0.8), 1.0);
        }

        // Modified by Niranjan: Implemented correction on seasonal factor as requested in feedback point 2
        private double ApplySeasonalFactorCorrection(double seasonalFactor)
        {
            // Apply correction: no less than 0.8 and no greater than 1
            return Math.Min(Math.Max(seasonalFactor, 0.8), 1.0);
        }

        // Modified by Niranjan: Implemented correction on seasonal RF as requested in feedback point 3
        private double ApplySeasonalRFCorrection(double seasonalRF)
        {
            // Apply correction: no less than 0.65 but no greater than 1.0
            return Math.Min(Math.Max(seasonalRF, 0.65), 1.0);
        }

        // Modified by Niranjan: Renamed from ApplySeasonalCorrection to ApplyVoyageLegCorrection as requested in feedback point 4
        // Also added proper voyage leg correction implementation and remember to apply correction on voyage leg annual RF
        private ReductionFactors ApplyVoyageLegCorrection(ReductionFactors legResult, ReductionFactors routeResult)
        {
            return new ReductionFactors
            {
                // Modified by Niranjan: Apply voyage leg correction on annual RF - ensure leg annual RF is not greater than route annual RF
                Annual = Math.Min(legResult.Annual, routeResult.Annual),
                // Modified by Niranjan: Apply voyage leg correction on seasonal RFs - ensure leg seasonal RF is not greater than route seasonal RF of same season
                Spring = Math.Min(legResult.Spring, routeResult.Spring),
                Summer = Math.Min(legResult.Summer, routeResult.Summer),
                Fall = Math.Min(legResult.Fall, routeResult.Fall),
                Winter = Math.Min(legResult.Winter, routeResult.Winter)
            };
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