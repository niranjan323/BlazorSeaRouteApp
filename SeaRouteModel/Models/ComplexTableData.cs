using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SeaRouteModel.Models
{
    public class ComplexTableData
    {
        public List<ComplexTableHeader> Headers { get; set; } = new List<ComplexTableHeader>();
        public List<ComplexTableRow> Rows { get; set; } = new List<ComplexTableRow>();
    }
}
