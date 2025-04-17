using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SeaRouteModel.Models
{
    public class RouteRequest
    {
        public double[] Origin { get; set; } = new double[2];
        public double[] Destination { get; set; } = new double[2];
        public string[] Restrictions { get; set; } = new string[1];
        public bool IncludePorts { get; set; }
        public string Units { get; set; }
        public bool OnlyTerminals { get; set; }
    }
}
