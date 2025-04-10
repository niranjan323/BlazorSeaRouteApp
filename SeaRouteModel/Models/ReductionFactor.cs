using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SeaRouteModel.Models
{
    public class ReductionFactor
    {
        [Required]
        public string PortOfDeparture { get; set; } = "Marseille";
        [Required]
        public string? PortOfArrival { get; set; } = "Shanghai";
        public string? TimeZone { get; set; } = "UTC";
        [Required]
        public DateOnly DateOfArrival { get; set; } = new DateOnly(2024, 8, 2);
        [Required]
        public DateOnly DateOfDeparture { get; set; } = new DateOnly(2024, 7, 30);
        public int? Duration { get; set; }

        public string? VesselName { get; set; }
        public string? IMONo { get; set; }
        public string? Master { get; set; }
        [Required]
        public int Breadth { get; set; }
        [Required]
        public TimeOnly ETD { get; set; } = new TimeOnly(02, 00);
        [Required]
        public TimeOnly ETA { get; set; } /*= new TimeOnly(02, 00);*/
        [Required]
        public DateOnly WeatherForecastDate { get; set; } = new DateOnly(2024, 7, 29);
        [Required]
        public TimeOnly WeatherForecasetTime { get; set; } /*= new TimeOnly(22, 00);*/

        public string? WeatherForecastSource { get; set; } = "www.weather.gov";

        public int? WeatherForecastBeforeETD { get; set; }
        [Required]
        public decimal WaveHeightHswell { get; set; } = 1;

        [Required]
        public decimal WaveHeightHwind { get; set; } = 1;
        public string? WaveHsmax { get; set; }
        public decimal? ShortVoyageReductionFactor { get; set; }

        public string? DurationOk { get; set; }
        public string? WeatherForecastBeforeETDOK { get; set; }

        public List<decimal>? XValues { get; set; } = new List<decimal>();
        public List<decimal>? YValues { get; set; } = new List<decimal>();

        public double? XIncrement { get; set; } = 2;

        public double? YIncrement { get; set; } = 0.2;



        public List<double>? Xlist { get; set; } = new List<double>();
        public List<double> Ylist { get; set; } = new List<double>();
        public decimal? CommonX { get; set; }
        public decimal? CommonY { get; set; }
    }
}
