using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SeaRouteModel.Models;
using SeaRouteWebApis.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SeaRouteWebApis.Controllers
{
    [Route("api/v1/short_voyage/")]
    [ApiController]
    public class ShortVoyageRecordsController : SeaRouteBaseController<ShortVoyageRecord>
    {
        public ShortVoyageRecordsController(ILoggerFactory loggerFactory, IRepository<ShortVoyageRecord> repository) : base(loggerFactory, repository)
        {
        }

        [HttpPost("short_voyage_reduction_factor")]
        public ActionResult ShortVoyageReductionFactor(ReductionFactor reductionFactor)
        {
            try
            {
                if (reductionFactor == null)
                {
                    return BadRequest("Invalid object.");
                }

                DateTime dateOfArrivalDateTime = reductionFactor.DateOfArrival.ToDateTime(TimeOnly.MinValue);
                DateTime dateOfDepartureDateTime = reductionFactor.DateOfDeparture.ToDateTime(TimeOnly.MinValue);

                reductionFactor.Duration = Convert.ToInt32((dateOfArrivalDateTime - dateOfDepartureDateTime).TotalHours) + Convert.ToInt32((reductionFactor.ETA - reductionFactor.ETD).TotalHours);
                reductionFactor.DurationOk = reductionFactor.Duration > 0 && reductionFactor.Duration <= 72 ? "OK" : "NA";
                DateTime weatherForecastDateTime = reductionFactor.WeatherForecastDate.ToDateTime(TimeOnly.MinValue);

                reductionFactor.WeatherForecastBeforeETD = Convert.ToInt32((dateOfDepartureDateTime - weatherForecastDateTime).TotalHours) +
                Convert.ToInt32((reductionFactor.ETD.Hour - reductionFactor.WeatherForecasetTime.Hour));
                reductionFactor.WeatherForecastBeforeETDOK = reductionFactor.WeatherForecastBeforeETD > 0 && reductionFactor.WeatherForecastBeforeETD <= 6 ? "OK" : "NA";


                if (reductionFactor.WaveHeightHswell > 0 && reductionFactor.WaveHeightHwind > 0)
                {
                    var value = (reductionFactor.WaveHeightHswell * reductionFactor.WaveHeightHswell) + (reductionFactor.WaveHeightHwind * reductionFactor.WaveHeightHwind);


                    double result = Math.Sqrt(Convert.ToDouble(value));


                    result = Math.Round(result, 2);


                    reductionFactor.WaveHsmax = result.ToString();
                }
                else
                {
                    reductionFactor.WaveHsmax = "NA";
                }

                var x = 2 * Math.Sqrt(reductionFactor.Breadth);

                if (reductionFactor.DurationOk == "OK" && reductionFactor.WeatherForecastBeforeETDOK == "OK" && reductionFactor.Breadth > 0)
                {

                    double result = Math.Max(Math.Min(Convert.ToDouble(reductionFactor.WaveHsmax) / (2 * Math.Sqrt(Convert.ToDouble(reductionFactor.Breadth))) + 0.4, 1), 0.6);
                    result = Math.Round(result, 2);
                    reductionFactor.ShortVoyageReductionFactor = Convert.ToDecimal(result);
                }
                else
                {
                    reductionFactor.ShortVoyageReductionFactor = 1;
                }

                reductionFactor.Xlist = new List<double> { 0.00, 2.82, 8.48, 12.00 };
                reductionFactor.Ylist = new List<double> { 0.6, 0.6, 1.0, 1.0 };
                reductionFactor.XValues = reductionFactor.Xlist.Select(x => (decimal)x).ToList();
                reductionFactor.YValues = reductionFactor.Ylist.Select(x => (decimal)x).ToList();

                // Common point to highlight
                reductionFactor.CommonX = Convert.ToDecimal(reductionFactor.WaveHsmax); ;// 1.49m;
                reductionFactor.CommonY = Convert.ToDecimal(reductionFactor.ShortVoyageReductionFactor); //0.60m;

                return Ok(reductionFactor);

            }
            catch (Exception ex)
            {
                return BadRequest("Error occured :" + ex.Message);
            }
        }
    }
}
