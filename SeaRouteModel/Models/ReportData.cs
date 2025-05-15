using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SeaRouteModel.Models
{
    public class ReportData
    {
        public string ReportName { get; set; }
        public string Title { get; set; }
        public string AttentionText { get; set; }
        public List<ReportSection> Sections { get; set; } = new List<ReportSection>();
        public List<string> Notes { get; set; } = new List<string>();
    }
}
