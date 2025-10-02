using Microsoft.JSInterop;
using NextGenEngApps.DigitalRules.CRoute.API.ResponseObjects;
using NextGenEngApps.DigitalRules.CRoute.DAL.Models.Domain.ReductionFactorReport;
using PuppeteerSharp;
using PuppeteerSharp.Cdp;
using SeaRouteModel.Models;
using SeaRouteModel.Reports;
using System.Text;
using System.Xml.Linq;
using static System.Net.Mime.MediaTypeNames;

namespace SeaRouteBlazorServerApp.Components.Services;

public interface IPdfService
{
    Task<byte[]> GenerateReportPdfAsync(ReportData reportData);
    Task DownloadPdfAsync(ReportData reportData, string fileName);
    Task<byte[]> GenerateReportPdfAsync(UnifiedReportData reportData);
    Task DownloadPdfAsync(UnifiedReportData reportData, string fileName);

    // New methods for ReportDataCollector
    Task<byte[]> GenerateReportPdfAsync(ReportDataCollector reportDataCollector);
    Task DownloadPdfAsync(ReportDataCollector reportDataCollector, string fileName);
}

public class PdfService : IPdfService
{
    private readonly IJSRuntime _jsRuntime;
    private readonly ILogger<PdfService> _logger;

    public PdfService(IJSRuntime jsRuntime, ILogger<PdfService> logger)
    {
        _jsRuntime = jsRuntime;
        _logger = logger;
    }

    // New: Generate PDF from ReportDataCollector
    public async Task<byte[]> GenerateReportPdfAsync(ReportDataCollector reportDataCollector)
    {
        try
        {
            if (reportDataCollector == null)
                throw new ArgumentNullException(nameof(reportDataCollector));

            _logger.LogInformation("Generating PDF report from ReportDataCollector for: {ReportTitle}", reportDataCollector.ReportTitle);

            var document = new PdfDocument();
            document.Info.Title = reportDataCollector.ReportTitle ?? "Route Reduction Factor Report";
            document.Info.Author = "ABS Online Reduction Factor Tool";
            document.Info.Subject = reportDataCollector.ReportInfo?.ReportName ?? "Reduction Factor Report";
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
            var title = reportDataCollector.ReportTitle ?? "Route Reduction Factor Report";
            gfx.DrawString(title, titleFont, XBrushes.Black,
                new XRect(margin, y, contentWidth, titleFont.Height), XStringFormats.TopLeft);
            y += titleFont.Height * 2;

            // Draw generation date
            gfx.DrawString($"Generated on: {DateTime.Now:yyyy-MM-dd HH:mm}", font, XBrushes.Black, margin, y);
            y += lineHeight * 1.5;

            // Draw attention section
            if (reportDataCollector.AttentionBlock != null && !string.IsNullOrEmpty(reportDataCollector.AttentionBlock.Salutation))
            {
                gfx.DrawString("Attention:", boldFont, XBrushes.Black, margin, y);
                y += lineHeight;
                gfx.DrawString(reportDataCollector.AttentionBlock.Salutation, font, XBrushes.Black, margin + 10, y);
                y += lineHeight;

                if (!string.IsNullOrEmpty(reportDataCollector.AttentionBlock.ABSContact))
                {
                    var contactLines = WrapText(reportDataCollector.AttentionBlock.ABSContact, font, contentWidth, gfx);
                    foreach (var line in contactLines)
                    {
                        gfx.DrawString(line, font, XBrushes.Black, margin + 10, y);
                        y += lineHeight;
                    }
                }
                y += lineHeight;
            }

            // User Inputs Section
            y = DrawUserInputsSection(gfx, reportDataCollector, margin, y, contentWidth, font, boldFont, headerFont);

            // Route Analysis Section (Reduction Factor Results)
            if (reportDataCollector.ReductionFactorResults?.Count > 0)
            {
                y = DrawRouteAnalysisSection(gfx, reportDataCollector.ReductionFactorResults, margin, y, contentWidth, font, boldFont, headerFont, page, document);
            }

            // Add notes section
            if (reportDataCollector.Notes != null)
            {
                y = DrawNotesSection(gfx, reportDataCollector.Notes, margin, y, contentWidth, font, boldFont, headerFont, italicFont, page, document);
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
            _logger.LogError(ex, "Error generating PDF report from ReportDataCollector");
            throw;
        }
    }

    // New: Download PDF from ReportDataCollector
    public async Task DownloadPdfAsync(ReportDataCollector reportDataCollector, string fileName)
    {
        try
        {
            var pdfBytes = await GenerateReportPdfAsync(reportDataCollector);
            await _jsRuntime.InvokeVoidAsync("downloadFileFromBase64", fileName, Convert.ToBase64String(pdfBytes));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading PDF report from ReportDataCollector");
            throw;
        }
    }

    private double DrawUserInputsSection(XGraphics gfx, ReportDataCollector dataCollector, double x, double y, double width,
        XFont font, XFont boldFont, XFont headerFont)
    {
        // Section header
        gfx.DrawString("User Inputs", headerFont, XBrushes.Black, x, y);
        y += headerFont.Height * 1.5;

        // Create table data
        var tableData = new List<ReportTableRow>
        {
            new ReportTableRow { Cells = new List<string> { "Parameter", "Value", "Code" } }
        };

        // Add route name
        if (dataCollector.ReportInfo != null)
        {
            tableData.Add(new ReportTableRow
            {
                Cells = new List<string>
                {
                    "Route Name",
                    dataCollector.ReportInfo.ReportName ?? "",
                    ""
                }
            });

            tableData.Add(new ReportTableRow
            {
                Cells = new List<string>
                {
                    "Report Date",
                    dataCollector.ReportInfo.ReportDate.ToString("yyyy-MM-dd"),
                    ""
                }
            });
        }

        // Add vessel info
        if (dataCollector.VesselInfo != null)
        {
            tableData.Add(new ReportTableRow
            {
                Cells = new List<string>
                {
                    "Vessel Name",
                    dataCollector.VesselInfo.VesselName ?? "",
                    ""
                }
            });

            tableData.Add(new ReportTableRow
            {
                Cells = new List<string>
                {
                    "IMO Number",
                    dataCollector.VesselInfo.IMONumber ?? "",
                    ""
                }
            });

            tableData.Add(new ReportTableRow
            {
                Cells = new List<string>
                {
                    "Flag",
                    dataCollector.VesselInfo.Flag ?? "",
                    ""
                }
            });
        }

        // Add ports
        if (dataCollector.RouteInfo?.Ports?.Count > 0)
        {
            var ports = dataCollector.RouteInfo.Ports.ToList();

            if (ports.Count > 0)
            {
                tableData.Add(new ReportTableRow
                {
                    Cells = new List<string>
                    {
                        "Departure Port",
                        ports.First().Name ?? "",
                        ports.First().Unlocode ?? ""
                    }
                });
            }

            if (ports.Count > 1)
            {
                tableData.Add(new ReportTableRow
                {
                    Cells = new List<string>
                    {
                        "Arrival Port",
                        ports.Last().Name ?? "",
                        ports.Last().Unlocode ?? ""
                    }
                });
            }
        }

        return DrawTable(gfx, tableData, x, y, width, font, boldFont);
    }

    private double DrawRouteAnalysisSection(XGraphics gfx, List<SegmentReductionFactorResults> reductionFactorResults,
        double x, double y, double width, XFont font, XFont boldFont, XFont headerFont, PdfPage currentPage, PdfDocument document)
    {
        // Check if we need a new page
        double estimatedHeight = headerFont.Height * 2 + (reductionFactorResults.Count + 1) * 30;
        if (y + estimatedHeight > currentPage.Height - 100)
        {
            var newPage = document.AddPage();
            gfx = XGraphics.FromPdfPage(newPage);
            y = 50;
        }

        // Section header
        gfx.DrawString("Route Analysis", headerFont, XBrushes.Black, x, y);
        y += headerFont.Height * 1.5;

        // Create complex table for route analysis
        var complexTable = new ComplexTableData();
        complexTable.Rows = new List<ComplexTableRow>();

        // Header row
        var headerRow = new ComplexTableRow { IsHeaderRow = true };
        headerRow.Cells.AddRange(new[]
        {
            new ComplexTableCell { Text = "Route", ColumnSpan = 1, IsBold = true },
            new ComplexTableCell { Text = "Segment", ColumnSpan = 1, IsBold = true },
            new ComplexTableCell { Text = "Distance (nm)", ColumnSpan = 1, IsBold = true },
            new ComplexTableCell { Text = "Annual RF", ColumnSpan = 1, IsBold = true },
            new ComplexTableCell { Text = "Spring", ColumnSpan = 1, IsBold = true },
            new ComplexTableCell { Text = "Summer", ColumnSpan = 1, IsBold = true },
            new ComplexTableCell { Text = "Fall", ColumnSpan = 1, IsBold = true },
            new ComplexTableCell { Text = "Winter", ColumnSpan = 1, IsBold = true }
        });
        complexTable.Rows.Add(headerRow);

        // Data rows
        foreach (var result in reductionFactorResults)
        {
            var dataRow = new ComplexTableRow { IsHeaderRow = false };

            var segmentName = $"{result.DeparturePort?.Name ?? "Unknown"} - {result.ArrivalPort?.Name ?? "Unknown"}";
            var portCodes = $"({result.DeparturePort?.Unlocode ?? "N/A"}) - ({result.ArrivalPort?.Unlocode ?? "N/A"})";

            dataRow.Cells.AddRange(new[]
            {
                new ComplexTableCell { Text = $"Leg {result.VoyageLegOrder}", ColumnSpan = 1 },
                new ComplexTableCell { Text = segmentName, SubItems = new List<string> { portCodes }, ColumnSpan = 1 },
                new ComplexTableCell { Text = result.Distance.ToString("F1"), ColumnSpan = 1 },
                new ComplexTableCell { Text = result.ReductionFactors?.Annual.ToString("F2") ?? "N/A", ColumnSpan = 1 },
                new ComplexTableCell { Text = result.ReductionFactors?.Spring.ToString("F2") ?? "N/A", ColumnSpan = 1 },
                new ComplexTableCell { Text = result.ReductionFactors?.Summer.ToString("F2") ?? "N/A", ColumnSpan = 1 },
                new ComplexTableCell { Text = result.ReductionFactors?.Fall.ToString("F2") ?? "N/A", ColumnSpan = 1 },
                new ComplexTableCell { Text = result.ReductionFactors?.Winter.ToString("F2") ?? "N/A", ColumnSpan = 1 }
            });
            complexTable.Rows.Add(dataRow);
        }

        return DrawComplexTable(gfx, complexTable, x, y, width, font, boldFont);
    }

    private double DrawNotesSection(XGraphics gfx, ReportNotes notes, double x, double y, double width,
        XFont font, XFont boldFont, XFont headerFont, XFont italicFont, PdfPage currentPage, PdfDocument document)
    {
        // Check if we need a new page
        if (y + 100 > currentPage.Height - 100)
        {
            var newPage = document.AddPage();
            gfx = XGraphics.FromPdfPage(newPage);
            y = 50;
        }

        gfx.DrawString("Notes:", headerFont, XBrushes.Black, x, y);
        y += headerFont.Height * 1.5;

        var notesList = new List<string>();

        if (!string.IsNullOrEmpty(notes.VesselCriteria))
        {
            notesList.Add(notes.VesselCriteria);
        }

        if (!string.IsNullOrEmpty(notes.GuideTitle))
        {
            var guideNote = notes.BuildGuideNote(notes.GuideTitle);
            if (!string.IsNullOrEmpty(guideNote))
            {
                notesList.Add(guideNote);
            }
        }

        foreach (var note in notesList)
        {
            // Check if note should be italic (starts and ends with asterisks)
            bool isItalic = note.StartsWith("*") && note.EndsWith("*");
            string noteText = isItalic ? note.Trim('*') : note;
            XFont noteFont = isItalic ? italicFont : font;

            var noteLines = WrapText("• " + noteText, noteFont, width, gfx);
            foreach (var line in noteLines)
            {
                if (y + 20 > currentPage.Height - 50)
                {
                    var newPage = document.AddPage();
                    gfx = XGraphics.FromPdfPage(newPage);
                    y = 50;
                }
                gfx.DrawString(line, noteFont, XBrushes.Black, x, y);
                y += 20;
            }
        }

        return y + 20;
    }

    // Existing methods remain the same...
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
                    g