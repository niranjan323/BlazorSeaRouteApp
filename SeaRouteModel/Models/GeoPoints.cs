using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SeaRouteModel.Models
{
    public partial class GeoPoints : BaseClass
    {
        public Guid PointId { get; set; } = new Guid();

        public double Latitude { get; set; }

        public double Longitude { get; set; }

        //public virtual ICollection<Waypoint> Waypoints { get; set; } = new List<Waypoint>();
    }
}
