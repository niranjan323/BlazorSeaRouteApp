using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SeaRouteModel.Models
{
    public class RouteItemModel
    {
        public int SequenceNumber { get; set; }
        public string ItemType { get; set; } // "Port" or "Waypoint"
        public PortSelectionModel? Port { get; set; }
        public WaypointModel? Waypoint { get; set; }
    }
}
