using Microsoft.Extensions.Logging;
using NextGenEngApps.DigitalRules.CRoute.DAL.Models.Domain.ReductionFactorReport;
using SeaRouteModel.Models;
using SeaRouteModel.Reports;
using System.Text.Json;

namespace YourNamespace.Services
{
    public interface IReportService
    {
        Task<JsonReportResponse> GetReportDataAsync(string routeVersionId);
        Task<string> GenerateReportHtmlAsync(string routeVersionId);
    }

    public class ReportService : IReportService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ReportService> _logger;

        public ReportService(HttpClient httpClient, ILogger<ReportService> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<JsonReportResponse> GetReportDataAsync(string routeVersionId)
        {
            try
            {
                if (string.IsNullOrEmpty(routeVersionId))
                    throw new ArgumentException("Route Version ID is required", nameof(routeVersionId));

                var response = await _httpClient.GetAsync($"api/route_versions/{routeVersionId}/json_report");

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Failed to get report data for Route Version ID {RouteVersionId}. Status: {StatusCode}",
                        routeVersionId, response.StatusCode);
                    return null;
                }

                var jsonContent = await response.Content.ReadAsStringAsync();
                var reportData = JsonSerializer.Deserialize<JsonReportResponse>(jsonContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return reportData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting report data for Route Version {RouteVersionId}", routeVersionId);
                throw;
            }
        }

        public async Task<string> GenerateReportHtmlAsync(string routeVersionId)
        {
            var reportData = await GetReportDataAsync(routeVersionId);
            if (reportData?.Report == null)
                return string.Empty;

            // Transform JsonReportResponse to ReportDataCollector for compatibility
            var dataCollector = TransformToReportDataCollector(reportData);

            // Generate HTML using the transformed data
            return GenerateHtmlFromDataCollector(dataCollector, reportData);
        }

        private ReportDataCollector TransformToReportDataCollector(JsonReportResponse jsonReport)
        {
            var dataCollector = new ReportDataCollector
            {
                ReportTitle = jsonReport.Report.Title,
                RecordId = jsonReport.RecordId
            };

            // Transform Attention Block
            if (jsonReport.Report.Sections?.Attention != null)
            {
                dataCollector.AttentionBlock = new AttentionBlock
                {
                    Salutation = jsonReport.Report.Sections.Attention.Salutation,
                    ABSContact = jsonReport.Report.Sections.Attention.AbsContact
                };
            }

            // Transform Report Info
            if (jsonReport.Report.Sections?.UserInputs?.ReportInfo != null)
            {
                dataCollector.ReportInfo = new ReportInfo
                {
                    ReportName = jsonReport.Report.Sections.UserInputs.ReportInfo.RouteName,
                    ReportDate = DateTime.TryParse(jsonReport.Report.Sections.UserInputs.ReportInfo.ReportDate, out var date)
                        ? date : DateTime.UtcNow
                };
            }

            // Transform Vessel Info
            if (jsonReport.Report.Sections?.UserInputs?.Vessel != null)
            {
                dataCollector.VesselInfo = new VesselInfo
                {
                    IMONumber = jsonReport.Report.Sections.UserInputs.Vessel.Imo,
                    VesselName = jsonReport.Report.Sections.UserInputs.Vessel.Name,
                    Flag = jsonReport.Report.Sections.UserInputs.Vessel.Flag
                };
            }

            // Transform Route Analysis
            if (jsonReport.Report.Sections?.RouteAnalysis != null)
            {
                var reductionFactorResults = jsonReport.Report.Sections.RouteAnalysis.Select(ra => new SegmentReductionFactorResults
                {
                    VoyageLegOrder = ra.Segment.Order,
                    Distance = ra.Segment.Distance,
                    DeparturePort = ExtractPortFromSegmentName(ra.Segment.Name, true),
                    ArrivalPort = ExtractPortFromSegmentName(ra.Segment.Name, false),
                    ReductionFactors = new SeasonalReductionFactors
                    {
                        Annual = ra.Segment.ReductionFactors.Annual,
                        Spring = ra.Segment.ReductionFactors.Spring,
                        Summer = ra.Segment.ReductionFactors.Summer,
                        Fall = ra.Segment.ReductionFactors.Fall,
                        Winter = ra.Segment.ReductionFactors.Winter
                    }
                }).ToList();

                // Add to dataCollector (you'll need to modify ReportDataCollector to allow adding results)
                foreach (var result in reductionFactorResults)
                {
                    dataCollector.ReductionFactorResults.Add(result);
                }
            }

            return dataCollector;
        }

        private Port ExtractPortFromSegmentName(string segmentName, bool isDeparture)
        {
            // Parse segment name like "Port Name (CODE) - Port Name (CODE)"
            var parts = segmentName.Split(" - ");
            var portPart = isDeparture ? parts[0] : parts[1];

            var nameEnd = portPart.LastIndexOf(" (");
            var name = nameEnd > 0 ? portPart.Substring(0, nameEnd) : "Unknown";
            var code = portPart.Substring(nameEnd + 2, portPart.Length - nameEnd - 3);

            return new Port
            {
                Name = name,
                Unlocode = code
            };
        }

        private string GenerateHtmlFromDataCollector(ReportDataCollector dataCollector, JsonReportResponse jsonReport)
        {
            var html = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <title>{dataCollector.ReportTitle}</title>
    <style>
        body {{
            font-family: Arial, sans-serif;
            margin: 20px;
            line-height: 1.6;
        }}
        .report-container {{
            max-width: 800px;
            margin: 0 auto;
        }}
        .header {{
            text-align: center;
            margin-bottom: 30px;
        }}
        .report-section {{
            margin-bottom: 30px;
        }}
        .report-table {{
            width: 100%;
            border-collapse: collapse;
            margin-top: 10px;
        }}
        .report-table th, .report-table td {{
            border: 1px solid #ddd;
            padding: 8px;
            text-align: left;
        }}
        .report-table th {{
            background-color: #f2f2f2;
            font-weight: bold;
        }}
        .highlight {{
            color: #0066cc;
        }}
        .reduction-factor {{
            font-weight: bold;
            color: #0066cc;
        }}
        h4, h5 {{
            color: #333;
        }}
        .notes-section ul {{
            padding-left: 20px;
        }}
        .seasonal-table {{
            width: 100%;
            margin-top: 20px;
        }}
        .seasonal-table th {{
            background-color: #f8f9fa;
            text-align: center;
        }}
    </style>
</head>
<body>
    <div class=""report-container"">
        <div class=""header"">
            <h4>{dataCollector.ReportTitle}</h4>
            <p><strong>Downloaded:</strong> {jsonReport.Report.DownloadTimestamp}</p>
        </div>

        <!-- Attention Section -->
        <div class=""report-section"">
            <p><strong>Attention:</strong> {dataCollector.AttentionBlock?.Salutation ?? "Mr. Alan Bond, Mani Industries (WCN: 123456)"}</p>
            <p>{jsonReport.Report.Sections?.Attention?.Body ?? ""}</p>
            <p>For any clarifications, contact {dataCollector.AttentionBlock?.ABSContact ?? "Mr. Holland Wright at +65 6371 2xxx or (HWright@eagle.org)"}.</p>
        </div>

        <!-- User Inputs Section -->
        <div class=""report-section"">
            <h5>User Inputs</h5>
            <table class=""report-table"">
                <tr>
                    <td><strong>Route Name:</strong></td>
                    <td class=""highlight"">{dataCollector.ReportInfo?.ReportName ?? ""}</td>
                    <td></td>
                </tr>
                <tr>
                    <td><strong>Report Date:</strong></td>
                    <td class=""highlight"">{dataCollector.ReportInfo?.ReportDate.ToString("yyyy-MM-dd") ?? DateTime.UtcNow.ToString("yyyy-MM-dd")}</td>
                    <td></td>
                </tr>
                <tr>
                    <td><strong>Vessel Name:</strong></td>
                    <td class=""highlight"">{dataCollector.VesselInfo?.VesselName ?? ""}</td>
                    <td></td>
                </tr>
                <tr>
                    <td><strong>Vessel IMO:</strong></td>
                    <td class=""highlight"">{dataCollector.VesselInfo?.IMONumber ?? ""}</td>
                    <td></td>
                </tr>
                <tr>
                    <td><strong>Flag:</strong></td>
                    <td class=""highlight"">{dataCollector.VesselInfo?.Flag ?? ""}</td>
                    <td></td>
                </tr>";

            // Add ports
            if (jsonReport.Report.Sections?.UserInputs?.Ports != null)
            {
                for (int i = 0; i < jsonReport.Report.Sections.UserInputs.Ports.Count; i++)
                {
                    var portInfo = jsonReport.Report.Sections.UserInputs.Ports[i];
                    var portName = i == 0 ? "Port of Departure" :
                                  i == jsonReport.Report.Sections.UserInputs.Ports.Count - 1 ? "Port of Arrival" :
                                  $"Loading Port {i}";

                    html += $@"
                <tr>
                    <td><strong>{portName}:</strong></td>
                    <td class=""highlight"">{portInfo}</td>
                    <td></td>
                </tr>";
                }
            }

            html += @"
            </table>
        </div>

        <!-- Output Section -->
        <div class=""report-section"">
            <h5>Output</h5>";

            if (jsonReport.Report.Sections?.RouteAnalysis != null && jsonReport.Report.Sections.RouteAnalysis.Any())
            {
                html += @"
            <table class=""seasonal-table report-table"">
                <thead>
                    <tr>
                        <th rowspan=""2"">Route Segment</th>
                        <th rowspan=""2"">Distance (nm)</th>
                        <th rowspan=""2"">Annual</th>
                        <th colspan=""4"">Seasonal Reduction Factors</th>
                    </tr>
                    <tr>
                        <th>Spring</th>
                        <th>Summer</th>
                        <th>Fall</th>
                        <th>Winter</th>
                    </tr>
                </thead>
                <tbody>";

                foreach (var segment in jsonReport.Report.Sections.RouteAnalysis)
                {
                    html += $@"
                    <tr>
                        <td>{segment.Segment.Name}</td>
                        <td>{Math.Round(segment.Segment.Distance)} nm</td>
                        <td class=""reduction-factor"">{segment.Segment.ReductionFactors.Annual:F2}</td>
                        <td class=""reduction-factor"">{segment.Segment.ReductionFactors.Spring:F2}</td>
                        <td class=""reduction-factor"">{segment.Segment.ReductionFactors.Summer:F2}</td>
                        <td class=""reduction-factor"">{segment.Segment.ReductionFactors.Fall:F2}</td>
                        <td class=""reduction-factor"">{segment.Segment.ReductionFactors.Winter:F2}</td>
                    </tr>";
                }

                html += @"
                </tbody>
            </table>";
            }

            html += @"
        </div>

        <!-- Notes Section -->
        <div class=""report-section notes-section"">
            <h5>Notes</h5>
            <ul>";

            if (jsonReport.Report.Sections?.Notes != null)
            {
                foreach (var note in jsonReport.Report.Sections.Notes)
                {
                    if (!string.IsNullOrEmpty(note.VesselCriteria))
                    {
                        html += $"<li>{note.VesselCriteria}</li>";
                    }
                    if (!string.IsNullOrEmpty(note.GuideNote))
                    {
                        html += $"<li><i>{note.GuideNote}</i></li>";
                    }
                }
            }
            else
            {
                html += @"
                <li>The vessel is to have CLP-V or CLP-V(PARR) notation, and the onboard Computer Lashing Program is to be approved to handle Route Reduction Factors.</li>
                <li><i>ABS Guide for Certification of Container Security Systems</i> (April 2025)</li>";
            }

            html += @"
            </ul>
        </div>
    </div>
</body>
</html>";

            return html;
        }
    }

    // Define the JSON response models based on your BuildJsonReportFromDataCollector method
    public class JsonReportResponse
    {
        public string RouteVersionId { get; set; }
        public string RecordId { get; set; }
        public ReportData Report { get; set; }
    }

    public class ReportData
    {
        public string Title { get; set; }
        public string DownloadTimestamp { get; set; }
        public ReportSections Sections { get; set; }
    }

    public class ReportSections
    {
        public AttentionSection Attention { get; set; }
        public UserInputsSection UserInputs { get; set; }
        public List<RouteAnalysisSegment> RouteAnalysis { get; set; }
        public List<ReportNote> Notes { get; set; }
    }

    public class AttentionSection
    {
        public string Salutation { get; set; }
        public string Body { get; set; }
        public string AbsContact { get; set; }
    }

    public class UserInputsSection
    {
        public ReportInfoSection ReportInfo { get; set; }
        public VesselSection Vessel { get; set; }
        public List<string> Ports { get; set; }
    }

    public class ReportInfoSection
    {
        public string RouteName { get; set; }
        public string ReportDate { get; set; }
    }

    public class VesselSection
    {
        public string Imo { get; set; }
        public string Name { get; set; }
        public string Flag { get; set; }
    }

    public class RouteAnalysisSegment
    {
        public SegmentInfo Segment { get; set; }
    }

    public class SegmentInfo
    {
        public string Name { get; set; }
        public int Order { get; set; }
        public double Distance { get; set; }
        public ReductionFactorsInfo ReductionFactors { get; set; }
    }

    public class ReductionFactorsInfo
    {
        public decimal Annual { get; set; }
        public decimal Spring { get; set; }
        public decimal Summer { get; set; }
        public decimal Fall { get; set; }
        public decimal Winter { get; set; }
    }

    public class ReportNote
    {
        public string VesselCriteria { get; set; }
        public string GuideNote { get; set; }
    }
}