using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SeaRouteModel.Models
{
    public class ComplexTableHeader
    {
        public string Text { get; set; }
        public int ColumnSpan { get; set; } = 1;
        public int RowSpan { get; set; } = 1;
        public bool HasImage { get; set; }
        public string ImagePath { get; set; }
    }
}
