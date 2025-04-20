using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SeaRouteModel.Models
{
    public class PortSelectionModel
    {
        public int SequenceNumber { get; set; }
        public PortModel? Port { get; set; }
        public string SearchTerm { get; set; } = string.Empty;
        public List<PortModel> SearchResults { get; set; } = new List<PortModel>();
    }
}
