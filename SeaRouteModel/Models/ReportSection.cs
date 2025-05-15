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
        public string Type { get; set; } = "text"; // "text" or "table"
        public string Content { get; set; } = ""; // Used for text sections
        public List<ReportTableRow> TableData { get; set; } = new List<ReportTableRow>(); // Used for table sections
    }
}
