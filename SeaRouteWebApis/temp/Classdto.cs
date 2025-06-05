// Updated WaypointDto
namespace NextGenEngApps.DigitalRules.CRoute.API.Dtos
{
    public class WaypointDto
    {
        public string? RoutePointId { get; set; } // Maps to RoutePointId
        public string UserId { get; set; } = string.Empty;
        public string RouteVersionId { get; set; } = string.Empty; // Replaces RecordId/WaypointsVersion combination
        public int RoutePointOrder { get; set; } // New property for ordering
        public Guid GeoPointId { get; set; }
        public double SegDistance { get; set; }
    }

    // Updated VoyageLegDto
    public class VoyageLegDto
    {
        public string UserId { get; set; } = string.Empty;
        public string RouteVersionId { get; set; } = string.Empty; // Replaces RecordId/WaypointVersion combination
        public Guid? DeparturePort { get; set; }
        public Guid? ArrivalPort { get; set; }
        public double Distance { get; set; }
        public int VoyageLegOrder { get; set; } // New property for ordering

        // Note: ReductionFactor is now handled separately through VoyageLegReductionFactor entity
        // If you need it in the DTO, you'll need to handle it differently
    }

    // Updated AddRecordDto
    public class AddRecordDto
    {
        public string UserId { get; set; } = string.Empty;
        public string RecordId { get; set; } = string.Empty;
        public string RouteName { get; set; } = string.Empty;
        public double RouteDistance { get; set; }
        public double ReductionFactor { get; set; }
        public string SeasonType { get; set; } = string.Empty; // Should be byte, but keeping as string for parsing
        public List<VoyageLegDto> VoyageLegs { get; set; } = new List<VoyageLegDto>();
        public List<WaypointDto> Waypoints { get; set; } = new List<WaypointDto>();

        // Note: DeparturePortId and ArrivalPortId removed since they're handled through VoyageLegs
        // If you need them for the overall record, they should be derived from first/last VoyageLeg
    }
}

// Updated Models
namespace NextGenEngApps.DigitalRules.CRoute.API.Models
{
    public class RecordDto
    {
        public string UserId { get; set; } = string.Empty;
        public string RecordId { get; set; } = string.Empty;
        public string RecordName { get; set; } = string.Empty;
        public string DeparturePort { get; set; } = string.Empty;
        public string ArrivalPort { get; set; } = string.Empty;
        public string LoadingPorts { get; set; } = string.Empty;
        public string VoyageDate { get; set; } = string.Empty;
        public double ReductionFactor { get; set; }
        public double RouteDistance { get; set; }
        public string SeasonType { get; set; } = string.Empty;
        public string VesselIMO { get; set; } = string.Empty;
        public string VesselName { get; set; } = string.Empty;
        public string CalcType { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
    }

    public class RecordDetailsDto
    {
        public string RouteName { get; set; } = string.Empty;
        public string RecordId { get; set; } = string.Empty;
        public PortModel DeparturePort { get; set; } = new PortModel();
        public PortModel ArrivalPort { get; set; } = new PortModel();
        public double ReductionFactor { get; set; }
        public List<RoutePointInfo> RoutePoints { get; set; } = new List<RoutePointInfo>();
    }

    public class RoutePointInfo
    {
        public string RecordId { get; set; } = string.Empty;
        public int WaypointId { get; set; }
        public string GeoPointId { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double SegDistance { get; set; }
        public string RoutePointType { get; set; } = string.Empty;
    }

    public class RouteLegInfo
    {
        public string RecordId { get; set; } = string.Empty;
        public int VoyageLegId { get; set; }
        public string DeparturePort { get; set; } = string.Empty;
        public string DeparturePortId { get; set; } = string.Empty;
        public string ArrivalPort { get; set; } = string.Empty;
        public string ArrivalPortId { get; set; } = string.Empty;
        public double Distance { get; set; }
        public double ReductionFactor { get; set; }
        public int RecordLegId { get; set; }
        public string RecordLegName { get; set; } = string.Empty;
    }

    public class PortModel
    {
        public string Port_Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }

    public class VesselDto
    {
        public string VesselIMO { get; set; } = string.Empty;
        public string VesselName { get; set; } = string.Empty;
    }
}