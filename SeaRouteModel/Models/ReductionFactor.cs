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

        // --- Utility for Route Splitting and Longitude Normalization ---
        public class RoutePointInput
        {
            public string Type { get; set; } // "port" or "waypoint"
            public string Name { get; set; }
            public double[] LatLng { get; set; } // [lat, lng]
            public double SegmentDistance { get; set; } // distance to next point
            public List<double[]> SegmentCoordinates { get; set; } // list of [lng, lat] pairs to next point
        }

        public class VoyageLeg
        {
            public string DeparturePort { get; set; }
            public string ArrivalPort { get; set; }
            public double Distance { get; set; }
            public List<double[]> Coordinates { get; set; } = new List<double[]>();
        }

        /// <summary>
        /// Splits a route into voyage legs at each port, combining waypoints as per the algorithm.
        /// </summary>
        public static List<VoyageLeg> SplitRouteIntoVoyageLegs(List<RoutePointInput> routePoints)
        {
            var voyageLegs = new List<VoyageLeg>();
            VoyageLeg currentLeg = null;
            for (int i = 0; i < routePoints.Count; i++)
            {
                var point = routePoints[i];
                if (point.Type.ToLower() == "port")
                {
                    if (currentLeg != null)
                    {
                        // Set arrival port for previous leg
                        currentLeg.ArrivalPort = point.Name;
                    }
                    // Start new leg
                    currentLeg = new VoyageLeg
                    {
                        DeparturePort = point.Name,
                        Distance = point.SegmentDistance,
                        Coordinates = point.SegmentCoordinates != null ? new List<double[]>(point.SegmentCoordinates) : new List<double[]>()
                    };
                    voyageLegs.Add(currentLeg);
                }
                else if (point.Type.ToLower() == "waypoint")
                {
                    if (currentLeg != null)
                    {
                        currentLeg.Distance += point.SegmentDistance;
                        if (point.SegmentCoordinates != null)
                            currentLeg.Coordinates.AddRange(point.SegmentCoordinates);
                    }
                }
                // Last point: set arrival port for last leg
                if (i == routePoints.Count - 1 && currentLeg != null)
                {
                    currentLeg.ArrivalPort = point.Name;
                }
            }
            // Post-process: normalize longitudes and remove duplicates
            foreach (var leg in voyageLegs)
            {
                leg.Coordinates = NormalizeLongitudesAndRemoveDuplicates(leg.Coordinates);
            }
            return voyageLegs;
        }

        /// <summary>
        /// Converts all longitude values in a list of [lng, lat] coordinates to [-180, 180) and removes duplicates.
        /// </summary>
        public static List<double[]> NormalizeLongitudesAndRemoveDuplicates(List<double[]> coordinates)
        {
            var seen = new HashSet<string>();
            var result = new List<double[]>();
            foreach (var coord in coordinates)
            {
                double lng = coord[0];
                double lat = coord[1];
                double lngNorm = NormalizeLongitude(lng);
                string key = $"{lngNorm:F8},{lat:F8}";
                if (!seen.Contains(key))
                {
                    result.Add(new double[] { lngNorm, lat });
                    seen.Add(key);
                }
            }
            return result;
        }

        /// <summary>
        /// Converts a longitude value to the range [-180, 180).
        /// </summary>
        public static double NormalizeLongitude(double longitude)
        {
            double T = 360.0;
            double t0 = -180.0;
            double k = Math.Floor((longitude - t0) / T);
            double alpha0 = longitude - k * T;
            // Handle edge case where alpha0 == 180
            if (alpha0 >= 180.0) alpha0 -= T;
            return alpha0;
        }

        /// <summary>
        /// Shifts all longitudes in a segment to ensure continuity with a reference longitude.
        /// </summary>
        public static List<Coordinate> TransformSegmentCoordinates(List<Coordinate> rawCoordinates, double referenceLongitude)
        {
            if (rawCoordinates == null || rawCoordinates.Count == 0)
                return new List<Coordinate>();

            double longitudeShift = CalculateLongitudeShift(referenceLongitude, rawCoordinates[0].Longitude);

            var transformedCoordinates = new List<Coordinate>();
            foreach (var coord in rawCoordinates)
            {
                transformedCoordinates.Add(new Coordinate
                {
                    Latitude = coord.Latitude,
                    Longitude = coord.Longitude + longitudeShift
                });
            }

            return transformedCoordinates;
        }

        /// <summary>
        /// Calculates the longitude shift needed to ensure continuity between segments.
        /// </summary>
        public static double CalculateLongitudeShift(double referenceLongitude, double firstSegmentLongitude)
        {
            double period = 360.0;
            double tolerance = 1.0;

            double p1 = referenceLongitude;
            double p2 = firstSegmentLongitude;

            double k = 0;
            if (Math.Abs(p2 - p1) > period - tolerance)
            {
                k = Math.Floor((p1 - p2) / period);
            }

            return k * period;
        }
    }
}
