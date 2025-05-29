using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SeaRouteModel.Models
{
    public class ReportSection
    {
        public string Title { get; set; }
        public string Type { get; set; } = "text"; // "text", "table", "complex-table", "image", "chart"
        public string Content { get; set; } = "";
        public List<ReportTableRow> TableData { get; set; } = new List<ReportTableRow>();
        public ComplexTableData ComplexTable { get; set; } // For the route splitting table
        public byte[] ImageData { get; set; } // For images/charts
        public string ImageType { get; set; } // "map", "chart"
    }
}
