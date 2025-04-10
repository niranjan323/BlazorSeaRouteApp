using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SeaRouteModel.Models
{
    public class VesselInfo
    {
        public string VesselName { get; set; }
        public string IMONumber { get; set; }
        public string Flag { get; set; }
        public DateTime? ReportDate { get; set; }
    }
}
