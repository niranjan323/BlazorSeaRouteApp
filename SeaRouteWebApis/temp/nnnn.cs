using Microsoft.Extensions.Logging;
using NextGenEngApps.DigitalRules.CRoute.Models;
using NextGenEngApps.DigitalRules.CRoute.Models.ReductionFactorReport;
using NextGenEngApps.DigitalRules.CRoute.Services.API.Response;
using SeaRouteModel.Models;
using SeaRouteModel.Reports;
using System.Reflection;

namespace NextGenEngApps.DigitalRules.CRoute.Services.API
{
    public interface IReportApiService
    {
        Task<ReportDataCollector> LoadAllAsync(string recordId, string routeVersionId);
    }

    public class ReportApiService : IReportApiService
    {
        private readonly CRouteAPIClient _apiClient;
        private readonly ILogger<ReportApiService> _logger;

        public ReportApiService(CRouteAPIClient apiClient, ILogger<ReportApiService> logger)
        {
            _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ReportDataCollector> LoadAllAsync(string recordId, string routeVersionId)
        {
            var reportDataCollector = new ReportDataCollector();

            try
            {
                _logger.LogInformation("Starting data collection for RecordId: {RecordId}, RouteVersionId: {RouteVersionId}",
                    recordId, routeVersionId);

                // Collect all data concurrently for better performance
                var tasks = new[]
                {
                    LoadRecordDetailsAsync(recordId),
                    LoadRouteVersionDetailsAsync(routeVersionId),
                    LoadActiveVesselAsync(recordId),
                    LoadVoyageLegsAsync(routeVersionId),
                    LoadVoyageLegReductionFactorsAsync(routeVersionId),
                    LoadRecordReductionFactorsAsync(recordId)
                };

                var results = await Task.WhenAll(tasks);

                var recordDetails = results[0] as RecordDetailsResponse;
                var routeVersionDetails = results[1] as RouteVersionDetailsResponse;
                var activeVessel = results[2] as ActiveVesselResponse;
                var voyageLegs = results[3] as RouteVersionLegs;
                var voyageLegReductionFactors = results[4] as RouteVersionReductionFactors;
                var recordReductionFactors = results[5] as RecordReductionFactorsResponse;

                // Populate ReportDataCollector
                PopulateReportInfo(reportDataCollector, recordDetails, routeVersionDetails);
                PopulateVesselInfo(reportDataCollector, activeVessel);
                PopulateRouteInfo(reportDataCollector, voyageLegs);
                PopulateReductionFactorResults(reportDataCollector, voyageLegs, voyageLegReductionFactors);
                PopulateAttentionBlock(reportDataCollector, voyageLegs, recordReductionFactors);

                _logger.LogInformation("Successfully collected all data for report");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while loading report data for RecordId: {RecordId}, RouteVersionId: {RouteVersionId}",
                    recordId, routeVersionId);
                throw;
            }

            return reportDataCollector;
        }

        private async Task<object?> LoadRecordDetailsAsync(string recordId)
        {
            try
            {
                var response = await _apiClient.GetAsync($"api/v1/reports/records/{recordId}");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<RecordDetailsResponse>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load record details for RecordId: {RecordId}", recordId);
                return null;
            }
        }

        private async Task<object?> LoadRouteVersionDetailsAsync(string routeVersionId)
        {
            try
            {
                var response = await _apiClient.GetAsync($"api/route_versions/{routeVersionId}");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<RouteVersionDetailsResponse>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load route version details for RouteVersionId: {RouteVersionId}", routeVersionId);
                return null;
            }
        }

        private async Task<object?> LoadActiveVesselAsync(string recordId)
        {
            try
            {
                var response = await _apiClient.GetAsync($"api/v1/reports/records/{recordId}/active_vessel");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<ActiveVesselResponse>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load active vessel for RecordId: {RecordId}", recordId);
                return null;
            }
        }

        private async Task<object?> LoadVoyageLegsAsync(string routeVersionId)
        {
            try
            {
                var response = await _apiClient.GetAsync($"api/route_versions/{routeVersionId}/voyage_legs");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<RouteVersionLegs>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load voyage legs for RouteVersionId: {RouteVersionId}", routeVersionId);
                return null;
            }
        }

        private async Task<object?> LoadVoyageLegReductionFactorsAsync(string routeVersionId)
        {
            try
            {
                var response = await _apiClient.GetAsync($"api/route_versions/{routeVersionId}/voyage_leg_reduction_factors");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<RouteVersionReductionFactors>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load voyage leg reduction factors for RouteVersionId: {RouteVersionId}", routeVersionId);
                return null;
            }
        }

        private async Task<object?> LoadRecordReductionFactorsAsync(string recordId)
        {
            try
            {
                var response = await _apiClient.GetAsync($"api/v1/reports/record_reduction_factors/{recordId}");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<RecordReductionFactorsResponse>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load record reduction factors for RecordId: {RecordId}", recordId);
                return null;
            }
        }

        private void PopulateReportInfo(ReportDataCollector reportDataCollector,
            RecordDetailsResponse? recordDetails, RouteVersionDetailsResponse? routeVersionDetails)
        {
            reportDataCollector.ReportTitle = "Reduction Factor Report";
            reportDataCollector.ReportInfo.ReportName = recordDetails?.RouteName ?? "Unknown Route";

            if (routeVersionDetails?.RecordDate != null &&
                DateOnly.TryParse(routeVersionDetails.RecordDate, out var recordDate))
            {
                reportDataCollector.ReportInfo.ReportDate = recordDate;
            }
        }

        private void PopulateVesselInfo(ReportDataCollector reportDataCollector, ActiveVesselResponse? activeVessel)
        {
            if (activeVessel?.Vessel != null)
            {
                reportDataCollector.VesselInfo = new VesselInfo
                {
                    VesselName = activeVessel.Vessel.Name,
                    IMONumber = activeVessel.Vessel.Imo,
                    Flag = activeVessel.Vessel.Flag,
                    Breadth = activeVessel.Vessel.Breadth
                };
            }
        }

        private void PopulateRouteInfo(ReportDataCollector reportDataCollector, RouteVersionLegs? voyageLegs)
        {
            if (voyageLegs?.VoyageLegs != null && voyageLegs.VoyageLegs.Any())
            {
                var uniquePorts = new HashSet<(string Name, string Code)>();

                foreach (var leg in voyageLegs.VoyageLegs.OrderBy(x => x.VoyageLegOrder))
                {
                    uniquePorts.Add((leg.DeparturePortName, leg.DeparturePortCode));
                    uniquePorts.Add((leg.ArrivalPortName, leg.ArrivalPortCode));
                }

                reportDataCollector.RouteInfo.Ports = uniquePorts
                    .Select(p => new PortInfo { Name = p.Name, Unlocode = p.Code })
                    .ToList();
            }
        }

        private void PopulateReductionFactorResults(ReportDataCollector reportDataCollector,
            RouteVersionLegs? voyageLegs, RouteVersionReductionFactors? reductionFactors)
        {
            reportDataCollector.ReductionFactorResults = new List<VoyageLegReductionFactor>();

            if (voyageLegs?.VoyageLegs == null || reductionFactors?.VoyageLegReductionFactors == null)
                return;

            var orderedLegs = voyageLegs.VoyageLegs.OrderBy(x => x.VoyageLegOrder).ToList();
            var orderedFactors = reductionFactors.VoyageLegReductionFactors.OrderBy(x => x.VoyageLegOrder).ToList();

            for (int i = 0; i < Math.Min(orderedLegs.Count, orderedFactors.Count); i++)
            {
                var leg = orderedLegs[i];
                var factors = orderedFactors[i];

                reportDataCollector.ReductionFactorResults.Add(new VoyageLegReductionFactor
                {
                    LegOrder = leg.VoyageLegOrder,
                    DeparturePort = new PortInfo
                    {
                        Name = leg.DeparturePortName,
                        Unlocode = leg.DeparturePortCode
                    },
                    ArrivalPort = new PortInfo
                    {
                        Name = leg.ArrivalPortName,
                        Unlocode = leg.ArrivalPortCode
                    },
                    Distance = leg.Distance,
                    ReductionFactors = new Models.ReductionFactorReport.ReductionFactors
                    {
                        Annual = factors.ReductionFactors.Annual,
                        Spring = factors.ReductionFactors.Spring,
                        Summer = factors.ReductionFactors.Summer,
                        Fall = factors.ReductionFactors.Fall,
                        Winter = factors.ReductionFactors.Winter
                    }
                });
            }
        }

        private void PopulateAttentionBlock(ReportDataCollector reportDataCollector,
            RouteVersionLegs? voyageLegs, RecordReductionFactorsResponse? recordReductionFactors)
        {
            if (voyageLegs?.VoyageLegs != null && voyageLegs.VoyageLegs.Any())
            {
                var firstLeg = voyageLegs.VoyageLegs.OrderBy(x => x.VoyageLegOrder).First();
                var lastLeg = voyageLegs.VoyageLegs.OrderByDescending(x => x.VoyageLegOrder).First();

                reportDataCollector.AttentionBlock.DeparturePort = firstLeg.DeparturePortName;
                reportDataCollector.AttentionBlock.ArrivalPort = lastLeg.ArrivalPortName;
            }

            if (recordReductionFactors?.ReductionFactors != null)
            {
                // Use Annual reduction factor as the main factor for attention block
                reportDataCollector.AttentionBlock.ReductionFactor = recordReductionFactors.ReductionFactors.Annual;
            }
        }
    }

    // Response models (add these if they don't exist)
    public class RecordDetailsResponse
    {
        public string RecordId { get; set; } = string.Empty;
        public string RouteName { get; set; } = string.Empty;
        public double RouteDistance { get; set; }
    }

    public class RouteVersionDetailsResponse
    {
        public string RouteVersionId { get; set; } = string.Empty;
        public string RecordId { get; set; } = string.Empty;
        public string RecordDate { get; set; } = string.Empty;
    }

    public class RouteVersionLegs
    {
        public string RouteVersionId { get; set; } = string.Empty;
        public List<VoyageLeg> VoyageLegs { get; set; } = new List<VoyageLeg>();
    }

    public class VoyageLeg
    {
        public int VoyageLegOrder { get; set; }
        public string DeparturePortCode { get; set; } = string.Empty;
        public string DeparturePortName { get; set; } = string.Empty;
        public string ArrivalPortCode { get; set; } = string.Empty;
        public string ArrivalPortName { get; set; } = string.Empty;
        public double Distance { get; set; }
    }

    public class RouteVersionReductionFactors
    {
        public string RouteVersionId { get; set; } = string.Empty;
        public List<VoyageLegReductionFactorData> VoyageLegReductionFactors { get; set; } = new List<VoyageLegReductionFactorData>();
    }

    public class VoyageLegReductionFactorData
    {
        public int VoyageLegOrder { get; set; }
        public ReductionFactorsData ReductionFactors { get; set; } = new ReductionFactorsData();
    }

    public class ReductionFactorsData
    {
        public double Annual { get; set; }
        public double Spring { get; set; }
        public double Summer { get; set; }
        public double Fall { get; set; }
        public double Winter { get; set; }
    }

    public class RecordReductionFactorsResponse
    {
        public string RecordId { get; set; } = string.Empty;
        public ReductionFactorsData ReductionFactors { get; set; } = new ReductionFactorsData();
    }
}