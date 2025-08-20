using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NextGenEngApps.DigitalRules.CRoute.API.ResponseObjects;
using SeaRouteModel.Models;
using SeaRouteModel.Reports;
using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace NextGenEngApps.DigitalRules.CRoute.DAL.Models.Domain.ReductionFactorReport
{
    public class ReportDataCollectorService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ReportDataCollectorService> _logger;

        public ReportDataCollectorService(HttpClient httpClient, ILogger<ReportDataCollectorService> logger, IConfiguration configuration)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            var apiUrl = configuration["ApiUrl"];
            if (!string.IsNullOrEmpty(apiUrl))
            {
                _httpClient.BaseAddress = new Uri(apiUrl);
            }
        }

        public async Task<ReportDataCollector> GetReportDataAsync(string routeVersionId)
        {
            try
            {
                if (string.IsNullOrEmpty(routeVersionId))
                {
                    throw new ArgumentException("Route Version ID is required", nameof(routeVersionId));
                }

                _logger.LogInformation("Fetching report data for Route Version ID: {RouteVersionId}", routeVersionId);

                var response = await _httpClient.GetAsync($"api/route_versions/{routeVersionId}/json_report");

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Failed to fetch report data. Status: {StatusCode}", response.StatusCode);
                    return null;
                }

                var jsonContent = await response.Content.ReadAsStringAsync();
                var jsonReportResponse = JsonSerializer.Deserialize<JsonReportResponse>(jsonContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                // Map JsonReportResponse back to ReportDataCollector
                return MapJsonResponseToDataCollector(jsonReportResponse);
            }
            catch (HttpRequestException httpEx)
            {
                _logger.LogError(httpEx, "HTTP error occurred while fetching report data for Route Version ID: {RouteVersionId}", routeVersionId);
                throw;
            }
            catch (JsonException jsonEx)
            {
                _logger.LogError(jsonEx, "JSON deserialization error for Route Version ID: {RouteVersionId}", routeVersionId);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred while fetching report data for Route Version ID: {RouteVersionId}", routeVersionId);
                throw;
            }
        }

        private ReportDataCollector MapJsonResponseToDataCollector(JsonReportResponse jsonResponse)
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
                    ABSContact = jsonResponse.Report.Sections.Attention.AbsContact
                    // Note: Body is built dynamically, so we'll need departure/arrival ports
                };
            }

            // Map Report Info
            if (jsonResponse.Report.Sections?.UserInputs?.ReportInfo != null)
            {
                dataCollector.ReportInfo = new ReportInfo
                {
                    ReportName = jsonResponse.Report.Sections.UserInputs.ReportInfo.RouteName,
                    ReportDate = DateTime.TryParse(jsonResponse.Report.Sections.UserInputs.ReportInfo.ReportDate, out var reportDate)
                        ? reportDate
                        : DateTime.UtcNow
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
                var ports = new List<Port>();
                foreach (var portString in jsonResponse.Report.Sections.UserInputs.Ports)
                {
                    // Parse "Port Name (UNLOCODE)" format
                    var match = System.Text.RegularExpressions.Regex.Match(portString, @"^(.+?)\s*\(([^)]+)\)$");
                    if (match.Success)
                    {
                        ports.Add(new Port
                        {
                            Name = match.Groups[1].Value.Trim(),
                            Unlocode = match.Groups[2].Value.Trim() == "N/A" ? null : match.Groups[2].Value.Trim()
                        });
                    }
                    else
                    {
                        ports.Add(new Port { Name = portString, Unlocode = null });
                    }
                }
                dataCollector.RouteInfo = new RouteInfo { Ports = ports };
            }

            // Map Route Analysis (Reduction Factor Results)
            if (jsonResponse.Report.Sections?.RouteAnalysis != null)
            {
                var results = new List<SegmentReductionFactorResults>();
                foreach (var segment in jsonResponse.Report.Sections.RouteAnalysis)
                {
                    var result = new SegmentReductionFactorResults
                    {
                        VoyageLegOrder = segment.Segment.Order,
                        Distance = segment.Segment.Distance,
                        ReductionFactors = new SeasonalReductionFactors
                        {
                            Annual = segment.Segment.ReductionFactors.Annual,
                            Spring = segment.Segment.ReductionFactors.Spring,
                            Summer = segment.Segment.ReductionFactors.Summer,
                            Fall = segment.Segment.ReductionFactors.Fall,
                            Winter = segment.Segment.ReductionFactors.Winter
                        }
                    };

                    // Parse departure and arrival ports from segment name
                    var segmentParts = segment.Segment.Name.Split(" - ");
                    if (segmentParts.Length == 2)
                    {
                        result.DeparturePort = ParsePortFromString(segmentParts[0]);
                        result.ArrivalPort = ParsePortFromString(segmentParts[1]);
                    }

                    results.Add(result);
                }
                dataCollector.ReductionFactorResults = results;
            }

            // Map Notes
            if (jsonResponse.Report.Sections?.Notes?.Any() == true)
            {
                var firstNote = jsonResponse.Report.Sections.Notes.First();
                dataCollector.Notes = new ReportNotes
                {
                    VesselCriteria = firstNote.VesselCriteria,
                    GuideTitle = "ABS Container Securing Guide" // Default or extract from GuideNote
                };
            }

            return dataCollector;
        }

        private Port ParsePortFromString(string portString)
        {
            var match = System.Text.RegularExpressions.Regex.Match(portString, @"^(.+?)\s*\(([^)]+)\)$");
            if (match.Success)
            {
                return new Port
                {
                    Name = match.Groups[1].Value.Trim(),
                    Unlocode = match.Groups[2].Value.Trim() == "N/A" ? null : match.Groups[2].Value.Trim()
                };
            }
            return new Port { Name = portString.Trim(), Unlocode = null };
        }
    }
}



======================================================
v2 
==============================================================
using Microsoft.Extensions.Logging;
using NextGenEngApps.DigitalRules.CRoute.DAL.Models.Domain.ReductionFactorReport;
using System.Text.Json;

namespace SeaRouteBlazorServerApp.Components.Services;

public interface IReportDataCollectorService
{
    Task<ReportDataCollector?> GetCompleteReportDataAsync(string routeVersionId);
}

public class ReportDataCollectorService : IReportDataCollectorService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ReportDataCollectorService> _logger;

    public ReportDataCollectorService(HttpClient httpClient, ILogger<ReportDataCollectorService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ReportDataCollector?> GetCompleteReportDataAsync(string routeVersionId)
    {
        try
        {
            if (string.IsNullOrEmpty(routeVersionId))
            {
                _logger.LogWarning("Route Version ID is null or empty");
                return null;
            }

            _logger.LogInformation("Fetching complete report data for Route Version ID: {RouteVersionId}", routeVersionId);

            var response = await _httpClient.GetAsync($"api/route_versions/{routeVersionId}/json_report");
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("API call failed with status code: {StatusCode} for Route Version ID: {RouteVersionId}", 
                    response.StatusCode, routeVersionId);
                return null;
            }

            var jsonContent = await response.Content.ReadAsStringAsync();
            
            if (string.IsNullOrWhiteSpace(jsonContent))
            {
                _logger.LogWarning("Empty response received for Route Version ID: {RouteVersionId}", routeVersionId);
                return null;
            }

            // Deserialize the JsonReportResponse
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                AllowTrailingCommas = true
            };

            var jsonReportResponse = JsonSerializer.Deserialize<JsonReportResponse>(jsonContent, jsonOptions);
            
            if (jsonReportResponse?.Report == null)
            {
                _logger.LogWarning("Invalid or null report data received for Route Version ID: {RouteVersionId}", routeVersionId);
                return null;
            }

            // Map JsonReportResponse to ReportDataCollector
            var reportDataCollector = MapToReportDataCollector(jsonReportResponse);
            
            _logger.LogInformation("Successfully fetched and mapped report data for Route Version ID: {RouteVersionId}", routeVersionId);
            
            return reportDataCollector;
        }
        catch (JsonException jsonEx)
        {
            _logger.LogError(jsonEx, "JSON deserialization error for Route Version ID: {RouteVersionId}", routeVersionId);
            return null;
        }
        catch (HttpRequestException httpEx)
        {
            _logger.LogError(httpEx, "HTTP request error for Route Version ID: {RouteVersionId}", routeVersionId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error fetching report data for Route Version ID: {RouteVersionId}", routeVersionId);
            return null;
        }
    }

    private ReportDataCollector MapToReportDataCollector(JsonReportResponse jsonResponse)
    {
        var collector = new ReportDataCollector
        {
            ReportTitle = jsonResponse.Report.Title,
            RecordId = jsonResponse.RecordId
        };

        // Map Attention Block
        if (jsonResponse.Report.Sections?.Attention != null)
        {
            collector.AttentionBlock = new AttentionBlock
            {
                Salutation = jsonResponse.Report.Sections.Attention.Salutation,
                ABSContact = jsonResponse.Report.Sections.Attention.AbsContact
            };
            
            // Parse body to extract departure/arrival ports if needed
            // This is a simplified mapping - you may need to enhance based on your AttentionBlock.BuildAttentionBody logic
        }

        // Map Report Info
        if (jsonResponse.Report.Sections?.UserInputs?.ReportInfo != null)
        {
            collector.ReportInfo = new ReportInfo
            {
                ReportName = jsonResponse.Report.Sections.UserInputs.ReportInfo.RouteName,
                ReportDate = DateTime.TryParse(jsonResponse.Report.Sections.UserInputs.ReportInfo.ReportDate, out var reportDate) 
                    ? reportDate 
                    : DateTime.UtcNow
            };
        }

        // Map Vessel Info
        if (jsonResponse.Report.Sections?.UserInputs?.Vessel != null)
        {
            collector.VesselInfo = new VesselInfo
            {
                IMONumber = jsonResponse.Report.Sections.UserInputs.Vessel.Imo,
                VesselName = jsonResponse.Report.Sections.UserInputs.Vessel.Name,
                Flag = jsonResponse.Report.Sections.UserInputs.Vessel.Flag
            };
        }

        // Map Route Info
        if (jsonResponse.Report.Sections?.UserInputs?.Ports != null)
        {
            collector.RouteInfo = new RouteInfo();
            
            foreach (var portString in jsonResponse.Report.Sections.UserInputs.Ports)
            {
                // Parse port string format: "Port Name (UNLOCODE)"
                var port = ParsePortString(portString);
                collector.RouteInfo.Ports.Add(port);
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
                    Distance = segment.Segment.Distance,
                    DeparturePort = ExtractPortFromSegmentName(segment.Segment.Name, true),
                    ArrivalPort = ExtractPortFromSegmentName(segment.Segment.Name, false),
                    ReductionFactors = new SeasonalReductionFactors
                    {
                        Annual = segment.Segment.ReductionFactors.Annual,
                        Spring = segment.Segment.ReductionFactors.Spring,
                        Summer = segment.Segment.ReductionFactors.Summer,
                        Fall = segment.Segment.ReductionFactors.Fall,
                        Winter = segment.Segment.ReductionFactors.Winter
                    }
                };
                
                collector.ReductionFactorResults.Add(result);
            }
        }

        // Map Notes
        if (jsonResponse.Report.Sections?.Notes?.Count > 0)
        {
            var note = jsonResponse.Report.Sections.Notes.First();
            collector.Notes = new ReportNotes
            {
                VesselCriteria = note.VesselCriteria,
                GuideTitle = ExtractGuideTitleFromGuideNote(note.GuideNote)
            };
        }

        return collector;
    }

    private Port ParsePortString(string portString)
    {
        // Expected format: "Port Name (UNLOCODE)"
        var parts = portString.Split('(', ')');
        
        if (parts.Length >= 2)
        {
            return new Port
            {
                Name = parts[0].Trim(),
                Unlocode = parts[1].Trim()
            };
        }
        
        return new Port
        {
            Name = portString,
            Unlocode = "N/A"
        };
    }

    private Port ExtractPortFromSegmentName(string segmentName, bool isDeparture)
    {
        // Expected format: "Port1 (UNLOCODE1) - Port2 (UNLOCODE2)"
        var parts = segmentName.Split(" - ");
        
        string portString;
        if (isDeparture && parts.Length > 0)
        {
            portString = parts[0].Trim();
        }
        else if (!isDeparture && parts.Length > 1)
        {
            portString = parts[1].Trim();
        }
        else
        {
            return new Port { Name = "Unknown", Unlocode = "N/A" };
        }
        
        return ParsePortString(portString);
    }

    private string ExtractGuideTitleFromGuideNote(string guideNote)
    {
        // Extract guide title from guide note - implement based on your logic
        // This is a placeholder implementation
        if (guideNote.Contains("ABS Container Securing Guide"))
        {
            return "ABS Container Securing Guide";
        }
        
        return "ABS Guide";
    }
}

// Supporting classes for JSON deserialization
public class JsonReportResponse
{
    public string RouteVersionId { get; set; } = string.Empty;
    public string RecordId { get; set; } = string.Empty;
    public ReportData Report { get; set; } = new();
}

public class ReportData
{
    public string Title { get; set; } = string.Empty;
    public string DownloadTimestamp { get; set; } = string.Empty;
    public ReportSections Sections { get; set; } = new();
}

public class ReportSections
{
    public AttentionSection Attention { get; set; } = new();
    public UserInputsSection UserInputs { get; set; } = new();
    public List<RouteAnalysisSegment> RouteAnalysis { get; set; } = new();
    public List<ReportNote> Notes { get; set; } = new();
}

public class AttentionSection
{
    public string Salutation { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string AbsContact { get; set; } = string.Empty;
}

public class UserInputsSection
{
    public ReportInfoSection ReportInfo { get; set; } = new();
    public VesselSection Vessel { get; set; } = new();
    public List<string> Ports { get; set; } = new();
}

public class ReportInfoSection
{
    public string RouteName { get; set; } = string.Empty;
    public string ReportDate { get; set; } = string.Empty;
}

public class VesselSection
{
    public string Imo { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Flag { get; set; } = string.Empty;
}

public class RouteAnalysisSegment
{
    public SegmentInfo Segment { get; set; } = new();
}

public class SegmentInfo
{
    public string Name { get; set; } = string.Empty;
    public int Order { get; set; }
    public double Distance { get; set; }
    public ReductionFactorsInfo ReductionFactors { get; set; } = new();
}

public class ReductionFactorsInfo
{
    public double Annual { get; set; }
    public double Spring { get; set; }
    public double Summer { get; set; }
    public double Fall { get; set; }
    public double Winter { get; set; }
}

public class ReportNote
{
    public string VesselCriteria { get; set; } = string.Empty;
    public string GuideNote { get; set; } = string.Empty;
}