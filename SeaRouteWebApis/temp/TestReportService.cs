using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenEngApps.DigitalRules.CRoute.Services.API;
using NextGenEngApps.DigitalRules.CRoute.Services.API.Response;
using SeaRouteModel.Reports;
using SeaRouteModel.Models;

public interface IReportApiService
{
    Task<ReportDataCollector> LoadAllAsync(string recordId, string routeVersionId);
    Task<RecordReductionFactorsResponse?> GetRecordReductionFactorsAsync(string recordId);
    Task<RecordDetailsResponse?> GetRecordDetailsAsync(string recordId);
    Task<RouteVersionDetailsResponse?> GetRouteVersionDetailsAsync(string routeVersionId);
    Task<ActiveVesselResponse?> GetActiveVesselAsync(string recordId);
}

namespace NextGenEngApps.DigitalRules.CRoute.Services
{
    public class ReportApiService
    {
        private readonly CRouteAPIClient _apiClient;

        public ReportApiService(CRouteAPIClient apiClient)
        {
            _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        }

        public async Task<ReportDataCollector> LoadAllAsync(string recordId, string routeVersionId)
        {
            var reportDataCollector = new ReportDataCollector();

            try
            {
                // Parallel API calls for better performance
                var reductionFactorsTask = GetRecordReductionFactorsAsync(recordId);
                var recordDetailsTask = GetRecordDetailsAsync(recordId);
                var routeVersionDetailsTask = GetRouteVersionDetailsAsync(routeVersionId);
                var activeVesselTask = GetActiveVesselAsync(recordId);
                var routeLegsTask = GetRouteLegsAsync(recordId);

                await Task.WhenAll(reductionFactorsTask, recordDetailsTask, routeVersionDetailsTask,
                                 activeVesselTask, routeLegsTask);

                // Map data to ReportDataCollector
                MapDataToReportCollector(reportDataCollector,
                                       await reductionFactorsTask,
                                       await recordDetailsTask,
                                       await routeVersionDetailsTask,
                                       await activeVesselTask,
                                       await routeLegsTask);
            }
            catch (Exception)
            {
                throw;
            }

            return reportDataCollector;
        }

        private async Task<RecordReductionFactorsResponse> GetRecordReductionFactorsAsync(string recordId)
        {
            try
            {
                var response = await _apiClient._httpClient.GetAsync($"api/v1/records/{recordId}/reduction_factors");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<RecordReductionFactorsResponse>();
            }
            catch (Exception)
            {
                throw;
            }
        }

        private async Task<RecordDetailsResponse> GetRecordDetailsAsync(string recordId)
        {
            try
            {
                var response = await _apiClient._httpClient.GetAsync($"api/v1/records/{recordId}");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<RecordDetailsResponse>();
            }
            catch (Exception)
            {
                throw;
            }
        }

        private async Task<RouteVersionDetailsResponse> GetRouteVersionDetailsAsync(string routeVersionId)
        {
            try
            {
                var response = await _apiClient._httpClient.GetAsync($"api/v1/reports/route_versions/{routeVersionId}");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<RouteVersionDetailsResponse>();
            }
            catch (Exception)
            {
                throw;
            }
        }

        private async Task<ActiveVesselResponse> GetActiveVesselAsync(string recordId)
        {
            try
            {
                var response = await _apiClient._httpClient.GetAsync($"api/v1/records/{recordId}/active_vessel");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<ActiveVesselResponse>();
            }
            catch (Exception)
            {
                throw;
            }
        }

        private async Task<List<LegModel>> GetRouteLegsAsync(string recordId)
        {
            try
            {
                string userId = await GetCurrentUserIdAsync();
                var response = await _apiClient._httpClient.GetAsync($"api/v1/records/legs?userId={userId}&recordId={recordId}");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<List<LegModel>>();
            }
            catch (Exception)
            {
                throw;
            }
        }

        private async Task<string> GetCurrentUserIdAsync()
        {
            try
            {
                var identityUser = await _apiClient._authenticationService.GetIdentityUserAsync();
                if (identityUser == null || !identityUser.IsAuthenticated)
                    return string.Empty;

                return identityUser.UserId ?? string.Empty;
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        private void MapDataToReportCollector(ReportDataCollector reportDataCollector,
                                            RecordReductionFactorsResponse reductionFactors,
                                            RecordDetailsResponse recordDetails,
                                            RouteVersionDetailsResponse routeVersionDetails,
                                            ActiveVesselResponse activeVessel,
                                            List<LegModel> routeLegs)
        {
            // Set Report Title
            reportDataCollector.ReportTitle = $"ABS Reduction Factor Report - {recordDetails?.RouteName ?? "Route"}";

            // Map Report Info
            reportDataCollector.ReportInfo.ReportDate = DateTime.Now;
            reportDataCollector.ReportInfo.ReportName = recordDetails?.RouteName ?? string.Empty;

            // Map Vessel Info
            if (activeVessel?.Vessel != null)
            {
                reportDataCollector.VesselInfo.VesselName = activeVessel.Vessel.Name ?? string.Empty;
                reportDataCollector.VesselInfo.IMONumber = activeVessel.Vessel.Imo ?? string.Empty;
                reportDataCollector.VesselInfo.Flag = activeVessel.Vessel.Flag ?? string.Empty;
            }

            // Map Route Info - Extract ports from route legs
            if (routeLegs != null && routeLegs.Any())
            {
                var ports = new List<PortInfo>();

                // Add departure port from first leg
                var firstLeg = routeLegs.OrderBy(x => x.LegOrder).FirstOrDefault();
                if (firstLeg != null)
                {
                    ports.Add(new PortInfo
                    {
                        Name = firstLeg.DeparturePortName ?? string.Empty,
                        Unlocode = firstLeg.DeparturePortUnLocode ?? string.Empty
                    });
                }

                // Add all arrival ports
                foreach (var leg in routeLegs.OrderBy(x => x.LegOrder))
                {
                    ports.Add(new PortInfo
                    {
                        Name = leg.ArrivalPortName ?? string.Empty,
                        Unlocode = leg.ArrivalPortUnLocode ?? string.Empty
                    });
                }

                reportDataCollector.RouteInfo.Ports = ports;

                // Set Attention Block info
                if (ports.Count >= 2)
                {
                    reportDataCollector.AttentionBlock.DeparturePort = ports.First().Name;
                    reportDataCollector.AttentionBlock.ArrivalPort = ports.Last().Name;
                }
            }

            // Map Reduction Factor Results
            if (routeLegs != null && reductionFactors?.ReductionFactors != null)
            {
                var reductionFactorResults = new List<VoyageLegReductionFactor>();

                foreach (var leg in routeLegs.OrderBy(x => x.LegOrder))
                {
                    var voyageLegRF = new VoyageLegReductionFactor
                    {
                        LegOrder = leg.LegOrder,
                        DeparturePort = new PortInfo
                        {
                            Name = leg.DeparturePortName ?? string.Empty,
                            Unlocode = leg.DeparturePortUnLocode ?? string.Empty
                        },
                        ArrivalPort = new PortInfo
                        {
                            Name = leg.ArrivalPortName ?? string.Empty,
                            Unlocode = leg.ArrivalPortUnLocode ?? string.Empty
                        },
                        Distance = leg.Distance,
                        ReductionFactors = new SeasonalReductionFactors
                        {
                            Annual = reductionFactors.ReductionFactors.Annual,
                            Spring = reductionFactors.ReductionFactors.Spring,
                            Summer = reductionFactors.ReductionFactors.Summer,
                            Fall = reductionFactors.ReductionFactors.Fall,
                            Winter = reductionFactors.ReductionFactors.Winter
                        }
                    };

                    reductionFactorResults.Add(voyageLegRF);
                }

                reportDataCollector.ReductionFactorResults = reductionFactorResults;

                // Set overall reduction factor in attention block (using Annual as default)
                reportDataCollector.AttentionBlock.ReductionFactor = reductionFactors.ReductionFactors.Annual;
            }
        }
    }

    // Response models that match your API responses
    public class RecordReductionFactorsResponse
    {
        public string RecordId { get; set; } = string.Empty;
        public ReductionFactorsResponse ReductionFactors { get; set; } = new();
    }

    public class ReductionFactorsResponse
    {
        public double Annual { get; set; }
        public double Spring { get; set; }
        public double Summer { get; set; }
        public double Fall { get; set; }
        public double Winter { get; set; }
    }

    public class RecordDetailsResponse
    {
        public string RecordId { get; set; } = string.Empty;
        public string RouteName { get; set; } = string.Empty;
        public double RouteDistance { get; set; }
    }

    public class RouteVersionDetailsResponse
    {
        public string RouteVersionId { get; set; } = string.Empty;
        public string RecordDate { get; set; } = string.Empty;
    }

    public class ActiveVesselResponse
    {
        public string RecordId { get; set; } = string.Empty;
        public VesselResponse Vessel { get; set; } = new();
    }

    public class VesselResponse
    {
        public string Id { get; set; } = string.Empty;
        public string Imo { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Flag { get; set; } = string.Empty;
        public double Breadth { get; set; }
    }
}


==========================================
builder.Services.AddScoped<ReportApiService>();

=======================================
public class ReportDataCollector
{
    public RecordReductionFactorsResponse? ReductionFactors { get; set; }
    public RecordDetailsResponse? RecordDetails { get; set; }
    public RouteVersionDetailsResponse? RouteVersionDetails { get; set; }
    public ActiveVesselResponse? ActiveVessel { get; set; }

    private readonly ReportApiService _apiService;

    public ReportDataCollector(ReportApiService apiService)
    {
        _apiService = apiService;
    }

    public async Task LoadAllAsync(string recordId, string routeVersionId)
    {
        ReductionFactors = await _apiService.GetRecordReductionFactorsAsync(recordId);
        RecordDetails = await _apiService.GetRecordDetailsAsync(recordId);
        RouteVersionDetails = await _apiService.GetRouteVersionDetailsAsync(routeVersionId);
        ActiveVessel = await _apiService.GetActiveVesselAsync(recordId);
    }
}
=========================
private ReportDataCollector reportDataCollector = new ReportDataCollector();

private async Task LoadReportData()
{
    //// Get recordId from your RouteModel
    //string recordId = routeModel.RouteId;

    //// Get routeVersionId (if you have it in RouteModel, otherwise fetch via API)
    //string routeVersionId = routeModel.RouteVersionId; // If available

    //// If not available, you may need to fetch the latest route version for the recordId
    //// Example:
    //// var routeVersionDetails = await ReportApiService.GetRouteVersionDetailsAsync(recordId);
    //// string routeVersionId = routeVersionDetails?.RouteVersionId;

    //// Call the APIs and bind to ReportDataCollector
    //await reportDataCollector.LoadAllAsync(recordId, routeVersionId);


    // Simple usage
string recordId = routeModel.RouteId;
string routeVersionId = await GetRouteVersionIdFromRecordId(recordId); // You need to implement 
ReportDataCollector reportData = await _reportApiService.LoadAllAsync(recordId, routeVersionId);
}
