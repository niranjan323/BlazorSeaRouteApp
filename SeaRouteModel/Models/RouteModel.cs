﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SeaRouteModel.Models
{
    public class RouteModel
    {
        public string RouteName { get; set; } = string.Empty;
        public string DepartureLocation { get; set; } = string.Empty;
        public string ArrivalLocation { get; set; } = string.Empty;
        public List<PortSelectionModel> DeparturePorts { get; set; } = new List<PortSelectionModel>();
        public List<PortSelectionModel> ArrivalPorts { get; set; } = new List<PortSelectionModel>();
        public List<WaypointModel> DepartureWaypoints { get; set; } = new List<WaypointModel>();
        public List<WaypointModel> ArrivalWaypoints { get; set; } = new List<WaypointModel>();
    }
}
