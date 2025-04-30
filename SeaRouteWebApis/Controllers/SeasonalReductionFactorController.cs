using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SeaRouteModel.Models;
using SeaRouteWebApis.Interfaces;
using SeaRouteWebApis.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SeaRouteWebApis.Controllers
{
    [Route("api/v1/reductionFactors")]
    [ApiController]
    public class SeasonalReductionFactorController : SeaRouteBaseController
    {
        private readonly IRepository _repository;
        private readonly ILogger _logger;

        public SeasonalReductionFactorController(ILoggerFactory loggerFactory, IRepository repository)
            : base(loggerFactory, repository)
        {
            _repository = repository;
            _logger = loggerFactory.CreateLogger<SeasonalReductionFactorController>();
        }

        [HttpPost("calculate")]
        public ActionResult<RouteReductionFactorResult> CalculateReductionFactors([FromBody] RouteModel routeModel)
        {
            try
            {
                _logger.LogInformation($"Calculating reduction factors for route: {routeModel.RouteName}, Season: {routeModel.SeasonalType}");

                if (routeModel == null)
                {
                    return BadRequest("Route model is required");
                }

                // Create result object to hold all seasonal calculation results
                var result = new RouteReductionFactorResult
                {
                    MainRoute = new SeasonalRouteInfo
                    {
                        DeparturePort = routeModel.MainDeparturePortSelection.Port?.Name ?? string.Empty,
                        ArrivalPort = routeModel.MainArrivalPortSelection.Port?.Name ?? string.Empty,
                        Distance = 1200 // Default distance, should be calculated or provided in the actual implementation
                    },
                    RouteLegs = new List<RouteSegmentInfo>()
                };

                // First calculate the base (Annual) reduction factors
                CalculateBaseReductionFactors(routeModel, result);

                // Apply seasonal correction if needed
                ApplySeasonalCorrection(routeModel.SeasonalType, result);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating reduction factors");
                return StatusCode(500, "An error occurred while calculating reduction factors");
            }
        }

        private void CalculateBaseReductionFactors(RouteModel routeModel, RouteReductionFactorResult result)
        {
            // Here you would implement the logic to calculate the base reduction factors
            // For demo purposes, we'll use sample values similar to your screenshot

            // Set the main route reduction factor
            result.MainRoute.ReductionFactor = 0.8583;

            // Add route legs (segments) with their reduction factors
            // In a real implementation, you would calculate these based on your business logic

            // Example leg 1
            result.RouteLegs.Add(new RouteSegmentInfo
            {
                DeparturePort = "Port 1",
                ArrivalPort = "Port 2",
                ReductionFactor = 0.82,
                Distance = 200
            });

            // Example leg 2
            result.RouteLegs.Add(new RouteSegmentInfo
            {
                DeparturePort = "Port 2",
                ArrivalPort = "Port 3",
                ReductionFactor = 0.81,
                Distance = 1000
            });
        }

        private void ApplySeasonalCorrection(string seasonalType, RouteReductionFactorResult result)
        {
            double seasonalFactor = 1.0; // Default - no adjustment for Annual

            // Apply seasonal correction based on selected season
            switch (seasonalType?.ToLower())
            {
                case "summer":
                    seasonalFactor = 0.9;
                    break;
                case "spring":
                    seasonalFactor = 0.93;
                    break;
                case "fall":
                    seasonalFactor = 0.97;
                    break;
                case "winter":
                case "annual":
                default:
                    // No correction needed for Winter or Annual (default)
                    seasonalFactor = 1.0;
                    break;
            }

            // Apply the correction to the main route
            if (seasonalFactor != 1.0)
            {
                result.MainRoute.ReductionFactor *= seasonalFactor;
            }

            // Apply the correction to each route leg
            foreach (var leg in result.RouteLegs)
            {
                if (seasonalFactor != 1.0)
                {
                    leg.ReductionFactor *= seasonalFactor;
                }
            }

            // Set the seasonal type used for the calculation
            result.SeasonalType = seasonalType ?? "Annual";
        }
    }

    // Model classes for the calculation results
    public class RouteReductionFactorResult
    {
        public string SeasonalType { get; set; } = "Annual";
        public SeasonalRouteInfo MainRoute { get; set; } = new SeasonalRouteInfo();
        public List<RouteSegmentInfo> RouteLegs { get; set; } = new List<RouteSegmentInfo>();
    }

    public class SeasonalRouteInfo
    {
        public string DeparturePort { get; set; } = string.Empty;
        public string ArrivalPort { get; set; } = string.Empty;
        public double ReductionFactor { get; set; }
        public double Distance { get; set; }
    }
    public class RouteSegmentInfo
    {
        public int SegmentIndex { get; set; }
        public string StartPointName { get; set; }
        public string EndPointName { get; set; }
        public double Distance { get; set; }
        public double DurationHours { get; set; }
        public string Units { get; set; } = "km";
        public double[] StartCoordinates { get; set; } // [longitude, latitude]
        public double[] EndCoordinates { get; set; } // [longitude, latitude]
    }
}