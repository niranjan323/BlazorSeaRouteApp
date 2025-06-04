using System;
using System.Collections.Generic;

namespace NextGenEngApps.DigitalRules.CRoute.API.Models
{
    public class RecordDto
    {
        public string UserId { get; set; }
        public string RecordId { get; set; }
        public string RecordName { get; set; }
        public string DeparturePort { get; set; }
        public string ArrivalPort { get; set; }
        public string LoadingPorts { get; set; }
        public string VoyageDate { get; set; }
        public double ReductionFactor { get; set; }
        public double RouteDistance { get; set; }
        public string SeasonType { get; set; }
        public string VesselIMO { get; set; }
        public string VesselName { get; set; }
        public string CalcType { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class RecordDetailsDto
    {
        public string RouteName { get; set; }
        public string RecordId { get; set; }
        public PortModel DeparturePort { get; set; }
        public PortModel ArrivalPort { get; set; }
        public double ReductionFactor { get; set; }
        public List<RoutePointInfo> RoutePoints { get; set; }
    }

    public class RoutePointInfo
    {
        public string RecordId { get; set; }
        public int WaypointId { get; set; }
        public string GeoPointId { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double SegDistance { get; set; }
        public string RoutePointType { get; set; }
    }

    public class AddRecordDto
    {
        public string UserId { get; set; }
        public string RecordId { get; set; }
        public string RouteName { get; set; }
        public string DeparturePortId { get; set; }
        public string ArrivalPortId { get; set; }
        public double RouteDistance { get; set; }
        public double ReductionFactor { get; set; }
        public string SeasonType { get; set; }
        public List<VoyageLegDto> VoyageLegs { get; set; } = new List<VoyageLegDto>();
        public List<WaypointDto> Waypoints { get; set; } = new List<WaypointDto>();
    }

    public class VoyageLegDto
    {
        public string VoyageLegId { get; set; }
        public string RouteVersionId { get; set; }
        public int VoyageLegOrder { get; set; }
        public string UserId { get; set; }
        public string RecordId { get; set; }
        public string WaypointVersion { get; set; } // Legacy field for compatibility
        public string ArrivalPort { get; set; }
        public string DeparturePort { get; set; }
        public double ReductionFactor { get; set; }
        public double Distance { get; set; }
    }

    public class WaypointDto
    {
        public string UserId { get; set; }
        public string RecordId { get; set; }
        public string WaypointsVersion { get; set; } // Legacy field for compatibility
        public string GeoPointId { get; set; }
        public double SegDistance { get; set; }
    }

    public class PortModel
    {
        public string Port_Id { get; set; }
        public string Name { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }

    public class RouteLegInfo
    {
        public string RecordId { get; set; }
        public int RecordLegId { get; set; }
        public string RecordLegName { get; set; }
        public string ArrivalPort { get; set; }
        public string ArrivalPortId { get; set; }
        public string DeparturePort { get; set; }
        public string DeparturePortId { get; set; }
        public double Distance { get; set; }
        public double ReductionFactor { get; set; }
    }

    // Additional DTOs that might be needed
    public class RouteVersionDto
    {
        public string RouteVersionId { get; set; }
        public string RecordId { get; set; }
        public int RecordRouteVersion { get; set; }
        public DateTime CreatedDate { get; set; }
        public bool IsActive { get; set; }
    }

    public class RecordReductionFactorDto
    {
        public string RecordId { get; set; }
        public byte SeasonType { get; set; }
        public float ReductionFactor { get; set; }
    }

    public class VoyageLegReductionFactorDto
    {
        public string VoyageLegId { get; set; }
        public byte SeasonType { get; set; }
        public float ReductionFactor { get; set; }
    }

    public class ShortVoyageRecordDto
    {
        public string RecordId { get; set; }
        public DateTime DepartureTime { get; set; }
        public DateTime ArrivalTime { get; set; }
        public DateTime? ForecastTime { get; set; }
        public float? ForecastHswell { get; set; }
        public float? ForecastHwind { get; set; }
    }
}