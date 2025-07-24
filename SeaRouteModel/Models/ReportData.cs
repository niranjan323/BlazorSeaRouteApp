using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SeaRouteModel.Models
{
    /// <summary>
    /// Unified report data structure for flexible report generation (PDF, HTML, etc.)
    /// </summary>
    public class UnifiedReportData
    {
        /// <summary>
        /// Main report title (H1)
        /// </summary>
        public string Title { get; set; }
        /// <summary>
        /// Attention block (e.g., recipient info)
        /// </summary>
        public string Attention { get; set; }
        /// <summary>
        /// Main body text (with dynamic placeholders)
        /// </summary>
        public string Body { get; set; }
        /// <summary>
        /// List of report sections (each can be H2, table, text, or image)
        /// </summary>
        public List<UnifiedReportSection> Sections { get; set; } = new();
        /// <summary>
        /// Static notes (shown at the end)
        /// </summary>
        public List<string> Notes { get; set; } = new();
        /// <summary>
        /// Map or other images (as byte[])
        /// </summary>
        public byte[] MapImage { get; set; }
    }

    /// <summary>
    /// Section of a report (H2, table, text, or image)
    /// </summary>
    public class UnifiedReportSection
    {
        /// <summary>
        /// Section heading (H2)
        /// </summary>
        public string Heading { get; set; }
        /// <summary>
        /// Section type: "text", "table", "image"
        /// </summary>
        public string Type { get; set; } = "text";
        /// <summary>
        /// Section content (for text)
        /// </summary>
        public string Content { get; set; } = "";
        /// <summary>
        /// Table data (for tables: key-value pairs)
        /// </summary>
        public List<KeyValuePair<string, string>> Table { get; set; } = new();
        /// <summary>
        /// Image data (for images)
        /// </summary>
        public byte[] ImageData { get; set; }
    }
}
