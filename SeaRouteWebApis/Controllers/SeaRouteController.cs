using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SeaRouteModel.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SeaRouteWebApis.Controllers
{
    [Route("api/searoute")]
    [ApiController]
    public class SeaRouteController : ControllerBase
    {
        private readonly ILogger<SeaRouteController> _logger;

        public SeaRouteController(ILogger<SeaRouteController> logger)
        {
            _logger = logger;
        }

        [HttpPost("short_voyage_reduction_factor")]
        public ActionResult ShortVoyageReductionFactor([FromBody] ReductionFactor reductionFactor)
        {
            try
            {
                if (reductionFactor == null)
                {
                    return BadRequest("Invalid object.");
                }

                DateTime dateOfArrivalDateTime = reductionFactor.DateOfArrival.ToDateTime(TimeOnly.MinValue);
                DateTime dateOfDepartureDateTime = reductionFactor.DateOfDeparture.ToDateTime(TimeOnly.MinValue);

                reductionFactor.Duration = Convert.ToInt32((dateOfArrivalDateTime - dateOfDepartureDateTime).TotalHours) +
                                           Convert.ToInt32((reductionFactor.ETA - reductionFactor.ETD).TotalHours);
                reductionFactor.DurationOk = reductionFactor.Duration > 0 && reductionFactor.Duration <= 72 ? "OK" : "NA";

                DateTime weatherForecastDateTime = reductionFactor.WeatherForecastDate.ToDateTime(TimeOnly.MinValue);

                reductionFactor.WeatherForecastBeforeETD = Convert.ToInt32((dateOfDepartureDateTime - weatherForecastDateTime).TotalHours) +
                                                           Convert.ToInt32((reductionFactor.ETD.Hour - reductionFactor.WeatherForecasetTime.Hour));
                reductionFactor.WeatherForecastBeforeETDOK = reductionFactor.WeatherForecastBeforeETD > 0 && reductionFactor.WeatherForecastBeforeETD <= 6 ? "OK" : "NA";

                if (reductionFactor.WaveHeightHswell > 0 && reductionFactor.WaveHeightHwind > 0)
                {
                    double value = Math.Pow((double)reductionFactor.WaveHeightHswell, 2) + Math.Pow((double)reductionFactor.WaveHeightHwind, 2);
                    reductionFactor.WaveHsmax = Math.Round(Math.Sqrt(value), 2).ToString();
                }
                else
                {
                    reductionFactor.WaveHsmax = "NA";
                }

                if (reductionFactor.DurationOk == "OK" && reductionFactor.WeatherForecastBeforeETDOK == "OK" && reductionFactor.Breadth > 0)
                {
                    double result = Math.Max(Math.Min(Convert.ToDouble(reductionFactor.WaveHsmax) / (2 * Math.Sqrt(reductionFactor.Breadth)) + 0.4, 1), 0.6);
                    reductionFactor.ShortVoyageReductionFactor = Convert.ToDecimal(Math.Round(result, 2));
                }
                else
                {
                    reductionFactor.ShortVoyageReductionFactor = 1;
                }

                reductionFactor.Xlist = new List<double> { 0.00, 2.82, 8.48, 12.00 };
                reductionFactor.Ylist = new List<double> { 0.6, 0.6, 1.0, 1.0 };
                reductionFactor.XValues = reductionFactor.Xlist.Select(x => (decimal)x).ToList();
                reductionFactor.YValues = reductionFactor.Ylist.Select(x => (decimal)x).ToList();

                reductionFactor.CommonX = Convert.ToDecimal(reductionFactor.WaveHsmax);
                reductionFactor.CommonY = reductionFactor.ShortVoyageReductionFactor;

                return Ok(reductionFactor);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while processing ShortVoyageReductionFactor");
                return BadRequest("Error occurred: " + ex.Message);
            }
        }
    }
}
