using Microsoft.JSInterop;
using SeaRouteModel.Models;
using System.Text;
using PuppeteerSharp;

namespace SeaRouteBlazorServerApp.Components.Services;

public class PdfService : IPdfService
{
    private readonly IJSRuntime _jsRuntime;
    private readonly ILogger<PdfService> _logger;

    public PdfService(IJSRuntime jsRuntime, ILogger<PdfService> logger)
    {
        _jsRuntime = jsRuntime;
        _logger = logger;
    }

    public async Task<byte[]> GenerateReportPdfAsync(ReportData reportData)
    {
        try
        {
            _logger.LogInformation("Generating enhanced PDF report for: {ReportName}", reportData.ReportName);

            var document = new PdfDocument();
            document.Info.Title = reportData.Title;
            document.Info.Author = "ABS Online Reduction Factor Tool";
            document.Info.Subject = reportData.ReportName;
            document.Info.Keywords = "Reduction Factor, ABS, Vessel";

            var page = document.AddPage();
            var gfx = XGraphics.FromPdfPage(page);

            // Define fonts
            var titleFont = new XFont("Arial", 18, XFontStyle.Bold);
            var headerFont = new XFont("Arial", 14, XFontStyle.Bold);
            var boldFont = new XFont("Arial", 12, XFontStyle.Bold);
            var font = new XFont("Arial", 12, XFontStyle.Regular);
            var smallFont = new XFont("Arial", 10, XFontStyle.Regular);
            var italicFont = new XFont("Arial", 8, XFontStyle.Italic);

            // Define layout constants
            double margin = 50;
            double contentWidth = page.Width - (margin * 2);
            double lineHeight = 20;
            double y = margin;

            // Draw title
            gfx.DrawString(reportData.Title, titleFont, XBrushes.Black,
                new XRect(margin, y, contentWidth, titleFont.Height), XStringFormats.TopLeft);
            y += titleFont.Height * 2;

            // Draw generation date
            gfx.DrawString($"Generated on: {DateTime.Now:yyyy-MM-dd HH:mm}", font, XBrushes.Black, margin, y);
            y += lineHeight * 1.5;

            // Draw attention section
            if (!string.IsNullOrEmpty(reportData.AttentionText))
            {
                gfx.DrawString("Attention:", boldFont, XBrushes.Black, margin, y);
                y += lineHeight;
                gfx.DrawString(reportData.AttentionText, font, XBrushes.Black, margin + 10, y);
                y += lineHeight;
            }

            // Draw description
            if (!string.IsNullOrEmpty(reportData.Description))
            {
                var descLines = WrapText(reportData.Description, font, contentWidth, gfx);
                foreach (var line in descLines)
                {
                    gfx.DrawString(line, font, XBrushes.Black, margin, y);
                    y += lineHeight;
                }
                y += lineHeight * 0.5;
            }

            // Draw contact info
            if (!string.IsNullOrEmpty(reportData.ContactInfo))
            {
                var contactLines = WrapText(reportData.ContactInfo, font, contentWidth, gfx);
                foreach (var line in contactLines)
                {
                    gfx.DrawString(line, font, XBrushes.Black, margin, y);
                    y += lineHeight;
                }
                y += lineHeight;
            }

            // Process sections
            foreach (var section in reportData.Sections)
            {
                // Check if we need a new page before drawing section
                double estimatedHeight = headerFont.Height * 2;
                if (section.Type.ToLower() == "image" || section.Type.ToLower() == "chart")
                    estimatedHeight += 300;
                else if (section.Type.ToLower() == "table" || section.Type.ToLower() == "complex-table")
                    estimatedHeight += 150;
                else
                    estimatedHeight += 100;

                if (y + estimatedHeight > page.Height - margin - 100)
                {
                    page = document.AddPage();
                    gfx = XGraphics.FromPdfPage(page);
                    y = margin;
                }

                y = await DrawSection(gfx, section, margin, y, contentWidth, font, boldFont, headerFont, page, document);
            }

            // Add notes section
            if (reportData.Notes?.Count > 0)
            {
                if (y + 100 > page.Height - margin)
                {
                    page = document.AddPage();
                    gfx = XGraphics.FromPdfPage(page);
                    y = margin;
                }

                gfx.DrawString("Notes:", headerFont, XBrushes.Black, margin, y);
                y += lineHeight * 1.5;

                foreach (var note in reportData.Notes)
                {
                    var noteLines = WrapText("• " + note, font, contentWidth, gfx);
                    foreach (var line in noteLines)
                    {
                        if (y + lineHeight > page.Height - margin - 50)
                        {
                            page = document.AddPage();
                            gfx = XGraphics.FromPdfPage(page);
                            y = margin;
                        }
                        gfx.DrawString(line, font, XBrushes.Black, margin, y);
                        y += lineHeight;
                    }
                }
                y += lineHeight;
            }

            // Add disclaimer at bottom
            var disclaimer = "This report is generated by the ABS Online Reduction Factor Tool and is subject to the terms and conditions provided by ABS.";
            double disclaimerY = page.Height - margin - lineHeight;
            gfx.DrawString(disclaimer, italicFont, XBrushes.Black,
                new XRect(margin, disclaimerY, contentWidth, lineHeight), XStringFormats.TopLeft);

            // Save document
            using (var stream = new MemoryStream())
            {
                document.Save(stream, false);
                return stream.ToArray();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating enhanced PDF report");
            throw;
        }
    }

    public async Task DownloadPdfAsync(ReportData reportData, string fileName)
    {
        try
        {
            var pdfBytes = await GenerateReportPdfAsync(reportData);
            await _jsRuntime.InvokeVoidAsync("downloadFileFromBase64", fileName, Convert.ToBase64String(pdfBytes));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading PDF report");
            throw;
        }
    }

    // New: Generate PDF from UnifiedReportData
    public async Task<byte[]> GenerateReportPdfAsync(UnifiedReportData reportData)
    {
        try
        {
            _logger.LogInformation("Generating unified PDF report for: {Title}", reportData.Title);
            var document = new PdfDocument();
            document.Info.Title = reportData.Title;
            document.Info.Author = "ABS Online Reduction Factor Tool";
            var page = document.AddPage();
            var gfx = XGraphics.FromPdfPage(page);
            var titleFont = new XFont("Arial", 18, XFontStyle.Bold);
            var h2Font = new XFont("Arial", 14, XFontStyle.Bold);
            var font = new XFont("Arial", 12, XFontStyle.Regular);
            var boldFont = new XFont("Arial", 12, XFontStyle.Bold);
            var italicFont = new XFont("Arial", 10, XFontStyle.Italic);
            double margin = 50;
            double contentWidth = page.Width - (margin * 2);
            double y = margin;
            // Title (H1)
            gfx.DrawString(reportData.Title, titleFont, XBrushes.Black, new XRect(margin, y, contentWidth, titleFont.Height), XStringFormats.TopLeft);
            y += titleFont.Height * 2;
            // Attention
            if (!string.IsNullOrEmpty(reportData.Attention))
            {
                gfx.DrawString("Attention:", boldFont, XBrushes.Black, margin, y);
                y += font.Height;
                gfx.DrawString(reportData.Attention, font, XBrushes.Black, margin + 10, y);
                y += font.Height * 1.5;
            }
            // Body
            if (!string.IsNullOrEmpty(reportData.Body))
            {
                var bodyLines = reportData.Body.Split('\n');
                foreach (var line in bodyLines)
                {
                    gfx.DrawString(line, font, XBrushes.Black, margin, y);
                    y += font.Height * 1.2;
                }
                y += font.Height * 0.5;
            }
            // Sections
            foreach (var section in reportData.Sections)
            {
                // H2
                gfx.DrawString(section.Heading, h2Font, XBrushes.Black, margin, y);
                y += h2Font.Height * 1.5;
                if (section.Type == "text" && !string.IsNullOrEmpty(section.Content))
                {
                    var lines = section.Content.Split('\n');
                    foreach (var line in lines)
                    {
                        gfx.DrawString(line, font, XBrushes.Black, margin, y);
                        y += font.Height * 1.2;
                    }
                    y += font.Height * 0.5;
                }
                else if (section.Type == "table" && section.Table != null && section.Table.Count > 0)
                {
                    // Render as key-value table
                    double tableX = margin;
                    double tableY = y;
                    double keyWidth = contentWidth * 0.45;
                    double valueWidth = contentWidth * 0.5;
                    double rowHeight = font.Height * 1.5;
                    // Table border
                    gfx.DrawRectangle(XPens.Black, tableX, tableY, keyWidth + valueWidth, rowHeight * section.Table.Count);
                    for (int i = 0; i < section.Table.Count; i++)
                    {
                        var kv = section.Table[i];
                        // Key cell
                        gfx.DrawRectangle(XPens.Black, tableX, tableY + i * rowHeight, keyWidth, rowHeight);
                        gfx.DrawString(kv.Key, boldFont, XBrushes.Black, new XRect(tableX + 5, tableY + i * rowHeight + 2, keyWidth - 10, rowHeight), XStringFormats.TopLeft);
                        // Value cell
                        gfx.DrawRectangle(XPens.Black, tableX + keyWidth, tableY + i * rowHeight, valueWidth, rowHeight);
                        gfx.DrawString(kv.Value, font, XBrushes.Black, new XRect(tableX + keyWidth + 5, tableY + i * rowHeight + 2, valueWidth - 10, rowHeight), XStringFormats.TopLeft);
                    }
                    y = tableY + section.Table.Count * rowHeight + 10;
                }
                else if (section.Type == "image" && section.ImageData != null && section.ImageData.Length > 0)
                {
                    using (var imageStream = new MemoryStream(section.ImageData))
                    {
                        var image = XImage.FromStream(() => imageStream);
                        double maxWidth = contentWidth * 0.8;
                        double maxHeight = 250;
                        double scale = Math.Min(maxWidth / image.PixelWidth, maxHeight / image.PixelHeight);
                        double imgW = image.PixelWidth * scale;
                        double imgH = image.PixelHeight * scale;
                        gfx.DrawImage(image, margin + (contentWidth - imgW) / 2, y, imgW, imgH);
                        y += imgH + 10;
                    }
                }
            }
            // Notes
            if (reportData.Notes != null && reportData.Notes.Count > 0)
            {
                gfx.DrawString("Notes:", h2Font, XBrushes.Black, margin, y);
                y += h2Font.Height * 1.2;
                foreach (var note in reportData.Notes)
                {
                    gfx.DrawString("• " + note, font, XBrushes.Black, margin + 10, y);
                    y += font.Height * 1.2;
                }
            }
            // Save document
            using (var stream = new MemoryStream())
            {
                document.Save(stream, false);
                return stream.ToArray();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating unified PDF report");
            throw;
        }
    }
    // New: Download PDF from UnifiedReportData
    public async Task DownloadPdfAsync(UnifiedReportData reportData, string fileName)
    {
        try
        {
            var pdfBytes = await GenerateReportPdfAsync(reportData);
            await _jsRuntime.InvokeVoidAsync("downloadFileFromBase64", fileName, Convert.ToBase64String(pdfBytes));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading unified PDF report");
            throw;
        }
    }

    // Add this new method to better handle section spacing and page breaks:
    private async Task<double> DrawSection(XGraphics gfx, ReportSection section, double x, double y, double width,
     XFont font, XFont boldFont, XFont headerFont, PdfPage currentPage, PdfDocument document)
    {
        // Estimate section height to determine if we need a new page
        double estimatedHeight = headerFont.Height * 2; // Title space

        if (section.Type.ToLower() == "image" || section.Type.ToLower() == "chart")
        {
            estimatedHeight += 300; // Estimated image space
        }
        else if (section.Type.ToLower() == "table" || section.Type.ToLower() == "complex-table")
        {
            estimatedHeight += 150; // Estimated table space
        }
        else
        {
            estimatedHeight += 100; // Text content
        }

        // Check if we need a new page for the entire section
        if (y + estimatedHeight > currentPage.Height - 100)
        {
            var newPage = document.AddPage();
            gfx = XGraphics.FromPdfPage(newPage);
            y = 50; // Reset with proper margin
        }

        // Draw section title with better spacing
        gfx.DrawString(section.Title, headerFont, XBrushes.Black, x, y);
        y += headerFont.Height * 1.8; // More space after title

        switch (section.Type.ToLower())
        {
            case "table":
                y = DrawTable(gfx, section.TableData, x, y, width, font, boldFont);
                break;

            case "complex-table":
                y = DrawComplexTable(gfx, section.ComplexTable, x, y, width, font, boldFont);
                break;

            case "image":
                y = await DrawImage(gfx, section, x, y, width, currentPage, document);
                break;

            case "chart":
                y = await DrawChart(gfx, section, x, y, width);
                break;

            case "text":
            default:
                y = DrawTextContent(gfx, section.Content, x, y, width, font);
                break;
        }

        return y + 25; // Consistent spacing after each section
    }

    private double DrawTable(XGraphics gfx, List<ReportTableRow> tableData, double x, double y,
    double width, XFont font, XFont headerFont)
    {
        if (tableData == null || tableData.Count == 0)
            return y;
        // Validate inputs
        if (width <= 0 || font == null || headerFont == null)
            return y;
        int colCount = tableData[0].Cells.Count;
        double availableWidth = Math.Max(width - 4, 100);
        double[] colWidths = CalculateColumnWidths(tableData, availableWidth, colCount); // Account for table borders
        double baseRowHeight = Math.Max(font.Height * 1.8, 24);
        double cellPadding = 6;

        // Calculate total table height first
        double tableHeight = 0;
        for (int i = 0; i < tableData.Count; i++)
        {
            var row = tableData[i];
            bool isHeader = i == 0;
            XFont currentFont = isHeader ? headerFont : font;

            double maxCellHeight = baseRowHeight;
            for (int j = 0; j < Math.Min(row.Cells.Count, colCount); j++)
            {
                var cellText = row.Cells[j] ?? "";
                var wrappedLines = WrapText(cellText, currentFont, colWidths[j] - (cellPadding * 2), gfx);
                double cellHeight = Math.Max(wrappedLines.Count * (currentFont.Height * 1.2) + (cellPadding * 2), baseRowHeight);
                maxCellHeight = Math.Max(maxCellHeight, cellHeight);
            }
            tableHeight += maxCellHeight;
        }

        // Draw outer table border
        gfx.DrawRectangle(XPens.Black, x, y, width, tableHeight);

        double currentY = y;

        for (int i = 0; i < tableData.Count; i++)
        {
            var row = tableData[i];
            bool isHeader = i == 0;
            XFont currentFont = isHeader ? headerFont : font;
            XBrush bgBrush = isHeader ? XBrushes.LightGray : XBrushes.White;

            // Calculate actual row height
            double maxCellHeight = baseRowHeight;
            for (int j = 0; j < Math.Min(row.Cells.Count, colCount); j++)
            {
                var cellText = row.Cells[j] ?? "";
                var wrappedLines = WrapText(cellText, currentFont, colWidths[j] - (cellPadding * 2), gfx);
                double cellHeight = Math.Max(wrappedLines.Count * (currentFont.Height * 1.2) + (cellPadding * 2), baseRowHeight);
                maxCellHeight = Math.Max(maxCellHeight, cellHeight);
            }

            // Draw cells for this row
            double xPos = x;
            for (int j = 0; j < Math.Min(row.Cells.Count, colCount); j++)
            {
                // Draw cell background and border
                gfx.DrawRectangle(XPens.Black, isHeader ? bgBrush : XBrushes.White, xPos, currentY, colWidths[j], maxCellHeight);

                // Draw cell text
                var cellText = row.Cells[j] ?? "";
                if (!string.IsNullOrWhiteSpace(cellText))
                {
                    var wrappedLines = WrapText(cellText, currentFont, colWidths[j] - (cellPadding * 2), gfx);

                    // Calculate vertical centering
                    double totalTextHeight = wrappedLines.Count * (currentFont.Height * 1.2);
                    double textStartY = currentY + (maxCellHeight - totalTextHeight) / 2 + cellPadding;

                    // Draw each line of text
                    for (int lineIndex = 0; lineIndex < wrappedLines.Count; lineIndex++)
                    {
                        var line = wrappedLines[lineIndex];
                        double textY = textStartY + (lineIndex * (currentFont.Height * 1.2));

                        // Determine text alignment
                        if (isHeader)
                        {
                            // Center all header text
                            var textRect = new XRect(xPos + cellPadding, textY, colWidths[j] - (cellPadding * 2), currentFont.Height);
                            gfx.DrawString(line, currentFont, XBrushes.Black, textRect, XStringFormats.TopCenter);
                        }
                        else
                        {
                            // For data: first column left-aligned, others center-aligned if numeric
                            bool isNumeric = (j > 0) && (double.TryParse(line.Trim(), out _) ||
                                           line.Contains("N/A") || line.Contains("nm") || line.Contains("0."));

                            if (isNumeric)
                            {
                                var textRect = new XRect(xPos + cellPadding, textY, colWidths[j] - (cellPadding * 2), currentFont.Height);
                                gfx.DrawString(line, currentFont, XBrushes.Black, textRect, XStringFormats.TopCenter);
                            }
                            else
                            {
                                gfx.DrawString(line, currentFont, XBrushes.Black, xPos + cellPadding, textY);
                            }
                        }
                    }
                }

                xPos += colWidths[j];
            }

            currentY += maxCellHeight;
        }

        return currentY + 15;
    }

    private double DrawComplexTable(XGraphics gfx, ComplexTableData complexTable, double x, double y,
    double width, XFont font, XFont boldFont)
    {
        if (complexTable == null || complexTable.Rows.Count == 0)
            return y;

        // Validate inputs
        if (width <= 0 || font == null || boldFont == null)
            return y;

        double cellPadding = 4; // Reduced padding to save space
        double minRowHeight = Math.Max(font.Height * 1.8, 24); // Slightly reduced row height

        // Better column width calculation for Route Analysis table
        double availableWidth = Math.Max(width - 2, 100); // Reduced border space
        double[] columnWidths;

        // Check if this is a Route Analysis table (has seasonal columns)
        bool hasSeasonalColumns = complexTable.Rows.Any(r =>
            r.Cells.Any(c => c.Text?.Contains("Spring") == true ||
                            c.Text?.Contains("Summer") == true ||
                            c.Text?.Contains("Fall") == true ||
                            c.Text?.Contains("Winter") == true));

        if (hasSeasonalColumns)
        {
            // Optimized Route Analysis table with 8 columns - better distribution
            columnWidths = new double[]
            {
            availableWidth * 0.12,  // Row Label (Entire Route/Route Splitting)
            availableWidth * 0.18,  // Routes (wider for port names)
            availableWidth * 0.10,  // Distance  
            availableWidth * 0.15,  // Annual Reduction Factor
            availableWidth * 0.11,  // Spring
            availableWidth * 0.11,  // Summer
            availableWidth * 0.11,  // Fall
            availableWidth * 0.12   // Winter
            };
        }
        else
        {
            // Default equal distribution for other tables
            int maxColumns = complexTable.Rows.Max(r => r.Cells.Count);
            columnWidths = new double[maxColumns];
            double equalWidth = availableWidth / maxColumns;
            for (int i = 0; i < maxColumns; i++)
            {
                columnWidths[i] = equalWidth;
            }
        }

        // Calculate table height with proper cell handling
        double tableHeight = 0;
        foreach (var row in complexTable.Rows)
        {
            double maxCellHeight = minRowHeight;
            int colIndex = 0;

            foreach (var cell in row.Cells)
            {
                // Skip cells that are part of a rowspan from previous rows
                if (colIndex >= columnWidths.Length) break;

                double cellWidth = 0;
                int effectiveColSpan = Math.Min(cell.ColumnSpan, columnWidths.Length - colIndex);

                for (int span = 0; span < effectiveColSpan; span++)
                {
                    cellWidth += columnWidths[colIndex + span];
                }
                cellWidth = Math.Max(cellWidth, 15); // Minimum cell width

                var cellText = cell.Text ?? "";
                if (cell.SubItems?.Count > 0)
                {
                    cellText += "\n" + string.Join("\n", cell.SubItems);
                }

                XFont currentFont = (row.IsHeaderRow || cell.IsBold) ? boldFont : font;
                var wrappedLines = WrapText(cellText, currentFont, cellWidth - (cellPadding * 2), gfx);
                double cellHeight = Math.Max(wrappedLines.Count * (currentFont.Height * 1.1) + (cellPadding * 2), minRowHeight);
                maxCellHeight = Math.Max(maxCellHeight, cellHeight);

                colIndex += effectiveColSpan;
            }
            tableHeight += maxCellHeight;
        }

        // Ensure table fits within available width
        double actualTableWidth = Math.Min(width, availableWidth);

        // Draw outer table border
        gfx.DrawRectangle(XPens.Black, x, y, actualTableWidth, tableHeight);

        double currentY = y;

        foreach (var row in complexTable.Rows)
        {
            double maxCellHeight = minRowHeight;
            int colIndex = 0;

            // Calculate row height first
            foreach (var cell in row.Cells)
            {
                if (colIndex >= columnWidths.Length) break;

                double cellWidth = 0;
                int effectiveColSpan = Math.Min(cell.ColumnSpan, columnWidths.Length - colIndex);

                for (int span = 0; span < effectiveColSpan; span++)
                {
                    cellWidth += columnWidths[colIndex + span];
                }

                var cellText = cell.Text ?? "";
                if (cell.SubItems?.Count > 0)
                {
                    cellText += "\n" + string.Join("\n", cell.SubItems);
                }

                XFont currentFont = (row.IsHeaderRow || cell.IsBold) ? boldFont : font;
                var wrappedLines = WrapText(cellText, currentFont, cellWidth - (cellPadding * 2), gfx);
                double cellHeight = Math.Max(wrappedLines.Count * (currentFont.Height * 1.1) + (cellPadding * 2), minRowHeight);
                maxCellHeight = Math.Max(maxCellHeight, cellHeight);

                colIndex += effectiveColSpan;
            }

            // Draw cells
            colIndex = 0;
            double xPos = x;

            foreach (var cell in row.Cells)
            {
                if (colIndex >= columnWidths.Length) break;

                double cellWidth = 0;
                int effectiveColSpan = Math.Min(cell.ColumnSpan, columnWidths.Length - colIndex);

                for (int span = 0; span < effectiveColSpan; span++)
                {
                    cellWidth += columnWidths[colIndex + span];
                }

                // Ensure cell doesn't exceed table boundary
                if (xPos + cellWidth > x + actualTableWidth)
                {
                    cellWidth = x + actualTableWidth - xPos;
                }

                XFont currentFont = (row.IsHeaderRow || cell.IsBold) ? boldFont : font;
                XBrush bgBrush = row.IsHeaderRow ? XBrushes.LightGray : XBrushes.White;

                // Draw cell background and border
                gfx.DrawRectangle(XPens.Black, bgBrush, xPos, currentY, cellWidth, maxCellHeight);

                // Prepare cell text
                var cellText = cell.Text ?? "";
                if (cell.SubItems?.Count > 0)
                {
                    cellText += "\n" + string.Join("\n", cell.SubItems);
                }

                if (!string.IsNullOrWhiteSpace(cellText))
                {
                    var wrappedLines = WrapText(cellText, currentFont, cellWidth - (cellPadding * 2), gfx);

                    // Calculate vertical centering
                    double totalTextHeight = wrappedLines.Count * (currentFont.Height * 1.1);
                    double textStartY = currentY + (maxCellHeight - totalTextHeight) / 2 + cellPadding;

                    // Draw text lines
                    for (int lineIndex = 0; lineIndex < wrappedLines.Count; lineIndex++)
                    {
                        var line = wrappedLines[lineIndex];
                        double textY = textStartY + (lineIndex * (currentFont.Height * 1.1));

                        if (row.IsHeaderRow)
                        {
                            // Center all header text
                            double rectWidth = Math.Max(cellWidth - (cellPadding * 2), 5);
                            double rectHeight = Math.Max(currentFont.Height, 5);
                            var textRect = new XRect(xPos + cellPadding, textY, rectWidth, rectHeight);
                            gfx.DrawString(line, currentFont, XBrushes.Black, textRect, XStringFormats.TopCenter);
                        }
                        else
                        {
                            // For data: center numeric values, left-align text
                            bool isNumeric = double.TryParse(line.Trim(), out _) ||
                                           line.Contains("nm") || line.Contains("0.") || line.Contains("N/A") ||
                                           line.Trim().All(c => char.IsDigit(c) || c == '.' || c == '-');

                            if (isNumeric)
                            {
                                double rectWidth = Math.Max(cellWidth - (cellPadding * 2), 5);
                                double rectHeight = Math.Max(currentFont.Height, 5);
                                var textRect = new XRect(xPos + cellPadding, textY, rectWidth, rectHeight);
                                gfx.DrawString(line, currentFont, XBrushes.Black, textRect, XStringFormats.TopCenter);
                            }
                            else
                            {
                                // Ensure text doesn't exceed cell boundary
                                double maxTextX = xPos + cellWidth - cellPadding;
                                if (xPos + cellPadding < maxTextX)
                                {
                                    gfx.DrawString(line, currentFont, XBrushes.Black, xPos + cellPadding, textY);
                                }
                            }
                        }
                    }
                }

                xPos += cellWidth;
                colIndex += effectiveColSpan;
            }

            currentY += maxCellHeight;
        }

        return currentY + 10; // Reduced bottom margin
    }

    private async Task<double> DrawImage(XGraphics gfx, ReportSection section, double x, double y,
 double width, PdfPage currentPage, PdfDocument document)
    {
        if (section.ImageData == null || section.ImageData.Length == 0)
            return y;

        try
        {
            using (var imageStream = new MemoryStream(section.ImageData))
            {
                var image = XImage.FromStream(() => imageStream);

                // Different sizing based on image type
                double maxWidth, maxHeight;

                // Check if this is a chart/graph or map based on section title or image type
                bool isChart = section.Title?.ToLower().Contains("chart") == true ||
                section.Title?.ToLower().Contains("graph") == true ||
                section.ImageType?.ToLower().Contains("chart") == true;

                bool isMap = section.Title?.ToLower().Contains("map") == true ||
                section.ImageType?.ToLower().Contains("map") == true;

                if (isChart)
                {
                    // Charts: Use more width, less height for better readability
                    maxWidth = width * 0.95;
                    maxHeight = 250;
                }
                else if (isMap)
                {
                    // Maps: Use wider aspect ratio, more space
                    maxWidth = width * 0.90;
                    maxHeight = 350;
                }
                else
                {
                    // Default images
                    maxWidth = width * 0.8;
                    maxHeight = 300;
                }

                double imageWidth = image.PixelWidth;
                double imageHeight = image.PixelHeight;

                // Scale image to fit
                double scaleX = maxWidth / imageWidth;
                double scaleY = maxHeight / imageHeight;
                double scale = Math.Min(scaleX, scaleY);

                double scaledWidth = imageWidth * scale;
                double scaledHeight = imageHeight * scale;

                // Check if image fits on current page with some buffer
                double requiredSpace = scaledHeight + 40; // Add buffer for spacing
                if (y + requiredSpace > currentPage.Height - 80)
                {
                    // Create new page for image
                    var newPage = document.AddPage();
                    var newGfx = XGraphics.FromPdfPage(newPage);
                    gfx = newGfx;
                    y = 50; // Reset Y position with margin
                }

                // Center the image horizontally
                double imageX = x + (width - scaledWidth) / 2;

                // Draw image
                gfx.DrawImage(image, imageX, y, scaledWidth, scaledHeight);

                // Add more spacing after image for better separation
                return y + scaledHeight + 30;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to draw image in PDF");
            // Draw placeholder text instead
            gfx.DrawString($"[Image: {section.ImageType ?? "Unknown"}]",
            new XFont("Arial", 10, XFontStyle.Italic), XBrushes.Gray, x, y);
            return y + 20;
        }
    }



    private async Task<double> DrawChart(XGraphics gfx, ReportSection section, double x, double y, double width)
    {
        // Similar to DrawImage but specifically for charts
        return await DrawImage(gfx, section, x, y, width, null, null);
    }

    private double DrawTextContent(XGraphics gfx, string content, double x, double y, double width, XFont font)
    {
        if (string.IsNullOrEmpty(content))
            return y;

        var lines = content.Split('\n');
        foreach (var line in lines)
        {
            var wrappedLines = WrapText(line, font, width, gfx);
            foreach (var wrappedLine in wrappedLines)
            {
                gfx.DrawString(wrappedLine, font, XBrushes.Black, x, y);
                y += font.Height * 1.2;
            }
        }

        return y + 10;
    }

    private double[] CalculateColumnWidths(List<ReportTableRow> tableData, double totalWidth, int columnCount)
    {
        var widths = new double[columnCount];

        // Ensure minimum total width
        totalWidth = Math.Max(totalWidth, columnCount * 20);

        if (tableData.Count == 0)
        {
            double baseWidth = totalWidth / columnCount;
            for (int i = 0; i < columnCount; i++)
            {
                widths[i] = Math.Max(baseWidth, 20);
            }
            return widths;
        }

        // Check for User Inputs table (3 columns with Parameter, Value, Code)
        if (columnCount == 3)
        {
            var headerRow = tableData.FirstOrDefault();
            if (headerRow?.Cells.Any(c => c?.Contains("Parameter") == true || c?.Contains("Param") == true) == true)
            {
                widths[0] = totalWidth * 0.45; // Parameter column - wider
                widths[1] = totalWidth * 0.35; // Value column
                widths[2] = totalWidth * 0.20; // Code column
                return widths;
            }
        }

        // For other tables, analyze content to determine optimal widths
        var maxLengths = new int[columnCount];
        var totalChars = new int[columnCount];

        // Analyze content in all rows
        foreach (var row in tableData)
        {
            for (int i = 0; i < Math.Min(row.Cells.Count, columnCount); i++)
            {
                var cellText = row.Cells[i] ?? "";
                maxLengths[i] = Math.Max(maxLengths[i], cellText.Length);
                totalChars[i] += cellText.Length;
            }
        }

        // Calculate proportional widths
        int totalAllChars = totalChars.Sum();
        if (totalAllChars > 0)
        {
            for (int i = 0; i < columnCount; i++)
            {
                double proportion = (double)totalChars[i] / totalAllChars;
                double minWidth = Math.Max(totalWidth * 0.10, 20); // Minimum 10% or 20 units
                double maxWidth = totalWidth * 0.50; // Maximum 50%

                widths[i] = Math.Max(minWidth, Math.Min(maxWidth, totalWidth * proportion));
            }

            // Normalize to ensure total equals available width
            double totalCalculated = widths.Sum();
            if (totalCalculated > 0)
            {
                double ratio = totalWidth / totalCalculated;
                for (int i = 0; i < columnCount; i++)
                {
                    widths[i] *= ratio;
                }
            }
        }
        else
        {
            // Fallback to equal distribution with minimum widths
            double baseWidth = Math.Max(totalWidth / columnCount, 20);
            for (int i = 0; i < columnCount; i++)
            {
                widths[i] = baseWidth;
            }
        }

        // Final validation - ensure no width is too small
        for (int i = 0; i < columnCount; i++)
        {
            widths[i] = Math.Max(widths[i], 20);
        }

        return widths;
    }

    private List<string> WrapText(string text, XFont font, double maxWidth, XGraphics gfx)
    {
        if (string.IsNullOrEmpty(text))
            return new List<string>();

        var lines = new List<string>();
        var words = text.Split(' ');
        var currentLine = new StringBuilder();

        foreach (var word in words)
        {
            var testLine = currentLine.Length == 0 ? word : currentLine.ToString() + " " + word;
            var size = gfx.MeasureString(testLine, font);

            if (size.Width <= maxWidth)
            {
                if (currentLine.Length > 0)
                    currentLine.Append(" ");
                currentLine.Append(word);
            }
            else
            {
                if (currentLine.Length > 0)
                {
                    lines.Add(currentLine.ToString());
                    currentLine.Clear();
                }

                // Handle very long words
                if (gfx.MeasureString(word, font).Width > maxWidth)
                {
                    // Break long word
                    var chars = word.ToCharArray();
                    var partialWord = new StringBuilder();

                    foreach (var ch in chars)
                    {
                        var testPartial = partialWord.ToString() + ch; // Fix: Convert StringBuilder to string before concatenation
                        if (gfx.MeasureString(testPartial, font).Width <= maxWidth)
                        {
                            partialWord.Append(ch);
                        }
                        else
                        {
                            if (partialWord.Length > 0)
                            {
                                lines.Add(partialWord.ToString());
                                partialWord.Clear();
                            }
                            partialWord.Append(ch);
                        }
                    }

                    if (partialWord.Length > 0)
                        currentLine.Append(partialWord);
                }
                else
                {
                    currentLine.Append(word);
                }
            }
        }

        if (currentLine.Length > 0)
            lines.Add(currentLine.ToString());

        return lines.Count > 0 ? lines : new List<string> { "" };
    }
}


