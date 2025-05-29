using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SeaRouteModel.Models
{
    public class ComplexTableRow
    {
        public List<ComplexTableCell> Cells { get; set; } = new List<ComplexTableCell>();
        public bool IsHeaderRow { get; set; }
    }
}
