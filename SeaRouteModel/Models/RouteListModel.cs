using System;
using System.Collections.Generic;

namespace SeaRouteModel.Models
{
    public class RouteListModel
    {
        public string RecordId { get; set; } = Guid.NewGuid().ToString();
        public string RecordName { get; set; } = string.Empty;
        public string DeparturePort { get; set; } = string.Empty;
        public string ArrivalPort { get; set; } = string.Empty;
        public DateTime VoyageDate { get; set; }
        public double ReductionFactor { get; set; }
        public double RouteDistance { get; set; }
        public string VesselIMO { get; set; } = string.Empty;
        public string VesselName { get; set; } = string.Empty;
        public bool IsSelected { get; set; }
        public bool Expanded { get; set; }
        public List<RouteListLegModel> Legs { get; set; } = new List<RouteListLegModel>();
    }

    public class RouteListLegModel
    {
        public string RecordLegName { get; set; } = string.Empty;
        public string DeparturePort { get; set; } = string.Empty;
        public string ArrivalPort { get; set; } = string.Empty;
        public double ReductionFactor { get; set; }
        public double Distance { get; set; }
    }
}