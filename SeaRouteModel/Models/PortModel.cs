using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SeaRouteModel.Models
{
    public class PortModel
    {
        public int Legacy_Place_Id { get; set; }
        public string Port_Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Country_Id { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string Country_Code { get; set; } = string.Empty;
        public string Admiralty_Chart { get; set; } = string.Empty;
        public string Unlocode { get; set; } = string.Empty;
        public string Principal_Facilities { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string Port_Authority { get; set; } = string.Empty;
        public DateTime Last_Updated { get; set; }
    }
}
