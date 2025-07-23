using System;
using System.Collections.Generic;

namespace SeaRouteModel.Models
{
    /// <summary>
    /// Utility methods for coordinate transformations and longitude normalization.
    /// </summary>
    public static class CoordinateUtils
    {
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