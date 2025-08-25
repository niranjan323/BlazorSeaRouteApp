// 1. Extended ReportDataCollector class
using Microsoft.AspNetCore.Mvc;
using NextGenEngApps.DigitalRules.CRoute.API.ResponseObjects;
using NextGenEngApps.DigitalRules.CRoute.API.Services.Interfaces;
using NextGenEngApps.DigitalRules.CRoute.DAL.Models.Domain.ReductionFactorReport;
using NextGenEngApps.DigitalRules.CRoute.DAL.Repositories;
using SeaRouteModel.Models;
using System.Text;
using System.Text.Json;

namespace NextGenEngApps.DigitalRules.CRoute.DAL.Models.Domain.ReductionFactorReport
{
    public class ReportDataCollector
    {
        private readonly List<SegmentReductionFactorResults> _segmentReductionFactorResults;
        private readonly RouteInfo _routeInfo;

        public string ReportTitle { get; set; }
        public string RecordId { get; set; }
        public AttentionBlock AttentionBlock { get; set; } = new AttentionBlock();
        public ReportInfo ReportInfo { get; set; } = new ReportInfo();
        public VesselInfo VesselInfo { get; set; } = new VesselInfo();
        public ShortVoyageInfo ShortVoyageInfo { get; set; } = new ShortVoyageInfo();
        public bool IsShortVoyageReport { get; set; } = false;

        public RouteInfo RouteInfo
        {
            get => _routeInfo;
        }

        public List<SegmentReductionFactorResults> ReductionFactorResults
        {
            get => _segmentReductionFactorResults;
        }

        public ReportNotes Notes { get; set; } = new ReportNotes();

        public ReportDataCollector()
        {
            _routeInfo = new();
            _segmentReductionFactorResults = [];
        }
    }

    // NEW: Short Voyage specific info class for ReportDataCollector
    public class ShortVoyageInfo
    {
        public DateTime DepartureTime { get; set; }
        public DateTime ArrivalTime { get; set; }
        public DateTime? ForecastTime { get; set; }
        public double? ForecastSwellHeight { get; set; }
        public double? ForecastWindHeight { get; set; }
        public double? ShortVoyageReductionFactor { get; set; }
        public double? CalculatedWaveHsmax { get; set; }
        public TimeSpan VoyageDuration => ArrivalTime - DepartureTime;
    }
}

// 2. Extended AttentionBlock for Short Voyage
namespace NextGenEngApps.DigitalRules.CRoute.DAL.Models.Domain.ReductionFactorReport
{
    public class AttentionBlock
    {
        private string _departurePort = string.Empty;
        private string _arrivalPort = string.Empty;
        private double _reductionFactor = 0.0;

        public string DeparturePort
        {
            get => _departurePort;
            set => _departurePort = value ?? string.Empty;
        }

        public string ArrivalPort
        {
            get => _arrivalPort;
            set => _arrivalPort = value ?? string.Empty;
        }

        public double ReductionFactor
        {
            get => _reductionFactor;
            set => _reductionFactor = value;
        }

        public string Salutation { get; set; } = string.Empty;
        public string ABSContact { get; set; } = string.Empty;

        public AttentionBlock()
        {
            Salutation = "Mr. Alan Bond, Mani Industries (WCN: 123456)";
            ABSContact = "For any clarifications, contact Mr. Holland Wright at +65 6371 2xxx or (HWright@eagle.org).";
        }

        public static string BuildAttentionBody(string depPortFmtStr, string arrPortFmtStr, string rfFmtStr)
        {
            return $"Based on your inputs in the ABS Online Reduction Factor Tool, the calculated Reduction Factor for the route from {depPortFmtStr} to {arrPortFmtStr} is {rfFmtStr}. More details can be found below.";
        }

        // NEW: Short Voyage specific attention body builder
        public static string BuildShortVoyageAttentionBody(string depPortFmtStr, string arrPortFmtStr, string rfFmtStr, DateTime departureTime, DateTime arrivalTime)
        {
            var duration = arrivalTime - departureTime;
            return $"Based on your inputs in the ABS Online Short Voyage Reduction Factor Tool, the calculated Short Voyage Reduction Factor for the route from {depPortFmtStr} to {arrPortFmtStr} (Duration: {duration.TotalHours:F1} hours) is {rfFmtStr}. More details can be found below.";
        }
    }
}

// 3. Extended ReportNotes for Short Voyage
namespace NextGenEngApps.DigitalRules.CRoute.DAL.Models.Domain.ReductionFactorReport
{
    public class ReportNotes
    {
        public string VesselCriteria { get; set; } = string.Empty;
        public string GuideTitle = "ABS Guide for Certification of Container Security Systems";

        // NEW: Short Voyage specific notes
        public string ShortVoyageCriteria { get; set; } = string.Empty;

        public ReportNotes()
        {
            VesselCriteria = "The vessel is to have CLP-V or CLP-V(PARR) notation, and the onboard Computer Lashing Program is to be approved to handle Route Reduction Factors.";
            ShortVoyageCriteria = "The vessel is to have CLP-V or CLP-V(PARR) notation, and the onboard Computer Lashing Program is to be approved to handle Short Voyage Reduction Factors.";
        }

        public string BuildGuideNote(string guideTitleFormatString)
        {
            return $"{guideTitleFormatString} (April 2025)";
        }

        // NEW: Build short voyage specific notes
        public List<string> BuildShortVoyageNotes()
        {
            return new List<string>
            {
                ShortVoyageCriteria,
                "The minimum value of the Short Voyage Reduction Factor is 0.6 and needs to be included in Cargo Securing Manual (CSM).",
                "A short voyage is to have a duration of less than 72 hours from departure port to arrival port.",
                "The weather reports need to be received within 6 hours of departure.",
                "The forecasted wave height needs to cover the duration of the voyage plus 12 hours."
            };
        }
    }
}

// 4. Service Interface Extension
using NextGenEngApps.DigitalRules.CRoute.DAL.Models.Domain.ReductionFactorReport;

namespace NextGenEngApps.DigitalRules.CRoute.API.Services.Interfaces
{
    public interface IShortVoyageReportService
    {
        Task<ReportDataCollector> GetShortVoyageReportDataAsync(Guid recordId);
        Task<JsonReportResponse> GetShortVoyageJsonReportAsync(Guid recordId);
        Task<HtmlReportResponse> GetShortVoyageHtmlReportAsync(Guid recordId);
    }
}

// 5. Service Implementation
using Microsoft.Extensions.Logging;
using NextGenEngApps.DigitalRules.CRoute.API.Services.Interfaces;
using NextGenEngApps.DigitalRules.CRoute.DAL.Models.Domain.ReductionFactorReport;
using NextGenEngApps.DigitalRules.CRoute.DAL.Repositories;

namespace NextGenEngApps.DigitalRules.CRoute.API.Services
{
    public class ShortVoyageReportService : IShortVoyageReportService
    {
        private readonly IShortVoyageRecordService _shortVoyageRecordService;
        private readonly ILogger<ShortVoyageReportService> _logger;

        public ShortVoyageReportService(
            IShortVoyageRecordService shortVoyageRecordService,
            ILogger<ShortVoyageReportService> logger)
        {
            _shortVoyageRecordService = shortVoyageRecordService ?? throw new ArgumentNullException(nameof(shortVoyageRecordService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ReportDataCollector> GetShortVoyageReportDataAsync(Guid recordId)
        {
            try
            {
                _logger.LogInformation($"Building short voyage report data for record ID: {recordId}");

                // Get short voyage record data using existing restoration endpoint
                var shortVoyageRecord = await _shortVoyageRecordService.RestoreShortVoyageRecordAsync(recordId);
                if (shortVoyageRecord == null)
                {
                    _logger.LogWarning($"Short voyage record not found for ID: {recordId}");
                    return null;
                }

                // Build ReportDataCollector from the restoration data
                var dataCollector = BuildReportDataCollectorFromRestoration(shortVoyageRecord);

                return dataCollector;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error building short voyage report data for record ID: {recordId}");
                throw;
            }
        }

        public async Task<JsonReportResponse> GetShortVoyageJsonReportAsync(Guid recordId)
        {
            try
            {
                var dataCollector = await GetShortVoyageReportDataAsync(recordId);
                if (dataCollector == null)
                    return null;

                return BuildJsonReportFromDataCollector(dataCollector, recordId.ToString());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error building short voyage JSON report for record ID: {recordId}");
                throw;
            }
        }

        public async Task<HtmlReportResponse> GetShortVoyageHtmlReportAsync(Guid recordId)
        {
            try
            {
                var dataCollector = await GetShortVoyageReportDataAsync(recordId);
                if (dataCollector == null)
                    return null;

                string html = GenerateHtmlFromDataCollector(dataCollector);
                return new HtmlReportResponse
                {
                    Html = html,
                    ContentType = "text/html; charset=utf-8",
                    FileName = $"{dataCollector.ReportTitle?.Replace(' ', '_')}_ShortVoyage.html"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error building short voyage HTML report for record ID: {recordId}");
                throw;
            }
        }

        private ReportDataCollector BuildReportDataCollectorFromRestoration(ShortVoyageRecordRestoreResponse shortVoyageRecord)
        {
            var dataCollector = new ReportDataCollector
            {
                RecordId = shortVoyageRecord.RecordId,
                ReportTitle = $"Short Voyage Report - {shortVoyageRecord.RouteName}",
                IsShortVoyageReport = true
            };

            // Report Info
            dataCollector.ReportInfo.ReportName = shortVoyageRecord.RouteName;
            if (shortVoyageRecord.RecordDate.HasValue)
            {
                dataCollector.ReportInfo.ReportDate = DateOnly.FromDateTime(shortVoyageRecord.RecordDate.Value);
            }

            // Vessel Info
            if (shortVoyageRecord.Vessel != null)
            {
                dataCollector.VesselInfo.VesselName = shortVoyageRecord.Vessel.VesselName;
                dataCollector.VesselInfo.IMONumber = shortVoyageRecord.Vessel.ImoNumber;
                dataCollector.VesselInfo.Flag = shortVoyageRecord.Vessel.Flag;
                dataCollector.VesselInfo.Breadth = shortVoyageRecord.Vessel.Breadth;
            }

            // Route Info - Build ports from route points
            if (shortVoyageRecord.RoutePoints?.Any() == true)
            {
                var portRoutePoints = shortVoyageRecord.RoutePoints
                    .Where(rp => rp.RoutePointType == "Port" && rp.PortData != null)
                    .OrderBy(rp => rp.RoutePointOrder)
                    .ToList();

                foreach (var portPoint in portRoutePoints)
                {
                    dataCollector.RouteInfo.Ports.Add(new PortInfo
                    {
                        Name = portPoint.PortData.PortName,
                        Unlocode = portPoint.PortData.PortCode
                    });
                }
            }

            // Short Voyage Info
            if (shortVoyageRecord.ShortVoyage != null)
            {
                dataCollector.ShortVoyageInfo.DepartureTime = shortVoyageRecord.ShortVoyage.DepartureTime;
                dataCollector.ShortVoyageInfo.ArrivalTime = shortVoyageRecord.ShortVoyage.ArrivalTime;
                dataCollector.ShortVoyageInfo.ForecastTime = shortVoyageRecord.ShortVoyage.ForecastTime;
                dataCollector.ShortVoyageInfo.ForecastSwellHeight = shortVoyageRecord.ShortVoyage.ForecastSwellHeight;
                dataCollector.ShortVoyageInfo.ForecastWindHeight = shortVoyageRecord.ShortVoyage.ForecastWindHeight;
                dataCollector.ShortVoyageInfo.ShortVoyageReductionFactor = shortVoyageRecord.ShortVoyage.ReductionFactor;

                // Calculate WaveHsmax if we have swell and wind heights
                if (shortVoyageRecord.ShortVoyage.ForecastSwellHeight.HasValue &&
                    shortVoyageRecord.ShortVoyage.ForecastWindHeight.HasValue)
                {
                    dataCollector.ShortVoyageInfo.CalculatedWaveHsmax = Math.Sqrt(
                        Math.Pow(shortVoyageRecord.ShortVoyage.ForecastSwellHeight.Value, 2) +
                        Math.Pow(shortVoyageRecord.ShortVoyage.ForecastWindHeight.Value, 2));
                }
            }

            // Attention Block
            if (dataCollector.RouteInfo.Ports.Any())
            {
                dataCollector.AttentionBlock.DeparturePort = dataCollector.RouteInfo.Ports.First().Name;
                dataCollector.AttentionBlock.ArrivalPort = dataCollector.RouteInfo.Ports.Last().Name;
                dataCollector.AttentionBlock.ReductionFactor = dataCollector.ShortVoyageInfo.ShortVoyageReductionFactor ?? 0;
            }

            // Reduction Factor Results - Create a single segment for short voyage
            if (dataCollector.RouteInfo.Ports.Count >= 2)
            {
                var segmentResult = new SegmentReductionFactorResults
                {
                    VoyageLegOrder = 1,
                    DeparturePort = dataCollector.RouteInfo.Ports.First(),
                    ArrivalPort = dataCollector.RouteInfo.Ports.Last(),
                    Distance = shortVoyageRecord.RouteDistance
                };

                // For short voyage, all seasonal factors are the same as the calculated short voyage reduction factor
                var reductionFactor = dataCollector.ShortVoyageInfo.ShortVoyageReductionFactor ?? 0;
                segmentResult.ReductionFactors.Annual = reductionFactor;
                segmentResult.ReductionFactors.Spring = reductionFactor;
                segmentResult.ReductionFactors.Summer = reductionFactor;
                segmentResult.ReductionFactors.Fall = reductionFactor;
                segmentResult.ReductionFactors.Winter = reductionFactor;

                dataCollector.ReductionFactorResults.Add(segmentResult);
            }

            return dataCollector;
        }

        private JsonReportResponse BuildJsonReportFromDataCollector(ReportDataCollector dataCollector, string recordId)
        {
            return new JsonReportResponse
            {
                RecordId = recordId,
                Report = new ReportData
                {
                    Title = dataCollector.ReportTitle ?? string.Empty,
                    DownloadTimestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    Sections = new ReportSections
                    {
                        Attention = new AttentionSection
                        {
                            Salutation = dataCollector.AttentionBlock?.Salutation ?? string.Empty,
                            Body = dataCollector.IsShortVoyageReport
                                ? AttentionBlock.BuildShortVoyageAttentionBody(
                                    dataCollector.AttentionBlock?.DeparturePort ?? string.Empty,
                                    dataCollector.AttentionBlock?.ArrivalPort ?? string.Empty,
                                    dataCollector.AttentionBlock?.ReductionFactor.ToString("0.00") ?? "0.00",
                                    dataCollector.ShortVoyageInfo.DepartureTime,
                                    dataCollector.ShortVoyageInfo.ArrivalTime)
                                : AttentionBlock.BuildAttentionBody(
                                    dataCollector.AttentionBlock?.DeparturePort ?? string.Empty,
                                    dataCollector.AttentionBlock?.ArrivalPort ?? string.Empty,
                                    dataCollector.AttentionBlock?.ReductionFactor.ToString("0.00") ?? "0.00"),
                            AbsContact = dataCollector.AttentionBlock?.ABSContact ?? string.Empty
                        },
                        UserInputs = new UserInputsSection
                        {
                            ReportInfo = new ReportInfoSection
                            {
                                RouteName = dataCollector.ReportInfo?.ReportName ?? string.Empty,
                                ReportDate = dataCollector.ReportInfo?.ReportDate.ToString("yyyy-MM-dd") ?? DateTime.UtcNow.ToString("yyyy-MM-dd")
                            },
                            Vessel = new VesselSection
                            {
                                Imo = dataCollector.VesselInfo?.IMONumber ?? string.Empty,
                                Name = dataCollector.VesselInfo?.VesselName ?? string.Empty,
                                Flag = dataCollector.VesselInfo?.Flag ?? string.Empty
                            },
                            Ports = dataCollector.RouteInfo?.Ports?.Where(p => p != null)
                                .Select(p => $"{p.Name ?? "Unknown"} ({p.Unlocode ?? "N/A"})")
                                .ToList() ?? new List<string>(),
                            // Add short voyage specific data
                            ShortVoyageData = dataCollector.IsShortVoyageReport ? new
                            {
                                DepartureTime = dataCollector.ShortVoyageInfo.DepartureTime.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                                ArrivalTime = dataCollector.ShortVoyageInfo.ArrivalTime.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                                VoyageDuration = dataCollector.ShortVoyageInfo.VoyageDuration.TotalHours,
                                ForecastTime = dataCollector.ShortVoyageInfo.ForecastTime?.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                                ForecastSwellHeight = dataCollector.ShortVoyageInfo.ForecastSwellHeight,
                                ForecastWindHeight = dataCollector.ShortVoyageInfo.ForecastWindHeight,
                                CalculatedWaveHsmax = dataCollector.ShortVoyageInfo.CalculatedWaveHsmax,
                                ShortVoyageReductionFactor = dataCollector.ShortVoyageInfo.ShortVoyageReductionFactor
                            } : null
                        },
                        RouteAnalysis = dataCollector.ReductionFactorResults?.Where(rf => rf != null)
                            .Select(rf => new RouteAnalysisSegment
                            {
                                Segment = new SegmentInfo
                                {
                                    Name = $"{rf.DeparturePort?.Name ?? "Unknown"} ({rf.DeparturePort?.Unlocode ?? "N/A"}) - {rf.ArrivalPort?.Name ?? "Unknown"} ({rf.ArrivalPort?.Unlocode ?? "N/A"})",
                                    Order = rf.VoyageLegOrder,
                                    Distance = rf.Distance,
                                    ReductionFactors = new ReductionFactorsInfo
                                    {
                                        Annual = rf.ReductionFactors?.Annual ?? 0,
                                        Spring = rf.ReductionFactors?.Spring ?? 0,
                                        Summer = rf.ReductionFactors?.Summer ?? 0,
                                        Fall = rf.ReductionFactors?.Fall ?? 0,
                                        Winter = rf.ReductionFactors?.Winter ?? 0
                                    }
                                }
                            }).ToList() ?? new List<RouteAnalysisSegment>(),
                        Notes = dataCollector.IsShortVoyageReport
                            ? dataCollector.Notes?.BuildShortVoyageNotes().Select(note => new ReportNote { VesselCriteria = note }).ToList()
                            : new List<ReportNote>
                            {
                                new ReportNote
                                {
                                    VesselCriteria = dataCollector.Notes?.VesselCriteria ?? string.Empty,
                                    GuideNote = dataCollector.Notes?.BuildGuideNote(dataCollector.Notes.GuideTitle) ?? string.Empty,
                                }
                            }
                    }
                }
            };
        }

        private string GenerateHtmlFromDataCollector(ReportDataCollector dataCollector)
        {
            // Implementation similar to your existing HTML generation but adapted for short voyage
            // This would include the short voyage specific data like forecast times, wave heights, etc.
            // For brevity, I'm providing a simplified version - you can extend this based on your HTML template

            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <title>{dataCollector.ReportTitle}</title>
    <!-- Your existing CSS styles -->
</head>
<body>
    <div class=""report-container"">
        <div class=""header"">
            <h4>{dataCollector.ReportTitle}</h4>
            <p class=""download-timestamp""><strong>Downloaded:</strong> {DateTime.UtcNow:yyyy-MM-ddTHH:mm:ssZ}</p>
        </div>
        
        <!-- Short Voyage Specific Sections -->
        <div class=""report-section"">
            <h5>Short Voyage Details</h5>
            <table class=""report-table"">
                <tr>
                    <td><strong>Departure Time:</strong></td>
                    <td>{dataCollector.ShortVoyageInfo.DepartureTime:yyyy-MM-dd HH:mm}</td>
                </tr>
                <tr>
                    <td><strong>Arrival Time:</strong></td>
                    <td>{dataCollector.ShortVoyageInfo.ArrivalTime:yyyy-MM-dd HH:mm}</td>
                </tr>
                <tr>
                    <td><strong>Voyage Duration:</strong></td>
                    <td>{dataCollector.ShortVoyageInfo.VoyageDuration.TotalHours:F1} hours</td>
                </tr>
                <tr>
                    <td><strong>Short Voyage Reduction Factor:</strong></td>
                    <td class=""reduction-factor"">{dataCollector.ShortVoyageInfo.ShortVoyageReductionFactor:F3}</td>
                </tr>
            </table>
        </div>
        
        <!-- Your existing sections adapted for short voyage -->
        
    </div>
</body>
</html>";
        }
    }
}

// 6. Controller Extension
[ApiController]
[Route("api/short-voyage-records")]
public class ShortVoyageReportsController : ControllerBase
{
    private readonly IShortVoyageReportService _shortVoyageReportService;
    private readonly ILogger<ShortVoyageReportsController> _logger;

    public ShortVoyageReportsController(
        IShortVoyageReportService shortVoyageReportService,
        ILogger<ShortVoyageReportsController> logger)
    {
        _shortVoyageReportService = shortVoyageReportService ?? throw new ArgumentNullException(nameof(shortVoyageReportService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpGet("{record_id}/json_report")]
    public async Task<IActionResult> DownloadJsonReport(string record_id)
    {
        try
        {
            if (!Guid.TryParse(record_id, out Guid recordGuid))
            {
                return BadRequest("Invalid record ID format.");
            }

            var jsonReport = await _shortVoyageReportService.GetShortVoyageJsonReportAsync(recordGuid);
            if (jsonReport == null)
            {
                return NotFound($"Short voyage report not found for record ID: {record_id}");
            }

            var json = System.Text.Json.JsonSerializer.Serialize(jsonReport, new JsonSerializerOptions { WriteIndented = true });
            var bytes = Encoding.UTF8.GetBytes(json);

            return File(bytes, "application/json", $"ShortVoyageReport_{record_id}.json");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while downloading short voyage JSON report for record {RecordId}", record_id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while generating the report.");
        }
    }

    [HttpGet("{record_id}/html_report")]
    public async Task<IActionResult> DownloadHtmlReport(string record_id)
    {
        try
        {
            if (!Guid.TryParse(record_id, out Guid recordGuid))
            {
                return BadRequest("Invalid record ID format.");
            }

            var htmlReport = await _shortVoyageReportService.GetShortVoyageHtmlReportAsync(recordGuid);
            if (htmlReport == null)
            {
                return NotFound($"Short voyage report not found for record ID: {record_id}");
            }

            var bytes = Encoding.UTF8.GetBytes(htmlReport.Html);
            return File(bytes, htmlReport.ContentType, htmlReport.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while downloading short voyage HTML report for record {RecordId}", record_id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while generating the report.");
        }
    }
}

// 7. DI Registration (Add to your Startup.cs or Program.cs)
// services.AddScoped<IShortVoyageReportService, ShortVoyageReportService>();{
public class ShortVoyageReport
{
}
}
