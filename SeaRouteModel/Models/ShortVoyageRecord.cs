using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SeaRouteModel.Models
{
    public partial class ShortVoyageRecord : BaseClass
    {
        public string UserId { get; set; } = null!;

        public string RecordId { get; set; } = null!;

        public DateTime DepartureTime { get; set; }

        public DateTime ArrivalTime { get; set; }

        public DateTime? ForecastTime { get; set; }

        public double? ForecastHswell { get; set; }

        public double? ForecastHwind { get; set; }

        public double? ReductionFactor { get; set; }


       // public Record Record { get; set; } = null!;
    }
}
