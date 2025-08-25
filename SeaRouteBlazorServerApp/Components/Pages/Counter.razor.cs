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
            string html = $@"
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
        .short-voyage-table {{
            width: 100%;
            margin-top: 20px;
        }}
        .short-voyage-table th {{
            background-color: #0066cc;
            color: white;
            text-align: center;
            font-size: 14px;
            padding: 12px;
        }}
        .short-voyage-table td {{
            text-align: center;
            font-weight: 500;
            padding: 10px;
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
        .short-voyage-details {{
            background-color: #e3f2fd;
            padding: 20px;
            border-radius: 5px;
            margin-bottom: 25px;
        }}
        .detail-grid {{
            display: grid;
            grid-template-columns: 1fr 1fr;
            gap: 15px;
            margin-top: 15px;
        }}
        .detail-item {{
            padding: 10px;
            background-color: white;
            border-radius: 4px;
            border-left: 4px solid #0066cc;
        }}
        .detail-label {{
            font-size: 12px;
            color: #666;
            text-transform: uppercase;
            letter-spacing: 0.5px;
        }}
        .detail-value {{
            font-size: 16px;
            font-weight: 600;
            color: #333;
            margin-top: 5px;
        }}
        .calculation-section {{
            background-color: #f8f9fa;
            padding: 20px;
            border-radius: 5px;
            margin-bottom: 25px;
        }}
        .formula-display {{
            font-family: 'Courier New', monospace;
            background-color: white;
            padding: 15px;
            border-radius: 4px;
            border-left: 4px solid #28a745;
            margin: 10px 0;
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

            // Attention Section
            if (!string.IsNullOrEmpty(dataCollector.AttentionBlock?.Salutation) ||
                !string.IsNullOrEmpty(dataCollector.AttentionBlock?.DeparturePort))
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
                    {AttentionBlock.BuildShortVoyageAttentionBody(
                        dataCollector.AttentionBlock.DeparturePort,
                        dataCollector.AttentionBlock.ArrivalPort,
                        dataCollector.AttentionBlock.ReductionFactor.ToString("F3"),
                        dataCollector.ShortVoyageInfo.DepartureTime,
                        dataCollector.ShortVoyageInfo.ArrivalTime)}
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

            // User Inputs Section
            html += @"
        <!-- User Inputs Section -->
        <div class=""report-section"">
            <h5>User Inputs</h5>
            <table class=""report-table"">";

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

            if (dataCollector.VesselInfo?.Breadth > 0)
            {
                html += $@"
                <tr>
                    <td><strong>Vessel Breadth:</strong></td>
                    <td class=""highlight"">{dataCollector.VesselInfo.Breadth:F2} m</td>
                    <td></td>
                </tr>";
            }

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

            // Short Voyage Details Section
            html += @"
        <!-- Short Voyage Details Section -->
        <div class=""report-section"">
            <h5>Short Voyage Details</h5>
            <div class=""short-voyage-details"">
                <div class=""detail-grid"">";

            html += $@"
                    <div class=""detail-item"">
                        <div class=""detail-label"">Departure Time</div>
                        <div class=""detail-value"">{dataCollector.ShortVoyageInfo.DepartureTime:yyyy-MM-dd HH:mm} UTC</div>
                    </div>
                    <div class=""detail-item"">
                        <div class=""detail-label"">Arrival Time</div>
                        <div class=""detail-value"">{dataCollector.ShortVoyageInfo.ArrivalTime:yyyy-MM-dd HH:mm} UTC</div>
                    </div>
                    <div class=""detail-item"">
                        <div class=""detail-label"">Voyage Duration</div>
                        <div class=""detail-value"">{dataCollector.ShortVoyageInfo.VoyageDuration.TotalHours:F1} hours</div>
                    </div>";

            if (dataCollector.ShortVoyageInfo.ForecastTime.HasValue)
            {
                html += $@"
                    <div class=""detail-item"">
                        <div class=""detail-label"">Forecast Time</div>
                        <div class=""detail-value"">{dataCollector.ShortVoyageInfo.ForecastTime.Value:yyyy-MM-dd HH:mm} UTC</div>
                    </div>";
            }

            if (dataCollector.ShortVoyageInfo.ForecastSwellHeight.HasValue)
            {
                html += $@"
                    <div class=""detail-item"">
                        <div class=""detail-label"">Forecast Swell Height (Hs)</div>
                        <div class=""detail-value"">{dataCollector.ShortVoyageInfo.ForecastSwellHeight.Value:F2} m</div>
                    </div>";
            }

            if (dataCollector.ShortVoyageInfo.ForecastWindHeight.HasValue)
            {
                html += $@"
                    <div class=""detail-item"">
                        <div class=""detail-label"">Forecast Wind Height (Hw)</div>
                        <div class=""detail-value"">{dataCollector.ShortVoyageInfo.ForecastWindHeight.Value:F2} m</div>
                    </div>";
            }

            html += @"
                </div>
            </div>
        </div>";

            // Calculation Section
            if (dataCollector.ShortVoyageInfo.ForecastSwellHeight.HasValue &&
                dataCollector.ShortVoyageInfo.ForecastWindHeight.HasValue)
            {
                html += @"
        <!-- Calculation Section -->
        <div class=""report-section"">
            <h5>Short Voyage Reduction Factor Calculation</h5>
            <div class=""calculation-section"">";

                var hswell = dataCollector.ShortVoyageInfo.ForecastSwellHeight.Value;
                var hwind = dataCollector.ShortVoyageInfo.ForecastWindHeight.Value;
                var breadth = dataCollector.VesselInfo.Breadth;
                var waveHsmax = dataCollector.ShortVoyageInfo.CalculatedWaveHsmax ?? 0;
                var reductionFactor = dataCollector.ShortVoyageInfo.ShortVoyageReductionFactor ?? 0;

                html += $@"
                <p><strong>Step 1:</strong> Calculate Maximum Significant Wave Height (Hs,max)</p>
                <div class=""formula-display"">
                    Hs,max = √(Hs² + Hw²)<br/>
                    Hs,max = √({hswell:F2}² + {hwind:F2}²)<br/>
                    Hs,max = √({hswell * hswell:F2} + {hwind * hwind:F2})<br/>
                    <strong>Hs,max = {waveHsmax:F2} m</strong>
                </div>

                <p><strong>Step 2:</strong> Calculate Short Voyage Reduction Factor</p>
                <div class=""formula-display"">
                    RF = max(min(Hs,max / (2 × √B) + 0.4, 1.0), 0.6)<br/>
                    RF = max(min({waveHsmax:F2} / (2 × √{breadth:F2}) + 0.4, 1.0), 0.6)<br/>
                    RF = max(min({waveHsmax:F2} / {2 * Math.Sqrt(breadth):F2} + 0.4, 1.0), 0.6)<br/>
                    RF = max(min({waveHsmax / (2 * Math.Sqrt(breadth)) + 0.4:F3}, 1.0), 0.6)<br/>
                    <strong>RF = {reductionFactor:F3}</strong>
                </div>

                <p><em>Where: B = Vessel Breadth ({breadth:F2} m)</em></p>";

                html += @"
            </div>
        </div>";
            }

            // Output Section
            html += @"
        <!-- Output Section -->
        <div class=""report-section"">
            <h5>Output</h5>
            <table class=""short-voyage-table report-table"">
                <thead>
                    <tr>
                        <th>Route</th>
                        <th>Distance (nm)</th>
                        <th>Short Voyage Reduction Factor</th>
                        <th>Voyage Duration (hours)</th>
                    </tr>
                </thead>
                <tbody>";

            if (dataCollector.ReductionFactorResults?.Any() == true)
            {
                var result = dataCollector.ReductionFactorResults.First();
                html += $@"
                    <tr>
                        <td>{result.DeparturePort?.Name ?? ""} - {result.ArrivalPort?.Name ?? ""}</td>
                        <td>{Math.Round(result.Distance)} nm</td>
                        <td class=""reduction-factor"">{dataCollector.ShortVoyageInfo.ShortVoyageReductionFactor:F3}</td>
                        <td>{dataCollector.ShortVoyageInfo.VoyageDuration.TotalHours:F1}</td>
                    </tr>";
            }

            html += @"
                </tbody>
            </table>
        </div>";

            // Notes Section
            if (dataCollector.Notes != null)
            {
                var shortVoyageNotes = dataCollector.Notes.BuildShortVoyageNotes();

                html += @"
        <!-- Notes Section -->
        <div class=""report-section notes-section"">
            <h5>Notes</h5>
            <ul>";

                foreach (var note in shortVoyageNotes)
                {
                    html += $@"<li>{note}</li>";
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
    }
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
