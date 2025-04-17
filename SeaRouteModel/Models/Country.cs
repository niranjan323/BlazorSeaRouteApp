using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SeaRouteModel.Models
{
    public partial class Country
    {
        public int CountryId { get; set; }

        public string CountryCode { get; set; } = null!;

        public string CountryName { get; set; } = null!;

        public virtual ICollection<CPorts> Ports { get; set; } = new List<CPorts>();
    }
}
