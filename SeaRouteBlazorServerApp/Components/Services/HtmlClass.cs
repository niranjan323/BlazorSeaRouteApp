using NextGenEngApps.DigitalRules.CRoute.DAL.Models.Domain.ReductionFactorReport;
using NextGenEngApps.DigitalRules.CRoute.Models;
using NextGenEngApps.DigitalRules.CRoute.Models.ReductionFactorReport;
using SeaRouteModel.Models;
using SeaRouteModel.Reports;
using System.Reflection.Metadata;

// Add this ReportNotes class if it doesn't exist
namespace NextGenEngApps.DigitalRules.CRoute.Models.ReductionFactorReport
{
    public class ReportNotes
    {
        public string VesselCriteria { get; set; } = string.Empty;
        public string GuideTitle { get; set; } = string.Empty;
        public List<string> Items { get; set; } = new List<string>();

        public string BuildGuideNote(string guideTitle)
        {
            return $"<i>{guideTitle}</i> (April 2025)";
        }
    }
}

namespace NextGenEngApps.DigitalRules.CRoute.API.Services
{
    public class HtmlReportGeneratorService
    {
        private readonly ReportDataCollectorService _reportDataCollectorService;
        private readonly ILogger<HtmlReportGeneratorService> _logger;

        public HtmlReportGeneratorService(
            ReportDataCollectorService reportDataCollectorService,
            ILogger<HtmlReportGeneratorService> logger)
        {
            _reportDataCollectorService = reportDataCollectorService;
            _logger = logger;
        }

        public async Task<string> GenerateCompleteHtmlReportAsync(string routeVersionId)
        {
            try
            {
                // Step 1: Get JsonReportResponse from API
                var jsonResponse = await GetJsonReportResponseAsync(routeVersionId);
                if (jsonResponse == null)
                {
                    _logger.LogWarning("JsonReportResponse is null for RouteVersionId: {RouteVersionId}", routeVersionId);
                    return string.Empty;
                }

                // Step 2: Map JsonReportResponse to ReportDataCollector
                var dataCollector = MapJsonResponseToReportDataCollector(jsonResponse);

                // Step 3: Generate HTML from ReportDataCollector
                return GenerateHtmlFromDataCollector(dataCollector);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating HTML report for RouteVersionId: {RouteVersionId}", routeVersionId);
                throw;
            }
        }

        private async Task<JsonReportResponse> GetJsonReportResponseAsync(string routeVersionId)
        {
            // This should call your existing API endpoint
            return await _reportDataCollectorService.GetJsonReportAsync(routeVersionId);
        }

        private ReportDataCollector MapJsonResponseToReportDataCollector(JsonReportResponse jsonResponse)
        {
            if (jsonResponse?.Report == null)
            {
                _logger.LogWarning("JsonReportResponse or Report is null");
                return new ReportDataCollector();
            }

            var dataCollector = new ReportDataCollector
            {
                ReportTitle = jsonResponse.Report.Title,
                RecordId = jsonResponse.RecordId
            };

            // Map Attention Section
            if (jsonResponse.Report.Sections?.Attention != null)
            {
                dataCollector.AttentionBlock = new AttentionBlock
                {
                    Salutation = jsonResponse.Report.Sections.Attention.Salutation,
                    ABSContact = jsonResponse.Report.Sections.Attention.AbsContact,
                    // Extract departure and arrival ports from body or use defaults
                    DeparturePort = ExtractDeparturePortFromBody(jsonResponse.Report.Sections.Attention.Body),
                    ArrivalPort = ExtractArrivalPortFromBody(jsonResponse.Report.Sections.Attention.Body),
                    ReductionFactor = ExtractReductionFactorFromBody(jsonResponse.Report.Sections.Attention.Body)
                };
            }

            // Map Report Info
            if (jsonResponse.Report.Sections?.UserInputs?.ReportInfo != null)
            {
                dataCollector.ReportInfo = new ReportInfo
                {
                    ReportName = jsonResponse.Report.Sections.UserInputs.ReportInfo.RouteName,
                    ReportDate = DateOnly.TryParse(jsonResponse.Report.Sections.UserInputs.ReportInfo.ReportDate, out var reportDate)
                        ? reportDate
                        : DateOnly.FromDateTime(DateTime.UtcNow)
                };
            }

            // Map Vessel Info
            if (jsonResponse.Report.Sections?.UserInputs?.Vessel != null)
            {
                dataCollector.VesselInfo = new VesselInfo
                {
                    IMONumber = jsonResponse.Report.Sections.UserInputs.Vessel.Imo,
                    VesselName = jsonResponse.Report.Sections.UserInputs.Vessel.Name,
                    Flag = jsonResponse.Report.Sections.UserInputs.Vessel.Flag
                };
            }

            // Map Route Info (Ports)
            if (jsonResponse.Report.Sections?.UserInputs?.Ports != null)
            {
                foreach (var portString in jsonResponse.Report.Sections.UserInputs.Ports)
                {
                    var port = ParsePortFromString(portString);
                    dataCollector.RouteInfo.Ports.Add(port);
                }
            }

            // Map Route Analysis (Reduction Factor Results)
            if (jsonResponse.Report.Sections?.RouteAnalysis != null)
            {
                foreach (var segment in jsonResponse.Report.Sections.RouteAnalysis)
                {
                    var result = new SegmentReductionFactorResults
                    {
                        VoyageLegOrder = segment.Segment.Order,
                        Distance = segment.Segment.Distance
                    };

                    // Set reduction factors
                    result.ReductionFactors.Annual = segment.Segment.ReductionFactors.Annual;
                    result.ReductionFactors.Spring = segment.Segment.ReductionFactors.Spring;
                    result.ReductionFactors.Summer = segment.Segment.ReductionFactors.Summer;
                    result.ReductionFactors.Fall = segment.Segment.ReductionFactors.Fall;
                    result.ReductionFactors.Winter = segment.Segment.ReductionFactors.Winter;

                    // Parse departure and arrival ports from segment name
                    var segmentParts = segment.Segment.Name.Split(" - ");
                    if (segmentParts.Length == 2)
                    {
                        result.DeparturePort = ParsePortFromString(segmentParts[0]);
                        result.ArrivalPort = ParsePortFromString(segmentParts[1]);
                    }

                    dataCollector.ReductionFactorResults.Add(result);
                }
            }

            // Map Notes
            if (jsonResponse.Report.Sections?.Notes?.Any() == true)
            {
                var firstNote = jsonResponse.Report.Sections.Notes.First();
                dataCollector.Notes = new ReportNotes
                {
                    VesselCriteria = firstNote.VesselCriteria,
                    GuideTitle = ExtractGuideTitleFromNote(firstNote.GuideNote)
                };
            }

            return dataCollector;
        }

        private string GenerateHtmlFromDataCollector(ReportDataCollector dataCollector)
        {
            var html = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <title>{dataCollector.ReportTitle}</title>
    <style>
        body {{
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            margin: 20px;
            line-height: 1.6;
            color: #333;
            background-color: #f8f9fa;
        }}
        .report-container {{
            max-width: 1000px;
            margin: 0 auto;
            background-color: white;
            padding: 30px;
            border-radius: 8px;
            box-shadow: 0 2px 10px rgba(0,0,0,0.1);
        }}
        .header {{
            text-align: center;
            margin-bottom: 40px;
            padding-bottom: 20px;
            border-bottom: 2px solid #0066cc;
        }}
        .report-section {{
            margin-bottom: 35px;
        }}
        .report-table {{
            width: 100%;
            border-collapse: collapse;
            margin-top: 15px;
            background-color: white;
        }}
        .report-table th, .report-table td {{
            border: 1px solid #ddd;
            padding: 12px 8px;
            text-align: left;
        }}
        .report-table th {{
            background-color: #f2f2f2;
            font-weight: bold;
            color: #333;
        }}
        .highlight {{
            color: #0066cc;
            font-weight: 500;
        }}
        .reduction-factor {{
            font-weight: bold;
            color: #0066cc;
        }}
        h4 {{
            color: #333;
            font-size: 24px;
            margin-bottom: 10px;
        }}
        h5 {{
            color: #333;
            font-size: 18px;
            margin-bottom: 15px;
            border-bottom: 1px solid #eee;
            padding-bottom: 5px;
        }}
        .notes-section ul {{
            padding-left: 20px;
        }}
        .notes-section li {{
            margin-bottom: 8px;
        }}
        .seasonal-table {{
            width: 100%;
            margin-top: 20px;
        }}
        .seasonal-table th {{
            background-color: #0066cc;
            color: white;
            text-align: center;
            font-size: 12px;
        }}
        .seasonal-header {{
            background-color: #e3f2fd !important;
            color: #0066cc !important;
            font-weight: bold;
        }}
        .route-segment-cell {{
            font-weight: 500;
        }}
        .attention-box {{
            background-color: #fff3cd;
            border: 1px solid #ffeaa7;
            border-radius: 4px;
            padding: 15px;
            margin-bottom: 20px;
        }}
        .download-timestamp {{
            color: #666;
            font-size: 14px;
        }}
        .route-analysis-section {{
            overflow-x: auto;
        }}
        .entire-route-row {{
            background-color: #f8f9fa;
            font-weight: bold;
        }}
        .route-splitting-row {{
            background-color: #ffffff;
        }}
        .season-months {{
            font-size: 10px;
            color: #666;
            font-style: italic;
        }}
        @media print {{
            body {{ background-color: white; }}
            .report-container {{ box-shadow: none; }}
        }}
    </style>
</head>
<body>
    <div class=""report-container"">

        <!-- Header -->
        <div class=""header"">
            <h4>{dataCollector.ReportTitle}</h4>
            <p class=""download-timestamp""><strong>Downloaded:</strong> {DateTime.UtcNow:yyyy-MM-ddTHH:mm:ssZ}</p>
        </div>";

            // Only add Attention Section if data exists
            if (!string.IsNullOrEmpty(dataCollector.AttentionBlock?.Salutation) ||
                !string.IsNullOrEmpty(dataCollector.AttentionBlock?.DeparturePort) ||
                !string.IsNullOrEmpty(dataCollector.AttentionBlock?.ArrivalPort))
            {
                html += $@"
        <!-- Attention Section -->
        <div class=""report-section"">
            <div class=""attention-box"">";

                if (!string.IsNullOrEmpty(dataCollector.AttentionBlock?.Salutation))
                {
                    html += $@"<p><strong>Attention:</strong> {dataCollector.AttentionBlock.Salutation}</p>";
                }

                if (!string.IsNullOrEmpty(dataCollector.AttentionBlock?.DeparturePort) &&
                    !string.IsNullOrEmpty(dataCollector.AttentionBlock?.ArrivalPort))
                {
                    html += $@"
                <p>
                    {AttentionBlock.BuildAttentionBody(
                                dataCollector.AttentionBlock.DeparturePort,
                                dataCollector.AttentionBlock.ArrivalPort,
                                dataCollector.AttentionBlock.ReductionFactor.ToString("F2"))}
                </p>";
                }

                if (!string.IsNullOrEmpty(dataCollector.AttentionBlock?.ABSContact))
                {
                    html += $@"<p>{dataCollector.AttentionBlock.ABSContact}</p>";
                }

                html += @"
            </div>
        </div>";
            }

            html += @"
        <!-- User Inputs Section -->
        <div class=""report-section"">
            <h5>User Inputs</h5>
            <table class=""report-table"">";

            // Add report info rows if data exists
            if (!string.IsNullOrEmpty(dataCollector.ReportInfo?.ReportName))
            {
                html += $@"
                <tr>
                    <td><strong>Route Name:</strong></td>
                    <td class=""highlight"">{dataCollector.ReportInfo.ReportName}</td>
                    <td></td>
                </tr>";
            }

            if (dataCollector.ReportInfo?.ReportDate != default)
            {
                html += $@"
                <tr>
                    <td><strong>Report Date:</strong></td>
                    <td class=""highlight"">{dataCollector.ReportInfo.ReportDate:yyyy-MM-dd}</td>
                    <td></td>
                </tr>";
            }

            // Add vessel info rows if data exists
            if (!string.IsNullOrEmpty(dataCollector.VesselInfo?.VesselName))
            {
                html += $@"
                <tr>
                    <td><strong>Vessel Name:</strong></td>
                    <td class=""highlight"">{dataCollector.VesselInfo.VesselName}</td>
                    <td></td>
                </tr>";
            }

            if (!string.IsNullOrEmpty(dataCollector.VesselInfo?.IMONumber))
            {
                html += $@"
                <tr>
                    <td><strong>Vessel IMO:</strong></td>
                    <td class=""highlight"">{dataCollector.VesselInfo.IMONumber}</td>
                    <td></td>
                </tr>";
            }

            if (!string.IsNullOrEmpty(dataCollector.VesselInfo?.Flag))
            {
                html += $@"
                <tr>
                    <td><strong>Flag:</strong></td>
                    <td class=""highlight"">{dataCollector.VesselInfo.Flag}</td>
                    <td></td>
                </tr>";
            }

            // Add Ports if they exist
            if (dataCollector.RouteInfo?.Ports != null && dataCollector.RouteInfo.Ports.Any())
            {
                for (int i = 0; i < dataCollector.RouteInfo.Ports.Count; i++)
                {
                    var port = dataCollector.RouteInfo.Ports[i];
                    string portType;

                    if (i == 0)
                        portType = "Port of Departure:";
                    else if (i == dataCollector.RouteInfo.Ports.Count - 1)
                        portType = "Port of Arrival:";
                    else
                        portType = $"Loading Port {i}:";

                    html += $@"
                <tr>
                    <td><strong>{portType}</strong></td>
                    <td class=""highlight"">{port.Unlocode ?? "N/A"}</td>
                    <td>{port.Name ?? ""}</td>
                </tr>";
                }
            }

            html += @"
            </table>
        </div>";

            // Only add Output Section if reduction factor results exist
            if (dataCollector.ReductionFactorResults != null && dataCollector.ReductionFactorResults.Any())
            {
                html += @"
        <!-- Output Section -->
        <div class=""report-section route-analysis-section"">
            <h5>Output</h5>
            <table class=""seasonal-table report-table"">
                <thead>
                    <tr>
                        <th rowspan=""2""></th>
                        <th colspan=""3""></th>
                        <th colspan=""4"" class=""seasonal-header"">Seasonal Reduction Factor</th>
                    </tr>
                    <tr>
                        <th class=""seasonal-header"">Routes</th>
                        <th class=""seasonal-header"">Distance</th>
                        <th class=""seasonal-header"">Annual Reduction Factor</th>
                        <th>Spring<br/><span class=""season-months"">(Mar-May)</span></th>
                        <th>Summer<br/><span class=""season-months"">(Jun-Aug)</span></th>
                        <th>Fall<br/><span class=""season-months"">(Sep-Nov)</span></th>
                        <th>Winter<br/><span class=""season-months"">(Dec-Feb)</span></th>
                    </tr>
                </thead>
                <tbody>";

                // Entire Route (Order = 0)
                var entireRoute = dataCollector.ReductionFactorResults.FirstOrDefault(r => r.VoyageLegOrder == 0);
                if (entireRoute != null)
                {
                    html += $@"
                    <tr class=""entire-route-row"">
                        <td><strong>Entire Route</strong></td>
                        <td class=""route-segment-cell"">{entireRoute.DeparturePort?.Name ?? ""} - {entireRoute.ArrivalPort?.Name ?? ""}</td>
                        <td>{Math.Round(entireRoute.Distance)} nm</td>
                        <td class=""reduction-factor"">{entireRoute.ReductionFactors.Annual:F2}</td>
                        <td class=""reduction-factor"">{entireRoute.ReductionFactors.Spring:F2}</td>
                        <td class=""reduction-factor"">{entireRoute.ReductionFactors.Summer:F2}</td>
                        <td class=""reduction-factor"">{entireRoute.ReductionFactors.Fall:F2}</td>
                        <td class=""reduction-factor"">{entireRoute.ReductionFactors.Winter:F2}</td>
                    </tr>";
                }

                // Route Splitting (Orders > 0)
                var routeLegs = dataCollector.ReductionFactorResults.Where(r => r.VoyageLegOrder > 0).OrderBy(r => r.VoyageLegOrder).ToList();
                if (routeLegs.Any())
                {
                    var firstLeg = routeLegs.First();
                    html += $@"
                    <tr class=""route-splitting-row"">
                        <td rowspan=""{routeLegs.Count}""><strong>Route Splitting</strong></td>
                        <td class=""route-segment-cell"">{firstLeg.DeparturePort?.Name ?? ""} - {firstLeg.ArrivalPort?.Name ?? ""}</td>
                        <td>{Math.Round(firstLeg.Distance)} nm</td>
                        <td class=""reduction-factor"">{firstLeg.ReductionFactors.Annual:F2}</td>
                        <td class=""reduction-factor"">{firstLeg.ReductionFactors.Spring:F2}</td>
                        <td class=""reduction-factor"">{firstLeg.ReductionFactors.Summer:F2}</td>
                        <td class=""reduction-factor"">{firstLeg.ReductionFactors.Fall:F2}</td>
                        <td class=""reduction-factor"">{firstLeg.ReductionFactors.Winter:F2}</td>
                    </tr>";

                    // Remaining legs
                    for (int i = 1; i < routeLegs.Count; i++)
                    {
                        var leg = routeLegs[i];
                        html += $@"
                    <tr class=""route-splitting-row"">
                        <td class=""route-segment-cell"">{leg.DeparturePort?.Name ?? ""} - {leg.ArrivalPort?.Name ?? ""}</td>
                        <td>{Math.Round(leg.Distance)} nm</td>
                        <td class=""reduction-factor"">{leg.ReductionFactors.Annual:F2}</td>
                        <td class=""reduction-factor"">{leg.ReductionFactors.Spring:F2}</td>
                        <td class=""reduction-factor"">{leg.ReductionFactors.Summer:F2}</td>
                        <td class=""reduction-factor"">{leg.ReductionFactors.Fall:F2}</td>
                        <td class=""reduction-factor"">{leg.ReductionFactors.Winter:F2}</td>
                    </tr>";
                    }
                }

                html += @"
                </tbody>
            </table>
        </div>";
            }

            html += @"
        <!-- Route Map Placeholder -->
        <div class=""report-section"">
            <h5>Route Map</h5>
            <div style=""height: 200px; width: 100%; border: 1px solid #ddd; display: flex; align-items: center; justify-content: center; background-color: #f8f9fa;"">
                <p style=""color: #666;"">Route Map (Map visualization would be rendered here)</p>
            </div>
        </div>";

            // Only add Notes Section if notes data exists
            if (dataCollector.Notes != null &&
                (!string.IsNullOrEmpty(dataCollector.Notes.VesselCriteria) || !string.IsNullOrEmpty(dataCollector.Notes.GuideTitle)))
            {
                html += @"
        <!-- Notes Section -->
        <div class=""report-section notes-section"">
            <h5>Notes</h5>
            <ul>";

                if (!string.IsNullOrEmpty(dataCollector.Notes.VesselCriteria))
                {
                    html += $@"<li>{dataCollector.Notes.VesselCriteria}</li>";
                }

                if (!string.IsNullOrEmpty(dataCollector.Notes.GuideTitle))
                {
                    html += $@"<li><i>{dataCollector.Notes.GuideTitle}</i> (April 2025)</li>";
                }

                html += @"
            </ul>
        </div>";
            }

            html += @"
    </div>
</body>
</html>";

            return html;
        }

        #region Helper Methods

        private PortInfo ParsePortFromString(string portString)
        {
            var match = System.Text.RegularExpressions.Regex.Match(portString, @"^(.+?)\s*\(([^)]+)\)$");
            if (match.Success)
            {
                return new PortInfo
                {
                    Name = match.Groups[1].Value.Trim(),
                    Unlocode = match.Groups[2].Value.Trim() == "N/A" ? null : match.Groups[2].Value.Trim()
                };
            }
            return new PortInfo { Name = portString.Trim(), Unlocode = null };
        }

        private string ExtractDeparturePortFromBody(string body)
        {
            var match = System.Text.RegularExpressions.Regex.Match(body, @"route from\s+(.+?)\s+to");
            return match.Success ? match.Groups[1].Value.Trim() : string.Empty;
        }

        private string ExtractArrivalPortFromBody(string body)
        {
            var match = System.Text.RegularExpressions.Regex.Match(body, @"to\s+(.+?)\s+is");
            return match.Success ? match.Groups[1].Value.Trim() : string.Empty;
        }

        private double ExtractReductionFactorFromBody(string body)
        {
            var match = System.Text.RegularExpressions.Regex.Match(body, @"is\s+([\d.]+)");
            return match.Success && double.TryParse(match.Groups[1].Value, out var factor) ? factor : 0.0;
        }

        private string ExtractGuideTitleFromNote(string guideNote)
        {
            if (string.IsNullOrEmpty(guideNote))
                return "ABS Guide for Certification of Container Security Systems";

            var match = System.Text.RegularExpressions.Regex.Match(guideNote, @"<i>(.+?)</i>");
            return match.Success ? match.Groups[1].Value.Trim() : "ABS Guide for Certification of Container Security Systems";
        }

        #endregion
    }
}