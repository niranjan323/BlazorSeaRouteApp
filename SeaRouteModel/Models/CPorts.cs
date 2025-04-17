using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SeaRouteModel.Models
{
    [Table("ports")]
    public partial class CPorts : BaseClass
    {
        public Guid PointId { get; set; } = new Guid()!;

        public string? Unlocode { get; set; }

        public string? PortName { get; set; }

        public string? CountryCode { get; set; }

        public string? PortAuthority { get; set; }

        public virtual Country? Country { get; set; }
 
    }
}
