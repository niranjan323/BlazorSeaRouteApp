using System.ComponentModel.DataAnnotations;

namespace SeaRouteBlazor.Models;

public class ReductionFactor
{
    [Required]
    public string PortOfDeparture { get; set; } 
    [Required]
    public string? PortOfArrival { get; set; } 
    public string? TimeZone { get; set; } 
    [Required]
    public DateOnly DateOfArrival { get; set; } 
    [Required]
    public DateOnly DateOfDeparture { get; set; } 
    public int? Duration { get; set; }

    public string? VesselName { get; set; }
    public string? IMONo { get; set; }
    public string? Master { get; set; }
    [Required]
    public int Breadth { get; set; } 
    [Required]
    public TimeOnly ETD { get; set; } 
    [Required]
    public TimeOnly ETA { get; set; }
    [Required]
    public DateOnly WeatherForecastDate { get; set; } 
    [Required]
    public TimeOnly WeatherForecasetTime { get; set; } 

    public string? WeatherForecastSource { get; set; } 

    public int? WeatherForecastBeforeETD { get; set; }
    [Required]
    public decimal WaveHeightHswell { get; set; } 

    [Required]
    public decimal WaveHeightHwind { get; set; } 
    public string? WaveHsmax { get; set; }
    public decimal? ShortVoyageReductionFactor { get; set; }

    public string? DurationOk { get; set; }
    public string? WeatherForecastBeforeETDOK { get; set; }

    public List<decimal>? XValues { get; set; } = new List<decimal>();
    public List<decimal>? YValues { get; set; } = new List<decimal>();

    public double? XIncrement { get; set; } 

    public double? YIncrement { get; set; }



    public List<double>? Xlist { get; set; } = new List<double>();
    public List<double> Ylist { get; set; } = new List<double>();
    public decimal? CommonX { get; set; }
    public decimal? CommonY { get; set; }
}