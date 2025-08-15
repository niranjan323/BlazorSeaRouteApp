using Microsoft.Extensions.Logging;
using SeaRouteModel.Models;
using SeaRouteModel.Reports;
using System.Text.Json;

namespace NextGenEngApps.DigitalRules.CRoute.Models.ReductionFactorReport
{
    public class ReportDataCollector
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ReportDataCollector> _logger;

        public string ReportTitle { get; set; } = string.Empty;
        public AttentionBlock AttentionBlock { get; set; } = new AttentionBlock();
        public ReportInfo ReportInfo { get; set; } = new ReportInfo();
        public VesselInfo VesselInfo { get; set; } = new VesselInfo();
        public RouteInfo RouteInfo { get; set; } = new RouteInfo();
        public List<VoyageLegReductionFactor> ReductionFactorResults { get; set; } = new();
        public ReportNotes Notes { get; set; } = new ReportNotes();
        public ReportDataCollector()
        {
        }

        public ReportDataCollector(HttpClient httpClient, ILogger<ReportDataCollector> logger, IConfiguration configuration)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            var apiUrl = configuration["ApiUrl"];
            if (!string.IsNullOrEmpty(apiUrl))
            {
                _httpClient.BaseAddress = new Uri(apiUrl);
            }
        }

        public async Task<bool> LoadReportDataAsync(string routeVersionId)
        {
            try
            {
                if (string.IsNullOrEmpty(routeVersionId))
                {
                    _logger.LogError("RouteVersionId cannot be null or empty");
                    return false;
                }

                var response = await _httpClient.GetAsync($"api/route_versions/{routeVersionId}/json_report");

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("API call failed with status code: {StatusCode}", response.StatusCode);
                    return false;
                }

                var jsonContent = await response.Content.ReadAsStringAsync();

                if (string.IsNullOrEmpty(jsonContent))
                {
                    _logger.LogError("Empty response received from API");
                    return false;
                }

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var apiResponse = JsonSerializer.Deserialize<CompleteReportApiResponse>(jsonContent, options);

                if (apiResponse?.Report == null)
                {
                    _logger.LogError("Failed to deserialize API response or report data is null");
                    return false;
                }

                await BindApiResponseToCollector(apiResponse);

                _logger.LogInformation("Report data loaded successfully for RouteVersionId: {RouteVersionId}", routeVersionId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while loading report data for RouteVersionId: {RouteVersionId}", routeVersionId);
                return false;
            }
        }

        private async Task BindApiResponseToCollector(CompleteReportApiResponse apiResponse)
        {
            try
            {
                var report = apiResponse.Report;
                var sections = report.Sections;

                ReportTitle = report.Title;

                // Bind AttentionBlock
                if (sections.Attention != null)
                {
                    AttentionBlock.Salutation = sections.Attention.Salutation ?? string.Empty;
                    AttentionBlock.ABSContact = sections.Attention.AbsContact ?? string.Empty;

                    ParseAttentionBody(sections.Attention.Body);
                }

                // Bind ReportInfo
                if (sections.UserInputs?.ReportInfo != null)
                {
                    ReportInfo.ReportName = sections.UserInputs.ReportInfo.RouteName ?? string.Empty;

                    if (DateTime.TryParse(sections.UserInputs.ReportInfo.ReportDate, out DateTime reportDate))
                    {
                        ReportInfo.ReportDate = DateOnly.FromDateTime(reportDate);
                    }
                }

                // Bind VesselInfo
                if (sections.UserInputs?.Vessel != null)
                {
                    VesselInfo = new VesselInfo
                    {
                        VesselName = sections.UserInputs.Vessel.Name ?? string.Empty,
                        IMONumber = sections.UserInputs.Vessel.Imo ?? string.Empty,
                        Flag = sections.UserInputs.Vessel.Flag ?? string.Empty,
                        Breadth = 0 // This field is not in the API response, keeping default
                    };
                }

                // Bind RouteInfo (Ports)
                if (sections.UserInputs?.Ports?.Any() == true)
                {
                    RouteInfo.Ports = sections.UserInputs.Ports.Select(portString =>
                    {
                        var parts = portString.Split(new[] { " (", ")" }, StringSplitOptions.RemoveEmptyEntries);
                        return new PortInfo
                        {
                            Name = parts.Length > 0 ? parts[0].Trim() : string.Empty,
                            Unlocode = parts.Length > 1 ? parts[1].Trim() : string.Empty
                        };
                    }).ToList();
                }

                // Bind ReductionFactorResults (RouteAnalysis)
                if (sections.RouteAnalysis?.Any() == true)
                {
                    ReductionFactorResults = sections.RouteAnalysis.Select(segment =>
                    {
                        var segmentInfo = segment.Segment;
                        var nameParts = segmentInfo.Name.Split(new[] { " - " }, StringSplitOptions.None);

                        return new VoyageLegReductionFactor
                        {
                            LegOrder = segmentInfo.Order,
                            DeparturePort = ParsePortFromSegmentName(nameParts.Length > 0 ? nameParts[0] : string.Empty),
                            ArrivalPort = ParsePortFromSegmentName(nameParts.Length > 1 ? nameParts[1] : string.Empty),
                            Distance = segmentInfo.Distance,
                            ReductionFactors = new ReductionFactors
                            {
                                Annual = segmentInfo.ReductionFactors.Annual,
                                Spring = segmentInfo.ReductionFactors.Spring,
                                Summer = segmentInfo.ReductionFactors.Summer,
                                Fall = segmentInfo.ReductionFactors.Fall,
                                Winter = segmentInfo.ReductionFactors.Winter
                            }
                        };
                    }).ToList();
                }

                // Bind Notes
                if (sections.Notes?.Any() == true)
                {
                    Notes = new ReportNotes(); // Assuming ReportNotes has properties to hold the notes
                    // You may need to adjust this based on your ReportNotes structure
                }

                _logger.LogInformation("API response successfully bound to ReportDataCollector");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while binding API response to ReportDataCollector");
                throw;
            }
        }

        private PortInfo ParsePortFromSegmentName(string portSegment)
        {
            if (string.IsNullOrEmpty(portSegment))
                return new PortInfo();

            var parts = portSegment.Split(new[] { " (", ")" }, StringSplitOptions.RemoveEmptyEntries);
            return new PortInfo
            {
                Name = parts.Length > 0 ? parts[0].Trim() : string.Empty,
                Unlocode = parts.Length > 1 ? parts[1].Trim() : string.Empty
            };
        }

        private void ParseAttentionBody(string? attentionBody)
        {
            if (string.IsNullOrEmpty(attentionBody))
                return;

            try
            {
                var fromIndex = attentionBody.IndexOf("from ", StringComparison.OrdinalIgnoreCase);
                var toIndex = attentionBody.IndexOf(" to ", StringComparison.OrdinalIgnoreCase);
                var isIndex = attentionBody.IndexOf(" is ", StringComparison.OrdinalIgnoreCase);

                if (fromIndex > -1 && toIndex > fromIndex && isIndex > toIndex)
                {
                    var departureStart = fromIndex + 5;
                    AttentionBlock.DeparturePort = attentionBody.Substring(departureStart, toIndex - departureStart).Trim();

                    var arrivalStart = toIndex + 4; 
                    AttentionBlock.ArrivalPort = attentionBody.Substring(arrivalStart, isIndex - arrivalStart).Trim();

                   
                    var rfStart = isIndex + 4;
                    var rfEnd = attentionBody.IndexOf(".", rfStart);
                    if (rfEnd > rfStart)
                    {
                        var rfString = attentionBody.Substring(rfStart, rfEnd - rfStart).Trim();
                        if (double.TryParse(rfString, out double reductionFactor))
                        {
                            AttentionBlock.ReductionFactor = reductionFactor;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse attention body: {AttentionBody}", attentionBody);
            }
        }
    }

    // Supporting classes for API response deserialization
    public class CompleteReportApiResponse
    {
        public string RouteVersionId { get; set; } = string.Empty;
        public string RecordId { get; set; } = string.Empty;
        public ApiReportData Report { get; set; } = new();
    }

    public class ApiReportData
    {
        public string Title { get; set; } = string.Empty;
        public string DownloadTimestamp { get; set; } = string.Empty;
        public ApiReportSections Sections { get; set; } = new();
    }

    public class ApiReportSections
    {
        public ApiAttentionSection Attention { get; set; } = new();
        public ApiUserInputsSection UserInputs { get; set; } = new();
        public List<ApiRouteAnalysisSegment> RouteAnalysis { get; set; } = new();
        public List<ApiReportNote> Notes { get; set; } = new();
    }

    public class ApiAttentionSection
    {
        public string Salutation { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public string AbsContact { get; set; } = string.Empty;
    }

    public class ApiUserInputsSection
    {
        public ApiReportInfoSection ReportInfo { get; set; } = new();
        public ApiVesselSection Vessel { get; set; } = new();
        public List<string> Ports { get; set; } = new();
    }

    public class ApiReportInfoSection
    {
        public string RouteName { get; set; } = string.Empty;
        public string ReportDate { get; set; } = string.Empty;
    }

    public class ApiVesselSection
    {
        public string Imo { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Flag { get; set; } = string.Empty;
    }

    public class ApiRouteAnalysisSegment
    {
        public ApiSegmentInfo Segment { get; set; } = new();
    }

    public class ApiSegmentInfo
    {
        public string Name { get; set; } = string.Empty;
        public int Order { get; set; }
        public double Distance { get; set; }
        public ApiReductionFactorsInfo ReductionFactors { get; set; } = new();
    }

    public class ApiReductionFactorsInfo
    {
        public double Annual { get; set; }
        public double Spring { get; set; }
        public double Summer { get; set; }
        public double Fall { get; set; }
        public double Winter { get; set; }
    }

    public class ApiReportNote
    {
        public string? VesselCriteria { get; set; }
        public string? GuideNote { get; set; }
    }

    // Assuming ReportNotes class structure - adjust as needed
    public class ReportNotes
    {
        public string VesselCriteria { get; set; } = string.Empty;
        public string GuideNote { get; set; } = string.Empty;
    }
}

// Usage in Blazor component:
/*
@inject HttpClient HttpClient
@inject ILogger<ReportDataCollector> Logger

@code {
    private ReportDataCollector reportCollector;
    private string routeVersionId = "95E43807-00A3-4E9B-9EA5-3DB7482A5846";

    protected override async Task OnInitializedAsync()
    {
        reportCollector = new ReportDataCollector(HttpClient, Logger);
        var success = await reportCollector.LoadReportDataAsync(routeVersionId);
        
        if (success)
        {
            // Data is now loaded and bound to reportCollector
            StateHasChanged();
        }
    }
}
*/