using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SeaRouteModel.Models
{

    public class RouteModel
    {
        public string RouteName { get; set; } = string.Empty;
        public string SeasonalType { get; set; } = string.Empty;
        public string WayType { get; set; } = string.Empty;
        public double? ExceedanceProbability { get; set; }
        public string DepartureLocation { get; set; } = string.Empty;
        public string ArrivalLocation { get; set; } = string.Empty;
        public List<RouteSegmentInfo> RouteSegments { get; set; } = new List<RouteSegmentInfo>();
        public double TotalDistance { get; set; }
        public double TotalDurationHours { get; set; }
        public List<RouteItemModel> DepartureItems { get; set; } = new List<RouteItemModel>();
        public List<RouteItemModel> ArrivalItems { get; set; } = new List<RouteItemModel>();

        // For main departure and arrival port selections
        public PortSelectionModel MainDeparturePortSelection { get; set; } = new PortSelectionModel();
        public PortSelectionModel MainArrivalPortSelection { get; set; } = new PortSelectionModel();

        public List<PortSelectionModel> DeparturePorts { get; set; } = new List<PortSelectionModel>();
        public List<PortSelectionModel> ArrivalPorts { get; set; } = new List<PortSelectionModel>();
        public List<WaypointModel> DepartureWaypoints { get; set; } = new List<WaypointModel>();
        public List<WaypointModel> ArrivalWaypoints { get; set; } = new List<WaypointModel>();
    }
}
