using Microsoft.JSInterop;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using SeaRouteModel.Models;
using System.Text;

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
                // Check if we need a new page
                if (y + 150 > page.Height - margin)
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

    private async Task<double> DrawSection(XGraphics gfx, ReportSection section, double x, double y, double width,
        XFont font, XFont boldFont, XFont headerFont, PdfPage currentPage, PdfDocument document)
    {
        // Draw section title
        gfx.DrawString(section.Title, headerFont, XBrushes.Black, x, y);
        y += headerFont.Height * 1.5;

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

        return y + 20; // Add spacing after section
    }

    // *** UPDATED METHOD - IMPROVED TABLE ALIGNMENT ***
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
    
    // *** NEW HELPER METHOD - CALCULATE TABLE HEIGHT ***
    private double CalculateTableHeight(List<ReportTableRow> tableData, double[] colWidths,
        XFont font, XFont headerFont, double cellPadding, double baseRowHeight)
    {
        double totalHeight = 0;

        for (int i = 0; i < tableData.Count; i++)
        {
            var row = tableData[i];
            bool isHeader = i == 0;
            XFont currentFont = isHeader ? headerFont : font;

            double maxCellHeight = baseRowHeight;
            for (int j = 0; j < Math.Min(row.Cells.Count, colWidths.Length); j++)
            {
                var cellText = row.Cells[j] ?? "";
                var wrappedLines = WrapText(cellText, currentFont, colWidths[j] - (cellPadding * 2), null);
                double cellHeight = Math.Max(wrappedLines.Count * (currentFont.Height * 1.3) + (cellPadding * 2), baseRowHeight);
                maxCellHeight = Math.Max(maxCellHeight, cellHeight);
            }

            totalHeight += maxCellHeight;
        }

        return totalHeight;
    }

    // *** UPDATED METHOD - IMPROVED COMPLEX TABLE ALIGNMENT ***
    private double DrawComplexTable(XGraphics gfx, ComplexTableData complexTable, double x, double y,
      double width, XFont font, XFont boldFont)
    {
        if (complexTable == null || complexTable.Rows.Count == 0)
            return y;

        // Validate inputs
        if (width <= 0 || font == null || boldFont == null)
            return y;

        double cellPadding = 6;
        double minRowHeight = Math.Max(font.Height * 2, 28);

        // Improved column width calculation for Route Analysis table
        double availableWidth = Math.Max(width - 4, 100); // Ensure minimum width
        double[] columnWidths;

        // Check if this is a Route Analysis table (has seasonal columns)
        bool hasSeasonalColumns = complexTable.Rows.Any(r =>
            r.Cells.Any(c => c.Text?.Contains("Spring") == true ||
                            c.Text?.Contains("Summer") == true ||
                            c.Text?.Contains("Fall") == true ||
                            c.Text?.Contains("Winter") == true));

        if (hasSeasonalColumns)
        {
            // Route Analysis table with 7 columns
            columnWidths = new double[]
            {
            availableWidth * 0.14,  // Routes
            availableWidth * 0.12,  // Distance  
            availableWidth * 0.20,  // Annual Reduction Factor
            availableWidth * 0.135, // Spring
            availableWidth * 0.135, // Summer
            availableWidth * 0.135, // Fall
            availableWidth * 0.135  // Winter
            };
        }
        else
        {
            // Default equal distribution
            int maxColumns = complexTable.Rows.Max(r => r.Cells.Sum(c => c.ColumnSpan));
            columnWidths = new double[maxColumns];
            double equalWidth = availableWidth / maxColumns;
            for (int i = 0; i < maxColumns; i++)
            {
                columnWidths[i] = equalWidth;
            }
        }

        // Calculate total table height
        double tableHeight = 0;
        foreach (var row in complexTable.Rows)
        {
            double maxCellHeight = minRowHeight;
            int colIndex = 0;

            foreach (var cell in row.Cells)
            {
                double cellWidth = 0;
                for (int span = 0; span < cell.ColumnSpan && (colIndex + span) < columnWidths.Length; span++)
                {
                    cellWidth += columnWidths[colIndex + span];
                }
                cellWidth = Math.Max(cellWidth, 20); // Ensure minimum cell width
                cellWidth = Math.Max(cellWidth, 20); // Ensure minimum cell width

                var cellText = cell.Text ?? "";
                if (cell.SubItems?.Count > 0)
                {
                    cellText += "\n" + string.Join("\n", cell.SubItems);
                }

                XFont currentFont = (row.IsHeaderRow || cell.IsBold) ? boldFont : font;
                var wrappedLines = WrapText(cellText, currentFont, cellWidth - (cellPadding * 2), gfx);
                double cellHeight = Math.Max(wrappedLines.Count * (currentFont.Height * 1.2) + (cellPadding * 2), minRowHeight);
                maxCellHeight = Math.Max(maxCellHeight, cellHeight);

                colIndex += cell.ColumnSpan;
            }
            tableHeight += maxCellHeight;
        }

        // Draw outer table border
        gfx.DrawRectangle(XPens.Black, x, y, width, tableHeight);

        double currentY = y;

        foreach (var row in complexTable.Rows)
        {
            double maxCellHeight = minRowHeight;
            int colIndex = 0;

            // Calculate row height first
            foreach (var cell in row.Cells)
            {
                double cellWidth = 0;
                for (int span = 0; span < cell.ColumnSpan && (colIndex + span) < columnWidths.Length; span++)
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
                double cellHeight = Math.Max(wrappedLines.Count * (currentFont.Height * 1.2) + (cellPadding * 2), minRowHeight);
                maxCellHeight = Math.Max(maxCellHeight, cellHeight);

                colIndex += cell.ColumnSpan;
            }

            // Draw cells
            colIndex = 0;
            double xPos = x;

            foreach (var cell in row.Cells)
            {
                double cellWidth = 0;
                for (int span = 0; span < cell.ColumnSpan && (colIndex + span) < columnWidths.Length; span++)
                {
                    cellWidth += columnWidths[colIndex + span];
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
                    double totalTextHeight = wrappedLines.Count * (currentFont.Height * 1.2);
                    double textStartY = currentY + (maxCellHeight - totalTextHeight) / 2 + cellPadding;

                    // Draw text lines
                    for (int lineIndex = 0; lineIndex < wrappedLines.Count; lineIndex++)
                    {
                        var line = wrappedLines[lineIndex];
                        double textY = textStartY + (lineIndex * (currentFont.Height * 1.2));

                        if (row.IsHeaderRow)
                        {
                            // Center all header text with validation
                            double rectWidth = Math.Max(cellWidth - (cellPadding * 2), 10);
                            double rectHeight = Math.Max(currentFont.Height, 10);
                            var textRect = new XRect(xPos + cellPadding, textY, rectWidth, rectHeight);
                            gfx.DrawString(line, currentFont, XBrushes.Black, textRect, XStringFormats.TopCenter);
                        }
                        else
                        {
                            // For data: center numeric values, left-align text
                            bool isNumeric = double.TryParse(line.Trim(), out _) ||
                                           line.Contains("nm") || line.Contains("0.") || line.Contains("N/A");

                            if (isNumeric)
                            {
                                double rectWidth = Math.Max(cellWidth - (cellPadding * 2), 10);
                                double rectHeight = Math.Max(currentFont.Height, 10);
                                var textRect = new XRect(xPos + cellPadding, textY, rectWidth, rectHeight);
                                gfx.DrawString(line, currentFont, XBrushes.Black, textRect, XStringFormats.TopCenter);
                            }
                            else
                            {
                                gfx.DrawString(line, currentFont, XBrushes.Black, xPos + cellPadding, textY);
                            }
                        }
                    }
                }

                xPos += cellWidth;
                colIndex += cell.ColumnSpan;
            }

            currentY += maxCellHeight;
        }

        return currentY + 15;
    }

    // *** NEW HELPER METHOD - CALCULATE COMPLEX TABLE HEIGHT ***
    private double CalculateComplexTableHeight(ComplexTableData complexTable, double[] baseColWidths,
        XFont font, XFont boldFont, double cellPadding, double minRowHeight)
    {
        double totalHeight = 0;

        foreach (var row in complexTable.Rows)
        {
            double maxCellHeight = minRowHeight;

            foreach (var cell in row.Cells)
            {
                double cellWidth = baseColWidths[0] * cell.ColumnSpan;
                var cellText = cell.Text ?? "";
                if (cell.SubItems?.Count > 0)
                {
                    cellText += "\n" + string.Join("\n", cell.SubItems);
                }

                var wrappedLines = WrapText(cellText, row.IsHeaderRow ? boldFont : font,
                    cellWidth - (cellPadding * 2), null);
                double cellHeight = Math.Max(wrappedLines.Count * font.Height + (cellPadding * 2), minRowHeight);
                maxCellHeight = Math.Max(maxCellHeight, cellHeight);
            }

            totalHeight += maxCellHeight;
        }

        return totalHeight;
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

                // Calculate image dimensions to fit within the page width
                double maxWidth = width * 0.8; // Use 80% of available width
                double maxHeight = 300; // Maximum height

                double imageWidth = image.PixelWidth;
                double imageHeight = image.PixelHeight;

                // Scale image to fit
                double scaleX = maxWidth / imageWidth;
                double scaleY = maxHeight / imageHeight;
                double scale = Math.Min(scaleX, scaleY);

                double scaledWidth = imageWidth * scale;
                double scaledHeight = imageHeight * scale;

                // Center the image
                double imageX = x + (width - scaledWidth) / 2;

                // Check if image fits on current page
                if (y + scaledHeight > currentPage.Height - 50)
                {
                    // Create new page for image
                    var newPage = document.AddPage();
                    var newGfx = XGraphics.FromPdfPage(newPage);
                    gfx = newGfx;
                    y = 50; // Reset Y position
                    imageX = 50 + (newPage.Width - 100 - scaledWidth) / 2;
                }

                gfx.DrawImage(image, imageX, y, scaledWidth, scaledHeight);
                return y + scaledHeight + 20;
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

    // *** UPDATED METHOD - IMPROVED COLUMN WIDTH CALCULATION ***
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

    // *** UPDATED METHOD - IMPROVED TEXT WRAPPING ***
    private List<string> WrapText(string text, XFont font, double maxWidth, XGraphics gfx)
    {
        if (string.IsNullOrEmpty(text))
            return new List<string> { "" };

        // Handle null graphics context (used for height calculations)
        if (gfx == null)
        {
            // Simple word-count based estimation when graphics context is not available
            int estimatedCharsPerLine = Math.Max(1, (int)(maxWidth / (font.Height * 0.6))); // Rough estimation
            var words = text.Split(' ');
            var lines = new List<string>();
            var currentLine = new StringBuilder();

            foreach (var word in words)
            {
                if (currentLine.Length + word.Length + 1 <= estimatedCharsPerLine)
                {
                    if (currentLine.Length > 0) currentLine.Append(" ");
                    currentLine.Append(word);
                }
                else
                {
                    if (currentLine.Length > 0)
                    {
                        lines.Add(currentLine.ToString());
                        currentLine.Clear();
                    }
                    currentLine.Append(word);
                }
            }

            if (currentLine.Length > 0)
                lines.Add(currentLine.ToString());

            return lines.Count > 0 ? lines : new List<string> { "" };
        }

        var resultLines = new List<string>();
        var paragraphs = text.Split('\n');

        foreach (var paragraph in paragraphs)
        {
            if (string.IsNullOrEmpty(paragraph))
            {
                resultLines.Add("");
                continue;
            }

            var words = paragraph.Split(' ');
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
                        resultLines.Add(currentLine.ToString());
                        currentLine.Clear();
                    }

                    // Handle very long words that exceed maxWidth
                    if (gfx.MeasureString(word, font).Width > maxWidth)
                    {
                        // Break long word character by character
                        var chars = word.ToCharArray();
                        var partialWord = new StringBuilder();

                        foreach (var ch in chars)
                        {
                            var testPartial = partialWord.ToString() + ch;
                            if (gfx.MeasureString(testPartial, font).Width <= maxWidth)
                            {
                                partialWord.Append(ch);
                            }
                            else
                            {
                                if (partialWord.Length > 0)
                                {
                                    resultLines.Add(partialWord.ToString());
                                    partialWord.Clear();
                                }
                                partialWord.Append(ch);
                            }
                        }

                        if (partialWord.Length > 0)
                            currentLine.Append(partialWord.ToString());
                    }
                    else
                    {
                        currentLine.Append(word);
                    }
                }
            }

            if (currentLine.Length > 0)
                resultLines.Add(currentLine.ToString());
        }

        return resultLines.Count > 0 ? resultLines : new List<string> { "" };
    }
}