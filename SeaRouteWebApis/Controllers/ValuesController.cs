using Microsoft.AspNetCore.Mvc;
using NextGenEngApps.DigitalRules.CRoute.API.Controllers;
using NextGenEngApps.DigitalRules.CRoute.API.Models;
using NextGenEngApps.DigitalRules.CRoute.API.Services;
using NextGenEngApps.DigitalRules.CRoute.API.Services.Interfaces;
using NextGenEngApps.DigitalRules.CRoute.DAL.Models;
using SeaRouteModel.Models;
using SeaRouteWebApis.Controllers;
using SeaRouteWebApis.Interfaces;
using System.Linq;

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
                var coordinatesProcessed = request.SegmentCoordinates.Select(coord => new Coordinate
                {
                    Longitude = coord.Longitude,
                    Latitude = coord.Latitude
                }).ToList();
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


        [HttpPost("route")]
        public async Task<IActionResult> CalculateRouteReductionFactors([FromBody] VoyageLegReductionFactorRequest request)
        {
            try
            {
                // 1. Abstract the creation of route coordinates
                var allCoordinates = ExtractRouteCoordinates(request.VoyageLegs);

                // Calculate route-level reduction factors
                var routeResult = await CalculateReductionFactorsForSegment(allCoordinates, request.ExceedanceProbability, request.Correction);
                if (routeResult == null)
                {
                    return BadRequest("Failed to calculate route reduction factors.");
                }

                // 2. Abstract the calculation of voyage leg reduction factors
                var voyageLegResults = await CalculateVoyageLegReductionFactors(request.VoyageLegs, request.ExceedanceProbability, routeResult, request.Correction);
                if (voyageLegResults == null)
                {
                    return BadRequest("Failed to calculate voyage leg reduction factors.");
                }

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

        private List<Coordinate> ExtractRouteCoordinates(List<VoyageLeg> voyageLegs)
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
        private async Task<List<VoyageLegReductionFactors>> CalculateVoyageLegReductionFactors(List<VoyageLeg> voyageLegs,double exceedanceProbability,ReductionFactors routeResult,bool applyCorrection)
        {
            var voyageLegResults = new List<VoyageLegReductionFactors>();

            foreach (var leg in voyageLegs)
            {
                var legResult = await CalculateReductionFactorsForSegment(leg.Coordinates, exceedanceProbability, false);
                if (legResult == null)
                {
                    _logger.LogError($"Failed to calculate reduction factors for voyage leg {leg.VoyageLegOrder}.");
                    return null;
                }

                // Apply voyage leg correction using the route result
                var correctedFactors = ApplyVoyageLegCorrection(legResult, routeResult, applyCorrection);

                voyageLegResults.Add(new VoyageLegReductionFactors
                {
                    VoyageLegOrder = leg.VoyageLegOrder,
                    ReductionFactors = correctedFactors
                });
            }

            return voyageLegResults;
        }
        private async Task<ReductionFactors> CalculateReductionFactorsForSegment(List<Coordinate> coordinates, double exceedanceProbability, bool applyCorrection)
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
                double annualHs = annualResult.SignificantWaveHeight;
                if (applyCorrection)
                {
                    annualRF = ApplyAnnualCorrection(annualRF);
                }
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

                    double uncorrectedSeasonalFactor = seasonalResult.SignificantWaveHeight / bmtAnnualHs;
                    double correctedSeasonalFactor = ApplySeasonalFactorCorrection(uncorrectedSeasonalFactor);
                    double uncorrectedSeasonalRF = annualRF * correctedSeasonalFactor;
                    double correctedSeasonalRF = ApplySeasonalRFCorrection(uncorrectedSeasonalRF);

                    seasonalRFs[season] = correctedSeasonalRF;
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
        private double ApplyAnnualCorrection(double annualRF)
        {
            return Math.Min(Math.Max(annualRF, 0.80), 1.0);
        }
        private ReductionFactors ApplyVoyageLegCorrection(ReductionFactors legResult, ReductionFactors routeResult, bool applyCorrection)
        {

            return new ReductionFactors
            {
                Annual = Math.Min(legResult.Annual, routeResult.Annual),
                Spring = Math.Min(legResult.Spring, routeResult.Spring),
                Summer = Math.Min(legResult.Summer, routeResult.Summer),
                Fall = Math.Min(legResult.Fall, routeResult.Fall),
                Winter = Math.Min(legResult.Winter, routeResult.Winter)
            };
        }
        private double ApplySeasonalFactorCorrection(double seasonalFactor)
        {
            return Math.Min(Math.Max(seasonalFactor, 0.8), 1.0);
        }
        private double ApplySeasonalRFCorrection(double seasonalRF)
        {
            return Math.Min(Math.Max(seasonalRF, 0.65), 1.0);
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
