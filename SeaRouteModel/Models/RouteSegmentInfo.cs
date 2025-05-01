using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SeaRouteModel.Models
{
    public class RouteSegmentInfo
    {
        public int SegmentIndex { get; set; }
        public string StartPointName { get; set; }
        public string EndPointName { get; set; }
        public double Distance { get; set; }
        public double DurationHours { get; set; }
        public string Units { get; set; } = "km";
        public double[] StartCoordinates { get; set; } // [longitude, latitude]
        public double[] EndCoordinates { get; set; } // [longitude, latitude]
    }
}
