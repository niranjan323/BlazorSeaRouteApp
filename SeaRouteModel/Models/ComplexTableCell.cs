using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SeaRouteModel.Models
{
    public class ComplexTableCell
    {
        public string Text { get; set; }
        public int ColumnSpan { get; set; } = 1;
        public int RowSpan { get; set; } = 1;
        public bool IsBold { get; set; }
        public List<string> SubItems { get; set; } = new List<string>(); // For multi-line cells
    }
}
