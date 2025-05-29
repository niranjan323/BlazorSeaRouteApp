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
            await _jsRuntime.InvokeVoidAsync("downloadFile", fileName, Convert.ToBase64String(pdfBytes));
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

    private double DrawTable(XGraphics gfx, List<ReportTableRow> tableData, double x, double y,
        double width, XFont font, XFont headerFont)
    {
        if (tableData == null || tableData.Count == 0)
            return y;

        int colCount = tableData[0].Cells.Count;
        double[] colWidths = CalculateColumnWidths(tableData, width, colCount);
        double rowHeight = font.Height * 1.8;
        double cellPadding = 5;

        for (int i = 0; i < tableData.Count; i++)
        {
            var row = tableData[i];
            bool isHeader = i == 0;
            XFont currentFont = isHeader ? headerFont : font;
            XBrush bgBrush = isHeader ? XBrushes.LightGray : XBrushes.White;

            double xPos = x;
            double maxCellHeight = rowHeight;

            // Calculate actual row height based on content
            for (int j = 0; j < Math.Min(row.Cells.Count, colCount); j++)
            {
                var cellText = row.Cells[j] ?? "";
                var wrappedLines = WrapText(cellText, currentFont, colWidths[j] - (cellPadding * 2), gfx);
                double cellHeight = wrappedLines.Count * font.Height + (cellPadding * 2);
                maxCellHeight = Math.Max(maxCellHeight, cellHeight);
            }

            // Draw cells
            xPos = x;
            for (int j = 0; j < Math.Min(row.Cells.Count, colCount); j++)
            {
                // Draw cell background and border
                gfx.DrawRectangle(XPens.Black, bgBrush, xPos, y, colWidths[j], maxCellHeight);

                // Draw cell text
                var cellText = row.Cells[j] ?? "";
                var wrappedLines = WrapText(cellText, currentFont, colWidths[j] - (cellPadding * 2), gfx);

                double textY = y + cellPadding;
                foreach (var line in wrappedLines)
                {
                    gfx.DrawString(line, currentFont, XBrushes.Black, xPos + cellPadding, textY);
                    textY += font.Height;
                }

                xPos += colWidths[j];
            }

            y += maxCellHeight;
        }

        return y + 10; // Add spacing after table
    }

    private double DrawComplexTable(XGraphics gfx, ComplexTableData complexTable, double x, double y,
        double width, XFont font, XFont boldFont)
    {
        if (complexTable == null || complexTable.Rows.Count == 0)
            return y;

        double cellPadding = 5;
        double minRowHeight = font.Height * 1.8;

        // Calculate column widths - simplified approach for complex table
        int maxCols = complexTable.Rows.Max(row => row.Cells.Count);
        double baseColWidth = width / maxCols;

        foreach (var row in complexTable.Rows)
        {
            double maxCellHeight = minRowHeight;
            double xPos = x;

            // Calculate row height
            foreach (var cell in row.Cells)
            {
                double cellWidth = baseColWidth * cell.ColumnSpan;
                var wrappedLines = WrapText(cell.Text ?? "", row.IsHeaderRow ? boldFont : font,
                    cellWidth - (cellPadding * 2), gfx);
                double cellHeight = wrappedLines.Count * font.Height + (cellPadding * 2);
                maxCellHeight = Math.Max(maxCellHeight, cellHeight);
            }

            // Draw cells
            xPos = x;
            foreach (var cell in row.Cells)
            {
                double cellWidth = baseColWidth * cell.ColumnSpan;
                XFont currentFont = (row.IsHeaderRow || cell.IsBold) ? boldFont : font;
                XBrush bgBrush = row.IsHeaderRow ? XBrushes.LightGray : XBrushes.White;

                // Draw cell background and border
                gfx.DrawRectangle(XPens.Black, bgBrush, xPos, y, cellWidth, maxCellHeight);

                // Draw cell text
                var cellText = cell.Text ?? "";
                if (cell.SubItems?.Count > 0)
                {
                    cellText += "\n" + string.Join("\n", cell.SubItems);
                }

                var wrappedLines = WrapText(cellText, currentFont, cellWidth - (cellPadding * 2), gfx);
                double textY = y + cellPadding;

                foreach (var line in wrappedLines)
                {
                    gfx.DrawString(line, currentFont, XBrushes.Black, xPos + cellPadding, textY);
                    textY += font.Height;
                }

                xPos += cellWidth;
            }

            y += maxCellHeight;
        }

        return y + 10; // Add spacing after table
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

    private double[] CalculateColumnWidths(List<ReportTableRow> tableData, double totalWidth, int columnCount)
    {
        var widths = new double[columnCount];

        // Simple equal distribution for now
        double baseWidth = totalWidth / columnCount;

        for (int i = 0; i < columnCount; i++)
        {
            widths[i] = baseWidth;
        }

        // Adjust for content if needed (simplified approach)
        if (tableData.Count > 0)
        {
            var headerRow = tableData[0];
            for (int i = 0; i < Math.Min(headerRow.Cells.Count, columnCount); i++)
            {
                var headerText = headerRow.Cells[i] ?? "";
                if (headerText.Length > 20) // Long header
                {
                    widths[i] = baseWidth * 1.2;
                }
                else if (headerText.Length < 8) // Short header
                {
                    widths[i] = baseWidth * 0.8;
                }
            }

            // Normalize to fit total width
            double totalCalculated = widths.Sum();
            double ratio = totalWidth / totalCalculated;
            for (int i = 0; i < columnCount; i++)
            {
                widths[i] *= ratio;
            }
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

